﻿using Microsoft.Extensions.Logging;

namespace KdSoft.Logging
{
  public class RollingFileLoggerOptions
  {
    public RollingFileLoggerOptions() { }

    /// <summary>
    /// Path to file directory. Can be relative or absolute.
    /// </summary>
    public string Directory { get; set; } = ".";

    /// <summary>
    /// .NET format string to generate file name from timestamp. When the file name would change
    /// based on the current timestamp, then a new file will be created with the updated file name.
    /// </summary>
    /// <remarks>The actual file name will have a sequnce number inserted before the extension.</remarks>
    public string FileNameFormat { get; set; } = "app-{0:yyyy-MM-dd}";

    /// <summary>
    /// File extension, must start with '.'.
    /// </summary>
    public string FileExtension { get; set; } = ".log";

    /// <summary>
    /// Use local time instead of UTC time.
    /// </summary>
    public bool UseLocalTime { get; set; } = true;

    /// <summary>
    /// The file size limit determines at which size a file should be closed and a new file should be created.
    /// When the file name would stay the same (based on <see cref="FileNameFormat"/>) then the sequence number
    /// usded in the file name format will be incremented.
    /// </summary>
    public int FileSizeLimitKB { get; set; } = 4096;

    /// <summary>
    /// Maximum number of files that should be kept. Oldest files will be purged first.
    /// </summary>
    public int MaxFileCount { get; set; } = 10;

    /// <summary>
    /// Determines if a new file should be created on event sink startup, even if an existing file
    /// could be continued based on the timestamp and its file size.
    /// </summary>
    public bool NewFileOnStartup { get; set; }

    /// <summary>
    /// Log events that happen frequently get written/flushed in batches. Indicates size of batches.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// When events get get written less often, it may take a while to fill a batch.
    /// This setting indiactes how long to wait in milliseconds before writing/flusing the incomplete batch.
    /// </summary>
    public int MaxWriteDelayMSecs { get; set; } = 400;

    /// <summary>
    /// Enables log scopes. See <see cref="ILogger.BeginScope"/>.
    /// </summary>
    public bool IncludeScopes { get; set; }
  }
}
