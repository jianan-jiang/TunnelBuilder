using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;

namespace TunnelBuilder
{
    [
    System.Runtime.InteropServices.Guid("424EC2A9-67A3-4F5B-B0BF-99C9017B6952"),
     Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)
    ]
    public class ExportCrossSection:Command
    {
        public override string EnglishName
        {
            get { return "ExportCrossSection"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.RunScript("CPlane Surface",true);
            RhinoApp.RunScript("_Pause",true);
            RhinoApp.RunScript("! _RemapCPlane _Pause View Top _Export _Enter _Undo", true);
            return Result.Success;
        }
    }
}
