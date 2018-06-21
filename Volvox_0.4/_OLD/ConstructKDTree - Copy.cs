﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Volvox_Cloud;
using KDTree.Core;
using KDTree.GH.Volvox.Params;
using KDTree.GH.Common;

namespace KDTree.GH.Volvox.Components
{
    public class ConstructKDTree : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ConstructKDTree class.
        /// </summary>
        public ConstructKDTree()
          : base("Construct KDTree", "KDTree",
              "Constructs a KDTreeCloud Structure from a Cloud",
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
            pManager.AddParameter(new Param_Cloud(), "Cloud", "C", "Cloud to create KDTree structure from.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new KDTreeParam(), "KDTreeCloud", "K", "KDTree PointCloud Structure", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            PointCloud cloud = null;
           

            if (!DA.GetData(0, ref cloud))
                return;

            KDTree<int> tree = KDTreeLib.ConstructKDTree(cloud);
            

            DA.SetData(0, new KDTreeType(tree, cloud));
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
            get { return new Guid("{843be322-f81d-4e43-8db7-ae64ff4b0378}"); }
        }
    }
}