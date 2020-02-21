using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("7E89AE64-4EB7-48B0-BE49-891B7FCA97C5")]
    public class TunnelBuilderCommand : Command
    {

        ///<summary>The only instance of this command.</summary>
        public static TunnelBuilderCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "BuildTunnel"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("The {0} command will build a tunnel surface.", EnglishName);
            OptionInteger intOption = new OptionInteger(10, 1, 1000);
            OptionDouble leftShoulderDoubleOption = new OptionDouble(1.25);
            OptionDouble rightShoulderDoubleOption = new OptionDouble(1.25);
            OptionToggle toggle = new OptionToggle(false, "Off", "On");
            OptionToggle keepsectionToggle = new OptionToggle(true, "No", "Yes");
            OptionToggle sectionAngleToggle = new OptionToggle(true, "VerticalToHorizon", "PerpendicularToControlLine");
            OptionToggle normalToggle = new OptionToggle(false, "U", "V");
            Curve controlLine = null;
            Curve leftShoulder = null;
            Curve rightShoulder = null;
            using (Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select tunnel control lines");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                go.AddOptionInteger("NoOfSections", ref intOption);
                go.AddOptionToggle("UseShoulderProfiles", ref toggle);
                go.AddOptionToggle("KeepSectionCurves", ref keepsectionToggle);
                go.AddOptionToggle("SectionAngle", ref sectionAngleToggle);
                go.AddOptionToggle("NormalDirection", ref normalToggle);
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
                
                if(controlLine == null)
                {
                    RhinoApp.WriteLine("No control line was selected");
                    return Rhino.Commands.Result.Failure;
                }
            }

            if(toggle.CurrentValue)
            {
                //Get shoulder profiles if enabled
                //Get left shoulder
                using (GetObject go = new GetObject())
                {
                    go.DisablePreSelect();
                    go.SetCommandPrompt("Select left shoulder profile");
                    go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                    go.AddOptionDouble("LeftShoulderPadding", ref leftShoulderDoubleOption);
                    while(true)
                    {
                        GetResult get_rc = go.GetMultiple(1, 0);
                        if(get_rc == GetResult.Object)
                        {
                            leftShoulder = go.Object(0).Geometry() as Curve;
                            if(leftShoulder == null)
                            {
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
                //Get right shoulder
                using (GetObject go = new GetObject())
                {
                    go.DisablePreSelect();
                    go.SetCommandPrompt("Select right shoulder profile");
                    go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                    go.AddOptionDouble("RightShoulderPadding", ref rightShoulderDoubleOption);
                    while (true)
                    {
                        GetResult get_rc = go.GetMultiple(1, 0);
                        if (get_rc == GetResult.Object)
                        {
                            rightShoulder = go.Object(0).Geometry() as Curve;
                            if (rightShoulder == null)
                            {
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

                if (!Curve.DoDirectionsMatch(leftShoulder, controlLine))
                {
                    controlLine.Reverse();
                }

                if (!Curve.DoDirectionsMatch(leftShoulder,rightShoulder))
                {
                    rightShoulder.Reverse();
                }

            }

            Curve tunnelSection;
            using (Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select tunnel section profile");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                go.GeometryAttributeFilter = Rhino.Input.Custom.GeometryAttributeFilter.ClosedCurve;
                go.GetMultiple(1, -1);
                if (go.CommandResult() != Rhino.Commands.Result.Success)
                {
                    return go.CommandResult();
                }
                if (go.ObjectCount != 1)
                {
                    return Rhino.Commands.Result.Failure;
                }
                tunnelSection = go.Object(0).Geometry() as Curve;
                if (tunnelSection == null)
                {
                    return Rhino.Commands.Result.Failure;
                }
                
            }

            Point3d tunnelSectionControlPoint;
            using (Rhino.Input.Custom.GetPoint getPointAction = new GetPoint())
            {
                if (toggle.CurrentValue)
                {
                    getPointAction.SetCommandPrompt("Please select the middle point along the control elevation");
                }
                else
                {
                    getPointAction.SetCommandPrompt("Please select the CL point");
                }
                
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No end point was selected.");
                    return getPointAction.CommandResult();
                }
                tunnelSectionControlPoint = getPointAction.Point();
            }

            bool vertical = false;
            var rc = RhinoGet.GetBool("Place profiles", false, "PerpendicularToControlLine", "Vertically", ref vertical);
            if (rc != Result.Success)
            {
                return rc;
            }

            //Rotate tunnel section perpendicular to control line
            Rhino.Geometry.Plane tunnelSectionPlane;
            if(!tunnelSection.TryGetPlane(out tunnelSectionPlane,0.01))
            {
                RhinoApp.WriteLine("Tunnel section must be planar");
                return Rhino.Commands.Result.Failure;
            }


            //Divide the control line into n sections
            int n = intOption.CurrentValue;
            Interval t = controlLine.Domain;
            Interval left_t;
            Interval right_t;
            Guid[] section_guids = { };
            Curve[] section_curves = { };
            for(int i=0; i<n+1;i++)
            {
                double x = (double)i/n * (t[1] - t[0]) + t[0];
                Vector3d tangent = controlLine.TangentAt(x);
                Vector3d tangentUsedToAlignControlLine = new Vector3d(tangent);
                if(vertical)
                {
                    tangentUsedToAlignControlLine[2] = 0.0;
                }
                
                Point3d point = controlLine.PointAt(x);
                var geometryBase = tunnelSection.Duplicate();
                Transform xform;
                Transform rotate;
                Transform rotateToPerpendicular;
                if (!toggle.CurrentValue)
                {
                    xform = Transform.Translation(point - tunnelSectionControlPoint);
                    if (!normalToggle.CurrentValue)
                    {
                        rotate = Transform.Rotation(tunnelSectionPlane.Normal, tangentUsedToAlignControlLine, tunnelSectionControlPoint);
                    }
                    else
                    {
                        rotate = Transform.Rotation(-tunnelSectionPlane.Normal, tangentUsedToAlignControlLine, tunnelSectionControlPoint);
                    }
                    rotateToPerpendicular = Transform.Rotation(tangentUsedToAlignControlLine, tangent, point);
                }
                else
                {
                    left_t = leftShoulder.Domain;
                    right_t = rightShoulder.Domain;
                    double left_x = (double)i / n * (left_t[1] - left_t[0]) + left_t[0];
                    Vector3d leftShoulderTangent = leftShoulder.TangentAt(left_x);
                    Point3d leftPoint = leftShoulder.PointAt(left_x);

                    double right_x = (double)i / n * (right_t[1] - right_t[0]) + right_t[0];
                    Vector3d rightShoulderTangent = rightShoulder.TangentAt(right_x);
                    Point3d rightPoint = rightShoulder.PointAt(right_x);

                    double distanceBetweenShoulders = leftPoint.DistanceTo(rightPoint);
                    double widthOfTunnel = distanceBetweenShoulders+leftShoulderDoubleOption.CurrentValue+rightShoulderDoubleOption.CurrentValue;

                    Point3d midPointBetweenShoulders = (leftPoint + rightPoint)/2.0;

                    xform = Transform.Translation(midPointBetweenShoulders - tunnelSectionControlPoint);
                    rotate = Transform.Rotation(tunnelSectionPlane.Normal, tangent, tunnelSectionControlPoint);
                    rotateToPerpendicular = Transform.Rotation(tangentUsedToAlignControlLine, tangent, point);
                }
                Guid guid = doc.Objects.Add(geometryBase);
                if(guid == Guid.Empty)
                {
                    return Rhino.Commands.Result.Failure;
                }
                guid = doc.Objects.Transform(guid, rotate, true);
                guid = doc.Objects.Transform(guid, xform,true);
                if (sectionAngleToggle.CurrentValue)
                {
                    guid = doc.Objects.Transform(guid, rotateToPerpendicular, true);
                }
                // Close the curve if it's been opened due to tolerance precision
                var sc_ref = new Rhino.DocObjects.ObjRef(guid);
                var sc = sc_ref.Curve();
                if(!sc.IsClosed)
                {
                    //Curve l = new Line(sc.PointAtStart, sc.PointAtEnd).ToNurbsCurve();
                    //Curve[] result = Curve.JoinCurves(new Curve[] { sc, l });
                    //sc = result[0];
                    //if(result.Length!=1)
                    //{
                    //    return Result.Failure;
                    //}
                    //doc.Objects.Replace(sc_ref, sc);
                    doc.Objects.Delete(sc_ref.Object());
                }
                else
                {
                    Array.Resize(ref section_guids, section_guids.Length + 1);
                    Array.Resize(ref section_curves, section_curves.Length + 1);

                    section_guids[section_guids.Length - 1] = guid;
                    section_curves[section_curves.Length - 1] = new Rhino.DocObjects.ObjRef(guid).Curve();
                }
             
                
            }
            
            //Create Loft Extrusion
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
            


            if (keepsectionToggle.CurrentValue)
            {
                int section_curves_layer_index = UtilFunctions.AddNewLayer(doc, brep_guid.ToString());
                if(section_curves_layer_index>0)
                {
                    foreach (var sg in section_guids)
                    {
                        var sc = new Rhino.DocObjects.ObjRef(sg).Object();
                        sc.Attributes.LayerIndex = section_curves_layer_index;
                        sc.CommitChanges();
                    }
                }
                
            }
            else
            {
                foreach (var sg in section_guids)
                {
                    doc.Objects.Delete(sg,true);
                }
                
            }

            doc.Views.Redraw();
            return Result.Success;

        }

    }
}
