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
using System.Xml.Serialization;
using System.IO;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("CF972E53-CCD5-4A61-9D0A-7A60E1DE5223")]
    public class BatchInstallBoltCommand : Command
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

            var fd = new Rhino.UI.OpenFileDialog { Filter = "XML Files (*.xml)|*.xml", Title = "Open Tunnel Support Definition File", MultiSelect = false, DefaultExt = "xml" };
            if (!fd.ShowOpenDialog())
            {
                return Result.Cancel;
            }
            var fn = fd.FileName;
            if (fn == string.Empty || !System.IO.File.Exists(fn))
            {
                return Result.Cancel;
            }


            TunnelSupportDefinition tsd;
            FileStream tstFileStream;
            XmlSerializer tstSerializer = new XmlSerializer(typeof(TunnelSupportDefinition));
            try
            {
                tstFileStream = new FileStream(fn, FileMode.Open);
            }
            catch
            {
                RhinoApp.WriteLine("Unable to open tunnel support definition file");
                return Result.Failure;
            }
            
            try
            {
                tsd = (TunnelSupportDefinition)tstSerializer.Deserialize(tstFileStream);
            }
            catch
            {
                RhinoApp.WriteLine("Wrong tunnel support definition format");
                return Result.Failure;
            }
            
            tstFileStream.Close();
            RhinoApp.WriteLine("Applying Tunnel Support Definition created on " + tsd.CreateDate.ToShortDateString());

            Curve controlLine = null;
            string boltLayerName = "Bolt";

            List<string> groundConditionNames = new List<string>();

            foreach (var groundCondition in tsd.GroundConditions)
            {
                groundConditionNames.Add(groundCondition.Name);
            }

            string[] groundConditionNameList = groundConditionNames.ToArray();
            int groundConditionNameIndex = 0;

            var rc = RhinoGet.GetString("Bolt Layer Name", true, ref boltLayerName);
            if (rc == Result.Cancel)
            {
                return rc;
            }
            else if (boltLayerName == "")
            {
                boltLayerName = "Bolt";
            }

            //Create layer to store bolt elements
            int bolt_layer_index = UtilFunctions.AddNewLayer(doc, "Bolt");
            if (boltLayerName != "Bolt")
            {
                // Create sub-layer if necessary
                bolt_layer_index = UtilFunctions.AddNewLayer(doc, boltLayerName, "Bolt");
            }
            if (bolt_layer_index == -1)
            {
                RhinoApp.WriteLine("Unable to add layer.");
                return Result.Failure;
            }



            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Control Line");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                int groundConditionOptionList = go.AddOptionList("GroundCondition", groundConditionNameList, groundConditionNameIndex);
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
                        if(go.OptionIndex() == groundConditionOptionList)
                        {
                            groundConditionNameIndex = go.Option().CurrentListOptionIndex;
                        }
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
                int groundConditionOptionList = go.AddOptionList("GroundCondition", groundConditionNameList, groundConditionNameIndex);
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
                        if (go.OptionIndex() == groundConditionOptionList)
                        {
                            groundConditionNameIndex = go.Option().CurrentListOptionIndex;
                        }
                        continue;
                    }
                    break;
                }

            }


            return installBolt(doc, controlLine, tunnelSurface, tsd, groundConditionNameList[groundConditionNameIndex], bolt_layer_index);
        }

        public static void getBoltLength(double tunnel_span,TunnelSupportDefinition tsd,out double boltLength,out string supportName)
        {
            boltLength = 0.0;
            supportName = "";
            BoltSupportLength bsl = null;
            foreach (var supportLengthsDefinition in tsd.BoltSupportLengths)
            {
                if (tunnel_span > supportLengthsDefinition.TunnelSpan)
                {
                    if (supportLengthsDefinition.Length > boltLength)
                    {
                        boltLength = supportLengthsDefinition.Length;
                        supportName = supportLengthsDefinition.Name;
                        bsl = supportLengthsDefinition;
                    }
                }
            }
        }

        public Result installBolt(RhinoDoc doc, Curve controlLine, Brep tunnelSurface, TunnelSupportDefinition tsd, string groundConditionName, int bolt_layer_index)
        {
            double controlLineLength = controlLine.GetLength();
            double totalAdvanceLength = 0.0;
            int advanceIteration = 1;
            double boltLongitudinalSpacing = 0;
            double boltTransversiveSpacing = 0;

            foreach (var groundCondition in tsd.GroundConditions)
            {
                if (groundCondition.Name == groundConditionName)
                {
                    boltLongitudinalSpacing = groundCondition.LongitudinalSpacing;
                    boltTransversiveSpacing = groundCondition.TransversiveSpacing;
                }
            }

            if(boltLongitudinalSpacing == 0 || boltTransversiveSpacing == 0)
            {
                RhinoApp.WriteLine("Invalid ground condition");
                return Result.Failure;
            }

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
                            totalAdvanceLength = totalAdvanceLength + boltLongitudinalSpacing;
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
                            double tunnel_span = ExportTunnelSpanCommand.getSpan(doc, apex, tunnel_profile);
                            string supportName = "";
                            if(tunnel_span > 0 )
                            {
                                double boltLength = 0.0;
                                BoltSupportLength bsl = null;
                                foreach (var supportLengthsDefinition in tsd.BoltSupportLengths)
                                {
                                    if (tunnel_span > supportLengthsDefinition.TunnelSpan)
                                    {
                                        if (supportLengthsDefinition.Length > boltLength)
                                        {
                                            boltLength = supportLengthsDefinition.Length;
                                            supportName = supportLengthsDefinition.Name;
                                            bsl = supportLengthsDefinition;
                                        }
                                    }
                                }

                                int layerIndex = UtilFunctions.AddNewLayer(doc, supportName, bolt_layer_index);

                                OptionToggle boltInstallLocationToggle = new OptionToggle(bsl.CrownOnly, "All", "CrownOnly");

                                if (bsl.Staggered)
                                {

                                    // If the bolt pattern is staggered, offset the bolt according to advance number.
                                    if (advanceIteration % 2 == 0)
                                    {
                                        //Intall the bolts in +t_param direction
                                        InstallBoltCommand.installBoltIteration(doc, apex, tunnel_profile, boltTransversiveSpacing, boltLength, layerIndex, boltInstallLocationToggle, tunnelSurface, plane_to_world, boltTransversiveSpacing / 2);
                                        //Intall the bolts in -t_param direction
                                        InstallBoltCommand.installBoltIteration(doc, apex, tunnel_profile, -boltTransversiveSpacing, boltLength, layerIndex, boltInstallLocationToggle, tunnelSurface, plane_to_world, -boltTransversiveSpacing / 2);
                                    }
                                    else
                                    {
                                        //Intall the bolts in +t_param direction
                                        InstallBoltCommand.installBoltIteration(doc, apex, tunnel_profile, boltTransversiveSpacing, boltLength, layerIndex, boltInstallLocationToggle, tunnelSurface, plane_to_world, 0);
                                        //Intall the bolts in -t_param direction
                                        InstallBoltCommand.installBoltIteration(doc, apex, tunnel_profile, -boltTransversiveSpacing, boltLength, layerIndex, boltInstallLocationToggle, tunnelSurface, plane_to_world, -boltTransversiveSpacing);
                                    }

                                }
                                else
                                {
                                    //Intall the bolts in +t_param direction
                                    InstallBoltCommand.installBoltIteration(doc, apex, tunnel_profile, boltTransversiveSpacing, boltLength, layerIndex, boltInstallLocationToggle, tunnelSurface, plane_to_world, 0);
                                    //Intall the bolts in -t_param direction
                                    InstallBoltCommand.installBoltIteration(doc, apex, tunnel_profile, -boltTransversiveSpacing, boltLength, layerIndex, boltInstallLocationToggle, tunnelSurface, plane_to_world, -boltTransversiveSpacing);
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
                totalAdvanceLength = totalAdvanceLength + boltLongitudinalSpacing;
            }

            return Result.Success;
        }
    }
}

