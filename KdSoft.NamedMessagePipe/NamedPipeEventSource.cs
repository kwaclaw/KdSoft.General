using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Collections;
#if NETFRAMEWORK
using Newtonsoft.Json;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace KdSoft.NamedMessagePipe
{
    /// <summary><see cref="EventSource"/> implementation for instrumenting NamedMessagePipe operations.</summary>
    [EventSource]
    public class NamedPipeEventSource: EventSource
    {
        public const string DefaultName = "KdSoft." + nameof(NamedMessagePipe);

#if NETFRAMEWORK
        static readonly JsonSerializerSettings JsonSerializerSettings;

        static NamedPipeEventSource()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new PropertyIgnoreContractResolver
                {
                    // necessary to have the value treated as normal JSON object by Json.Net
                    IgnoreSerializableAttribute = true,
                    IgnoreSerializableInterface = true
                }
            .IgnoreProperty(typeof(Exception), "InnerException")
            .IgnoreProperty(typeof(AggregateException), "InnerExceptions"),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };
            JsonSerializerSettings = settings;
        }

        static string? SerializeException(ExceptionFlyweight ex)
        {
            try { return JsonConvert.SerializeObject(ex, JsonSerializerSettings); }
            catch { return ex.Message; }
        }

        static string SerializeExceptionData(IDictionary? exData)
        {
            if ((exData?.Count ?? 0) == 0)
                return "";
            try { return JsonConvert.SerializeObject(exData, JsonSerializerSettings); }
            catch { return $"Cannot serialize extra exception data: {exData?.Count} entries."; }
        }
#else
        static readonly JsonSerializerOptions JsonSerializerOptions;

        static NamedPipeEventSource() {
            var opts = new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.Preserve,
            };
            JsonSerializerOptions = opts;
        }

        static string? SerializeException(ExceptionFlyweight ex) {
            try { return JsonSerializer.Serialize(ex, JsonSerializerOptions); }
            catch { return ex.Message; }
        }

        static string SerializeExceptionData(IDictionary? exData) {
            if ((exData?.Count ?? 0) == 0)
                return "";
            try { return JsonSerializer.Serialize(exData, JsonSerializerOptions); }
            catch { return $"Cannot serialize extra exception data: {exData?.Count} entries."; }
        }
#endif

        NamedPipeEventSource(string name) : base(name) {
            //
        }

        /// <summary>The default <see cref="NamedPipeEventSource"/>  instance.</summary>
        public static readonly NamedPipeEventSource Log = CreateInstance();

        static NamedPipeEventSource CreateInstance() {
            var instance = new NamedPipeEventSource(DefaultName);
            return instance;
        }

        class ExceptionFlyweight
        {
            public ExceptionFlyweight() { }

            public ExceptionFlyweight(Exception ex) {
                Set(ex);
            }

            public void Set(Exception ex) {
                Type = ex.GetType().Name;
                Message = ex.Message;
                StackTrace = ex.StackTrace;
            }

            public string? Type { get; private set; }
            public string? Message { get; private set; }
            public string? StackTrace { get; private set; }
        }

        [NonEvent]
        static void AddBaseExceptions(Exception ex, ref IList<Exception>? baseExceptions) {
            if (baseExceptions == null)
                baseExceptions = new List<Exception>();
            var baseEx = ex.GetBaseException();
            if (baseEx is AggregateException aggEx) {
                var flattenedEx = aggEx.Flatten();
                foreach (var innerEx in flattenedEx.InnerExceptions) {
                    var baseInnerEx = innerEx.GetBaseException();
                    baseExceptions.Add(baseInnerEx);
                }
                return;
            }
            else if (!Object.ReferenceEquals(ex, baseEx)) {
                baseExceptions.Add(baseEx);
            }
        }

        #region Custom WriteEvent overloads

        [NonEvent]
        public unsafe void WriteEvent(int eventId, string? arg1, string? arg2, int arg3) {
            if (!IsEnabled()) {
                return;
            }

            if (arg1 == null)
                arg1 = "";
            if (arg2 == null)
                arg2 = "";
            fixed (char* arg1Ptr = arg1, arg2Ptr = arg2) {
                EventData* dataDesc = stackalloc EventData[3];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (arg1.Length + 1) * 2; // Size in bytes, including a null terminator.
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (arg2.Length + 1) * 2;
                dataDesc[2].DataPointer = (IntPtr)(&arg3);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(eventId, 3, dataDesc);
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3, int arg4) {
            if (!IsEnabled()) {
                return;
            }

            if (arg1 == null)
                arg1 = "";
            if (arg2 == null)
                arg2 = "";
            if (arg3 == null)
                arg3 = "";
            fixed (char* arg1Ptr = arg1, arg2Ptr = arg2, arg3Ptr = arg3) {
                EventData* dataDesc = stackalloc EventData[4];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (arg1.Length + 1) * 2; // Size in bytes, including a null terminator.
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (arg2.Length + 1) * 2;
                dataDesc[2].DataPointer = (IntPtr)arg3Ptr;
                dataDesc[2].Size = (arg3.Length + 1) * 2;
                dataDesc[3].DataPointer = (IntPtr)(&arg4);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(eventId, 4, dataDesc);
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3, string? arg4) {
            if (!IsEnabled()) {
                return;
            }

            if (arg1 == null)
                arg1 = "";
            if (arg2 == null)
                arg2 = "";
            if (arg3 == null)
                arg3 = "";
            if (arg4 == null)
                arg4 = "";
            fixed (char* arg1Ptr = arg1, arg2Ptr = arg2, arg3Ptr = arg3, arg4Ptr = arg4) {
                EventData* dataDesc = stackalloc EventData[4];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (arg1.Length + 1) * 2; // Size in bytes, including a null terminator.
                dataDesc[1].DataPointer = (IntPtr)arg2Ptr;
                dataDesc[1].Size = (arg2.Length + 1) * 2;
                dataDesc[2].DataPointer = (IntPtr)arg3Ptr;
                dataDesc[2].Size = (arg3.Length + 1) * 2;
                dataDesc[3].DataPointer = (IntPtr)arg4Ptr;
                dataDesc[3].Size = (arg4.Length + 1) * 2;

                WriteEventCore(eventId, 4, dataDesc);
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3, string? arg4, string? arg5) {
            if (!IsEnabled()) {
                return;
            }

            if (arg1 == null)
                arg1 = "";
            if (arg2 == null)
                arg2 = "";
            if (arg3 == null)
                arg3 = "";
            if (arg4 == null)
                arg4 = "";
            if (arg5 == null)
                arg5 = "";
            fixed (char* arg1Ptr = arg1, arg2Ptr = arg2, arg3Ptr = arg3, arg4Ptr = arg4, arg5Ptr = arg5) {
                EventData* dataDesc = stackalloc EventData[5];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (arg1.Length + 1) * 2; // Size in bytes, including a null terminator.
                dataDesc[1].DataPointer = (IntPtr)arg2Ptr;
                dataDesc[1].Size = (arg2.Length + 1) * 2;
                dataDesc[2].DataPointer = (IntPtr)arg3Ptr;
                dataDesc[2].Size = (arg3.Length + 1) * 2;
                dataDesc[3].DataPointer = (IntPtr)arg4Ptr;
                dataDesc[3].Size = (arg4.Length + 1) * 2;
                dataDesc[4].DataPointer = (IntPtr)arg5Ptr;
                dataDesc[4].Size = (arg5.Length + 1) * 2;

                WriteEventCore(eventId, 5, dataDesc);
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3, string? arg4, long arg5, long arg6) {
            if (!IsEnabled()) {
                return;
            }

            if (arg1 == null)
                arg1 = "";
            if (arg2 == null)
                arg2 = "";
            if (arg3 == null)
                arg3 = "";
            if (arg4 == null)
                arg4 = "";
            fixed (char* arg1Ptr = arg1, arg2Ptr = arg2, arg3Ptr = arg3, arg4Ptr = arg4) {
                EventData* dataDesc = stackalloc EventData[6];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (arg1.Length + 1) * 2; // Size in bytes, including a null terminator.
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (arg2.Length + 1) * 2;
                dataDesc[2].DataPointer = (IntPtr)arg3Ptr;
                dataDesc[2].Size = (arg3.Length + 1) * 2;
                dataDesc[3].DataPointer = (IntPtr)arg4Ptr;
                dataDesc[3].Size = (arg4.Length + 1) * 2;
                dataDesc[4].DataPointer = (IntPtr)(&arg5);
                dataDesc[4].Size = sizeof(long);
                dataDesc[5].DataPointer = (IntPtr)(&arg6);
                dataDesc[5].Size = sizeof(long);

                WriteEventCore(eventId, 6, dataDesc);
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3, string? arg4, string? arg5, string? arg6) {
            if (!IsEnabled()) {
                return;
            }

            if (arg1 == null)
                arg1 = "";
            if (arg2 == null)
                arg2 = "";
            if (arg3 == null)
                arg3 = "";
            if (arg4 == null)
                arg4 = "";
            if (arg5 == null)
                arg5 = "";
            if (arg6 == null)
                arg6 = "";
            fixed (char* arg1Ptr = arg1, arg2Ptr = arg2, arg3Ptr = arg3, arg4Ptr = arg4, arg5Ptr = arg5, arg6Ptr = arg6) {
                EventData* dataDesc = stackalloc EventData[6];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (arg1.Length + 1) * 2; // Size in bytes, including a null terminator.
                dataDesc[1].DataPointer = (IntPtr)arg2Ptr;
                dataDesc[1].Size = (arg2.Length + 1) * 2;
                dataDesc[2].DataPointer = (IntPtr)arg3Ptr;
                dataDesc[2].Size = (arg3.Length + 1) * 2;
                dataDesc[3].DataPointer = (IntPtr)arg4Ptr;
                dataDesc[3].Size = (arg4.Length + 1) * 2;
                dataDesc[4].DataPointer = (IntPtr)arg5Ptr;
                dataDesc[4].Size = (arg5.Length + 1) * 2;
                dataDesc[5].DataPointer = (IntPtr)arg6Ptr;
                dataDesc[5].Size = (arg6.Length + 1) * 2;

                WriteEventCore(eventId, 6, dataDesc);
            }
        }

        #endregion

        [NonEvent]
        static void SerializeException(Exception ex, StringBuilder sb, ExceptionFlyweight? exFlyweight = null) {
            if (exFlyweight is null)
                exFlyweight = new ExceptionFlyweight(ex);
            else
                exFlyweight.Set(ex);
            sb.AppendLine(SerializeException(exFlyweight));
        }

        [NonEvent]
        public void LogException(string serviceType, string pipeName, string instanceId, Exception ex) {
            IList<Exception>? baseExceptions = null;
            AddBaseExceptions(ex, ref baseExceptions);

            var sb = new StringBuilder();
            var exFlyweight = new ExceptionFlyweight();

            SerializeException(ex, sb, exFlyweight);
            LogError(serviceType, pipeName, instanceId, sb.ToString(), SerializeExceptionData(ex.Data));

            foreach (var baseEx in baseExceptions!) {
                sb.Clear();
                SerializeException(baseEx, sb, exFlyweight);
                LogError(serviceType, pipeName, instanceId, sb.ToString(), SerializeExceptionData(baseEx.Data));
            }
        }

        [Event(1, Level = EventLevel.Error)]
        public void LogError(string serviceType, string pipeName, string instanceId, string message, string? data = null, string? details = null) {
            WriteEvent(1, serviceType, pipeName, instanceId, message, data, details);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void LogInfo(string serviceType, string pipeName, string instanceId, string message, long data1 = 0, long data2 = 0) {
            WriteEvent(2, serviceType, pipeName, instanceId, message, data1, data2);
        }

        #region GetMessages

        [Event(3, Level = EventLevel.Informational)]
        public void GetMessagesBeginRead(string pipeType, string pipeName, string instanceId) {
            WriteEvent(3, pipeType, pipeName, instanceId);
        }

        [Event(4, Level = EventLevel.Informational)]
        public void GetMessagesEndRead(string pipeType, string pipeName, string instanceId) {
            WriteEvent(4, pipeType, pipeName, instanceId);
        }

        [Event(5, Level = EventLevel.Informational)]
        public void GetMessagesCancel(string pipeType, string pipeName, string instanceId) {
            WriteEvent(5, pipeType, pipeName, instanceId);
        }

        [Event(6, Level = EventLevel.Informational)]
        public void GetMessagesEnd(string pipeType, string pipeName, string instanceId, bool fromBreak) {
            WriteEvent(6, pipeType, pipeName, instanceId, fromBreak ? 1 : 0);
        }

        [NonEvent]
        public void GetMessagesError(string pipeType, string pipeName, string instanceId, Exception ex) {
            IList<Exception>? baseExceptions = null;
            AddBaseExceptions(ex, ref baseExceptions);

            var sb = new StringBuilder();
            var exFlyweight = new ExceptionFlyweight();
            SerializeException(ex, sb, exFlyweight);
            GetMessagesError(pipeType, pipeName, instanceId, sb.ToString(), SerializeExceptionData(ex.Data));

            foreach (var baseEx in baseExceptions!) {
                sb.Clear();
                SerializeException(baseEx, sb, exFlyweight);
                GetMessagesError(pipeType, pipeName, instanceId, sb.ToString(), SerializeExceptionData(baseEx.Data));
            }
        }

        [Event(7, Level = EventLevel.Error)]
        public void GetMessagesError(string pipeType, string pipeName, string instanceId, string error, string details) {
            WriteEvent(7, pipeType, pipeName, instanceId, error, details);
        }

        #endregion

        #region Client Listen

        [Event(8, Level = EventLevel.Informational)]
        public void ListenBeginRead(string pipeType, string pipeName, string instanceId) {
            WriteEvent(8, pipeType, pipeName, instanceId);
        }

        [Event(9, Level = EventLevel.Informational)]
        public void ListenEndRead(string pipeType, string pipeName, string instanceId) {
            WriteEvent(9, pipeType, pipeName, instanceId);
        }

        [Event(10, Level = EventLevel.Informational)]
        public void ListenCancel(string pipeType, string pipeName, string instanceId) {
            WriteEvent(10, pipeType, pipeName, instanceId);
        }

        [Event(11, Level = EventLevel.Informational)]
        public void ListenEnd(string pipeType, string pipeName, string instanceId) {
            WriteEvent(11, pipeType, pipeName, instanceId);
        }

        [NonEvent]
        public void ListenError(string pipeType, string pipeName, string instanceId, Exception ex) {
            IList<Exception>? baseExceptions = null;
            AddBaseExceptions(ex, ref baseExceptions);

            var sb = new StringBuilder();
            var exFlyweight = new ExceptionFlyweight();
            SerializeException(ex, sb, exFlyweight);
            ListenError(pipeType, pipeName, instanceId, sb.ToString(), SerializeExceptionData(ex.Data));

            foreach (var baseEx in baseExceptions!) {
                sb.Clear();
                SerializeException(baseEx, sb, exFlyweight);
                ListenError(pipeType, pipeName, instanceId, sb.ToString(), SerializeExceptionData(baseEx.Data));
            }
        }

        [Event(12, Level = EventLevel.Error)]
        public void ListenError(string pipeType, string pipeName, string instanceId, string error, string details) {
            WriteEvent(12, pipeType, pipeName, instanceId, error, details);
        }

        #endregion

        #region Client Connection

        [Event(13, Level = EventLevel.Informational)]
        public void ClientConnected(string pipeName, string instanceId) {
            WriteEvent(13, pipeName, instanceId);
        }

        [NonEvent]
        public void ClientConnectError(string pipeName, string instanceId, Exception ex) {
            IList<Exception>? baseExceptions = null;
            AddBaseExceptions(ex, ref baseExceptions);

            var sb = new StringBuilder();
            var exFlyweight = new ExceptionFlyweight();
            SerializeException(ex, sb, exFlyweight);
            ClientConnectError(pipeName, instanceId, sb.ToString(), SerializeExceptionData(ex.Data));

            foreach (var baseEx in baseExceptions!) {
                sb.Clear();
                SerializeException(baseEx, sb, exFlyweight);
                ClientConnectError(pipeName, instanceId, sb.ToString(), SerializeExceptionData(baseEx.Data));
            }
        }

        [Event(14, Level = EventLevel.Error)]
        public void ClientConnectError(string pipeName, string instanceId, string error, string details) {
            WriteEvent(14, pipeName, instanceId, error, details);
        }

        #endregion

        #region Server Connection

        [Event(15, Level = EventLevel.Informational)]
        public void ServerWaitForConnection(string pipeName, string instanceId) {
            WriteEvent(15, pipeName, instanceId);
        }

        [Event(16, Level = EventLevel.Informational)]
        public void ServerConnectedToClient(string pipeName, string instanceId) {
            WriteEvent(16, pipeName, instanceId);
        }

        [Event(17, Level = EventLevel.Informational)]
        public void ServerDisconnectedFromClient(string pipeName, string instanceId, bool endedByClient) {
            WriteEvent(17, pipeName, instanceId, endedByClient ? 1 : 0); //kw
        }

        [NonEvent]
        public void ServerWaitForConnectionError(string pipeName, string instanceId, Exception ex) {
            IList<Exception>? baseExceptions = null;
            AddBaseExceptions(ex, ref baseExceptions);

            var sb = new StringBuilder();
            var exFlyweight = new ExceptionFlyweight();
            SerializeException(ex, sb, exFlyweight);
            ServerWaitForConnectionError(pipeName, instanceId, sb.ToString(), SerializeExceptionData(ex.Data));

            foreach (var baseEx in baseExceptions!) {
                sb.Clear();
                SerializeException(baseEx, sb, exFlyweight);
                ServerWaitForConnectionError(pipeName, instanceId, sb.ToString(), SerializeExceptionData(baseEx.Data));
            }
        }

        [Event(18, Level = EventLevel.Error)]
        public void ServerWaitForConnectionError(string pipeName, string instanceId, string error, string details) {
            WriteEvent(18, pipeName, instanceId, error, details);
        }

        #endregion
    }
}
