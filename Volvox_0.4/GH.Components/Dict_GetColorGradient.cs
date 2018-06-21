using System;
using System.Collections.Generic;

using System.Linq;

using Grasshopper.Kernel;
using GH_IO.Serialization;
using Rhino.Geometry;

using Volvox_Cloud;
using Volvox_Instr;
using Volvox.Common;
using System.Drawing;

namespace Volvox.GH.Component
{
    public class Dict_GetColorGradient : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Dict_GetColorGradient()
          : base("Get ColorGradient", "GetCol", "Get the ColorGradient, which the Cloud is displaying.", "Volvox", "UserData")
        {
          
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.quarternary; }
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
                return Volvox.Common.My.Resources.Resources.Icon_ColorGradient;
                
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return GUIDs.GuidsRelease4.Component.Dict_GetColorGradient; }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Point Cloud to get UserData from.", Optional = false });
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddColourParameter("Colors", "C", "Gradient Colors", GH_ParamAccess.list);
            pManager.AddNumberParameter("Values", "V", "Gradient Values", GH_ParamAccess.list);
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            PointCloud pc = null;

            if (!DA.GetData(0, ref pc)) return;

            ///Write ErrorMessage eles add box to Box List. 
            if (!pc.UserDictionary.ContainsKey("ColorGradient"))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Cloud doesn't contain ColorGradient Information.");
                return;
            }


            List<Color> Colors = Color_Utils.Get_Colors(pc);
            List<double> Values = Color_Utils.Get_ColorValues(pc);

            
            DA.SetDataList(0, Colors);
            DA.SetDataList(1, Values);


        }

       
    }
}
