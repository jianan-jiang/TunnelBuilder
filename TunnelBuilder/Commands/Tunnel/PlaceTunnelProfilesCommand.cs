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

            var controlLinesDictionary = getProfileDictionaryFromLayer(controlineLayer,doc,Models.ProfileRole.ControlLine);

            iterateProfileLayers(profileLayer, controlLinesDictionary, doc);
            clearProfileBuffer(controlLinesDictionary, doc);
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
                    transformTunnelProfile(tunnelProfile, transforms, tunnelProperty, tunnelProfileRole, doc);
                }
                
            }

            return Result.Success;
        }

        public Result iterateProfileLayers(Rhino.DocObjects.Layer profileLayer,Dictionary<string,List<ControlLine>> controlLineProfileDictionary, RhinoDoc doc)
        {
            Rhino.DocObjects.Layer[] childrenLayers = profileLayer.GetChildren();
            if (childrenLayers != null)
            {
                for (int i = 0; i < childrenLayers.Length; i++)
                {
                   iterateProfileLayers(childrenLayers[i],  controlLineProfileDictionary, doc);
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
                    TransformBuffer[tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()] = getTranforms(cL, tunnelProfilePolyCurve, tunnelProfileChainage, tunnelProperty.ProfileName);
                }
            }
            Transform[] transforms = TransformBuffer[tunnelProfileAlignmentName + "_" + tunnelProfileChainage.ToString()];
            return transformTunnelProfile(tunnelProfile,transforms,tunnelProperty,tunnelProfileRole,doc);
        }

        public static Result transformTunnelProfile(PolyCurve tunnelProfile, Transform[] transforms, Models.TunnelProperty controlLineProperty, string tunnelProfileRole, RhinoDoc doc)
        {
            Curve tunnelProfileCurve = GetCurve(tunnelProfile);
            return transformTunnelProfile(tunnelProfileCurve, transforms, controlLineProperty, tunnelProfileRole, doc);
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

        public static Result transformTunnelProfile(Curve tunnelProfile, Transform[] transforms, Models.TunnelProperty controlLineProperty, string tunnelProfileRole, RhinoDoc doc)
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
            transformedTunnelProfile.UserData.Add(tunnelProfileProperty);

            var attributes = new Rhino.DocObjects.ObjectAttributes();
            int transformedProfileParentLayerIndex = UtilFunctions.AddNewLayer(doc, controlLineProperty.ProfileName, "Transformed Profiles");
            attributes.LayerIndex = UtilFunctions.AddNewLayer(doc, tunnelProfileRole, transformedProfileParentLayerIndex);
            doc.Objects.AddCurve(transformedTunnelProfile, attributes);
            return Result.Success;
        }

        public static Transform[] getTranforms(ControlLine cL, PolyCurve tunnelProfile,double chainage, string ProfileName,bool flip=false)
        {
            List<Transform> resultBuffer= new List<Transform>();
            Plane tunnelProfilePlane = Plane.WorldXY;
            Point3d insertionPoint = cL.GetPointAtChainage(chainage);
            double insertionPointTParam;
            cL.Profile.ClosestPoint(insertionPoint, out insertionPointTParam, 1);
            Vector3d tangent = cL.Profile.TangentAt(insertionPointTParam);
            Vector3d tangentUsedToAlignCPlane = new Vector3d(tangent);
            tangentUsedToAlignCPlane[2] = 0.0;
            Point3d point = cL.Profile.PointAt(insertionPointTParam);
            Plane cplane = new Plane(point, tangentUsedToAlignCPlane);


            var cplane_to_world = Transform.ChangeBasis(cplane, Plane.WorldXY);
            var world_to_cplane = Transform.ChangeBasis(Plane.WorldXY, cplane);

            Curve tunnelProfileCurve = GetCurve(tunnelProfile);

            BoundingBox tunnelProfileBoundingBox = tunnelProfileCurve.GetBoundingBox(true);
            Point3d tunnelProfileBasePoint = new Point3d(0, 0, 0);

            Transform tunnelProfileRotationTransform1 = Transform.Rotation(tunnelProfilePlane.Normal, new Vector3d(1, 0, 0), tunnelProfileBasePoint);
            Transform tunnelProfileRotationTransform2 = Transform.Rotation(new Vector3d(1, 0, 0), cplane.Normal, tunnelProfileBasePoint);
            Transform tunnelProfileMoveTransform = Transform.Translation(point - tunnelProfileBasePoint);
            Curve transformedTunnelProfile = tunnelProfileCurve.DuplicateCurve();
            PolyCurve transformedTunnelProfilePolyCurve = tunnelProfile.DuplicatePolyCurve();

            if (flip)
            {
                Transform flipTransform = Transform.Rotation(Math.PI, new Vector3d(0, 1, 0), new Point3d(0, 0, 0));

                transformedTunnelProfile.Transform(flipTransform);
                transformedTunnelProfilePolyCurve.Transform(flipTransform);

                resultBuffer.Add(flipTransform);
            }

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

        public static ControlLine getControlLine(Dictionary<string, List<ControlLine>> profileDictionary, string profileName, double chainage)
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
            var result = Rhino.Geometry.Intersect.Intersection.CurveLine(Profile, verticalLine, IntersectionTolerance, OverlapTolerance);
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
