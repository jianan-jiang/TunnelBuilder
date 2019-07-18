using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("BC2AD4A6-6CB8-4398-B4F5-C2D32E3A7E26")]
    public class ExportTunnelSpanCommand : Command
    {
        ///<summary>The only instance of this command.</summary>
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
            get { return "ExportTunnelSpan"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("The {0} will query and export tunnel span along an alignment", EnglishName);



            Curve controlLine = null;
            double querySpacing = 1;
            double controlLineStartChainage = 0;

            OptionToggle controlLineDirectionToggle = new OptionToggle(true,"-","+");

            var rc = RhinoGet.GetNumber("Query Spacing", false, ref querySpacing);
            if (rc != Result.Success)
            {
                return rc;
            }
            if (querySpacing < 0)
            {
                RhinoApp.WriteLine("Query Spacing must be positive");
                return Result.Failure;
            }

            rc = RhinoGet.GetNumber("Control Line Start Chaiange", false, ref controlLineStartChainage);
            if (rc != Result.Success)
            {
                return rc;
            }
            if (querySpacing < 0)
            {
                RhinoApp.WriteLine("Control Line Start Chaiange must be positive");
                return Result.Failure;
            }

            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Control Line");
                go.AddOptionToggle("controlLineDirection", ref controlLineDirectionToggle);
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

            string method = "S";
            rc = Rhino.Input.RhinoGet.GetString("Method: (S)urface / (E)-Line", true, ref method);
            if (rc != Rhino.Commands.Result.Success)
                return rc;
            if (method != "S" && method != "s" && method !="E" && method !="e")
            {
                return Result.Failure;
            }

            if (method == "S" || method =="s")
            {
                Brep tunnelSurface = null;
                using (GetObject go = new GetObject())
                {
                    go.DisablePreSelect();
                    go.SetCommandPrompt("Select Tunnel Surface");
                    go.AddOptionToggle("controlLineDirection", ref controlLineDirectionToggle);
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

                var fn = RhinoGet.GetFileName(GetFileNameMode.SaveTextFile, "span.txt", "Tunnel Span File Name", null);
                if (fn == string.Empty)
                {
                    return Result.Cancel;
                }
                System.IO.StreamWriter fs = new System.IO.StreamWriter(fn);
                fs.WriteLine("Chainage (m), Span (m)");

                double controlLineLength = controlLine.GetLength();
                double totalAdvanceLength = 0.0;
                int advanceIteration = 1;


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
                                double span = getSpan(doc, apex, tunnel_profile);
                                double currentChainage = 0.0;
                                if (controlLineDirectionToggle.CurrentValue)
                                {
                                    currentChainage = controlLineStartChainage + totalAdvanceLength;
                                }
                                else
                                {
                                    currentChainage = controlLineLength - totalAdvanceLength + controlLineStartChainage;
                                }
                                string line = currentChainage.ToString() + "," + span.ToString();
                                fs.WriteLine(line);
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

                fs.Close();
            }
            else
            {
                Curve leftELine = null;
                using (GetObject go = new GetObject())
                {
                    go.DisablePreSelect();
                    go.SetCommandPrompt("Select Left E-Line");
                    go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                    while (true)
                    {
                        GetResult get_rc = go.GetMultiple(1,0);
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


                var fn = RhinoGet.GetFileName(GetFileNameMode.SaveTextFile, "span.txt", "Tunnel Span File Name", null);
                if (fn == string.Empty)
                {
                    return Result.Cancel;
                }
                System.IO.StreamWriter fs = new System.IO.StreamWriter(fn);
                fs.WriteLine("Chainage (m), Span (m)");

                double controlLineLength = controlLine.GetLength();
                double totalAdvanceLength = 0.0;
                int advanceIteration = 1;




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

                    var plane_to_world = Transform.ChangeBasis(cplane, Plane.WorldXY);
                    var world_to_plane = Transform.ChangeBasis(Plane.WorldXY, cplane);

                    Curve leftELine_plane = leftELine.DuplicateCurve();
                    Curve rightELine_plane = rightELine.DuplicateCurve();

                    leftELine_plane.Transform(world_to_plane);
                    rightELine_plane.Transform(world_to_plane);

                    double span = getSpan(doc,currentAdvancePoint, leftELine_plane, rightELine_plane);

                    double currentChainage = 0.0;
                    if (controlLineDirectionToggle.CurrentValue)
                    {
                        currentChainage = controlLineStartChainage + totalAdvanceLength;
                    }
                    else
                    {
                        currentChainage = controlLineLength - totalAdvanceLength + controlLineStartChainage;
                    }
                    string line = currentChainage.ToString() + "," + span.ToString();
                    fs.WriteLine(line);

                    advanceIteration = advanceIteration + 1;
                    totalAdvanceLength = totalAdvanceLength + querySpacing;
                }

                fs.Close();

            }

            doc.Views.Redraw();
            return Result.Success;
        }

        public static SpanResult getSpan(RhinoDoc doc, Point3d currentPoint, Curve leftELine, Curve rightELine,bool twoD=false)
        {
            double span = -1.0;

            double query_z = currentPoint.Z;
            Point3d start_point = new Point3d(-1000, query_z, 0);
            Point3d end_point = new Point3d(1000, query_z, 0);
            Point3d leftPoint = new Point3d();
            Point3d rightPoint = new Point3d();


            Line l = new Line(start_point, end_point);
            Rhino.Geometry.Intersect.CurveIntersections left_events = Rhino.Geometry.Intersect.Intersection.CurveLine(leftELine, l, intersection_tolerance, overlap_tolerance);
            bool flag = false;

            Rhino.Geometry.Intersect.CurveIntersections right_events = Rhino.Geometry.Intersect.Intersection.CurveLine(rightELine, l, intersection_tolerance, overlap_tolerance);
            if (right_events.Count > 0 && left_events.Count>0)
            {
                leftPoint = left_events[0].PointA;
                rightPoint = right_events[0].PointA;
                flag = true;
            }
            else
            {
                flag = false;
            }


            if (flag)
            {
                span = leftPoint.DistanceTo(rightPoint);
            }

            SpanResult sr = new SpanResult();
            sr.span = span;
            sr.leftIntersection = leftPoint;
            sr.rightIntersection = rightPoint;

            return sr;
        }

        public static double getSpan(RhinoDoc doc, Point3d currentPoint, Curve leftELine, Curve rightELine)
        {
            SpanResult sr = getSpan( doc,  currentPoint,  leftELine,rightELine,false);
            return sr.span;
        }

        public static double getSpan(RhinoDoc doc, Point3d apex, Curve tunnel_profile)
        {

            Interval tunnel_profile_domain = tunnel_profile.Domain;

            double tunnel_profile_legnth = tunnel_profile.GetLength();
            double apex_t_param;
            tunnel_profile.ClosestPoint(apex, out apex_t_param);
            double apex_tunnel_profile_length = tunnel_profile.GetLength(new Interval(tunnel_profile_domain[0], apex_t_param));
            double span_query_point_profile_length = apex_tunnel_profile_length;

            double span = -1.0;

            var bbox = tunnel_profile.GetBoundingBox(true);
            double sideWallY = bbox.Min.Y + 1;
            var start_point = new Point3d(-1000, sideWallY, 0);
            var end_point = new Point3d(1000, sideWallY, 0);
            var l = new Line(start_point, end_point);
            Rhino.Geometry.Intersect.CurveIntersections span_events = Rhino.Geometry.Intersect.Intersection.CurveLine(tunnel_profile, l, intersection_tolerance, overlap_tolerance);
            if (span_events.Count == 2)
            {
                Point3d span1 = span_events[0].PointA;
                Point3d span2 = span_events[1].PointA;
                span = span1.DistanceTo(span2);
                
            }

            if (span == -1)
            {
                try
                {
                    
                    span = bbox.Max.X - bbox.Min.X;
                }catch
                {
                    
                }
                
            }

            return span;
        }
    }
    public class SpanResult
    {
        public double span;
        public Point3d leftIntersection;
        public Point3d rightIntersection;
    }
}
