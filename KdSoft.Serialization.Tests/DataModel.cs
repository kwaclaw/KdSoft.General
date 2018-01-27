using System;
using KdSoft.Serialization.Buffer;

namespace KdSoft.Serialization.Tests
{
  public class Vendor
  {
    internal string name;
    internal string street;
    internal string city;
    internal string state;
    internal string zipCode;
    internal string phoneNumber;
    internal SalesRep salesRep;

    public string Name { get => name; set => name = value; }
    public string Street { get => street; set => street = value; }
    public string City { get => city; set => city = value; }
    public string State { get => state; set => state = value; }
    public string ZipCode { get => zipCode; set => zipCode = value; }
    public string PhoneNumber { get => phoneNumber; set => phoneNumber = value; }
    public SalesRep SalesRep { get => salesRep; set => salesRep = value; }

    public static bool Equals(Vendor x, Vendor y) {
      if (object.ReferenceEquals(x, y))
        return true;
      if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
        return false;
      return x.name == y.name && x.street == y.street && x.city == y.city && x.state == y.state
        && x.zipCode == y.zipCode && x.phoneNumber == y.phoneNumber && x.salesRep == y.salesRep;
    }

    public override bool Equals(object obj) {
      // need to cast obj to Vendor, as otherwise we would call the base implementation
      // of static Equals, leading to infinite recursion
      return Equals(this, obj as Vendor);
    }

    public static bool operator ==(Vendor x, Vendor y) { return Equals(x, y); }
    public static bool operator !=(Vendor x, Vendor y) { return !(x == y); }
  }

  public class VendorField : ReferenceField<Vendor>
  {
    public VendorField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    protected override void SerializeValue(Span<byte> target, Vendor value) {
      Fmt.SerializeObject<string>(target, value.Name);
      Fmt.SerializeObject<string>(target, value.Street);
      Fmt.SerializeObject<string>(target, value.City);
      Fmt.SerializeObject<string>(target, value.State);
      Fmt.SerializeObject<string>(target, value.ZipCode);
      Fmt.SerializeObject<string>(target, value.PhoneNumber);
      Fmt.SerializeObject<SalesRep>(target, value.SalesRep);
    }

    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref Vendor instance) {
      if (instance == null)
        instance = new Vendor();
    }

    protected override void DeserializeMembers(ReadOnlySpan<byte> source, Vendor instance) {
      Fmt.DeserializeObject<string>(source, ref instance.name);
      Fmt.DeserializeObject<string>(source, ref instance.street);
      Fmt.DeserializeObject<string>(source, ref instance.city);
      Fmt.DeserializeObject<string>(source, ref instance.state);
      Fmt.DeserializeObject<string>(source, ref instance.zipCode);
      Fmt.DeserializeObject<string>(source, ref instance.phoneNumber);
      Fmt.DeserializeObject<SalesRep>(source, ref instance.salesRep);
    }

    protected override void SkipValue(ReadOnlySpan<byte> source) {
      if (Fmt.Skip<string>(source))
        if (Fmt.Skip<string>(source))
          if (Fmt.Skip<string>(source))
            if (Fmt.Skip<string>(source))
              if (Fmt.Skip<string>(source))
                if (Fmt.Skip<string>(source))
                  Fmt.Skip<SalesRep>(source);
    }
  }

  public class SalesRep
  {
    internal string name;
    internal string phoneNumber;

    public string Name { get => name; set => name = value; }
    public string PhoneNumber { get => phoneNumber; set => phoneNumber = value; }

    public static bool Equals(SalesRep x, SalesRep y) {
      if (object.ReferenceEquals(x, y))
        return true;
      if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
        return false;
      return x.name == y.name && x.phoneNumber == y.phoneNumber;
    }

    public override bool Equals(object obj) {
      return Equals(this, obj as SalesRep);
    }
    public static bool operator ==(SalesRep x, SalesRep y) { return Equals(x, y); }
    public static bool operator !=(SalesRep x, SalesRep y) { return !(x == y); }
  }

  public class SalesRepField : ReferenceField<SalesRep>
  {
    public SalesRepField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    protected override void SerializeValue(Span<byte> target, SalesRep value) {
      Fmt.SerializeObject<string>(target, value.Name);
      Fmt.SerializeObject<string>(target, value.PhoneNumber);
    }

    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref SalesRep instance) {
      if (instance == null)
        instance = new SalesRep();
    }

    protected override void DeserializeMembers(ReadOnlySpan<byte> source, SalesRep instance) {
      instance.Name = Fmt.DeserializeObject<string>(source);
      instance.phoneNumber = Fmt.DeserializeObject<string>(source);
    }

    protected override void SkipValue(ReadOnlySpan<byte> source) {
      if (Fmt.Skip<string>(source))
        Fmt.Skip<string>(source);
    }
  }

  public class StockItem
  {
    internal string name;
    internal string category;
    internal string vendor;
    internal string sku;
    internal float? price;
    internal int? quantity;

    public string Name { get => name; set => name = value; }
    public string Category { get => category; set => category = value; }
    public string Vendor { get => vendor; set => vendor = value; }
    public string Sku { get => sku; set => sku = value; }
    public float? Price { get => price; set => price = value; }
    public int? Quantity { get => quantity; set => quantity = value; }

    public static bool Equals(StockItem x, StockItem y) {
      if (object.ReferenceEquals(x, y))
        return true;
      if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
        return false;
      return x.name == y.name && x.category == y.category && x.vendor == y.vendor
        && x.sku == y.sku && x.price == y.price && x.quantity == y.quantity;
    }

    public override bool Equals(object obj) {
      return Equals(this, obj as StockItem);
    }
    public static bool operator ==(StockItem x, StockItem y) { return Equals(x, y); }
    public static bool operator !=(StockItem x, StockItem y) { return !(x == y); }
  }

  public class StockItemField : ReferenceField<StockItem>
  {
    public StockItemField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    protected override void SerializeValue(Span<byte> target, StockItem value) {
      Fmt.SerializeObject<string>(target, value.name);
      Fmt.SerializeObject<string>(target, value.category);
      Fmt.SerializeObject<string>(target, value.vendor);
      Fmt.SerializeObject<string>(target, value.sku);
      Fmt.SerializeStruct<float>(target, value.price);
      Fmt.SerializeStruct<int>(target, value.quantity);
    }

    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref StockItem instance) {
      if (instance == null)
        instance = new StockItem();
    }

    protected override void DeserializeMembers(ReadOnlySpan<byte> source, StockItem instance) {
      Fmt.DeserializeObject<string>(source, ref instance.name);
      Fmt.DeserializeObject<string>(source, ref instance.category);
      Fmt.DeserializeObject<string>(source, ref instance.vendor);
      Fmt.DeserializeObject<string>(source, ref instance.sku);
      instance.price = Fmt.DeserializeStruct<float>(source);
      instance.quantity = Fmt.DeserializeStruct<int>(source);
    }

    protected override void SkipValue(ReadOnlySpan<byte> source) {
      if (Fmt.Skip<string>(source))
        if (Fmt.Skip<string>(source))
          if (Fmt.Skip<string>(source))
            if (Fmt.Skip<string>(source))
              if (Fmt.Skip<float>(source))
                Fmt.Skip<int>(source);
    }
  }
}