using System;
using System.Runtime.CompilerServices;
using System.Text;
using static KdSoft.CredentialManagement.NativeMethods;

namespace KdSoft.CredentialManagement
{
    public class Credential
    {
        CredentialType _type;
        PersistenceType _persistenceType;
        DateTimeOffset _lastWriteTime;

        string _target = "";
        string? _targetAlias;
        string? _username;
        string? _password;
        string? _comment;

        public Credential() {
        }

        public Credential(string target, string? username = null, string? password = null, CredentialType type = CredentialType.Generic) {
            this.Target = target;
            this.Username = username;
            this.Password = password;
            this.Type = type;
            this.PersistenceType = PersistenceType.LocalMachine;
        }

        public string? Username {
            get => _username;
            set => _username = CheckStringLength(value, 513);
        }

        public string? Password {
            get => _password;
            set => _password = value;
        }

        public string Target {
            get => _target;
            set => _target = CheckStringLength(value, 256)!;
        }

        public string? TargetAlias {
            get => _targetAlias;
            set => _targetAlias = CheckStringLength(value, 256);
        }

        public string? Comment {
            get => _comment;
            set => _comment = CheckStringLength(value, 256);
        }

        public CredentialType Type {
            get => _type;
            set => _type = value;
        }

        public PersistenceType PersistenceType {
            get => _persistenceType;
            set => _persistenceType = value;
        }

        public DateTimeOffset LastWriteTime => _lastWriteTime;

        public static Credential? Read(string target, CredentialType type) {
            bool success = CredRead(target, type, 0, out var credentialHandle);
            if (!success) {
                return null;
            }
            using (credentialHandle) {
                var result = new Credential();
                result.LoadInternal(credentialHandle.GetCredential());
                return result;
            }
        }

        public static bool Exists(string target, CredentialType type) {
            if (CredRead(target, type, 0, out var credentialHandle)) {
                credentialHandle.Dispose();
                return true;
            }
            return false;
        }

        public static bool Delete(string target, CredentialType type) {
            return CredDelete(target, type, 0);
        }

        public static Credential? FindBest(string target, CredentialType type) {
            bool success = CredFindBestCredential(target, type, 0, out var credentialHandle);
            if (!success) {
                return null;
            }
            using (credentialHandle) {
                var result = new Credential();
                result.LoadInternal(credentialHandle.GetCredential());
                return result;
            }
        }

        // filter can have wildcard at end
        public static Credential[] Enumerate(string? filter = null) {
            var flags = filter is null ? CredFlags.EnumerateAllCredentials : CredFlags.None;
            bool success = CredEnumerate(filter, flags, out var count, out var credentialHandle);
            if (!success) {
                return Array.Empty<Credential>();
            }
            using (credentialHandle) {
                var result = new Credential[count];
                var nativeCreds = new CREDENTIAL[count];
                credentialHandle.GetCredentials(nativeCreds);
                for (int indx = 0; indx < nativeCreds.Length; indx++) {
                    var cred = new Credential();
                    cred.LoadInternal(nativeCreds[indx]);
                    result[indx] = cred;
                }
                return result;
            }
        }

        unsafe public bool Write() {
            if (string.IsNullOrEmpty(_target)) {
                throw new InvalidOperationException("Target must be specified to write a credential.");
            }
            byte[]? passwordBytes = Password is null ? null : Encoding.Unicode.GetBytes(Password);
            if (passwordBytes?.Length > (5 * 512)) {
                throw new ArgumentOutOfRangeException($"The password size must not exceed {5 * 512} bytes.");
            }

            fixed (byte* pwdPtr = passwordBytes) {
                var credential = new CREDENTIAL {
                    TargetName = _target,
                    TargetAlias = _targetAlias,
                    UserName = _username,
                    CredentialBlob = (IntPtr)pwdPtr,
                    CredentialBlobSize = passwordBytes?.Length ?? 0,
                    Comment = _comment,
                    Type = _type,
                    Persist = _persistenceType,
                };

                bool result = CredWrite(ref credential, 0);
                return result;
            }
        }

        public bool Delete() {
            if (string.IsNullOrEmpty(_target)) {
                throw new InvalidOperationException("Target must be specified to delete a credential.");
            }
            return Delete(_target!, _type);
        }

        public bool Read() {
            bool result = NativeMethods.CredRead(_target, _type, 0, out var credentialHandle);
            if (!result) {
                return false;
            }
            using (credentialHandle) {
                LoadInternal(credentialHandle.GetCredential());
            }
            return true;
        }

        public bool Exists() {
            if (string.IsNullOrEmpty(_target)) {
                throw new InvalidOperationException("Target must be specified to check existence of a credential.");
            }
            return Exists(_target!, _type);
        }

        static string? CheckStringLength(string? value, int maxLength, [CallerMemberName] string propertyName = "") {
            if (value?.Length > maxLength)
                throw new ArgumentOutOfRangeException($"The size of {propertyName} must not exceed {maxLength} characters.");
            return value;
        }

        unsafe void LoadInternal(NativeMethods.CREDENTIAL credential) {
            _username = credential.UserName;

            if (credential.CredentialBlobSize > 0) {
                byte* pwdPtr = (byte*)credential.CredentialBlob.ToPointer();
                _password = Encoding.Unicode.GetString(pwdPtr, credential.CredentialBlobSize);
            }
            _target = credential.TargetName;
            _targetAlias = credential.TargetName;
            _type = credential.Type;
            _persistenceType = credential.Persist;
            _comment = credential.Comment;
            _lastWriteTime = DateTimeOffset.FromFileTime(credential.LastWritten);
        }
    }
}
