using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder
{
    public class StageConstructionPlaneCommand : Command
    {
        static StageConstructionPlaneCommand _instance;
        public StageConstructionPlaneCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the StageConstructionPlaneCommand command.</summary>
        public static StageConstructionPlaneCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "StageConstructionPlane"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Curve controlLine = null;
            double planeOffsetFromCrown = 0;

            var rc = RhinoGet.GetNumber("Vertical offset from apex of the tunnel crown", false, ref planeOffsetFromCrown);
            if (rc != Result.Success)
            {
                return rc;
            }

            int numberOfAdvances = 0;
            rc = RhinoGet.GetInteger("Number of advances", false, ref numberOfAdvances);
            if (rc != Result.Success)
            {
                return rc;
            }

            OptionDouble lengthCorrectionOption = new OptionDouble(0.001,true,0.0);

            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Control Line");
                go.AddOptionDouble("LengthCorrectionFactor", ref lengthCorrectionOption);
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
                go.AddOptionDouble("LengthCorrectionFactor", ref lengthCorrectionOption);
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

            Interval t = controlLine.Domain;
            Point3d[] left_surface_points = { };
            Point3d[] right_surface_points = { };
            for (int i = 0; i <= numberOfAdvances; i++)
            {
                double normalizedLengthParam = (double)i / numberOfAdvances;
                double length_correction_factor = lengthCorrectionOption.CurrentValue;// To prevent nil intersection at the start and the end of the loft
                if (i == 0 && 1.0 / numberOfAdvances > length_correction_factor) 
                {
                    normalizedLengthParam += length_correction_factor;
                }
                else if (i==numberOfAdvances && 1.0/numberOfAdvances > length_correction_factor)
                {
                    normalizedLengthParam -= length_correction_factor;
                }
                double x = normalizedLengthParam * (t[1] - t[0]) + t[0];
                Vector3d tangent = controlLine.TangentAt(x);
                Vector3d tangentUsedToAlignCPlane = new Vector3d(tangent);
                tangentUsedToAlignCPlane[2] = 0.0;
                Point3d point = controlLine.PointAt(x);
                Plane cplane = new Plane(point, tangentUsedToAlignCPlane);
                Surface srf = new PlaneSurface(cplane, new Interval(-1000, 1000), new Interval(-1000, 1000));
                //doc.Objects.AddPoint(point);
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

                        Curve tunnel_profile = Curve.JoinCurves(intersection_curves)[0].DuplicateCurve();
                        Curve tunnel_profile_World = tunnel_profile.DuplicateCurve();
                        tunnel_profile.Transform(world_to_plane);
                        var bbox = tunnel_profile.GetBoundingBox(true);
                        var crown_z = bbox.Max[1];
                        if (cplane.YAxis[2] < 0)
                        {
                            crown_z = bbox.Min[1];
                        }
                        var start_point = new Point3d(-1000, crown_z, 0);
                        var end_point = new Point3d(1000, crown_z, 0);


                        var l = new Line(start_point, end_point);
                        Rhino.Geometry.Intersect.CurveIntersections apex_events = Rhino.Geometry.Intersect.Intersection.CurveLine(tunnel_profile, l, intersection_tolerance, overlap_tolerance);
                        if (apex_events.Count > 0)
                        {
                            Point3d apex = apex_events[0].PointA;
                            apex.Transform(plane_to_world);
                            Point3d construction_surface_mid_point = new Point3d(apex);
                            construction_surface_mid_point[2] = construction_surface_mid_point[2] - planeOffsetFromCrown;

                            Point3d tunnel_width_test_line_left_point = new Point3d(construction_surface_mid_point);
                            Point3d tunnel_width_test_line_right_point = new Point3d(construction_surface_mid_point);

                            Plane construction_stage_plane = new Plane(construction_surface_mid_point, new Vector3d(0, 0, 1));
                            var tunnel_width_intersection_events = Rhino.Geometry.Intersect.Intersection.CurvePlane(tunnel_profile_World, construction_stage_plane, 0.001);

                            if(tunnel_width_intersection_events!=null && tunnel_width_intersection_events.Count==2)
                            {
                                Point3d left_intersection = new Point3d(tunnel_width_intersection_events[0].PointA);
                                Point3d right_intersection = new Point3d(tunnel_width_intersection_events[1].PointA);
                                left_intersection.Transform(world_to_plane);
                                right_intersection.Transform(world_to_plane);

                                Point3d left_intersection_World = new Point3d(left_intersection);
                                Point3d right_intersection_World = new Point3d(right_intersection);
                                left_intersection_World.Transform(plane_to_world);
                                right_intersection_World.Transform(plane_to_world);

                                if (left_surface_points.Length > 0 && right_surface_points.Length>0)
                                {
                                    Point3d previousLeft_World = new Point3d(left_surface_points[left_surface_points.Length - 1]);
                                    Point3d previousRight_World = new Point3d (right_surface_points[right_surface_points.Length - 1]);

                                    Curve left_segement = new Line(previousLeft_World, left_intersection_World).ToNurbsCurve();
                                    Curve right_segement = new Line(previousRight_World, right_intersection_World).ToNurbsCurve();

                                    left_segement = Curve.ProjectToPlane(left_segement, Plane.WorldXY);
                                    right_segement = Curve.ProjectToPlane(right_segement, Plane.WorldXY);


                                    var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(left_segement, right_segement,Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,0);
                                    if (intersection.Count>0)
                                    {
                                        left_intersection_World = new Point3d(right_intersection);
                                        right_intersection_World = new Point3d(left_intersection);
                                        left_intersection_World.Transform(plane_to_world);
                                        right_intersection_World.Transform(plane_to_world);
                                    }
                                }

                                left_intersection_World.Transform(world_to_plane);
                                right_intersection_World.Transform(world_to_plane);

                                left_intersection_World[0] = left_intersection_World[0] - 1;
                                right_intersection_World[0] = right_intersection_World[0] + 1;

                                if (tunnel_profile.Contains(left_intersection_World, Plane.WorldXY, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) == Rhino.Geometry.PointContainment.Inside)
                                {
                                    left_intersection_World[0] = left_intersection_World[0] + 2;
                                    right_intersection_World[0] = right_intersection_World[0] - 2;
                                }

                                left_intersection_World.Transform(plane_to_world);
                                right_intersection_World.Transform(plane_to_world);

                                Array.Resize(ref left_surface_points, left_surface_points.Length + 1);
                                Array.Resize(ref right_surface_points, right_surface_points.Length + 1);
                                left_surface_points[left_surface_points.Length-1] = left_intersection_World;
                                right_surface_points[right_surface_points.Length-1] = right_intersection_World;
                            }
                            else
                            {
                                RhinoApp.WriteLine("Fail to extract tunnel section width");
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
            }

            Curve left_curve = Curve.CreateInterpolatedCurve(left_surface_points,3);
            Curve right_curve = Curve.CreateInterpolatedCurve(right_surface_points, 3);

            Line start_line = new Line(left_surface_points[0], right_surface_points[0]);
            Line end_line = new Line(right_surface_points[right_surface_points.Length-1],left_surface_points[left_surface_points.Length-1]);
            Curve[] curves = { start_line.ToNurbsCurve(), right_curve, end_line.ToNurbsCurve(), left_curve };


            var brep = Brep.CreateEdgeSurface(curves);
   
            if(brep!=null)
            {
                doc.Objects.AddBrep(brep);
                doc.Views.Redraw();
            }
            return Result.Success;
        }
    }
}