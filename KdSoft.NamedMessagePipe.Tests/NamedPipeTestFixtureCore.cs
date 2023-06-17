#if !NETFRAMEWORK

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace KdSoft.NamedMessagePipe.Tests
{
    public class NamedPipeTestFixtureCore: NamedPipeTestFixtureBase
    {
        EventPipeSession? _session;
        readonly ArrayBufferWriter<byte> _bufferWriter;
        readonly ReadOnlyMemory<byte> _newLineMemory;

        public NamedPipeTestFixtureCore() {
            _newLineMemory = base._newLine;

            _bufferWriter = new ArrayBufferWriter<byte>(1024);

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider(NamedPipeEventSource.DefaultName, EventLevel.Informational)
            };

            var logTask = Task.Run(() => {
                using var jsonWriter = new Utf8JsonWriter(_bufferWriter, _jsonOptions);

                var eventPipeSource = InitializeEventPipe(providers, jsonWriter);
                if (eventPipeSource == null) {
                    Debug.WriteLine("Could not initialize EventPipe.");
                    return;
                }
                try {
                    eventPipeSource.Process();
                }
                catch (Exception ex) {
                    Debug.WriteLine("Error encountered while processing events");
                    Debug.WriteLine(ex.ToString());
                }
                finally {
                    _logPipe.Writer.Complete();
                }
            });
        }

        void WriteEventJson(TraceEvent evt, Utf8JsonWriter jsonWriter) {
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
        }

        EventPipeEventSource? InitializeEventPipe(List<EventPipeProvider> providers, Utf8JsonWriter jsonWriter) {
           try {
                var client = new DiagnosticsClient(Process.GetCurrentProcess().Id);
                //TODO this hangs in full .NET framework
                _session = client.StartEventPipeSession(providers, false);

                var source = new EventPipeEventSource(_session.EventStream);
                source.Dynamic.All += (TraceEvent evt) => {
                    try {
                        _bufferWriter.Clear();
                        WriteEventJson(evt, jsonWriter);
                        _bufferWriter.Write(_newLineMemory.Span);
                        _logPipe.Writer.Write(_bufferWriter.WrittenMemory.Span);
                    }
                    catch {
                        //
                    }
                };
                return source;
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
                _logPipe.Writer.Complete();
                return null;
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _session?.StopAsync(new CancellationTokenSource(5000).Token).Wait();
                _session?.Dispose();
                _logFileTask.Wait();
            }
            base.Dispose(disposing);
        }
    }
}

#endif