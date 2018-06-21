using System;
using System.Collections.Generic;

using System.Linq;

using Grasshopper.Kernel;
using GH_IO.Serialization;
using Rhino.Geometry;

using Volvox_Cloud;
using Volvox_Instr;
using Volvox.Common;

namespace Volvox.GH.Component
{
    public class Dict_GetDictionary : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Dict_GetDictionary()
          : base("Get Data", "GetData", "Get data set stored in a cloud.", "Volvox", "UserData")
        {
          
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
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
                return Volvox.Common.My.Resources.Resources.Icon_GetData;
                
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return GUIDs.GuidsRelease4.Component.Dict_GetDictionary; }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Point Cloud to get UserData from.", Optional = false });
            pManager.AddTextParameter("Key", "K", "Key", GH_ParamAccess.item);



        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Values", "V", "User data values", GH_ParamAccess.list);


        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string strdc = null;
            PointCloud pc = null;

      

            if (!DA.GetData(0, ref pc)) return;
            if (!DA.GetData(1, ref strdc)) return;


            List<double> nl = new List<double>();
            Dict_Utils.CastDictionary_ToArrayDouble(ref pc);
            nl.AddRange((double[])pc.UserDictionary[strdc]);

            DA.SetDataList(0, nl);



        }

       
    }
}
