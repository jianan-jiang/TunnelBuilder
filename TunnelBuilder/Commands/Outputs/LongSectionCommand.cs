using System;
using System.Runtime.InteropServices;

using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder.Commands.Outputs
{
    [Guid("F8FF77FC-B0EB-46BA-B789-02058938FC0F")]
    public class LongSectionCommand:Command
    {
        public override string EnglishName
        {
            get { return "LongSection"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string controlLineName="";
            double chainage=0;

            ControlLine controlLine;

            var rc = RhinoGet.GetString("Control line name", false, ref controlLineName);
            if(rc!=Result.Success)
            {
                return rc;
            }



            rc = RhinoGet.GetNumber("Chainage", true, ref chainage);
        }
    }
}
