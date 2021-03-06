﻿using System;
using System.Collections.Generic;
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

        public string ProfileName { get; set; }
        public double ChainageAtStart { get; set; }
        public double Span { get; set; }

        public string ProfileRole { get; set; }

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
            return string.Format("Tunnel Area: {0}m2, Trough Width Parameter {1}, Volume Loss {2}, Profile {3}, Chainage at start {4}, Profile Role {5}, Span {6}m", Area, TroughWidthParameter, VolumeLoss, ProfileName, ChainageAtStart,ProfileRole,Span);
        }

        protected override void OnDuplicate(Rhino.DocObjects.Custom.UserData source)
        {
            TunnelProperty src = source as TunnelProperty;
            if (src!=null)
            {
                Area = src.Area;
                TroughWidthParameter = src.TroughWidthParameter;
                VolumeLoss = src.VolumeLoss;
                ProfileName = src.ProfileName;
                ChainageAtStart = src.ChainageAtStart;
                ProfileRole = src.ProfileRole;
                Span = src.Span;
            }
        }

        public override bool ShouldWrite
        {
            get
            {
                return true;
            }
        }

        protected override bool Read(BinaryArchiveReader archive)
        {
            Rhino.Collections.ArchivableDictionary dict = archive.ReadDictionary();
            if(dict.ContainsKey("Area")&&dict.ContainsKey("VolumeLoss")&&dict.ContainsKey("TroughWidthParameter")&&dict.ContainsKey("ProfileName")&&dict.ContainsKey("ChainageAtStart")&&dict.ContainsKey("ProfileRole"))
            {
                Area = (double)dict["Area"];
                TroughWidthParameter = (double)dict["TroughWidthParameter"];
                VolumeLoss = (double)dict["VolumeLoss"];
                ProfileName = (string)dict["ProfileName"];
                ChainageAtStart = (double)dict["ChainageAtStart"];
                ProfileRole = (string)dict["ProfileRole"];
            }
            if(dict.ContainsKey("Span"))
            {
                Span = (double)dict["Span"];
            }
            return true;
        }

        protected override bool Write(BinaryArchiveWriter archive)
        {
            var dict = new Rhino.Collections.ArchivableDictionary(4, "TunnelProperty");
            dict.Set("Area", Area);
            dict.Set("TroughWidthParameter", TroughWidthParameter);
            dict.Set("VolumeLoss", VolumeLoss);
            dict.Set("ProfileName", ProfileName);
            dict.Set("ChainageAtStart", ChainageAtStart);
            dict.Set("ProfileRole", ProfileRole);
            dict.Set("Span", Span);
            archive.WriteDictionary(dict);
            return true;
        }

        

        public static Dictionary<ProfileRole, string> ProfileRoleNameDictionary = new Dictionary<ProfileRole, string>
        {
            { Models.ProfileRole.ControlLine,"Control Line" },
            { Models.ProfileRole.LeftELine, "Left E-Line Profile" },
            { Models.ProfileRole.RightELine,"Right E-Line Profile" },
            { Models.ProfileRole.ELineProfile,"E-Line" },
            { Models.ProfileRole.DLineProfile,"D-Line" },
            { Models.ProfileRole.CLineProfile,"C-Line" },
            {Models.ProfileRole.ELineSurface,"E-Line Surface" },
            {Models.ProfileRole.BoltedZone,"BoltedZone" }
        };

    }

    public enum ProfileRole
    {
        ControlLine=1,
        LeftELine=2,
        RightELine=3,
        ELineProfile=4,
        DLineProfile=5,
        CLineProfile=6,
        ELineSurface=7,
        BoltedZone=8
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
                string ProfileName = "";
                double ChainageAtStart = 0;
                string ProfileRole = "";
                double Span = 0;
                rc = Rhino.Input.RhinoGet.GetNumber("Tunnel Area", false, ref Area);
                if (rc != Result.Success)
                    return rc;
                rc = Rhino.Input.RhinoGet.GetNumber("Trough Width Parameter", false, ref TroughWidthParameter);
                if (rc != Result.Success)
                    return rc;
                rc = Rhino.Input.RhinoGet.GetNumber("Volume Loss", false, ref VolumeLoss);
                if (rc != Result.Success)
                    return rc;
                rc = Rhino.Input.RhinoGet.GetString("Profile Name", false, ref ProfileName);
                if (rc != Result.Success)
                    return rc;
                rc = Rhino.Input.RhinoGet.GetNumber("Chaiange at start", false, ref ChainageAtStart);
                if (rc != Result.Success)
                    return rc;
                rc = Rhino.Input.RhinoGet.GetString("Profile Role", false, ref ProfileRole);
                if (rc != Result.Success)
                    return rc;
                rc = Rhino.Input.RhinoGet.GetNumber("Span", false, ref Span);
                if (rc != Result.Success)
                    return rc;
                tunnelProperty = new Models.TunnelProperty();
                tunnelProperty.Area = Area;
                tunnelProperty.TroughWidthParameter = TroughWidthParameter;
                tunnelProperty.VolumeLoss = VolumeLoss;
                tunnelProperty.ProfileName = ProfileName;
                tunnelProperty.ChainageAtStart = ChainageAtStart;
                tunnelProperty.ProfileRole = ProfileRole;
                tunnelProperty.Span = Span;
                objref.Geometry().UserData.Add(tunnelProperty);
            }
            else
            {
                string output = string.Format("Tunnel Area: {0}m2, Trough Width Parameter {1}, Volume Loss {2}, Profile {3}, Chainage at start {4}, Profile Role {5}, Span {6}m", tunnelProperty.Area, tunnelProperty.TroughWidthParameter, tunnelProperty.VolumeLoss, tunnelProperty.ProfileName, tunnelProperty.ChainageAtStart, tunnelProperty.ProfileRole,tunnelProperty.Span);
                RhinoApp.WriteLine(output);
            }
            return Result.Success;
        }
    }
}
