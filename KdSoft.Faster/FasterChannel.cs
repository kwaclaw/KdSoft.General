using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FASTER.core;

namespace KdSoft.Faster
{
  /// <summary>
  /// A persistent queue, similar to <see cref="Channel"/>.
  /// Uses <see cref="FasterLog"/> as the storage mechanism.
  /// </summary>
  public sealed class FasterChannel: IDisposable
  {
    readonly FasterLog _log;
    readonly IDevice _device;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="deviceLogPath">Path to storage file.</param>
    public FasterChannel(string deviceLogPath) {
      _device = Devices.CreateLogDevice(deviceLogPath);
      _log = new FasterLog(new FasterLogSettings { LogDevice = _device });
    }

    /// <inheritdoc cref="FasterLog.TryEnqueue(ReadOnlySpan{byte}, out long)"/>
    public bool TryWrite(ReadOnlyMemory<byte> item, out long logicalAddress) {
      return _log.TryEnqueue(item.Span, out logicalAddress);
    }

    /// <inheritdoc cref="FasterLog.TryEnqueue(ReadOnlySpan{byte}, out long)"/>
    public bool TryWrite(ReadOnlyMemory<byte> item) {
      return _log.TryEnqueue(item.Span, out var _);
    }

    /// <inheritdoc cref="FasterLog.EnqueueAsync(byte[], CancellationToken)"/>
    public ValueTask<long> WriteAsync(ReadOnlyMemory<byte> item, CancellationToken cancellationToken = default) {
      return _log.EnqueueAsync(item, cancellationToken);
    }

    /// <summary>
    /// Returns new instance of <see cref="FasterReader"/>
    /// </summary>
    public FasterReader GetNewReader() {
      return new FasterReader(_log);
    }

    /// <inheritdoc cref="FasterLog.Commit(bool)"/>
    public void Commit(bool spinWait = false) {
      _log.Commit(spinWait);
    }

    /// <inheritdoc cref="FasterLog.CommitAsync(CancellationToken)"/>
    public ValueTask CommitAsync(CancellationToken cancellationToken = default) {
      return _log.CommitAsync(cancellationToken);
    }

    /// <inheritdoc cref="IDisposable.Dispose()"/>
    public void Dispose() {
      _log.Dispose();
      _device.Dispose();
    }
  }
}
