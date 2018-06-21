using System;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Drawing;

using Rhino.Geometry;
using KDTree.Core;
using Volvox_Cloud;

namespace KDTree.Common
{
  
    public class KDTreeLib
    {

        /// <summary>
        /// Constructs a KDTree from a PointCloud.
        /// </summary>
        /// <param name="cloud"></param>
        /// <returns></returns>
        public static KDTree<int> ConstructKDTree(PointCloud cloud)
        {
            
            //Initialize KDTree with three Dimentions.
            KDTree<int> tree = new KDTree<int>(3);

            //Add Points to KDTree.
            for (int i = 0; i <= cloud.Count - 1; i++)
            {
               tree.AddPoint(new double[] { cloud[i].Location.X, cloud[i].Location.Y, cloud[i].Location.Z }, i);
            }
            return tree;

        }

    
        /// <summary>
        /// Duplicates a KDTree.
        /// </summary>
        /// <param name="KDTree"></param>
        /// <returns></returns>
        public static KDTree<int> DubplicateTree(KDTree<int> KDTree)
        {
           using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, KDTree);
                ms.Position = 0;
                return (KDTree<int>)formatter.Deserialize(ms);
            }
        }


        /// <summary>
        /// Search for a number of nearest neighbors to a point(s) in a KDTree Structure. 
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="pts"></param>
        /// <param name="range"></param>
        /// <param name="maxReturned"></param>
        /// <returns></returns>
        public static List<int>[] NearestNeighborSearch(KDTree<int> tree, List<Point3d> pts, List<double> range, int maxReturned)
        {
           if (range.Count == 1)
            {
                for (int i = 1; i < pts.Count; i++)
                {
                    range.Add(range[0]);
                }
            }

            List<int>[] indices = new List<int>[pts.Count];

            //Create Partitions for multithreading.
            var rangePartitioner = System.Collections.Concurrent.Partitioner.Create(0, pts.Count, (int)Math.Ceiling(pts.Count / (double)Environment.ProcessorCount));

            //Run MultiThreaded Loop.
            System.Threading.Tasks.Parallel.ForEach(rangePartitioner, (rng, loopState) =>

            {
                for (int i = rng.Item1; i < rng.Item2; i++)
                {
                    //Point to measure From
                    var p = pts[i];
                    //Range
                    var r = range[i];
                    //Get one Nearest Neighbor (Part of KDTree.dll)
                    var ns = tree.NearestNeighbors(new double[] { p.X, p.Y, p.Z }, maxReturned, r * r);
                    var indList = ns.ToList();

                    indices[i] = indList;
                }
            }
              );

            return indices;
        }


        
        /// <summary>
        /// Calculate closestPoint distances between the two Clouds (KDTree Already Constructed).
        /// </summary>
        /// <param name="cloudFrom"></param>
        /// <param name="KDTreeCloud"></param>
        /// <param name="Amount">Amount of closest points to average out.</param>/param
        /// <returns></returns>
        public static double[] CloudCloudDist(PointCloud cloudFrom, Tuple<KDTree<int>, GH_Cloud> KDTreeCloud, int Amount)
        {
            //KDTree to meassure Distance To.
            KDTree<int> tree = KDTreeCloud.Item1;
            //Cloud to meassure Distance To.
            PointCloud cloudTo = KDTreeCloud.Item2.Value;
            //Get List of Points for PointCLoud to Meassure Distance From.
            Point3d[] pts = cloudFrom.GetPoints();


            //Create Partitions for multithreading.
            var rangePartitioner = System.Collections.Concurrent.Partitioner.Create(0, pts.Length, (int)Math.Ceiling(pts.Length / (double)Environment.ProcessorCount));


            //Crete empty Distance Array.
            double[] dist = new double[pts.Length];
            //Run MultiThreaded Loop.
            System.Threading.Tasks.Parallel.ForEach(rangePartitioner, (rng, loopState) =>

            {
                for (int i = rng.Item1; i < rng.Item2; i++)
                {
                    //Point to measure From
                    var p = pts[i];
                    //Range
                    var r = Double.MaxValue;
                    //Get one Nearest Neighbor (Part of KDTree.dll)
                    var ns = tree.NearestNeighbors(new double[] { p.X, p.Y, p.Z }, Amount, r);
                    //The index of the Nearest Point in the PointCloud to Measure To.
                    var indList = ns.ToList();

                    double D = 0.0;
                    foreach (int idx in indList)
                    {
                        //Distance between Point measured From to Point meassured To.
                        //Point meassured From.
                        Point3d P2 = cloudTo[idx].Location;
                        //Distance betwen Points
                        //double D = fastDist(p, P2);
                        //Cumulative Distance
                        D = D + p.DistanceTo(P2);
                    }
                    //Average distances
                    double  aveD = D / indList.Count;
                    //Add Distances to Array.
                    dist[i] = aveD;
                }
            }
              );

            return dist;
        }

        /// <summary>
        /// Calculate Normals for a KDTreeCloud. 
        /// </summary>
        /// <param name="KDTreeCloud">KDTreeCloud to calculate normals on.</param>
        /// <param name="NeighborRange">Distance Range for Nearest Neighbors to each point in Cloud for the calculation of normal.</param>
        /// <param name="NeighborAmount">Ámount of Nearest Neighbors to use for calculation of normal. Minimum 3.</param>
        /// <param name="Guide">Guiding Vector or Centering Point to orient Normal. Supply a vector3d and if centering point is chosen for GuideStyle, 
        /// then the Vector3d will be directly tranlated to a point.</param>
        /// <param name="GuideStyle">Choose between Guiding Vector3d or Guiding Center Point. Vector3d = 0, CenterPoint = 1.</param>
        /// <param name="color">Boolean telling if Cloud should be colored according to Normals. </param>
        /// <returns></returns>
        public static Tuple<KDTree<int>, PointCloud> CalcNormals(Tuple<KDTree<int>, PointCloud> KDTreeCloud, double NeighborRange, int NeighborAmount,
            Vector3d Guide, int GuideStyle, int color)
        {
            //Deconstruct KDTreeCloud into KDTree and PointCloud
            KDTree<int> tree = KDTreeCloud.Item1;
            PointCloud cloud = (PointCloud)KDTreeCloud.Item2.Duplicate();
            
            //Get all points in PointCloud. Use these to search for nearest neighbors per point to estimate a plane. 
            List<Point3d> points = cloud.GetPoints().ToList();



            //Search for NearestNeighbors per point in PointCloud. Gets the indices in the Cloud for the neighbors to each Point.
            List<double> NRange = new List<double> { NeighborRange };
            List<int>[] idc = NearestNeighborSearch(tree, points, NRange, NeighborAmount);

            //Calculate the not-oriented normals per point. 
            for (int i = 0; i < idc.Length - 1; i++)
            {
                //Add points to calculate normals from to list. 
                List<Point3d> cullPoints = new List<Point3d>();
                List<Point3d> normalPts = new List<Point3d>();
                normalPts.Add(cloud[i].Location);
                List<int> ids = idc[i];

                if (ids.Count >= 3)
                {
                    for (int j = 0; j < ids.Count; j++)
                    {
                        Point3d nPt = cloud[ids[j]].Location;
                        normalPts.Add(nPt);
                    }
                    //Get not-oriented normal from Z-Axis of best fit plane to NormalPoints.
                    Plane normalPln = new Plane();
                    Plane.FitPlaneToPoints(normalPts, out normalPln);

                    Vector3d notOriNormal = normalPln.ZAxis;

                    //Orient not-oriented NormalVector according to the guiding vector.
                    Point3d PP = new Point3d(Guide);

                    Vector3d guideVector = new Vector3d();
                    if (GuideStyle == 0) { guideVector = Guide; }
                    else if (GuideStyle == 1) { guideVector = Point3d.Subtract(cloud[i].Location, new Point3d(Guide)); }
                    else { break; }

                    Vector3d normalVector = new Vector3d();
                    double vecAngle = Vector3d.VectorAngle(notOriNormal, guideVector);
                    if (vecAngle < Math.PI / 2) { normalVector = notOriNormal; }
                    else
                    {
                        normalVector = notOriNormal;
                        normalVector.Reverse();
                    }

                    //Add normal to Cloud.
                    cloud[i].Normal = normalVector;

                    //Colour Cloud.
                        if (color != 0)
                        {
                             cloud[i].Color  = normalColors(color, normalVector, cloud[i].Color);
                        }

                   

                }


                else
                {
                    cullPoints.Add(cloud[i].Location);
                }


            }

          Tuple<KDTree<int>, PointCloud> newKDTreeCloud = new Tuple<KDTree<int>, PointCloud>(tree, cloud);
            return newKDTreeCloud;
        }

      

        public static Color normalColors(int ColorStyle, Vector3d normalVector, Color CloudCOlor)
        {

            Color colour = new Color();
            //Color Cloud according to Normals.
            if (ColorStyle == 1)
            {
                int R = (int)Math.Round((normalVector.X - (-1.0)) / (1 - (-1)) * 255);
                int G = (int)Math.Round((normalVector.Y - (-1.0)) / (1 - (-1)) * 255);
                int B = (int)Math.Round((normalVector.Z - (-1.0)) / (1 - (-1)) * 255);

                colour = Color.FromArgb(R, G, B);

            }
            else if (ColorStyle == 2)
            {
                int R = (int)Math.Round((normalVector.X - (-1.0)) / (1 - (-1)) * 255);
                int G = (int)Math.Round((normalVector.Y - (-1.0)) / (1 - (-1)) * 255);
                int B = (int)Math.Round((normalVector.Z - (-1.0)) / (1 - (-1)) * 255);

                int grey = (int)(R * 0.3 + G * 0.59 + B * 0.11);

                colour = Color.FromArgb(grey, grey, grey);
            }

            else if (ColorStyle == 3)
            {
                int R = (int)Math.Round((normalVector.X - (-1.0)) / (1 - (-1)) * 255);
                int G = (int)Math.Round((normalVector.Y - (-1.0)) / (1 - (-1)) * 255);
                int B = (int)Math.Round((normalVector.Z - (-1.0)) / (1 - (-1)) * 255);

                int grey = (int)(R * 0.3 + G * 0.59 + B * 0.11);

                double pct = 0.5;
                byte Rp = (byte)((CloudCOlor.R * pct) + grey * (1 - pct));
                byte Gp = (byte)((CloudCOlor.G * pct) + grey * (1 - pct));
                byte Bp = (byte)((CloudCOlor.B * pct) + grey * (1 - pct));

                colour = Color.FromArgb(Rp, Gp, Bp);

            }
            return colour;
        }

    }
}
