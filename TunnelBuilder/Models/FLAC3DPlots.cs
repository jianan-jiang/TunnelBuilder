using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using SuperXML;

namespace TunnelBuilder.Models.FLAC3D
{
    public class Camera
    {
        public Point3d Center;
        public Point3d Eye;
        public double Roll;

        public Camera(Point3d center,Point3d eye, double roll)
        {
            Center = center;
            Eye = eye;
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
}
