using System.IO;

namespace KdSoft.StreamUtils
{
  public class SerialFileStreamWriter: SerialStreamWriter<FileStream>
  {
    public SerialFileStreamWriter(
      string fileName,
      FileMode mode,
      int bufferSize = Constants.DefaultIOConcurrency * Constants.DefaultBufferSize,
      FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan
    ) : base(new FileStream(fileName, mode, FileAccess.Write, FileShare.Write | FileShare.Delete, bufferSize, options)) { }

    protected override void Abort() {
      File.Delete(Stream.Name);
    }
  }

  public class SerialFileAsyncStreamWriter: SerialAsyncStreamWriter<FileStream>
  {
    public SerialFileAsyncStreamWriter(
      string fileName,
      FileMode mode,
      int bufferSize = Constants.DefaultIOConcurrency * Constants.DefaultBufferSize,
      FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan
    ) : base(new FileStream(fileName, mode, FileAccess.Write, FileShare.Write | FileShare.Delete, bufferSize, options)) { }

    protected override void Abort() {
      File.Delete(Stream.Name);
    }
  }

  public class RandomFileStreamWriter: RandomStreamWriter<FileStream>
  {
    public RandomFileStreamWriter(
      string fileName,
      FileMode mode,
      int bufferSize = Constants.DefaultIOConcurrency * Constants.DefaultBufferSize,
      FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan
    ) : base(new FileStream(fileName, mode, FileAccess.Write, FileShare.Write | FileShare.Delete, bufferSize, options)) { }

    protected override void Abort() {
      File.Delete(Stream.Name);
    }
  }

  public class RandomFileAsyncStreamWriter: RandomAsyncStreamWriter<FileStream>
  {
    public RandomFileAsyncStreamWriter(
      string fileName,
      FileMode mode,
      int bufferSize = Constants.DefaultIOConcurrency * Constants.DefaultBufferSize,
      FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan
    ) : base(new FileStream(fileName, mode, FileAccess.Write, FileShare.Write | FileShare.Delete, bufferSize, options)) { }

    protected override void Abort() {
      File.Delete(Stream.Name);
    }
  }
}
