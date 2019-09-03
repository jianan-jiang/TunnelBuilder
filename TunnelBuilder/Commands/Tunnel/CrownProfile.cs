using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("CC778B91-B0D3-4C27-BF4D-21F9B7A289FD")]
    public class CrownProfileCommand:Command
    {
        public override string EnglishName
        {
            get { return "CrownProfile"; }
        }
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Curve controlLine = null;
            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Control Line");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                while (true)
                {
                    GetResult get_rc = go.GetMultiple(1, 0);
                    if (get_rc == GetResult.Object)
                    {
                        controlLine = go.Object(0).Geometry() as Curve;
                        if (controlLine == null)
                        {
                            return Rhino.Commands.Result.Failure;
                        }
                    }
                    else if (get_rc == GetResult.Option)
                    {
                        continue;
                    }
                    break;
                }
            }

            Brep tunnelSurface = null;
            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Tunnel Surface");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Brep;
                while (true)
                {
                    GetResult get_rc = go.GetMultiple(1, 0);
                    if (get_rc == GetResult.Object)
                    {
                        tunnelSurface = go.Object(0).Geometry() as Brep;
                        if (tunnelSurface == null)
                        {
                            RhinoApp.WriteLine("No tunnel surface was selected");
                            return Result.Failure;
                        }
                    }
                    else if (get_rc == GetResult.Option)
                    {
                        continue;
                    }
                    break;
                }

            }
            double advanceSpacing = 1;
            var rc = RhinoGet.GetNumber("Sample Spacing", false, ref advanceSpacing);
            if (rc != Result.Success)
            {
                return rc;
            }

            double controlLineLength = controlLine.GetLength();
            double totalAdvanceLength = 0.0;
            int advanceIteration = 1;

            List<Point3d> apexPoints = new List<Point3d>();

            while (totalAdvanceLength <= controlLineLength)
            {
                Point3d currentAdvancePoint = controlLine.PointAtLength(totalAdvanceLength);
                double currentAdvancePoint_t_param;
                controlLine.ClosestPoint(currentAdvancePoint, out currentAdvancePoint_t_param);
                Vector3d tangent = controlLine.TangentAt(currentAdvancePoint_t_param);
                Vector3d tangentUsedToAlignCPlane = new Vector3d(tangent);
                tangentUsedToAlignCPlane[2] = 0.0;
                Point3d point = controlLine.PointAt(currentAdvancePoint_t_param);
                Plane cplane = new Plane(point, tangentUsedToAlignCPlane);

                if (cplane.YAxis[2] < 0)
                {
                    //Rotate the plane 180 degree if y axis is pointing down
                    cplane.Rotate(Math.PI, cplane.XAxis);
                }

                Surface srf = new PlaneSurface(cplane, new Interval(-1000, 1000), new Interval(-1000, 1000));
                const double intersection_tolerance = 0.001;
                const double overlap_tolerance = 0.0;
                Curve[] intersection_curves;
                Point3d[] intersection_points;
                var events = Rhino.Geometry.Intersect.Intersection.BrepSurface(tunnelSurface, srf, intersection_tolerance, out intersection_curves, out intersection_points);
                if (events)
                {
                    if (intersection_curves.Length > 0 || intersection_points.Length > 0)
                    {
                        var plane_to_world = Transform.ChangeBasis(cplane, Plane.WorldXY);
                        var world_to_plane = Transform.ChangeBasis(Plane.WorldXY, cplane);
                        Curve tunnel_profile = null;
                        Curve[] joint_tunnel_profile = Curve.JoinCurves(intersection_curves, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, false);
                        if (joint_tunnel_profile.Length == 0)
                        {
                            RhinoApp.WriteLine("Fail to extract tunnel section profile");
                            continue;
                        }
                        tunnel_profile = joint_tunnel_profile[0];

                        if (!tunnel_profile.IsClosed)
                        {
                            advanceIteration = advanceIteration + 1;
                            totalAdvanceLength = totalAdvanceLength + advanceSpacing;
                            continue;
                        }

                        tunnel_profile.Transform(world_to_plane);

                        if (!tunnel_profile.IsClosed)
                        {
                            Curve newLine = new Line(tunnel_profile.PointAtStart, tunnel_profile.PointAtEnd).ToNurbsCurve();
                            Curve[] result = Curve.JoinCurves(new Curve[] { tunnel_profile, newLine });
                            tunnel_profile = result[0];
                        }

                        // By Default, the command will only install bolts on the crown of the tunnel.
                        var bbox = tunnel_profile.GetBoundingBox(true);
                        var crown_z = bbox.Max[1];
                        var start_point = new Point3d(-1000, crown_z, 0);
                        var end_point = new Point3d(1000, crown_z, 0);


                        var l = new Line(start_point, end_point);
                        Rhino.Geometry.Intersect.CurveIntersections apex_events = Rhino.Geometry.Intersect.Intersection.CurveLine(tunnel_profile, l, intersection_tolerance, overlap_tolerance);
                        if (apex_events.Count > 0)
                        {
                            Point3d apex = apex_events[0].PointA;
                            apex.Transform(plane_to_world);
                            apexPoints.Add(apex);
                        }
                    }
                    else
                    {
                        RhinoApp.WriteLine("Fail to extract tunnel section profile");
                        //return Result.Failure;
                    }
                }
                else
                {
                    RhinoApp.WriteLine("Fail to extract tunnel section profile");
                    return Result.Failure;
                }

                advanceIteration = advanceIteration + 1;
                totalAdvanceLength = totalAdvanceLength + advanceSpacing;
            }

            Curve projectedProfile = Curve.CreateInterpolatedCurve(apexPoints,1);
            var guid = doc.Objects.AddCurve(projectedProfile);
            

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
