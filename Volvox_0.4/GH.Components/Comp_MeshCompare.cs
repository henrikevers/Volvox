using System;
using System.Collections.Generic;

using System.Linq;

using Grasshopper.Kernel;
using GH_IO.Serialization;
using Rhino.Geometry;

using Volvox_Cloud;
using Volvox_Instr;
using System.Drawing;

namespace Volvox.GH.Component
{
    public class Comp_MeshCompare : GH_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Comp_MeshCompare()
          : base("Mesh Compare", "MCompare", "Compute distance to a mesh.", "Volvox", "UserData")
        {
          
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary; }
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
                return Volvox.Common.My.Resources.Resources.Icon_Compare;
                
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return GUIDs.GuidsRelease4.Component.Comp_MeshCompare; }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Point Cloud", Optional = false });
            pManager.AddMeshParameter("Mesh", "M", "Mesh to compare with", GH_ParamAccess.item);
            pManager.AddTextParameter("Key", "K", "UserData Key", GH_ParamAccess.item);



        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Point Cloud" });
           
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {


            //Initialize Input Variables, Persistent in ComponentChange. 
            Mesh Mesh = null;
            string Key = string.Empty;
            if (!DA.GetData("Mesh", ref Mesh)) return;
            if (!DA.GetData("Key", ref Key)) return;

            //Excute Instruction
            ///If Component is set as Instruction.
            if (isInstruction)
            {
                //this.Message = "";
                DA.SetData("Instr", new Instruction.Instr_MeshCompare(Mesh, Key, colorize, false, null, ""));
            }
            else
            ///If Component is set to StandAlone. 
            {
                ///Initialize PointCloud Input Variable. 
                GH_Cloud pointCloud = null;
                if (!DA.GetData("Cloud", ref pointCloud)) return;
                ///Duplicate GH Cloud.
                GH_Cloud newGHCloud = pointCloud.DuplicateCloud();
                PointCloud newCloud = newGHCloud.Value;
                ///Execute Instruction.
                Instruction.Instr_MeshCompare inst = new Instruction.Instr_MeshCompare(Mesh, Key, colorize, angleApproach, pointCloud.ScannerPosition.Origin, "ApproachAngle");
                Boolean Result = inst.Execute(ref newCloud);
                
                ///Set New Output Cloud
                newGHCloud.Value = newCloud;
                DA.SetData("Cloud", newGHCloud);
                
                //Add RuntimeMessage: ComponentChange.
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "'Right Click' to Switch between StandAlone and Instruction Component.");
            }
            
        }

        #region - Component / Method Change
        // Variable methods
        bool isInstruction = false;
        bool colorize = false;
        bool angleApproach = false;

        //      Serialization
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("isInstruction", isInstruction);
            writer.SetBoolean("colorize", colorize);
           writer.SetBoolean("angleApproach", angleApproach);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            isInstruction = reader.GetBoolean("isInstruction");
            colorize = reader.GetBoolean("colorize");
            angleApproach = reader.GetBoolean("angleApproach");
            return base.Read(reader);
        }

        //      Menu items
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Angle Of Approach", SwitchAngleEvent, true, angleApproach);
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
                Params.RegisterInputParam(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Point Cloud", Optional = false }, 0);
                Params.RegisterOutputParam(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Point Cloud", Optional = false }, 0);
             

                isInstruction = false;
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Cloud"), true);
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Cloud"), true);
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

        private void SwitchAngleEvent(object sender, EventArgs e) => SwitchAngle();
        private void SwitchAngle()
        {
            if (angleApproach)
            {
                angleApproach = false;
            }
            else
            {
                angleApproach = true;
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
