using System;
using System.Drawing;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;


namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("B1E056A3-488D-4F1B-88D1-280269EFED79")]
    public class MinimumClearanceCommand : Command
    {
        static MinimumClearanceCommand _instance;
        public MinimumClearanceCommand()
        {
            _instance = this;
        }
        public static MinimumClearanceCommand Instance
        {
            get { return _instance; }
        }
        public override string EnglishName
        {
            get { return "MinimumClearance"; }
        }
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            List<Guid> boltIDs = new List<Guid>();

            using (GetObject go = new GetObject())
            {
                go.DisablePreSelect();
                go.SetCommandPrompt("Select Bolts");
                go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
                GetResult get_rc = go.GetMultiple(1, 0);
                if (go.CommandResult() != Result.Success)
                {
                    return go.CommandResult();
                }
                for (int i = 0; i < go.ObjectCount; i++)
                {
                    boltIDs.Add(go.Object(i).ObjectId);
                }
            }

            ObjRef obj_ref;
            var rc = RhinoGet.GetOneObject("select tunnel surface", true, ObjectType.Brep, out obj_ref);
            if (rc != Result.Success)
                return rc;
            var brep = obj_ref.Brep();

            double minimumDistance = Double.MaxValue;
            Guid minimumDistanceBoltGuid = boltIDs[0];

            for (int i = 0; i < boltIDs.Count; i++)
            {
                Guid boltId = boltIDs[i];
                var bolt = new Rhino.DocObjects.ObjRef(boltId);
                if (bolt != null)
                {
                    var boltLine = bolt.Curve();
                    if (boltLine.GetLength() != 0)
                    {
                        var closestPoint = brep.ClosestPoint(boltLine.PointAtEnd);
                        var distance = closestPoint.DistanceTo(boltLine.PointAtEnd);
                        if (distance < minimumDistance)
                        {
                            minimumDistance = distance;
                            minimumDistanceBoltGuid = boltId;
                        }
                    }
                }
            }
            Color c = Color.FromArgb(255, 0, 0);

            var boltObject = new Rhino.DocObjects.ObjRef(minimumDistanceBoltGuid).Object();
            boltObject.Attributes.ObjectColor = c;
            boltObject.Attributes.ColorSource = ObjectColorSource.ColorFromObject;
            boltObject.CommitChanges();
            RhinoApp.WriteLine("Minimum clearance is " +minimumDistance.ToString());
            doc.Views.Redraw();

            return Result.Success;
        }
    }
}

