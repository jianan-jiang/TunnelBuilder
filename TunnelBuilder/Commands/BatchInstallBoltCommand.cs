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

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("CF972E53-CCD5-4A61-9D0A-7A60E1DE5223")]
    class BatchInstallBoltCommand : Command
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
            BoltSupportType bst = new BoltSupportType("");
            return Result.Success;
        }
    }
}
