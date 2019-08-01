using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Input;
using Rhino.Geometry;
using Rhino.Commands;
using TunnelBuilder.Models;

using Excel = Microsoft.Office.Interop.Excel;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("E56AAFD5-4E3F-41CF-8AC8-9EDC035EAA2C")]
    public class ProfileTestCommand:Command
    {
        public override string EnglishName
        { get { return "TestProfile"; } }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Point2d[] sp= new Point2d[10];
            sp[0] = new Point2d(-3.3, -0.05);
            sp[1] = new Point2d(-3.3, 4.37754);
            sp[2] = new Point2d(-2.5, 5.25);
            sp[3] = new Point2d(-0.668, 6.18682003);
            sp[4] = new Point2d(-0.668, 6.18682);
            sp[5] = new Point2d(3.2, 6.29018);
            sp[6] = new Point2d(3.2, 6.29018003);
            sp[7] = new Point2d(4.73837477, 5.57215);
            sp[8] = new Point2d(5.6, 4.69);
            sp[9] = new Point2d(5.6, 0.09);

            DLineShapeParameter dsp = new DLineShapeParameter(sp);
            DLineProfile dProfile = new DLineProfile(dsp);
            PolyCurve dProfilePolyCurve = dProfile.GetPolyCurve();
            doc.Objects.Add(dProfilePolyCurve);

            CLineGenerationParameter cgp = new CLineGenerationParameter();
            cgp.AngleofHaunchCircle = 60 * Math.PI / 180;
            cgp.CrownRadiusOption = CrownRadiusOption.R1EqualToW;

            CLineProfile cProfile = new CLineProfile(dsp, cgp);
            PolyCurve cProfilePolyCurve = cProfile.GetPolyCurve();
            doc.Objects.Add(cProfilePolyCurve);

            TunnelProfileShapeParameter shapeParameter = cProfile.ShapeParameter;
            shapeParameter.WallCLineELineOffset = 0.165;
            shapeParameter.CrownCLineELineOffset = 0.44;
            shapeParameter.HitchRadius = 0.65;
            shapeParameter.HitchOffset = 0.45;
            shapeParameter.FloorOffset = 0.5;

            ELineProfile eProfile = new ELineProfile(cProfile, shapeParameter);
            PolyCurve eProfilePolyCurve = eProfile.GetPolyCurve();
            doc.Objects.Add(eProfilePolyCurve);

            doc.Views.Redraw();

            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("744E474E-0F26-4FBA-93FF-1FE906998D4E")]
    public class GenerateProfilesCommand:Command
    {
        public override string EnglishName
        { get { return "GenerateProfiles"; } }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var fd = new Rhino.UI.OpenFileDialog { Filter = "Excel workbook with macro (*.xlsm)|*.xlsm| Excel workbook (*.xlsx)|*.xlsx", Title = "Open Tunnel Setout Points File", MultiSelect = false, DefaultExt = ".xlsm" };
            if (!fd.ShowOpenDialog())
            {
                return Result.Cancel;
            }
            var fn = fd.FileName;
            if (fn == string.Empty || !System.IO.File.Exists(fn))
            {
                return Result.Cancel;
            }
            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(fd.FileName);
            string[] sheetNames = new string[xlWorkbook.Worksheets.Count];
            for (int i= 1;i <= xlWorkbook.Worksheets.Count;i++)
            {
                Excel._Worksheet ws = xlWorkbook.Worksheets[i];
                sheetNames[i-1] = ws.Name;
                
            }
            var sheetNameDialog = new Views.SheetNameDialog(doc, "Select the sheet that contains tunnel setout points", sheetNames);
            var rc = sheetNameDialog.ShowModal();
            if(rc!=Result.Success)
            {
                return rc;
            }

            var layerNameDialog = new Views.LayerNameDialog(doc, "Select control line layer", "");
            rc = layerNameDialog.ShowModal();
            if (rc != Result.Success)
            {
                return rc;
            }

            int controlLineLayerIndex = doc.Layers.FindByFullPath(layerNameDialog.selectedLayerFullPath, -1);
            if (controlLineLayerIndex < 0)
            {
                return Result.Failure;
            }

            bool flip = false;
            rc = RhinoGet.GetBool("Flip profiles?", false, "No", "Yes",ref flip);
            if(rc !=Result.Success)
            {
                return rc;
            }

            var controlLineLayer = doc.Layers.FindIndex(controlLineLayerIndex);
            var controlLineDictionary = PlaceTunnelProfilesCommand.getProfileDictionaryFromLayer(controlLineLayer, doc, Models.ProfileRole.ControlLine);

            Excel._Worksheet xlWorksheet = xlWorkbook.Worksheets[sheetNameDialog.selectedSheetID];
            Excel.Range xlRange = xlWorksheet.UsedRange;

            int rowCount = xlRange.Rows.Count;
            int colCount = xlRange.Columns.Count;

            object[,] values = xlRange.Value;
#if DEBUG
            int profileLayerIndex = UtilFunctions.AddNewLayer(doc, "Profile");
#endif

            for(int i=4;i<=rowCount;i++)
            {
                var val = values[i, 1];
                //Try to get the control line name and setoutpoints
                if (values[i,1] ==null)
                {
                    RhinoApp.WriteLine("Wrong format");
                    return Result.Failure;
                }
                string controlLine = values[i, 1].ToString();
                if (values[i, 2] == null)
                {
                    RhinoApp.WriteLine("Wrong format");
                    return Result.Failure;
                }
                double chainage = Convert.ToDouble(values[i, 2]);

                Point2d[] setOutPoints = new Point2d[10];

                for(int k=0;k<setOutPoints.Length;k++)
                {
                    if (values[i, 19+2*k] == null)
                    {
                        RhinoApp.WriteLine("Wrong format");
                        return Result.Failure;
                    }
                    if (values[i, 19 + 2 * k+1] == null)
                    {
                        RhinoApp.WriteLine("Wrong format");
                        return Result.Failure;
                    }
                    double x = Convert.ToDouble(values[i, 19 + 2 * k]);
                    double y = Convert.ToDouble(values[i, 19 + 2 * k+1]);

                    setOutPoints[k] = new Point2d(x, y);
                }


                DLineShapeParameter dLineShapeParameter = new DLineShapeParameter(setOutPoints);
                DLineProfile dLineProfile = new DLineProfile(dLineShapeParameter);
                PolyCurve dLineProfilePolyCurve = dLineProfile.GetPolyCurve();

                CLineGenerationParameter cgp = new CLineGenerationParameter();
                cgp.AngleofHaunchCircle = 60 * Math.PI / 180;
                cgp.CrownRadiusOption = CrownRadiusOption.R1EqualToW;

                CLineProfile cLineProfile = new CLineProfile(dLineShapeParameter, cgp);
                PolyCurve cLineProfilePolyCurve = cLineProfile.GetPolyCurve();

                TunnelProfileShapeParameter shapeParameter = cLineProfile.ShapeParameter;
                shapeParameter.WallCLineELineOffset = 0.165;
                shapeParameter.CrownCLineELineOffset = 0.44;
                shapeParameter.HitchRadius = 0.65;
                shapeParameter.HitchOffset = 0.45;
                shapeParameter.FloorOffset = 0.5;

                ELineProfile eLineProfile = new ELineProfile(cLineProfile, shapeParameter);
                PolyCurve eLineProfilePolyCurve = eLineProfile.GetPolyCurve();

                var cL = PlaceTunnelProfilesCommand.getControlLine(controlLineDictionary, controlLine, chainage);
                var cLProperty = cL.Profile.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                var transforms = PlaceTunnelProfilesCommand.getTranforms(cL, eLineProfilePolyCurve, chainage, controlLine,flip);
                PlaceTunnelProfilesCommand.transformTunnelProfile(eLineProfilePolyCurve, transforms, cLProperty, Models.TunnelProperty.ProfileRoleNameDictionary[ProfileRole.ELineProfile], doc);
                PlaceTunnelProfilesCommand.transformTunnelProfile(cLineProfilePolyCurve, transforms, cLProperty, Models.TunnelProperty.ProfileRoleNameDictionary[ProfileRole.CLineProfile], doc);
                PlaceTunnelProfilesCommand.transformTunnelProfile(dLineProfilePolyCurve, transforms, cLProperty, Models.TunnelProperty.ProfileRoleNameDictionary[ProfileRole.DLineProfile], doc);
#if DEBUG
                int profileForControlLineLayerIndex = UtilFunctions.AddNewLayer(doc, controlLine, profileLayerIndex);

                
                int dLineLayerIndex = UtilFunctions.AddNewLayer(doc, controlLine + "_" + chainage.ToString("0.##") + "_" + "D-Line", profileForControlLineLayerIndex);
                var attributes = new Rhino.DocObjects.ObjectAttributes();
                attributes.LayerIndex = dLineLayerIndex;

                doc.Objects.AddCurve(dLineProfilePolyCurve, attributes);

                int cLineLayerIndex = UtilFunctions.AddNewLayer(doc, controlLine + "_" + chainage.ToString("0.##") + "_" + "C-Line", profileForControlLineLayerIndex);
                attributes = new Rhino.DocObjects.ObjectAttributes();
                attributes.LayerIndex = cLineLayerIndex;
                doc.Objects.AddCurve(cLineProfilePolyCurve, attributes);

                int eLineLayerIndex = UtilFunctions.AddNewLayer(doc, controlLine + "_" + chainage.ToString("0.##") + "_" + "E-Line", profileForControlLineLayerIndex);
                attributes = new Rhino.DocObjects.ObjectAttributes();
                attributes.LayerIndex = eLineLayerIndex;
                doc.Objects.AddCurve(eLineProfilePolyCurve, attributes);
                
#endif


            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Marshal.ReleaseComObject(xlRange);
            Marshal.ReleaseComObject(xlWorksheet);

            xlWorkbook.Close();
            Marshal.ReleaseComObject(xlWorkbook);

            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);

            doc.Views.Redraw();
            return Result.Success;
        }
    }

    public abstract class TunnelProfile
    {
        public abstract ProfileRole Role{
            get;
        }
        public string RoleName
        {
            get
            {
                return TunnelProperty.ProfileRoleNameDictionary[Role];
            }
        }
        public abstract PolyCurve GetPolyCurve();
        public Point2d[] SetoutPoints;
    }

    class ELineProfile:TunnelProfile
    {
        PolyCurve Profile;
        double CrownRadius;
        double AngleOfFloor;
        TunnelProfileShapeParameter ShapeParameter;
        new public Point2d[] SetoutPoints;
        public override ProfileRole Role
        {
            get { return ProfileRole.ELineProfile; }
        }
        public override PolyCurve GetPolyCurve()
        {
            Profile = new PolyCurve();

            Line rightEWall = new Line(new Point3d(SetoutPoints[7].X, SetoutPoints[7].Y, 0), new Point3d(SetoutPoints[6].X, SetoutPoints[6].Y, 0));
            Profile.Append(rightEWall);

            Arc rightHitch = new Arc(new Point3d(SetoutPoints[6].X, SetoutPoints[6].Y, 0), new Point3d(SetoutPoints[5].X, SetoutPoints[5].Y, 0), new Point3d(SetoutPoints[4].X, SetoutPoints[4].Y, 0));
            Profile.Append(rightHitch);

            double mainArcStartAngle = Math.Acos(Math.Abs((SetoutPoints[10].X - SetoutPoints[4].X) / CrownRadius));
            double mainArcEndAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[10].X - SetoutPoints[3].X) / CrownRadius));
            Interval mainArcInterval = new Interval(mainArcStartAngle, mainArcEndAngle);
            Circle mainCircle = new Circle(new Point3d(SetoutPoints[10].X, SetoutPoints[10].Y, 0), CrownRadius);
            Arc mainArc = new Arc(mainCircle, mainArcInterval);
            Profile.Append(mainArc);

            Arc leftHitch = new Arc(new Point3d(SetoutPoints[3].X, SetoutPoints[3].Y, 0), new Point3d(SetoutPoints[2].X, SetoutPoints[2].Y, 0), new Point3d(SetoutPoints[1].X, SetoutPoints[1].Y, 0));
            Profile.Append(leftHitch);

            Line leftEWall = new Line(new Point3d(SetoutPoints[1].X, SetoutPoints[1].Y, 0), new Point3d(SetoutPoints[0].X, SetoutPoints[0].Y, 0));
            Profile.Append(leftEWall);

            Line invert = new Line(new Point3d(SetoutPoints[0].X, SetoutPoints[0].Y, 0), new Point3d(SetoutPoints[7].X, SetoutPoints[7].Y, 0));
            Profile.Append(invert);

            return Profile;
        }

        public ELineProfile(CLineProfile cLineProfile, TunnelProfileShapeParameter shapeParameter)
        {
            ShapeParameter = shapeParameter;
            CrownRadius = ShapeParameter.R1 + ShapeParameter.CrownCLineELineOffset;
            AngleOfFloor = Math.Atan(Math.Abs((cLineProfile.SetoutPoints[8].Y - cLineProfile.SetoutPoints[7].Y) / (cLineProfile.SetoutPoints[8].X - cLineProfile.SetoutPoints[7].X)));

            SetoutPoints = new Point2d[11];

            //Main Arc Center
            SetoutPoints[10].X = cLineProfile.SetoutPoints[6].X;
            SetoutPoints[10].Y = cLineProfile.SetoutPoints[6].Y;

            //Left Side
            SetoutPoints[1].X = cLineProfile.SetoutPoints[7].X - ShapeParameter.WallCLineELineOffset;

            SetoutPoints[2].X = SetoutPoints[1].X - ShapeParameter.HitchOffset;

            SetoutPoints[8].X = SetoutPoints[2].X + ShapeParameter.HitchRadius;
            SetoutPoints[8].Y = cLineProfile.SetoutPoints[6].Y + Math.Sqrt(Math.Pow(CrownRadius - ShapeParameter.HitchRadius, 2) - Math.Pow(cLineProfile.SetoutPoints[6].X - SetoutPoints[8].X, 2));
            SetoutPoints[2].Y = SetoutPoints[8].Y;

            SetoutPoints[3].X = (CrownRadius * SetoutPoints[8].X - ShapeParameter.HitchRadius * cLineProfile.SetoutPoints[6].X) / (CrownRadius - ShapeParameter.HitchRadius);
            SetoutPoints[3].Y = cLineProfile.SetoutPoints[6].Y + Math.Sqrt(Math.Pow(CrownRadius,2) - Math.Pow(cLineProfile.SetoutPoints[6].X-SetoutPoints[3].X,2));

            SetoutPoints[1].Y = SetoutPoints[8].Y - Math.Sqrt(Math.Pow(shapeParameter.HitchRadius,2) - Math.Pow(SetoutPoints[1].X-SetoutPoints[8].X,2));

            SetoutPoints[0].X = SetoutPoints[1].X;
            SetoutPoints[0].Y = cLineProfile.SetoutPoints[7].Y - ShapeParameter.FloorOffset + ShapeParameter.WallCLineELineOffset * Math.Tan(AngleOfFloor);

            //Right Side
            SetoutPoints[6].X = cLineProfile.SetoutPoints[8].X + ShapeParameter.WallCLineELineOffset;
            SetoutPoints[6].Y = SetoutPoints[1].Y;

            SetoutPoints[5].X = SetoutPoints[6].X + ShapeParameter.HitchOffset;
            

            SetoutPoints[9].X = SetoutPoints[5].X - ShapeParameter.HitchRadius;
            SetoutPoints[9].Y = SetoutPoints[8].Y;
            SetoutPoints[5].Y = SetoutPoints[9].Y;

            SetoutPoints[4].X = (CrownRadius * SetoutPoints[9].X - ShapeParameter.HitchRadius * cLineProfile.SetoutPoints[6].X) / (CrownRadius - ShapeParameter.HitchRadius);
            SetoutPoints[4].Y = SetoutPoints[3].Y;

            SetoutPoints[7].X = SetoutPoints[6].X;
            SetoutPoints[7].Y = cLineProfile.SetoutPoints[8].Y - ShapeParameter.FloorOffset + ShapeParameter.WallCLineELineOffset * Math.Tan(AngleOfFloor);
        }

    }

    class CLineProfile:TunnelProfile
    {
        PolyCurve Profile;
        new public Point2d[] SetoutPoints;
        public TunnelProfileShapeParameter ShapeParameter;
        public override ProfileRole Role
        {
            get { return ProfileRole.CLineProfile; }
        }
        public override PolyCurve GetPolyCurve()
        {
            Profile = new PolyCurve();

            Line rightCWall = new Line(new Point3d(SetoutPoints[8].X, SetoutPoints[8].Y, 0), new Point3d(SetoutPoints[3].X,SetoutPoints[3].Y,0));
            Profile.Append(rightCWall);

            double rightArcStartAngle = Math.Acos(Math.Abs((SetoutPoints[5].X - SetoutPoints[3].X) / ShapeParameter.R2));
            double rightArcEndAngle = Math.Acos(Math.Abs((SetoutPoints[5].X - SetoutPoints[2].X) / ShapeParameter.R2));
            Interval rightArcInterval = new Interval(rightArcStartAngle, rightArcEndAngle);
            Circle rightCircle = new Circle(new Point3d(SetoutPoints[5].X, SetoutPoints[5].Y, 0), ShapeParameter.R2);
            Arc rightArc = new Arc(rightCircle, rightArcInterval);
            Profile.Append(rightArc);

            double mainArcStartAngle = Math.Acos(Math.Abs((SetoutPoints[6].X - SetoutPoints[2].X) / ShapeParameter.R1));
            double mainArcEndAngle = Math.PI-Math.Acos(Math.Abs((SetoutPoints[6].X - SetoutPoints[1].X) / ShapeParameter.R1));
            Interval mainArcInterval = new Interval(mainArcStartAngle, mainArcEndAngle);
            Circle mainCircle = new Circle(new Point3d(SetoutPoints[6].X, SetoutPoints[6].Y, 0), ShapeParameter.R1);
            Arc mainArc = new Arc(mainCircle, mainArcInterval);
            Profile.Append(mainArc);

            double leftArcStartAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[4].X - SetoutPoints[1].X) / ShapeParameter.R2));
            double leftArcEndAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[4].X - SetoutPoints[0].X) / ShapeParameter.R2));
            Interval leftArcInterval = new Interval(leftArcStartAngle, leftArcEndAngle);
            Circle leftCircle = new Circle(new Point3d(SetoutPoints[4].X, SetoutPoints[4].Y, 0), ShapeParameter.R2);
            Arc leftArc = new Arc(leftCircle, leftArcInterval);
            Profile.Append(leftArc);

            Line leftCWall = new Line(new Point3d(SetoutPoints[0].X, SetoutPoints[0].Y, 0), new Point3d(SetoutPoints[7].X, SetoutPoints[7].Y, 0));
            Profile.Append(leftCWall);

            Line Invert = new Line(new Point3d(SetoutPoints[7].X, SetoutPoints[7].Y, 0), new Point3d(SetoutPoints[8].X, SetoutPoints[8].Y, 0));
            Profile.Append(Invert);

            return Profile;
        }

        public CLineProfile(DLineShapeParameter sp, CLineGenerationParameter gp)
        {
            ShapeParameter = new TunnelProfileShapeParameter();
            ShapeParameter.B1 = sp.getB1();
            switch (gp.CrownRadiusOption)
            {
                case CrownRadiusOption.R1EqualToW:
                    ShapeParameter.R1 = Math.Round(ShapeParameter.B1 * 10, MidpointRounding.AwayFromZero) / 10;
                    break;
                case CrownRadiusOption.R1EqualToWPlusTwo:
                    ShapeParameter.R1 = Math.Round((ShapeParameter.B1 +2)* 2, MidpointRounding.AwayFromZero) / 2;
                    break;
                case CrownRadiusOption.R1EqualToWRounded:
                    ShapeParameter.R1 = Math.Round(ShapeParameter.B1*2,MidpointRounding.AwayFromZero)/2;
                    break;
                default:
                    throw new ArgumentException();
            }
            ShapeParameter.R2 = Math.Round(ShapeParameter.R1/4 * 10, MidpointRounding.AwayFromZero) / 10;
            calculateSetOutPoints(sp,gp);

        }

        private void calculateSetOutPoints(DLineShapeParameter sp, CLineGenerationParameter gp)
        {
            SetoutPoints = new Point2d[9];
            //Xc7
            SetoutPoints[6] = new Point2d();
            SetoutPoints[6].X = 0.5 * (sp.SetoutPoints[9].X + sp.SetoutPoints[0].X);
            //Xc5
            SetoutPoints[4] = new Point2d();
            SetoutPoints[4].X = sp.SetoutPoints[0].X + ShapeParameter.R2 * Math.Sin(gp.AngleofHaunchCircle);
            //Xc6
            SetoutPoints[5] = new Point2d();
            SetoutPoints[5].X = sp.SetoutPoints[9].X - ShapeParameter.R2 * Math.Sin(gp.AngleofHaunchCircle);
            //Xc2
            SetoutPoints[1] = new Point2d();
            SetoutPoints[1].X = (ShapeParameter.R1 * SetoutPoints[4].X - ShapeParameter.R2 * SetoutPoints[6].X) / (ShapeParameter.R1 - ShapeParameter.R2);
            //Xc3
            SetoutPoints[2] = new Point2d();
            SetoutPoints[2].X = (ShapeParameter.R1 * SetoutPoints[5].X - ShapeParameter.R2 * SetoutPoints[6].X) / (ShapeParameter.R1 - ShapeParameter.R2);
            //Xc1
            SetoutPoints[0].X = sp.SetoutPoints[0].X;
            //Xc4
            SetoutPoints[3].X = sp.SetoutPoints[9].X;


            //Yc7
            SetoutPoints[6].Y = getYc7(sp);
            //Yc5
            SetoutPoints[4].Y = SetoutPoints[6].Y + Math.Sqrt(Math.Pow(ShapeParameter.R1 - ShapeParameter.R2, 2) - Math.Pow(SetoutPoints[6].X - SetoutPoints[4].X, 2));
            //Yc1
            SetoutPoints[0].Y = SetoutPoints[4].Y + Math.Sqrt(Math.Pow(ShapeParameter.R2, 2) - Math.Pow(SetoutPoints[4].X - sp.SetoutPoints[0].X, 2));
            //Yc2
            SetoutPoints[1].Y = SetoutPoints[6].Y + Math.Sqrt(Math.Pow(ShapeParameter.R1, 2) - Math.Pow(SetoutPoints[6].X - SetoutPoints[1].X, 2));
            //Yc6
            SetoutPoints[5].Y = SetoutPoints[4].Y;
            //Yc4
            SetoutPoints[3].Y = SetoutPoints[0].Y;
            //Yc3
            SetoutPoints[2].Y = SetoutPoints[1].Y;

            //Xc8 and Yc8
            SetoutPoints[7] = new Point2d();
            SetoutPoints[7].X = sp.SetoutPoints[0].X;
            SetoutPoints[7].Y = sp.SetoutPoints[0].Y;

            //Xc9 and Yc9
            SetoutPoints[8] = new Point2d();
            SetoutPoints[8].X = sp.SetoutPoints[9].X;
            SetoutPoints[8].Y = sp.SetoutPoints[9].Y;

        }

        private double getYc7(DLineShapeParameter sp)
        {
            double max = Yc7(sp.SetoutPoints[1]);
            for(int i=2;i<sp.SetoutPoints.Length-1;i++)
            {
                double y = Yc7(sp.SetoutPoints[i]);
                if(y>max)
                {
                    max = y;
                }
            }
            return max;
        }

        private double Yc5(Point2d point)
        {
            if(point.X<SetoutPoints[1].X)
            {
                return point.Y - Math.Sqrt(Math.Pow(ShapeParameter.R2, 2) - Math.Pow(SetoutPoints[4].X - point.X, 2));
            }
            return Double.NaN;
        }

        private double Yc6(Point2d point)
        {
            if(point.X>=SetoutPoints[2].X)
            {
                return point.Y - Math.Sqrt(Math.Pow(ShapeParameter.R2,2) - Math.Pow(point.X - SetoutPoints[5].X, 2));
            }
            return Double.NaN;
        }

        private double Yc7(Point2d point)
        {
            if(point.X>SetoutPoints[1].X && point.X<SetoutPoints[2].X)
            {
                return point.Y - Math.Sqrt(Math.Pow(ShapeParameter.R1, 2) - Math.Pow(SetoutPoints[6].X - point.X, 2));
            }
            if(point.X<SetoutPoints[1].X)
            {
                return Yc5(point) - Math.Sqrt(Math.Pow(ShapeParameter.R1 - ShapeParameter.R2, 2) - Math.Pow(SetoutPoints[6].X - SetoutPoints[4].X, 2));
            }
            if(point.X>=SetoutPoints[2].X)
            {
                return Yc6(point) - Math.Sqrt(Math.Pow(ShapeParameter.R1 - ShapeParameter.R2, 2) - Math.Pow(SetoutPoints[5].X-SetoutPoints[6].X, 2));
            }
            return Double.NaN;
        }
    }

    class DLineProfile:TunnelProfile
    {
        PolyCurve Profile;
        new Point2d[] SetoutPoints;
        public override ProfileRole Role
        {
            get { return ProfileRole.ELineProfile; }
        }

        public DLineProfile(DLineShapeParameter param)
        {
            Profile = new PolyCurve();
            SetoutPoints = param.SetoutPoints;
            for(int i=0;i<SetoutPoints.Length-1;i++)
            {
                Profile.Append(new Line(SetoutPoints[i].X, SetoutPoints[i].Y, 0, SetoutPoints[i + 1].X, SetoutPoints[i + 1].Y, 0));
            }
        }

        public override PolyCurve GetPolyCurve()
        {
            return Profile;
        }
    }

    class CLineGenerationParameter
    {
        public double AngleofHaunchCircle;
        public CrownRadiusOption CrownRadiusOption;

    }

    public enum CrownRadiusOption
    {
        R1EqualToW,
        R1EqualToWRounded,
        R1EqualToWPlusTwo
    }

    class TunnelProfileShapeParameter
    {
        public double B1;
        public double B2;
        public double B3;
        public double R1;
        public double R2;
        public double Theta1;
        public double Theta2;
        public double H1;
        public double H2;
        public double H3;
        public double H4;

        public double WallCLineELineOffset;
        public double CrownCLineELineOffset;
        public double HitchRadius;
        public double HitchOffset;
        public double FloorOffset;
    }

    class DLineShapeParameter
    {
        public DLineShapeParameter(Point2d[] sP)
        {
            if(sP.Length!=10)
            {
                throw new ArgumentException("There should be ten set-out points for D-Line Profile");
            }
            SetoutPoints = sP;
        }
        public Point2d[] SetoutPoints { get; set; } = new Point2d[10];

        public double getB1()
        {
            double max = SetoutPoints[0].X;
            double min = SetoutPoints[0].X;

            foreach(Point2d p in SetoutPoints)
            {
                if(p.X>max)
                {
                    max = p.X;
                }
                if(p.X<min)
                {
                    min = p.X;
                }
            }

            return max - min;
        }

        
    }
}
