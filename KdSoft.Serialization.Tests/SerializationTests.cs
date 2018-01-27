using System;
using System.Collections.Generic;
using System.Text;
using KdSoft.Serialization.Buffer;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Serialization.Tests
{
  [Collection("DataModel")]
  public class SerializationTests
  {
    readonly SerializationFixture fixture;
    readonly ITestOutputHelper output;

    public SerializationTests(SerializationFixture fixture, ITestOutputHelper output) {
      this.fixture = fixture;
      this.output = output;
    }

    [Fact]
    public void SerializeVendors() {
      var buffer = new byte[4096];
      var fmt = new StdFormatter(ByteConverter.SystemByteOrder);

      // instantiate and register Field instances
      var vendorField = new VendorField(fmt, true);
      var salesRepField = new SalesRepField(fmt, true);
      var target = new Span<byte>(buffer);

      fmt.InitSerialization(0);
      foreach (var vendor in fixture.Vendors) {
        vendorField.Serialize(target, vendor);
      }
      int endIndex = fmt.FinishSerialization(target);

      var vendors = new List<Vendor>();
      fmt.InitDeserialization(target, 0);
      for (int indx = 0; indx < fixture.Vendors.Count; indx++) {
        var vendor = fmt.DeserializeObject<Vendor>(buffer);
        vendors.Add(vendor);
      }
      int endIndex2 = fmt.FinishDeserialization();

      Assert.Equal(endIndex, endIndex2);
      Assert.Equal(fixture.Vendors, vendors);
    }

    [Fact]
    public void SerializeStockItems() {
      var buffer = new byte[256000];
      var fmt = new StdFormatter(ByteConverter.SystemByteOrder);

      // instantiate and register Field instances
      var stockItemField = new StockItemField(fmt, true);
      var target = new Span<byte>(buffer);

      // here we are serializing collections

      fmt.InitSerialization(0);
      fmt.SerializeObjects(target, fixture.StockItems);
      int endIndex = fmt.FinishSerialization(target);

      // callback that sets up any type of custom collection to be deserialized into;
      // creates collection (if necessary) and returns a delegate to add items to the collection
      AddItem<StockItem, List<StockItem>> InitSequence(int size, ref List<StockItem> collection) {
        if (collection == null)
          collection = new List<StockItem>(size);
        else
          collection.Capacity += size;
        return (StockItem item, List<StockItem> list) => list.Add(item);
      }

      List<StockItem> stockItems = null;
      fmt.InitDeserialization(buffer, 0);
      fmt.DeserializeObjects(target, InitSequence, ref stockItems);
      int endIndex2 = fmt.FinishDeserialization();

      Assert.Equal(endIndex, endIndex2);
      Assert.Equal(fixture.StockItems, stockItems);
    }

  }
}
