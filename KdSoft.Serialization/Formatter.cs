/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006, 2008
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections.Generic;
using KdSoft.Utils;
using KdSoft.Utils.Portable;

namespace KdSoft.Serialization
{
  /// <summary>
  /// Implementation of abstract class <see cref="KdSoft.Serialization.Formatter{F}"/>.
  /// </summary>
  /// <remarks><list type="bullet">
  /// <item><description>The cooperating descendants of <see cref="ValueField{T}"/> and
  ///   <see cref="ReferenceField{T}"/> are designed to generate serialized representations
  ///   that can be used for lexical sorting.</description></item>
  /// <item><description>Each implementation of the abstract base class will add specific API methods necessary
  ///   for its use. In this case the methods are <see cref="InitSerialization(int)"/>, <see cref="FinishSerialization(Span{byte})"/>,
  ///   <see cref="InitDeserialization(ReadOnlySpan{byte}, int)"/> and <see cref="FinishDeserialization()"/>.
  ///   Check the example for how they are used.</description></item>
  /// </list></remarks>
  /// <example>Basic usage of byte buffer oriented serialization:
  /// <code>
  /// StdFormatter fmt = new StdFormatter(ByteOrder.BigEndian);
  /// new StringField(fmt, true);  // default serializer for strings
  /// new IntField(fmt, true);     // default serializer for integers
  /// ...
  /// Span&lt;byte> target = ... (target can only exist on stack, not on heap)
  /// int index = 0;
  /// fmt.InitSerialization( index);
  /// fmt.SerializeObject&lt;string>(target, name);
  /// fmt.SerializeStruct&lt;int>(target, age);
  /// index = fmt.FinishSerialization(target); 
  /// ...
  /// // do some work
  /// ...
  /// ReadOnlySpan&lt;byte> source = ... (source can only exist on stack, not on heap)
  /// index = 0;
  /// fmt.InitDeserialization(source, index);
  /// fmt.DeserializeObject&lt;string>(source, ref name);
  /// age = (int)fmt.DeserializeStruct&lt;int>(source);
  /// index = fmt.FinishDeserialization(); 
  /// </code></example>
  public class Formatter: KdSoft.Serialization.Formatter<Formatter>
  {
    //TODO In stead of abstract Formatter base class implement a span-specific interface
    //TODO turn the Formatter base class into some internal implementation class

    //TODO add Polymorph<T> type/field to serialize with type information, e.g. for polymorphic collections (needs Type map)

    //TODO add Keyed<T> type/field to identify instances by key (for deserializing from multiple buffers)

    // buffer for status bits, used for write operations only
    private byte[] statusBuffer;
    // save status buffer for re-use
    private byte[] statusBuf;

    private int statusIndx;  // two-bit index (four per byte)
    private int startIndx;   // index where (de)serialization starts
    internal int valueIndx;  // byte index

    private int openObjCount;
    private object[] openObjects;               // maps object handle to object
    private Dictionary<object, int> openObjMap; // maps object to object handle
    private List<object> permanentObjects;
    private HashSet<object> permanentObjSet;

    private ByteConverter converter;
    private Dictionary<Type, object> fieldRegistry;

    /// <summary>
    /// Constructor. Sets the byte order.
    /// </summary>
    public Formatter(ByteOrder byteOrder) {
      openObjCount = 0;
      openObjects = new object[0];
      openObjMap = new Dictionary<object, int>(new ObjectEqualityComparer());
      permanentObjects = new List<object>();
      permanentObjSet = new HashSet<object>(new ObjectEqualityComparer());
      converter = new ByteConverter(byteOrder);
      fieldRegistry = new Dictionary<Type, object>();
    }

    /// <summary>ByteConverter instance used for serialization.</summary>
    public ByteConverter Converter {
      get { return converter; }
    }

    /// <summary>Resets the object handle table.</summary>
    /// <remarks>Must be called before each serialization or deserialization.</remarks>
    protected void ResetOpenObjects() {
      if (openObjects.Length < permanentObjects.Count)
        openObjects = new object[permanentObjects.Count];
      else
        Array.Clear(openObjects, 0, openObjects.Length);
      openObjMap.Clear();
      permanentObjects.CopyTo(openObjects);
      openObjCount = permanentObjects.Count;
      for (int indx = 0; indx < openObjCount; indx++)
        openObjMap[openObjects[indx]] = indx;
    }

    /// <summary>Bitmask for serializing <see cref="SerialStatus"/> values.</summary>
    [CLSCompliant(false)]
    protected const uint twoBitIndexMask = 0x00000003;
    /// <summary>Bitmask for serializing <see cref="SerialStatus"/> values.</summary>
    protected static readonly byte[] slotMasks = { 0x03, 0x0C, 0x30, 0xC0 };
    /// <summary>Bitmask for clearing unused <see cref="SerialStatus"/> values.</summary>
    protected static readonly byte[] clearMasks = { 0x03, 0x0F, 0x3F, 0xFF };

    #region Implementation Overrides

    /// <inheritdoc />
    protected internal override void WriteStatus(SerialStatus value) {
      unchecked {
        int byteIndex = statusIndx >> 2;
        int size = byteIndex + 1;
        if (statusBuffer.Length < size) {
          size = size << 1;
          Array.Resize<byte>(ref statusBuffer, size);
        }
        int byteTwoBitIndex = statusIndx & (int)twoBitIndexMask;
        // clear two-bit slot
        uint bufByte = (uint)(statusBuffer[byteIndex] & ~slotMasks[byteTwoBitIndex]);
        // set bits in slot
        bufByte |= ((uint)value << (byteTwoBitIndex << 1));
        statusBuffer[byteIndex] = (byte)bufByte;
      }
      statusIndx++;
    }

    /// <inheritdoc />
    protected internal override void WriteObjRef(Span<byte> target, Int64 value) {
      converter.WriteBytes(value, target, ref valueIndx);
    }

    /// <inheritdoc />
    protected internal override void WriteCount(Span<byte> target, int value) {
      converter.WriteBytes(value, target, ref valueIndx);
    }

    /// <inheritdoc />
    protected internal override SerialStatus ReadStatus(ReadOnlySpan<byte> source) {
      SerialStatus result;
      unchecked {
        int byteIndex = statusIndx >> 2;
        int byteTwoBitIndex = statusIndx & (int)twoBitIndexMask;
        // mask out the interesting bits in the slot
        uint bufByte = (uint)source[byteIndex] & slotMasks[byteTwoBitIndex];
        result = (SerialStatus)(bufByte >> (byteTwoBitIndex << 1));
      }
      statusIndx++;
      return result;
    }

    /// <inheritdoc />
    protected internal override Int64 ReadObjRef(ReadOnlySpan<byte> source) {
      converter.ReadBytes(source, ref valueIndx, out Int64 result);
      return result;
    }

    /// <inheritdoc />
    protected internal override void SkipObjRef() {
      valueIndx += sizeof(int);
    }

    /// <inheritdoc />
    protected internal override int ReadCount(ReadOnlySpan<byte> source) {
      converter.ReadBytes(source, ref valueIndx, out int result);
      return result;
    }

    /// <inheritdoc />
    protected internal override bool OpenReference(object obj, out Int64 handle) {
      int hndl;
      if (openObjMap.TryGetValue(obj, out hndl)) {
        handle = hndl;
        return true;
      }
      else {
        openObjCount++;
        if (openObjCount >= openObjects.Length)
          Array.Resize<object>(ref openObjects, openObjCount << 1);
        openObjects[openObjCount] = obj;
        openObjMap[obj] = openObjCount;
        handle = openObjCount;
        return false;
      }
    }

    /// <inheritdoc />
    protected internal override object GetReference(Int64 handle) {
      return openObjects[handle];
    }

    /// <inheritdoc />
    protected override bool InternalRegisterField<T>(Field<T, Formatter> field) {
      bool result = !fieldRegistry.ContainsKey(typeof(T));
      if (result)
        fieldRegistry[typeof(T)] = field;
      return result;
    }

    /// <inheritdoc />
    public override void SetPermanentReferences(params object[] objects) {
      SetPermanentReferences((IList<object>)objects);
    }

    /// <inheritdoc />
    public override void SetPermanentReferences(IEnumerable<object> objects) {
      foreach (object obj in objects) {
        if (permanentObjSet.Add(obj))          
          permanentObjects.Add(obj);
        else
          throw new SerializationException("Permanent object already registered.");
      }
    }

    /// <inheritdoc/>
    public override Field<T, Formatter> GetField<T>() {
      object field;
      Type dataType = typeof(T);
      if (!fieldRegistry.TryGetValue(dataType, out field))
        throw new SerializationException("No field registered for type '" + dataType.Name + "'.");
      return (Field<T, Formatter>)field;
    }

    #endregion

    /* currently we do not resize the buffer
    // call to make sure that buffer for serialization is large enough
    public void Checktarget(int requiredSize) {
      requiredSize = requiredSize + valueIndx;
      if (valueBuf.Length < requiredSize) {
        requiredSize += requiredSize >> 1;  // go 50% beyond
        Array.Resize<byte>(ref valueBuf, requiredSize);
      }
    }
    */

    #region Serialization API

    private static int nextStatusByte(int statusIndx) {
      return (statusIndx + 3) >> 2;
    }

    /// <summary>Initializes serialization process.</summary>
    /// <remarks>
    /// The calls to the serialization methods of <see cref="Formatter{F}"/> must be bracketed
    /// between calls to <see cref="InitSerialization"/> and <see cref="FinishSerialization"/>.
    /// Any objects serialized between these two calls will have shared object references
    /// serialized only once, avoiding multiple serialization of the same object.
    /// <para>Used together with <see cref="FinishSerialization"/>.</para>
    /// </remarks>
    /// <example>In this example, <c>SalesRep</c> instances shared by <c>Vendor</c>
    /// instances are only serialized once.
    /// <code>
    /// StdFormatter fmt = new StdFormatter(ByteOrder.BigEndian);
    /// new VendorField(fmt, true);
    /// new SalesRepField(fmt, true);
    /// ...
    /// Span&lt;byte> target = ... (target can only exist on stack)
    /// int index = 0;
    /// fmt.InitSerialization(index);
    /// fmt.SerializeObject&lt;Vendor>(target, vendor1);
    /// fmt.SerializeObject&lt;Vendor>(target, vendor2);
    /// fmt.SerializeObject&lt;Vendor>(target, vendor3);
    /// index = fmt.FinishSerialization(target); 
    /// </code></example>
    /// <seealso cref="FinishSerialization"/>
    /// <param name="index">Start index of buffer. Serialization will start writing
    /// to the buffer at this index.</param>
    public void InitSerialization(int index) {
      ResetOpenObjects();

      statusIndx = 0;
      startIndx = index;
      valueIndx = index + sizeof(uint); // leave space for length prefix
      if (statusBuf == null)
        statusBuf = new byte[4];
      statusBuffer = statusBuf;
    }

    /// <summary>Completes serialization process.</summary>
    /// <remarks>See <see cref="InitSerialization"/>. Must be called right after
    /// the last object was serialized. At that point, object references cannot
    /// be shared with previously serialized objects anymore, even if they both
    /// target the same buffer.</remarks>
    /// <example>See <see cref="InitSerialization"/>.</example>
    /// <seealso cref="InitSerialization"/>
    /// <returns>End index. Points to the next byte following the end of the
    /// serialized data in the buffer.</returns>
    public int FinishSerialization(Span<byte> target) {
      // save status buffer for re-use
      statusBuf = statusBuffer;

      int bufIndx = startIndx;
      // size should not include prefix size!
      int bufSize = valueIndx - bufIndx - sizeof(uint);
      // write length prefix to start of value buffer
      converter.WriteBytes(bufSize, target, ref bufIndx);
      int endIndex = valueIndx + nextStatusByte(statusIndx);
      if (target.Length < endIndex)
        throw new IndexOutOfRangeException("Buffer size too small.");

      // clear unused bits in status buffer
      if (statusIndx > 0)
        unchecked {
          int lastStatusIndx = statusIndx - 1;
          int lastByteIndex = lastStatusIndx >> 2;
          statusBuffer[lastByteIndex] &= clearMasks[lastStatusIndx & (int)twoBitIndexMask];
        }

      // copy status buffer to end of target
      bufIndx = valueIndx;
      int statusBufSize = nextStatusByte(statusIndx);

      var statusSpan = new ReadOnlySpan<byte>(statusBuffer, 0, statusBufSize);
      converter.WriteValueBytes(statusSpan, target, ref bufIndx);
      //System.Buffer.BlockCopy(statusBuffer, 0, target, bufIndx, bufSize);

      return endIndex;
    }

#if BUFFER_API
    #region Single Instances

    /// <overloads>Serializes an object graph into a byte buffer.</overloads>
    /// <summary>Serializes a nullable value type into a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize, or <c>null</c>.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// serializes the <c>value </c>argument.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStruct<T>(Span<byte> target, T? value, ValueField<T, Formatter> field, ref int index)
      where T: struct
    {
      InitSerialization(index);
      field.Serialize(target, value);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes a nullable value type into a byte buffer using
    /// the default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize, or <c>null</c>.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStruct<T>(Span<byte> target, T? value, ref int index)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      SerializeStruct<T>(target, value, field, ref index);
    }

    /// <summary>Serializes a value type into a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.
    /// However, this does not allow to pass <c>nulls</c>. Therefore the companion
    /// method <see cref="SerializeNull"/> is provided.</remarks>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// serializes the <c>value </c>argument.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStruct<T>(Span<byte> target, ref T value, ValueField<T, Formatter> field, ref int index)
      where T: struct 
    {
      InitSerialization(index);
      field.Serialize(target, ref value);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes a value type into a byte buffer using the default
    /// <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.
    /// However, this does not allow to pass <c>nulls</c>. Therefore the companion
    /// method <see cref="SerializeNull"/> is provided.</remarks>
    /// <typeparam name="T">Value type to serialize.</typeparam>
    /// <param name="value">Struct instance to serialize.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStruct<T>(Span<byte> target, ref T value, ref int index)
      where T: struct 
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      SerializeStruct<T>(target, ref value, field, ref index);
    }

    /// <summary>Serializes a <c>null</c> into a byte buffer.</summary>
    /// <remarks>Companion method for <see cref="SerializeStruct{T}(Span{byte}, ref T, ref int)" />.</remarks>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// serialized <c>null</c>.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeNull(Span<byte> target, ref int index) {
      InitSerialization(index);
      SerializeNull();
      index = FinishSerialization(target);
    }

    /// <summary>Serializes a reference type into a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Reference type to serialize.</typeparam>
    /// <param name="obj">Object to serialize.</param>
    /// <param name="field"><see cref="ReferenceField{T, Formatter}"/> instance that
    /// serializes the <c>obj</c>argument.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObject<T>(Span<byte> target, T obj, ReferenceField<T, Formatter> field, ref int index) 
      where T: class 
    {
      InitSerialization(index);
      field.Serialize(target, obj);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes a reference type into a byte buffer using the default
    /// <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Reference type to serialize.</typeparam>
    /// <param name="obj">Object to serialize.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObject<T>(Span<byte> target, T obj, ref int index) 
      where T: class 
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      SerializeObject<T>(target, obj, field, ref index);
    }

    #endregion Single Instances

    #region Value Type Arrays

    /// <overloads>Serializes sequences (arrays, collections) of a given type
    /// using a specific <see cref="Field{T, F}"/> instance.</overloads>
    /// <summary>Serializes value type arrays.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStructs<T>(Span<byte> target, T[] value, ValueField<T, Formatter> field, ref int index)
      where T: struct 
    {
      InitSerialization(index);
      SerializeStructs<T>(target, value, field);
      index = FinishSerialization(target);
    }

    /// <overloads>Serializes sequences (arrays, collections) of a value type
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</overloads>
    /// <summary>Serializes value type arrays.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStructs<T>(Span<byte> target, T[] value, ref int index)
      where T: struct
    {
      InitSerialization(index);
      SerializeStructs<T>(target, value);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes arrays of nullable value types.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStructs<T>(Span<byte> target, T?[] value, ValueField<T, Formatter> field, ref int index)
      where T: struct
    {
      InitSerialization(index);
      SerializeStructs<T>(target, value, field);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes arrays of nullable value types.</summary>
    /// <typeparam name="T">Type of array elements - must be value type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStructs<T>(Span<byte> target, T?[] value, ref int index)
      where T: struct 
    {
      InitSerialization(index);
      SerializeStructs<T>(target, value);
      index = FinishSerialization(target);
    }

    #endregion Value Type Arrays

    #region Value Type Collections

    /// <summary>Serializes value type sequences accessible through <see cref="IList{T}"/>.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStructs<T>(Span<byte> target, IList<T> value, ValueField<T, Formatter> field, ref int index)
      where T: struct
    {
      InitSerialization( index);
      SerializeStructs<T>(target, value, field);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IList{T}"/>.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStructs<T>(Span<byte> target, IList<T> value, ref int index)
      where T: struct 
    {
      InitSerialization(index);
      SerializeStructs<T>(target, value);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IEnumerable{T}"/></summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStructs<T>(Span<byte> target, IEnumerable<T> value, ValueField<T, Formatter> field, ref int index)
      where T: struct 
    {
      InitSerialization(index);
      SerializeStructs<T>(target, value, field);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes value type sequences accessible through <see cref="IEnumerable{T}"/></summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeStructs<T>(Span<byte> target, IEnumerable<T> value, ref int index)
      where T: struct 
    {
      InitSerialization(index);
      SerializeStructs<T>(target, value);
      index = FinishSerialization(target);
    }

    #endregion Value Type Collections

    #region Reference Type Arrays

    /// <summary>Serializes reference type arrays.</summary>
    /// <typeparam name="T">Type of array elements - must be reference type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="field">Field that serializes the array elements.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(Span<byte> target, T[] value, ReferenceField<T, Formatter> field, ref int index)
      where T: class
    {
      InitSerialization(index);
      SerializeObjects<T>(target, value, field);
      index = FinishSerialization(target);
    }

    /// <overloads>Serializes sequences (arrays, collections) of a reference type
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</overloads>
    /// <summary>Serializes reference type arrays.</summary>
    /// <typeparam name="T">Type of array elements - must be reference type.</typeparam>
    /// <param name="value">Array to serialize.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(Span<byte> target, T[] value, ref int index)
      where T: class
    {
      InitSerialization(index);
      SerializeObjects<T>(target, value);
      index = FinishSerialization(target);
    }

    #endregion Reference Type Arrays

    #region Reference Type Collections

    /// <summary>Serializes reference type sequences accessible through <see cref="IList{T}"/>.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(Span<byte> target, IList<T> value, ReferenceField<T, Formatter> field, ref int index)
      where T: class 
    {
      InitSerialization(index);
      SerializeObjects<T>(target, value, field);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IList{T}"/>.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IList{T}"/> sequence to serialize.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(Span<byte> target, IList<T> value, ref int index)
      where T: class
    {
      InitSerialization(index);
      SerializeObjects<T>(target, value);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IEnumerable{T}"/></summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="field">Field that serializes the sequence elements.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(Span<byte> target, IEnumerable<T> value, ReferenceField<T, Formatter> field, ref int index)
      where T: class 
    {
      InitSerialization( index);
      SerializeObjects<T>(target, value, field);
      index = FinishSerialization(target);
    }

    /// <summary>Serializes reference type sequences accessible through <see cref="IEnumerable{T}"/></summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="value"><see cref="IEnumerable{T}"/> sequence to serialize.</param>
    /// <param name="target">Target buffer. Must have sufficient size to hold the 
    /// complete serialized object graph.</param>
    /// <param name="index">Start index in target buffer.</param>
    public void SerializeObjects<T>(Span<byte> target, IEnumerable<T> value, ref int index)
      where T: class
    {
      InitSerialization(index);
      SerializeObjects<T>(target, value);
      index = FinishSerialization(target);
    }

    #endregion Reference Type Collections
#endif  // BUFFER_API

    #endregion Serialization API

    #region Deserialization API

    /// <summary>Initializes deserialization process.</summary>
    /// <remarks>
    /// The calls to the deserialization methods of <see cref="Formatter{F}"/> must be bracketed
    /// between calls to <see cref="InitDeserialization"/> and <see cref="FinishDeserialization"/>.
    /// The same data types must be deserialized in the same order as were serialized between
    /// the original calls to <see cref="InitSerialization"/> and <see cref="FinishSerialization"/>.
    /// Used together with <see cref="FinishDeserialization"/>.
    /// </remarks>
    /// <example>In this example, <c>SalesRep</c> instances shared by <c>Vendor</c>
    /// instances were only serialized once, and thus must be deserialized the same way.
    /// <code>
    /// StdFormatter fmt = new StdFormatter(ByteOrder.BigEndian);
    /// new VendorField(fmt, true);
    /// new SalesRepField(fmt, true);
    /// ...
    /// ReadOnlySpan&lt;byte> source = ... (source can only exist on stack)
    /// int index = 0;
    /// fmt.InitDeserialization(source, index);
    /// // get the field only once - more efficient than calling fmt.DeserializeObject()
    /// VendorField vendorField = (VendorField)fmt.GetField&lt;Vendor>();
    /// vendorField.DeserializeObject(source, ref vendor1);
    /// vendorField.DeserializeObject(source, ref vendor2);
    /// vendorField.DeserializeObject(source, ref vendor3);
    /// index = fmt.FinishDeserialization(); 
    /// </code></example>
    /// <example>In this example, the sales rep phone number is extracted from a serialized
    /// <c>Vendor</c> instance, using partial deserialization by skipping nodes in the object
    /// graph. The sales rep is the 7th serialized member in <c>Vendor</c>, and the phone
    /// number is the 2nd serialized member in <c>SalesRep</c>. 
    /// <code>
    /// StdFormatter fmt = new StdFormatter(ByteOrder.BigEndian);
    /// new VendorField(fmt, true);
    /// new SalesRepField(fmt, true);
    /// ...
    /// ReadOnlySpan&lt;byte> source = ... (source can only exist on stack)
    /// int index = 0;
    /// fmt.InitDeserialization(source, index);
    /// string phoneNo = "";
    /// if (fmt.SkipMembers&lt;Vendor>(6, 1)) {
    ///   fmt.DeserializeObject&lt;string>(source, ref phoneNo);
    /// }
    /// else
    ///   throw new ApplicationException("Member path invalid.");
    /// index = fmt.FinishDeserialization();
    /// </code></example>
    /// <seealso cref="FinishDeserialization"/>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void InitDeserialization(ReadOnlySpan<byte> source, int index) {
      ResetOpenObjects();
      valueIndx = index;

      // read size of values, initialize statusIndx (status is at end of values)
      converter.ReadBytes(source, ref valueIndx, out int bufSize);
      statusIndx = (valueIndx + bufSize) << 2; // statusByte << 2

      // we cannot copy to a local status buffer because we don't know its size until
      // we have deserialized the last item. And since the source argument is a stack -only
      // value type, we cannot store a reference to it, so we only have statusIndx to work with.
      statusBuffer = null;  // not used for reading
    }

    /// <summary>Completes deserialization process.</summary>
    /// <remarks>See <see cref="InitDeserialization"/>. This call must mirror the
    /// original call to <see cref="FinishSerialization"/>, that is, it must occur
    /// right after the last object was deserialized.</remarks>
    /// <example>See <see cref="InitDeserialization"/>.</example>
    /// <seealso cref="InitDeserialization"/>
    /// <returns>End index. Points to the next byte following the end of the
    /// serialized data in the buffer.</returns>
    public int FinishDeserialization() {
      int endIndex = nextStatusByte(statusIndx);
      statusBuffer = null;
      return endIndex;
    }

#if BUFFER_API
    #region Single Instances

    /// <overloads>Deserializes an object graph from a byte buffer.</overloads>
    /// <summary>Deserializes a nullable value type from a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>buffer</c> contents.</param>
    /// <returns>Deserialized struct instance, or <c>null</c>.</returns>
    public T? DeserializeStruct<T>(ReadOnlySpan<byte> source, ref int index, ValueField<T, Formatter> field)
      where T: struct
    {
      InitDeserialization(source, index);
      T? result = field.Deserialize(source);
      index = FinishDeserialization();
      return result;
    }

    /// <summary>Deserializes a nullable value type from a byte buffer using the
    /// default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    /// <returns>Deserialized struct instance, or <c>null</c>.</returns>
    public T? DeserializeStruct<T>(ReadOnlySpan<byte> source, ref int index)
      where T: struct
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      return DeserializeStruct<T>(source, ref index, field);
    }

    /// <summary>Deserializes a value type from a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.</remarks>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="value">Struct instance to deserialize.</param>
    /// <param name="isNull">Returns <c>true</c> if a <c>null</c> is deserialized,
    /// in which case the <c>value</c> parameter is ignored.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    /// <param name="field"><see cref="ValueField{T, Formatter}"/> instance that
    /// deserializes the <c>buffer</c> contents.</param>
    public void DeserializeStruct<T>(ReadOnlySpan<byte> source, ref T value, out bool isNull, ref int index, ValueField<T, Formatter> field)
      where T: struct 
    {
      InitDeserialization(source, index);
      field.Deserialize(source, ref value, out isNull);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes a value type from a byte buffer using the default
    /// <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <remarks>Passing the value type by reference avoids copy overhead.</remarks>
    /// <typeparam name="T">Value type to deserialize.</typeparam>
    /// <param name="value">Struct instance to deserialize.</param>
    /// <param name="isNull">Returns <c>true</c> if a <c>null</c> is deserialized,
    /// in which case the <c>value</c> parameter is ignored.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeStruct<T>(ReadOnlySpan<byte> source, ref T value, out bool isNull, ref int index)
      where T: struct 
    {
      ValueField<T, Formatter> field = (ValueField<T, Formatter>)GetField<T>();
      DeserializeStruct<T>(source, ref value, out isNull, ref index, field);
    }

    /// <summary>Deserializes a reference type from a byte buffer
    /// using a specific <see cref="Field{T, F}"/> instance.</summary>
    /// <typeparam name="T">Reference type to deserialize.</typeparam>
    /// <param name="obj">Object to deserialize, or <c>null</c> - in which
    /// case a new object will be instantiated.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    /// <param name="field"><see cref="ReferenceField{T, Formatter}"/> instance that
    /// deserializes the <c>buffer</c> contents.</param>
    public void DeserializeObject<T>(ReadOnlySpan<byte> source, ref T obj, ref int index, ReferenceField<T, Formatter> field)
      where T: class
    {
      InitDeserialization(source, index);
      field.Deserialize(source, ref obj);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes a reference type from a byte buffer using the
    /// default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Reference type to deserialize.</typeparam>
    /// <param name="obj">Object to deserialize, or <c>null</c> - in which
    /// case a new object will be instantiated.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeObject<T>(ReadOnlySpan<byte> source, ref T obj, ref int index)
      where T: class
    {
      ReferenceField<T, Formatter> field = (ReferenceField<T, Formatter>)GetField<T>();
      DeserializeObject<T>(source, ref obj, ref index, field);
    }

    /// <summary>Deserializes a reference type from a byte buffer using the
    /// default <see cref="Field{T, F}"/> instance registered for that type.</summary>
    /// <typeparam name="T">Reference type to deserialize.</typeparam>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    /// <returns>New object instance.</returns>
    public T DeserializeObject<T>(ReadOnlySpan<byte> source, ref int index)
      where T: class
    {
      T obj = null;
      DeserializeObject<T>(source, ref obj,  ref index);
      return obj;
    }

    #endregion Single Instances

    #region Value Type Arrays

    /// <overloads>Deserializes sequences (arrays, collections) of a given type
    /// using a specific <see cref="Field{T, F}"/> instance.</overloads>
    /// <summary>Deserializes value type arrays.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type array to deserialize.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeStructs<T>(ReadOnlySpan<byte> source, out T[] value, ValueField<T, Formatter> field, ref int index)
      where T: struct 
    {
      InitDeserialization(source, index);
      DeserializeStructs<T>(source, out value, field);
      index = FinishDeserialization();
    }

    /// <overloads>Deserializes sequences (arrays, collections) of a value type
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</overloads>
    /// <summary>Deserializes value type arrays.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeStructs<T>(ReadOnlySpan<byte> source, out T[] value, ref int index)
      where T: struct
    {
      InitDeserialization(source, index);
      DeserializeStructs<T>(source, out value);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes arrays of nullable value types.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeStructs<T>(ReadOnlySpan<byte> source, out T?[] value, ValueField<T, Formatter> field, ref int index)
      where T: struct
    {
      InitDeserialization(source, index);
      DeserializeStructs<T>(source, out value, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes arrays of nullable value types.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <param name="value">Value type to deserialize.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeStructs<T>(ReadOnlySpan<byte> source, out T?[] value, ref int index)
      where T: struct
    {
      InitDeserialization(source, index);
      DeserializeStructs<T>(source, out value);
      index = FinishDeserialization();
    }

    #endregion Value Type Arrays

    #region Value Type Collections

    /// <summary>Deserializes any kind of value type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <typeparam name="C">Type of collection.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="collection">Reference to collection. Can be null, in which case
    /// <c>initSequence()</c> must create a new instance for a non-null deserialization.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeStructs<T, C>(
      ReadOnlySpan<byte> source, 
      InitValueSequence<T, C> initSequence, 
      ref C collection, 
      ValueField<T, Formatter> field, 
      ref int index
    )
      where T: struct
      where C: class
    {
      InitDeserialization(source, index);
      DeserializeStructs<T, C>(source, initSequence, ref collection, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of value type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <typeparam name="C">Type of collection.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="collection">Reference to collection. Can be null, in which case
    /// <c>initSequence()</c> must create a new instance for a non-null deserialization.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeStructs<T, C>(
      ReadOnlySpan<byte> source, 
      InitValueSequence<T, C> initSequence, 
      ref C collection, 
      ref int index
    )
      where T: struct
      where C: class
    {
      InitDeserialization(source, index);
      DeserializeStructs<T, C>(source, initSequence, ref collection);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of nullable value type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <typeparam name="C">Type of collection.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="collection">Reference to collection. Can be null, in which case
    /// <c>initSequence()</c> must create a new instance for a non-null deserialization.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeStructs<T, C>(
      ReadOnlySpan<byte> source, 
      InitSequence<T?, C> initSequence,
      ref C collection,
      ValueField<T, Formatter> field,
      ref int index
    )
      where T: struct
      where C: class
    {
      InitDeserialization(source, index);
      DeserializeStructs<T, C>(source, initSequence, ref collection, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of nullable value type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be value type.</typeparam>
    /// <typeparam name="C">Type of collection.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="collection">Reference to collection. Can be null, in which case
    /// <c>initSequence()</c> must create a new instance for a non-null deserialization.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeStructs<T, C>(
      ReadOnlySpan<byte> source, 
      InitSequence<T?, C> initSequence,
      ref C collection,
      ref int index
    )
      where T: struct
      where C: class
    {
      InitDeserialization(source, index);
      DeserializeStructs<T, C>(source, initSequence, ref collection);
      index = FinishDeserialization();
    }

    #endregion Value Type Collections

    #region Reference Type Arrays

    /// <summary>Deserializes reference type arrays.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="obj">Reference type array to deserialize.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeObjects<T>(
      ReadOnlySpan<byte> source, 
      ref T[] obj,
      ReferenceField<T, Formatter> field,
      ref int index
    )
      where T: class 
    {
      InitDeserialization(source, index);
      DeserializeObjects<T>(source, ref obj, field);
      index = FinishDeserialization();
    }

    /// <overloads>Deserializes sequences (arrays, collections) of a refence type
    /// using the default <see cref="Field{T, F}"/> instance registered for that type.</overloads>
    /// <summary>Deserializes reference type arrays.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <param name="obj">Reference type array to deserialize.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeObjects<T>(ReadOnlySpan<byte> source, ref T[] obj, ref int index)
      where T: class
    {
      InitDeserialization(source, index);
      DeserializeObjects<T>(source, ref obj);
      index = FinishDeserialization();
    }

    #endregion Reference Type Arrays

    #region Reference Type Collections

    /// <summary>Deserializes any kind of reference type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <typeparam name="C">Type of collection.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="collection">Reference to collection. Can be null, in which case
    /// <c>initSequence()</c> must create a new instance for a non-null deserialization.</param>
    /// <param name="field">Field that deserializes the sequence elements.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeObjects<T, C>(
      ReadOnlySpan<byte> source, 
      InitSequence<T, C> initSequence,
      ref C collection,
      ReferenceField<T, Formatter> field,
      ref int index
    )
      where T: class
      where C: class
    {
      InitDeserialization(source, index);
      DeserializeObjects<T, C>(source, initSequence, ref collection, field);
      index = FinishDeserialization();
    }

    /// <summary>Deserializes any kind of reference type collection through call-backs.</summary>
    /// <typeparam name="T">Type of sequence elements - must be reference type.</typeparam>
    /// <typeparam name="C">Type of collection.</typeparam>
    /// <param name="initSequence">Call-back delegate to instantiate/initialize collection.
    /// Returns a delegate to add sequence elements to the collection.</param>
    /// <param name="collection">Reference to collection. Can be null, in which case
    /// <c>initSequence()</c> must create a new instance for a non-null deserialization.</param>
    /// <param name="source">Source buffer.</param>
    /// <param name="index">Start index in source buffer.</param>
    public void DeserializeObjects<T, C>(
      ReadOnlySpan<byte> source, 
      InitSequence<T, C> initSequence,
      ref C collection,
      ref int index
    )
      where T: class 
      where C: class
    {
      InitDeserialization(source, index);
      DeserializeObjects<T, C>(source, initSequence, ref collection);
      index = FinishDeserialization();
    }

    #endregion Reference Type Collections
#endif  // BUFFER_API

    #endregion Deserialization API
  }

  /// <summary><see cref="Formatter"/> subclass with default fields pre-registered
  /// for the basic types.</summary>
  public class StdFormatter: Formatter
  {
    static readonly BufferPool buffers = new BufferPool();

    void RegisterFieldConverters() {
#pragma warning disable RECS0026 // Possible unassigned object created by 'new'
      // basic value type fields
      new ByteField(this, true);
      new SByteField(this, true);
      new BoolField(this, true);
      new UShortField(this, true);
      new ShortField(this, true);
      new CharField(this, true);
      new UIntField(this, true);
      new IntField(this, true);
      new ULongField(this, true);
      new LongField(this, true);
      new SingleField(this, true);
      new DoubleField(this, true);
      new DecimalField(this, true);
      new DateTimeField(this, true);
      new TimeSpanField(this, true);
      new DateTimeOffsetField(this, true);
      // frequently used reference fields
      new BlobField(this, true);
      new StringField(this, true, buffers);
      new UShortArrayField(this, true);
      new ShortArrayField(this, true);
      new CharArrayField(this, true);
      new UIntArrayField(this, true);
      new IntArrayField(this, true);
#pragma warning restore RECS0026 // Possible unassigned object created by 'new'
    }

    /// <inheritdoc />
    public StdFormatter(ByteOrder byteOrder): base(byteOrder) {
      RegisterFieldConverters();
    }

    static void AddItem<T>(T item, List<T> list) {
      list.Add(item);
    }

    static void AddValueItem<T>(ref T item, bool isNull, List<T> list) where T: struct {
      list.Add(isNull ? default(T) : item);
    }

    static void AddValueItem<T>(ref T item, bool isNull, List<T?> list) where T: struct {
      list.Add(isNull ? (T?)null : item);
    }

    /// <summary>
    /// A <see cref="List{T}"/> based implementation of <see cref="InitSequence{T, C}"/>.
    /// </summary>
    /// <seealso cref="InitSequence{T, C}"/>
    public static AddItem<T, List<T>> InitList<T>(int size, ref List<T> collection) {
      if (size < 0)
        return null;
      if (collection == null)
        collection = new List<T>(size);
      else
        collection.Capacity += size;
      return AddItem<T>;
    }

    /// <summary>
    /// A <see cref="List{T}"/> based implementation of <see cref="InitValueSequence{T, C}"/>
    /// where null values will be deserialized as the value type's default value.
    /// </summary>
    /// <seealso cref="InitValueSequence{T, C}"/>
    public static AddValueItem<T, List<T>> InitValueList<T>(int capacity, ref List<T> collection) where T: struct {
      if (capacity < 0)
        return null;
      if (collection == null)
        collection = new List<T>(capacity);
      else
        collection.Capacity += capacity;
      return AddValueItem<T>;
    }

    /// <summary>
    /// A <see cref="List{T}"/> based implementation of <see cref="InitValueSequence{T, C}"/>
    /// where values will be deserialized as <see cref="Nullable{T}"/>.
    /// </summary>
    /// <seealso cref="InitValueSequence{T, C}"/>
    public static AddValueItem<T, List<T?>> InitNullableList<T>(int capacity, ref List<T?> collection) where T: struct {
      if (capacity < 0)
        return null;
      if (collection == null)
        collection = new List<T?>(capacity);
      else
        collection.Capacity += capacity;
      return AddValueItem<T>;
    }
  }
}