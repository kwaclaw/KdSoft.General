using System;
using KdSoft.Reflection;
using Xunit;

namespace KdSoft.Utils.Tests
{
  public class ReflectionTests
  {
    public ReflectionTests() {
    }

    public class PropAccess
    {
      public int IntProp { get; set; }
      public string StringProp { get; set; }

      public static int StaticInt { get; set; }

      public ChildClass Child { get; set; }
    }

    public class ChildClass
    {
      public int IntProp { get; set; }
      public string StringProp { get; set; }

      public static int StaticInt { get; set; }

      public GrandChildClass GrandChild { get; set; }
    }

    public class GrandChildClass
    {
      public int IntProp { get; set; }
      public string StringProp { get; set; }

      public static int StaticInt { get; set; }
    }

    [Fact]
    public void PropertyAccess() {
      var obj = new PropAccess();

      int intValue = 234;
      obj.SetPropertyValue("IntProp", intValue);
      var intReturn = obj.GetPropertyValue("IntProp");

      Assert.Equal(intValue, intReturn);
    }

    [Fact]
    public void NonExistingPropertyAccess() {
      var obj = new PropAccess();

      int intValue = 234;
      obj.IntProp = intValue;

      try {
        int intReturn = (int)obj.GetPropertyValue("IntProp2");
      }
      catch (MissingMemberException ex) {
        if (ex.Message != "IntProp2")
          throw;
      }
    }

    [Fact]
    public void StaticPropertyAccess() {
      var obj = new PropAccess();

      int intValue = 234;
      obj.SetPropertyValue("StaticInt", intValue);
      var intReturn = obj.GetPropertyValue("StaticInt");

      Assert.Equal(intValue, intReturn);

      // static property access without instance
      var stacc = typeof(PropAccess).GetPropertyAccessor("StaticInt");
      stacc.Value.SetValue(null, intValue);
      intReturn = stacc.Value.GetValue(null);

      Assert.Equal(intValue, intReturn);

      // static property access without instance, second approach
      typeof(PropAccess).SetStaticPropertyValue("StaticInt", intValue);
      intReturn = typeof(PropAccess).GetStaticPropertyValue("StaticInt");

      Assert.Equal(intValue, intReturn);
    }

    [Fact]
    public void PropertyPathAccess() {
      var obj = new PropAccess();

      obj.SetPropertyValue("Child", new ChildClass());
      var child = obj.GetPropertyValue("Child");

      child.SetPropertyValue("GrandChild", new GrandChildClass());
      var grandChild = child.GetPropertyValue("GrandChild");

      var strValue = "abakadabra";
      grandChild.SetPropertyValue("StringProp", strValue);

      var strReturn = obj.GetPropertyPathValue("Child.GrandChild.StringProp");

      Assert.Equal(strValue, strReturn);
    }

  }
}
