using System;
using System.Drawing;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;
using TunnelBuilder.Models;
using System.Xml.Serialization;
using System.IO;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("CF972E53-CCD5-4A61-9D0A-7A60E1DE5223")]
    public class BatchInstallBoltCommand : Command
    {
        ///<summary>The only instance of this command.</summary>
        public static BatchInstallBoltCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "BatchInstallBolt"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            var fd = new Rhino.UI.OpenFileDialog { Filter = "XML Files (*.xml)|*.xml", Title = "Open Tunnel Support Definition File", MultiSelect = false,DefaultExt="xml" };
            if(!fd.ShowOpenDialog())
            {
                return Result.Cancel;
            }
            var fn = fd.FileName;
            if (fn == string.Empty || !System.IO.File.Exists(fn))
            {
                return Result.Cancel;
            }


            TunnelSupportType tst;
            XmlSerializer tstSerializer = new XmlSerializer(typeof(TunnelSupportType));
            FileStream tstFileStream = new FileStream(fn, FileMode.Open);
            tst = (TunnelSupportType)tstSerializer.Deserialize(tstFileStream);
            RhinoApp.WriteLine("Applying Tunnel Support Definition created on " + tst.CreateDate.ToShortDateString());
            return Result.Success;
        }
    }
}
