using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
#if !NETSTANDARD1_3
using System.Threading.Tasks;
#endif

namespace KdSoft.Utils
{
  /// <summary>
  /// Wrapper for FileSystemWatcher. Accumulates changes and allows for changes to settle before they are reported.
  /// Reports only the last change of a set of changes that are observed together.
  /// </summary>
  public class FileChangeDetector: IDisposable
  {
    object syncObj = new object();

    FileSystemWatcher fsw;
    Dictionary<string, FileChangeTracker> activeFiles;
    ConcurrentQueue<FileChange> fileChangeQueue;

    CancellationTokenSource? cts;
    CancellationToken cancelToken = CancellationToken.None;
    Timer? timer;

    TimeSpan settleTime;
    public TimeSpan SettleTime {
      get { return settleTime; }
    }

    public string BaseDirectory {
      get { return fsw.Path; }
    }

    public event FileSystemEventHandler? FileChanged;

    public event ErrorEventHandler? Error {
      add { fsw.Error += value; }
      remove { fsw.Error -= value; }
    }

    void CallHandler(FileSystemEventArgs eventArgs) {
      var fcEvent = FileChanged;
      if (fcEvent == null)
        return;
      fcEvent(this, eventArgs);
    }

    class FileChange
    {
      public FileChange(FileSystemEventArgs eventArgs, DateTimeOffset changeTime, FileChangeTracker fcEvent) {
        this.EventArgs = eventArgs;
        this.ChangeTime = changeTime;
        this.Tracker = fcEvent;
      }

      public FileSystemEventArgs EventArgs { get; private set; }
      public DateTimeOffset ChangeTime { get; private set; }
      public FileChangeTracker Tracker { get; set; }

      public bool IsLastChange {
        get { return Object.ReferenceEquals(this, Tracker.LastChange); }
      }
    }

    class FileChangeTracker
    {
      public FileChangeTracker(string filePath) {
        this.FilePath = filePath;
      }

      public string FilePath { get; private set; }
      public FileChange? LastChange { get; set; }
    }

    public FileChangeDetector(string baseDirectory, string filter, bool subDirectories, NotifyFilters notifyFilters, TimeSpan settleTime) {
      this.settleTime = settleTime;
      activeFiles = new Dictionary<string, FileChangeTracker>();
      fileChangeQueue = new ConcurrentQueue<FileChange>();

      fsw = new FileSystemWatcher(baseDirectory);
      fsw.EnableRaisingEvents = false;
      fsw.NotifyFilter = notifyFilters;
      fsw.IncludeSubdirectories = subDirectories;
      fsw.Filter = filter;
      fsw.Changed += fsw_Changed;
      fsw.Created += fsw_Changed;
      fsw.Renamed += fsw_Renamed;
      fsw.Error += fsw_Error;
    }

    void fsw_Renamed(object sender, RenamedEventArgs e) {
      RegisterFileEvent(e);
    }

    void fsw_Error(object sender, ErrorEventArgs e) {
      //TODO log error
    }

    void fsw_Changed(object sender, FileSystemEventArgs e) {
      RegisterFileEvent(e);
    }

    void RegisterFileEvent(FileSystemEventArgs eventArgs) {
      FileChange fcChange;
      lock (syncObj) {
        if (!activeFiles.TryGetValue(eventArgs.FullPath, out var fcEvent)) {
          fcEvent = new FileChangeTracker(eventArgs.FullPath);
          activeFiles[eventArgs.FullPath] = fcEvent;
        }
        fcEvent.LastChange = fcChange = new FileChange(eventArgs, DateTimeOffset.UtcNow, fcEvent);
        CheckTimerStarted();
      }
      fileChangeQueue.Enqueue(fcChange);
    }

    void RegisterFileEvents(IEnumerable<FileSystemEventArgs> eventArgsList) {
      var fcChangeList = new List<FileChange>();
      lock (syncObj) {
        foreach (var eventArgs in eventArgsList) {
          if (!activeFiles.TryGetValue(eventArgs.FullPath, out var fcEvent)) {
            fcEvent = new FileChangeTracker(eventArgs.FullPath);
            activeFiles[eventArgs.FullPath] = fcEvent;
          }
          var fcChange = new FileChange(eventArgs, DateTimeOffset.UtcNow, fcEvent);
          fcEvent.LastChange = fcChange;
          fcChangeList.Add(fcChange);
        }
        CheckTimerStarted();
      }
      foreach (var fcChange in fcChangeList)
        fileChangeQueue.Enqueue(fcChange);
    }

    // must run under sync lock
    void CheckTimerStarted() {
      if (timer == null) {
        var period = TimeSpan.FromTicks(SettleTime.Ticks / 2);
        timer = new Timer(TimerCallback, this, TimeSpan.Zero, period);
      }
    }

    // must run under sync lock
    void CloseTimer() {
      var tmr = timer;
      if (tmr != null) {
        timer = null;
        tmr.Dispose();
      }
    }

    void TimerCallback(object? state) {
      try {
        while (fileChangeQueue.TryPeek(out var fileChange)) {
          var timeDiff = DateTimeOffset.UtcNow - fileChange.ChangeTime;
          if (timeDiff < SettleTime)
            return;

          if (cancelToken.IsCancellationRequested)
            return;

          if (!fileChangeQueue.TryDequeue(out fileChange))
            return;

          string filePath;
          lock (syncObj) {
            // if no other file changes are active then we process this one
            if (!fileChange.IsLastChange)
              continue;
            filePath = fileChange.Tracker.FilePath;
            activeFiles.Remove(filePath);
            if (activeFiles.Count == 0)
              CloseTimer();
          }

          try {
            CallHandler(fileChange.EventArgs);
          }
          catch (Exception) {
            //TODO log exception?
          }
        }
      }
      catch (Exception) {
        //
      }
    }

    public bool IsStopped {
      get { lock (syncObj) return cts == null; }
    }

    public bool Start(bool detectExisting) {
      // check existing files beforehand so that we don't have a long time gap between 
      // starting the file system watcher and reporting the existing files
      IEnumerable<FileSystemEventArgs> existingFileEvents = new FileSystemEventArgs[0];
      if (detectExisting) {
        var option = fsw.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var dirInfo = new DirectoryInfo(fsw.Path);
        existingFileEvents = dirInfo.EnumerateFiles(fsw.Filter, option)
            .Select(fi => new FileSystemEventArgs(WatcherChangeTypes.Created, fi.DirectoryName!, fi.Name));
      }

      lock (syncObj) {
        // wait if Stop(timeout) was called with a cancellation timeout
        if (cts != null || (cancelToken.CanBeCanceled && !cancelToken.IsCancellationRequested))
          return false;
        cts = new CancellationTokenSource();
        cancelToken = cts.Token;
        fsw.EnableRaisingEvents = true;
      }

      // now quickly register the existing files as "created" file events
      RegisterFileEvents(existingFileEvents);

      return true;
    }

#if !NETSTANDARD1_3
    static Task Delay(TimeSpan timeout) {
      var tcs = new TaskCompletionSource<object?>();
      new Timer(_ => tcs.SetResult(null), null, (int)timeout.TotalMilliseconds, -1);
      return tcs.Task;
    }
#endif

    public void Stop(TimeSpan timeout) {
      lock (syncObj) {
        if (cts == null)
          return;
#if !NETSTANDARD1_3
        var delayedCts = cts;
        Delay(timeout).ContinueWith(_ => delayedCts.Cancel());
#else
        cts.CancelAfter(timeout);
#endif
        cts = null;
        fsw.EnableRaisingEvents = false;
      }
    }

    public void Stop() {
      lock (syncObj) {
        if (cts == null)
          return;
        cts.Cancel(true);
        cts = null;

        fsw.EnableRaisingEvents = false;
        activeFiles.Clear();
        CloseTimer();
      }
    }

    public void Dispose() {
      try {
        Stop();
      }
      catch (Exception) { }

      var locFsw = fsw;
      if (locFsw != null) {
        locFsw.Dispose();
      }
      GC.SuppressFinalize(this);
    }
  }
}
