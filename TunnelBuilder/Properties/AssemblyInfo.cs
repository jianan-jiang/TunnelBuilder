using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rhino.PlugIns;

// Plug-in Description Attributes - all of these are optional.
// These will show in Rhino's option dialog, in the tab Plug-ins.
[assembly: PlugInDescription(DescriptionType.Address, "G3 56 Delhi Road North Ryde NSW")]
[assembly: PlugInDescription(DescriptionType.Country, "Australia")]
[assembly: PlugInDescription(DescriptionType.Email, "jianan.jiang@psm.com.au")]
[assembly: PlugInDescription(DescriptionType.Phone, "+61 2 98125000")]
[assembly: PlugInDescription(DescriptionType.Fax, "+61 2 98125001")]
[assembly: PlugInDescription(DescriptionType.Organization, "Pells Sullivan Meynink")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "https://github.com/jianan-jiang/TunnelBuilder")]
[assembly: PlugInDescription(DescriptionType.WebSite, "www.psm.com.au")]

// Icons should be Windows .ico files and contain 32-bit images in the following sizes: 16, 24, 32, 48, and 256.
// This is a Rhino 6-only description.
[assembly: PlugInDescription(DescriptionType.Icon, "TunnelBuilder.EmbeddedResources.plugin-utility.ico")]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("TunnelBuilder")]

// This will be used also for the plug-in description.
[assembly: AssemblyDescription("TunnelBuilder plug-in, please attach the rhino file if you want to report a bug")]

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Pells Sullivan Meynink")]
[assembly: AssemblyProduct("TunnelBuilder")]
[assembly: AssemblyCopyright("Copyright ©  2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("470cfdd4-ab90-4775-acd0-8244c66f22a0")] // This will also be the Guid of the Rhino plug-in

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

[assembly: AssemblyVersion("2.7.0.0")]
[assembly: AssemblyFileVersion("2.7.0.0")]


// Make compatible with Rhino Installer Engine
[assembly: AssemblyInformationalVersion("2")]
