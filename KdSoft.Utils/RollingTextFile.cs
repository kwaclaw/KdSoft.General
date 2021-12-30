using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.Utils
{
  /// <summary>
  /// Class that writes to text file and rolls it when necessary.
  /// </summary>
  public class RollingTextFile: IDisposable
  {
    static readonly Regex NumberRegex = new Regex("(\\d)+$", RegexOptions.Compiled);

    readonly Func<DateTime, string> timestampPattern;
    readonly Func<DateTime, int, string> fileNameSelector;
    readonly Encoding encoding;
    readonly bool autoFlush;
    readonly long rollSizeInBytes;
    readonly Action<string, Exception> errorCallback;

    object syncObj = new object();
    string? currentTimestampPattern;
    Task<AsyncTextFileWriter>? asyncWriterTask;
    int createNewFileOnRollOver;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileNameSelector">Selector of output file name. DateTime is date of file open time, int is sequence number.</param>
    /// <param name="timestampPattern">Pattern of rolling identifier. DateTime is write time of message. If pattern is different roll new file.</param>
    /// <param name="rollSizeKB">When this size is exceeded, start next file.</param>
    /// <param name="encoding">String encoding.</param>
    /// <param name="autoFlush">If true, call Flush on every write.</param>
    /// <param name="newFileOnStart">If true, a new log file is created on startup, even if the rollover conditions are not met.</param>
    /// <param name="errorCallback">Gets called when file I/O errors occur.</param>
    public RollingTextFile(
        Func<DateTime, int, string> fileNameSelector,
        Func<DateTime, string> timestampPattern,
        int rollSizeKB,
        Encoding encoding,
        bool autoFlush,
        bool newFileOnStart,
        Action<string, Exception> errorCallback
    ) {
      this.timestampPattern = timestampPattern ?? (dt => string.Empty);
      this.fileNameSelector = fileNameSelector;
      this.rollSizeInBytes = rollSizeKB * 1024;
      this.encoding = encoding;
      this.autoFlush = autoFlush;
      this.errorCallback = errorCallback;
      this.createNewFileOnRollOver = newFileOnStart ? 1 : 0;

      ValidateFileNameSelector(nameof(fileNameSelector));
    }

    const string seqNoFormatError = "FileNameSelector returned invalid format, must have sequence number (integer) last.";
    const string seqNoSequentialError = "FileNameSelector returned invalid format, sequence number is not incremented.";
    const string sameFileNameError = "FileNameSelector returned same file name.";

    void ValidateFileNameSelector(string argName) {
      var now = DateTime.Now;
      var fileName1 = Path.GetFileNameWithoutExtension(fileNameSelector(now, 0));
      var fileName2 = Path.GetFileNameWithoutExtension(fileNameSelector(now, 1));


      if (!NumberRegex.IsMatch(fileName1) || !NumberRegex.IsMatch(fileName2)) {
        throw new ArgumentException(seqNoFormatError, argName);
      }

      var seqStr1 = NumberRegex.Match(fileName1).Groups[0].Value;
      var seqStr2 = NumberRegex.Match(fileName2).Groups[0].Value;

      int seq1;
      int seq2;
      if (!int.TryParse(seqStr1, out seq1) || !int.TryParse(seqStr2, out seq2)) {
        throw new ArgumentException(seqNoFormatError, argName);
      }

      if (seq1 == seq2) {
        throw new ArgumentException(seqNoSequentialError, argName);
      }
    }

    async Task<AsyncTextFileWriter> CreateNewFileWriter(AsyncTextFileWriter? oldWriter, DateTime now, bool createNewFile) {
      int sequenceNo = 0;
      if (oldWriter != null) {
        sequenceNo = ExtractCurrentSequence(oldWriter.FileName) + 1;
      }

      string? fn = null;
      while (true) {
        var newFn = fileNameSelector(now, sequenceNo);
        if (fn == newFn) {
          throw new InvalidOperationException(sameFileNameError);
        }

        fn = newFn;
        var fi = new FileInfo(fn);
        if (fi.Exists) {
          if (createNewFile || fi.Length >= rollSizeInBytes) {
            sequenceNo++;
            continue;
          }
        }

        break;
      }

      if (oldWriter != null)
        await oldWriter.CloseAsync(true).ConfigureAwait(false);
      return new AsyncTextFileWriter(fn, encoding, autoFlush);
    }

    /// <summary>
    /// Checks file rollover conditions and if necessary, rolls the file to the next file name, closing the current file.
    /// </summary>
    protected Task<AsyncTextFileWriter> CheckRollover() {
      lock (syncObj) {
        var now = DateTime.Now;
        string ts = timestampPattern(now);
        var createWriterTask = asyncWriterTask;

        AsyncTextFileWriter currentWriter = null;
        if (createWriterTask != null) {
          currentWriter = createWriterTask.Result;
          bool createNewFile = currentWriter.CurrentStreamLength >= rollSizeInBytes || ts != currentTimestampPattern;
          if (!createNewFile)
            return createWriterTask;
        }

        var oldCreateNewFileOnRollOver = Interlocked.Exchange(ref createNewFileOnRollOver, 0);
        createWriterTask = CreateNewFileWriter(currentWriter, now, oldCreateNewFileOnRollOver == 1);
        currentTimestampPattern = ts;
        asyncWriterTask = createWriterTask;
        return createWriterTask;
      }
    }

    static int ExtractCurrentSequence(string fileName) {
      int extensionDotIndex = fileName.LastIndexOf('.');

      fileName = Path.GetFileNameWithoutExtension(fileName);

      var sequenceString = NumberRegex.Match(fileName).Groups[0].Value;
      int seq;
      if (int.TryParse(sequenceString, out seq)) {
        return seq;
      }
      else {
        return 0;
      }
    }

    /// <summary>
    /// Write text to file. Roll file name if necessary.
    /// </summary>
    /// <param name="text">Text to write.</param>
    public async Task WriteAsync(string text) {
      var asyncWriter = await CheckRollover().ConfigureAwait(false);
      if (asyncWriter.IsDisposed)
        throw new ObjectDisposedException(nameof(RollingTextFile));

      await asyncWriter.WriteAsync(text).ConfigureAwait(false);
    }

    /// <summary>
    /// Write plain bytes bytes to file, ignoring configured encoding.
    /// Can be used to write UTF-8 encoded text directly.
    /// Rolls file name if necessary.
    /// </summary>
    /// <param name="data">Bytes to write.</param>
    public async Task WriteAsync(ArraySegment<byte> data) {
      var asyncWriter = await CheckRollover().ConfigureAwait(false);
      if (asyncWriter.IsDisposed)
        throw new ObjectDisposedException(nameof(RollingTextFile));

      await asyncWriter.WriteAsync(data).ConfigureAwait(false);
    }

    /// <summary>Flush and close file.</summary>
    public async Task CloseAsync() {
      var asyncWriter = await CheckRollover().ConfigureAwait(false);
      await asyncWriter.CloseAsync(true);
    }

    /// <inheritdoc/>
    public void Dispose() {
      var asyncWriter = CheckRollover().Result;
      asyncWriter.CloseAsync(false).Wait();
    }

    /// <summary>
    /// Implements encoded writing of text.
    /// </summary>
    protected class AsyncTextFileWriter
    {
      readonly FileStream fileStream;
      readonly Encoding encoding;
      readonly bool autoFlush;

      int disposedCount = 0;
      readonly CancellationTokenSource cts = new CancellationTokenSource();

      /// <summary>Returns if instance has been disposed.</summary>
      public bool IsDisposed => disposedCount > 0;

      /// <summary>Expanded file name.</summary>
      public string FileName { get; }

      /// <summary>Current length of file stream.</summary>
      public long CurrentStreamLength { get; private set; }

      /// <summary>
      /// Constructor.
      /// </summary>
      /// <param name="fileName">Name of file.</param>
      /// <param name="encoding">Encoding to use.</param>
      /// <param name="autoFlush"><c>true</c> if flushing after every write is required.</param>
      public AsyncTextFileWriter(
          string fileName,
          Encoding encoding,
          bool autoFlush
      ) {
        this.FileName = fileName;
        // NOTE: it seems when using "useAsync: true" we get random spurious stretches of null bytes in the output, why??
        this.fileStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, useAsync: false);
        this.encoding = encoding;
        this.autoFlush = autoFlush;

        this.CurrentStreamLength = fileStream.Length;
      }

      /// <summary>
      /// Writes text to stream with configured encoding.
      /// </summary>
      /// <param name="text">Text to write.</param>
      public async Task WriteAsync(string text) {
        var bufPool = ArrayPool<byte>.Shared;
        int bufLen = encoding.GetMaxByteCount(text.Length);
        var buffer = bufPool.Rent(bufLen);
        try {
          var byteCount = encoding.GetBytes(text, 0, text.Length, buffer, 0);
          CurrentStreamLength += byteCount;
          await fileStream.WriteAsync(buffer, 0, byteCount, cts.Token);
          if (autoFlush) {
            await fileStream.FlushAsync(cts.Token);
          }
        }
        finally {
          bufPool.Return(buffer);
        }
      }

      /// <summary>
      /// Writes plain bytes to stream, ignoring the configured encoding.
      /// </summary>
      /// <param name="data">Bytes to write.</param>
      public async Task WriteAsync(ArraySegment<byte> data) {
        CurrentStreamLength += data.Count;
        if (data.Array != null)
          await fileStream.WriteAsync(data.Array, data.Offset, data.Count, cts.Token);
        if (autoFlush) {
          await fileStream.FlushAsync(cts.Token);
        }
      }

      /// <summary>
      /// Closes stream after flushing it (optional).
      /// </summary>
      /// <param name="wait">Flushes stream and waits for flush to complete before closing the stream.</param>
      public async Task CloseAsync(bool wait = true) {
        if (Interlocked.Increment(ref disposedCount) == 1) {
          if (wait) {
            await fileStream.FlushAsync();
          }
          else {
            cts.Cancel();
          }

          fileStream.Dispose();
        }
      }
    }
  }
}