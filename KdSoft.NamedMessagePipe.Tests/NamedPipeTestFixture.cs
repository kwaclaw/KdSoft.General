using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.IO.Pipelines;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KdSoft.Utils;
using KdSoft.Utils.Tests;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace KdSoft.NamedMessagePipe.Tests
{
#if !NETFRAMEWORK
    public class NamedPipeTestFixture: IDisposable
    {
        EventPipeSession? _session;
        readonly RollingFileFactory _fileFactory;
        readonly Pipe _logPipe;
        readonly ArrayBufferWriter<byte> _bufferWriter;
        readonly ReadOnlyMemory<byte> _newLine = new byte[] { 10 };

        public NamedPipeTestFixture() {
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

            _bufferWriter = new ArrayBufferWriter<byte>(1024);
            var jsonOptions = new JsonWriterOptions {
                Indented = false,
                SkipValidation = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider(NamedPipeEventSource.Log.Name, EventLevel.Informational)
            };

            var logFileTask = Task.Run(async () => {
                do {
                    // last stream will be closed when disposing _fileFactory
                    var stream = _fileFactory.GetCurrentFileStream();

                    var res = await _logPipe.Reader.ReadAsync().ConfigureAwait(false);
                    var position = res.Buffer.GetPosition(0);
                    while (res.Buffer.TryGet(ref position, out var memory)) {
                        await stream.WriteAsync(memory).ConfigureAwait(false);
                    }

                    if (res.IsCanceled || res.IsCompleted) {
                        break;
                    }
                }
                while (true);

                // do not do this - the fileFactory disposes the last used stream
                // stream?.Dispose();
            });

            var logTask = Task.Run(() => LogEvents(providers, jsonOptions));
        }

        void WriteEventJson(TraceEvent evt, Utf8JsonWriter jsonWriter, ArrayBufferWriter<byte> bufferWriter) {
            jsonWriter.Reset();
            jsonWriter.WriteStartObject();

            jsonWriter.WriteString("providerName", evt.ProviderName);
            jsonWriter.WriteNumber("channel", (uint)evt.Channel);
            jsonWriter.WriteNumber("id", (uint)evt.ID);
            jsonWriter.WriteNumber("keywords", (long)evt.Keywords);
            jsonWriter.WriteString("level", evt.Level.ToString());
            jsonWriter.WriteNumber("opcode", (uint)evt.Opcode);
            jsonWriter.WriteString("opcodeName", evt.OpcodeName);
            jsonWriter.WriteString("taskName", evt.TaskName);
            if (evt.TimeStamp == default)
                jsonWriter.WriteString("timeStamp", DateTimeOffset.UtcNow.ToString("o"));
            else {
                jsonWriter.WriteString("timeStamp", evt.TimeStamp.ToUniversalTime().ToString("o"));
            }
            jsonWriter.WriteNumber("version", evt.Version);

            jsonWriter.WriteStartObject("payload");
            for (int indx = 0; indx < evt.PayloadNames.Length; indx++) {
                var payloadName = evt.PayloadNames[indx];
                var payloadValue = evt.PayloadString(indx);
                jsonWriter.WriteString(payloadName, payloadValue);
            }
            jsonWriter.WriteEndObject();

            jsonWriter.WriteEndObject();
            jsonWriter.Flush();

            bufferWriter.Write(_newLine.Span);
        }

        readonly object _syncObj = new object();

        void LogEvents(List<EventPipeProvider> providers, JsonWriterOptions jsonOptions) {
            using var jsonWriter = new Utf8JsonWriter(_bufferWriter, jsonOptions);

            var client = new DiagnosticsClient(Process.GetCurrentProcess().Id);
            _session = client.StartEventPipeSession(providers, false);

            var source = new EventPipeEventSource(_session.EventStream);
            source.Dynamic.All += (TraceEvent evt) => {
                try {
                    _bufferWriter.Clear();
                    WriteEventJson(evt, jsonWriter, _bufferWriter);
                    _logPipe.Writer.Write(_bufferWriter.WrittenMemory.Span);
                }
                catch {
                    //
                }
                finally {
                    // 
                }
            };

            try {
                source.Process();
            }
            catch (Exception ex) {
                Debug.WriteLine("Error encountered while processing events");
                Debug.WriteLine(ex.ToString());
            }
            finally {
                _logPipe.Writer.Complete();
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                _session?.StopAsync(new CancellationTokenSource(5000).Token).Wait();
                _session?.Dispose();
                _fileFactory.Dispose();
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
#endif
}
