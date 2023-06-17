#if NETFRAMEWORK

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text.Json;

namespace KdSoft.NamedMessagePipe.Tests
{
    public class NamedPipeTestFixtureFramework: NamedPipeTestFixtureBase
    {
        readonly TestEventListener _eventListener;
        readonly Stream _logPipeWriterStream;
        readonly Utf8JsonWriter _jsonWriter;
        readonly object _lock = new object();

        public NamedPipeTestFixtureFramework() {
            _logPipeWriterStream = _logPipe.Writer.AsStream();
            _jsonWriter = new Utf8JsonWriter(_logPipeWriterStream, _jsonOptions);
            _eventListener = new TestEventListener();
            _eventListener.EventWritten += (s, evt) => {
                lock (_lock) {
                    WriteEventJson(evt, _jsonWriter);
                    _logPipeWriterStream.Write(_newLine, 0, _newLine.Length);
                }
            };
        }

        void WriteEventJson(EventWrittenEventArgs evt, Utf8JsonWriter jsonWriter) {
            jsonWriter.Reset();
            jsonWriter.WriteStartObject();

            jsonWriter.WriteString("providerName", NamedPipeEventSource.Log.Name);
            jsonWriter.WriteNumber("channel", (uint)evt.Channel);
            jsonWriter.WriteNumber("id", (uint)evt.EventId);
            jsonWriter.WriteNumber("keywords", (long)evt.Keywords);
            jsonWriter.WriteString("level", evt.Level.ToString());
            jsonWriter.WriteNumber("opcode", (uint)evt.Opcode);
            jsonWriter.WriteString("eventName", evt.EventName);
            jsonWriter.WriteString("timeStamp", DateTimeOffset.UtcNow.ToString("o"));
            jsonWriter.WriteNumber("version", evt.Version);

            jsonWriter.WriteStartObject("payload");
            if (evt.Payload is not null) {
                for (int indx = 0; indx < evt.PayloadNames.Count; indx++) {
                    var payloadName = evt.PayloadNames[indx];
                    var payloadValue = evt.Payload[indx].ToString();
                    jsonWriter.WriteString(payloadName, payloadValue);
                }
            }
            jsonWriter.WriteEndObject();

            jsonWriter.WriteEndObject();
            jsonWriter.Flush();
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _eventListener.Dispose();
                _logPipeWriterStream.Close();
                _jsonWriter.Dispose();
                _logFileTask.Wait();
            }
            base.Dispose(disposing);
        }

        class TestEventListener: EventListener
        {
            protected override void OnEventSourceCreated(EventSource eventSource) {
                base.OnEventSourceCreated(eventSource);

                if (eventSource.Name == NamedPipeEventSource.DefaultName) {
                    EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
                }
            }
        }
    }
}

#endif