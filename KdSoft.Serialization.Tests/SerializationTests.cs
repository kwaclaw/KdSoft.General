using System;
using System.Collections.Generic;
using System.Linq;
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

      List<StockItem> stockItems = null;
      fmt.InitDeserialization(buffer, 0);
      fmt.DeserializeObjects(target, StdFormatter.InitList, ref stockItems);
      int endIndex2 = fmt.FinishDeserialization();

      Assert.Equal(endIndex, endIndex2);
      Assert.Equal(fixture.StockItems, stockItems);
    }

    [Fact]
    public void SerializeOrders() {
      var buffer = new byte[256000];

      // serialization

      var fmt = new StdFormatter(ByteConverter.SystemByteOrder);
      // instantiate and register Field instances
      var stockItemField = new StockItemField(fmt, true);
      var lineItemField = new LineItemField(fmt, true);
      var salesRepField = new SalesRepField(fmt, true);
      var orderField = new OrderField(fmt, true);
      var target = new Span<byte>(buffer);

      fmt.SetPermanentReferences(fixture.StockItems.Cast<object>());
      fmt.InitSerialization(0);
      fmt.SerializeObjects(target, fixture.Orders);
      int endIndex = fmt.FinishSerialization(target);

      // deserialization

      List<Order> orders = null;
      var fmt2 = new StdFormatter(ByteConverter.SystemByteOrder);
      stockItemField = new StockItemField(fmt2, true);
      lineItemField = new LineItemField(fmt2, true);
      salesRepField = new SalesRepField(fmt2, true);
      orderField = new OrderField(fmt2, true);
      target = new Span<byte>(buffer);

      fmt2.SetPermanentReferences(fixture.StockItems.Cast<object>());
      fmt2.InitDeserialization(buffer, 0);
      fmt2.DeserializeObjects(target, StdFormatter.InitList, ref orders);
      int endIndex2 = fmt2.FinishDeserialization();

      Assert.Equal(endIndex, endIndex2);
      Assert.Equal(fixture.Orders, orders);

      // check if the deserialized StockItems match an input StockItem by object reference
      // and not by equals comparison - this is required becasue we are using permanent references!
      bool FindByReference(StockItem item) {
        foreach (var inItem in fixture.StockItems) {
          if (object.ReferenceEquals(item, inItem))
            return true;
        }
        return false;
      }

      foreach (var ord in orders)
        foreach (var li in ord.LineItems) {
          Assert.True(FindByReference(li.Item));
        }
    }
  }
}
