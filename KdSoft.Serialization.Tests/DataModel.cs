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