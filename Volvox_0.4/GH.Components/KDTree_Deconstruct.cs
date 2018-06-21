using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;


using Grasshopper.Kernel;
using Rhino.Geometry;
using Volvox.GH.Params;
using KDTree.Core;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Volvox_Cloud;


namespace Volvox.GH.Component
{
    public class DeconstructKDTree : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NearestNeighbor class.
        /// </summary>
        public DeconstructKDTree()
          : base("Deconstruct KDTreeCloud", "deKDTree",
              "Deconstruct the KDTreeCloud and get the Cloud.",
              "Volvox", "KDTree")
        {
        
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_KDTree(), "KDTree", "K", "KDTree PointCloud Structure", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud(), "Cloud", "C", "Cloud", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Type_KDTree treeT = null;

            if (!DA.GetData("KDTree", ref treeT)) return;

            Tuple<KDTree<int>, GH_Cloud> KDTreeCloud = treeT.Value;
            GH_Cloud cloud = KDTreeCloud.Item2;
           

            DA.SetData(0, cloud);

        }



        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Volvox.Common.My.Resources.Resources.Icon_KDTree_Deconstruct;

            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("811f4795-ebe8-43d4-a7b3-2ec07a615f14"); }
        }
    }
}