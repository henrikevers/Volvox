using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Volvox_Cloud;
using KDTree.Common;
using KDTree.Core;
using Volvox.GH.Params;

namespace KDTree.GH.Volvox.Components
{
    public class Cloud2CloudDist : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Cloud2CloudDist()
          : base("Cloud Cloud Distance", "C2C",
                "Calculates Nearest Neighbor Distance between Two Point Clouds. Outputs Cloud Meassured From with UserData.",
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
            pManager.AddParameter(new Param_Cloud(), "Cloud From", "C", "Cloud to measure Distance From. Resulting Cloud with distances as UserData.", GH_ParamAccess.item);
            pManager.AddParameter(new Param_KDTree(), "KDTreeCloud To", "K", "KDTreeCloud to measure Distance To.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Amount", "A", "Amount of nearest neighbors to average out distances.", GH_ParamAccess.item, 1);
            pManager.AddTextParameter("Key", "K", "Key", GH_ParamAccess.item, "CloCloD");


            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud(), "Cloud", "C", "Cloud with UserData", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {


            // PointCloud to meassure Distance From.
            PointCloud cloudFrom = null;
            // PointCloud/KDTree to meassure Distance From.
            Type_KDTree treeT = null;
            // Amount
            int amount = 1;
            //UserData Key String.
            string key = "CloCloD";

                if (!DA.GetData(0, ref cloudFrom))
                    return;
                if (!DA.GetData(1, ref treeT))
                    return;
                if (!DA.GetData(2, ref amount))
                    return;
                if (!DA.GetData(3, ref key))
                     return;

            Tuple<KDTree<int>, GH_Cloud> KDTreeCloud = treeT.Value;

            //Make Duplicate to make sure I dont change UserData in input Cloud. 
            cloudFrom = (PointCloud) cloudFrom.Duplicate();

            //Calculate closestPoint distances between the two Clouds.
            double[] dist = KDTreeLib.CloudCloudDist(cloudFrom, KDTreeCloud, amount);
               
                //Add Distances to UserDictionary
                cloudFrom.UserDictionary.Set(key, dist);

                //Output
                DA.SetData(0, cloudFrom);
           
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{351a87bb-7023-4978-950f-69472e82747d}"); }
        }
    }
}
