The .NET framework provides already a way of serializing and deserializing
object graphs. However, for the purposes of record storage in a database like,
for instance, Berkeley DB, it has several shortcomings:

* Inefficient usage of storage space - there is no need to always store
  the meta-data in each record.
* Slow - as it is entireley based on runtime type discovery.
* No facility for partial deserialization - extracting a secondary index
  field should not require deserializing the complete record.
* Does not necessarily support lexical sorting on numeric types - that
  is, the lexical sort order would be different from the numerical sort
  order (big-endian vs. little-endian).

Therefore, another serialization framework is provided to alleviate
these issues. It does this at the cost of extra coding, as it is currently
not based on runtime type information.
*  It requires that any data members to be serialized are accessible to the framework code (public or internal
visibility).
* It is intended to serialize only to generalized byte buffers in the form
  of `Span<byte>` which allows for zero-copy capability. This way one can
  avoid  intermediate byte buffers.

Any implementation of the framework has to subclass the following
cooperating abstract base classes:
* `Kds.Serialization.Formatter<F>`  
  This class controls the overall serialization process.
* `Kds.Serialization.ValueField<T>`  
  A concrete subclass implements the serialization of a specific value type.
* `Kds.Serialization.ReferenceField<T>`  
  A concrete subclass implements the serialization of a specific reference type.

Such an implementation is included with `StdFormatter` as well as a set of
associated `ValueField` subclasses for the basic value types, and
`ReferenceField` subclasses for strings, character and integer arrays.
All of the above is contained in the assembly _Kds.Serialization.dll_.

## Basic Usage

Here is a simple example of how to use the framework with the standard formatter.
```
// instantiate big-endian serialization formatter
StdFormatter fmt = new StdFormatter(ByteOrder.BigEndian);
// register fields as defaults for the type (by passing true)
new VendorField(fmt, true);
new SalesRepField(fmt, true);
new StockItemField(fmt, true);
  
// create instances of Vendor, SalesRep, StockItem where
// the Vendor instance is the root of the object graph
Vendor vendor = new Vendor();
...
  
byte[] buffer = new byte[2048];
var target = new Span<byte>(buffer);
  
// serialize Vendor (and all reachable objects like SalesRep and StockItems)
int index = 0;
fmt.SerializeObject(target, vendor, ref index);
  
// deserialize Vendor object graph
var source = new ReadOnlySpan<byte>(buffer);
index = 0;

var vendor2 = fmt.DeserializeObject<Vendor>(source, ref index);
```

## Defining A Persistence Model

Unlike the built-in .NET serialization framework, we have to write code to
specify how a given class or struct is serialized. Let's assume we have the
following class:
```
public class StockItem
{
  internal string name;
  internal string sku;
  internal float? price;
  internal int? quantity;

  public string Name { get => name; set => name = value; }
  public string Sku { get => sku; set => sku = value; }
  public float? Price { get => price; set => price = value; }
  public int? Quantity { get => quantity; set => quantity = value; }
}
```

Then we have to define a `ReferenceField<T>` subclass to serialize this class:
```
public class StockItemField : ReferenceField<StockItem>
{
  public StockItemField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

  protected override void SerializeValue(Span<byte> target, StockItem value) {
    Fmt.SerializeObject<string>(target, value.name);
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
    Fmt.DeserializeObject<string>(source, ref instance.sku);
    instance.price = Fmt.DeserializeStruct<float>(source);
    instance.quantity = Fmt.DeserializeStruct<int>(source);
  }

  protected override void SkipValue(ReadOnlySpan<byte> source) {
    if (Fmt.Skip<string>(source))
      if (Fmt.Skip<string>(source))
        if (Fmt.Skip<float>(source))
          Fmt.Skip<int>(source);
  }
}
```
All the overridden methods must be re-entrant, that is, their implementations
may only rely on the arguments passed to the method, and on object state that
will not change during (de)serialization, unless one can guarantee that they
cannot be called recursively. The latter applies to those fields that serialize
the basic value types, as their methods would not call serialization methods
on other fields.

### Responsibilities of `SerializeValue<T>(Span<byte> target, T value)`

Here, one chooses which members of the class get serialized in which
order. This is simply done by calling the various `SerializeObject<T>()`
or `SerializeStruct<T>()` overloads of the formatter implementation -
accessible through the protected `Field<T, F>.Fmt` property.

This is a **must** override.

### Responsibilities of `DeserializeInstance<T>(ReadOnlySpan<byte> source, ref T instance)`

If necessary, an instance of the class must be created, and all those
fields that should be populated initially can be deserialized here as well,
as long as it is done in the exact same order as during serialization.

However, one **must not** deserialize any members which might directly
or indirectly (recursively) reference the instance that was just created.
Generally, the safest approach is to stop deserialization at the first
reference type member (in serialization order), and leave the rest of
deserialization to the implementation of `DeserializeMembers()`.

Note that there are two different signatures/overloads for calling `Formatter.DeserializeXXX<T>()`:
* Reference type members:  
  `Formatter.DeserializeObject<T>(ReadOnlySpan<T> source, ref T member);`  
   or  
  `T member = Formatter.DeserializeObject<T>(ReadOnlySpan<T> source)`
* Value type members:
  `Formatter.DeserializeStruct<T>(ReadOnlySpan<T> source, ref T member, out bool isNull);`  
  or  
  `T member = (T)Formatter.DeserializeStruct<T>(ReadOnlySpan<T> source);`  
  where the cast `(T)` is only necessary when `T` is not a nullable type.

This is a **must** override.

### Responsibilities of `DeserializeMembers<T>(ReadOnlySpan<byte> source, T instance)`

The argument is the class instance created/initialized in
DeserializeInstance()</code>. Here is the place where one must
deserialize those reference type members which can have a reference back to
the argument. Again, the order of serialization must be followed.</p>

This is an **optional** override (only when needed).

### Responsibilities of `SkipValue(ReadOnlySpan<byte> source)`

This allows skipping parts of the serialized instance. The members must be
skipped in the exact same order in which they were serialized. Additionally,
the routine must return immediately if one of the calls to
`Formatter.Skip<T>(source)` returns `false`.

This is a **must** override, it can be empty or throw an exception if
partial deserialization is never used.

## Defining Which Fields To Use

Calling any of the `Formatter.SerializeXXX<T>()` methods
causes the default field for the argument's type T to be used for the actual
serialization of the argument. The `Kds.Serialization.Formatter` class
has no default fields registered, but the `StdFormatter` subclass comes with fields
pre-registered for all the basic value types as well as strings and integer
arrays.

Typically, any fields that handle user defined types should be registered
as the default field type to serialize or deserialize the user defined type.
This is shown in the Basic Usage section above.

Sometimes it may be necessary to treat different instances of the same
type differently (e.g. fixed size vs. variable size arrays, which are both
the same type). In this case, one uses a field instance directly, like in
this example where class `MyClas` contains a member that should
be serialized as a fixed size byte array (using a `BinaryField` instance):

```
public class MyField: ReferenceField<MyClass>
{
  private ReferenceField<byte[]> binField;

  public MyField(Formatter fmt, bool isDefault): base(fmt, isDefault) {
    // not the default field for byte[] - so we pass false for isDefault
    binField = new BinaryField(fmt, false, 16);
  }

  // MyClass.member is a fixed size byte array of length 16
  protected override void SerializeValue(Span<byte> target, MyClass value) {
    ...
    binField.Serialize(target, value.member);
    ...
  }
  
  protected override void DeserializeMembers(ReadOnlySpan<byte> source, MyClass value) {
    ...
    binField.Deserialize(source, ref value.member);
    ...
  }

  ...
}
```

## Object References and Pre-existing Objects

The serialization mechanism can deal with references to already serialized
objects (in the same byte buffer), including circular references, by storing an
object handle instead of repeatedly serializing the same object again (which
could lead to infinite recursion). However, sometimes there is a need to
serialize references to objects that exist outside of the byte buffer.
* Such objects can be system wide objects, and then they often are singletons. One
  generally does not want to serialize and then re-create or update these
  objects during deserialization.
* Other objects are those that belong to the same object graph but are 
  serialized into a separate byte buffer.

The way to deal with this problem is to register these objects before
serializing/deserializing into/from a given byte buffer:
```
// instances of MyClass contain references to externalObj1 and externalObj2
formatter.SetPermanentReferences(externalObj1, externalObj2);
...
formatter.SerializeObject<MyClass>(target, myObject, ref index);
```

When deserializing, these pre-existing objects must be registered in the
exact same order again:
```
formatter.SetPermanentReferences(externalObj1, externalObj2);
...
formatter.DeserializeObject<MyClass>(source, ref myObject, ref index);
```
## Key Based vs. Pointer Based References

The serialization framework described above takes care of pointer-based
references within the same object graph, even if it is stored in multiple
separate byte buffers (see above).

Often, when using a database (e.g. key-value storage like Berkeley DB)
objects (or small object graphs) would be stored as key/data pairs (or records),
and references between such records would be encoded as primary and
foreign keys (like in a relational database).

The serialization frame work described above does not (at this time)
provide a mechanism for generating keys for references between serialized
object graphs. It is up to the programmer to create the keys and to
re-establish object references after deserialization (based on matching keys),
or to use the mechanism of external object registration as described above.
