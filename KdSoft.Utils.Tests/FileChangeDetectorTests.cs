using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Utils.Tests
{
  public class FileChangeDetectorTests
  {
    readonly ITestOutputHelper _output;

    public FileChangeDetectorTests(ITestOutputHelper output) {
      this._output = output;
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void CheckFilters() {
      //var nf = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes;
      //var settleTime = TimeSpan.FromSeconds(2);
      using var fsw1 = new FileSystemWatcher("C:\\Temp", "*.txt");
      using var fsw2 = new FileSystemWatcher("C:\\Temp", "*.bin") {
        Filters = { "*.txt", "*.dat" }
      };

      Assert.Equal(fsw1.Filters, new[] { "*.txt" });
      Assert.Equal(fsw2.Filters, new[] { "*.bin", "*.txt", "*.dat" });
    }
#endif


    [Fact]
    public async Task DetectDirectoryChanges() {
      var changedDir = new DirectoryInfo(Path.Combine(TestUtils.ProjectDir!, "ChangedFiles"));
      var filesDir = new DirectoryInfo(Path.Combine(TestUtils.ProjectDir!, "TestFiles"));
      var changeList = new List<RenamedEventArgs>();
      var errorList = new List<ErrorEventArgs>();

      var nf = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes;
      using var fcd = new FileChangeDetector(changedDir.FullName, (string?)null, false, nf, TimeSpan.FromSeconds(2));
      fcd.FileChanged += (object s, RenamedEventArgs e) => {
        changeList.Add(e);
      };
      fcd.ErrorEvent += (object s, ErrorEventArgs e) => {
        errorList.Add(e);
      };

      var waitTime = fcd.SettleTime + TimeSpan.FromSeconds(2);
      int fileCount = 0;
      foreach (var file in changedDir.GetFiles()) {
        file.Delete();
      }

      fcd.Start(true);

      foreach (var file in filesDir.GetFiles()) {
        File.Copy(file.FullName, Path.Combine(changedDir.FullName, file.Name));
        fileCount += 1;
      }

      await Task.Delay(waitTime);

      // files created
      Assert.Equal(changeList.Count, fileCount);

      changeList.Clear();
      errorList.Clear();
      var testFile3 = Path.Combine(changedDir.FullName, "TestFile3.txt");
      var testFile3A = Path.Combine(changedDir.FullName, "TestFile3A.txt");

      // change, rename, delete
      File.WriteAllText(testFile3, "12345");
      File.Move(testFile3, testFile3A);  // rename
      File.Delete(testFile3A);

      // create + change, move out (= delete), re-create + change, rename
      File.WriteAllText(testFile3, "12345");
      var tmpFile = Path.Combine("C:\\Temp", "TestFile3.txt");
      File.Delete(tmpFile);
      File.Move(testFile3, tmpFile);  // move out (= delete)
      File.WriteAllText(testFile3, "6789");
      File.Move(testFile3, testFile3A);  // rename

      await Task.Delay(waitTime);

      foreach (var ch in changeList) {
        _output.WriteLine($"===== {ch.OldName} => {ch.Name} =====");
        _output.WriteLine($"ChangeType: {ch.ChangeType}\n");
      }

      foreach (var err in errorList) {
        _output.WriteLine($"===== Error =====");
        _output.WriteLine(err.GetException().ToString());
      }

      Assert.Equal(2, changeList.Count);

      Assert.Equal(WatcherChangeTypes.Changed | WatcherChangeTypes.Renamed | WatcherChangeTypes.Deleted, changeList[0].ChangeType);
      Assert.Equal(testFile3, changeList[0].OldFullPath);
      Assert.Null(changeList[0].Name);  // due to deletion

      Assert.Equal(WatcherChangeTypes.All, changeList[1].ChangeType);
      Assert.Equal(testFile3, changeList[1].OldFullPath);
      Assert.Equal(testFile3A, changeList[1].FullPath);
    }
  }
}
