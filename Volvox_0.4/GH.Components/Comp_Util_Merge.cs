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
    public class Comp_Util_Merge : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Comp_Util_Merge()
          : base("Merge Clouds 0.4", "MCloud 0.4", "Merge multiple clouds into one. (version 0.4)", "Volvox", "Cloud")
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
                return Volvox.Common.My.Resources.Resources.Icon_Merge;
                
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return GUIDs.GuidsRelease4.Component.Util_Merge; }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Clouds to merge", Optional = false, Access = GH_ParamAccess.list });
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Cloud", Access = GH_ParamAccess.item });
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<PointCloud> cl = new List<PointCloud>();
            if(!DA.GetDataList(0, cl)) return;

            
            //Set Warnings
            Boolean sameKeys = true;
            for (int i = 0; i < cl.Count; i++)
            {
                foreach (string key in cl[i].UserDictionary.Keys)
                {
                    for (int j = 0; j < cl.Count; j++)
                    {
                        if(! cl[j].UserDictionary.ContainsKey(key))
                        {
                            sameKeys = false;
                            break;
                        }
                    }
                }

            }
            if(!sameKeys)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Clouds Doesn't contain the same UserData.");
                return;
            }

            // Execute
            PointCloud NewCloud = new PointCloud();
            object[] DictPieces = new object[cl.Count];
            for (int i = 0; i < cl.Count; i++)
            {
                
                NewCloud.Merge(cl[i]);

                if(cl[0].UserDictionary.Count > 0)
                {
                    List<double>[] MyDict = new List<double>[cl[0].UserDictionary.Count];
                    for (int k = 0; k < cl[i].UserDictionary.Keys.Length; k++)
                    {
                        string key = cl[i].UserDictionary.Keys[k];
                        double[] vals = (double[])cl[i].UserDictionary[key];
                        MyDict[k] = vals.ToList();
                    }

                    DictPieces[i] = MyDict;
                }
                
            }

            List<double>[] NewDict = new List<double>[cl[0].UserDictionary.Count];
            Dict_Utils.Merge_DictPieces(ref NewDict, DictPieces);
            Dict_Utils.SetUserDict_FromDictLists(ref NewCloud, NewDict, cl[0].UserDictionary.Keys);



            DA.SetData(0, NewCloud);



        }

       
    }
}
