using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.Commands;
using TunnelBuilder.Models;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("E56AAFD5-4E3F-41CF-8AC8-9EDC035EAA2C")]
    public class ProfileTestCommand:Command
    {
        public override string EnglishName
        { get { return "testProfile"; } }

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
    }

    class ELineProfile:TunnelProfile
    {
        PolyCurve Profile;
        public override ProfileRole Role
        {
            get { return ProfileRole.ELineProfile; }
        }
        public override PolyCurve GetPolyCurve()
        {
            return Profile;
        }
    }

    class CLineProfile:TunnelProfile
    {
        PolyCurve Profile;
        Point2d[] SetoutPoints;
        CLineShapeParameter CLineShapeParameter;
        public override ProfileRole Role
        {
            get { return ProfileRole.CLineProfile; }
        }
        public override PolyCurve GetPolyCurve()
        {
            Profile = new PolyCurve();

            Line rightCWall = new Line(new Point3d(SetoutPoints[8].X, SetoutPoints[8].Y, 0), new Point3d(SetoutPoints[3].X,SetoutPoints[3].Y,0));
            Profile.Append(rightCWall);

            double rightArcStartAngle = Math.Acos(Math.Abs((SetoutPoints[5].X - SetoutPoints[3].X) / CLineShapeParameter.R2));
            double rightArcEndAngle = Math.Acos(Math.Abs((SetoutPoints[5].X - SetoutPoints[2].X) / CLineShapeParameter.R2));
            Interval rightArcInterval = new Interval(rightArcStartAngle, rightArcEndAngle);
            Circle rightCircle = new Circle(new Point3d(SetoutPoints[5].X, SetoutPoints[5].Y, 0), CLineShapeParameter.R2);
            Arc rightArc = new Arc(rightCircle, rightArcInterval);
            Profile.Append(rightArc);

            double mainArcStartAngle = Math.Acos(Math.Abs((SetoutPoints[6].X - SetoutPoints[2].X) / CLineShapeParameter.R2));
            double mainArcEndAngle = Math.PI-Math.Acos(Math.Abs((SetoutPoints[6].X - SetoutPoints[1].X) / CLineShapeParameter.R2));
            Interval mainArcInterval = new Interval(mainArcStartAngle, mainArcEndAngle);
            Circle mainCircle = new Circle(new Point3d(SetoutPoints[6].X, SetoutPoints[6].Y, 0), CLineShapeParameter.R1);
            Arc mainArc = new Arc(mainCircle, mainArcInterval);
            Profile.Append(mainArc);

            double leftArcStartAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[4].X - SetoutPoints[1].X) / CLineShapeParameter.R2));
            double leftArcEndAngle = Math.PI - Math.Acos(Math.Abs((SetoutPoints[4].X - SetoutPoints[0].X) / CLineShapeParameter.R2));
            Interval leftArcInterval = new Interval(leftArcStartAngle, leftArcEndAngle);
            Circle leftCircle = new Circle(new Point3d(SetoutPoints[4].X, SetoutPoints[4].Y, 0), CLineShapeParameter.R2);
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
            CLineShapeParameter = new CLineShapeParameter();
            CLineShapeParameter.B1 = sp.getB1();
            switch (gp.CrownRadiusOption)
            {
                case CrownRadiusOption.R1EqualToW:
                    CLineShapeParameter.R1 = CLineShapeParameter.B1;
                    break;
                case CrownRadiusOption.R1EqualToWPlusTwo:
                    CLineShapeParameter.R1 = CLineShapeParameter.B1 + 2;
                    break;
                case CrownRadiusOption.R1EqualToWRounded:
                    CLineShapeParameter.R1 = Math.Round(CLineShapeParameter.B1*2,MidpointRounding.AwayFromZero)/2;
                    break;
                default:
                    throw new ArgumentException();
            }
            CLineShapeParameter.R2 = CLineShapeParameter.R1 / 4;
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
            SetoutPoints[4].X = sp.SetoutPoints[0].X + CLineShapeParameter.R2 * Math.Sin(gp.AngleofHaunchCircle);
            //Xc6
            SetoutPoints[5] = new Point2d();
            SetoutPoints[5].X = sp.SetoutPoints[9].X - CLineShapeParameter.R2 * Math.Sin(gp.AngleofHaunchCircle);
            //Xc2
            SetoutPoints[1] = new Point2d();
            SetoutPoints[1].X = (CLineShapeParameter.R1 * SetoutPoints[4].X - CLineShapeParameter.R2 * SetoutPoints[6].X) / (CLineShapeParameter.R1 - CLineShapeParameter.R2);
            //Xc3
            SetoutPoints[2] = new Point2d();
            SetoutPoints[2].X = (CLineShapeParameter.R1 * SetoutPoints[5].X - CLineShapeParameter.R2 * SetoutPoints[6].X) / (CLineShapeParameter.R1 - CLineShapeParameter.R2);
            //Xc1
            SetoutPoints[0].X = sp.SetoutPoints[0].X;
            //Xc4
            SetoutPoints[3].X = sp.SetoutPoints[9].X;


            //Yc7
            SetoutPoints[6].Y = getYc7(sp);
            //Yc5
            SetoutPoints[4].Y = SetoutPoints[6].Y + Math.Sqrt(Math.Pow(CLineShapeParameter.R1 - CLineShapeParameter.R2, 2) - Math.Pow(SetoutPoints[6].X - SetoutPoints[4].X, 2));
            //Yc1
            SetoutPoints[0] = new Point2d();
            SetoutPoints[0].Y = SetoutPoints[4].Y + Math.Sqrt(Math.Pow(CLineShapeParameter.R2, 2) - Math.Pow(SetoutPoints[4].X - sp.SetoutPoints[0].X, 2));
            //Yc2
            SetoutPoints[1].Y = SetoutPoints[6].Y + Math.Sqrt(Math.Pow(CLineShapeParameter.R1, 2) - Math.Pow(SetoutPoints[6].X - SetoutPoints[1].X, 2));
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
            double max = Yc7(sp.SetoutPoints[0]);
            for(int i=1;i<sp.SetoutPoints.Length;i++)
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
                return point.Y - Math.Sqrt(Math.Pow(CLineShapeParameter.R2, 2) - Math.Pow(SetoutPoints[4].X - point.X, 2));
            }
            return Double.NaN;
        }

        private double Yc6(Point2d point)
        {
            if(point.X>=SetoutPoints[2].X)
            {
                return point.Y - Math.Sqrt(CLineShapeParameter.R2 - Math.Pow(point.X - SetoutPoints[5].X, 2));
            }
            return Double.NaN;
        }

        private double Yc7(Point2d point)
        {
            if(point.X>SetoutPoints[1].X && point.X<SetoutPoints[2].X)
            {
                return point.Y - Math.Sqrt(Math.Pow(CLineShapeParameter.R1, 2) - Math.Pow(CLineShapeParameter.R1 - point.X, 2));
            }
            if(point.X<SetoutPoints[1].X)
            {
                return Yc5(point) - Math.Sqrt(Math.Pow(CLineShapeParameter.R1 - CLineShapeParameter.R2, 2) - Math.Pow(SetoutPoints[6].X - SetoutPoints[4].X, 2));
            }
            if(point.X>=SetoutPoints[2].X)
            {
                return Yc6(point) - Math.Sqrt(Math.Pow(CLineShapeParameter.R1 - CLineShapeParameter.R2, 2) - Math.Pow(SetoutPoints[5].X-SetoutPoints[6].X, 2));
            }
            return Double.NaN;
        }
    }

    class DLineProfile:TunnelProfile
    {
        PolyCurve Profile;
        public override ProfileRole Role
        {
            get { return ProfileRole.ELineProfile; }
        }

        public DLineProfile(DLineShapeParameter param)
        {
            Profile = new PolyCurve();
            for(int i=0;i<param.SetoutPoints.Length-1;i++)
            {
                Profile.Append(new Line(param.SetoutPoints[i].X, param.SetoutPoints[i].Y, 0, param.SetoutPoints[i + 1].X, param.SetoutPoints[i + 1].Y, 0));
            }
            TunnelProperty tunnelProperty = new TunnelProperty();
            tunnelProperty.ProfileRole = RoleName;
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

    class CLineShapeParameter
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
    }

    class DLineShapeParameter
    {
        private Point2d[] setoutPoints = new Point2d[10];
        
        public DLineShapeParameter(Point2d[] sP)
        {
            if(sP.Length!=10)
            {
                throw new ArgumentException();
            }
            setoutPoints = sP;
        }
        public Point2d[] SetoutPoints
        {
            get { return setoutPoints; }
            set { setoutPoints = value; }
        }

        public double getB1()
        {
            double max = setoutPoints[0].X;
            double min = setoutPoints[0].X;

            foreach(Point2d p in setoutPoints)
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
