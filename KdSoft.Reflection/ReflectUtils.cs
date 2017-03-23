
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if !COREFX
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
#endif

namespace KdSoft.Reflection
{
    /// <summary>
    /// Reflection helper routines.
    /// </summary>
#if !COREFX
    [SecuritySafeCritical, PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
    public static class ReflectUtils
    {
        /// <summary>
        /// Extracts the simple assembly name from an assembly's display name.
        /// </summary>
        /// <param name="assemblyDisplayName">Assembly display name to extract from.</param>
        /// <returns>Extracted simple name.</returns>
        public static string ExtractSimpleName(string assemblyDisplayName) {
            if (string.IsNullOrEmpty(assemblyDisplayName))
                return assemblyDisplayName;
            int commaIndx = assemblyDisplayName.IndexOf(',');
            if (commaIndx < 0)
                return assemblyDisplayName;
            else
                return assemblyDisplayName.Substring(0, commaIndx);
        }

        /// <summary>
        /// Finds assemblies in the <see cref="DependencyContext"/> of an assmebly, given an assembly "simple name".
        /// The comparison is not case-sensitive.
        /// </summary>
        /// <param name="deps">DependencyContext to search.</param>
        /// <param name="matchName">Simple name of the assembly, to match against.</param>
        /// <returns>List of <see cref="AssemblyName"/> instances.</returns>
        public static IList<AssemblyName> FindAssemblies(this DependencyContext deps, string matchName) {
            var result = new List<AssemblyName>();
            foreach (var lib in deps.RuntimeLibraries) {
                foreach (var runtimeLib in lib.Assemblies) {
                    if (string.Compare(runtimeLib.Name.Name, matchName, StringComparison.OrdinalIgnoreCase) == 0) {
                        result.Add(runtimeLib.Name);
                    }
                }
            }
            return result;
        }

#if !COREFX
        /// <summary>
        /// Finds assembly loaded into AppDomain given a full or partial assembly name.
        /// </summary>
        /// <param name="domain">AppDomain to search.</param>
        /// <param name="matchName">Name to match against loaded assemblies.</param>
        /// <param name="reflectionOnly"><c>true</c> if assemblies loaded into the ReflectionOnly context should be returned,
        /// <c>false</c> if assemblies loaded into the Execution context should be returned.</param>
        /// <returns>Matching assembly or <c>null</c>, if not found.</returns>
        public static Assembly FindAssembly(this AppDomain domain, AssemblyName matchName, bool reflectionOnly) {
            Assembly[] assemblies;
            if (reflectionOnly)
                assemblies = domain.ReflectionOnlyGetAssemblies();
            else
                assemblies = domain.GetAssemblies();
            Assembly result = null;
            foreach (Assembly assembly in assemblies) {
                if (AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), matchName)) {
                    result = assembly;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Finds assembly loaded into AppDomain's Execution context given a full or partial assembly name.
        /// </summary>
        /// <param name="domain">AppDomain to search.</param>
        /// <param name="matchName">Name to match against loaded assemblies.</param>
        /// <returns>Matching assembly or <c>null</c>, if not found.</returns>
        public static Assembly FindAssembly(this AppDomain domain, AssemblyName matchName) {
            return FindAssembly(domain, matchName, false);
        }

        /// <summary>
        /// Loads assembly into reflection-only context from display name, and as fall-back uses an optional base Uri to construct a CodeBase.
        /// </summary>
        /// <param name="name">Assembly display name.</param>
        /// <param name="baseUri">Base Uri for resolving assembly code base. Can be <c>null</c> or empty.</param>
        /// <returns>Assembly loaded into reflection-only context</returns>
        /// <remarks>Gives preference to Codebase, but will try loading by display name if loading from CodeBase fails.
        /// Should not be used when a Codebase for the assembly is already available.</remarks>
        public static Assembly ReflectionOnlyLoad(string name, string baseUri) {
            if (string.IsNullOrEmpty(baseUri))
                return Assembly.ReflectionOnlyLoad(name);
            Uri loadUri;
            if (Uri.TryCreate(baseUri, UriKind.Absolute, out loadUri)) {
                string fileName = ExtractSimpleName(name) + ".dll";
                Uri codeBaseUri;
                try {
                    if (Uri.TryCreate(loadUri, fileName, out codeBaseUri))
                        return Assembly.ReflectionOnlyLoadFrom(codeBaseUri.ToString());
                    else
                        return Assembly.ReflectionOnlyLoadFrom(fileName);
                }
                catch (FileNotFoundException) {
                    return Assembly.ReflectionOnlyLoad(name);
                }
                catch (FileLoadException) {
                    return Assembly.ReflectionOnlyLoad(name);
                }
            }
            return Assembly.ReflectionOnlyLoad(name);
        }
#endif

        /// <summary>
        /// Finds all classes that derive from a given class or implement a given interface.
        /// </summary>
        /// <param name="assembly">Assembly to search.</param>
        /// <param name="superType">Super type to search for in classes.</param>
        /// <param name="publicOnly">Indicates if only public types should be searched.</param>
        /// <returns>List of classes that derive from the given class or that implement the given interface.</returns>
        public static List<Type> FindSubClasses(this Assembly assembly, Type superType, bool publicOnly) {
            List<Type> result = new List<Type>();
            Type[] types;
            if (publicOnly)
                types = assembly.GetExportedTypes();
            else
                types = assembly.GetTypes();
            foreach (Type tp in types) {
#if COREFX
                if (tp.GetTypeInfo().IsClass && superType.IsAssignableFrom(tp))
#else
                    if (tp.IsClass && superType.IsAssignableFrom(tp))
#endif
                    result.Add(tp);
            }
            return result;
        }

        /// <summary>
        /// Returns type objects for a given set of object instances. If an instance reference is <c>null</c>,
        /// the type <see cref="object"/> is assumed.
        /// </summary>
        /// <param name="objs">Object instances to return types for. Can be <c>null</c>.</param>
        /// <returns>Array of <see cref="Type"/> instances, which can be empty, but will not be <c>null</c>.</returns>
        public static Type[] GetTypes(params object[] objs) {
            if (objs == null)
                return new Type[] { typeof(object) };
            Type[] result = new Type[objs.Length];
            for (int indx = 0; indx < result.Length; indx++) {
                object obj = objs[indx];
                if (obj != null)
                    result[indx] = obj.GetType();
                else
                    result[indx] = typeof(object);
            }
            return result;
        }

        /// <summary>
        /// Returns the singleton instance for a given class type, if it exists.
        /// The instance must be exposed through a public static property or field with the specified name.
        /// </summary>
        /// <param name="type">Type to get singleton instance for.</param>
        /// <param name="name">Name for public static property or field. String comparison is *not* case sensitive.</param>
        /// <returns>Singleton instance, or <c>null</c> if no matching property or field is found.</returns>
#if COREFX
        public static object GetSingletonInstance(this Type type, string name) {
            if (!type.GetTypeInfo().IsClass)
                throw new ArgumentException(string.Format("Singleton type '{0}' must be class.", type.FullName));
            BindingFlags bfs = BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public;
            PropertyInfo propInfo = type.GetProperty(name, bfs);
            if (propInfo != null)
                return propInfo.GetValue(null, null);
            FieldInfo fldInfo = type.GetField(name, bfs);
            if (fldInfo != null && fldInfo.FieldType == type)
                return fldInfo.GetValue(null);
            return null;
        }
#else
        public static object GetSingletonInstance(this Type type, string name) {
            if (!type.IsClass)
                throw new ArgumentException(string.Format("Singleton type '{0}' must be class.", type.FullName));
            BindingFlags bfs = BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public;
            PropertyInfo propInfo = type.GetProperty(name, bfs, null, type, Type.EmptyTypes, null);
            if (propInfo != null)
                return propInfo.GetValue(null, null);
            FieldInfo fldInfo = type.GetField(name, bfs);
            if (fldInfo != null && fldInfo.FieldType == type)
                return fldInfo.GetValue(null);
            return null;
        }
#endif

#if !COREFX
        /// <summary>
        /// Gets all strong names for an assembly and its referenced assemblies and adds them
        /// to an existing array. Does not include assemblies not loaded (yet), unless <c>load</c> is true;
        /// </summary>
        /// <param name="assembly">Strong named assembly. *Not* checked if really strong named!</param>
        /// <param name="load">If <c>true</c> forces loading of assembly.</param>
        /// <param name="strongNames">Array of strong names to which to append the result.</param>
        public static void AddStrongNames(this Assembly assembly, bool load, HashSet<StrongName> strongNames) {
            AssemblyName[] refAssemblies = assembly.GetReferencedAssemblies();
            Assembly[] assemblies = new Assembly[refAssemblies.Length + 1];
            int assIndx = 0;
            for (int indx = 0; indx < refAssemblies.Length; indx++) {
                AssemblyName assName = refAssemblies[indx];
                Assembly ass = AppDomain.CurrentDomain.FindAssembly(assName);
                if (ass == null && load)
                    ass = Assembly.Load(assName);
                if (ass != null)
                    assemblies[assIndx++] = ass;
            }
            assemblies[assIndx++] = assembly;  // make it the last one
            for (int indx = 0; indx < assIndx; indx++) {
                StrongName strongName = assemblies[indx].Evidence.GetHostEvidence<StrongName>();
                strongNames.Add(strongName);
            }
        }
#endif
    }

#if !COREFX
    /// <summary>
    /// This class just exists to be able to load this assembly across
    /// AppDomains using <see cref="Activator.CreateInstanceFrom(AppDomain, string, string)"/>.
    /// </summary>
    public class Reflection: MarshalByRefObject { }
#endif
}
