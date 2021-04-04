using System.IO;

namespace KdSoft.StreamUtils
{
  public class SerialFileStreamReader: SerialStreamReader<FileStream>
  {
    public SerialFileStreamReader(
      string fileName,
      FileMode mode,
      int bufferSize = Constants.DefaultIOConcurrency * Constants.DefaultBufferSize,
      FileOptions options = FileOptions.SequentialScan
    ) : base(new FileStream(fileName, mode, FileAccess.Read, FileShare.Read, bufferSize, options)) { }
  }

  public class SerialFileAsyncStreamReader: SerialAsyncStreamReader<FileStream>
  {
    public SerialFileAsyncStreamReader(
      string fileName,
      FileMode mode,
      int bufferSize = Constants.DefaultIOConcurrency * Constants.DefaultBufferSize,
      FileOptions options = FileOptions.Asynchronous | FileOptions.SequentialScan
    ) : base(new FileStream(fileName, mode, FileAccess.Read, FileShare.Read, bufferSize, options)) { }
  }

  public class RandomFileStreamReader: RandomStreamReader<FileStream>
  {
    public RandomFileStreamReader(
      string fileName,
      FileMode mode,
      int bufferSize = Constants.DefaultBufferSize,
      FileOptions options = FileOptions.RandomAccess
    ) : base(new FileStream(fileName, mode, FileAccess.Read, FileShare.Read, bufferSize, options)) { }
  }

  public class RandomFileAsyncStreamReader: RandomAsyncStreamReader<FileStream>
  {
    public RandomFileAsyncStreamReader(
      string fileName,
      FileMode mode,
      int bufferSize = Constants.DefaultBufferSize,
      FileOptions options = FileOptions.Asynchronous | FileOptions.RandomAccess
    ) : base(new FileStream(fileName, mode, FileAccess.Read, FileShare.Read, bufferSize, options)) { }
  }
}
