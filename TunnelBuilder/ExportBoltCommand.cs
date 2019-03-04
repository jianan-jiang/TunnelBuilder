using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TunnelBuilder
{
    public class ExportBoltCommand : Command
    {
        static ExportBoltCommand _instance;
        public ExportBoltCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ExportBolt command.</summary>
        public static ExportBoltCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "ExportBolt"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string boltLayerName = "Bolt";
            int boltSegment = 10;
            int boltStartId = 1;

            var rc = RhinoGet.GetString("Bolt Layer Name", true, ref boltLayerName);
            if (rc == Result.Cancel)
            {
                return rc;
            }
            else if (boltLayerName == "")
            {
                boltLayerName = "Bolt";
            }

            Rhino.DocObjects.Layer boltLayer = null;

            if (boltLayerName == "Bolt")
            {
                boltLayer = doc.Layers.FindName("Bolt");
            }
            else
            {
                int boltLayerIndex = doc.Layers.FindByFullPath("Bolt::" + boltLayerName, -1);
                if (boltLayerIndex == -1)
                {
                    RhinoApp.WriteLine("Unable to find bolt layer");
                    return Result.Failure;
                }
                boltLayer = doc.Layers.FindIndex(boltLayerIndex);
            }




            rc = RhinoGet.GetInteger("Number of segment", true, ref boltSegment);

            if (boltSegment < 0)
            {
                RhinoApp.WriteLine("Number of segment must be positive");
                return Result.Failure;
            }

            rc = RhinoGet.GetInteger("Bolt Start ID", true, ref boltStartId);

            if (boltSegment < 1)
            {
                RhinoApp.WriteLine("Bolt Start ID must be positive");
                return Result.Failure;
            }

            Rhino.DocObjects.RhinoObject[] boltObjs = doc.Objects.FindByLayer(boltLayer);
            if (boltObjs == null || boltObjs.Length < 1)
            {
                return Result.Cancel;
            }

            var fn = RhinoGet.GetFileName(GetFileNameMode.SaveTextFile, "bolt.f3dat", "Bolt File Name", null);
            if(fn==string.Empty)
            {
                return Result.Cancel;
            }

            string extension = System.IO.Path.GetExtension(fn);
            fn = fn.Replace(extension, ".f3dat");

            System.IO.StreamWriter fs = new System.IO.StreamWriter(fn);

            for (int i = 0; i < boltObjs.Length; i++)
            {
                Guid boltId = boltObjs[i].Id;
                var boltLine = new Rhino.DocObjects.ObjRef(boltId).Curve();
                string line = "structure cable create by-line " + getCoordString(boltLine.PointAtStart) + " " + getCoordString(boltLine.PointAtEnd) + " id=" + (boltStartId+ i).ToString() + " seg=" + boltSegment.ToString();
                fs.WriteLine(line);
            }
            fs.Close();
            return Result.Success;
        }
        private string getCoordString(Point3d point)
        {
            return "(" + point.X.ToString() + "," + point.Y.ToString() + "," + point.Z.ToString() + ")";
        }
    }
    
}