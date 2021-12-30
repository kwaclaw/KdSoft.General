using System.Collections.Concurrent;

namespace KdSoft.Utils
{
  // thread-safe
  public class BufferPool
  {
    ConcurrentBag<byte[]> pool;

    public BufferPool() {
      pool = new ConcurrentBag<byte[]>();
    }

    public byte[] Acquire(int minSize) {
      return Acquire(minSize, 2 * minSize);
    }

    // we discard non-matching buffers, keeping the pool's buffers close to the desired buffer sizes;
    // this works well when consecutive requests require similar buffer sizes
    public byte[] Acquire(int minSize, int maxSize) {
      byte[]? buffer;
      if (!pool.TryTake(out buffer) || (buffer.Length < minSize || buffer.Length > maxSize)) {
        buffer = new byte[minSize];
      }
      return buffer;
    }

    public byte[] AcquireExact(int size) {
      byte[]? buffer;
      if (!pool.TryTake(out buffer) || (buffer.Length != size)) {
        buffer = new byte[size];
      }
      return buffer;
    }

    public void Return(byte[] buffer) {
      pool.Add(buffer);
    }

    public int Count {
      get { return pool.Count; }
    }
  }
}
