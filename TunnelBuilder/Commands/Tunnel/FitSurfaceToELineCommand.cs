using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;


namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("056E9EA1-C3BE-4A60-A598-717D25DCC545")]
    public class FitSurfaceToELineCommand : Command
    {
        const double intersection_tolerance = 0.001;
        const double queryTrialSpacing = 0.5;
        const double overlap_tolerance = 0.0;

        public static TunnelBuilderCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "FitSurfaceToELine"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Curve controlLine = null;
            double querySpacing = 1;
            double controlLineStartChainage = 0;
            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Control Line");
                //go.AddOptionToggle("controlLineDirection", ref controlLineDirectionToggle);
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
                //go.AddOptionToggle("controlLineDirection", ref controlLineDirectionToggle);
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

            Curve leftELine = null;
            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Left E-Line");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                while (true)
                {
                    GetResult get_rc = go.GetMultiple(1, 0);
                    if (get_rc == GetResult.Object)
                    {
                        leftELine = go.Object(0).Geometry() as Curve;
                        if (leftELine == null)
                        {
                            RhinoApp.WriteLine("No E-Line was selected");
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

            Curve rightELine = null;
            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Right E-Line");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                while (true)
                {
                    GetResult get_rc = go.GetMultiple(1, 0);
                    if (get_rc == GetResult.Object)
                    {
                        rightELine = go.Object(0).Geometry() as Curve;
                        if (rightELine == null)
                        {
                            RhinoApp.WriteLine("No E-Line was selected");
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

            double controlLineLength = controlLine.GetLength();
            double totalAdvanceLength = 0.0;
            int advanceIteration = 1;
            List<Curve> section_curves = new List<Curve>();
            double lastELineSpan = 0.1;
            double lastTunnelProfileChaiange = 0;
            Curve lastTunnelProfile = null;
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
                    cplane.Rotate(Math.PI, cplane.ZAxis);
                }

                Surface srf = new PlaneSurface(cplane, new Interval(-1000, 1000), new Interval(-1000, 1000));

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
                            totalAdvanceLength = totalAdvanceLength + querySpacing;
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
                            Curve leftELine_plane = leftELine.DuplicateCurve();
                            Curve rightELine_plane = rightELine.DuplicateCurve();
                            Point3d ELineAdvancePoint = new Point3d(currentAdvancePoint);
                            ELineAdvancePoint.Z = 0;
                            Plane ELineCPlane = new Plane(ELineAdvancePoint, tangentUsedToAlignCPlane);
                            var ELineCPlane_to_world = Transform.ChangeBasis(ELineCPlane, Plane.WorldXY);
                            var world_to_ELineCPlane = Transform.ChangeBasis(Plane.WorldXY, ELineCPlane);
                            leftELine_plane.Transform(world_to_ELineCPlane);
                            rightELine_plane.Transform(world_to_ELineCPlane);
                            SpanResult sr = ExportTunnelSpanCommand.getSpan(ELineAdvancePoint, leftELine_plane, rightELine_plane,true);
                            double ELineSpan = sr.span;
                            Point3d ELineCentre = 0.5 * (sr.leftIntersection + sr.rightIntersection);
                            double surfaceSpan = ExportTunnelSpanCommand.getSpan(doc, apex, tunnel_profile);

                            if (ELineSpan > 0 && surfaceSpan > 0)
                            {
                                Transform scale = Transform.Scale(Plane.WorldXY, ELineSpan / surfaceSpan, 1, 1);
                                Curve new_tunnel_profile = tunnel_profile.DuplicateCurve();


                                new_tunnel_profile.Transform(scale);
                                BoundingBox tunnel_profile_bbox = new_tunnel_profile.GetBoundingBox(true);
                                Point3d tunnel_profile_centre = 0.5 * (tunnel_profile_bbox.Min + tunnel_profile_bbox.Max);
                                new_tunnel_profile.Translate(ELineCentre.X - tunnel_profile_centre.X, 0, 0);
                                new_tunnel_profile.Transform(plane_to_world);

                                double changeInSpan = (ELineSpan - lastELineSpan) / lastELineSpan;
                                if (changeInSpan > 0.02 || changeInSpan < -0.02)
                                {

                                    lastELineSpan = ELineSpan;
                                    if (lastTunnelProfile != null)
                                    {
                                        doc.Objects.Add(lastTunnelProfile);
                                        section_curves.Add(lastTunnelProfile);
                                    }
                                    Guid guid = doc.Objects.Add(new_tunnel_profile);
                                    section_curves.Add(new_tunnel_profile);
                                    lastTunnelProfileChaiange = totalAdvanceLength;
                                }else if(totalAdvanceLength-lastTunnelProfileChaiange>10)
                                {
                                    if (lastTunnelProfile != null)
                                    {
                                        doc.Objects.Add(lastTunnelProfile);
                                        section_curves.Add(lastTunnelProfile);
                                        lastTunnelProfileChaiange = totalAdvanceLength;
                                    }
                                }
                                lastTunnelProfile = new_tunnel_profile;
                            }
                            else
                            {
                                if (lastTunnelProfile != null)
                                {
                                    doc.Objects.Add(lastTunnelProfile);
                                    section_curves.Add(lastTunnelProfile);
                                    lastTunnelProfile = null;
                                    lastELineSpan = 0.1;
                                }
                            }
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
                totalAdvanceLength = totalAdvanceLength + querySpacing;
            }
            var breps = Brep.CreateFromLoft(section_curves, Point3d.Unset, Point3d.Unset, LoftType.Tight, false);

            Guid brep_guid = default(Guid);
            if (breps.Length == 0)
            {
                return Result.Failure;
            }
            foreach (var brep in breps)
            {
                brep_guid = doc.Objects.AddBrep(brep);
            }
            return Result.Success;
        }
    }
}
