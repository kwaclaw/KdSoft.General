using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using KdSoft.Utils;
using KdSoft.Utils.Tests;

namespace KdSoft.NamedMessagePipe.Tests
{
    public class NamedPipeTestFixtureBase: IDisposable
    {
        protected readonly RollingFileFactory _fileFactory;
        protected readonly Pipe _logPipe;
        protected readonly byte[] _newLine = new byte[] { 10 };
        protected readonly JsonWriterOptions _jsonOptions;
        protected readonly Task _logFileTask;

        public NamedPipeTestFixtureBase() {
            var logDirInfo = new DirectoryInfo(Path.Combine(TestUtils.ProjectDir!, "Logs"));
            logDirInfo.Create();

            Func<DateTimeOffset, string> fileNameSelector = (dto) => string.Format("app-{0:yyyy-MM-dd}", dto);

            _fileFactory = new RollingFileFactory(
                logDirInfo,
                fileNameSelector,
                ".txt",
                true,
                4096,
                11,
                true
            );

            _logPipe = new Pipe();

            _jsonOptions = new JsonWriterOptions {
                Indented = false,
                SkipValidation = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            _logFileTask = LogToFile();
        }

        async Task LogToFile() {
            do {
                // last stream will be closed when disposing _fileFactory
                var stream = _fileFactory.GetCurrentFileStream();

                var res = await _logPipe.Reader.ReadAsync().ConfigureAwait(false);
                var position = res.Buffer.GetPosition(0);
                while (res.Buffer.TryGet(ref position, out var memory)) {
#if NETFRAMEWORK
                    var resArray = res.Buffer.ToArray();
                    _logPipe.Reader.AdvanceTo(res.Buffer.End);
                    await stream.WriteAsync(resArray, 0, resArray.Length).ConfigureAwait(false);
#else
                    await stream.WriteAsync(memory).ConfigureAwait(false);
#endif
                }

                if (res.IsCanceled || res.IsCompleted) {
                    break;
                }
            }
            while (true);

            // do not do this - the fileFactory disposes the last used stream
            // stream?.Dispose();
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                _fileFactory.Dispose();
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
