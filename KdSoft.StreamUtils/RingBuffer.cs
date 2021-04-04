using System;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// Circular generic buffer with fixed capacity. Not thread-safe.
  /// </summary>
  public class RingBuffer<T>
  {
    T[] buffer;
    // tail = head means: either full or empty depending on hasData flag
    int head;     // will be advanced when writing/adding - one byte past the last byte written, position of first byte in gap
    int tail;     // will be advanced when reading/removing - one byte past the last byte read, position of first byte written
    bool hasDataToTake;    // indicates that there are data written but not taken, helps to decide the meaning of "head == tail"
    bool capacityReached;  // indicates that write capacity has been reached and from now on data will be overwritten

    public RingBuffer(int capacity) {
      buffer = new T[capacity];
      Clear();
    }

    public int Capacity {
      get { return buffer.Length; }
    }

    /// <summary>
    /// Amount of data written/added but not read/taken.
    /// </summary>
    public int Count {
      get {
        if (!hasDataToTake)
          return 0;
        int distance = head - tail;
        return distance > 0 ? distance : buffer.Length + distance;
      }
    }

    /// <summary>
    /// Amount of space available to write/add into.
    /// </summary>
    public int AvailableToWrite {
      get {
        if (!hasDataToTake)
          return buffer.Length;
        int distance = tail - head;
        return distance > 0 ? distance : buffer.Length + distance;
      }
    }

    /// <summary>
    /// Amount of space available to read into.
    /// </summary>
    public int AvailableToRead {
      get {
        if (capacityReached)
          return buffer.Length;
        else
          return head;
      }
    }

    public void Clear() {
      head = 0;
      tail = 0;
      hasDataToTake = false;
      capacityReached = false;
    }

    /// <summary>
    /// Writes/appends data to the fixed size ring buffer.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <param name="offset">The offset in the data array to start reading from.</param>
    /// <param name="count">The number of items to read from the data buffer and write to the ring buffer.</param>
    /// <returns>The number of items written. Can be less than requested (count) if the ring buffer does not have enough space.</returns>
    /// <remarks>If the ring buffer has no space this method returns 0 and does not throw an exception.</remarks>
    public int Add(T[] data, int offset, int count) {
      if (offset < 0)
        throw new ArgumentException("Write offset must be >= 0.", "offset");
      if (count < 0)
        throw new ArgumentException("Write count must be >= 0.", "count");
      // writes data starting at head until data is exhausted or tail is encountered
      int result = count;
      int distance = tail - head;
      if (distance >= count) {  // tail is ahead of head, gap >= count
        Buffer.BlockCopy(data, offset, buffer, head, count);
        head += count;
      }
      else if (distance > 0) {  // tail is ahead of head, gap < count
        Buffer.BlockCopy(data, offset, buffer, head, distance);
        head = tail;
        result = distance;
      }
      else if (distance == 0 && hasDataToTake) { // we have a full buffer available for taking, but no space to add
        result = 0;
      }
      else {  // distance <= 0, head is ahead of tail or equal, buffer has space for adding
        int headGap = buffer.Length - head;  // always > 0 
        int restCount = count - headGap;
        if (restCount <= 0) {  // head gap >= count
          Buffer.BlockCopy(data, offset, buffer, head, count);
          head += count;
          if (head == buffer.Length) {
            head = 0;
            capacityReached = true;
          }
        }
        else {  // head gap < count
          Buffer.BlockCopy(data, offset, buffer, head, headGap);
          if (restCount < tail) {  // gap before tail > rest of count
            Buffer.BlockCopy(data, offset + headGap, buffer, 0, restCount);
            head = restCount;
          }
          else {  // gap before tail <= rest of count, we copy everything up to tail
            Buffer.BlockCopy(data, offset + headGap, buffer, 0, tail);
            head = tail;
            result = headGap + tail;
          }
          capacityReached = true;
        }
      }
      if (result > 0)
        hasDataToTake = true;
      return result;
    }

    public int Add(T[] data) {
      return Add(data, 0, data.Length);
    }

    public int Add(T[] data, int count) {
      return Add(data, 0, count);
    }

    /// <summary>
    /// Reads and removes a specified amount of data from the ring buffer.
    /// </summary>
    /// <param name="data">The buffer to receive the data.</param>
    /// <param name="offset">The offset in the data buffer to start writing at.</param>
    /// <param name="count">The number of items to read from the ring buffer and write to the data buffer.</param>
    /// <returns>The number of items read. Can be less than the requested count if the ring buffer does not contain enough data.</returns>
    /// <remarks>If the ring buffer is empty this method returns 0 and does not throw an exception.</remarks>
    public int Take(T[] data, int offset, int count) {
      if (offset < 0)
        throw new ArgumentException("Read offset must be >= 0.", "offset");
      if (count < 0)
        throw new ArgumentException("Read count must be >= 0.", "count");
      if (!hasDataToTake)
        return 0;
      // reads data starting at tail until data is full or head is encountered
      int result = count;
      int distance = head - tail;
      if (distance >= count) {  // head is ahead of tail, count <= available data
        Buffer.BlockCopy(buffer, tail, data, offset, count);
        tail += count;
      }
      else if (distance > 0) {  // head is ahead of tail, count > available data
        Buffer.BlockCopy(buffer, tail, data, offset, distance);
        tail = head;
        result = distance;
      }
      else {  // tail is ahead of head or buffer is full
        int tailSize = buffer.Length - tail;
        int restCount = count - tailSize;
        if (restCount <= 0) {  // count <= available data following tail
          Buffer.BlockCopy(buffer, tail, data, offset, count);
          tail += count;
          if (tail == buffer.Length)
            tail = 0;
        }
        else {  // count > available data following tail
          Buffer.BlockCopy(buffer, tail, data, offset, tailSize);
          if (restCount <= head) {  // rest of count <= available data before head
            Buffer.BlockCopy(buffer, 0, data, offset + tailSize, restCount);
            tail = restCount;
          }
          else {  // rest of count > available data before head
            Buffer.BlockCopy(buffer, 0, data, offset + tailSize, head);
            tail = head;
            result = tailSize + head;
          }
        }
      }
      if (tail == head)  // if empty, reset to initial state
        hasDataToTake = false;
      return result;
    }

    public int Take(T[] data) {
      return Take(data, 0, data.Length);
    }

    public int Take(T[] data, int count) {
      return Take(data, 0, count);
    }

    /// <summary>
    /// Copies a specified portion of data from ring buffer, with the logical start at the oldest data (0 or head).
    /// </summary>
    /// <param name="data">The buffer to copy the data into.</param>
    /// <param name="offset">The offset in the data buffer to start writing at.</param>
    /// <param name="count">The number of items to read from the ring buffer.</param>
    /// <param name="bufferOffset">The logical offset in the ring buffer to start reading from.
    /// The origin for calculating the offset is head, unless the capacity has not been reached yet,
    /// in which case the origin is 0. This is tracked with the isFull field.</param>
    /// <returns>The number of bytes read.</returns>
    public int Read(T[] data, int offset, int count, int bufferOffset) {
      if (bufferOffset < 0)
        throw new ArgumentException("Read offset must be >= 0.", "offset");
      if (count < 0)
        throw new ArgumentException("Read count must be >= 0.", "count");
      if (count == 0)
        return count;
      // return only as much as we have data to read
      int maxCount = capacityReached ? Capacity - bufferOffset : head - bufferOffset;
      if (maxCount <= 0)
        return 0;
      if (count > maxCount)
        count = maxCount;

      // the possible configurations are: (H is head, S is start)
      // ~~~~~~~~~~~S~~~H~~~~~~~
      // ~~~~~~~H~~~S~~~~~~~~~~~

      // calculate offset from logical start of buffer (oldest logical byte)
      int start = bufferOffset;
      if (capacityReached) {
        start += head;
        if (start >= Capacity)
          start -= Capacity;
      }

      if (start < head)       // available data fits into contiguous space between start and head
        Buffer.BlockCopy(buffer, start, data, offset, count);
      else {                  // may have to copy two chunks of data
        int startGap = Capacity - start;
        int restCount = count - startGap;
        if (restCount <= 0)   // data to read fits into gap between start position and end of buffer
          Buffer.BlockCopy(buffer, start, data, offset, count);
        else {
          Buffer.BlockCopy(buffer, start, data, offset, startGap);
          Buffer.BlockCopy(buffer, 0, data, offset + startGap, restCount);
        }
      }
      return count;
    }

    //TODO Review ReadAdded() and ReadTaken() with respect to the use of isFull in Read()

    /// <summary>
    /// Copies a specified portion of data from the written but not taken ring buffer.
    /// </summary>
    /// <param name="data">The buffer to copy the data into.</param>
    /// <param name="offset">The offset in the data buffer to start writing at.</param>
    /// <param name="count">The number of items to read from the ring buffer.</param>
    /// <param name="bufferOffset">The logical offset in the ring buffer to start reading from.</param>
    /// <returns>The number of bytes read.</returns>
    public int ReadAdded(T[] data, int offset, int count, int bufferOffset) {
      if (bufferOffset < 0)
        throw new ArgumentException("Read offset must be >= 0.", "offset");
      if (count < 0)
        throw new ArgumentException("Read count must be >= 0.", "count");
      if (count == 0)
        return count;
      // return only as much as we have data to read
      int cn = Count;
      if ((bufferOffset + count) > cn)  // delta = count + bufferOffset - Count, newCount = count - delta
        count = cn - bufferOffset;
      if (count <= 0)
        return 0;

      // we have eliminated these configurations: (~~~ is data, --- is empty)
      // ~~~~~~~H---S---T~~~~~~~
      // ---S---T~~~~~~~H-------
      // -------T~~~~~~~H---S---
      // the remaining possible configurations are:
      // ~~~S~~~H-------T~~~~~~~
      // ~~~~~~~H-------T~~~S~~~
      // -------T~~~S~~~H-------

      // calculate offset from logical start of buffer (oldest logical byte)
      int start = tail + bufferOffset;
      if (start >= buffer.Length)
        start -= buffer.Length;

      if (tail < start) {
        if (start < head)
          Buffer.BlockCopy(buffer, start, data, offset, count);
        else {
          int startGap = buffer.Length - start;
          int restCount = count - startGap;
          if (restCount <= 0)
            Buffer.BlockCopy(buffer, start, data, offset, count);
          else {
            Buffer.BlockCopy(buffer, start, data, offset, startGap);
            Buffer.BlockCopy(buffer, 0, data, offset + startGap, restCount);
          }
        }
      }
      else  // tail > start
        Buffer.BlockCopy(buffer, start, data, offset, count);
      return count;
    }

    /// <summary>
    /// Copies a specified portion of data from the available (already taken) part of the ring buffer.
    /// </summary>
    /// <param name="data">The buffer to copy the data into.</param>
    /// <param name="offset">The offset in the data buffer to start writing at.</param>
    /// <param name="count">The number of items to read from the ring buffer.</param>
    /// <param name="bufferOffset">The logical offset in the available part of ring buffer to start reading from.</param>
    /// <returns>The number of bytes read.</returns>
    public int ReadTaken(T[] data, int offset, int count, int bufferOffset) {
      if (bufferOffset < 0)
        throw new ArgumentException("Read offset must be >= 0.", "offset");
      if (count < 0)
        throw new ArgumentException("Read count must be >= 0.", "count");
      if (count == 0)
        return count;
      // return only as much as we have data to read
      int av = AvailableToWrite;
      if ((bufferOffset + count) > av)  // delta = count + bufferOffset - Available, newCount = count - delta
        count = av - bufferOffset;
      if (count <= 0)
        return 0;

      // we have eliminated these configurations: (~~~ is data, --- is available)
      // ~~~S~~~H-------T~~~~~~~
      // ~~~~~~~H-------T~~~S~~~
      // -------T~~~S~~~H-------
      // the remaining possible configurations are:
      // ~~~~~~~H---S---T~~~~~~~
      // ---S---T~~~~~~~H-------
      // -------T~~~~~~~H---S---

      // calculate offset from logical start of available space (newest logical byte)
      int start = head + bufferOffset;
      if (start >= buffer.Length)
        start -= buffer.Length;

      if (head < start) {
        if (start < tail)
          Buffer.BlockCopy(buffer, start, data, offset, count);
        else {
          int startGap = buffer.Length - start;
          int restCount = count - startGap;
          if (restCount <= 0)
            Buffer.BlockCopy(buffer, start, data, offset, count);
          else {
            Buffer.BlockCopy(buffer, start, data, offset, startGap);
            Buffer.BlockCopy(buffer, 0, data, offset + startGap, restCount);
          }
        }
      }
      else  // head > start
        Buffer.BlockCopy(buffer, start, data, offset, count);
      return count;
    }
  }
}
