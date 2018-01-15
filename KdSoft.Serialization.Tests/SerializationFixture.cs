using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace KdSoft.Serialization.Tests
{
  public class SerializationFixture : IDisposable
  {
    const string dataDirName = @"Data";
    readonly string dataPath;
    readonly List<Vendor> vendors;
    readonly List<StockItem> stockItems;

    public IReadOnlyList<Vendor> Vendors => vendors;
    public IReadOnlyList<StockItem> StockItems => stockItems;

    public SerializationFixture() {
      dataPath = Path.Combine(TestUtils.ProjectDir, dataDirName);
      vendors = LoadVendors();
      stockItems = LoadStockItems();
    }

    void InitVendorFromRegex(Vendor vendor, GroupCollection groups) {
      vendor.Name = groups[1].Value;
      vendor.Street = groups[2].Value;
      vendor.City = groups[3].Value;
      vendor.State = groups[4].Value;
      vendor.ZipCode = groups[5].Value;
      vendor.PhoneNumber = groups[6].Value;
      SalesRep rep = new SalesRep();
      rep.Name = groups[7].Value;
      rep.PhoneNumber = groups[8].Value;
      vendor.SalesRep = rep;
    }

    List<Vendor> LoadVendors() {
      StreamReader sr = new StreamReader(Path.Combine(dataPath, "vendors.txt"));
      Regex rgx = new Regex("(.*)#(.*)#(.*)#(.*)#(.*)#(.*)#(.*)#(.*)", RegexOptions.Compiled);
      string line;
      var result = new List<Vendor>();

      while ((line = sr.ReadLine()) != null && line.Length > 0) {
        Match match = rgx.Match(line);
        GroupCollection groups = match.Groups;
        if (groups.Count < 9)
          throw new ApplicationException("Input error on vendors.txt");
        Vendor vendor = new Vendor();
        InitVendorFromRegex(vendor, groups);
        result.Add(vendor);
      }
      return result;
    }

    void InitStockItemFromRegex(StockItem item, GroupCollection groups) {
      item.Name = groups[1].Value;
      item.Sku = groups[2].Value;
      item.Price = float.Parse(groups[3].Value);
      item.Quantity = int.Parse(groups[4].Value);
      item.Category = groups[5].Value;
      item.Vendor = groups[6].Value;
    }

    List<StockItem> LoadStockItems() {
      var sr = new StreamReader(Path.Combine(dataPath, "inventory.txt"));
      var rgx = new Regex("(.*)#(.*)#(.*)#(.*)#(.*)#(.*)", RegexOptions.Compiled);
      string line;
      var result = new List<StockItem>();

      while ((line = sr.ReadLine()) != null && line.Length > 0) {
        Match match = rgx.Match(line);
        GroupCollection groups = match.Groups;
        if (groups.Count < 7)
          throw new ApplicationException("Input error on inventory.txt");
        StockItem item = new StockItem();
        InitStockItemFromRegex(item, groups);
        result.Add(item);
      }
      return result;
    }

    protected virtual void Dispose(bool disposing) {
      if (disposing) {
        //Env.Close();
      }
    }
    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }

  [CollectionDefinition("DataModel")]
  public class SerializationGroup : ICollectionFixture<SerializationFixture>
  {
    // This class has no code, and is never created. Its purpose is simply to be
    // the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces. 
  }
}
