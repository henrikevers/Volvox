using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;


using Grasshopper.Kernel;
using Rhino.Geometry;
using KDTree.Core;
using KDTree.Common;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Volvox.GH.Params;
using Volvox_Cloud;

namespace Volvox.GH.Component
{
    public class NearestNeighbor : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NearestNeighbor class.
        /// </summary>
        public NearestNeighbor()
          : base("Nearest Neighbor", "NN",
              "Find Nearest Neighbors to Point(s).",
              "Volvox", "KDTree")
        {
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_KDTree(), "KDTree", "K", "KDTree PointCloud Structure", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "P", "Point(s) to search From.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Range", "R", "Range to look for Nearest Neighbor. Single Value or Same number as Points.", GH_ParamAccess.list, double.MaxValue);
            pManager.AddIntegerParameter("Number of Neighbors", "N", "Maximum Number of Nearest Neibors returned.", GH_ParamAccess.item, 100);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "Closest point in cloud", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Normal", "N", "Normal at point", GH_ParamAccess.tree);
            pManager.AddColourParameter("Color", "C", "Color at point", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Distance", "D", "Distance to closest point", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Index", "I", "Closest point index", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Type_KDTree treeT = null;
            List<Point3d> pts = new List<Point3d>();
            List<double> range = new List<double>();
            int maxReturned = 100;

            if (!DA.GetData("KDTree", ref treeT)) return;
            if (!DA.GetDataList("Points", pts)) return;
            if (!DA.GetDataList("Range", range)) return;
            DA.GetData("Number of Neighbors", ref maxReturned);

            Tuple<KDTree<int>, GH_Cloud> KDTreeCloud = treeT.Value;
            KDTree<int> tree = KDTreeCloud.Item1;
            PointCloud cloud = KDTreeCloud.Item2.Value;
            

            if (range.Count > 1 && range.Count < pts.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Amount of Range values should be a single value, or match the amount of search points.");
                return;
            }

            List<int>[] idc = KDTreeLib.NearestNeighborSearch(tree, pts, range, maxReturned);


            var path1 = DA.ParameterTargetPath(0);
            DataTree<Point3d> pointT = new DataTree<Point3d>();
            DataTree<Vector3d > normalT = new DataTree<Vector3d>();
            DataTree<Color> colorT = new DataTree<Color>();
            DataTree<double> distanceT = new DataTree<double>();
            GH_Structure<GH_Integer> idxT = new GH_Structure<GH_Integer>();

            for (int i = 0; i < pts.Count; i++)
            {
                List<int> ids = idc[i];
                for (int j = 0; j < ids.Count ; j++)
                {
                    pointT.Add(cloud[ids[j]].Location, path1.AppendElement(i));
                    if(cloud.ContainsNormals ) { normalT.Add(cloud[ids[j]].Normal, path1.AppendElement(i)); }
                    if (cloud.ContainsColors) { colorT.Add(cloud[ids[j]].Color, path1.AppendElement(i)); }
                    //Distance betwen Points
                    double D = pts[i].DistanceTo(cloud[ids[j]].Location);
                    distanceT.Add(D, path1.AppendElement(i));
                }
                idxT.AppendRange(ids.Select(x => new GH_Integer(x)), path1.AppendElement(i));
            }


            DA.SetDataTree(0, pointT);
            DA.SetDataTree(1, normalT);
            DA.SetDataTree(2, colorT);
            DA.SetDataTree(3, distanceT);
            DA.SetDataTree(4, idxT);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Volvox.Common.My.Resources.Resources.Icon_KDTree_NearestNeighbor;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{f50cda07-8253-4027-aacf-1a68c8d6b8cd}"); }
        }
    }
}