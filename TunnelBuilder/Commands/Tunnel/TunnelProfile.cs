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
using TunnelBuilder.Views;
using System.Xml.Serialization;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;

namespace TunnelBuilder
{
    [Guid("8F04A035-7789-4D88-B8E8-C44C82F67690")]
    public class GenerateProfilesFromSetoutTableCommand:Command
    {
        public override string EnglishName
        { get { return "GenerateProfilesFromSetoutTable"; } }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("Create 3D tunnel surface from Setout Table as shown in the drawings");
            var fd = new Rhino.UI.OpenFileDialog { Filter = "Excel workbook (*.xlsx;*.xlsm)|*.xlsx;*.xlsm", Title = "Open Tunnel Setout Points File", MultiSelect = false };
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
            for (int i = 1; i <= xlWorkbook.Worksheets.Count; i++)
            {
                Excel._Worksheet ws = xlWorkbook.Worksheets[i];
                sheetNames[i - 1] = ws.Name;

            }
            var sheetNameDialog = new Views.SheetNameDialog(doc, "Select the sheet that contains tunnel setout points", sheetNames);
            var rc = sheetNameDialog.ShowModal();
            if (rc != Result.Success)
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

            fd = new Rhino.UI.OpenFileDialog { Filter = "XML Files (*.xml)|*.xml", Title = "Open Tunnel Support Definition File", MultiSelect = false, DefaultExt = "xml" };
            if (!fd.ShowOpenDialog())
            {
                return Result.Cancel;
            }
            fn = fd.FileName;
            if (fn == string.Empty || !System.IO.File.Exists(fn))
            {
                return Result.Cancel;
            }


            TunnelSupportDefinition tsd;
            FileStream tstFileStream;
            XmlSerializer tstSerializer = new XmlSerializer(typeof(TunnelSupportDefinition));
            try
            {
                tstFileStream = new FileStream(fn, FileMode.Open);
            }
            catch
            {
                RhinoApp.WriteLine("Unable to open tunnel support definition file");
                return Result.Failure;
            }

            try
            {
                tsd = (TunnelSupportDefinition)tstSerializer.Deserialize(tstFileStream);
            }
            catch
            {
                RhinoApp.WriteLine("Wrong tunnel support definition format");
                return Result.Failure;
            }

            tstFileStream.Close();
            RhinoApp.WriteLine("Applying Tunnel Support Definition created on " + tsd.CreateDate.ToShortDateString());

            List<string> groundConditionNames = new List<string>();

            foreach (var groundCondition in tsd.GroundConditions)
            {
                groundConditionNames.Add(groundCondition.Name);
            }

            string[] groundConditionNameList = groundConditionNames.ToArray();

            GenerateTunnelProfilesDialog generateTunnelProfilesDialog = new GenerateTunnelProfilesDialog(doc);
            generateTunnelProfilesDialog.WallCLineELineOffset = 0.335;
            generateTunnelProfilesDialog.CrownCLineELineOffset = 0.78;
            generateTunnelProfilesDialog.HitchRadius = 0.65;
            generateTunnelProfilesDialog.HitchOffset = 0.45;
            generateTunnelProfilesDialog.includeHitch = true;
            generateTunnelProfilesDialog.KeepTwoDProfiles = false;
            generateTunnelProfilesDialog.FlipProfiles = true;

            var dialog_rc = generateTunnelProfilesDialog.ShowModal();
            if (dialog_rc != Result.Success)
            {
                return dialog_rc;
            }

            var WallCLineELineOffset = generateTunnelProfilesDialog.WallCLineELineOffset;
            var CrownCLineELineOffset = generateTunnelProfilesDialog.CrownCLineELineOffset;
            var includeHitch = generateTunnelProfilesDialog.includeHitch;
            var HitchRadius = generateTunnelProfilesDialog.HitchRadius;
            var HitchOffset = generateTunnelProfilesDialog.HitchOffset;
            var keepTwoDProfiles = generateTunnelProfilesDialog.KeepTwoDProfiles;


            // Based on design drawing specification, the profiles should always be "flipped" due to the fact that
            // the local coordinate system used for set-out is a "left-handed" coordinate system (see https://www.evl.uic.edu/ralph/508S98/coordinates.html)
            // but the system that has been conventionally adopted in rhino is a "right-handed" coordiante system.
            bool flip = generateTunnelProfilesDialog.FlipProfiles;

            var controlLineLayer = doc.Layers.FindIndex(controlLineLayerIndex);
            var placeTunnelProfilesCommand = new PlaceTunnelProfilesCommand();
            var controlLineDictionary = PlaceTunnelProfilesCommand.getProfileDictionaryFromLayer(controlLineLayer, doc, Models.ProfileRole.ControlLine);
            placeTunnelProfilesCommand.ControlLinesDictionary = controlLineDictionary;
            Excel._Worksheet xlWorksheet = xlWorkbook.Worksheets[sheetNameDialog.selectedSheetID];
            Excel.Range xlRange = xlWorksheet.UsedRange;

            int rowCount = xlRange.Rows.Count;
            int colCount = xlRange.Columns.Count;

            object[,] values = xlRange.Value;

            int profileLayerIndex = -1;
            if (keepTwoDProfiles)
            {
                profileLayerIndex = UtilFunctions.AddNewLayer(doc, "Profile");
            }
            


            for (int i = 2; i <= rowCount; i++)
            {
                var val = values[i, 1];
                //Try to get the control line name and setoutpoints
                if (values[i, 1] == null)
                {
                    RhinoApp.WriteLine("Wrong format");
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                string controlLine = values[i, 1].ToString();
                if (values[i, 2] == null)
                {
                    RhinoApp.WriteLine("Wrong format");
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                double chainage = Convert.ToDouble(values[i, 2]);

                TunnelProfileShapeParameter shapeParameter = new TunnelProfileShapeParameter();
                if (values[i, 3] == null)
                {
                    RhinoApp.WriteLine(string.Format("B1 not found for {0} CH {1}",controlLine,chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.B1 = Convert.ToDouble(values[i, 3]);

                if (values[i, 4] == null)
                {
                    RhinoApp.WriteLine(string.Format("B2 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.B2 = Convert.ToDouble(values[i, 4]);

                if (values[i, 5] == null)
                {
                    RhinoApp.WriteLine(string.Format("B3 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.B3 = Convert.ToDouble(values[i, 5]);

                if (values[i, 6] == null)
                {
                    RhinoApp.WriteLine(string.Format("R1 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.R1 = Convert.ToDouble(values[i, 6]);

                if (values[i, 7] == null)
                {
                    RhinoApp.WriteLine(string.Format("R2 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.R2 = Convert.ToDouble(values[i, 7]);

                if (values[i, 8] == null)
                {
                    RhinoApp.WriteLine(string.Format("Theta 1 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.Theta1 = Convert.ToDouble(values[i, 8])*Math.PI/180;

                if (values[i, 9] == null)
                {
                    RhinoApp.WriteLine(string.Format("Theta 2 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.Theta2 = Convert.ToDouble(values[i, 9]) * Math.PI / 180;

                if (values[i, 10] == null)
                {
                    RhinoApp.WriteLine(string.Format("H1 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.H1 = Convert.ToDouble(values[i, 10]);

                if (values[i, 11] == null)
                {
                    RhinoApp.WriteLine(string.Format("H2 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.H2 = Convert.ToDouble(values[i, 11]);

                if (values[i, 12] == null)
                {
                    RhinoApp.WriteLine(string.Format("H3 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.H3 = Convert.ToDouble(values[i, 12]);

                if (values[i, 13] == null)
                {
                    RhinoApp.WriteLine(string.Format("H4 not found for {0} CH {1}", controlLine, chainage));
                    return returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Failure);
                }
                shapeParameter.H4 = Convert.ToDouble(values[i, 13]);

                if (values[i, 14] != null)
                {
                    shapeParameter.H5 = Convert.ToDouble(values[i, 14]);
                }
                else
                {
                    RhinoApp.WriteLine(string.Format("Assuming H5={0} for {1} CH {2}",-0.525,controlLine,chainage));
                    shapeParameter.H5 = -0.525;
                }

                if (values[i, 15] != null)
                {
                    shapeParameter.H6 = Convert.ToDouble(values[i, 15]);
                }
                else
                {
                    RhinoApp.WriteLine(string.Format("Assuming H6={0} for {1} CH {2}", -0.525, controlLine, chainage));
                    shapeParameter.H6 = -0.525;
                }


                CLineProfile cLineProfile = new CLineProfile(shapeParameter);
                PolyCurve cLineProfilePolyCurve = cLineProfile.GetPolyCurve();

                shapeParameter.WallCLineELineOffset = WallCLineELineOffset;
                shapeParameter.CrownCLineELineOffset = CrownCLineELineOffset;
                shapeParameter.HitchRadius = HitchRadius;
                shapeParameter.HitchOffset = HitchOffset;

                ELineProfile eLineProfile = new ELineProfile(cLineProfile, shapeParameter);
                PolyCurve eLineProfilePolyCurve = eLineProfile.GetPolyCurve();

                double tunnel_span = ExportTunnelSpanCommand.getSpan(doc,eLineProfilePolyCurve);
                string supportName = "";
                double crownBoltLength = 0.0;

                BatchInstallBoltCommand.getBoltLength(tunnel_span, tsd, out crownBoltLength, out supportName);

                double wallBoltLength = 1.5;
                double angleOffCrownBoltExtent = 15 * Math.PI / 180;

                BoltedZoneProfile boltedZoneProfile = new BoltedZoneProfile(eLineProfile, wallBoltLength, crownBoltLength, angleOffCrownBoltExtent);
                PolyCurve boltedZoneProfilePolyCurve = boltedZoneProfile.GetPolyCurve();

                var cL = placeTunnelProfilesCommand.getControlLine(controlLineDictionary, controlLine, chainage);
                var cLProperty = cL.Profile.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                var transforms = placeTunnelProfilesCommand.getTranforms(cL, cLineProfilePolyCurve, chainage, controlLine, flip);
                placeTunnelProfilesCommand.transformTunnelProfile(eLineProfilePolyCurve, transforms, cLProperty, Models.TunnelProperty.ProfileRoleNameDictionary[ProfileRole.ELineProfile], doc, chainage);
                placeTunnelProfilesCommand.transformTunnelProfile(cLineProfilePolyCurve, transforms, cLProperty, Models.TunnelProperty.ProfileRoleNameDictionary[ProfileRole.CLineProfile], doc, chainage);
                placeTunnelProfilesCommand.transformTunnelProfile(boltedZoneProfilePolyCurve, transforms, cLProperty, Models.TunnelProperty.ProfileRoleNameDictionary[ProfileRole.BoltedZone], doc, chainage);

                if(keepTwoDProfiles)
                {
                    int profileForControlLineLayerIndex = UtilFunctions.AddNewLayer(doc, controlLine, profileLayerIndex);

                    int cLineLayerIndex = UtilFunctions.AddNewLayer(doc, controlLine + "_" + chainage.ToString("0.##") + "_" + "C-Line", profileForControlLineLayerIndex);
                    var attributes = new Rhino.DocObjects.ObjectAttributes();
                    attributes.LayerIndex = cLineLayerIndex;
                    doc.Objects.AddCurve(cLineProfilePolyCurve, attributes);

                    int eLineLayerIndex = UtilFunctions.AddNewLayer(doc, controlLine + "_" + chainage.ToString("0.##") + "_" + "E-Line", profileForControlLineLayerIndex);
                    attributes = new Rhino.DocObjects.ObjectAttributes();
                    attributes.LayerIndex = eLineLayerIndex;
                    doc.Objects.AddCurve(eLineProfilePolyCurve, attributes);

                    int boltedZoneLayerIndex = UtilFunctions.AddNewLayer(doc, controlLine + "_" + chainage.ToString("0.##") + "_" + "Bolted Zone", profileForControlLineLayerIndex);
                    attributes = new Rhino.DocObjects.ObjectAttributes();
                    attributes.LayerIndex = eLineLayerIndex;
                    doc.Objects.AddCurve(boltedZoneProfilePolyCurve, attributes);
                }
            }

            placeTunnelProfilesCommand.createSweep(doc);

            returnResult(xlApp, xlWorkbook, xlWorksheet, xlRange, Result.Success);

            doc.Views.Redraw();

            return Result.Success;
        }

        private Result returnResult(Excel.Application xlApp, Excel.Workbook xlWorkbook, Excel._Worksheet xlWorksheet, Excel.Range xlRange, Result result)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Marshal.ReleaseComObject(xlRange);
            Marshal.ReleaseComObject(xlWorksheet);

            xlWorkbook.Close();
            Marshal.ReleaseComObject(xlWorkbook);

            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);
            return result;
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

    class BoltedZoneProfile:TunnelProfile
    {
        PolyCurve Profile;
        double WallBoltLength;
        double CrownBoltLength;
        double AngleOffCrownBoltExtent;
        double BoltedZoneRadius;

        new public Point2d[] SetoutPoints;
        public override ProfileRole Role
        {
            get { return ProfileRole.BoltedZone; }
        }

        public override PolyCurve GetPolyCurve()
        {
            Profile = new PolyCurve();

            Line rightEWall = new Line(new Point3d(SetoutPoints[5].X, SetoutPoints[5].Y, 0), new Point3d(SetoutPoints[4].X, SetoutPoints[4].Y, 0));
            Profile.Append(rightEWall);

            Line rightOffCrownExtent = new Line(new Point3d(SetoutPoints[4].X, SetoutPoints[4].Y, 0), new Point3d(SetoutPoints[3].X, SetoutPoints[3].Y, 0));
            Profile.Append(rightOffCrownExtent);

            double mainArcStartAngle = Math.Acos(Math.Abs((SetoutPoints[6].X - SetoutPoints[3].X) / BoltedZoneRadius));
            double mainArcEndAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[6].X - SetoutPoints[2].X) / BoltedZoneRadius));
            Interval mainArcInterval = new Interval(mainArcStartAngle, mainArcEndAngle);
            Circle mainCircle = new Circle(new Point3d(SetoutPoints[6].X, SetoutPoints[6].Y, 0), BoltedZoneRadius);
            Arc mainArc = new Arc(mainCircle, mainArcInterval);
            Profile.Append(mainArc);

            Line leftOffCrownExtent = new Line(new Point3d(SetoutPoints[2].X, SetoutPoints[2].Y, 0), new Point3d(SetoutPoints[1].X, SetoutPoints[1].Y, 0));
            Profile.Append(leftOffCrownExtent);

            Line leftEWall = new Line(new Point3d(SetoutPoints[1].X, SetoutPoints[1].Y, 0), new Point3d(SetoutPoints[0].X, SetoutPoints[0].Y, 0));
            Profile.Append(leftEWall);

            return Profile;
        }

        public BoltedZoneProfile(ELineProfile eLineProfile,double wallBoltLength,double crownBoltLength,double angleOffCrownBoltExtent)
        {
            WallBoltLength = wallBoltLength;
            CrownBoltLength = crownBoltLength;
            AngleOffCrownBoltExtent = angleOffCrownBoltExtent;

            BoltedZoneRadius = eLineProfile.ShapeParameter.R1 + eLineProfile.ShapeParameter.CrownCLineELineOffset + CrownBoltLength;
            double eLineCrownSpreadAngle = Math.Atan((eLineProfile.SetoutPoints[10].X - eLineProfile.SetoutPoints[3].X) / (eLineProfile.SetoutPoints[3].Y-eLineProfile.SetoutPoints[10].Y));
            double angle3 = Math.Asin(eLineProfile.ShapeParameter.R1*Math.Sin(Math.PI-AngleOffCrownBoltExtent)/BoltedZoneRadius);
            double angle4 = AngleOffCrownBoltExtent - angle3;

            SetoutPoints = new Point2d[7];

            SetoutPoints[0] = new Point2d();
            SetoutPoints[0].X = eLineProfile.SetoutPoints[0].X - WallBoltLength;
            SetoutPoints[0].Y = eLineProfile.SetoutPoints[0].Y;

            SetoutPoints[1] = new Point2d();
            SetoutPoints[1].X = eLineProfile.SetoutPoints[1].X - WallBoltLength;
            SetoutPoints[1].Y = eLineProfile.SetoutPoints[1].Y;

            SetoutPoints[2] = new Point2d();
            SetoutPoints[2].X = eLineProfile.SetoutPoints[10].X + BoltedZoneRadius * Math.Cos(Math.PI / 2 + eLineCrownSpreadAngle + angle4);
            SetoutPoints[2].Y = eLineProfile.SetoutPoints[10].Y + BoltedZoneRadius * Math.Sin(Math.PI / 2 + eLineCrownSpreadAngle + angle4);

            SetoutPoints[3] = new Point2d();
            SetoutPoints[3].X = eLineProfile.SetoutPoints[10].X + BoltedZoneRadius * Math.Cos(Math.PI / 2 - eLineCrownSpreadAngle - angle4);
            SetoutPoints[3].Y = eLineProfile.SetoutPoints[10].Y + BoltedZoneRadius * Math.Sin(Math.PI / 2 - eLineCrownSpreadAngle - angle4);

            SetoutPoints[4] = new Point2d();
            SetoutPoints[4].X = eLineProfile.SetoutPoints[6].X + WallBoltLength;
            SetoutPoints[4].Y = eLineProfile.SetoutPoints[6].Y;

            SetoutPoints[5] = new Point2d();
            SetoutPoints[5].X = eLineProfile.SetoutPoints[7].X + WallBoltLength;
            SetoutPoints[5].Y = eLineProfile.SetoutPoints[7].Y;

            //Center of the arc
            SetoutPoints[6] = new Point2d();
            SetoutPoints[6].X = eLineProfile.SetoutPoints[10].X;
            SetoutPoints[6].Y = eLineProfile.SetoutPoints[10].Y;
        }
    }

    class ELineProfile:TunnelProfile
    {
        PolyCurve Profile;
        double CrownRadius;
        double AngleOfFloor;
        public TunnelProfileShapeParameter ShapeParameter;
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
            if (rightHitch.Length > 0)
            {
                Profile.Append(rightHitch);
            }
            

            double mainArcStartAngle = Math.Acos(Math.Abs((SetoutPoints[10].X - SetoutPoints[4].X) / CrownRadius));
            double mainArcEndAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[10].X - SetoutPoints[3].X) / CrownRadius));
            Interval mainArcInterval = new Interval(mainArcStartAngle, mainArcEndAngle);
            Circle mainCircle = new Circle(new Point3d(SetoutPoints[10].X, SetoutPoints[10].Y, 0), CrownRadius);
            Arc mainArc = new Arc(mainCircle, mainArcInterval);
            Profile.Append(mainArc);

            Arc leftHitch = new Arc(new Point3d(SetoutPoints[3].X, SetoutPoints[3].Y, 0), new Point3d(SetoutPoints[2].X, SetoutPoints[2].Y, 0), new Point3d(SetoutPoints[1].X, SetoutPoints[1].Y, 0));
            if(leftHitch.Length>0)
            {
                Profile.Append(leftHitch);
            }
            

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
            ShapeParameter.R3 = CrownRadius;
            AngleOfFloor = Math.Atan(((cLineProfile.SetoutPoints[5].Y - cLineProfile.SetoutPoints[0].Y) / (cLineProfile.SetoutPoints[5].X - cLineProfile.SetoutPoints[0].X)));

            SetoutPoints = new Point2d[11];

            //Main Arc Center
            SetoutPoints[10].X = cLineProfile.SetoutPoints[8].X;
            SetoutPoints[10].Y = cLineProfile.SetoutPoints[8].Y;

            //Left Side
            SetoutPoints[1].X = cLineProfile.SetoutPoints[0].X - ShapeParameter.WallCLineELineOffset;

            SetoutPoints[2].X = SetoutPoints[1].X - ShapeParameter.HitchOffset;

            SetoutPoints[8].X = SetoutPoints[2].X + ShapeParameter.HitchRadius;
            SetoutPoints[8].Y = cLineProfile.SetoutPoints[8].Y + Math.Sqrt(Math.Pow(CrownRadius - ShapeParameter.HitchRadius, 2) - Math.Pow(cLineProfile.SetoutPoints[8].X - SetoutPoints[8].X, 2));
            SetoutPoints[2].Y = SetoutPoints[8].Y;

            SetoutPoints[3].X = (CrownRadius * SetoutPoints[8].X - ShapeParameter.HitchRadius * cLineProfile.SetoutPoints[8].X) / (CrownRadius - ShapeParameter.HitchRadius);
            SetoutPoints[3].Y = cLineProfile.SetoutPoints[8].Y + Math.Sqrt(Math.Pow(CrownRadius,2) - Math.Pow(cLineProfile.SetoutPoints[8].X-SetoutPoints[3].X,2));

            SetoutPoints[1].Y = SetoutPoints[8].Y - Math.Sqrt(Math.Pow(shapeParameter.HitchRadius,2) - Math.Pow(SetoutPoints[1].X-SetoutPoints[8].X,2));

            SetoutPoints[0].X = SetoutPoints[1].X;
            SetoutPoints[0].Y = cLineProfile.SetoutPoints[0].Y - ShapeParameter.WallCLineELineOffset * Math.Tan(AngleOfFloor);

            //Right Side
            SetoutPoints[6].X = cLineProfile.SetoutPoints[5].X + ShapeParameter.WallCLineELineOffset;
            SetoutPoints[6].Y = SetoutPoints[1].Y;

            SetoutPoints[5].X = SetoutPoints[6].X + ShapeParameter.HitchOffset;
            

            SetoutPoints[9].X = SetoutPoints[5].X - ShapeParameter.HitchRadius;
            SetoutPoints[9].Y = SetoutPoints[8].Y;
            SetoutPoints[5].Y = SetoutPoints[9].Y;

            SetoutPoints[4].X = (CrownRadius * SetoutPoints[9].X - ShapeParameter.HitchRadius * cLineProfile.SetoutPoints[8].X) / (CrownRadius - ShapeParameter.HitchRadius);
            SetoutPoints[4].Y = SetoutPoints[3].Y;

            SetoutPoints[7].X = SetoutPoints[6].X;
            SetoutPoints[7].Y = cLineProfile.SetoutPoints[5].Y +ShapeParameter.WallCLineELineOffset * Math.Tan(AngleOfFloor);
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

            Line rightCWall = new Line(new Point3d(SetoutPoints[5].X, SetoutPoints[5].Y, 0), new Point3d(SetoutPoints[4].X,SetoutPoints[4].Y,0));
            Profile.Append(rightCWall);

            if(ShapeParameter.R2>0)
            {
                double rightArcStartAngle = Math.Acos(Math.Abs((SetoutPoints[7].X - SetoutPoints[4].X) / ShapeParameter.R2));
                double rightArcEndAngle = Math.Acos(Math.Abs((SetoutPoints[7].X - SetoutPoints[3].X) / ShapeParameter.R2));
                Interval rightArcInterval = new Interval(rightArcStartAngle, rightArcEndAngle);
                Circle rightCircle = new Circle(new Point3d(SetoutPoints[7].X, SetoutPoints[7].Y, 0), ShapeParameter.R2);
                Arc rightArc = new Arc(rightCircle, rightArcInterval);
                Profile.Append(rightArc);
            }
            
            if(ShapeParameter.R1>0)
            {
                double mainArcStartAngle = Math.Acos(Math.Abs((SetoutPoints[8].X - SetoutPoints[3].X) / ShapeParameter.R1));
                double mainArcEndAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[8].X - SetoutPoints[2].X) / ShapeParameter.R1));
                Interval mainArcInterval = new Interval(mainArcStartAngle, mainArcEndAngle);
                Circle mainCircle = new Circle(new Point3d(SetoutPoints[8].X, SetoutPoints[8].Y, 0), ShapeParameter.R1);
                Arc mainArc = new Arc(mainCircle, mainArcInterval);
                Profile.Append(mainArc);
            }
            
            if(ShapeParameter.R2>0)
            {
                double leftArcStartAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[6].X - SetoutPoints[2].X) / ShapeParameter.R2));
                double leftArcEndAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[6].X - SetoutPoints[1].X) / ShapeParameter.R2));
                Interval leftArcInterval = new Interval(leftArcStartAngle, leftArcEndAngle);
                Circle leftCircle = new Circle(new Point3d(SetoutPoints[6].X, SetoutPoints[6].Y, 0), ShapeParameter.R2);
                Arc leftArc = new Arc(leftCircle, leftArcInterval);
                Profile.Append(leftArc);
            }

            Line leftCWall = new Line(new Point3d(SetoutPoints[1].X, SetoutPoints[1].Y, 0), new Point3d(SetoutPoints[0].X, SetoutPoints[0].Y, 0));
            Profile.Append(leftCWall);

            Line Invert = new Line(new Point3d(SetoutPoints[0].X, SetoutPoints[0].Y, 0), new Point3d(SetoutPoints[5].X, SetoutPoints[5].Y, 0));
            Profile.Append(Invert);

            return Profile;
        }

        public CLineProfile(TunnelProfileShapeParameter tunnelProfileShapeParameter)
        {
            ShapeParameter = tunnelProfileShapeParameter;
            calculateSetoutPointsFromTunnelProfileShapeParameter();
        }

        private void calculateSetoutPointsFromTunnelProfileShapeParameter()
        {
            SetoutPoints = new Point2d[9];

            var Theta3 = Math.Atan((ShapeParameter.H2 - ShapeParameter.H3) / (0.5 * ShapeParameter.B1 - ShapeParameter.B3));

            //C1
            SetoutPoints[0] = new Point2d();
            SetoutPoints[0].X = -ShapeParameter.B2 - 0.5 * ShapeParameter.B1;
            SetoutPoints[0].Y = ShapeParameter.H6;

            //C2
            SetoutPoints[1] = new Point2d();
            SetoutPoints[1].X = -ShapeParameter.B2 - 0.5 * ShapeParameter.B1;
            SetoutPoints[1].Y = -ShapeParameter.H4 + ShapeParameter.H2;

            //C3
            SetoutPoints[2] = new Point2d();
            SetoutPoints[2].X = -ShapeParameter.B2 - ShapeParameter.B3 - ShapeParameter.R2 * Math.Cos(ShapeParameter.Theta2 + Theta3);
            SetoutPoints[2].Y = -ShapeParameter.H4 + ShapeParameter.H3 + ShapeParameter.R2 * Math.Sin(ShapeParameter.Theta2 + Theta3);

            //C4
            SetoutPoints[3] = new Point2d();
            SetoutPoints[3].X = -ShapeParameter.B2 + ShapeParameter.B3 + ShapeParameter.R2 * Math.Cos(ShapeParameter.Theta2 + Theta3);
            SetoutPoints[3].Y = -ShapeParameter.H4 + ShapeParameter.H3 + ShapeParameter.R2 * Math.Sin(ShapeParameter.Theta2 + Theta3);

            //C5
            SetoutPoints[4] = new Point2d();
            SetoutPoints[4].X = -ShapeParameter.B2 + 0.5 * ShapeParameter.B1;
            SetoutPoints[4].Y = -ShapeParameter.H4 + ShapeParameter.H2;

            //C6
            SetoutPoints[5] = new Point2d();
            SetoutPoints[5].X = -ShapeParameter.B2 + 0.5 * ShapeParameter.B1;
            SetoutPoints[5].Y = ShapeParameter.H5;

            //C7
            SetoutPoints[6] = new Point2d();
            SetoutPoints[6].X = -ShapeParameter.B2 - ShapeParameter.B3;
            SetoutPoints[6].Y = -ShapeParameter.H4 + ShapeParameter.H3;

            //C8
            SetoutPoints[7] = new Point2d();
            SetoutPoints[7].X = -ShapeParameter.B2 + ShapeParameter.B3;
            SetoutPoints[7].Y = -ShapeParameter.H4 + ShapeParameter.H3;

            //C9
            SetoutPoints[8] = new Point2d();
            SetoutPoints[8].X = -ShapeParameter.B2;
            SetoutPoints[8].Y = -ShapeParameter.H4;
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
        // Parameters below are not officially present in the drawing
        // H5 and H6 are used to define profiles below control level
        public double H5;
        public double H6;
        public double Theta3;
        public double R3;
        public double R4;

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
