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
using KDTree.Core;

namespace Volvox.GH.Instruction
{
    public class Instr_KDTree_Construct : Instr_BaseReporting
    {
        #region - Instruction Initialization
        /// <summary>
        /// Initialize Instruction Input Variables
        /// </summary>
        private KDTree<int> insV_KDTree { get; set; }

        /// <summary>
        /// Set Instruction Input Variables
        /// </summary>
        /// <param name="M"></param>
        /// <param name="K"></param>
        /// <param name="C_bool"></param>
        public Instr_KDTree_Construct(ref KDTree<int> KDTree)
        {
            insV_KDTree = KDTree;
        }

        /// <summary>
        /// Instruction GUID.
        /// </summary>
        public override Guid InstructionGUID
        {
            get { return GUIDs.GuidsRelease4.Instruction.Instr_ColorBlend; }
        }

        /// <summary>
        /// Set InstructionType / String to Return. 
        /// </summary>
        public override string InstructionType
        {
            get { return "Construct KDTree"; }
        }

        // Initialize Global Variables
        PointCloud GlobalCloud = null;

        int ProcCount = Environment.ProcessorCount;
        int PointCounter = 0;
        int LastPercentReported = 0;


        /// <summary>
        /// SetUp Duplication procedure. 
        /// </summary>
        /// <returns></returns>
        public override IGH_Goo Duplicate()
        {
            KDTree<int> nKDTree = insV_KDTree;
            Instr_KDTree_Construct ni = new Instr_KDTree_Construct(ref nKDTree);
            ni.GlobalCloud = null;
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
        /// Parallel Computing Setup for Code to Execute. 
        /// </summary>
        /// <param name="pointCloud">ByRef PointCloud to Execute Code on.</param>
        /// <returns></returns>
        public override bool Execute(ref PointCloud pointCloud)
        {
            //Set Global Variables
            LastPercentReported = 0;
            PointCounter = 0;

            GlobalCloud = pointCloud;


            //Initialize KDTree with three Dimentions.
            //KDTree = new KDTree<int>(3);
            /// Get Total Count Fraction to calculate Operation Percentage. 
            double totc = (double)1 / GlobalCloud.Count;


                //Loop over individual RangePartitions per processor. 
                for (int i = 0; i < GlobalCloud.Count; i++)
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
                //Add Points to KDTree.
                insV_KDTree.AddPoint(new double[] { GlobalCloud[i].Location.X, GlobalCloud[i].Location.Y, GlobalCloud[i].Location.Z }, i);
                    
                }






            //Set OutputCloud
            PointCloud newcloud = (PointCloud)pointCloud.Duplicate();
            

            //Dispose of Global Clouds.
            GlobalCloud.Dispose();
            pointCloud.Dispose();
            
            pointCloud = newcloud;



            //Return True on Finish
            return true;
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

            GlobalCloud = null;
          
        }
        #endregion - End of Abort Execution
    }
}