using System;
using System.ComponentModel;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("35FC0792-5A85-4744-B8EF-C2E62144178F")]
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

        Dictionary<ExportEnvironment, String> ExportEnvironmentExtension = new Dictionary<ExportEnvironment, string>
        {
            { ExportEnvironment.FLAC3D,".f3dat"},
            {ExportEnvironment.UDEC,".uddat" }
        };

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string boltLayerName = "Bolt";
            int boltSegment = 10;
            int boltStartId = 1;

            

            var fd = new Rhino.UI.SaveFileDialog { Filter = "FLAC3D Data File (*.f3dat)|*.f3dat|UDEC Data File (*.uddat)|*.uddat", Title = "Save bolt files", DefaultExt = "f3dat",FileName="bolt.f3dat" };
            if (!fd.ShowSaveDialog())
            {
                return Result.Cancel;
            }
            var fn = fd.FileName;
            if (fn == string.Empty)
            {
                return Result.Cancel;
            }

            ExportEnvironment exportEnvironment;
            if (System.IO.Path.GetExtension(fn)==".f3dat")
            {
                exportEnvironment = ExportEnvironment.FLAC3D;
            }
            else
            {
                exportEnvironment = ExportEnvironment.UDEC;
            }

            var dialog = new Views.ExportBoltDialog(exportEnvironment,doc);
            var dialog_rc = dialog.ShowModal();
            if(dialog_rc!=Result.Success)
            {
                return dialog_rc;
            }
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
            bp.longitudinalSpacing = dialog.longitudinalSpacing;

            Rhino.DocObjects.Layer boltLayer;

            int boltLayerIndex = doc.Layers.FindByFullPath(dialog.boltLayerFullPath,-1);
            if (boltLayerIndex == -1)
            {
                
                RhinoApp.WriteLine("Unable to find bolt layer");
                return Result.Failure;
            }
            boltLayer = doc.Layers.FindIndex(boltLayerIndex);

            iterateLayers(doc, boltLayer, fn, boltStartId, boltSegment,exportEnvironment,1);

            RhinoApp.WriteLine("Successfully exported all bolts in " + dialog.boltLayerName + " to " + exportEnvironment.GetDescription() + " data file(s)");

            return Result.Success;
        }

        private int iterateLayers(RhinoDoc doc, Rhino.DocObjects.Layer boltLayer, string fn, int boltStartId, int boltSegment,ExportEnvironment exportEnvironment,int fileCount)
        {
            string filenameWithoutExtension = System.IO.Path.ChangeExtension(fn, null);
            boltStartId = boltStartId + exportBolts(doc, boltLayer, filenameWithoutExtension + "-"+getGroupName(boltLayer) + ExportEnvironmentExtension[exportEnvironment], boltStartId, boltSegment, exportEnvironment,fileCount+1);

            Rhino.DocObjects.Layer[] childrenLayers = boltLayer.GetChildren();
            if(childrenLayers!=null)
            {
                for (int i = 0; i < childrenLayers.Length; i++)
                {
                    boltStartId=iterateLayers(doc, childrenLayers[i], fn, boltStartId, boltSegment,exportEnvironment,fileCount+1);
                }
            }
            return boltStartId;
        }

        private string getCoordString(Point3d point, ExportEnvironment exportEnvironment)
        {
            switch (exportEnvironment)
            {
                case ExportEnvironment.FLAC3D:
                    return "(" + point.X.ToString() + "," + point.Y.ToString() + "," + point.Z.ToString() + ")";
                case ExportEnvironment.UDEC:
                    return point.X.ToString() + "," + point.Y.ToString();
                default:
                    throw new System.ArgumentException("Unsupported Export Environment");
            }
            
        }

        private int exportBolts(RhinoDoc doc, Rhino.DocObjects.Layer boltLayer, string filename,int boltStartId,int boltSegment, ExportEnvironment exportEnvironment,int grountMaterialCount)
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
                string line;
                switch (exportEnvironment)
                {
                    case ExportEnvironment.FLAC3D:
                        line = "structure cable create by-line " + getCoordString(boltLine.PointAtStart, exportEnvironment) + " " + getCoordString(boltLine.PointAtEnd, exportEnvironment) + " id=" + (boltStartId + i).ToString() + " seg=" + boltSegment.ToString();
                        break;
                    case ExportEnvironment.UDEC:
                        if (bp.preTension == 0)
                        {
                            line = "block structure cable create begin " + getCoordString(boltLine.PointAtStart, exportEnvironment) + " end " + getCoordString(boltLine.PointAtEnd, exportEnvironment) + " group='" + getGroupName(boltLayer) + "' seg=" + boltSegment.ToString();
                        }
                        else
                        {
                            line = "block structure cable create begin " + getCoordString(boltLine.PointAtStart, exportEnvironment) + " end " + getCoordString(boltLine.PointAtEnd, exportEnvironment) + " group='" + getGroupName(boltLayer) + "' seg=" + boltSegment.ToString() +" m-s 1 m-g "+grountMaterialCount.ToString();
                        }
                        
                        break;
                    default:
                        throw new System.ArgumentException("Unsupported Export Environment");
                }
                fs.WriteLine(line);
            }
           
            switch (exportEnvironment)
            {
                case ExportEnvironment.FLAC3D:
                    fs.WriteLine();
                    fs.WriteLine("; Define bolt properties");
                    fs.WriteLine("structure cable group '" + getGroupName(boltLayer) + "' ra id = " + boltStartId + ", " + (boltStartId + boltObjs.Length - 1).ToString());
                    if (bp.preTension > 0)
                    {
                        fs.WriteLine("structure cable in f-a=" + bp.preTension.ToString() + " range group '" + getGroupName(boltLayer) + "'");
                    }         
                    fs.WriteLine("structure cable property young= " + bp.young + " g-c= " + bp.groutCohesion + " g-s= " + bp.groutStiffness + " g-p= " + bp.groutPerimeter + " c-s-a= " + bp.crossSectionArea + " y-t= " + bp.yieldTension + " y-c= " + bp.yieldCompression + " range group '" + getGroupName(boltLayer) + "'");
                    break;
                case ExportEnvironment.UDEC:
                    fs.WriteLine();
                    fs.WriteLine("; Define bolt properties");
                    fs.WriteLine("block structure cable property material 1 young= "+ bp.young +" c-s-a= "+bp.crossSectionArea+" y-t= "+bp.yieldTension+" y-c= "+bp.yieldCompression+" density=0.008 spacing "+bp.longitudinalSpacing.ToString());
                    fs.WriteLine("block structure cable property material "+grountMaterialCount.ToString()+" grout-stiffness="+bp.groutStiffness+" grout-strength="+bp.groutCohesion);
                    if(bp.preTension>0)
                    {
                        fs.WriteLine();
                        fs.WriteLine("; Pre-tensioning the bolts");
                        fs.WriteLine("block structure cable fix-tension " + bp.preTension.ToString()+" range group '"+getGroupName(boltLayer)+"'");
                        fs.WriteLine("block structure cable property material " + grountMaterialCount.ToString() + " grout-strength=0.0");
                        fs.WriteLine("model cycle 1");
                        fs.WriteLine("block structure cable property material " + grountMaterialCount.ToString() + " grout-strength=" +bp.groutCohesion);
                        fs.WriteLine("block structure cable free-tension range group '"+getGroupName(boltLayer)+"'");
                    }
                    break;
                default:
                    throw new System.ArgumentException("Unsupported Export Environment");
            }
           
            fs.Close();

            // Return the number of bolts in the layer
            return boltObjs.Length;
        }

        private string getGroupName(Rhino.DocObjects.Layer boltLayer)
        {
            return boltLayer.FullPath.Replace("::", "-");
        }
    }
    
    public enum ExportEnvironment
    {
        [Description("FLAC3D")]
        FLAC3D,
        [Description("UDEC")]
        UDEC
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

        public double longitudinalSpacing;

    }
}