using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using SuperXML;

using Microsoft.Office.Interop.Excel;

namespace TunnelBuilder.Models.FLAC3D
{
    public class Camera
    {
        public Point3d Center;
        public Point3d Eye;
        public double Roll;

        public Camera(Point3d center,Point3d eye, double roll)
        {
            Vector3d direction = eye - center;

            Center = center;
            Eye = center + 1.0/2.0*direction;
            Roll = roll;
        }

        public string CenterCoordinates
        {
            get
            {
                return string.Format("({0:0.0000},{1:0.0000},{2:0.0000})",Center.X,Center.Y,Center.Z);
            }
        }

        public string EyeCoordinates
        {
            get
            {
                return string.Format("({0:0.0000},{1:0.0000},{2:0.0000})", Eye.X, Eye.Y, Eye.Z);
            }
        }
    }

    public class Section
    {
        public Point3d Origin;
        public Vector3d Normal;

        public Section(Point3d origin, Vector3d normal)
        {
            Origin = origin;
            Normal = normal;
        }

        public Section(Point3d origin, double dip,double dipDirection)
        {
            Origin = origin;
            double dipRadian = dip * Math.PI / 180;
            double dipDirectionRadian = dipDirection * Math.PI / 180;

            double x = Math.Cos(dipDirectionRadian);
            double y = Math.Sin(dipDirectionRadian);
            double z = Math.Cos(dip);

            Normal = new Vector3d(x, y, z);
            Normal.Unitize();
        }

        public string OriginCoordinates
        {
            get { return string.Format("({0:0.0000},{1:0.0000},{2:0.0000})", Origin.X, Origin.Y, Origin.Z); }
        }

        public string NormalCoordinates
        {
            get { return string.Format("({0:0.0000},{1:0.0000},{2:0.0000})", Normal.X, Normal.Y, Normal.Z); }
        }
    }

    public class Plot
    {
        public Camera Camera;
        string PlotTemplate;
        protected Compiler Compiler;

        public Plot(Camera camera, string plotTemplate)
        {
            Camera = camera;
            PlotTemplate = plotTemplate;
            Compiler = new Compiler();
            Compiler.AddKey("CAMERA_CENTER_COORDINATES", Camera.CenterCoordinates);
            Compiler.AddKey("CAMERA_EYE_COORDINATES", Camera.EyeCoordinates);
            Compiler.AddKey("CAMERA_ROLL", string.Format("{0:0.0000}", Camera.Roll));
        }

        public virtual string compile()
        {
            return Compiler.CompileString(PlotTemplate);
        }
    }

    public class SectionPlot:Plot
    {
        public Section Section;
        string SectionName;
        public SectionPlot(Camera camera,Section section, string plotTemplate,string sectionName):base(camera,plotTemplate)
        {
            Section = section;
            SectionName = sectionName;

            base.Compiler.AddKey("SECTION_ORIGIN", Section.OriginCoordinates);
            base.Compiler.AddKey("SECTION_NORMAL", Section.NormalCoordinates);
            base.Compiler.AddKey("SECTION_NAME", SectionName);
        }

        public override string compile()
        {
            return base.compile();
        }
    }

    public class PlotDescriptionExcelFile
    {
        Application ExcelApplication;
        Workbook Workbook;

        List<PlotDescription> PlotDescriptions;
        public Point2d OriginOffset;

        public PlotDescriptionExcelFile()
        {
            PlotDescriptions = new List<PlotDescription>();
            OriginOffset = new Point2d();
        }

        public bool addPlotDescription(PlotDescription plotDescription)
        {
            PlotDescriptions.Add(plotDescription);
            return true;
        }

        public bool save(string folder)
        {
            try
            {
                ExcelApplication = new Application();
                ExcelApplication.Visible = false;
                ExcelApplication.DisplayAlerts = false;

                Workbook = ExcelApplication.Workbooks.Add(Type.Missing);

                var plotDescriptionSheet = (Worksheet)Workbook.ActiveSheet;
                plotDescriptionSheet.Name = "Plots";

                plotDescriptionSheet.Cells[1, 1] = "Plot Data File Name";
                plotDescriptionSheet.Cells[1, 2] = "Description";
                plotDescriptionSheet.Cells[1, 3] = "Section";

                int rowIndex = 2;

                foreach (var plotDescription in PlotDescriptions)
                {
                    plotDescriptionSheet.Cells[rowIndex, 1] = plotDescription.DataFileName;
                    plotDescriptionSheet.Cells[rowIndex, 2] = plotDescription.Description;
                    plotDescriptionSheet.Cells[rowIndex, 3] = plotDescription.Section;
                    rowIndex = rowIndex + 1;
                }

                var runsSheet = (Worksheet)Workbook.Sheets.Add(Type.Missing, Workbook.Sheets[Workbook.Sheets.Count], Type.Missing, Type.Missing);
                runsSheet.Name = "Runs";
                runsSheet.Cells[1, 1] = "RunFile";
                runsSheet.Cells[1, 2] = "Run Descritption";
                runsSheet.Cells[1, 3] = "Attachment Ref";

                var surfaceDisplacementSheet = (Worksheet)Workbook.Sheets.Add(Type.Missing, Workbook.Sheets[Workbook.Sheets.Count], Type.Missing, Type.Missing);
                surfaceDisplacementSheet.Name = "Surface_displacement";
                surfaceDisplacementSheet.Cells[1, 1] = "orig_x";
                surfaceDisplacementSheet.Cells[1, 2] = OriginOffset.X;
                surfaceDisplacementSheet.Cells[2, 1] = "orig_y";
                surfaceDisplacementSheet.Cells[2, 2] = OriginOffset.Y;

                Workbook.SaveAs(folder + "\\Plot Description.xlsx",Type.Missing,Type.Missing,Type.Missing,Type.Missing,Type.Missing,XlSaveAsAccessMode.xlNoChange,Type.Missing,Type.Missing,Type.Missing,Type.Missing,Type.Missing);
                Workbook.Close(false);

                ExcelApplication.Quit();
            }
            catch
            {
                return false;
            }
            

            return true;
        }
    }

    public struct PlotDescription
    {
        public string DataFileName;
        public string Description;
        public string Section;

        public PlotDescription(string dataFileName, string description, string section)
        {
            DataFileName = dataFileName;
            Description = description;
            Section = section;
        }

        public PlotDescription(string dataFileName, string description)
        {
            DataFileName = dataFileName;
            Description = description;
            Section = "";
        }
    }
}
