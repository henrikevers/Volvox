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
    public class KDTree :  GH_Goo<Tuple<KDTree<int>, PointCloud >>, IGH_PreviewData

    {
        public KDTree()
        {
          
        }

        public KDTree(KDTree<int> tree, PointCloud  cloud)
        {
            this.Value = new Tuple<KDTree<int>, PointCloud>(tree, cloud);
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



        public override IGH_Goo Duplicate()
        {
            PointCloud  newCloud = (PointCloud) this.Value.Item2.Duplicate() ;
            KDTree < int > newTree = KDTreeLib.DubplicateTree(this.Value.Item1);

            return new KDTree(newTree, newCloud);
            
        }

        public override string ToString()
        {
            if (m_value != null)
                return ("KDTreeCloud with " + this.Value.Item2.Count + " points");
            else
                return null;
            
        }



        public void DrawViewportWires(GH_PreviewWireArgs args) 
            {

            args.Pipeline.DrawPointCloud(this.Value.Item2, Settings_Global.DisplayRadius , args.Color);
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
        }

        public BoundingBox ClippingBox
        {
            get
            {
                return this.Value.Item2.GetBoundingBox(true);
            }
        }
    }



}
