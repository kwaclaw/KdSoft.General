using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FASTER.core;

namespace KdSoft.Faster
{
  /// <summary>
  /// Helper routines to iterate over a <see cref="FasterLog"/> in a channel-like way.
  /// </summary>
  public class FasterReader: IDisposable
  {
    readonly FasterLog _log;
    readonly FasterLogScanIterator _iter;
    readonly MemoryPool<byte> _pool;

    long _nextAddress;

    public FasterReader(FasterLog log) {
      this._log = log;
      _nextAddress = log.BeginAddress;
      _iter = log.Scan(0, long.MaxValue, "channel");
      _pool = MemoryPool<byte>.Shared;
    }

    public bool TryRead([MaybeNullWhen(false)] out IMemoryOwner<byte> item, out int entryLength) {
      if (_iter.GetNext(_pool, out item, out entryLength, out var currentAddress, out _nextAddress)) {
        return true;
      }
      item = null;
      return false;
    }

    public async ValueTask<(IMemoryOwner<byte>, int)> ReadAsync(CancellationToken cancellationToken = default) {
      while (true) {
        if (!await WaitToReadAsync(cancellationToken).ConfigureAwait(false))
          throw new ChannelClosedException();

        if (TryRead(out IMemoryOwner<byte>? item, out int entryLength))
          return (item, entryLength);
      }
    }

    // this approach completes the iteration wheren there is no more data to read and does not wait for the next commit
    public async IAsyncEnumerable<(IMemoryOwner<byte>, int)> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default) {
      await foreach ((IMemoryOwner<byte> entry, var entryLength, var currentAddress, var nextAddress) in _iter.GetAsyncEnumerable(_pool, cancellationToken)) {
        _nextAddress = nextAddress;
        yield return (entry, entryLength);

        // MoveNextAsync() would hang at TailAddress, waiting for more entries (that we don't add).
        // Note: If this happens and the test has to be canceled, there may be a leftover blob from the log.Commit(), because
        // the log device isn't Dispose()d; the symptom is currently a numeric string format error in DefaultCheckpointNamingScheme.
        if (nextAddress == _log.TailAddress)
          break;
      }
    }

    // this approach will wait for the next commit
    public IAsyncEnumerable<(IMemoryOwner<byte> entry, int entryLength, long currentAddress, long nextAddress)> GetAsyncEnumerable(CancellationToken token = default) {
      return _iter.GetAsyncEnumerable(_pool, token);
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default) {
      return _iter.WaitAsync(cancellationToken);
    }

    public void Truncate() {
      _log.TruncateUntil(_nextAddress);
    }

    public void Dispose() {
      _iter.Dispose();
      _pool.Dispose();
    }
  }
}
