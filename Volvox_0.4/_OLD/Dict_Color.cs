
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Volvox_Cloud;
using System.Linq;

namespace Volvox.GH.Component
{

    public class Dict_ColorDictionary : GH_Component
    {

        public Dict_ColorDictionary() : base("Preview Data", "PrevData", "Assign colors to cloud according to user data.", "Volvox", "UserData")
        {
        }

        public override Guid ComponentGuid
        {
            get { return GUIDs.GuidsRelease4.Component.Comp_DictColorDictionary; }
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary; }
        }

        protected override Bitmap Icon
        {
            get {
                return null;
                //return My.Resources.Icon_PreviewData;
            }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Point Cloud", Optional = false });
            pManager.AddTextParameter("Key", "K", "Key", GH_ParamAccess.item);
            pManager.AddNumberParameter("Values", "V", "Values", GH_ParamAccess.list);
            pManager.AddColourParameter("Colors", "C", "Colors", GH_ParamAccess.list);
            pManager.AddNumberParameter("StepSize", "S", "Step Size of Gradient", GH_ParamAccess.item);
            pManager[pManager.ParamCount - 1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_Cloud { Name = "Cloud", NickName = "C", Description = "Point Cloud" });
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int ProcCount = Environment.ProcessorCount;

            Color[] ColVal = null;
            Interval Itv = new Interval();

            List<double> pars = new List<double>();
            List<Color> cols = new List<Color>();

            string strdc = null;
            PointCloud GlobalCloud = null;

            double step = 0.0;

            if (!DA.GetData("Cloud", ref GlobalCloud))
                return;
            if (!DA.GetData(1, ref strdc))
                return;
            if (!DA.GetDataList(2, pars))
                return;
            if (!DA.GetDataList(3, cols))
                return;
            DA.GetData(4, ref step);
            ///Duplicate Point Cloud.
            PointCloud newCloud = (PointCloud)GlobalCloud.Duplicate();
            

            Bitmap bmp = new Bitmap(1000, 1);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            System.Drawing.Drawing2D.ColorBlend cb = new System.Drawing.Drawing2D.ColorBlend();
            float[] pos = new float[cols.Count];

            for (int i = 0; i <= pos.Length - 1; i += 1)
            {
                pos[i] = (float)pars[i];

            }

            Color[] colors = cols.ToArray();
            Array.Sort(pos, colors);
            Array.Sort(pos, pos);
            float[] normpos = new float[cols.Count];
            Itv = new Interval(pos[0], pos[pos.Length - 1]);

            for (int i = 0; i <= normpos.Length - 1; i += 1)
            {
                normpos[i] = (float)Itv.NormalizedParameterAt(pos[i]);
            }

            cb.Positions = normpos;
            cb.Colors = colors;
            

            System.Drawing.Drawing2D.LinearGradientBrush lin = new System.Drawing.Drawing2D.LinearGradientBrush(rect, Color.Black, Color.Black, 0, false);
            lin.LinearColors = colors;
            lin.InterpolationColors = cb;

 
            ColVal = null;
            ColVal = new Color[bmp.Width];

            int counter = 0;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(lin, rect);
                for (int i = 0; i <= bmp.Width - 1; i += 1)
                {
                    Color col = bmp.GetPixel(i, 0);
                    ColVal[counter] = col;
                    counter += 1;
                }
            }



            List<double> DictVal = new List<double>();
            DictVal.AddRange((double[])newCloud.UserDictionary[strdc]);
            int idx = ColVal.Length - 1;

            for (int i = 0; i <= newCloud.Count - 1; i += 1)
            {
                double thisnorm = new double();
                if (step <= 0.0)
                {
                    thisnorm = Itv.NormalizedParameterAt(DictVal[i]);
                }
                else
                {
                    thisnorm = Itv.NormalizedParameterAt(Math.Floor(DictVal[i] / step) * step);
                }
                

                if (thisnorm < (0.0 ))//- Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
                {
                    newCloud[i].Color = ColVal.First();//Color.Black;
                }
                else if (thisnorm > (1.0 ))//+ Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
                {
                    newCloud[i].Color = ColVal.Last();//Color.White;
                }
                else
                {
                    newCloud[i].Color = ColVal[(int)(thisnorm * idx)];
                }
                



            }




            DA.SetData(0, newCloud);
            GlobalCloud.Dispose();
            //newCloud.Dispose();
            newCloud = null;
            GlobalCloud = null;
            ColVal = null;
            bmp.Dispose();
            Itv = new Interval();
            

        }


    }
}


