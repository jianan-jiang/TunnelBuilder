using System;
using System.Runtime.InteropServices;

using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder
{
    [Guid("B3AD5CC2-DB0B-47CF-AE40-83EF3C17ECE8")]
    public class CrossSectionCommand:Command
    {
        public override string EnglishName
        {
            get { return "CrossSection"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            throw new NotImplementedException();
        }
    }
}
