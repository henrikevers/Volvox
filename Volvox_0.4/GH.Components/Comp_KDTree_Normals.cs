using System;
using System.Collections.Generic;

using System.Linq;

using Grasshopper.Kernel;
using GH_IO.Serialization;
using Rhino.Geometry;

using Volvox_Cloud;
using Volvox_Instr;
using System.Drawing;
using Volvox.GH.Params;
using KDTree.Core;
using KDTree.Common;

namespace Volvox.GH.Component
{
    public class Comp_KDTree_Normals : GH_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Comp_KDTree_Normals()
          : base("Cloud Normals", "Normals", "Compute Normals in a PointCloud.", "Volvox", "KDTree")
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
                return Volvox.Common.My.Resources.Resources.Icon_CloudNormals;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return GUIDs.GuidsRelease4.Component.Comp_KDTree_Normals; }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_KDTree(), "KDTree", "K", "KDTreeCloud to calculate normals on.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Number of Neighbors", "N", "Ámount of Nearest Neighbors to use for calculation of normal. Minimum 3.", GH_ParamAccess.item, 5);
            pManager.AddVectorParameter("Guide Vector", "G", "Guiding Vector or Centering Point, Scan Position to orient Normal. Supply a vector3d and if centering point is chosen for GuideStyle, then the Vector3d will be directly tranlated to a point.", GH_ParamAccess.item, Vector3d.ZAxis);
            pManager.AddBooleanParameter("GuideStyle", "S", "Choose between Guiding Vector3d or Guiding Center Point. \n Vector3d = 0, CenterPoint = 1, Previous Calculated Nomal = 2.", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            
            pManager.AddParameter(new Param_KDTree { Name = "KDTree", NickName = "K", Description = "KDTreeCloud with normals."});
           
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {


            //Initialize Input Variables, Persistent in ComponentChange. 
            Type_KDTree KDTree = null;
            int Amount = 1;
            Vector3d GV = Vector3d.ZAxis;
            Boolean GS = true;
           
            if (!DA.GetData("Number of Neighbors", ref Amount)) return;
            if (!DA.GetData("Guide Vector", ref GV)) return;
            if (!DA.GetData("GuideStyle", ref GS)) return;

            //Excute Instruction
            ///If Component is set as Instruction.
            if (isInstruction)
            {
                     //this.Message = "";
                DA.SetData("Instr", new Instruction.Instr_KDTree_Normals(null, GV, GS, Amount, colorize));
            }
            else
            ///If Component is set to StandAlone. 
            {
                
                if (!DA.GetData("KDTree", ref KDTree)) return;
                ///Initialize PointCloud Input Variable. 
                GH_Cloud pointCloud = KDTree.Value.Item2;
                //if (!DA.GetData("Cloud", ref pointCloud)) return;
                ///Duplicate GH Cloud.
                GH_Cloud newGHCloud = pointCloud.DuplicateCloud();
                PointCloud newCloud = newGHCloud.Value;
                ///Execute Instruction.
                Instruction.Instr_KDTree_Normals inst = new Instruction.Instr_KDTree_Normals(KDTree.Value.Item1, GV, GS, Amount, colorize);
                Boolean Result = inst.Execute(ref newCloud);
                
                ///Set New Output Cloud
                newGHCloud.Value = newCloud;
                Type_KDTree newKDTree = new Type_KDTree(KDTree.Value.Item1, newGHCloud);
                DA.SetData("KDTree", newKDTree);
                
                //Add RuntimeMessage: ComponentChange.
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "'Right Click' to Switch between StandAlone and Instruction Component.");
            }
            
        }

        #region - Component / Method Change
        // Variable methods
        bool isInstruction = false;
        bool colorize = false;
        //bool angleApproach = false;

        //      Serialization
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("isInstruction", isInstruction);
            writer.SetBoolean("colorize", colorize);
           //writer.SetBoolean("angleApproach", angleApproach);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            isInstruction = reader.GetBoolean("isInstruction");
            colorize = reader.GetBoolean("colorize");
            //angleApproach = reader.GetBoolean("angleApproach");
            return base.Read(reader);
        }

        //      Menu items
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            //Menu_AppendItem(menu, "Angle Of Approach", SwitchColorizeEvent, true, angleApproach);
            //Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Colorize", SwitchColorizeEvent, true, colorize);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Instruction", SwitchInstructionEvent, true, isInstruction);
         }

        //      Change Component
        private void SwitchInstructionEvent(object sender, EventArgs e) => SwitchInstruction();
        private void SwitchInstruction()
        {
            if (isInstruction)
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Instr"), true);
                
                Params.RegisterInputParam(new Param_KDTree { Name = "KDTree", NickName = "K", Description = "KDTreeCloud with normals.", Access = GH_ParamAccess.item}, 0);
                Params.RegisterOutputParam(new Param_KDTree { Name = "KDTree", NickName = "K", Description = "KDTreeCloud with normals." }, 0);

                isInstruction = false;
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "KDTree"), true);
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "KDTree"), true);
                Params.RegisterOutputParam(new Param_Instr(), 0);

                isInstruction = true;
            }

            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        private void SwitchColorizeEvent(object sender, EventArgs e) => SwitchColorize();
        private void SwitchColorize()
        {
            if (colorize)
            {
                colorize = false;
            }
            else
            {
                colorize = true;
            }
            //Params.OnParametersChanged();
            ExpireSolution(true);
        }


        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        #endregion
    }
}
