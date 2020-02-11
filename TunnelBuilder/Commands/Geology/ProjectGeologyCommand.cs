using System;
using System.ComponentModel;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("F4BF6040-683A-4387-AE5D-65DC6D3170C7")]
    public class ProjectGeologyCommand:Command
    {
        static ProjectGeologyCommand _instance;
        public ProjectGeologyCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ProjectGeologyCommand Instance
        {
            get { return _instance; }
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "ProjectGeology"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("The {0} will project geology profile(s) along 3D control line", EnglishName);
            // Input: 
            //  1.  3D control line
            double controlLineStartChainage = 0;
            string controlLineName = "";

            Curve threeDControlLine = null;
            using (Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select tunnel control lines");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                while (true)
                {
                    GetResult get_rc = go.GetMultiple(1, 0);
                    if (get_rc == GetResult.Object)
                    {
                        threeDControlLine = go.Object(0).Geometry() as Curve;
                        if (threeDControlLine == null)
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

                if (threeDControlLine == null)
                {
                    RhinoApp.WriteLine("No control line was selected");
                    return Rhino.Commands.Result.Failure;
                }
            }

            var CLProperty = threeDControlLine.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;

            if (CLProperty != null && CLProperty.ProfileRole == Models.TunnelProperty.ProfileRoleNameDictionary[Models.ProfileRole.ControlLine])
            {
                controlLineStartChainage = CLProperty.ChainageAtStart;
                controlLineName = CLProperty.ProfileName;
                RhinoApp.WriteLine(String.Format("Using {0} Control Line Start Chaiange: {1}m", controlLineName, controlLineStartChainage));
            }
            else
            {
                var r = RhinoGet.GetNumber("Control Line Start Chaiange", false, ref controlLineStartChainage);
                if (r != Result.Success)
                {
                    return r;
                }
                if (controlLineStartChainage < 0)
                {
                    RhinoApp.WriteLine("Control Line Start Chaiange must be positive");
                    return Result.Failure;
                }
            }

            //  2.  Geology profile(s)
            double profileStartChainage = 0.0;
            var rc = RhinoGet.GetNumber("The start chainage of available geological profiles", false, ref profileStartChainage);
            if (rc != Result.Success)
            {
                return rc;
            }
            if (profileStartChainage < 0)
            {
                RhinoApp.WriteLine("The start chainage of available geological profilesh must be positive");
                return Result.Failure;
            }

            List<Curve> geologyProfiles = new List<Curve>();
            using (Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select geology profile curves");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                while (true)
                {
                    GetResult get_rc = go.GetMultiple(1,0);
                    if (get_rc == GetResult.Object)
                    {
                        for(int i=0; i < go.ObjectCount;i++)
                        {
                            try
                            {
                                Curve c = go.Object(i).Geometry() as Curve;
                                geologyProfiles.Add(c);
                            }
                            catch
                            {
                                RhinoApp.WriteLine("The {0} object you selected is not a curve object",UtilFunctions.GetOrdinal(i+1));
                            }
                            
                        }
                        if (threeDControlLine == null)
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

                if (geologyProfiles.Count==0)
                {
                    RhinoApp.WriteLine("No geology profile was selected");
                    return Rhino.Commands.Result.Failure;
                }
            }
            // Algorithm:
            //  1.  Create temprorary 2D control line from 3D control line
            Curve twoDControlLine = Curve.ProjectToPlane(threeDControlLine, Plane.WorldXY);
            double twoDControlLineLength = twoDControlLine.GetLength();
            //  2.  Match chainage and create new control point from 2D control line point (X,Y) and geology profile elevation Z 
            List<Guid> projectedProfileGuids = new List<Guid>();
            foreach (Curve profile in geologyProfiles)
            {
                
                double advanceLength = 0.0;
                double controlLineChainage = 0.0;
                double advanceStep = 1.0;
                List<Point3d> projectedProfilePoints = new List<Point3d>();
                Vector3d startTangent = profile.TangentAtStart;
                Vector3d endTangent = profile.TangentAtEnd;

                Curve twoDProfile = Curve.ProjectToPlane(profile, Plane.WorldXY);
                double twoDprofileLength = twoDProfile.GetLength();
                while (advanceLength<twoDprofileLength && controlLineChainage < twoDControlLineLength)
                {
                    controlLineChainage = advanceLength + profileStartChainage - controlLineStartChainage;
                    Point3d twoDprofilePoint = twoDProfile.PointAtLength(advanceLength);
                    double twoDParam = 0.0;
                    twoDProfile.ClosestPoint(twoDprofilePoint, out twoDParam);
                    Vector3d tangent = twoDProfile.TangentAt(twoDParam);
                    Vector3d tangentUsedToAlignProfile = new Vector3d(tangent);
                    tangentUsedToAlignProfile[2] = 0.0;
                    Plane cplane = new Plane(twoDprofilePoint, tangentUsedToAlignProfile);
                    if (cplane.YAxis[2] < 0)
                    {
                        //Rotate the plane 180 degree if y axis is pointing down
                        cplane.Rotate(Math.PI, cplane.ZAxis);
                    }
                    Surface srf = new PlaneSurface(cplane, new Interval(-1000, 1000), new Interval(-1000, 1000));
                    const double intersection_tolerance = 0.001;
                    const double overlap_tolerance = 0.0;
                    var events = Rhino.Geometry.Intersect.Intersection.CurveSurface(profile,srf,intersection_tolerance,overlap_tolerance);
                    if (events.Count>0)
                    {
                        Point3d profilePoint = events[0].PointA;
                        if(controlLineChainage>twoDControlLineLength)
                        {
                            break;
                        }
                        Point3d controlLinePoint = twoDControlLine.PointAtLength(controlLineChainage);
                        Point3d newPoint = new Point3d(controlLinePoint.X,controlLinePoint.Y,profilePoint.Z);
                        projectedProfilePoints.Add(newPoint);
                    }

                    advanceLength += advanceStep;
                }
                Curve projectedProfile = Curve.CreateInterpolatedCurve(projectedProfilePoints, 3, CurveKnotStyle.Chord, startTangent, endTangent);
                if(projectedProfile==null)
                {
                    projectedProfile = Curve.CreateInterpolatedCurve(projectedProfilePoints, 3);
                }
                var guid = doc.Objects.AddCurve(projectedProfile);
                projectedProfileGuids.Add(guid);
            }
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
