#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace KdSoft.Utils.Tests
{
  public class RollingFileTests
  {
    public RollingFileTests() {
    }

    const string line = "This is test line #{0} for thread '{1}', which shows us how the files roll over when iterating at great speed\n";


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
      FileStream stream = null;
      try {
        var bufferWriter = new ArrayBufferWriter<byte>(1024);
        for (int indx = 0; indx < 9790000; indx++) {
          // every 100 lines check for rollover
          if (indx % 100 == 0) {
            stream = await fileFactory.GetCurrentFileStream().ConfigureAwait(false);
          }
          var str = string.Format(line, indx, "T");
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

    void WriteLines(RollingFileFactory fileFactory, Channel<string> channel, string threadId) {
      for (int indx = 0; indx < 5000000; indx++) {
        var str = string.Format(line, indx, threadId);
        channel.Writer.TryWrite(str);
      }
    }

    [Fact]
    public async Task MultiThreadRollFiles() {
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

      var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions {
        AllowSynchronousContinuations = true,
        SingleReader = true,
        SingleWriter = false
      });

      var threadA = new Thread(() => { WriteLines(fileFactory, channel, "A"); });
      var threadB = new Thread(() => { WriteLines(fileFactory, channel, "B"); });
      var threadC = new Thread(() => { WriteLines(fileFactory, channel, "C"); });

      var readTask = Task.Run(async () => {
        var bufferWriter = new ArrayBufferWriter<byte>(1024);
        int counter = 0;
        FileStream stream = null;
        await foreach (var str in channel.Reader.ReadAllAsync().ConfigureAwait(false)) {
          // every 100 lines check for rollover
          if (counter++ % 100 == 0) {
            stream = await fileFactory.GetCurrentFileStream().ConfigureAwait(false);
          }
          bufferWriter.Clear();
          var byteCount = Encoding.UTF8.GetBytes(str, bufferWriter);
          await stream.WriteAsync(bufferWriter.WrittenMemory).ConfigureAwait(false);
        }
      });

      threadA.Start();
      threadB.Start();
      threadC.Start();

      threadA.Join();
      threadB.Join();
      threadC.Join();

      channel.Writer.Complete();

      await readTask.ConfigureAwait(false);
    }

  }
}
#endif
