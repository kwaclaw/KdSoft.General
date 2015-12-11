using KdSoft.Reflection;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace KdSoft.Config
{
  /// <summary>
  /// Contains helper functions for simple (dynamic) compilation of source files.
  /// </summary>
  /// <remarks>This can be useful when using configuration through C# code instead of XML.</remarks>
  [SecuritySafeCritical, PermissionSet(SecurityAction.Assert, Unrestricted = true)]
  public static class Compile
  {
    static List<string> ParseReferences(string sourceFile) {
      List<string> result = new List<string>();
      using (StreamReader sr = new StreamReader(sourceFile)) {
        const string refStart = @"//ref ";
        string line;
        while ((line = sr.ReadLine()) != null) {
          line = line.Trim();
          if (line == String.Empty)  // skip empty lines
            continue;
          if (line.StartsWith(refStart, StringComparison.OrdinalIgnoreCase))
            result.Add(line.Substring(refStart.Length));
          else  // first line not matching indicates end of 'references' section
            break;
        }
      }
      return result;
    }

    /// <summary>
    /// Parses source files for occurrences of custom formatted comments that list referenced assemblies.
    /// </summary>
    /// <param name="sourceFiles">Source file names to parse.</param>
    /// <returns>Set of file names for referenced assemblies - duplicates removed.</returns>
    /// <remarks>A 'references' section must be at the beginning of the file and each line must start
    /// with '//ref ' followed by the file name for a referenced assembly. The file name does not have
    /// to be an absolute path, as long as the compiler can find it from its current directory.
    /// A typical 'references' section looks like this:
    /// <code>
    /// //ref System.ServiceProcess.dll
    /// //ref KdSoft.ServiceInterface.dll
    /// //ref KdSoft.ServiceHost.exe
    /// //ref KdSoft.WindowsLogon.dll
    /// //ref KdSoft.ServiceHostIce.dll
    /// </code></remarks>
    public static HashSet<string> ParseReferences(params string[] sourceFiles) {
      HashSet<string> refs = new HashSet<string>();
      foreach (string sf in sourceFiles) {
        List<string> fileRefs = ParseReferences(sf);
        foreach (string fr in fileRefs)
          refs.Add(fr);
      }
      return refs;
    }

    /// <summary>
    /// Determines if a given string can be an assembly display name.
    /// </summary>
    /// <param name="assemblyName">String to check.</param>
    /// <returns>An <see cref="AssemblyName"/> reference if the argument is valid display name, 
    /// <c>null</c> otherwise.</returns>
    /// <remarks>We require that the supplied string is a valid argument to the <see cref="AssemblyName"/>
    /// constructor, and that it also does not contain a file extension.</remarks>
    public static AssemblyName CheckDisplayName(string assemblyName) {
      try {
        return new AssemblyName(assemblyName);
      }
      catch {
        return null;
      }
    }

    /// <summary>
    /// Maps a list of assembly file names to assemblies already loaded into an AppDomain.
    /// </summary>
    /// <param name="domain">AppDomain to search for loaded assemblies.</param>
    /// <param name="references">List of assembly file or display names to match.</param>
    /// <returns>List of assembly file or display names. Those display names that could be mapped
    /// to a loaded assembly are converted to that assembly's location (file path).</returns>
    public static List<string> MapLoadedReferences(AppDomain domain, IEnumerable<string> references) {
      List<string> result = new List<string>();
      foreach (string reference in references) {
        string refNoExtension = reference;
        if (reference.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || 
            reference.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
          refNoExtension = Path.GetFileNameWithoutExtension(reference);
        AssemblyName refName = CheckDisplayName(refNoExtension);
        if (refName != null) {
          Assembly refAssembly = domain.FindAssembly(refName);
          if (refAssembly != null) {
            string resolvedRef = refAssembly.Location;
            if (!string.IsNullOrEmpty(resolvedRef)) {
              result.Add(resolvedRef);
              continue;
            }
          }
        }
        result.Add(reference);
      }
      return result;
    }

    /// <summary>
    /// Compiles given C# files into an assembly loaded into the current AppDomain.
    /// </summary>
    /// <param name="baseDir">Base directory where to look for source files and referenced assemblies.</param>
    /// <param name="assemblyFile">File name for generated assembly. 
    /// If null or empty, an in-memory assembly will be generated.</param>
    /// <param name="references">Assemblies that must be referenced.</param>
    /// <param name="sourceFiles">List of C# files to compile.</param>
    /// <returns>New assembly.</returns>
    public static Assembly CompileLibrary(string baseDir, string assemblyFile, IEnumerable<string> references, params string[] sourceFiles) {
      using (CodeDomProvider compiler = new CSharpCodeProvider()) {
        CompilerParameters pars = new CompilerParameters();
        foreach (string reference in references)
          pars.ReferencedAssemblies.Add(reference);
        pars.GenerateExecutable = false;
        pars.GenerateInMemory = string.IsNullOrEmpty(assemblyFile);
        if (!pars.GenerateInMemory)
          pars.OutputAssembly = assemblyFile;
        pars.IncludeDebugInformation = false;
        string curDir = Directory.GetCurrentDirectory();
        try {
          Directory.SetCurrentDirectory(baseDir);
          CompilerResults compileResults = compiler.CompileAssemblyFromFile(pars, sourceFiles);
          if (compileResults.Errors.HasErrors) {
            string nl = Environment.NewLine;
            string errMsg = "Error compiling:" + nl;
            for (int indx = 0; indx < compileResults.Errors.Count; indx++) {
              CompilerError err = compileResults.Errors[indx];
              errMsg += "  File " + err.FileName + " at line " + err.Line + ": " + nl + "    " + err.ErrorText;
            }
            throw new ArgumentException(errMsg);
          }
          return compileResults.CompiledAssembly;
        }
        finally {
          Directory.SetCurrentDirectory(curDir);
        }
      }
    }
  }
}
