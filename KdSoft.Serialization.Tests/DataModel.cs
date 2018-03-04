using System;
using System.Collections.Generic;

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

  public class LineItem
  {
    internal float quantity;
    internal StockItem item;

    public float Quantity { get => quantity; set => quantity = value; }
    public StockItem Item { get => item; set => item = value; }

    public static bool Equals(LineItem x, LineItem y) {
      if (object.ReferenceEquals(x, y))
        return true;
      if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
        return false;
      return x.quantity == y.quantity && x.item == y.item;
    }

    public override bool Equals(object obj) {
      return Equals(this, obj as LineItem);
    }
    public static bool operator ==(LineItem x, LineItem y) { return Equals(x, y); }
    public static bool operator !=(LineItem x, LineItem y) { return !(x == y); }
  }

  public class LineItemField : ReferenceField<LineItem>
  {
    public LineItemField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    protected override void SerializeValue(Span<byte> target, LineItem value) {
      Fmt.SerializeStruct<float>(target, value.quantity);
      Fmt.SerializeObject<StockItem>(target, value.item);
    }

    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref LineItem instance) {
      if (instance == null)
        instance = new LineItem();
    }

    protected override void DeserializeMembers(ReadOnlySpan<byte> source, LineItem instance) {
      instance.quantity = Fmt.DeserializeStruct<float>(source) ?? default;
      Fmt.DeserializeObject<StockItem>(source, ref instance.item);
    }

    protected override void SkipValue(ReadOnlySpan<byte> source) {
      if (Fmt.Skip<float>(source))
        if (Fmt.Skip<StockItem>(source))
          Fmt.SkipSequence<StockItem>(source);
    }
  }

  public class Order
  {
    internal string orderNumber;
    internal DateTimeOffset creationTime;
    internal List<LineItem> lineItems;

    public string OrderNumber { get => orderNumber; set => orderNumber = value; }
    public DateTimeOffset CreationTime { get => creationTime; set => creationTime = value; }
    public List<LineItem> LineItems { get => lineItems; set => lineItems = value; }

    public static bool Equals(Order x, Order y) {
      if (object.ReferenceEquals(x, y))
        return true;
      if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
        return false;
      bool result = x.orderNumber == y.orderNumber && x.creationTime == y.creationTime;
      if (result) {
        if (x.LineItems?.Count != y.LineItems?.Count)
          return false;
        if (x.LineItems == null)
          return true;
        for (int indx = 0; indx < x.LineItems.Count; indx++) {
          if (x.LineItems[indx] != y.LineItems[indx])
            return false;
        }
      }
      return result;
    }

    public override bool Equals(object obj) {
      return Equals(this, obj as Order);
    }
    public static bool operator ==(Order x, Order y) { return Equals(x, y); }
    public static bool operator !=(Order x, Order y) { return !(x == y); }
  }

  public class OrderField : ReferenceField<Order>
  {
    public OrderField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    protected override void SerializeValue(Span<byte> target, Order value) {
      Fmt.SerializeStruct<DateTimeOffset>(target, value.creationTime);
      Fmt.SerializeObject<string>(target, value.orderNumber);
      Fmt.SerializeObjects<LineItem>(target, value.lineItems);
    }

    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref Order instance) {
      if (instance == null)
        instance = new Order();
    }

    AddItem<LineItem, List<LineItem>> InitLineItems(int size, ref List<LineItem> lineItems) {
      if (size < 0) // size < 0 means the collection is deserialized as null
        return null;
      if (lineItems == null)
        lineItems = new List<LineItem>(size);
      else
        lineItems.Capacity += size;
      return (LineItem item, List<LineItem> collection) => collection.Add(item);
    }

    protected override void DeserializeMembers(ReadOnlySpan<byte> source, Order instance) {
      instance.creationTime = Fmt.DeserializeStruct<DateTimeOffset>(source) ?? default;
      Fmt.DeserializeObject<string>(source, ref instance.orderNumber);
      Fmt.DeserializeObjects<LineItem, List<LineItem>>(source, InitLineItems, ref instance.lineItems);
    }

    protected override void SkipValue(ReadOnlySpan<byte> source) {
      if (Fmt.Skip<DateTimeOffset>(source))
        if (Fmt.Skip<string>(source))
          Fmt.SkipSequence<LineItem>(source);
    }
  }

}