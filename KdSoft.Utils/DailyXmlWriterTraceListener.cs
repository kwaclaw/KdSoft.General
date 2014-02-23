using System;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;

namespace KdSoft.Utils
{

  /// <summary>
  /// An extended XmlWriterTraceListener that starts a new file for every day.
  /// <example>
  ///     <code>
  ///         <sharedListeners>
  ///             <add type="KdSoft.Utils.DailyXmlWriterTraceListener, KdSoft.Utils" name="MyTraceListener" traceOutputOptions="None" initializeData="C:\Logs\MyTraceFileName.svclog" />
  ///         </sharedListeners>
  ///     </code>
  /// </example>
  /// </summary>
  [HostProtection(Synchronization = true)]
  public class DailyXmlWriterTraceListener : XmlWriterTraceListener
  {
    string baseTraceFileName;
    DateTime activeUtcFileDate;

    /// <summary>
    /// Initializes a new instance of the <see cref="DailyXmlWriterTraceListener"/> class by specifying the trace file
    /// name.
    /// </summary>
    /// <param name="filename">The trace file name.</param>
    public DailyXmlWriterTraceListener(string filename) : base(filename) {
      Initialize(filename);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DailyXmlWriterTraceListener"/> class by specifying the trace file
    /// name and the name of the new instance.
    /// </summary>
    /// <param name="filename">The trace file name.</param>
    /// <param name="name">The name of the new instance.</param>
    public DailyXmlWriterTraceListener(string filename, string name) : base(filename, name) {
      Initialize(filename);
    }

    void Initialize(string fileName) {
      this.activeUtcFileDate = DateTime.UtcNow.Date;
      this.baseTraceFileName = fileName;

      // create a new file stream and a new stream writer and pass it to the listener
      this.Writer = new StreamWriter(new FileStream(GetTraceFileName(activeUtcFileDate), FileMode.Append));
    }

    string GetDatePostfix(DateTime date) {
      var dtStr = date.ToString("o");
      int sepIndex = dtStr.IndexOf('T');
      return dtStr.Substring(0, sepIndex);
    }

    string GetTraceFileName(DateTime date) {
      var fileDir = Path.GetDirectoryName(baseTraceFileName);
      var fileName = Path.GetFileNameWithoutExtension(baseTraceFileName) + "_UTC_" 
        + GetDatePostfix(date) + Path.GetExtension(baseTraceFileName);
      return Path.Combine(fileDir, fileName);
    }

    void OpenNewTraceFile(DateTime date) {
      Flush();
      // get the underlying file stream and close it
      StreamWriter streamWriter = (StreamWriter)this.Writer;
      FileStream fileStream = (FileStream)streamWriter.BaseStream;
      fileStream.Close();
      // create a new file stream and a new stream writer and pass it to the listener
      this.Writer = new StreamWriter(new FileStream(GetTraceFileName(date), FileMode.Append));
    }

    protected bool CheckRollover() {
      var currentUtcDate = DateTime.UtcNow.Date;
      var dateDiff = currentUtcDate.Subtract(activeUtcFileDate);
      if (dateDiff.TotalDays < 1.0)
        return false;
      activeUtcFileDate = currentUtcDate;
      OpenNewTraceFile(currentUtcDate);
      return true;
    }

    #region Overrides

    public override void Fail(string message) {
      CheckRollover();
      base.Fail(message);
    }

    public override void Fail(string message, string detailMessage) {
      CheckRollover();
      base.Fail(message, detailMessage);
    }

    public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data) {
      CheckRollover();
      base.TraceData(eventCache, source, eventType, id, data);
    }

    public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data) {
      CheckRollover();
      base.TraceData(eventCache, source, eventType, id, data);
    }

    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id) {
      CheckRollover();
      base.TraceEvent(eventCache, source, eventType, id);
    }

    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message) {
      CheckRollover();
      base.TraceEvent(eventCache, source, eventType, id, message);
    }

    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args) {
      CheckRollover();
      base.TraceEvent(eventCache, source, eventType, id, format, args);
    }

    public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId) {
      CheckRollover();
      base.TraceTransfer(eventCache, source, id, message, relatedActivityId);
    }

    public override void Write(object o) {
      CheckRollover();
      base.Write(o);
    }

    public override void Write(object o, string category) {
      CheckRollover();
      base.Write(o, category);
    }

    public override void Write(string message) {
      CheckRollover();
      base.Write(message);
    }

    public override void Write(string message, string category) {
      CheckRollover();
      base.Write(message, category);
    }

    public override void WriteLine(object o) {
      CheckRollover();
      base.WriteLine(o);
    }

    public override void WriteLine(object o, string category) {
      CheckRollover();
      base.WriteLine(o, category);
    }

    public override void WriteLine(string message) {
      CheckRollover();
      base.WriteLine(message);
    }

    public override void WriteLine(string message, string category) {
      CheckRollover();
      base.WriteLine(message, category);
    }

    #endregion
  }
}