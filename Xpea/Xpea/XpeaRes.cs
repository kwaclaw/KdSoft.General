/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Resources;
using System.Reflection;

namespace Org.System.Xml.Xpea
{
  /// <summary>Identifies localized string constants.</summary>
  public enum RsId
  {
    // for Org.System.Xml.Xpea namespace
    DefErrorMsg,
    PathEmpty
  }

  /// <summary>Enables access to localized resources.</summary>
  public class Resources
  {
    private static ResourceManager rm;

    private Resources() { }

    /// <summary>Returns localized string constants.</summary>
    public static string GetString(RsId id) {
      // do not use RsId.ToString() - it does not have a fixed output format
      string name = Enum.GetName(typeof(RsId), id);
      return rm.GetString(name);
    }

    static Resources() {
      rm = new ResourceManager("Org.System.Xml.Xpea.Xpea", Assembly.GetExecutingAssembly());
    }
  }
}
