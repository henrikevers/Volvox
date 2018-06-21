using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using KDTree.Core;
using KDTree.Common;
using Volvox_Cloud;
using Rhino.Geometry;
using Rhino;




namespace Volvox.GH.Params
{
    public class Type_KDTree : GH_GeometricGoo<Tuple<KDTree<int>, GH_Cloud>>, IGH_PreviewData

    {
        public Type_KDTree()
        {

        }

        public Type_KDTree(KDTree<int> tree, GH_Cloud cloud)
        {
            this.Value = new Tuple<KDTree<int>, GH_Cloud>(tree, cloud);
        }


        //Instance always available.
        public override bool IsValid
        {
            get
            {
                return true;
            }
        }

        // Return a string with the name of this Type.
        public override string TypeName
        {
            get
            {
                return "KDTreeCloud";
            }
        }

        // Return a string describing what this Type is about.
        public override string TypeDescription
        {
            get
            {
                return "KDTree Structure of PointCloud.";
            }
        }





        public override string ToString()
        {
            if (m_value != null)
                return ("KDTreeCloud with " + this.Value.Item2.Value.Count + " points");
            else
                return null;

        }



        public void DrawViewportWires(GH_PreviewWireArgs args)
        {

            args.Pipeline.DrawPointCloud(this.Value.Item2.Value, Settings_Global.DisplayRadius, args.Color);
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
        }

        //Duplication
        public Type_KDTree DuplicateKDTree()
        {
            GH_Cloud newCloud = (GH_Cloud)this.Value.Item2.Duplicate();
            KDTree<int> newTree = KDTreeLib.DubplicateTree(this.Value.Item1);

            return new Type_KDTree(newTree, newCloud);
        }

        public override IGH_Goo Duplicate()
        {
            return DuplicateKDTree();
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return DuplicateKDTree();
        }

        //BoundingBox
        public override BoundingBox GetBoundingBox(Transform xform)
        {
            return base.m_value.Item2.Boundingbox;
        }

        public override BoundingBox Boundingbox
        {
            get
            {
                return base.m_value.Item2.Boundingbox;
            }

        }

        public BoundingBox ClippingBox
        {
            get{return this.m_value.Item2.Boundingbox;  }
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            return base.m_value.Item2.Transform(xform);
            
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            return base.m_value.Item2.Morph(xmorph);
        }

       



    }

}
