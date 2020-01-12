using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;

using Firebase.Database;
using Firebase.Database.Query;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("7B2260B5-C0C7-446F-91B2-365DFE928385"),
        Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)]
    public class ConnectToGriddle:Command
    {
        public override string EnglishName
        {
            get { return "ConnectToGriddle"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();

            process.StartInfo.FileName = "C:\\Program Files\\USB over Network\\usbclncmd.exe";
            process.StartInfo.Arguments = "list -a";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            String strOutput = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            List<String> lines = strOutput.Split('\n').ToList();

            int srvID = -1;
            int devID = -1;
            bool griddleIsConnected = false;

            for(int i=0;i<lines.Count;i++)
            {
                string l = lines[i].Trim();
                int srvIDIndex = l.IndexOf("srvID:");
                if(srvIDIndex > -1)
                {
                    srvID = getID(l);
                }

                int devIsGriddle = l.IndexOf("Griddle",StringComparison.CurrentCultureIgnoreCase);
                int devIsShared = l.IndexOf("Shared");
                int devIsConnected = l.IndexOf("Connected");
                if (devIsGriddle>-1 && devIsShared>-1)
                {
                    devID = getID(l);
                    break;
                }

                if(devIsGriddle>-1 && devIsConnected>-1)
                {
                    devID = getID(l);
                    griddleIsConnected = true;
                    break;
                }
            }

            var licenseUser = new LicenseUser(Environment.UserName, "Griddle");
            
            if (srvID>-1 && devID>-1 && !griddleIsConnected)
            {
                System.Diagnostics.Process connectionProcess = new System.Diagnostics.Process();

                connectionProcess.StartInfo.FileName = "C:\\Program Files\\USB over Network\\usbclncmd.exe";
                connectionProcess.StartInfo.Arguments = String.Format("connect {0} {1}",srvID,devID);
                connectionProcess.StartInfo.UseShellExecute = false;
                connectionProcess.StartInfo.RedirectStandardOutput = true;

                connectionProcess.Start();
                String connectionOutput = connectionProcess.StandardOutput.ReadToEnd();

                connectionProcess.WaitForExit();
                if(connectionOutput.IndexOf("Device is connected")>-1)
                {
                    griddleIsConnected = true;
                    licenseUser.ConnectedToLicense();
                }
            }

            if(griddleIsConnected)
            {
                RhinoApp.RunScript("SetWorkingDirectory", true);
                RhinoApp.RunScript("!_BR _EnterEnd", true);
                RhinoApp.RunScript("!_GInt _EnterEnd", true);
                RhinoApp.RunScript("!_GSurf _EnterEnd", true);
                RhinoApp.RunScript("!_GVol _EnterEnd", true);
                RhinoApp.RunScript("!_G_NMExtract _EnterEnd", true);

                System.Diagnostics.Process disconnectProcess = new System.Diagnostics.Process();

                disconnectProcess.StartInfo.FileName = "C:\\Program Files\\USB over Network\\usbclncmd.exe";
                disconnectProcess.StartInfo.Arguments = String.Format("disconnect {0} {1}", srvID, devID);
                disconnectProcess.StartInfo.UseShellExecute = false;
                disconnectProcess.StartInfo.RedirectStandardOutput = true;

                disconnectProcess.Start();
                String disconnectOutput = disconnectProcess.StandardOutput.ReadToEnd();

                disconnectProcess.WaitForExit();
                return Result.Success;
            }

            RhinoApp.WriteLine("Cannot connect to Griddle License");

            return Result.Failure;
        }

        private int getID(string line)
        {
            string[] digits = Regex.Split(line, @"\D+");
            int number;
            foreach(string value in digits)
            {
                if (value.Length>0)
                {
                    int.TryParse(value, out number);
                    return number;
                }
            }
            return -1;
        }

    }
    public class LicenseUser
    {
        public string Name { get; set; }
        public string Software { get; set; }

        public LicenseUser(string name,string software)
        {
            Name = name;
            Software = software;
        }

        public void ConnectedToLicense()
        {
            var firebase = new FirebaseClient("https://license-users.firebaseio.com/");
            try
            {
                firebase.Child(Software).PutAsync(Name).Wait();
            }
            catch
            {

            }
            
            return;
        }
    }
}
