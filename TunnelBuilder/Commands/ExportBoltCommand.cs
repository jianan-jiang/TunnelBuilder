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
        private BoltParameter bp;
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

            var dialog = new Views.ExportBoltDialog();
            var dialog_rc = dialog.ShowModal();
            boltLayerName = dialog.boltLayerName;
            boltSegment = dialog.boltSegment;
            boltStartId = dialog.boltStartId;

            bp = new BoltParameter();
            bp.preTension = dialog.preTension;
            bp.young = dialog.young;
            bp.groutCohesion = dialog.groutCohesion;
            bp.groutStiffness = dialog.groutStiffness;
            bp.groutPerimeter = dialog.groutPerimeter;
            bp.crossSectionArea = dialog.crossSectionArea;
            bp.yieldTension = dialog.yieldTension;
            bp.yieldCompression = dialog.yieldCompression;

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


            var fn = RhinoGet.GetFileName(GetFileNameMode.SaveTextFile, "bolt.f3dat", "Bolt File Name", null);
            if(fn==string.Empty)
            {
                return Result.Cancel;
            }

            string extension = System.IO.Path.GetExtension(fn);
            fn = fn.Replace(extension, ".f3dat");

            iterateLayers(doc, boltLayer, fn, boltStartId, boltSegment);
            
            return Result.Success;
        }

        private int iterateLayers(RhinoDoc doc, Rhino.DocObjects.Layer boltLayer, string fn, int boltStartId, int boltSegment)
        {
            string filenameWithoutExtension = System.IO.Path.ChangeExtension(fn, null);
            boltStartId = boltStartId + exportBolts(doc, boltLayer, filenameWithoutExtension + "-"+getGroupName(boltLayer) + ".f3dat", boltStartId, boltSegment);

            Rhino.DocObjects.Layer[] childrenLayers = boltLayer.GetChildren();
            if(childrenLayers!=null)
            {
                for (int i = 0; i < childrenLayers.Length; i++)
                {
                    boltStartId=iterateLayers(doc, childrenLayers[i], fn, boltStartId, boltSegment);
                }
            }
            return boltStartId;
        }

        private string getCoordString(Point3d point)
        {
            return "(" + point.X.ToString() + "," + point.Y.ToString() + "," + point.Z.ToString() + ")";
        }

        private int exportBolts(RhinoDoc doc, Rhino.DocObjects.Layer boltLayer, string filename,int boltStartId,int boltSegment)
        { 
            Rhino.DocObjects.RhinoObject[] boltObjs = doc.Objects.FindByLayer(boltLayer);
            if (boltObjs == null || boltObjs.Length < 1)
            {
                return 0;
            }

            System.IO.StreamWriter fs = new System.IO.StreamWriter(filename);
            for (int i = 0; i < boltObjs.Length; i++)
            {
                Guid boltId = boltObjs[i].Id;
                var boltLine = new Rhino.DocObjects.ObjRef(boltId).Curve();
                string line = "structure cable create by-line " + getCoordString(boltLine.PointAtStart) + " " + getCoordString(boltLine.PointAtEnd) + " id=" + (boltStartId + i).ToString() + " seg=" + boltSegment.ToString();
                fs.WriteLine(line);
            }
            fs.WriteLine("structure cable group '" + getGroupName(boltLayer)+"' ra id = "+boltStartId+", "+(boltStartId+boltObjs.Length-1).ToString());
            fs.WriteLine("structure cable in f-a=" + bp.preTension.ToString() + " range group '" + getGroupName(boltLayer) + "'");
            fs.WriteLine("structure cable property young= "+bp.young+" g-c= "+bp.groutCohesion+" g-s= "+bp.groutStiffness+" g-p= "+bp.groutPerimeter+" c-s-a= "+bp.crossSectionArea+" y-t= "+bp.yieldTension+" y-c= "+bp.yieldCompression+" range group '"+getGroupName(boltLayer)+"'");
            fs.Close();

            // Return the number of bolts in the layer
            return boltObjs.Length;
        }

        private string getGroupName(Rhino.DocObjects.Layer boltLayer)
        {
            return boltLayer.FullPath.Replace("::", "-");
        }
    }
    
    

    public class BoltParameter
    {
        public double preTension;

        public double young;

        public double groutCohesion;

        public double groutStiffness;

        public double groutPerimeter;

        public double crossSectionArea;

        public double yieldTension;

        public double yieldCompression;

    }
}