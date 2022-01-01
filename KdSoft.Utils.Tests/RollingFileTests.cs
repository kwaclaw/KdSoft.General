using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using KdSoft.Reflection;
using Xunit;

namespace KdSoft.Utils.Tests
{
  public class RollingFileTests
  {
    public RollingFileTests() {
    }

    const string line = "This is test line #{0}, which shows us how the files roll over when iterating at great speed\n";

    #if NETSTANDARD2_1 || NET5_0_OR_GREATER

    [Fact]
    public async Task RollFiles() {
      var dirInfo = new DirectoryInfo(@"C:\Temp\RollFiles");
      dirInfo.Create();
      Func<DateTimeOffset, string> fileNameSelector = (dto) => string.Format("app-{0:yyyy-MM-dd}", dto);

      using var fileFactory = new RollingFileFactory(
          dirInfo,
          fileNameSelector,
          ".txt",
          true,
          4096,
          11,
          true
      );
      FileStream? stream = null;
      try {
        var bufferWriter = new ArrayBufferWriter<byte>(1024);
        for (int indx = 0; indx < 790000; indx++) {
          // every 100 lines check for rollover
          if (indx % 100 == 0) {
            stream = await fileFactory.GetCurrentFileStream().ConfigureAwait(false);
          }
          var str = string.Format(line, indx);
          bufferWriter.Clear();
          var byteCount = Encoding.UTF8.GetBytes(str, bufferWriter);
          await stream.WriteAsync(bufferWriter.WrittenMemory).ConfigureAwait(false);
        }
      }
      finally {
        // do not do this - the fileFactory disposes the last used stream
        // stream?.Dispose();
      }
    }

#endif
  }
}
