using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.DocObjects;

namespace TunnelBuilder.Commands
{
    [System.Runtime.InteropServices.Guid("A9A848B4-3367-433E-962B-FC2387CD89B1")]
    public class BoltClearanceContourCommand:Command
    {
        ///<summary>The only instance of this command.</summary>
        public static BoltClearanceContourCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "BoltClearanceContour"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            double boltLength = 0;
            var rc = RhinoGet.GetNumber("Bolt Length", false, ref boltLength);
            if (rc != Result.Success)
            {
                return rc;
            }
            if (boltLength < 0)
            {
                RhinoApp.WriteLine("Bolt length must be positive");
                return Result.Failure;
            }

            Surface crownSurface = null ;
            Brep crownBrep = null;
            Brep clearanceBrep = null ;
            OptionDouble accuracyOption = new OptionDouble(0.5,0.1,10);
            OptionInteger maxDistanceOption = new OptionInteger(10, 1, 100);


            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.AddOptionDouble("Accuracy", ref accuracyOption);
                go.AddOptionInteger("MaxDistance", ref maxDistanceOption);
                go.SetCommandPrompt("Select tunnel crown surface");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Surface;
                while (true)
                {
                    GetResult get_rc = go.GetMultiple(1, 0);
                    if (get_rc == GetResult.Object)
                    {
                        crownSurface = go.Object(0).Surface();
                        crownBrep = go.Object(0).Geometry() as Brep;
                        if (crownSurface == null)
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

            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.AddOptionDouble("Accuracy", ref accuracyOption);
                go.AddOptionInteger("Max Distance", ref maxDistanceOption);
                go.SetCommandPrompt("Select surface that needs to calculate clearance from");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Brep;
                while (true)
                {
                    GetResult get_rc = go.GetMultiple(1, 0);
                    if (get_rc == GetResult.Object)
                    {
                        clearanceBrep = go.Object(0).Geometry() as Brep;
                        if (clearanceBrep == null)
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

            var fd = new Rhino.UI.SaveFileDialog { Filter = "Comma Seperated Values (*.csv)|*.csv", Title = "Save bolt contour", DefaultExt = "csv", FileName = "bolt_clearance.csv" };
            if (!fd.ShowSaveDialog())
            {
                return Result.Cancel;
            }
            var fn = fd.FileName;
            if (fn == string.Empty)
            {
                return Result.Cancel;
            }
            

            BoundingBox crownBrepBoudingBox = crownBrep.GetBoundingBox(false);
            Random random = new Random();
            double u, v;
            double accuracy = accuracyOption.CurrentValue;
            int maxDistance = maxDistanceOption.CurrentValue;
            List<string> lines = new List<string>();
            lines.Add("X [m], Y [m], Z[m], Clearance [m]");
            for (double i= 0;i<(crownBrepBoudingBox.Max.X-crownBrepBoudingBox.Min.X);i = i+accuracy)
            {
                for(double j=0; j<(crownBrepBoudingBox.Max.Y - crownBrepBoudingBox.Min.Y); j = j + accuracy)
                {
                    for(double k = 0; k < (crownBrepBoudingBox.Max.Z - crownBrepBoudingBox.Min.Z); k++)
                    {
                        Point3d randomBoundingBoxPoint = crownBrepBoudingBox.PointAt(i, j, k);
                        crownSurface.ClosestPoint(randomBoundingBoxPoint, out u, out v);
                        Point3d crownBrepTestPoint = crownBrep.ClosestPoint(randomBoundingBoxPoint);
                        Vector3d normal = crownSurface.NormalAt(u, v);
                        Point3d boltEnd = crownBrepTestPoint + boltLength * normal;
                        Point3d clearanceBrepPoint = clearanceBrep.ClosestPoint(boltEnd);
                        if (clearanceBrepPoint != Point3d.Unset)
                        {
                            double distance = boltEnd.DistanceTo(clearanceBrepPoint);
                            if(distance < maxDistance)
                            {
                                lines.Add(crownBrepTestPoint.X.ToString() + "," + crownBrepTestPoint.Y.ToString() + "," + crownBrepTestPoint.Z.ToString() + "," + distance);
                            }      
                        }
                    }
                }
                
                
            }

            System.IO.File.WriteAllLines(fn, lines);
            return Result.Success;
        }
    }
}
