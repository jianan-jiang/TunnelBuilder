using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.Commands;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("AA5E7151-9F50-47C9-AC13-2B048D682576")]
    public class GenerateSettlementContourCommand:Command
    {
        public override string EnglishName
        {
            get { return "GenerateSettlementContour"; }
        }
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Rhino.DocObjects.ObjRef objref;
            Rhino.DocObjects.ObjRef[] objrefs;
            var rc = Rhino.Input.RhinoGet.GetOneObject("Select Ground Surface", false, Rhino.DocObjects.ObjectType.Surface, out objref);
            if (rc != Result.Success)
                return rc;
            Surface groundSurface = objref.Surface();
            rc = Rhino.Input.RhinoGet.GetMultipleObjects("Select Tunnel Alignment", false, Rhino.DocObjects.ObjectType.Curve,out objrefs);
            if (rc != Result.Success)
                return rc;
            List<Curve> alignments = new List<Curve>();

            foreach (var oref in objrefs)
            {
                alignments.Add(oref.Curve());
            }

            Curve[] tunnelAlignments = alignments.ToArray();
            List<SettlementGridNode> settlementGrid = GenerateSettlementGrid(groundSurface, tunnelAlignments);
            PointCloud settlementCloud = new PointCloud();
            foreach(SettlementGridNode node in settlementGrid)
            {
                Point3d settlementPoint = new Point3d(node.Point.X, node.Point.Y, node.Settlement);
                settlementCloud.Add(settlementPoint);
            }
            doc.Objects.AddPointCloud(settlementCloud);
            doc.Views.Redraw();
            return Result.Success;
        }

        public static List<Point3d> GenerateSurfaceGrid(Surface srf,int uDivisions, int vDivisions)
        {
            List<Point3d> uvGrid = new List<Point3d>();
            for (int i = 0; i < uDivisions; i++)
            {
                for (int j = 0; j < vDivisions; j++)
                {
                    Point3d srfPt = srf.PointAt(srf.Domain(0).Min + srf.Domain(0).Length / uDivisions * i, srf.Domain(1).Min + srf.Domain(1).Length / vDivisions * j);
                    uvGrid.Add(srfPt);
                }
            }
            return uvGrid;
        }

        public static double GenerateSettlement(Point3d point,Curve alignment)
        {
            double settlement = 0;
            double closestPointParameter;

            var tunnelProperty = alignment.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
            if(tunnelProperty == null)
            {
                return settlement;
            }

            bool existClosestPoint = alignment.ClosestPoint(point, out closestPointParameter);
            if(existClosestPoint)
            {
                Point3d closestPoint = alignment.PointAt(closestPointParameter);
                double horizontalDistance = Math.Sqrt(Math.Pow(closestPoint.X - point.X, 2)+Math.Pow(closestPoint.Y-point.Y,2));
                double verticalDistance = Math.Abs(closestPoint.Z - point.Z);
                double ix = tunnelProperty.TroughWidthParameter * verticalDistance;
                double factor1 = tunnelProperty.Area * tunnelProperty.VolumeLoss / (Math.Sqrt(2*Math.PI)*ix);
                double factor2 = Math.Exp(-Math.Pow(horizontalDistance, 2) / (2 * Math.Pow(ix, 2)));
                settlement = factor1 * factor2;
            }
            return settlement;
        }

        public static List<SettlementGridNode> GenerateSettlementGrid(Surface groundSurface,Curve[]tunnelAlignments)
        {
            List<Point3d> uvGrid = GenerateSurfaceGrid(groundSurface, 100, 100);
            List<SettlementGridNode> settlementGrid = new List<SettlementGridNode>();
            
            foreach (Point3d point in uvGrid)
            {
                SettlementGridNode settlementGridNode = new SettlementGridNode(point, 0);
                foreach(Curve alignment in tunnelAlignments)
                {
                    settlementGridNode.Settlement += GenerateSettlement(point, alignment);
                }
                settlementGrid.Add(settlementGridNode);
            }

            return settlementGrid;

        }
    }

    public class SettlementGridNode
    {
        public Point3d Point;
        public double Settlement;
        public double rotation;

        public SettlementGridNode(Point3d point,double settlement)
        {
            Point = point;
            Settlement = settlement;
        }
    }
}
