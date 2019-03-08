using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder
{
    public class StageExcavationCommand : Command
    {
        static StageExcavationCommand _instance;
        public StageExcavationCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the StageExcavationCommand command.</summary>
        public static StageExcavationCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "StageExcavation"; }
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

            OptionDouble lengthCorrectionOption = new OptionDouble(0.001, true, 0.0);

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
                else if (i == numberOfAdvances && 1.0 / numberOfAdvances > length_correction_factor)
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

                        var bbox = tunnel_profile.GetBoundingBox(cplane);
                        Surface tunnel_profile_srf = new PlaneSurface(cplane, new Interval(bbox.Min[0], bbox.Max[0]),new Interval(bbox.Min[1],bbox.Max[1]));

                        doc.Objects.AddBrep(tunnel_profile_srf.ToBrep());
                    }
                }

            }
            return Result.Success;
        }
    }
}