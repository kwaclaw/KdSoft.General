using System;
using System.Runtime.InteropServices;

namespace KdSoft.CredentialManagement
{
    /// <summary>
    /// Based on https://learn.microsoft.com/en-us/windows/win32/api/wincred/
    /// </summary>
    internal class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct CREDENTIAL
        {
            public int Flags;
            public CredentialType Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetName;  // must be <= 256 characters
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? Comment;  // must be <= 256 characters
            public long LastWritten;  // UTC, ignored for writes
            public int CredentialBlobSize; // must be <= 5*512 bytes
            public IntPtr CredentialBlob;
            public PersistenceType Persist;
            public int AttributeCount;  // must be <= 64
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? TargetAlias;  // must be <= 256 characters
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? UserName; // must be <= 513 characters
        }

        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead([In] string target, [In] CredentialType type, [Optional] int flags, [Out] out SafeCredentialHandle credentialPtr);

        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] int flags);

        [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        public static extern bool CredFree([In] IntPtr cred);

        [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredDelete([In] string target, [In] CredentialType type, [In] int flags);

        [DllImport("Advapi32.dll", EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredEnumerate([In] string? filter, [In] CredFlags flags, [Out] out int count, [Out] out SafeCredentialHandle credentialPtr);

        [DllImport("Advapi32.dll", EntryPoint = "CredFindBestCredentialW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredFindBestCredential([In] string target, [In] CredentialType type, [Optional] int flags, [Out] out SafeCredentialHandle credentialPtr);
    }

    public enum CredentialType: int
    {
        None = 0,
        Generic = 1,
        DomainPassword = 2,
        DomainCertificate = 3,
        DomainVisiblePassword = 4,
        GenericCertificate = 5,
        DomainExtended = 6,
        Maximum = 7,
        MaximumEx = Maximum + 1000
    }

    public enum PersistenceType: int
    {
        Session = 1,
        LocalMachine = 2,
        Enterprise = 3
    }

    [Flags]
    public enum CredFlags: int
    {
        None = 0,
        EnumerateAllCredentials = 1,
    }
}
