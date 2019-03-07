using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder
{

    public class InstallBoltCommand : Command
    {
        ///<summary>The only instance of this command.</summary>
        public static TunnelBuilderCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "InstallBolt"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("The {0} will install bolts",EnglishName);

            

            Curve controlLine=null;
            double boltLength = 0;
            double boltAdvanceSpacing = 0;
            double boltSectionSpacing = 0;
            string boltLayerName = "Bolt";

            OptionToggle boltInstallLocationToggle = new OptionToggle(true, "All", "CrownOnly");
            OptionToggle staggeredToggle = new OptionToggle(false, "Normal", "Staggered");

            var rc = RhinoGet.GetNumber("Bolt Length", false, ref boltLength);
            if (rc != Result.Success)
            {
                return rc;
            }
            if (boltLength<0)
            {
                RhinoApp.WriteLine("Bolt length must be positive");
                return Result.Failure;
            }

            rc = RhinoGet.GetNumber("Bolt In-Plane Spacing", false, ref boltSectionSpacing);
            if (rc != Result.Success)
            {
                return rc;
            }
            if (boltSectionSpacing < 0)
            {
                RhinoApp.WriteLine("Bolt In-Plane Spacing must be positive");
                return Result.Failure;
            }

            rc = RhinoGet.GetNumber("Bolt Out-of-plane Spacing", false, ref boltAdvanceSpacing);
            if (rc != Result.Success)
            {
                return rc;
            }
            if (boltAdvanceSpacing < 0)
            {
                RhinoApp.WriteLine("Bolt Out-of-plane Spacing must be positive");
                return Result.Failure;
            }


            rc = RhinoGet.GetString("Bolt Layer Name", true, ref boltLayerName);
            if (rc == Result.Cancel)
            {
                return rc;
            }
            else if(boltLayerName == "")
            {
                boltLayerName = "Bolt";
            }

            //Create layer to store bolt elements
            int bolt_layer_index = UtilFunctions.AddNewLayer(doc, "Bolt");
            if (boltLayerName != "Bolt")
            {
                // Create sub-layer if necessary
                bolt_layer_index =  UtilFunctions.AddNewLayer(doc, boltLayerName,"Bolt");
            }
            if(bolt_layer_index == -1)
            {
                RhinoApp.WriteLine("Unable to add layer.");
                return Result.Failure;
            }
            


            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Control Line");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                go.AddOptionToggle("BoltInstallationRange",ref boltInstallLocationToggle);
                go.AddOptionToggle("BoltPattern", ref staggeredToggle);
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
                go.AddOptionToggle("BoltInstallationRange", ref boltInstallLocationToggle);
                go.AddOptionToggle("BoltPattern", ref staggeredToggle);
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
            int numberOfAdvances = (int) Math.Floor(controlLine.GetLength()/boltAdvanceSpacing);
            for(int i=0;i<numberOfAdvances;i++)
            {
                double x = (double)i / numberOfAdvances * (t[1] - t[0]) + t[0];
                Vector3d tangent = controlLine.TangentAt(x);
                Vector3d tangentUsedToAlignCPlane = new Vector3d(tangent);
                tangentUsedToAlignCPlane[2] = 0.0;
                Point3d point = controlLine.PointAt(x);
                Plane cplane = new Plane(point, tangentUsedToAlignCPlane);
                Surface srf = new PlaneSurface(cplane, new Interval(-1000,1000), new Interval(-1000, 1000));
                const double intersection_tolerance = 0.001;
                const double overlap_tolerance = 0.0;
                Curve[] intersection_curves;
                Point3d[] intersection_points;
                var events = Rhino.Geometry.Intersect.Intersection.BrepSurface(tunnelSurface, srf, intersection_tolerance, out intersection_curves, out intersection_points);
                if(events)
                {
                    if(intersection_curves.Length>0||intersection_points.Length>0)
                    {
                        var plane_to_world = Transform.ChangeBasis(cplane, Plane.WorldXY);
                        var world_to_plane = Transform.ChangeBasis(Plane.WorldXY, cplane);
                        Curve tunnel_profile = null;
                        Curve[] joint_tunnel_profile = Curve.JoinCurves(intersection_curves, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,false);
                        if(joint_tunnel_profile.Length == 0)
                        {
                            RhinoApp.WriteLine("Fail to extract tunnel section profile");
                            continue;
                        }
                        tunnel_profile = joint_tunnel_profile[0];
                        if(!tunnel_profile.IsClosed)
                        {
                            continue;
                        }
                        tunnel_profile.Transform(world_to_plane);
                        // By Default, the command will only install bolts on the crown of the tunnel.
                        var bbox = tunnel_profile.GetBoundingBox(true);
                        var crown_z = bbox.Max[1];
                        if(cplane.YAxis[2]<0)
                        {
                            crown_z = bbox.Min[1];
                        }
                        var start_point = new Point3d(-1000, crown_z,0);
                        var end_point = new Point3d(1000, crown_z,0);
                        

                        var l = new Line(start_point, end_point);
                        Rhino.Geometry.Intersect.CurveIntersections apex_events = Rhino.Geometry.Intersect.Intersection.CurveLine(tunnel_profile, l, intersection_tolerance, overlap_tolerance);
                        if(apex_events.Count>0)
                        {
                            Point3d apex = apex_events[0].PointA;
                            if (staggeredToggle.CurrentValue)
                            {
                                // If the bolt pattern is staggered, offset the bolt according to advance number.
                                if(i % 2==0)
                                {
                                    //Intall the bolts in +t_param direction
                                    installBoltIteration(doc, apex, tunnel_profile, boltSectionSpacing, boltLength, bolt_layer_index, boltInstallLocationToggle, tunnelSurface, plane_to_world, boltSectionSpacing / 2);
                                    //Intall the bolts in -t_param direction
                                    installBoltIteration(doc, apex, tunnel_profile, -boltSectionSpacing, boltLength, bolt_layer_index, boltInstallLocationToggle, tunnelSurface, plane_to_world, -boltSectionSpacing / 2);
                                }
                                else
                                {
                                    //Intall the bolts in +t_param direction
                                    installBoltIteration(doc, apex, tunnel_profile, boltSectionSpacing, boltLength, bolt_layer_index, boltInstallLocationToggle, tunnelSurface, plane_to_world, 0);
                                    //Intall the bolts in -t_param direction
                                    installBoltIteration(doc, apex, tunnel_profile, -boltSectionSpacing, boltLength, bolt_layer_index, boltInstallLocationToggle, tunnelSurface, plane_to_world, -boltSectionSpacing);
                                }
                                
                            }
                            else
                            {
                                //Intall the bolts in +t_param direction
                                installBoltIteration(doc, apex, tunnel_profile, boltSectionSpacing, boltLength, bolt_layer_index, boltInstallLocationToggle, tunnelSurface, plane_to_world, 0);
                                //Intall the bolts in -t_param direction
                                installBoltIteration(doc, apex, tunnel_profile, -boltSectionSpacing, boltLength, bolt_layer_index, boltInstallLocationToggle, tunnelSurface, plane_to_world, -boltSectionSpacing);
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

            doc.Views.Redraw();
            return Result.Success;
        }

        private bool installBoltIteration(RhinoDoc doc,Point3d apex,Curve tunnel_profile,double boltSectionSpacing,double boltLength,int bolt_layer_index,OptionToggle boltInstallLocationToggle, Brep tunnelSurface,Transform plane_to_world,double offset)
        {
            
            Interval tunnel_profile_domain = tunnel_profile.Domain;
            double tunnel_profile_legnth = tunnel_profile.GetLength();
            double apex_t_param;
            tunnel_profile.ClosestPoint(apex, out apex_t_param);
            double apex_tunnel_profile_length = tunnel_profile.GetLength(new Interval(tunnel_profile_domain[0], apex_t_param));
            double bolt_installation_point_tunnel_profile_length = apex_tunnel_profile_length + offset;
            Point3d bolt_installation_point = tunnel_profile.PointAtLength(bolt_installation_point_tunnel_profile_length);
            
            var current_curvature = tunnel_profile.CurvatureAt(apex_t_param).Length;
            bool onCrownFlag = true;
            //Intall the bolts in +t_param direction
            while (bolt_installation_point_tunnel_profile_length + boltSectionSpacing < tunnel_profile_legnth && bolt_installation_point_tunnel_profile_length + boltSectionSpacing > 0)
            {


                double bolt_installation_point_t_param;
                tunnel_profile.ClosestPoint(bolt_installation_point, out bolt_installation_point_t_param);

                var bolt_installation_point_curvarture = tunnel_profile.CurvatureAt(bolt_installation_point_t_param).Length;

                onCrownFlag = Math.Abs(current_curvature - bolt_installation_point_curvarture) < 0.05;

                if (boltInstallLocationToggle.CurrentValue == true && onCrownFlag == false)
                {
                    break;
                }

                Line line = getBoltLine(tunnel_profile, bolt_installation_point, boltLength);


                //Transform everything backto world coordinates
                Point3d bolt_installation_point_World = new Point3d(bolt_installation_point);
                bolt_installation_point_World.Transform(plane_to_world);
                Line line_World = line;
                line_World.Transform(plane_to_world);

                //Test if bolt intersects with tunnel surface more than once.
                Curve[] intersectCurves;
                Point3d[] intersectPoints;
                Rhino.Geometry.Intersect.Intersection.CurveBrep(line_World.ToNurbsCurve(), tunnelSurface, 0.001, out intersectCurves, out intersectPoints);



                // Advance to next step
                Point3d nextBoltInstallationPoint = tunnel_profile.PointAtLength(bolt_installation_point_tunnel_profile_length + boltSectionSpacing);
                bolt_installation_point = nextBoltInstallationPoint;
                bolt_installation_point_tunnel_profile_length = bolt_installation_point_tunnel_profile_length + boltSectionSpacing;

                if (intersectPoints.Length > 1)
                {
                    //If yes, do not install this bolt
                    continue;
                }

                var guid = doc.Objects.AddLine(line_World);
                var bolt_line_object = new Rhino.DocObjects.ObjRef(guid).Object();
                bolt_line_object.Attributes.LayerIndex = bolt_layer_index;
                bolt_line_object.CommitChanges();
            }

            return true;
        }

        private Line getBoltLine(Curve tunnel_profile, Point3d bolt_installation_point, double boltLength)
        {
            double bolt_installation_point_t_param;
            tunnel_profile.ClosestPoint(bolt_installation_point, out bolt_installation_point_t_param);
            Vector3d tangentAtInstallationPoint = tunnel_profile.TangentAt(bolt_installation_point_t_param);
            Vector3d tangentOnTunnelProfilePlane = tangentAtInstallationPoint;


            //Get Normal Vector
            Vector3d normal;
            if (Math.Abs(tangentOnTunnelProfilePlane[0]) > 0.001)
            {
                //If x-component of the tangent is not zero, use the x-component as denominator and assume y-component of the normal is 1
                normal = new Vector3d(-tangentOnTunnelProfilePlane[1] / tangentOnTunnelProfilePlane[0], 1, 0);
            }
            else
            {
                normal = new Vector3d(1, -tangentOnTunnelProfilePlane[0] / tangentOnTunnelProfilePlane[1], 0);
            }

            Line line = new Line(bolt_installation_point, normal, boltLength);

            var events = Rhino.Geometry.Intersect.Intersection.CurveCurve(tunnel_profile, line.ToNurbsCurve(), Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, 0);

            if (tunnel_profile.Contains(line.PointAtLength(line.Length)) == Rhino.Geometry.PointContainment.Inside)
            {
                normal = -normal;
            }
            else if (events.Count>1)
            {
                normal = -normal;
            }

            line = new Line(bolt_installation_point, normal, boltLength);
            return line;
        }
    }
    
}
