using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.CredentialManagement.Tests
{
    public class Tests
    {
        readonly ITestOutputHelper _output;

        public Tests(ITestOutputHelper output) {
            this._output = output;
        }

        static void CheckWin32Error() {
            var err = Marshal.GetLastWin32Error();
            if (err != 0)
                throw new Win32Exception(err);
        }

        [Fact]
        public void ReadWriteTargetCredentials() {
            var utcNow = DateTimeOffset.UtcNow;
            var credential = new Credential("http://bumble.bee.com", "elekta_tester", "must-not-reveal");
            credential.Write();
            CheckWin32Error();

            var credential2 = Credential.Read("http://bumble.bee.com", CredentialType.Generic);
            CheckWin32Error();
            
            Assert.NotNull(credential2);
            Assert.Equal(credential.Username, credential2.Username);
            Assert.Equal(credential.Password, credential2.Password);
            Assert.Equal(credential.Target, credential2.Target);
            // we expect the last write time to be within 10 milliseconds of the current time
            var timeDelta = credential2.LastWriteTime - utcNow;
            Assert.InRange(timeDelta, -TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void ReadWriteUserCredentials() {
            var utcNow = DateTimeOffset.UtcNow;
            var credential = new Credential("dummy", "elekta_tester", "must-not-reveal");
            credential.Write();
            CheckWin32Error();

            var credential2 = Credential.Read("dummy", CredentialType.Generic);
            CheckWin32Error();

            Assert.NotNull(credential2);
            Assert.Equal(credential.Username, credential2.Username);
            Assert.Equal(credential.Password, credential2.Password);
            Assert.Equal(credential.Target, credential2.Target);
            // we expect the last write time to be within 10 milliseconds of the current time
            var timeDelta = credential2.LastWriteTime - utcNow;
            Assert.InRange(timeDelta, -TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void EnumerateCredentials() {
            var credential = new Credential("dummy-dore", "elekta_tester", "must-not-reveal");
            credential.Write();
            CheckWin32Error();

            var credentials = Credential.Enumerate("dummy*");
            CheckWin32Error();
            foreach (var cred in credentials) {
                _output.WriteLine($"{cred.Target} - {cred.Username}");
            }

            var deleted = credential.Delete();
            Assert.True(deleted);
            CheckWin32Error();

            credentials = Credential.Enumerate("dummy*");
            foreach (var cred in credentials) {
                _output.WriteLine($"{cred.Target} - {cred.Username}");
            }
        }

        [Fact]
        public void FindBestCredential() {
            var credential = Credential.Enumerate().First(f => f.Type == CredentialType.Generic);
            var credential2 = Credential.FindBest(credential.Target, credential.Type);
            CheckWin32Error();
            _output.WriteLine($"{credential?.Target} - {credential?.Username}");
        }
    }
}
