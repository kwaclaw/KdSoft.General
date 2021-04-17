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
  public class FasterChannel: IDisposable
  {
    readonly FasterLog _log;
    readonly IDevice _device;
    long _logicalAddress;

    public FasterChannel(string deviceLogPath) {
      _device = Devices.CreateLogDevice(deviceLogPath);
      _log = new FasterLog(new FasterLogSettings { LogDevice = _device });
    }

    public bool TryWrite(ReadOnlyMemory<byte> item) {
      return _log.TryEnqueue(item.Span, out _logicalAddress);
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> item, CancellationToken cancellationToken = default) {
      _logicalAddress = await _log.EnqueueAsync(item, cancellationToken).ConfigureAwait(false);
    }

    public FasterReader GetNewReader() {
      return new FasterReader(_log);
    }

    public void Commit(bool spinWait = false) {
      _log.Commit(spinWait);
    }

    public ValueTask CommitAsync(CancellationToken cancellationToken = default) {
      return _log.CommitAsync(cancellationToken);
    }

    public void Dispose() {
      _log.Dispose();
      _device.Dispose();
    }
  }
}
