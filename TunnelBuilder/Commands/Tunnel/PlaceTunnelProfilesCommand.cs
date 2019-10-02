using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

using MathNet.Numerics.RootFinding;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("47306683-E908-48F3-A810-91B6EF965331")]
    public class PlaceTunnelProfilesCommand:Command
    {
        public override string EnglishName
        {
            get { return "PlaceTunnelProfiles"; }
        }

        private Dictionary<String,Transform[]> TransformBuffer = new Dictionary<string, Transform[]>();
        private Dictionary<String, List<PolyCurve>> ProfileBuffer = new Dictionary<string, List<PolyCurve>>();
        private Dictionary<String, Dictionary<string, Point3d[]>> ELineEdgePointsBuffer = new Dictionary<String, Dictionary<string, Point3d[]>>();
        public Dictionary<string, List<ControlLine>> ControlLinesDictionary = new Dictionary<string, List<ControlLine>>();
        private Dictionary<string, Dictionary<string, Dictionary<double, List<Curve>>>> ProfileDictionary = new Dictionary<string, Dictionary<string, Dictionary<double, List<Curve>>>>();
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            TransformBuffer = new Dictionary<string, Transform[]>();
            ProfileBuffer = new Dictionary<string, List<PolyCurve>>();
            ELineEdgePointsBuffer = new Dictionary<String, Dictionary<string, Point3d[]>>();
            ProfileDictionary = new Dictionary<string, Dictionary<string, Dictionary<double, List<Curve>>>>();
            ControlLinesDictionary = new Dictionary<string, List<ControlLine>>();
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

            bool flip = false;
            var rc = RhinoGet.GetBool("Flip profiles?", false, "No", "Yes", ref flip);
            if (rc != Result.Success)
            {
                return rc;
            }

            ControlLinesDictionary = getProfileDictionaryFromLayer(controlineLayer,doc,Models.ProfileRole.ControlLine);

            var controlLinesNameList = ControlLinesDictionary.Keys;
            foreach(var alignment in controlLinesNameList)
            {
                ELineEdgePointsBuffer[alignment] = new Dictionary<string, Point3d[]>();
            }

            UtilFunctions.AddNewLayer(doc, "Tunnels");

            iterateProfileLayers(profileLayer, ControlLinesDictionary, doc,flip);
            clearProfileBuffer(ControlLinesDictionary, doc);
            createSweep(doc);

            doc.Views.Redraw();

            return Result.Success;
        }

        public Result createSweep(RhinoDoc doc)
        {
            List<string> alignmentList = ProfileDictionary.Keys.ToList();
            foreach(string alignment in alignmentList)
            {
                List<string> profileRoleList = ProfileDictionary[alignment].Keys.ToList();
                foreach(string profileRole in profileRoleList)
                {
                    var sweepOneD = new SweepOneRail();
                    sweepOneD.GlobalShapeBlending = true;
                    List<Guid> brepBuffer = new List<Guid>();

                    List<double> chainageList = ProfileDictionary[alignment][profileRole].Keys.ToList();
                    chainageList.Sort();
                    Curve[] crossSections = new Curve[2];
                    if(ProfileDictionary[alignment][profileRole][chainageList[0]].Count>1)
                    {
                        continue;
                    }
                    var profile1 = ProfileDictionary[alignment][profileRole][chainageList[0]][0];
                    var profile2 = ProfileDictionary[alignment][profileRole][chainageList[0]][0];
                    for (int i = 0; i < chainageList.Count - 1; i++)
                    {
                        if (ProfileDictionary[alignment][profileRole][chainageList[i]].Count > 1)
                        {
                            profile1 = ProfileDictionary[alignment][profileRole][chainageList[i]][ProfileDictionary[alignment][profileRole][chainageList[i]].Count - 1];
                        }
                        else
                        {
                            profile1 = profile2;
                        }
                        profile2 = ProfileDictionary[alignment][profileRole][chainageList[i + 1]][0];

                        var cL = getControlLine(ControlLinesDictionary, alignment, chainageList[i]);

                        crossSections[0] = profile1;
                        crossSections[1] = profile2;

                        var breps = sweepOneD.PerformSweep(cL.Profile, crossSections);
                        for (int j = 0; j < breps.Length; j++)
                        {
                            var tunnelSurfaceProperty = new Models.TunnelProperty();

                            tunnelSurfaceProperty.ProfileName = alignment;
                            tunnelSurfaceProperty.ProfileRole = Models.TunnelProperty.ProfileRoleNameDictionary[Models.ProfileRole.ELineSurface];
                            tunnelSurfaceProperty.Span = cL.Profile.GetLength();
                            breps[j].UserData.Add(tunnelSurfaceProperty);

                            var attributes = new Rhino.DocObjects.ObjectAttributes();
                            var parentLayerIndex = UtilFunctions.AddNewLayer(doc, alignment, "Tunnels");
                            attributes.LayerIndex = UtilFunctions.AddNewLayer(doc, profileRole, parentLayerIndex);

                            var id = doc.Objects.AddBrep(breps[j], attributes);
                            brepBuffer.Add(id);

                        }

                    }
                    doc.Groups.Add(alignment+"_"+profileRole, brepBuffer);

                }
            }

            return Result.Success;
        }

        public Result clearProfileBuffer(Dictionary<string,List<ControlLine>> controlLineProfileDictionary, RhinoDoc doc)
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
                    ControlLine cL = getControlLine(controlLineProfileDictionary, tunnelProfileAlignmentName, tunnelProfileChainage);
                    var tunnelProperty = cL.Profile.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                    double tunnelProfileLength = cL.Profile.GetLength();
                    double offset = tunnelProfileChainage - tunnelProperty.ChainageAtStart;
                    if (offset < 0)
                    {
                        continue;
                    }
                    Transform[] transforms = TransformBuffer[tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()];
                    transformTunnelProfile(tunnelProfile, transforms, tunnelProperty, tunnelProfileRole, doc,tunnelProfileChainage);
                }
                
            }

            return Result.Success;
        }

        public Result iterateProfileLayers(Rhino.DocObjects.Layer profileLayer,Dictionary<string,List<ControlLine>> controlLineProfileDictionary, RhinoDoc doc,bool flip)
        {
            Rhino.DocObjects.Layer[] childrenLayers = profileLayer.GetChildren();
            if (childrenLayers != null)
            {
                for (int i = 0; i < childrenLayers.Length; i++)
                {
                   iterateProfileLayers(childrenLayers[i],  controlLineProfileDictionary, doc,flip);
                }
            }

            string[] tunnelProfileInformation = profileLayer.Name.Split('_');

            if(tunnelProfileInformation.Count()<3)
            {
                return Result.Nothing;
            }

            string tunnelProfileAlignmentName = tunnelProfileInformation[0];
            double tunnelProfileChainage = Double.Parse(tunnelProfileInformation[1]);
            string tunnelProfileRole = String.Join("",tunnelProfileInformation.Skip(2));

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

            ControlLine cL = getControlLine(controlLineProfileDictionary, tunnelProfileAlignmentName, tunnelProfileChainage);
            var tunnelProperty = cL.Profile.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
            double tunnelProfileLength = cL.Profile.GetLength();

            Curve transformedTunnelProfile = tunnelProfile.DuplicateCurve();
            if (!TransformBuffer.ContainsKey(tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()))
            {
                if (tunnelProfileRole == "D-Line")
                {
                    if (!ProfileBuffer.ContainsKey(profileLayer.Name))
                    {
                        ProfileBuffer[profileLayer.Name] = new List<PolyCurve>();

                    }
                    ProfileBuffer[profileLayer.Name].Add(tunnelProfilePolyCurve);
                    return Result.Failure;
                }
                else
                {
                    TransformBuffer[tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()] = getTranforms(cL, tunnelProfilePolyCurve, tunnelProfileChainage, tunnelProperty.ProfileName,flip);
                }
            }

            Transform[] transforms = TransformBuffer[tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()];
            return transformTunnelProfile(tunnelProfilePolyCurve,transforms,tunnelProperty,tunnelProfileRole,doc,tunnelProfileChainage);
        }

        public Result transformTunnelProfile(PolyCurve tunnelProfile, Transform[] transforms, Models.TunnelProperty controlLineProperty, string tunnelProfileRole, RhinoDoc doc,double chainage)
        {
            Curve tunnelProfileCurve = GetCurve(tunnelProfile);
            return transformTunnelProfile(tunnelProfileCurve, transforms, controlLineProperty, tunnelProfileRole, doc,chainage);
        }

        public static Curve GetCurve(PolyCurve polyCurve)
        {
            List<Curve> curveBuffer = new List<Curve>();
            for (int i = 0; i < polyCurve.SegmentCount; i++)
            {
                Curve curveLine = polyCurve.SegmentCurve(i);
                curveBuffer.Add(curveLine);
            }
            Curve joinedCurve = Curve.JoinCurves(curveBuffer)[0];
            if(joinedCurve != null)
            {
                return joinedCurve;
            }
            return null;
        }

        public Result transformTunnelProfile(Curve tunnelProfile, Transform[] transforms, Models.TunnelProperty controlLineProperty, string tunnelProfileRole, RhinoDoc doc,double chainage)
        {
            Curve transformedTunnelProfile = tunnelProfile.DuplicateCurve();
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

            tunnelProfileProperty.ProfileName = controlLineProperty.ProfileName;
            tunnelProfileProperty.ProfileRole = tunnelProfileRole;
            tunnelProfileProperty.ChainageAtStart = chainage;
            tunnelProfileProperty.Span = ExportTunnelSpanCommand.getSpan(doc, tunnelProfile);
            transformedTunnelProfile.UserData.Add(tunnelProfileProperty);

            var attributes = new Rhino.DocObjects.ObjectAttributes();
            int transformedProfileParentLayerIndex = UtilFunctions.AddNewLayer(doc, controlLineProperty.ProfileName, "Transformed Profiles");
            attributes.LayerIndex = UtilFunctions.AddNewLayer(doc, tunnelProfileRole, transformedProfileParentLayerIndex);
            doc.Objects.AddCurve(transformedTunnelProfile, attributes);

            if(!ProfileDictionary.ContainsKey(controlLineProperty.ProfileName))
            {
                ProfileDictionary[controlLineProperty.ProfileName] = new Dictionary<string, Dictionary<double, List<Curve>>>();
            }

            if (!ProfileDictionary[controlLineProperty.ProfileName].ContainsKey(tunnelProfileRole))
            {
                ProfileDictionary[controlLineProperty.ProfileName][tunnelProfileRole] = new Dictionary<double, List<Curve>>();
            }

            if (!ProfileDictionary[controlLineProperty.ProfileName][tunnelProfileRole].ContainsKey(chainage))
            {
                ProfileDictionary[controlLineProperty.ProfileName][tunnelProfileRole][chainage] = new List<Curve>();
            }

            ProfileDictionary[controlLineProperty.ProfileName][tunnelProfileRole][chainage].Add(transformedTunnelProfile);

            return Result.Success;
        }

        public Transform[] getTranforms(ControlLine cL, PolyCurve tunnelProfile, double chainage, string alignmentName, bool flip = false)
        {
            List<Transform> resultBuffer = new List<Transform>();
            Point3d insertionPoint = cL.GetPointAtChainage(chainage);
            double insertionPointTParam;
            cL.Profile.ClosestPoint(insertionPoint, out insertionPointTParam, 1);
            Vector3d tangent = cL.Profile.TangentAt(insertionPointTParam);
            Vector3d tangentUsedToAlignCPlane = new Vector3d(tangent);
            tangentUsedToAlignCPlane[2] = 0.0;
            Point3d point = cL.Profile.PointAt(insertionPointTParam);

            Plane cplane = new Plane(point, tangentUsedToAlignCPlane);
            if (cplane.YAxis[2] < 0)
            {
                //Rotate the plane 180 degree if y axis is pointing down
                cplane.Rotate(Math.PI, cplane.ZAxis);
            }


            var cplane_to_world = Transform.ChangeBasis(cplane, Plane.WorldXY);


            Transform tunnelProfileMoveTransform = cplane_to_world;

            if (flip)
            {
                Transform flipTransform = Transform.Rotation(Math.PI, new Vector3d(0, 1, 0), new Point3d(0, 0, 0));
                resultBuffer.Add(flipTransform);
            }

            resultBuffer.Add(tunnelProfileMoveTransform);

            return resultBuffer.ToArray();
            
        }

        private Point3d transformInterpolatedPoint(Point3d startPoint,Point3d endPoint,double chainage,string alignmentName, double startChaiange,double endChaiange,bool flip)
        {
            ControlLine cL = getControlLine(ControlLinesDictionary, alignmentName, chainage);
            Point3d insertionPoint = cL.GetPointAtChainage(chainage);
            double insertionPointTParam;
            cL.Profile.ClosestPoint(insertionPoint, out insertionPointTParam, 1);
            Vector3d tangent = cL.Profile.TangentAt(insertionPointTParam);
            Vector3d tangentUsedToAlignCPlane = new Vector3d(tangent);
            tangentUsedToAlignCPlane[2] = 0.0;
            Point3d point = cL.Profile.PointAt(insertionPointTParam);

            Plane cplane = new Plane(point, tangentUsedToAlignCPlane);
            if (cplane.YAxis[2] < 0)
            {
                //Rotate the plane 180 degree if y axis is pointing down
                cplane.Rotate(Math.PI, cplane.ZAxis);
            }


            var cplane_to_world = Transform.ChangeBasis(cplane, Plane.WorldXY);
            double xLeft = startPoint.X + (endPoint.X - startPoint.X) / (endChaiange - startChaiange) * (chainage - startChaiange);
            double yLeft = startPoint.Y + (endPoint.Y - startPoint.Y) / (endChaiange - startChaiange) * (chainage - startChaiange);
            Point3d transfomredPoint = new Point3d(xLeft, yLeft, 0);

            if (flip)
            {
                Transform flipTransform = Transform.Rotation(Math.PI, new Vector3d(0, 1, 0), new Point3d(0, 0, 0));
                transfomredPoint.Transform(flipTransform);
            }

            transfomredPoint.Transform(cplane_to_world);
            return transfomredPoint;
        }

        public Result DrawELineEdgeCurves(RhinoDoc doc,string alignmentName,bool flip=false)
        {
            var chaiange_list = ELineEdgePointsBuffer[alignmentName].Keys.ToList();
            chaiange_list.Sort();

            for(int i=0;i<chaiange_list.Count-1;i++)
            {
                var leftPointList = new List<Point3d>();
                var rightPointList = new List<Point3d>();

                Point3d startLeftPoint = ELineEdgePointsBuffer[alignmentName][chaiange_list[i]][0];
                Point3d startRightPoint = ELineEdgePointsBuffer[alignmentName][chaiange_list[i]][1];
                Point3d endLeftPoint = ELineEdgePointsBuffer[alignmentName][chaiange_list[i+1]][0];
                Point3d endRightPoint = ELineEdgePointsBuffer[alignmentName][chaiange_list[i+1]][1];

                double startChaiange = Double.Parse(chaiange_list[i]);
                double endChainage = Double.Parse(chaiange_list[i + 1]);

                double currentChainage = startChaiange;

                while(currentChainage<endChainage)
                {

                    Point3d leftPoint = transformInterpolatedPoint(startLeftPoint, endLeftPoint, currentChainage, alignmentName, startChaiange, endChainage, flip);
                    Point3d rightPoint = transformInterpolatedPoint(startRightPoint, endRightPoint, currentChainage, alignmentName, startChaiange, endChainage, flip);
                    leftPointList.Add(leftPoint);
                    rightPointList.Add(rightPoint);
                    currentChainage = currentChainage + 1;
                }

                endLeftPoint = transformInterpolatedPoint(startLeftPoint, endLeftPoint, endChainage, alignmentName, startChaiange, endChainage, flip);
                endRightPoint = transformInterpolatedPoint(startRightPoint, endRightPoint, endChainage, alignmentName, startChaiange, endChainage, flip);
                leftPointList.Add(endLeftPoint);
                rightPointList.Add(endRightPoint);

                var left_curve = Curve.CreateInterpolatedCurve(leftPointList, 3);
                var right_curve = Curve.CreateInterpolatedCurve(rightPointList, 3);

                var attributes = new Rhino.DocObjects.ObjectAttributes();
                int transformedProfileParentLayerIndex = UtilFunctions.AddNewLayer(doc, alignmentName, "Transformed Profiles");
                attributes.LayerIndex = transformedProfileParentLayerIndex;

                doc.Objects.Add(left_curve,attributes);
                doc.Objects.Add(right_curve,attributes);
            }
            return Result.Success;
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

        public static Dictionary<string,List<ControlLine>> getProfileDictionaryFromLayer(Rhino.DocObjects.Layer controlLineLayer, RhinoDoc doc, Models.ProfileRole role)
        {
            var result = new Dictionary<string, List<ControlLine>>();
            Rhino.DocObjects.Layer[] childrenLayers = controlLineLayer.GetChildren();
            if (childrenLayers != null)
            {
                for (int i = 0; i < childrenLayers.Length; i++)
                {
                    var child_result = getProfileDictionaryFromLayer(childrenLayers[i], doc, role);
                    foreach(KeyValuePair<string,List<ControlLine>> controLineKeyValuePair in child_result)
                    {
                        result[controLineKeyValuePair.Key].Concat(controLineKeyValuePair.Value);
                    }
                }
            }
            Rhino.DocObjects.RhinoObject[] curveObjs = doc.Objects.FindByLayer(controlLineLayer);
            if (curveObjs == null || curveObjs.Length < 1)
            {
                return result;
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
                    if (tunnelProperty.ProfileRole == Models.TunnelProperty.ProfileRoleNameDictionary[role])
                    {
                        List<ControlLine> curveList = new List<ControlLine>();
                        if(!result.TryGetValue(tunnelProperty.ProfileName,out curveList))
                        {
                            result[tunnelProperty.ProfileName] = new List<ControlLine>();
                        }
                        result[tunnelProperty.ProfileName].Add(new ControlLine(curveLine));
                    }
                }
            }
            return result;
        }

        public ControlLine getControlLine(Dictionary<string, List<ControlLine>> profileDictionary, string profileName, double chainage)
        {
            if(!profileDictionary.ContainsKey(profileName))
            {
                return null;
            }
            var profiles = profileDictionary[profileName];
            foreach(var p in profiles)
            {
                if(p.ChainageInterval.Contains(chainage))
                {
                    return p;
                }
            }
            return null;
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

    public class ControlLine
    {
        Curve TwoDCurve;
        BoundingBox ThreeDCurveBoundingBox;

        public UtilFunctions.Interval<double> ChainageInterval;
        public double IntersectionTolerance = 0.001;
        public double OverlapTolerance = 0.001;

        public Curve Profile { get; }
        public ControlLine(Curve curve)
        {
            var tunnelProperty = curve.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
            if(tunnelProperty==null)
            {
                throw new ArgumentException();
            }
            Profile = curve;
            TwoDCurve = Curve.ProjectToPlane(Profile, Plane.WorldXY);
            ThreeDCurveBoundingBox = Profile.GetBoundingBox(true);

            ChainageInterval = UtilFunctions.Interval.Range(tunnelProperty.ChainageAtStart, tunnelProperty.ChainageAtStart + TwoDCurve.GetLength(), UtilFunctions.IntervalType.Closed, UtilFunctions.IntervalType.Open);
        }

        public Point3d GetPointAtChainage(double chaiange)
        {
            if(!ChainageInterval.Contains(chaiange))
            {
                return Point3d.Unset;
            }
            double offset = chaiange - ChainageInterval.LowerBound;
            
            Point3d twoDPoint = TwoDCurve.PointAtLength(offset);
            Line verticalLine = new Line(new Point3d(twoDPoint.X, twoDPoint.Y, ThreeDCurveBoundingBox.Max.Z + 10), new Point3d(twoDPoint.X, twoDPoint.Y, ThreeDCurveBoundingBox.Min.Z - 10));
            var result = Rhino.Geometry.Intersect.Intersection.CurveCurve(Profile, verticalLine.ToNurbsCurve(), IntersectionTolerance, OverlapTolerance);
            if(result.Count > 0)
            {
                return result[0].PointA;
            }
            else
            {
                return Point3d.Unset;
            }
        }
    }

}
