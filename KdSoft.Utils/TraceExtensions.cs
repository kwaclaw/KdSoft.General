using System;
using System.Diagnostics;

namespace KdSoft.Utils
{
    /// <summary>
    /// These TraceSource extensions allow creation of the trace arguments and message to be deferred until after
    /// the SourceSwitch has determined that they are needed. This has the advantage that for those trace levels 
    /// that are turned off, no trace arguments will be created. This is especially an improvement for formatted strings.
    /// </summary>
    public static class TraceExtensions
    {
        public static void Trace(this TraceSource trs, TraceEventType eventType, Func<Tuple<int, object>> traceDataArgs) {
            if (trs.Switch.ShouldTrace(eventType)) {
                var tda = traceDataArgs();
                trs.TraceData(eventType, tda.Item1, tda.Item2);
            }
        }

        public static void Trace(this TraceSource trs, TraceEventType eventType, Func<Tuple<int, object[]>> traceDataArgs) {
            if (trs.Switch.ShouldTrace(eventType)) {
                var tda = traceDataArgs();
                trs.TraceData(eventType, tda.Item1, tda.Item2);
            }
        }

        public static void Trace(this TraceSource trs, TraceEventType eventType, Func<int> traceDataArgs) {
            if (trs.Switch.ShouldTrace(eventType)) {
                trs.TraceData(eventType, traceDataArgs());
            }
        }

        public static void Trace(this TraceSource trs, TraceEventType eventType, Func<Tuple<int, string, object[]>> traceDataArgs) {
            if (trs.Switch.ShouldTrace(eventType)) {
                var tda = traceDataArgs();
                trs.TraceData(eventType, tda.Item1, tda.Item2, tda.Item3);
            }
        }

        public static void Trace(this TraceSource trs, TraceEventType eventType, Func<Tuple<int, string>> traceDataArgs) {
            if (trs.Switch.ShouldTrace(eventType)) {
                var tda = traceDataArgs();
                trs.TraceData(eventType, tda.Item1, tda.Item2);
            }
        }
    }
}
