using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;


namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("D2DEE4E5-3851-45EB-80AE-6C12AC393344")]
    public class ModifyBoltLengthCommand:Command
    {
        static ModifyBoltLengthCommand _instance;
        public ModifyBoltLengthCommand()
        {
            _instance = this;
        }
        public static ModifyBoltLengthCommand Instance
        {
            get { return _instance; }
        }
        public override string EnglishName
        {
            get { return "ModifyBoltLength"; }
        }
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            List<Guid> boltIDs = new List<Guid>();
            double boltLength = 0;

            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Bolts");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                GetResult get_rc = go.GetMultiple(1,0);
                if(go.CommandResult()!=Result.Success)
                {
                    return go.CommandResult();
                }
                for (int i=0;i<go.ObjectCount;i++)
                {
                    boltIDs.Add(go.Object(i).ObjectId);
                }
            }

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

            for (int i = 0; i < boltIDs.Count; i++)
            {
                Guid boltId = boltIDs[i];
                var bolt = new Rhino.DocObjects.ObjRef(boltId);
                if(bolt !=null)
                {
                    var boltLine = bolt.Curve();
                    if (boltLine.GetLength() != 0)
                    {
                        Vector3d normalised_direction = (boltLine.PointAtEnd - boltLine.PointAtStart) / boltLine.GetLength();
                        Point3d newPointAtEnd = boltLine.PointAtStart + normalised_direction * boltLength;
                        doc.Objects.Replace(bolt, new Line(boltLine.PointAtStart, newPointAtEnd));
                    }
                }
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
