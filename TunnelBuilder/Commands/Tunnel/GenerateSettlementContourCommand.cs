using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics;
using MathNet.Numerics.Optimization;
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
            return GenerateSettlementGrid(uvGrid,tunnelAlignments);

        }

        public static List<SettlementGridNode> GenerateSettlementGrid( List<Point3d> uvGrid, Curve[] tunnelAlignments)
        {
            List<SettlementGridNode> settlementGrid = new List<SettlementGridNode>();

            foreach (Point3d point in uvGrid)
            {
                SettlementGridNode settlementGridNode = new SettlementGridNode(point, 0);
                foreach (Curve alignment in tunnelAlignments)
                {
                    settlementGridNode.Settlement += GenerateSettlement(point, alignment);
                }
                settlementGrid.Add(settlementGridNode);
            }

            return settlementGrid;

        }

        
    }

    public class OptimizeSettlementContourCommand : Command
    {
        public override string EnglishName
        {
            get { return "OptimizeSettlementContour"; }
        }
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Rhino.DocObjects.ObjRef objref;
            Rhino.DocObjects.ObjRef[] objrefs;

            var rc = Rhino.Input.RhinoGet.GetOneObject("Select Ground Surface", false, Rhino.DocObjects.ObjectType.Surface, out objref);
            if (rc != Result.Success)
                return rc;

            Surface groundSurface = objref.Surface();

            rc = Rhino.Input.RhinoGet.GetOneObject("Select Target Settlement Grid", false, Rhino.DocObjects.ObjectType.PointSet, out objref);
            if (rc != Result.Success)
                return rc;
            PointCloud targetSettlementPointCloud = objref.PointCloud();
            List<SettlementGridNode> targetSettlementGrid = new List<SettlementGridNode>();
            foreach(var p in targetSettlementPointCloud)
            {
                var points = Rhino.Geometry.Intersect.Intersection.ProjectPointsToBreps(new List<Brep> { groundSurface.ToBrep() }, new List<Point3d> { new Point3d(p.X,p.Y,0) }, new Vector3d(0, 0, 1), doc.ModelAbsoluteTolerance);
                if(points.Length>0)
                {
                    targetSettlementGrid.Add(new SettlementGridNode(new Point3d(p.X, p.Y, points[0].Z), p.Z));
                }
                else
                {
                    targetSettlementGrid.Add(new SettlementGridNode(new Point3d(p.X, p.Y, 0), p.Z));
                }
                
            }



            rc = Rhino.Input.RhinoGet.GetMultipleObjects("Select Tunnel Alignment", false, Rhino.DocObjects.ObjectType.Curve, out objrefs);
            if (rc != Result.Success)
                return rc;
            List<Curve> alignments = new List<Curve>();

            foreach (var oref in objrefs)
            {
                alignments.Add(oref.Curve());
            }
            int maximumIterations = 10000;
            rc = Rhino.Input.RhinoGet.GetInteger("Enter maximum iterations", true, ref maximumIterations);
            if (rc != Result.Success)
                return rc;

            double convergenceTolerance = 0.01;
            rc = Rhino.Input.RhinoGet.GetNumber("Enter convergence tolerance", true, ref convergenceTolerance);
            if (rc != Result.Success)
                return rc;

            Curve[] tunnelAlignments = alignments.ToArray();
            RhinoApp.WriteLine("Optimizing");
            var res = OptimizeVolumeLoss(targetSettlementGrid, tunnelAlignments,convergenceTolerance,maximumIterations);
            RhinoApp.WriteLine("Finished");
            RhinoApp.WriteLine("Volume Losses: {0}", res.MinimizingPoint);
            RhinoApp.WriteLine("Minimum Square Distance: {0}m", res.FunctionInfoAtMinimum.Value);

            List<SettlementGridNode> settlementGrid = GenerateSettlementContourCommand.GenerateSettlementGrid(groundSurface, tunnelAlignments);
            PointCloud settlementCloud = new PointCloud();
            foreach (SettlementGridNode node in settlementGrid)
            {
                Point3d settlementPoint = new Point3d(node.Point.X, node.Point.Y, node.Settlement);
                settlementCloud.Add(settlementPoint);
            }
            doc.Objects.AddPointCloud(settlementCloud);
            doc.Views.Redraw();

            return Result.Success;
        }

        public static MinimizationResult OptimizeVolumeLoss(List<SettlementGridNode> targetSettlementGrid, Curve[] tunnelAlignments,double convergenceTolerance, int maximumInterations)
        {
            SettlementObjectiveFunction objectiveFunction = new SettlementObjectiveFunction(targetSettlementGrid, tunnelAlignments);
            var obj = ObjectiveFunction.Value(objectiveFunction.Value);
            var solver = new NelderMeadSimplex(convergenceTolerance,maximumInterations);
            double[] initialVolumeLoss = new double[tunnelAlignments.Length];
            for (var i = 0; i < tunnelAlignments.Length; i++)
            {
                var tunnelProperty = tunnelAlignments[i].UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                initialVolumeLoss[i] = tunnelProperty.VolumeLoss;
            }
            var initialGuess = new DenseVector(initialVolumeLoss);

            var result = solver.FindMinimum(obj, initialGuess);

            return result;
        }
    }

    public class SettlementObjectiveFunction
    {
        double[] TargetSettlement;
        double[] CurrentSettlement;
        List<Point3d> uvGrid = new List<Point3d>();
        Curve[] Alignments;
        public SettlementObjectiveFunction(List<SettlementGridNode> targetSettlementGrid,Curve[] alignments)
        {
            TargetSettlement = new double[targetSettlementGrid.Count];
            CurrentSettlement = new double[targetSettlementGrid.Count];
            for (int i = 0; i < targetSettlementGrid.Count; i++)
            {
                SettlementGridNode gridNode = targetSettlementGrid[i];
                uvGrid.Add(gridNode.Point);
                TargetSettlement[i] = gridNode.Settlement;
            }
            Alignments = alignments;
        }

        public double Value(Vector<double> input)
        {
            for(int i=0;i<input.Count;i++)
            {
                var tunnelProperty = Alignments[i].UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                Alignments[i].UserData.Remove(tunnelProperty);
                tunnelProperty.VolumeLoss = input[i];
                Alignments[i].UserData.Add(tunnelProperty);
            }
            List<SettlementGridNode> currentSettlementGrid = GenerateSettlementContourCommand.GenerateSettlementGrid(uvGrid, Alignments);
            for (int i = 0; i < currentSettlementGrid.Count; i++)
            {
                SettlementGridNode gridNode = currentSettlementGrid[i];
                CurrentSettlement[i] = gridNode.Settlement;
            }

            double d = Distance.SSD(TargetSettlement, CurrentSettlement);
            return d;
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
