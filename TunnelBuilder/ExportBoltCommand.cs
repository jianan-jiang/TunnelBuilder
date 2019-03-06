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

            

            var fn = RhinoGet.GetFileName(GetFileNameMode.SaveTextFile, "bolt.f3dat", "Bolt File Name", null);
            if(fn==string.Empty)
            {
                return Result.Cancel;
            }

            string extension = System.IO.Path.GetExtension(fn);
            fn = fn.Replace(extension, ".f3dat");

            

            Rhino.DocObjects.Layer[] childrenLayers = boltLayer.GetChildren();
            if(childrenLayers == null)
            {
                exportBolts(doc, boltLayer, fn, boltStartId, boltSegment);
            }
            else
            {
                for(int i=0;i<childrenLayers.Length;i++)
                {

                    string filenameWithoutExtension = System.IO.Path.ChangeExtension(fn, null);
                    boltStartId = boltStartId + exportBolts(doc, childrenLayers[i], filenameWithoutExtension+"-"+childrenLayers[i].Name+".f3dat", boltStartId, boltSegment);
                }
            }

            
            return Result.Success;
        }
        private string getCoordString(Point3d point)
        {
            return "(" + point.X.ToString() + "," + point.Y.ToString() + "," + point.Z.ToString() + ")";
        }

        private int exportBolts(RhinoDoc doc, Rhino.DocObjects.Layer boltLayer, string filename,int boltStartId,int boltSegment)
        { 
            System.IO.StreamWriter fs = new System.IO.StreamWriter(filename);

            Rhino.DocObjects.RhinoObject[] boltObjs = doc.Objects.FindByLayer(boltLayer);
            if (boltObjs == null || boltObjs.Length < 1)
            {
                return -1;
            }


            for (int i = 0; i < boltObjs.Length; i++)
            {
                Guid boltId = boltObjs[i].Id;
                var boltLine = new Rhino.DocObjects.ObjRef(boltId).Curve();
                string line = "structure cable create by-line " + getCoordString(boltLine.PointAtStart) + " " + getCoordString(boltLine.PointAtEnd) + " id=" + (boltStartId + i).ToString() + " seg=" + boltSegment.ToString();
                fs.WriteLine(line);
            }
            fs.Close();

            // Return the number of bolts in the layer
            return boltObjs.Length;
        }
    }
    
}