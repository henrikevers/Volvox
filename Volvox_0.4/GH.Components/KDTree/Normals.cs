using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;


using Grasshopper.Kernel;
using Rhino.Geometry;
using KDTree.GH.Volvox.Params;
using KDTree.Common;
using KDTree.Core;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;


namespace KDTree.GH.Volvox.Components
{
    public class Normals : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NearestNeighbor class.
        /// </summary>
        public Normals()
          : base("Calculate Normals", "celcNormal",
              "Calculate Normals for KDTreeCloud",
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
            pManager.AddParameter(new KDTreeParam(), "KDTreeCloud", "K", "KDTreeCloud to calculate normals on.", GH_ParamAccess.item);
            pManager.AddNumberParameter("NeighborRange", "R", "Distance Range for Nearest Neighbors to each point in Cloud for the calculation of normal.", GH_ParamAccess.item, double.MaxValue);
            pManager.AddIntegerParameter("Number of Neighbors", "N", "Ámount of Nearest Neighbors to use for calculation of normal. Minimum 3.", GH_ParamAccess.item, 5);
            pManager.AddVectorParameter("Guide Vector", "G", "Guiding Vector or Centering Point, Scan Position to orient Normal. Supply a vector3d and if centering point is chosen for GuideStyle, then the Vector3d will be directly tranlated to a point.", GH_ParamAccess.item, Vector3d.ZAxis);
            pManager.AddIntegerParameter ("GuideStyle", "S", "Choose between Guiding Vector3d or Guiding Center Point. \n Vector3d = 0, CenterPoint = 1, Previous Calculated Nomal = 2.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter ("Color", "C", "Integer defining color style.0 = Original Colors, 1 = Color Normals, 2 = GrayScale Normals, 3 = Original + GrayScale Normals", GH_ParamAccess.item, 1);
         
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }


        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new KDTreeParam(), "KDTreeCloud", "K", "KDTreeCloud with Normals.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            KDTreeType treeT = null;
            double NeighborRange = new double();
            int NeighborAmount = 5;
            Vector3d GuideVector = Vector3d.ZAxis;
            int GuideStyle = 0;
            int Color = 1;
            bool Unify = false;


            if (!DA.GetData(0, ref treeT)) return;
            if (!DA.GetData(1, ref NeighborRange)) return;
            if (!DA.GetData(2, ref NeighborAmount)) return;
            if (!DA.GetData(3, ref GuideVector)) return;
            if (!DA.GetData(4, ref GuideStyle)) return;
            if (!DA.GetData(5, ref Color)) return;
         
            Tuple<KDTree<int>, PointCloud> KDTreeCloud = treeT.Value;


            if (NeighborAmount < 3)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Minimum Amount of Neighboring Points to calculate Normal is 3.");
                return;
            }

            Tuple<KDTree<int>, PointCloud> newKDTreeCloud = KDTreeLib.CalcNormals(KDTreeCloud, NeighborRange, NeighborAmount, GuideVector, GuideStyle, Color);


            DA.SetData(0, new KDTreeType(newKDTreeCloud.Item1 , newKDTreeCloud.Item2 ));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{c5dac753-75d6-4614-b929-abdb050a03c8}"); }
        }
    }
}