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

      fmt.InitSerialization(target, 0);
      foreach (var vendor in fixture.Vendors) {
        vendorField.Serialize(target, vendor);
      }
      fmt.FinishSerialization(target);

    }

  }
}
