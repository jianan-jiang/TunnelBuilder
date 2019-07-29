using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

using MathNet.Numerics.RootFinding;

namespace TunnelBuilder
{
    public class PlaceTunnelProfilesCommand:Command
    {
        public override string EnglishName
        {
            get { return "PlaceTunnelProfiles"; }
        }

        private Dictionary<String,Transform[]> TransformBuffer = new Dictionary<string, Transform[]>();
        private Dictionary<String, List<PolyCurve>> ProfileBuffer = new Dictionary<string, List<PolyCurve>>();

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            var dialog = new Views.LayerNameDialog(doc,"Select control line layer","");
            var dialog_rc = dialog.ShowModal();
            if (dialog_rc != Result.Success)
            {
                return dialog_rc;
            }

            int controlLineLayerIndex = doc.Layers.FindByFullPath(dialog.selectedLayerFullPath, -1);
            if(controlLineLayerIndex<0)
            {
                return Result.Failure;
            }
            var controlineLayer = doc.Layers.FindIndex(controlLineLayerIndex);

            dialog = new Views.LayerNameDialog(doc, "Tunnel profile root layer", "");
            dialog_rc = dialog.ShowModal();
            if (dialog_rc != Result.Success)
            {
                return dialog_rc;
            }

            int profileLayerIndex = doc.Layers.FindByFullPath(dialog.selectedLayerFullPath, -1);
            if(profileLayerIndex<0)
            {
                return Result.Failure;
            }
            var profileLayer = doc.Layers.FindIndex(profileLayerIndex);

            List<Curve> controlLines = getProfilesFromLayer(controlineLayer,doc,Models.ProfileRole.ControlLine);
            List<Curve> leftELines = getProfilesFromLayer(controlineLayer, doc, Models.ProfileRole.LeftELine);
            List<Curve> rightELines = getProfilesFromLayer(controlineLayer, doc, Models.ProfileRole.RightELine);

            iterateProfileLayers(profileLayer, controlLines,leftELines,rightELines, doc);
            clearProfileBuffer(controlLines, leftELines, rightELines, doc);
            return Result.Success;
        }

        public Result clearProfileBuffer(List<Curve> controlLines, List<Curve> leftELines, List<Curve> rightELines, RhinoDoc doc)
        {
            if(ProfileBuffer.Count==0)
            {
                return Result.Success;
            }

            foreach(KeyValuePair<String, List<PolyCurve>> entry in ProfileBuffer)
            {
                string[] tunnelProfileInformation = entry.Key.Split('_');
                if (tunnelProfileInformation.Length != 3)
                {
                    return Result.Failure;
                }
                string tunnelProfileAlignmentName = tunnelProfileInformation[0];
                double tunnelProfileChainage = Double.Parse(tunnelProfileInformation[1]);
                string tunnelProfileRole = tunnelProfileInformation[2];

                foreach(PolyCurve tunnelProfilePolyCurve in entry.Value)
                {
                    List<Curve> curveBuffer = new List<Curve>();
                    for (int i = 0; i < tunnelProfilePolyCurve.SegmentCount; i++)
                    {
                        Curve curveLine = tunnelProfilePolyCurve.SegmentCurve(i);
                        curveBuffer.Add(curveLine);
                    }

                    Curve tunnelProfile = Curve.JoinCurves(curveBuffer)[0];

                    foreach (Curve cL in controlLines)
                    {
                        var tunnelProperty = cL.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                        if (tunnelProperty != null && tunnelProperty.ProfileName == tunnelProfileAlignmentName)
                        {
                            double tunnelProfileLength = cL.GetLength();
                            double offset = tunnelProfileChainage - tunnelProperty.ChainageAtStart;
                            if (offset < 0)
                            {
                                continue;
                            }

                            Curve transformedTunnelProfile = tunnelProfile.DuplicateCurve();
                            Transform[] transforms = TransformBuffer[tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()];
                            if (transforms.Length > 0)
                            {
                                foreach (Transform t in transforms)
                                {
                                    transformedTunnelProfile.Transform(t);
                                }
                            }

                            var tunnelProfileProperty = new Models.TunnelProperty();
                            AreaMassProperties areaMassProperties = AreaMassProperties.Compute(transformedTunnelProfile);
                            if (areaMassProperties != null)
                            {
                                tunnelProfileProperty.Area = AreaMassProperties.Compute(transformedTunnelProfile).Area;
                            }

                            tunnelProfileProperty.ProfileName = tunnelProperty.ProfileName;
                            tunnelProfileProperty.ProfileRole = tunnelProperty.ProfileRole;
                            transformedTunnelProfile.UserData.Add(tunnelProfileProperty);

                            var attributes = new Rhino.DocObjects.ObjectAttributes();
                            int transformedProfileParentLayerIndex = UtilFunctions.AddNewLayer(doc, tunnelProperty.ProfileName, "Transformed Profiles");
                            attributes.LayerIndex = UtilFunctions.AddNewLayer(doc, tunnelProfileRole, transformedProfileParentLayerIndex);
                            doc.Objects.AddCurve(transformedTunnelProfile, attributes);

                        }
                    }


                }
                
            }

            return Result.Success;
        }

        public Result iterateProfileLayers(Rhino.DocObjects.Layer profileLayer,List<Curve> controlLines, List<Curve> leftELines, List<Curve> rightELines,RhinoDoc doc)
        {
            Rhino.DocObjects.Layer[] childrenLayers = profileLayer.GetChildren();
            if (childrenLayers != null)
            {
                for (int i = 0; i < childrenLayers.Length; i++)
                {
                   iterateProfileLayers(childrenLayers[i],  controlLines,leftELines,rightELines, doc);
                }
            }

            string[] tunnelProfileInformation = profileLayer.Name.Split('_');
            if (tunnelProfileInformation.Length != 3)
            {
                return Result.Failure;
            }
            string tunnelProfileAlignmentName = tunnelProfileInformation[0];
            double tunnelProfileChainage = Double.Parse(tunnelProfileInformation[1]);
            string tunnelProfileRole = tunnelProfileInformation[2];

            Rhino.DocObjects.RhinoObject[] curveObjs = doc.Objects.FindByLayer(profileLayer);
            if (curveObjs == null || curveObjs.Length < 1)
            {
                return Result.Failure;
            }

            List<Curve> curveBuffer = new List<Curve>();
            PolyCurve tunnelProfilePolyCurve = new PolyCurve();
            for (int i = 0; i < curveObjs.Length; i++)
            {
                Guid curveId = curveObjs[i].Id;
                Curve curveLine = new Rhino.DocObjects.ObjRef(curveId).Curve();
                tunnelProfilePolyCurve.AppendSegment(curveLine);
                curveBuffer.Add(curveLine);
            }

            Curve tunnelProfile = Curve.JoinCurves(curveBuffer)[0];



            foreach (Curve cL in controlLines)
            {
                var tunnelProperty = cL.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                if (tunnelProperty != null && tunnelProperty.ProfileName == tunnelProfileAlignmentName)
                {
                    double tunnelProfileLength = cL.GetLength();
                    double offset = tunnelProfileChainage - tunnelProperty.ChainageAtStart;
                    if(offset < 0)
                    {
                        continue;
                    }

                    Curve transformedTunnelProfile = tunnelProfile.DuplicateCurve();
                    if(!TransformBuffer.ContainsKey(tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()))
                    {
                        if(tunnelProfileRole=="D-Line")
                        {
                            if(!ProfileBuffer.ContainsKey(profileLayer.Name))
                            {
                                ProfileBuffer[profileLayer.Name] = new List<PolyCurve>();
                                
                            }
                            ProfileBuffer[profileLayer.Name].Add(tunnelProfilePolyCurve);
                            return Result.Failure;
                        }
                        else
                        {
                            TransformBuffer[tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()] = getTranforms(cL, leftELines, rightELines, tunnelProfile, tunnelProfilePolyCurve, offset, tunnelProperty.ProfileName);
                        }  
                    }
                    Transform[] transforms = TransformBuffer[tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()];
                    if (transforms.Length>0)
                    {
                        foreach(Transform t in transforms)
                        {
                            transformedTunnelProfile.Transform(t);
                        }
                    }

                    var tunnelProfileProperty = new Models.TunnelProperty();
                    AreaMassProperties areaMassProperties = AreaMassProperties.Compute(transformedTunnelProfile);
                    if (areaMassProperties != null)
                    {
                        tunnelProfileProperty.Area = AreaMassProperties.Compute(transformedTunnelProfile).Area;
                    }

                    tunnelProfileProperty.ProfileName = tunnelProperty.ProfileName;
                    tunnelProfileProperty.ProfileRole = tunnelProperty.ProfileRole;
                    transformedTunnelProfile.UserData.Add(tunnelProfileProperty);

                    var attributes = new Rhino.DocObjects.ObjectAttributes();
                    int transformedProfileParentLayerIndex = UtilFunctions.AddNewLayer(doc, tunnelProperty.ProfileName, "Transformed Profiles");
                    attributes.LayerIndex = UtilFunctions.AddNewLayer(doc, tunnelProfileRole, transformedProfileParentLayerIndex);
                    doc.Objects.AddCurve(transformedTunnelProfile, attributes);

                }
            }
            return Result.Success;
        }

        private Transform[] getTranforms(Curve cL, List<Curve> leftELines, List<Curve> rightELines,Curve tunnelProfile,PolyCurve tunnelProfilePolyCurve,double offset, string ProfileName)
        {
            List<Transform> resultBuffer= new List<Transform>();
            Plane tunnelProfilePlane = Plane.WorldXY;
            Point3d insertionPoint = cL.PointAtLength(offset);
            double insertionPointTParam;
            cL.ClosestPoint(insertionPoint, out insertionPointTParam, 1);
            Vector3d tangent = cL.TangentAt(insertionPointTParam);
            Vector3d tangentUsedToAlignCPlane = new Vector3d(tangent);
            tangentUsedToAlignCPlane[2] = 0.0;
            Point3d point = cL.PointAt(insertionPointTParam);
            Plane cplane = new Plane(point, tangentUsedToAlignCPlane);

            Curve profileLeftELine = filterProfiles(leftELines, Models.ProfileRole.LeftELine, ProfileName)[0];
            Curve profileRightELine = filterProfiles(rightELines, Models.ProfileRole.RightELine, ProfileName)[0];

            Curve leftELine_plane = profileLeftELine.DuplicateCurve();
            Curve rightELine_plane = profileRightELine.DuplicateCurve();

            var cplane_to_world = Transform.ChangeBasis(cplane, Plane.WorldXY);
            var world_to_cplane = Transform.ChangeBasis(Plane.WorldXY, cplane);

            leftELine_plane.Transform(world_to_cplane);
            rightELine_plane.Transform(world_to_cplane);

            SpanResult sr = ExportTunnelSpanCommand.getSpan(new Point3d(0, 0, 0), leftELine_plane, rightELine_plane, true);
            if (sr.span < 0)
            {
                return null;
            }
            Point3d tunnelHorizontalCentre_cplane = new Point3d(0.5 * (sr.leftIntersection.X + sr.rightIntersection.X), 0, 0);
            Point3d tunnelHorizontalCentre_world = new Point3d(tunnelHorizontalCentre_cplane);
            tunnelHorizontalCentre_world.Transform(cplane_to_world);
            Transform tunnelHorizontalTransform = Transform.Translation(tunnelHorizontalCentre_world - point);

            BoundingBox tunnelProfileBoundingBox = tunnelProfile.GetBoundingBox(true);
            Point3d tunnelProfileBasePoint = new Point3d(0, 0, 0);

            Transform tunnelProfileRotationTransform1 = Transform.Rotation(tunnelProfilePlane.Normal, new Vector3d(1, 0, 0), tunnelProfileBasePoint);
            Transform tunnelProfileRotationTransform2 = Transform.Rotation(new Vector3d(1, 0, 0), cplane.Normal, tunnelProfileBasePoint);
            Transform tunnelProfileMoveTransform = Transform.Translation(point - tunnelProfileBasePoint);
            Curve transformedTunnelProfile = tunnelProfile.DuplicateCurve();
            PolyCurve transformedTunnelProfilePolyCurve = tunnelProfilePolyCurve.DuplicatePolyCurve();



            transformedTunnelProfile.Transform(tunnelProfileRotationTransform1);
            transformedTunnelProfilePolyCurve.Transform(tunnelProfileRotationTransform1);
            transformedTunnelProfile.Transform(tunnelProfileRotationTransform2);
            transformedTunnelProfilePolyCurve.Transform(tunnelProfileRotationTransform2);

            resultBuffer.Add(tunnelProfileRotationTransform1);
            resultBuffer.Add(tunnelProfileRotationTransform2);

            BoundingBox transformedTunnelProfileBoundingBox = transformedTunnelProfile.GetBoundingBox(true);
            if (transformedTunnelProfile.IsClosed)
            {
                Curve[] explodedCurves = transformedTunnelProfilePolyCurve.Explode();
                double radius = 0;
                Arc mainArc = new Arc();
                foreach (Curve c in explodedCurves)
                {
                    double tParam;
                    c.ClosestPoint(c.PointAtNormalizedLength(0.5), out tParam);
                    if (c.IsArc())
                    {
                        Arc arc;
                        c.TryGetArc(out arc);
                        double r = arc.Radius;
                        if (r > radius)
                        {
                            radius = r;
                            mainArc = arc;
                        }
                    }

                }

                RotationFunction rotationFunction = new RotationFunction(mainArc, tunnelProfileBasePoint, cplane);

                double angle = Bisection.FindRoot(rotationFunction.loss, -Math.PI / 2, Math.PI / 2, 0.001, 1000);

                Transform tunnelProfileRotationTransform = Transform.Rotation(angle, cplane.Normal, tunnelProfileBasePoint);
                mainArc.Transform(tunnelProfileRotationTransform);
                transformedTunnelProfile.Transform(tunnelProfileRotationTransform);
                resultBuffer.Add(tunnelProfileRotationTransform);
                transformedTunnelProfileBoundingBox = transformedTunnelProfile.GetBoundingBox(true);
                if (Math.Abs(transformedTunnelProfile.PointAtEnd.Z - transformedTunnelProfileBoundingBox.Min.Z) < Math.Abs(transformedTunnelProfile.PointAtEnd.Z - transformedTunnelProfileBoundingBox.Max.Z))
                {
                    tunnelProfileRotationTransform = Transform.Rotation(Math.PI, cplane.Normal, tunnelProfileBasePoint);
                    transformedTunnelProfile.Transform(tunnelProfileRotationTransform);
                    resultBuffer.Add(tunnelProfileRotationTransform);
                }
            }

            transformedTunnelProfile.Transform(tunnelProfileMoveTransform);
            resultBuffer.Add(tunnelProfileMoveTransform);

            return resultBuffer.ToArray();
            
        }

        public List<Curve> filterProfiles(List<Curve> profiles, Models.ProfileRole role,string profileName)
        {
            List<Curve> curveList = new List<Curve>();
            for (int i = 0; i < profiles.Count; i++)
            {
               var tunnelProperty = profiles[i].UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
               if (tunnelProperty == null)
               {
                  continue;
               }
               if (tunnelProperty.ProfileRole == Models.TunnelProperty.ProfileRoleNameDictionary[role] && tunnelProperty.ProfileName==profileName)
               {
                  curveList.Add(profiles[i]);
               }
            }
            return curveList;
        }

        public List<Curve> getProfilesFromLayer(Rhino.DocObjects.Layer controlLineLayer, RhinoDoc doc, Models.ProfileRole role)
        {
            List<Curve> curveList = new List<Curve>();
            Rhino.DocObjects.Layer[] childrenLayers = controlLineLayer.GetChildren();
            if (childrenLayers != null)
            {
                for (int i = 0; i < childrenLayers.Length; i++)
                {
                    var child_result = getProfilesFromLayer(childrenLayers[i], doc,role);
                    curveList.AddRange(child_result);
                }
            }
            Rhino.DocObjects.RhinoObject[] curveObjs = doc.Objects.FindByLayer(controlLineLayer);
            if (curveObjs == null || curveObjs.Length < 1)
            {
                return curveList;
            }

            for (int i = 0; i < curveObjs.Length; i++)
            {
                Guid curveId = curveObjs[i].Id;
                Curve curveLine = new Rhino.DocObjects.ObjRef(curveId).Curve();
                if (curveLine != null)
                {
                    var tunnelProperty = curveLine.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                    if (tunnelProperty == null)
                    {
                        continue;
                    }
                    if(tunnelProperty.ProfileRole == Models.TunnelProperty.ProfileRoleNameDictionary[role])
                    {
                        curveList.Add(curveLine);
                    }
                }
            }

            return curveList;
        }
    }

    public class RotationFunction
    {
        Arc Arc;
        Point3d RotationBasePoint;
        Vector3d axis;

        public RotationFunction(Arc arc, Point3d rotationBasePoint,Plane plane)
        {
            Arc = arc;
            RotationBasePoint = rotationBasePoint;
            axis = plane.Normal;
        }

        public double loss(double angle)
        {
            Point3d startPoint = new Point3d(Arc.StartPoint);
            Point3d endPoint = new Point3d(Arc.EndPoint);

            Transform rotationTransform = Transform.Rotation(angle, axis, RotationBasePoint);
            startPoint.Transform(rotationTransform);
            endPoint.Transform(rotationTransform);
            return Math.Pow(startPoint.Z - endPoint.Z, 1);
        }
    }

}
