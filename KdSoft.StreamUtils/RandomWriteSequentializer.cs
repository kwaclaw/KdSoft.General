using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// Converts random synchronous writes into sequential synchronous writes to an <see cref="IFilterWriter"/> instance.
  /// This is achieved by buffering and re-ordering them into a sorted set.
  /// </summary>
  /// <remarks><list type="">
  /// <item>Thread-safe. Outgoing writes will be ordered and never run concurrently.</item>
  /// <item>Writes may overlap, but the overlapping portions of data will not be written to the target.</item>
  /// <item>This implementation will complete itself automatically after <see cref="EndWrite"/> was called once
  /// there are no more pending write requests. It will not be necessary to call <see cref="SetComplete"/>, and if
  /// it is called with <c>abort == false</c> before all write requests are satisfied, an exception will be thrown.</item>
  /// </list></remarks>
  public class RandomWriteSequentializer : IRandomWriter
  {
    IFilterWriter writer;
    byte[] outBuffer;
    int outSize;

    readonly object syncObj = new object();
    SortedSet<WriteRequest> writeQueue;
    WriteRequest finalWriteRequest;
    long sequentialOffset;
    long endOffset;
    bool isComplete;

    class BufferSegment
    {
      public int Start { get; set; }
      public int Count { get; set; }
    }

    class WriteRequest
    {
      public byte[] Buffer { get; set; }
      public BufferSegment Segment { get; set; }
      public long Offset { get; set; }
    }

    class RequestComparer : IComparer<WriteRequest>
    {
      IComparer<long> longComparer = Comparer<long>.Default;

      public int Compare(WriteRequest x, WriteRequest y) {
        return longComparer.Compare(x.Offset, y.Offset);
      }
    }

    public RandomWriteSequentializer(IFilterWriter writer, int queueCapacity) {
      if (writer == null)
        throw new ArgumentNullException("writer");
      this.writer = writer;
      outBuffer = new byte[4096];
      isComplete = false;
      writeQueue = new SortedSet<WriteRequest>(new RequestComparer());
    }

    public long CurrentOffset {
      get { lock (syncObj) { return sequentialOffset; } }
    }

    public long CurrentGap {
      get {
        lock (syncObj) {
          if (writeQueue.Count == 0)
            return 0;
          else {
            long minOffset = writeQueue.Min.Offset;
            return minOffset - sequentialOffset;
          }
        }
      }
    }

    public int QueuedCount {
      get { lock (syncObj) { return writeQueue.Count; } }
    }

    /// <summary>
    /// Called when writing has been completed successfully, or when aborted without error.
    /// </summary>
    public event EventHandler OnCompleted;

    #region Implementation (call under lock protection)

    void HandleCompleted() {
      var onCompleted = OnCompleted;
      if (onCompleted != null)
        onCompleted(this, EventArgs.Empty);
    }

    void CancelQueue() {
      writeQueue.Clear();
      isComplete = true;
    }

    static bool IsReadyToRun(long sequentialOffset, BufferSegment segment, long offset) {
      long offsetDelta = sequentialOffset - offset;
      // we can only write contiguous data
      if (offsetDelta < 0) {
        return false;
      }
      if (offsetDelta >= segment.Count) {  // this write request only covers an already written range
        segment.Count = 0;
      }
      else {  // part of this write request covers an unwritten range, must modify paramteters
        segment.Start += (int)offsetDelta;
        segment.Count -= (int)offsetDelta;
      }
      return true;
    }

    void PerformWrite(byte[] buffer, int start, int count) {
      if (outSize > 0) {
        int requiredSize = outSize + count;
        if (outBuffer.Length < requiredSize) {
          var oldBuffer = outBuffer;
          outBuffer = new byte[((requiredSize >> 3) + 1) << 3];  // 8 byte aligned
          Buffer.BlockCopy(oldBuffer, 0, outBuffer, 0, outSize);
        }
        Buffer.BlockCopy(buffer, start, outBuffer, outSize, count);
        int written = writer.Write(outBuffer, 0, requiredSize);
        int remaining = requiredSize - written;
        if (remaining > 0)
          Buffer.BlockCopy(outBuffer, written, outBuffer, 0, remaining);
        outSize = remaining;
      }
      else {
        int written = writer.Write(buffer, start, count);
        int remaining = count - written;
        if (remaining > 0) {
          if (outBuffer.Length < remaining)
            outBuffer = new byte[((remaining >> 3) + 1) << 3];  // 8 byte aligned
          Buffer.BlockCopy(buffer, start + written, outBuffer, 0, remaining);
        }
        outSize = remaining;
      }
    }

    void ExecuteWriteRequests(List<WriteRequest> wrs) {
      for (int indx = 0; indx < wrs.Count; indx++) {
        var wr = wrs[indx];
        if (wr.Segment.Count > 0) {
          PerformWrite(wr.Buffer, wr.Segment.Start, wr.Segment.Count);
        }
      }
    }

    void PerformFinalWrite(byte[] buffer, int start, int count) {
      if (outSize > 0) {
        int requiredSize = outSize + count;
        if (outBuffer.Length < requiredSize) {
          var oldBuffer = outBuffer;
          outBuffer = new byte[((requiredSize >> 3) + 1) << 3];  // 8 byte aligned
          Buffer.BlockCopy(oldBuffer, 0, outBuffer, 0, outSize);
        }
        Buffer.BlockCopy(buffer, start, outBuffer, outSize, count);
        writer.FinalWrite(outBuffer, 0, requiredSize);
      }
      else {
        writer.FinalWrite(buffer, start, count);
      }
    }

    void CheckFinalWrite() {
      // can only perform final write request if all active requests are done
      var fwr = finalWriteRequest;
      if (fwr != null && writeQueue.Count == 0 && IsReadyToRun(sequentialOffset, fwr.Segment, fwr.Offset)) {
        PerformFinalWrite(fwr.Buffer, fwr.Segment.Start, fwr.Segment.Count);
        sequentialOffset += fwr.Segment.Count;
        isComplete = true;
        HandleCompleted();
      }
    }

    #endregion

    #region IRandomWriter Members

    public bool Write(byte[] buffer, int start, int count, long targetOffset) {
      lock (syncObj) {
        if (isComplete) {
          return false;
        }

        if (finalWriteRequest != null && (targetOffset + count) > endOffset)
          throw new InvalidOperationException("Write operation extending beyond final size.");

        var segment = new BufferSegment() { Start = start, Count = count };
        var wr = new WriteRequest { Buffer = buffer, Segment = segment, Offset = targetOffset };
        writeQueue.Add(wr);

        var nextWrites = new List<WriteRequest>();
        long serialWriteOffset = sequentialOffset;

        var inOrder = writeQueue.GetEnumerator();
        while (inOrder.MoveNext()) {
          var wrq = inOrder.Current;
          if (IsReadyToRun(serialWriteOffset, wrq.Segment, wrq.Offset)) {
            nextWrites.Add(wrq);
            serialWriteOffset += wrq.Segment.Count;
          }
          else
            break;
        }

        if (nextWrites.Count == 0)
          return true;
        for (int wrIndx = 0; wrIndx < nextWrites.Count; wrIndx++)
          writeQueue.Remove(nextWrites[wrIndx]);
        // update sequentialOffset now because CheckFinalWrite will check it again
        sequentialOffset = serialWriteOffset;

        try {
          ExecuteWriteRequests(nextWrites);
          CheckFinalWrite();
        }
        catch {
          CancelQueue();
          throw;
        }

        return true;
      }
    }

    public bool EndWrite(byte[] buffer, int start, int count, long targetOffset) {
      lock (syncObj) {
        if (isComplete || finalWriteRequest != null) {
          return false;
        }

        endOffset = targetOffset + count;
        bool endExceeded = false;
        if (writeQueue.Count == 0) {
          endExceeded = sequentialOffset > endOffset;
        }
        else {
          var max = writeQueue.Max;
          endExceeded = (max.Offset + max.Segment.Count) > endOffset;
        }
        if (endExceeded)
          throw new InvalidOperationException("Final size must not be less than existing size.");

        var segment = new BufferSegment() { Start = start, Count = count };
        finalWriteRequest = new WriteRequest { Buffer = buffer, Segment = segment, Offset = targetOffset };

        try {
          // if we have already written to the final offset then we need to perform the final write now
          CheckFinalWrite();
        }
        catch {
          CancelQueue();
          throw;
        }

        return true;
      }
    }

    public bool SetComplete(bool abort) {
      lock (syncObj) {
        if (isComplete)
          return false;
        if (!abort && finalWriteRequest == null)
          throw new InvalidOperationException("Cannot set complete before EndWrite was called.");
        if (abort) {
          CancelQueue();
          HandleCompleted();
          return true;
        }
        else if (writeQueue.Count > 0)
          throw new InvalidOperationException("There are still pending write requests.");
        else {  // should not happen
          isComplete = true;
          HandleCompleted();
          return true;
        }
      }
    }

    #endregion
  }
}
