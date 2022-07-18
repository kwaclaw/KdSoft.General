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
  /// Wrapper for <see cref="FileSystemWatcher"/>. Accumulates changes and allows for changes to settle before they are reported.
  /// Reports all change types as well as the initial and last file name (if applicable).
  /// </summary>
  public sealed class FileChangeDetector: IDisposable
  {
    object syncObj = new object();

    FileSystemWatcher fsw;
    Dictionary<string, FileChangeAccumulator> activeFiles;
    ConcurrentQueue<FileChange> fileChangeQueue;

    CancellationTokenSource? cts;
    CancellationToken cancelToken = CancellationToken.None;
    Timer? timer;

    TimeSpan settleTime;
    /// <summary>
    /// Time span to allow for changes to settle before reporting them.
    /// </summary>
    public TimeSpan SettleTime {
      get { return settleTime; }
    }

    /// <summary>
    /// The directory to monitor, in standard or Universal Naming Convention (UNC) notation.
    /// </summary>
    public string BaseDirectory {
      get { return fsw.Path; }
    }

    /// <summary>
    /// Occurs when a file or directory in the specified <see cref="BaseDirectory"/> is created, deleted, renamed or changed.
    /// The initial and final file names are reported (if applicable). If deletion was the last event, then only the initial
    /// file name (OldName) is reported, while the final file name (Name) is null.
    /// All change types since the last event are indicated in the <see cref="FileSystemEventArgs.ChangeType"/> property,
    /// but no distinction is possible as to how often a specific change type occurred.
    /// </summary>
    public event RenamedEventHandler? FileChanged;

    ErrorEventHandler? errorEvent;
    /// <summary>
    /// Occurs when an error in the underlying <see cref="FileSystemWatcher"/> is reported, or when an internal error happens.
    /// </summary>
    public event ErrorEventHandler? ErrorEvent {
      add {
        errorEvent -= value;
        errorEvent += value;
      }
      remove {
        errorEvent -= value;
      }
    }

    class FileChangeAccumulator
    {
      public FileChangeAccumulator(WatcherChangeTypes changeTypes, string fullPath, string name) {
        this.ChangeTypes = changeTypes;
        this.FullPath = fullPath;
        this.Name = name;
      }

      public WatcherChangeTypes ChangeTypes { get; set; }
      public string? FullPath { get; set; }
      public string? Name { get; set; }
      public string? OldFullPath { get; set; }
      public string? OldName { get; set; }

      public FileChange? FileChange { get; set; }
    }

    class FileChange
    {
      public FileChange(FileChangeAccumulator changeAccumulator, DateTimeOffset changeTime) {
        this.ChangeAccumulator = changeAccumulator;
        this.ChangeTime = changeTime;
      }

      public bool IsLastChange {
        get { return object.ReferenceEquals(this, ChangeAccumulator.FileChange); }
      }

      public FileChangeAccumulator ChangeAccumulator { get; }

      public DateTimeOffset ChangeTime { get; }
    }

    FileChangeDetector(string baseDirectory, bool subDirectories, NotifyFilters notifyFilters, TimeSpan settleTime) {
      this.settleTime = settleTime;
      activeFiles = new Dictionary<string, FileChangeAccumulator>();
      fileChangeQueue = new ConcurrentQueue<FileChange>();

      fsw = new FileSystemWatcher(baseDirectory);
      fsw.EnableRaisingEvents = false;
      fsw.NotifyFilter = notifyFilters;
      fsw.IncludeSubdirectories = subDirectories;
      fsw.Changed += Fsw_Changed;
      fsw.Created += Fsw_Changed;
      fsw.Renamed += Fsw_Renamed;
      fsw.Deleted += Fsw_Deleted;
      fsw.Error += Fsw_Error;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="baseDirectory">The directory to monitor, in standard or Universal Naming Convention (UNC) notation.</param>
    /// <param name="filter">Filter string used to determine what files are monitored in the directory. Defaults to "*.*" when <c>null</c> is passed.</param>
    /// <param name="subDirectories"><c>true</c> if you want to monitor subdirectories; otherwise, <c>false</c>.</param>
    /// <param name="notifyFilters">Type of changes to watch for.</param>
    /// <param name="settleTime">Time span to allow for changes to settle before reporting them.</param>
    public FileChangeDetector(
      string baseDirectory,
      string? filter,
      bool subDirectories,
      NotifyFilters notifyFilters,
      TimeSpan settleTime
    ) : this(baseDirectory, subDirectories, notifyFilters, settleTime) {
      if (filter != null)
        fsw.Filter = filter;
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="baseDirectory">The directory to monitor, in standard or Universal Naming Convention (UNC) notation.</param>
    /// <param name="filters">Collection of all the filters used to determine what files are monitored.</param>
    /// <param name="subDirectories"><c>true</c> if you want to monitor subdirectories; otherwise, <c>false</c>.</param>
    /// <param name="notifyFilters">Type of changes to watch for.</param>
    /// <param name="settleTime">Time span to allow for changes to settle before reporting them.</param>
    public FileChangeDetector(
      string baseDirectory,
      IEnumerable<string>? filters,
      bool subDirectories,
      NotifyFilters notifyFilters,
      TimeSpan settleTime
    ) : this(baseDirectory, subDirectories, notifyFilters, settleTime) {
      if (filters != null) {
        foreach (var filter in filters) {
          fsw.Filters.Add(filter);
        }
      }
    }
#endif

    void Fsw_Renamed(object sender, RenamedEventArgs e) {
      lock (syncObj) {
        RegisterRenamedEvent(e);
      }
    }

    void Fsw_Error(object sender, ErrorEventArgs e) {
      errorEvent?.Invoke(sender, e);
    }

    void Fsw_Changed(object sender, FileSystemEventArgs e) {
      lock (syncObj) {
        RegisterChangedEvent(e);
      }
    }

    void Fsw_Deleted(object sender, FileSystemEventArgs e) {
      lock (syncObj) {
        RegisterDeletedEvent(e);
      }
    }

    void RegisterChangedEvent(FileSystemEventArgs eventArgs) {
      if (activeFiles.TryGetValue(eventArgs.FullPath, out var fcAccumulator)) {
        fcAccumulator.ChangeTypes |= eventArgs.ChangeType;
      }
      else {
        fcAccumulator = new FileChangeAccumulator(eventArgs.ChangeType, eventArgs.FullPath, eventArgs.Name!);
        activeFiles[eventArgs.FullPath] = fcAccumulator;
      }

      fcAccumulator.FileChange = new FileChange(fcAccumulator, DateTimeOffset.UtcNow);
      fileChangeQueue.Enqueue(fcAccumulator.FileChange);

      CheckTimerStarted();
    }

    void RegisterDeletedEvent(FileSystemEventArgs eventArgs) {
      if (!activeFiles.TryGetValue(eventArgs.FullPath, out var fcAccumulator)) {
        return;
      }

      fcAccumulator.ChangeTypes |= eventArgs.ChangeType;
      // handle multiple rename events
      if (fcAccumulator.OldFullPath == null) {
        fcAccumulator.OldFullPath = fcAccumulator.FullPath;
        fcAccumulator.OldName = fcAccumulator.Name;
      }
      fcAccumulator.FullPath = null;
      fcAccumulator.Name = null;

      fcAccumulator.FileChange = new FileChange(fcAccumulator, DateTimeOffset.UtcNow);
      fileChangeQueue.Enqueue(fcAccumulator.FileChange);

      CheckTimerStarted();
    }

    void RegisterRenamedEvent(RenamedEventArgs eventArgs) {
      if (activeFiles.TryGetValue(eventArgs.OldFullPath, out var fcAccumulator)) {
        activeFiles.Remove(eventArgs.OldFullPath);
        activeFiles[eventArgs.FullPath] = fcAccumulator;

        fcAccumulator.ChangeTypes |= eventArgs.ChangeType;
        // handle multiple rename events
        if (fcAccumulator.OldFullPath == null) {
          fcAccumulator.OldFullPath = eventArgs.OldFullPath;
          fcAccumulator.OldName = eventArgs.OldName;
        }
        fcAccumulator.FullPath = eventArgs.FullPath;
        fcAccumulator.Name = eventArgs.Name;
      }
      else {
        fcAccumulator = new FileChangeAccumulator(eventArgs.ChangeType, eventArgs.FullPath, eventArgs.Name!);
        activeFiles[eventArgs.FullPath] = fcAccumulator;
      }

      fcAccumulator.FileChange = new FileChange(fcAccumulator, DateTimeOffset.UtcNow);
      fileChangeQueue.Enqueue(fcAccumulator.FileChange);

      CheckTimerStarted();
    }

    void RegisterInitialEvents(IEnumerable<FileSystemEventArgs> eventArgsList) {
      var now = DateTimeOffset.UtcNow;
      lock (syncObj) {
        foreach (var eventArgs in eventArgsList) {
          var fcAccumulator = new FileChangeAccumulator(eventArgs.ChangeType, eventArgs.FullPath, eventArgs.Name!);
          activeFiles[eventArgs.FullPath] = fcAccumulator;
          fcAccumulator.FileChange = new FileChange(fcAccumulator, now);
          fileChangeQueue.Enqueue(fcAccumulator.FileChange);
        }

        CheckTimerStarted();
      }
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

          string? filePath;
          FileChangeAccumulator fcAccumulator;
          lock (syncObj) {
            // if no other file changes are active then we process this one
            if (!fileChange.IsLastChange)
              continue;
            fcAccumulator = fileChange.ChangeAccumulator;
            // when deletion was the last event, then Name and FullPath are null
            filePath = fcAccumulator.FullPath ?? fcAccumulator.OldFullPath;
            if (filePath != null) {
              activeFiles.Remove(filePath!);
              if (activeFiles.Count == 0)
                CloseTimer();
            }
          }

          try {
            var baseDir = Path.GetDirectoryName(filePath);
            var eventArgs = new RenamedEventArgs(fcAccumulator.ChangeTypes, baseDir!, fcAccumulator.Name, fcAccumulator.OldName);
            FileChanged?.Invoke(this, eventArgs);
          }
          catch (Exception ex) {
            errorEvent?.Invoke(this, new ErrorEventArgs(ex));
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

    /// <summary>
    /// Turns on file change monitoring.
    /// </summary>
    /// <param name="detectExisting">If <c>true</c>, then existing files will be reported as newly created files./</param>
    /// <returns></returns>
    public bool Start(bool detectExisting) {
      // check existing files beforehand so that we don't have a long time gap between 
      // starting the file system watcher and reporting the existing files
      var existingFileEvents = new List<FileSystemEventArgs>();
      if (detectExisting) {
        var option = fsw.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var dirInfo = new DirectoryInfo(fsw.Path);
#if NET6_0_OR_GREATER
        foreach (var filter in fsw.Filters) {
          existingFileEvents.AddRange(
            dirInfo.EnumerateFiles(filter, option).Select(fi => new FileSystemEventArgs(WatcherChangeTypes.Created, fi.DirectoryName!, fi.Name))
          );
        }
#else
        existingFileEvents.AddRange(
          dirInfo.EnumerateFiles(fsw.Filter, option).Select(fi => new FileSystemEventArgs(WatcherChangeTypes.Created, fi.DirectoryName!, fi.Name))
        );
#endif
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
      RegisterInitialEvents(existingFileEvents);

      return true;
    }

#if !NETSTANDARD1_3
    static Task Delay(TimeSpan timeout) {
      var tcs = new TaskCompletionSource<object?>();
      new Timer(_ => tcs.SetResult(null), null, (int)timeout.TotalMilliseconds, -1);
      return tcs.Task;
    }
#endif


    /// <summary>
    /// Stops file change monitoring after a given time span.
    /// </summary>
    /// <param name="timeout">Time span after which to stop.</param>
    public void Stop(TimeSpan timeout) {
      lock (syncObj) {
        if (cts == null)
          return;
#if !NETSTANDARD1_3
        var delayedCts = cts;
        Delay(timeout).ContinueWith(_ => {
          delayedCts.Cancel();
          delayedCts.Dispose();
        });
#else
        cts.CancelAfter(timeout);
        cts.Dispose();
#endif
        cts = null;
        fsw.EnableRaisingEvents = false;
      }
    }

    /// <summary>
    /// Stops file change monitoring immediately.
    /// </summary>
    public void Stop() {
      lock (syncObj) {
        if (cts == null)
          return;
        cts.Cancel(true);
        cts.Dispose();
        cts = null;

        fsw.EnableRaisingEvents = false;
        activeFiles.Clear();
        CloseTimer();
      }
    }

    /// <inheritdoc cref="IDisposable"/>
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
