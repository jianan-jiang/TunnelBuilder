using System;
using System.IO;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
namespace NurbsMesher
{
    public class NurbsMesher
    {
        public NurbsMesher(Rhino.Geometry.NurbsSurface surface)
        {
            var controlPoints = surface.Points;
            
        }

        public bool Save(string path)
        {
            FileStream fileStream = new FileStream(path, FileMode.Create);
            MeshFileConfiguration configuration = new MeshFileConfiguration();
            configuration.fileVersion = MeshFileVersion.One;
            configuration.dimension = MeshDimension.Three;
            MeshFile meshFile = new MeshFile(configuration);
            return meshFile.Write(fileStream);
        }
    }

    public abstract class Geometry
    {
        public int[] VertexIndicies;
        public abstract string Description();

        public abstract GeometryType GeometryType
        {
            get;
        }
        
        public Geometry(int[] indicies)
        {
            VertexIndicies = indicies;
        }
    }

    public class Point : Geometry
    {
        public override string Description()
        {
            return String.Format("0 {0}", VertexIndicies[0]);
        }

        public override GeometryType GeometryType
        {
            get { return GeometryType.Point; }
        }

        private static int[] toIndicies(int i)
        {
            int[] result = new int[1];
            result[0] = i;
            return result;
        }

        public Point(int i):base(toIndicies(i))
        {
            if(i<0)
            {
                throw new ArgumentException();
            }
        }
    }

    public class Element
    {
        public int ElementAttribute;
        public Geometry Geometry;

        public string Description()
        {
            return String.Format("{0} {1}", ElementAttribute, Geometry.Description());
        }

        public Element()
        {

        }
    }

    public class MeshFile
    {
        MeshFileConfiguration Configuration;
        private string[] GetHeader()
        {
            List<string> result = new List<string>();
            string versionText = "";
            switch (Configuration.fileVersion)
            {
                case MeshFileVersion.One:
                    versionText = "MFEM NURBS mesh v1.0"; break;
                case MeshFileVersion.OnePointOne:
                    versionText = "MFEM NURBS mesh v1.1"; break;
                default:
                    throw new ArgumentException();
            }
            result.Add(versionText);
            result.Add("");
            result.Add("dimension");
            switch (Configuration.dimension)
            {
                case MeshDimension.Two:
                    result.Add("2"); break;
                case MeshDimension.Three:
                    result.Add("3"); break;
                default:
                    throw new ArgumentException();
            }
            return result.ToArray();
          
        }

        public MeshFile(MeshFileConfiguration configuration)
        {
            Configuration = configuration;
        }

        public bool Write(Stream stream)
        {
            StreamWriter meshStreamWriter = new StreamWriter(stream);
            string[] header = GetHeader();
            for(int i=0;i<header.Length;i++)
            {
                meshStreamWriter.WriteLine(header[i]);
            }
            return true;
        }
    }

    public struct MeshFileConfiguration
    {
        public MeshFileVersion fileVersion;
        public MeshDimension dimension;
    }

    public enum MeshFileVersion
    {
        One,
        OnePointOne
    }

    public enum MeshDimension
    {
        Two=2,
        Three=3
    }

    public enum GeometryType
    {
        Point=0,
        Segment=1,
        Triangle=2,
        Square=3,
        Tetrahedron =4,
        Cube=5,
        Prism=6
    }
}
