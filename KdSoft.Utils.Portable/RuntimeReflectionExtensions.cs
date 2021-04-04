namespace System.Reflection
{
  // Copy of RuntimeReflectionExtensions from .NET 4.5.
  // Does not work on Mono because System.RuntimeType and System.RuntimeMethodInfo do not exist there.
  // It seems System.MonoType takes the role of System.RuntimeType, but IsInstanceOfType(runtimeType) still does not work.
#if NET40 || NET403
    public static class RuntimeReflectionExtensions
    {
        private const BindingFlags everything = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static IEnumerable<PropertyInfo> GetRuntimeProperties(this Type type) {
            RuntimeReflectionExtensions.CheckAndThrow(type);
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static IEnumerable<EventInfo> GetRuntimeEvents(this Type type) {
            RuntimeReflectionExtensions.CheckAndThrow(type);
            return type.GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static IEnumerable<MethodInfo> GetRuntimeMethods(this Type type) {
            RuntimeReflectionExtensions.CheckAndThrow(type);
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static IEnumerable<FieldInfo> GetRuntimeFields(this Type type) {
            RuntimeReflectionExtensions.CheckAndThrow(type);
            return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static PropertyInfo GetRuntimeProperty(this Type type, string name) {
            RuntimeReflectionExtensions.CheckAndThrow(type);
            return type.GetProperty(name);
        }

        public static EventInfo GetRuntimeEvent(this Type type, string name) {
            RuntimeReflectionExtensions.CheckAndThrow(type);
            return type.GetEvent(name);
        }

        public static MethodInfo GetRuntimeMethod(this Type type, string name, Type[] parameters) {
            RuntimeReflectionExtensions.CheckAndThrow(type);
            return type.GetMethod(name, parameters);
        }

        public static FieldInfo GetRuntimeField(this Type type, string name) {
            RuntimeReflectionExtensions.CheckAndThrow(type);
            return type.GetField(name);
        }

        public static MethodInfo GetRuntimeBaseDefinition(this MethodInfo method) {
            RuntimeReflectionExtensions.CheckAndThrow(method);
            return method.GetBaseDefinition();
        }

        private static Type runtimeType = Type.GetType("System.RuntimeType");
        private static Type runtimeMethodInfo = Type.GetType("System.RuntimeMethodInfo");

        // Not portable
        //public static InterfaceMapping GetRuntimeInterfaceMap(this Type type, Type interfaceType)
        //{
        //    if (type == null)
        //    {
        //        throw new ArgumentNullException("type");
        //    }
        //    if (!(type.IsInstanceOfType(runtimeType)))
        //    {
        //        throw new ArgumentException("Argument Must Be RuntimeType");
        //    }
        //    return type.GetInterfaceMap(interfaceType);
        //}

        public static MethodInfo GetMethodInfo(this Delegate del) {
            if (del == null) {
                throw new ArgumentNullException("del");
            }
            return del.Method;
        }

        private static void CheckAndThrow(Type type) {
            if (type == null) {
                throw new ArgumentNullException("type");
            }
            if (!(type.IsInstanceOfType(runtimeType))) {
                throw new ArgumentException("Argument Must Be RuntimeType");
            }
        }

        private static void CheckAndThrow(MethodInfo method) {
            if (method == null) {
                throw new ArgumentNullException("method");
            }
            if (!method.GetType().IsInstanceOfType(runtimeMethodInfo)) {
                throw new ArgumentException("Argument Must Be RuntimeMethodInfo");
            }
        }
    }
#endif
}
