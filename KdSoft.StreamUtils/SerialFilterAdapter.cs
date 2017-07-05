using System;
using System.Threading.Tasks;
using KdSoft.Utils;

namespace KdSoft.StreamUtils
{
  public class SerialWriterException: Exception
  {
    public SerialWriterException() : base() { }
    public SerialWriterException(string message) : base(message) { }
  }

  /// <summary>
  /// Exposes an <see cref="ISerialAsyncWriter"/> as <see cref="IFilterWriter" />.
  /// Will raise errors if not all data could be written, or for any other async write errors.
  /// </summary>
  public class SerialFilterWriter: IFilterWriter, IDisposable
  {
    ISerialAsyncWriter writer;
    TaskCreationOptions options;
    TaskWaiter taskWaiter;

    public SerialFilterWriter(ISerialAsyncWriter writer, TaskCreationOptions options = TaskCreationOptions.None) {
      if (writer == null)
        throw new ArgumentNullException("writer");
      this.writer = writer;
      this.options = options;
      taskWaiter = new TaskWaiter(true);
    }

    public event EventHandler<EventArgs<Exception>> OnAsyncCompleted {
      add { taskWaiter.OnCompleted += value; }
      remove { taskWaiter.OnCompleted -= value; }
    }

    byte[] CloneBuffer(byte[] buffer, int start, int count) {
      byte[] result = new byte[count];
      Buffer.BlockCopy(buffer, start, result, 0, count);
      return result;
    }

    #region IFilterWriter Members

    /// <summary>
    /// Implements <see cref="IFilterWriter.Write"/>.
    /// </summary>
    /// <remarks>Makes a copy of the input buffer, as the input buffer might be a shared buffer which could
    /// already be re-used while its contents are still needed by an unfinished asynchronous write.</remarks>
    public int Write(byte[] buffer, int start, int count) {
      if (taskWaiter.AddingComplete)
        return 0;
      var writeBuffer = CloneBuffer(buffer, start, count);
      var writeTask = writer.WriteAsync(writeBuffer, 0, count, options);
      var waitTask = writeTask.ContinueWith(antecedent => {
        if (antecedent.Result.Count < count)
          throw new SerialWriterException("Write operation incomplete");
      }, (TaskContinuationOptions)options);
      if (!taskWaiter.Add(waitTask))
        return 0;
      return count;
    }

    /// <summary>
    /// Implements <see cref="IFilterWriter.FinalWrite"/>.
    /// </summary>
    /// <remarks>Makes a copy of the input buffer, as the input buffer might be a shared buffer which could
    /// already be re-used while its contents are still needed by an unfinished asynchronous write.</remarks>
    public void FinalWrite(byte[] buffer, int start, int count) {
      var writeBuffer = CloneBuffer(buffer, start, count);
      var writeTask = writer.FinalWriteAsync(writeBuffer, 0, count, options);
      taskWaiter.Add(writeTask);
      taskWaiter.CompleteAdding();
    }

    #endregion

    #region IDisposable Members

    protected virtual void Dispose(bool disposing) {
      if (disposing) {
        var tw = taskWaiter;
        if (tw != null) {
          tw.Dispose();
        }
      }
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #endregion
  }
}
