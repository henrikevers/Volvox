using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Rhino.Geometry;


namespace Volvox.GH.Params
{
    public class Param_KDTree : GH_Param<Type_KDTree>, IGH_PreviewObject
    {
        // We need to supply a constructor without arguments that calls the base class constructor.
        public Param_KDTree():
                        base("KDTree Cloud", "KDTree", "A KDTree PointCloud Structure.", "Params", "Geometry",GH_ParamAccess.tree) { }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary ; }
        }

        public BoundingBox ClippingBox
        {
            get
            {
                return this.Preview_ComputeClippingBox();
            }
        }

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("6cd69326-90d5-4b21-af50-b5ae234c912c");
            }
        }

        public bool Hidden
        {
            get; set;
        }
       

        public bool IsPreviewCapable
        {
            get
            {
                return true;
            }
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
        }

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            this.Preview_DrawWires(args);
        }
        
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Volvox.Common.My.Resources.Resources.Icon_CloudParam_KDTree;
            }
        }

    }


}
