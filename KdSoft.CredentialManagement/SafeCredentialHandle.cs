using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static KdSoft.CredentialManagement.NativeMethods;

namespace KdSoft.CredentialManagement
{
    internal class SafeCredentialHandle: SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeCredentialHandle() : base(true) { }

        protected override bool ReleaseHandle() {
            return CredFree(handle);
        }

        public CREDENTIAL GetCredential() {
            if (IsInvalid) {
                throw new InvalidOperationException("Invalid SafeCredentialHandle!");
            }
            return (CREDENTIAL)Marshal.PtrToStructure(handle, typeof(CREDENTIAL))!;
        }

        /// <summary>
        /// Loads CREDENTIAL array from SafeHandle. Danger of accessing out of bounds memory if array is too long.
        /// </summary>
        /// <param name="credentials">Array to fill.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void GetCredentials(CREDENTIAL[] credentials) {
            if (IsInvalid) {
                throw new InvalidOperationException("Invalid SafeCredentialHandle!");
            }
            //var nativeCredPtrs = (CREDENTIAL**)handle;
            //for (int indx = 0; indx < credentials.Length; indx++) {
            //    var nativeCredPtr = (IntPtr)nativeCredPtrs[indx];
            //    credentials[indx] = Marshal.PtrToStructure<CREDENTIAL>(nativeCredPtr);
            //}
            var nativeCredsBasePtr = handle;
            for (int indx = 0; indx < credentials.Length; indx++) {
                var nativeCredPtr = Marshal.PtrToStructure<IntPtr>(nativeCredsBasePtr);
                credentials[indx] = Marshal.PtrToStructure<CREDENTIAL>(nativeCredPtr);
                nativeCredsBasePtr += Marshal.SizeOf(typeof(IntPtr));
            }
        }
    }
}
