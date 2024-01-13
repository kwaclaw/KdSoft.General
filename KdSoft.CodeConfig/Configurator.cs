#if NET462_OR_GREATER
using System;
#endif

namespace KdSoft.Config
{
    /// <summary>
    /// Interface that allows configuration through code (dynamic compilation).
    /// </summary>
    /// <typeparam name="T">Type of object to be configure.</typeparam>
    public interface IConfigurator<T>
  {
    /// <summary>Configures an existing object.</summary>
    /// <param name="obj"></param>
    void Configure(T obj);

    /// <summary>Creates a new and configured object.</summary>
    /// <param name="args">Some initial string arguments (e.g. from command line) can be supplied.</param>
    /// <returns>Properly configure instance.</returns>
    T CreateConfigured(ref string[] args);
  }

#if NET462_OR_GREATER
  /// <summary>
  /// This class just exists to be able to load this assembly across
  /// AppDomains using <see cref="Activator.CreateInstanceFrom(AppDomain, string, string)"/>.
  /// </summary>
  public class Configurator: MarshalByRefObject { }
#endif
}
