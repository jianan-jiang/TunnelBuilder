using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace SplitTunnelCL
{
    [System.Runtime.InteropServices.Guid("8C519DB8-2E4C-4C12-866F-EAFCA3CA6E10")]
    public class SplitTunnelCLCommand : Command
    {
        public SplitTunnelCLCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static SplitTunnelCLCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "SplitTunnelCLCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: start here modifying the behaviour of your command.
            Curve CL = null;
            using (Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("select tunnel control line in plane");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                go.GetMultiple(1, 0);
                CL = go.Object(0).Geometry() as Curve;
            }

            Curve CL_3D = null;
            using (Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("select 3D tunnel control line");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                go.GetMultiple(1, 0);
                CL_3D = go.Object(0).Geometry() as Curve;
            }

            using (Rhino.Input.Custom.GetObject gp = new Rhino.Input.Custom.GetObject())
            {
                gp.SetCommandPrompt("Select points with chainage as x coordinates");
                gp.GeometryFilter = Rhino.DocObjects.ObjectType.Point;
                gp.GetMultiple(1, 0);

                if (gp.CommandResult() != Result.Success)
                    return gp.CommandResult();
                foreach (var o_ref in gp.Objects())
                {
                    var pt = o_ref.Point();
                    if (pt != null)
                    {
                        var x = o_ref.Point().Location.X;
                        Point3d CP = CL.PointAtLength(x);
                        Vector3d vector1 = new Vector3d(0, 0, -100);
                        Vector3d vector2 = new Vector3d(0, 0, 100);
                        Point3d pt_start = CP + vector1;
                        Point3d pt_end = CP + vector2;
                        Line L = new Line(pt_start, pt_end);

                        const double intersection_tolerance = 0.001;
                        const double overlap_tolerance = 0.0;
                        var events = Rhino.Geometry.Intersect.Intersection.CurveCurve(CL_3D, new Rhino.Geometry.LineCurve(L), intersection_tolerance, overlap_tolerance);
                        if (events[0] != null)
                        {
                            doc.Objects.AddPoint(events[0].PointA);
                        }
                    }

                }
            }



            return Result.Success;
        }
    }
}