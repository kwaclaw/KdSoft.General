#pragma warning disable CA1063 // Implement IDisposable Correctly

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.Utils
{
    /// <summary>
    /// Manages creation of file streams based on configured roll-over conditions.
    /// </summary>
    /// <remarks>
    /// Whenever a re-check of the roll-over conditions is desired, simply call <see cref="GetCurrentFileStream"/>,
    /// and the file stream returned will be the currently applicable one (the last one  or a new one).
    /// This evaluates the roll-over conditions and therefore has some overhead.
    /// </remarks>
    public sealed class RollingFileFactory: IDisposable, IAsyncDisposable
    {
        readonly DirectoryInfo _dirInfo;
        readonly Func<DateTimeOffset, string> _fileNameSelector;
        readonly string _fileExtension;
        readonly long _fileSizeLimitBytes;
        readonly int _maxFileCount;
        readonly bool _useLocalTime;
        readonly FileStream _lockFile;

        /// <summary>
        /// Number of files to delete in  one roll-over check operation.
        /// </summary>
        public const int MaxFilesToDelete = 5;

        /// <summary>
        /// Type of time (local or UTC) to use for filename formatting.
        /// </summary>
        public bool UseLocalTime => _useLocalTime;

        // used to enable file creation on startup (regardless of other checks)
        int _createNewFileOnStartup;
#pragma warning disable CA2213 // Disposable fields should be disposed
        FileStream? _stream;
#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dirInfo">Directory where files should be created.</param>
        /// <param name="fileNameSelector">Callback that returns a filename based on time and format string.
        ///   A changed of filename triggers a roll-over to a new file.</param>
        /// <param name="fileExtension">File extension to use for files.</param>
        /// <param name="useLocalTime">Type of time (local or UTC) to use for filename formatting.</param>
        /// <param name="fileSizeLimitKB">Maximum file size to trigger a roll-over to a new file.</param>
        /// <param name="maxFileCount">Maximum number of files to keep in that directory.</param>
        /// <param name="newFileOnStartup">Indicates if a new file should be created on startup, regardless of roll-over conditions.</param>
        public RollingFileFactory(
            DirectoryInfo dirInfo,
            Func<DateTimeOffset, string> fileNameSelector,
            string fileExtension,
            bool useLocalTime,
            int fileSizeLimitKB,
            int maxFileCount,
            bool newFileOnStartup
        ) {
            this._dirInfo = dirInfo;
            this._fileNameSelector = fileNameSelector;
            this._fileExtension = fileExtension;
            this._useLocalTime = useLocalTime;
            this._fileSizeLimitBytes = fileSizeLimitKB * 1024;
            this._maxFileCount = maxFileCount;
            this._createNewFileOnStartup = newFileOnStartup ? 1 : 0;

            var lockFilePath = Path.Combine(dirInfo.FullName, "fileFactory.lock");
            _lockFile = new FileStream(lockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete, 64, FileOptions.DeleteOnClose);
        }

        bool TimestampPatternChanged(FileStream stream, DateTimeOffset now) {
            var newFnBase = _fileNameSelector(now);
            var currentFn = Path.GetFileNameWithoutExtension(stream.Name);
            var compared = string.Compare(newFnBase, 0, currentFn, 0, newFnBase.Length, StringComparison.CurrentCultureIgnoreCase);
            return compared != 0;
        }

        bool TryGetSequenceNo(string fileName, out int value) {
            int lastDotIndex = fileName.LastIndexOf('.');
            int lastDashIndex = fileName.LastIndexOf('-', lastDotIndex);
            var sequenceNoSpan = fileName.AsSpan(lastDashIndex + 1, lastDotIndex - lastDashIndex);
            return int.TryParse(sequenceNoSpan, out value);
        }

        // NOT THREAD-SAFE
        FileStream CreateNextSuitableFileStream(DateTimeOffset now, bool alwaysCreateNewFile) {
            var newFnBase = _fileNameSelector(now);
            var newNameStart = $"{newFnBase}-";

            var enumOptions = new EnumerationOptions {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false,
            };
            var currentFiles = _dirInfo.GetFiles($"*-*{_fileExtension}", enumOptions);
            Array.Sort(currentFiles, (x, y) => DateTime.Compare(x.LastWriteTimeUtc, y.LastWriteTimeUtc));

            var matchingFiles = new SortedList<string, FileInfo>(StringComparer.CurrentCultureIgnoreCase);
            var maxDeleteCount = currentFiles.Length - _maxFileCount;
            if (maxDeleteCount > MaxFilesToDelete)
                maxDeleteCount = MaxFilesToDelete;
            var deleteCount = 0;

            for (int fileIndx = 0; fileIndx < currentFiles.Length; fileIndx++) {
                var file = currentFiles[fileIndx];
                // maintain file count
                if (deleteCount < maxDeleteCount) {
                    file.Delete();
                    deleteCount++;
                }
                // collect files that match our new name
                else if (file.Name.StartsWith(newNameStart))
                    matchingFiles.Add(file.Name, file);
            }

            int lastSequenceNo = 0;
            if (matchingFiles.Count > 0) {
                var lastFileName = matchingFiles.Values[matchingFiles.Count - 1].Name;
                if (!TryGetSequenceNo(lastFileName, out lastSequenceNo)) {
                    lastSequenceNo = 0;
                }
            }

            IOException? ex = null;
            for (int sequenceNo = lastSequenceNo; sequenceNo < 1000; sequenceNo++) {
                var seqNoStr = sequenceNo.ToString("D2");
                var newFn = $"{newFnBase}-{seqNoStr}{_fileExtension}";
                if (matchingFiles.TryGetValue(newFn, out var newFi) /* && newFi.Exists */) {
                    if (alwaysCreateNewFile || newFi.Length >= _fileSizeLimitBytes) {
                        continue;
                    }
                }
                try {
                    var fileName = Path.Combine(_dirInfo.FullName, newFn);
                    return new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
                }
                catch (IOException ioex) {
                    // make one attempt to ignore an IOException because the file may have been created in the meantime
                    if (ex == null) {
                        ex = ioex;
                        continue;
                    }
                    throw ex;
                }
            }
            throw new InvalidOperationException($"Too many files like {newFnBase} for same timestamp.");
        }

        // NOT THREAD-SAFE
        /// <summary>
        /// Checks rollover conditions and returns old or new file stream task as a result.
        /// </summary>
        /// <remarks>NOT THREAD-SAFE. Must not be called concurrently with itself!
        /// When a new file stream is created then the previous one is disposed.
        /// </remarks>
        public FileStream GetCurrentFileStream() {
            var currentStream = _stream;
            var now = _useLocalTime ? DateTimeOffset.Now : DateTimeOffset.UtcNow;
            bool createNewFile = false;
            if (currentStream != null) {
                createNewFile = currentStream.Length >= _fileSizeLimitBytes || TimestampPatternChanged(currentStream, now);
            }

            if (currentStream is null || createNewFile) {
                var oldCreateNewFile = Interlocked.Exchange(ref _createNewFileOnStartup, 0);
                var newStream = CreateNextSuitableFileStream(now, oldCreateNewFile == 1);
                var oldStream = Interlocked.Exchange(ref _stream, newStream);
                if (oldStream != null) {
                    oldStream.Flush();
                    oldStream.Dispose();
                }
                return newStream;
            }

            return currentStream;
        }

        /// <summary>
        /// The currently open file stream is flushed and disposed.
        /// </summary>
        public void Dispose() {
            var oldStream = Interlocked.Exchange(ref _stream, null);
            if (oldStream != null) {
                try {
                    oldStream.Flush();
                    oldStream.Dispose();
                }
                finally {
                    _lockFile.Close();
                }
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The currently open file stream is flushed and disposed.
        /// </summary>
        public async ValueTask DisposeAsync() {
            var oldStream = Interlocked.Exchange(ref _stream, null);
            if (oldStream != null) {
                try {
                    await oldStream.FlushAsync().ConfigureAwait(false);
                    await oldStream.DisposeAsync().ConfigureAwait(false);
                }
                finally {
                    _lockFile.Close();
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}

#endif