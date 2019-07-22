using System;
using Rhino;
using Rhino.Commands;
using Rhino.FileIO;

namespace TunnelBuilder.Models
{
    [System.Runtime.InteropServices.Guid("DFAB6E7D-E3DC-40CE-A1D8-BF4C657675F4")]
    public class TunnelProperty:Rhino.DocObjects.Custom.UserData
    {
        public double Area { get; set; }
        public double TroughWidthParameter { get; set; }

        public double VolumeLoss { get; set; }

        public TunnelProperty() { }

        public TunnelProperty(double area,double troughWidthParameter,double volumeLoss)
        {
            Area = area;
            TroughWidthParameter = troughWidthParameter;
            VolumeLoss = volumeLoss;
        }

        public override string Description
        {
            get { return "Tunnel Properties"; }
        }

        public override string ToString()
        {
            return string.Format("s_m={0}, K={1}, VL={2}", Area, TroughWidthParameter,VolumeLoss);
        }

        protected override void OnDuplicate(Rhino.DocObjects.Custom.UserData source)
        {
            TunnelProperty src = source as TunnelProperty;
            if (src!=null)
            {
                Area = src.Area;
                TroughWidthParameter = src.TroughWidthParameter;
                VolumeLoss = src.VolumeLoss;
            }
        }

        public override bool ShouldWrite
        {
            get
            {
                if(Area>0 && TroughWidthParameter>0)
                {
                    return true;
                }
                return false;
            }
        }

        protected override bool Read(BinaryArchiveReader archive)
        {
            Rhino.Collections.ArchivableDictionary dict = archive.ReadDictionary();
            if(dict.ContainsKey("Area")&&dict.ContainsKey("VolumeLoss")&&dict.ContainsKey("TroughWidthParameter"))
            {
                Area = (double)dict["Area"];
                TroughWidthParameter = (double)dict["TroughWidthParameter"];
                VolumeLoss = (double)dict["VolumeLoss"];

            }
            return true;
        }

        protected override bool Write(BinaryArchiveWriter archive)
        {
            var dict = new Rhino.Collections.ArchivableDictionary(2, "TunnelProperty");
            dict.Set("Area", Area);
            dict.Set("TroughWidthParameter", TroughWidthParameter);
            dict.Set("VolumeLoss", VolumeLoss);
            archive.WriteDictionary(dict);
            return true;
        }
    }
}

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("092AE258-29D9-4E54-98CE-4CA295962F56")]
    public class ex_TunnelPropertyCommand:Command
    {
        public override string EnglishName { get { return "cs_TunnelPropertyCommand"; } }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Rhino.DocObjects.ObjRef objref;

            var rc = Rhino.Input.RhinoGet.GetOneObject("Select Object", false, Rhino.DocObjects.ObjectType.AnyObject, out objref);
            if (rc != Result.Success)
                return rc;
            var tunnelProperty = objref.Geometry().UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
            if(tunnelProperty == null)
            {
                double Area = 0;
                double TroughWidthParameter = 0;
                double VolumeLoss = 0;
                rc = Rhino.Input.RhinoGet.GetNumber("Tunnel Area", false, ref Area);
                if (rc != Result.Success)
                    return rc;
                rc = Rhino.Input.RhinoGet.GetNumber("Trough Width Parameter", false, ref TroughWidthParameter);
                if (rc != Result.Success)
                    return rc;
                rc = Rhino.Input.RhinoGet.GetNumber("Volume Loss", false, ref VolumeLoss);
                if (rc != Result.Success)
                    return rc;
                tunnelProperty = new Models.TunnelProperty(Area, TroughWidthParameter,VolumeLoss);
                objref.Geometry().UserData.Add(tunnelProperty);
            }
            else
            {
                RhinoApp.WriteLine("Tunnel Area: {0}m2, Trough Width Parameter {1}, Volume Loss {2}", tunnelProperty.Area, tunnelProperty.TroughWidthParameter,tunnelProperty.VolumeLoss);
            }
            return Result.Success;
        }
    }
}
