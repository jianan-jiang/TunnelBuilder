using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using SuperXML;

namespace ReleaseHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            string target = args[0];

            string path = Path.IsPathRooted(target)
                                ? target
                                : Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + Path.DirectorySeparatorChar + target;

            

            var template = new AutoUpdaterTemplate();
            template.Version = Assembly.LoadFile(path).GetName().Version.ToString(4);
            template.InstallerPath = "W:\\Technical\\Software_Engineering\\Rhino\\Rhino 6\\Plugins\\TunnelBuilder\\TunnelBuilder\\bin\\Release\\TunnelBuilder.rhi";
            template.ChangeLogPath = "https://jianan-jiang.github.io/TunnelBuilder/Change_Log.html";
            template.Mandatory = "false";

            var complier = new Compiler();
            complier.AddKey("Version", template.Version);
            complier.AddKey("InstallerPath", template.InstallerPath);
            complier.AddKey("ChangeLogPath", template.ChangeLogPath);
            complier.AddKey("Mandatory", template.Mandatory);

            var compliedXML = complier.CompileXml(args[1]);
            System.IO.StreamWriter file =new System.IO.StreamWriter(args[2]);
            file.Write(compliedXML);
            file.Close();

        }
    }

    class AutoUpdaterTemplate
    {
        public string Version;
        public string InstallerPath;
        public string ChangeLogPath;
        public string Mandatory;
    }
}
