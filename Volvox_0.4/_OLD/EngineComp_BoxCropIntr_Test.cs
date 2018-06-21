using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using Grasshopper.Kernel;
using Volvox_Instr;
using Rhino.Geometry;

public class Eng_BoxCrop : GH_Component
{

    public Eng_BoxCrop() : base("MyProject1", "Nickname",
              "Description",
              "Category", "Subcategory")
    {
    }

    public override Guid ComponentGuid
    {
        get {  return new Guid("36b3c7c9-23ab-4d2f-ac88-35468e079fd5"); }
    }

    public override GH_Exposure Exposure
    {
        get { return GH_Exposure.tertiary; }
    }

    protected override Bitmap Icon
    {
        get { return null; }
    }

    protected override void RegisterInputParams(Grasshopper.Kernel.GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddBrepParameter("Box", "B", "Cropping box", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(Grasshopper.Kernel.GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new Param_Instr());
    }


    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<Brep> cboxlist = new List<Brep>();
        if (!DA.GetDataList(0, cboxlist))
            return;

        List<Box> outlist = new List<Box>();

        foreach (Brep cbox in cboxlist)
        {
            BrepFace bf = cbox.Faces[0];

            Plane thisplane = new Plane();
            if (!bf.TryGetPlane(out thisplane, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Are you sure it's a box ?");
                return;
            }

            if (cbox.DuplicateVertices().Length != 8)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Are you sure it's a box ?");
                return;
            }

            outlist.Add(new Box(thisplane, cbox));

        }

        DA.SetData(0, new Instr_BoxCrop2(outlist));

    }

}