using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using JetBrains.Application.PluginSupport;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ReshSettingsDiscover")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ReshSettingsDiscover")]
[assembly: AssemblyCopyright("Copyright ©  2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f4ece711-9e77-4e0c-8763-4fc0c8094bf2")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly:PluginTitle("ReSharper Solution Settings Autodiscovery")]
[assembly:PluginDescription("When you open a solution in Visual Studio, this plugin looks for any *.AutoLoad.DotSettings files in parent folders and loads them as ReSharper settings layers. This allows to apply a root settings file to all solutions in the same source control.")]
[assembly:PluginVendor("hypersw")]