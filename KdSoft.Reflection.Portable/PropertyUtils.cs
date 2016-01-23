using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace KdSoft.Reflection
{
    public static class PropertyUtils
    {
        struct PropertyKey
        {
            public readonly Type ObjType;
            public readonly string Name;

            public PropertyKey(Type objType, string name) {
                this.ObjType = objType;
                this.Name = name;
            }

            public override bool Equals(Object obj) {
                return obj is PropertyKey && this == (PropertyKey)obj;
            }

            public override int GetHashCode() {
                if (Name == null)
                    return ObjType.GetHashCode();
                else
                    return ObjType.GetHashCode() ^ Name.GetHashCode();
            }

            public static bool operator ==(PropertyKey x, PropertyKey y) {
                return x.ObjType == y.ObjType && x.Name == y.Name;
            }

            public static bool operator !=(PropertyKey x, PropertyKey y) {
                return !(x == y);
            }

            public override String ToString() {
                if (Name == null)
                    return ObjType.ToString();
                else
                    return String.Format("({0}, {1})", ObjType, Name);
            }
        }

        public struct PropertyAccessor
        {
            public readonly string Name;
            public readonly Func<object, object> GetValue;
            public readonly Action<object, object> SetValue;

            public PropertyAccessor(string name, Func<object, object> getValue, Action<object, object> setValue) {
                this.Name = name;
                this.GetValue = getValue;
                this.SetValue = setValue;
            }
        }

        static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();
        static readonly Dictionary<PropertyKey, object> propertyAccessorCache = new Dictionary<PropertyKey, object>();

        public static IEnumerable<KeyValuePair<string, object>> GetPropertyValues(this object instance) {
            if (instance == null) {
                throw new ArgumentNullException("instance");
            }
            var accessors = GetPropertyAccessors(instance.GetType());
            return EnumeratePropertyValues(instance, accessors);
        }

        static IEnumerable<KeyValuePair<string, object>> EnumeratePropertyValues(object instance, PropertyAccessor[] accessors) {
            foreach (var accessor in accessors) {
                yield return new KeyValuePair<string, object>(accessor.Name, accessor.GetValue(instance));
            }
        }

        public static object GetPropertyValue(this object instance, string propertyName) {
            var accessor = GetPropertyAccessor(instance.GetType(), propertyName);
            if (accessor == null)
                throw new MissingMemberException(propertyName);
            if (accessor.Value.GetValue == null)
                throw new MemberAccessException(propertyName);
            return accessor.Value.GetValue(instance);
        }

        public static void SetPropertyValue(this object instance, string propertyName, object value) {
            var accessor = GetPropertyAccessor(instance.GetType(), propertyName);
            if (accessor == null)
                throw new MissingMemberException(propertyName);
            if (accessor.Value.SetValue == null)
                throw new MemberAccessException(propertyName);
            accessor.Value.SetValue(instance, value);
        }
        public static object GetStaticPropertyValue(this Type type, string propertyName) {
            var accessor = GetPropertyAccessor(type, propertyName);
            if (accessor == null)
                throw new MissingMemberException(propertyName);
            if (accessor.Value.GetValue == null)
                throw new MemberAccessException(propertyName);
            return accessor.Value.GetValue(null);
        }

        public static void SetStaticPropertyValue(this Type type, string propertyName, object value) {
            var accessor = GetPropertyAccessor(type, propertyName);
            if (accessor == null)
                throw new MissingMemberException(propertyName);
            if (accessor.Value.SetValue == null)
                throw new MemberAccessException(propertyName);
            accessor.Value.SetValue(null, value);
        }

        public static object GetPropertyPathValue(this object instance, string propertyPath) {
            int dotIndex = propertyPath.IndexOf('.');
            if (dotIndex < 0)
                return GetPropertyValue(instance, propertyPath);
            else {
                var propObject = GetPropertyValue(instance, propertyPath.Substring(0, dotIndex));
                var nextPropertyPath = propertyPath.Substring(dotIndex + 1);
                return GetPropertyPathValue(propObject, nextPropertyPath);
            }
        }


        public static PropertyAccessor[] GetPropertyAccessors(Type type) {
            if (type == null) {
                throw new ArgumentNullException("type");
            }
            PropertyAccessor[] result = null;
            object resultObj;
            var propTypeKey = new PropertyKey(type, null);

            rwLock.EnterReadLock();
            try {

                if (propertyAccessorCache.TryGetValue(propTypeKey, out resultObj)) {
                    result = (PropertyAccessor[])resultObj;
                }
            }
            finally {
                rwLock.ExitReadLock();
            }

            if (result == null) {
                rwLock.EnterUpgradeableReadLock();
                try {
                    if (propertyAccessorCache.TryGetValue(propTypeKey, out resultObj)) {
                        result = (PropertyAccessor[])resultObj;
                    }
                    else {
                        result = CreatePropertyAccessors(type);
                        rwLock.EnterWriteLock();
                        try {
                            propertyAccessorCache.Add(propTypeKey, result);
                            foreach (var accessor in result) {
                                var propKey = new PropertyKey(type, accessor.Name);
                                propertyAccessorCache.Add(propKey, accessor);
                            }
                        }
                        finally {
                            rwLock.ExitWriteLock();
                        }
                    }
                }
                finally {
                    rwLock.ExitUpgradeableReadLock();
                }
            }

            return result;
        }

        public static PropertyAccessor? GetPropertyAccessor(this Type type, string propertyName) {
            if (type == null) {
                throw new ArgumentNullException("type");
            }
            PropertyAccessor? result = null;
            object resultObj;

            rwLock.EnterReadLock();
            try {

                var propKey = new PropertyKey(type, propertyName);
                if (propertyAccessorCache.TryGetValue(propKey, out resultObj)) {
                    result = (PropertyAccessor)resultObj;
                }
            }
            finally {
                rwLock.ExitReadLock();
            }

            if (result == null) {
                var propTypeKey = new PropertyKey(type, null);
                rwLock.EnterUpgradeableReadLock();
                try {
                    // at this point wew need to know if the property even exists on the type;
                    // if the type was previously processed then we iterate over the property accessors
                    if (propertyAccessorCache.TryGetValue(propTypeKey, out resultObj)) {
                        var accessors = (PropertyAccessor[])resultObj;
                        foreach (var accessor in accessors) {
                            if (accessor.Name == propertyName) {
                                result = accessor;
                                break;
                            }
                        }
                    }
                    else {
                        var accessors = CreatePropertyAccessors(type);
                        rwLock.EnterWriteLock();
                        try {
                            propertyAccessorCache.Add(propTypeKey, accessors);
                            foreach (var accessor in accessors) {
                                var propKey = new PropertyKey(type, accessor.Name);
                                propertyAccessorCache.Add(propKey, accessor);
                                if (accessor.Name == propertyName)
                                    result = accessor;

                            }
                        }
                        finally {
                            rwLock.ExitWriteLock();
                        }
                    }
                }
                finally {
                    rwLock.ExitUpgradeableReadLock();
                }
            }

            return result;
        }

#if COREFX
        static MethodInfo GetterMethod(PropertyInfo propInfo) {
            return propInfo.GetMethod;
        }

        static MethodInfo SetterMethod(PropertyInfo propInfo) {
            return propInfo.SetMethod;
        }
#else
        static MethodInfo GetterMethod(PropertyInfo propInfo) {
            return propInfo.GetGetMethod();
        }

        static MethodInfo SetterMethod(PropertyInfo propInfo) {
            return propInfo.GetSetMethod();
        }
#endif

        static PropertyAccessor[] CreatePropertyAccessors(Type type) {
#if COREFX
            var props = type.GetRuntimeProperties();
#else
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
#endif
            var result = new List<PropertyAccessor>();

            foreach (var pi in props) {
                Func<object, object> propGetter = null;
                Action<object, object> propSetter = null;
                if (pi.CanRead) {
                    var mget = GetterMethod(pi);
                    if (mget.IsPublic && mget.GetParameters().Length == 0)
                        propGetter = CreatePropertyGetter(pi);
                }
                if (pi.CanWrite) {
                    var mset = SetterMethod(pi);
                    if (mset.IsPublic && mset.GetParameters().Length == 1)
                        propSetter = CreatePropertySetter(pi);
                }

                if (propGetter != null || propSetter != null) {
                    result.Add(new PropertyAccessor(pi.Name, propGetter, propSetter));
                }
            }

            return result.ToArray();
        }

        public static Func<object, object> CreatePropertyGetter(PropertyInfo prop, Type reflectedType = null) {
            var instance = Expression.Parameter(typeof(object), "i");
            var instCast = GetterMethod(prop).IsStatic ? null : Expression.Convert(instance, reflectedType ?? prop.DeclaringType);

            var propAcc = Expression.Property(instCast, prop);
            var castProp = Expression.Convert(propAcc, typeof(object));

            var lambda = Expression.Lambda<Func<object, object>>(castProp, instance);
            return lambda.Compile();
        }

        public static Func<T, object> CreateValueGetter<T>(this PropertyInfo propertyInfo) {
            if (typeof(T) != propertyInfo.DeclaringType) {
                throw new ArgumentException();
            }
            var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
            var instCast = GetterMethod(propertyInfo).IsStatic ? null : Expression.Convert(instance, propertyInfo.DeclaringType);

            var propAcc = Expression.Property(instCast, propertyInfo);
            var castProp = Expression.Convert(propAcc, typeof(object));

            var lambda = Expression.Lambda<Func<T, object>>(castProp, instance);
            return lambda.Compile();
        }

        public static Func<T, object> CreateValueGetter<T>(this FieldInfo fieldInfo) {
            var instance = Expression.Parameter(typeof(T), "i");
            var field = typeof(T) != fieldInfo.DeclaringType
                ? Expression.Field(Expression.Convert(instance, fieldInfo.DeclaringType), fieldInfo)
                : Expression.Field(instance, fieldInfo);

            var castField = Expression.Convert(field, typeof(object));
            return Expression.Lambda<Func<T, object>>(castField, instance).Compile();
        }


        public static Action<object, object> CreatePropertySetter(PropertyInfo prop, Type reflectedType = null) {
            var instance = Expression.Parameter(typeof(object), "i");
            var instCast = SetterMethod(prop).IsStatic ? null : Expression.Convert(instance, reflectedType ?? prop.DeclaringType);

            var argument = Expression.Parameter(typeof(object), "a");
            var castArg = Expression.Convert(argument, prop.PropertyType);

            var setterCall = Expression.Call(instCast, SetterMethod(prop), castArg);
            return Expression.Lambda<Action<object, object>>(setterCall, instance, argument).Compile();
        }


        public static Action<T, object> CreateValueSetter<T>(this PropertyInfo propertyInfo) {
            if (typeof(T) != propertyInfo.DeclaringType) {
                throw new ArgumentException();
            }
            var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");

            var argument = Expression.Parameter(typeof(object), "a");
            var castArg = Expression.Convert(argument, propertyInfo.PropertyType);

            var setterCall = Expression.Call(instance, SetterMethod(propertyInfo), castArg);
            return Expression.Lambda<Action<T, object>>(setterCall, instance, argument).Compile();
        }

        public static Action<T, object> CreateValueSetter<T>(this FieldInfo fieldInfo) {
            var instance = Expression.Parameter(typeof(T), "i");

            var argument = Expression.Parameter(typeof(object), "a");
            var castArg = Expression.Convert(argument, fieldInfo.FieldType);

            var field = typeof(T) != fieldInfo.DeclaringType
                ? Expression.Field(Expression.TypeAs(instance, fieldInfo.DeclaringType), fieldInfo)
                : Expression.Field(instance, fieldInfo);

            var setterCall = Expression.Assign(field, castArg);
            return Expression.Lambda<Action<T, object>>(setterCall, instance, argument).Compile();
        }

        public static IEnumerable<PropertyInfo> GetPublicGetSetProperties(this Type type) {
#if COREFX
            var props = type.GetRuntimeProperties();
#else
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
#endif

            foreach (PropertyInfo propInfo in props) {
                // must be readable and writable
                if (!propInfo.CanWrite || !propInfo.CanRead)
                    continue;

                // Get and set methods have to be public
                var mget = GetterMethod(propInfo);
                if (mget == null || !mget.IsPublic)
                    continue;
                var mset = GetterMethod(propInfo);
                if (mset == null || !mset.IsPublic)
                    continue;

                yield return propInfo;
            }
        }

        public static string[] GetPublicGetSetPropertyNames(this Type type) {
            return GetPublicGetSetProperties(type).Select(pi => pi.Name).ToArray();
        }
    }
}
