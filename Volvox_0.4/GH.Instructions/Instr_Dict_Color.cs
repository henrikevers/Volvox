using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Volvox_Instr;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volvox.Common;
using System.Drawing;

namespace Volvox.GH.Instruction
{
    public class Instr_Dict_Color : Instr_BaseReporting
    {
        #region - Instruction Initialization
        /// <summary>
        /// Initialize Instruction Input Variables
        /// </summary>
        private string insV_Key { get; set; }
        private List<double> insV_Values { get; set; }
        private List<Color> insV_Colors { get; set; }
        private double insV_Step { get; set; }
        private double insV_ColPct { get; set; }
        private Color[] ColVal { get; set; }
        private Interval Itv { get; set; }

        /// <summary>
        /// Set Instruction Variables. 
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Values"></param>
        /// <param name="Colors"></param>
        /// <param name="Step"></param>
        /// <param name="ColPct">Percentage of color blend</param>
        public Instr_Dict_Color(string Key, List<double> Values, List<Color> Colors, double Step, double ColPct)
        {
            insV_Key = Key;
            insV_Values = Values;
            insV_Colors = Colors;
            insV_Step = Step;
            insV_ColPct = ColPct;


        }

        /// <summary>
        /// Instruction GUID.
        /// </summary>
        public override Guid InstructionGUID
        {
            get { return GUIDs.GuidsRelease4.Instruction.Instr_Dict_Color; }
        }

        /// <summary>
        /// Set InstructionType / String to Return. 
        /// </summary>
        public override string InstructionType
        {
            get { return "Color Cloud"; }
        }

        // Initialize Global Variables
        PointCloud GlobalCloud = null;
        PointCloud NewCloud = null;
        PointCloud[] CloudPieces = null;
        int ProcCount = Environment.ProcessorCount;
        int PointCounter = 0;
        int LastPercentReported = 0;

        


        /// <summary>
        /// SetUp Duplication procedure. 
        /// </summary>
        /// <returns></returns>
        public override IGH_Goo Duplicate()
        {
            string n_Key = insV_Key;
            List<double> n_Values = new List<double>(insV_Values);
            List<Color> n_Colors = new List<Color>(insV_Colors);
            double n_Step = insV_Step;
            double n_ColPct = insV_ColPct;


            Instr_Dict_Color ni = new Instr_Dict_Color(n_Key, n_Values, n_Colors, n_Step, n_ColPct);
            ni.GlobalCloud = null;
            ni.NewCloud = null;
            ni.CloudPieces = null;
            ni.PointCounter = 0;
            ni.LastPercentReported = 0;
            return ni;
        }
        #endregion - Instruction Initialization

        #region Cancellation Ini.
        // Setup the cancellation mechanism.
        CancellationTokenSource cts = new CancellationTokenSource();
        ParallelOptions po = new ParallelOptions();
        #endregion Cencellation Ini.

        /// <summary>
        /// 
        /// </summary>
        /// <param name="PointCloud"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        public override bool Execute(ref PointCloud pointCloud)
        {

            #region - Initialize Execution
            //Set Global Variables
            LastPercentReported = 0;
            PointCounter = 0;
            NewCloud = new PointCloud();
            CloudPieces = null;
            CloudPieces = new PointCloud[ProcCount];
            GlobalCloud = pointCloud;
           

            // Setup the cancellation mechanism.
            po.CancellationToken = cts.Token;

            //Create Partitions for multithreading.
            var rangePartitioner = System.Collections.Concurrent.Partitioner.Create(0, GlobalCloud.Count, (int)Math.Ceiling((double)GlobalCloud.Count / ProcCount));

            Dict_Utils.CastDictionary_ToArrayDouble(ref GlobalCloud);


            ////
            //Initialize ColorGradient
            Interval Itv = new Interval();
            Bitmap bmp = new Bitmap(1000, 1);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            System.Drawing.Drawing2D.ColorBlend cb = new System.Drawing.Drawing2D.ColorBlend();
            float[] pos = new float[insV_Colors.Count];

            for (int i = 0; i <= pos.Length - 1; i += 1)
            {
                pos[i] = (float)insV_Values[i];

            }

            Color[] colors = insV_Colors.ToArray();
            Array.Sort(pos, colors);
            Array.Sort(pos, pos);
            float[] normpos = new float[insV_Colors.Count];
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

            ////

            //Get Dictionary Values from PointCloud
            List<double> DictVal = new List<double>();
            DictVal.AddRange((double[])GlobalCloud.UserDictionary[insV_Key]);
            //Get idx to multiply with for normalized values. 
            int idx = ColVal.Length - 1;

            //Run MultiThreaded Loop.
            Parallel.ForEach(rangePartitioner, po, (rng, loopState) =>
            {
                //Initialize Local Variables.
                /// Get Index for Processor to be able to merge clouds in ProcesserIndex order in the end. 
                int MyIndex = (int)(rng.Item1 / Math.Ceiling(((double)GlobalCloud.Count / ProcCount)));
                /// Initialize Partial PointCloud
                PointCloud MyCloud = new PointCloud();
                
                /// Get Total Count Fraction to calculate Operation Percentage. 
                double totc = (double)1 / GlobalCloud.Count;
                
                //Loop over individual RangePartitions per processor. 
                for (int i = rng.Item1; i < rng.Item2; i++)
                {
                    //Operation Percentage Report
                    ///Safe Counter Increment.
                    Interlocked.Increment(ref PointCounter);
                    ///Calculate and Report Percentage. 
                    if (LastPercentReported < ((PointCounter * totc) * 100))
                    {
                        LastPercentReported = (int)(5 * Math.Ceiling((double)(PointCounter * totc) * 20));
                        this.ReportPercent = LastPercentReported;
                    }
                    #endregion - End of Initialize Execution

                    #region - Code to Process on PointCloud.


                    double thisnorm = new double();
                if (insV_Step <= 0.0)
                {
                    thisnorm = Itv.NormalizedParameterAt(DictVal[i]);
                }
                else
                {
                    thisnorm = Itv.NormalizedParameterAt((Math.Floor((DictVal[i] - Itv[0]) / insV_Step) * insV_Step) + Itv[0]);
                    }

                    Color col = new Color();
                    if (thisnorm < (0.0000000))
                    {
                    col = ColVal.First();
                    }
                    else if (thisnorm > (1.0000000) )//+ Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    {
                    col = ColVal.Last();
                    }
                    else
                    {
                    col = ColVal[(int)(thisnorm * idx)];
                    }

                    if( insV_ColPct >= 0.0)
                    {
                        col = Color_Utils.Blend(GlobalCloud[i].Color, col, insV_ColPct);
                    }

                    MyCloud.Add(GlobalCloud[i].Location, col);


                    #endregion - End of Code to Process on PointCloud.
                    #region - Finalize Execution
                }

                

                //Add MyCloud to CloudPieces at ProcesserIndex. 
                CloudPieces[(int)MyIndex] = MyCloud;

                //Enable Parrallel Computing Cancellation
                po.CancellationToken.ThrowIfCancellationRequested();
            }
            );


            //Merge PointCloud Pieces into One PointCloud. 
            Cloud_Utils.MergeClouds(ref NewCloud, CloudPieces);

            //Set UserDictionary from DictionaryLists.
            Dict_Utils.SetUserDict_FromOtherCloud(ref NewCloud, GlobalCloud);
            //Set ColorGradient UserData
            List<double> gradVals = new List<double>();
            List<Color> gradCols = new List<Color>();
            if ( insV_Step > 0.00)
            {
                for (int i = 0; i < pos.Length; i++)
                {
                    double gradVal = (Math.Floor((pos[i] - Itv[0]) / insV_Step) * insV_Step) + Itv[0];
                    if (!gradVals.Contains(gradVal))
                    {
                        Color gradCol = ColVal[(int)(Itv.NormalizedParameterAt(gradVal) * idx)];
                        gradVals.Add(gradVal);
                        gradCols.Add(gradCol);
                    }

                }
                double lastVal = (Math.Ceiling((pos[pos.Length-1] - Itv[0]) / insV_Step) * insV_Step) + Itv[0];
                if (!gradVals.Contains(lastVal))
                {
                    Color gradCol = ColVal[(int)(Itv.NormalizedParameterAt(pos[pos.Length - 1]) * idx)];
                    gradVals.Add(lastVal);
                    gradCols.Add(gradCol);
                }
            }
            else
            {
                gradVals = insV_Values;
                gradCols = insV_Colors;
            }
           
            Color_Utils.Set_ColorGradient_Dict(ref NewCloud, gradCols, gradVals);

            //Dispose of Global Clouds.
            GlobalCloud.Dispose();
            pointCloud.Dispose();

            //Set OutputCloud
            pointCloud = (PointCloud)NewCloud.Duplicate();


            //Dispose of PointCloud Pieces and NewCloud. 
            CloudPieces = null;
            NewCloud.Dispose();
            #endregion - End of Finalize Execution

            //Return True on Finish
            return true;
        }

        private void Instr_Dict_Color_MessageEvt(Instr_Dict_Color sender, MessageEventArgs e)
        {
            throw new NotImplementedException();
        }

        #region Abort Execution
        /// <summary>
        /// Abort Function to Facilitate Cancellation of Parrallel Computing. 
        /// </summary>
        public override void Abort()
        {
            // Call Cancel to make Parallel.ForEach throw.
            // Obviously this must done from another thread.
            cts.Cancel();

            CloudPieces = null;
            GlobalCloud = null;
            if (NewCloud != null)
                NewCloud.Dispose();
        }
        #endregion - End of Abort Execution
    }
}