using KdSoft.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace KdSoft.Config
{
  /// <summary>
  /// Configurator helper routines.
  /// </summary>
  [SecuritySafeCritical, PermissionSet(SecurityAction.Assert, Unrestricted = true)]
  public static class ConfigUtil
  {
    /// <summary>
    /// Instantiates a configurator class for the given class type <c>T</c>. Looks for
    /// the first class in the given assembly that implements <see cref="IConfigurator{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type for which to load configurator - parameter for <see cref="IConfigurator{T}"/>.</typeparam>
    /// <param name="configAssembly">Assembly to load configurator from.</param>
    /// <returns>Configurator instance as <see cref="IConfigurator{T}"/> interface.</returns>
    public static IConfigurator<T> GetConfigurator<T>(Assembly configAssembly) where T: class {
      Type confType = typeof(IConfigurator<T>);
      List<Type> cts = configAssembly.FindSubClasses(confType, true);
      if (cts.Count == 0)
        throw new ArgumentException("No configurator found in assembly '" + configAssembly.FullName + "'.");
      return (IConfigurator<T>)configAssembly.CreateInstance(cts[0].FullName);
    }

    /// <summary>
    /// Instantiates a configurator class in the specified <see cref="AppDomain"/> for the given class type <c>T</c>.
    /// Looks for the first class in the given assembly that implements <see cref="IConfigurator{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type for which to load configurator - parameter for <see cref="IConfigurator{T}"/>.</typeparam>
    /// <param name="domain"><see cref="AppDomain"/> into which to load the assembly.</param>
    /// <param name="assemblyName">Name for assembly to load.</param>
    /// <returns>Configurator instance as <see cref="IConfigurator{T}"/> interface.</returns>
    /// <remarks>If the application domain is not the current domain, then the configurator class should be
    /// accessible across domains, e.g. by being serializable, or by deriving from <see cref="MarshalByRefObject"/>.</remarks>
    [SecurityCritical]
    public static IConfigurator<T> GetConfigurator<T>(AppDomain domain, AssemblyName assemblyName) where T: class {
      Assembly configAssembly = domain.FindAssembly(assemblyName);
      if (configAssembly == null)
        configAssembly = domain.Load(assemblyName);
      return GetConfigurator<T>(configAssembly);
    }

    /// <summary>
    /// Compiles a given source file (C#) into an assembly and instantiates a configurator class in
    /// that assembly for type T. Looks for the first class that implements <see cref="IConfigurator{T}"/>.
    /// </summary>
    /// <param name="baseDir">Base directory where source and referenced files are located.</param>
    /// <param name="configFileCs">Source file for generating assembly that contains the configurator class.</param>
    /// <param name="outFileDll">File name for assembly to be generated. Can be null or empty.</param>
    /// <param name="deleteCs">Indicates if source file should be deleted after successful compilation.
    ///    If <c>false</c>, then file will be renamed by adding the extension ".loaded".</param>
    /// <returns>Configurator instance as <see cref="IConfigurator{T}"/> interface, or <c>null</c> if source file cannot be found.</returns>
    [SecurityCritical]
    public static IConfigurator<T> GetConfigurator<T>(string baseDir, string configFileCs, string outFileDll, bool deleteCs) where T: class {
      string configSource = Path.Combine(baseDir, configFileCs);
      if (!File.Exists(configSource))
        return null;
      HashSet<string> references = Compile.ParseReferences(configSource);
      List<string> mappedRefs = Compile.MapLoadedReferences(AppDomain.CurrentDomain, references);
      Assembly configAssembly = Compile.CompileLibrary(baseDir, outFileDll, mappedRefs, configFileCs);
      if (deleteCs)
        File.Delete(configSource);
      else {
        string configSourceLoaded = configSource + ".loaded";
        if (File.Exists(configSourceLoaded))
          File.Delete(configSourceLoaded);
        File.Move(configSource, configSourceLoaded);
      }
      return GetConfigurator<T>(configAssembly);
    }

    /// <summary>
    /// Supports configuration update and re-compile logic (in current AppDomain) by recompiling
    /// the configuration assembly if a matching source file is present (optional deletion of source file).
    /// If no source file is found, an already existing configuration assembly will be loaded.
    /// If no configuration assembly can be found, the behaviour depends on the 'throwIfMissing' parameter.
    /// </summary>
    /// <typeparam name="T">Type to configure.</typeparam>
    /// <param name="assemblyName">Configuration assembly simple name. No extension!</param>
    /// <param name="throwIfMissing">Indicates if a missing configuration assembly should trigger an exception,
    /// or if the method should simply return <c>null</c>.</param>
    /// <param name="deleteCs">Indicates if the configuration source file (if present) should be deleted.</param>
    /// <returns>Configurator instance as <see cref="IConfigurator{T}"/> interface, or <c>null</c> if no
    /// configuration assembly was found.</returns>
    public static IConfigurator<T> CheckConfigurator<T>(string assemblyName, bool throwIfMissing, bool deleteCs) where T: class {
      IConfigurator<T> result;
      string baseDir = AppDomain.CurrentDomain.BaseDirectory;
      result = GetConfigurator<T>(baseDir, assemblyName + ".cs", assemblyName + ".dll", deleteCs);
      if (result == null) {
        try {
          result = GetConfigurator<T>(AppDomain.CurrentDomain, new AssemblyName(assemblyName));
        }
        catch (FileNotFoundException) {
          if (throwIfMissing)
            throw;
          else
            result = null;
        }
      }
      return result;
    }
  }
}
