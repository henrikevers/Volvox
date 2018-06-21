using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;



using Rhino.Geometry;


namespace KDTree.Common.Utils
{
    public class Util
    {
        
        /// <summary>
        /// Blend colors in a point cloud with a list of colors same amount as amount of points) or one color. 
        /// </summary>
        /// <param name="cloud"></param>
        /// <param name="color"></param>
        /// <param name="pct"></param>
        /// <returns></returns>
        public static PointCloud BlendColors(PointCloud cloud, List<Color> color, Double pct)
        {

            PointCloud newCloud = new PointCloud(cloud);


            //Create Partitions for multithreading.
            var rangePartitioner = System.Collections.Concurrent.Partitioner.Create(0, newCloud.Count, (int)Math.Ceiling(newCloud.Count / (double)Environment.ProcessorCount));

            List<PointCloud> subclouds = new List<PointCloud>();
            //Run MultiThreaded Loop.
            System.Threading.Tasks.Parallel.ForEach(rangePartitioner, (rng, loopState) =>
            {
                PointCloud subCloud = new PointCloud();
                for (int j = rng.Item1; j < rng.Item2; j++)
                {
                    Color Ptcolor = newCloud[j].Color;
                    Color newColor = new Color();
                    if (color.Count == 1)
                    {newColor = Blend(Ptcolor, color[0], pct); }
                    else
                    {newColor = Blend(Ptcolor, color[j], pct); }
                    
                    subCloud.Add(newCloud[j].Location, newColor);
                }
                subclouds.Add(subCloud);
            }

            );

            PointCloud mergeCloud = new PointCloud();
            for (int i = 0; i < subclouds.Count; i++)
            {
                mergeCloud.Merge(subclouds[i]);
            }
            return mergeCloud;
        }

        /// <summary>
        /// Blend Color
        /// </summary>
        /// <param name="color"></param>
        /// <param name="backColor"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static Color Blend(Color color, Color backColor, double amount)
        {
            byte r = (byte)((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backColor.B * (1 - amount));
            return Color.FromArgb(r, g, b);
        }
    }
}





