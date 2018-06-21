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
using Volvox.GH.Params;
using KDTree.Core;

namespace Volvox.GH.Instruction
{
    public class Instr_KDTree_CloudCloud : Instr_BaseReporting
    {
        #region - Instruction Initialization
        /// <summary>
        /// Initialize Instruction Input Variables
        /// </summary>
        private Type_KDTree insV_KDTree { get; set; }
        private string insV_Key { get; set; }
        private Boolean insV_Colorize { get; set; }
        private int insV_Amount { get; set; }


        /// <summary>
        /// Set Instruction Input Variables
        /// Calculate closestPoint distances between the two Clouds (KDTree Already Constructed).
        /// </summary>
        /// <param name="kdTree"></param>
        /// <param name="K"></param>
        ///  <param name="A">Amount of closest points to average out.</param>/param
        /// <param name="C_bool"></param>
        public Instr_KDTree_CloudCloud(Type_KDTree kdTree, string K, int A, Boolean C_bool)
        {
             insV_KDTree = kdTree;
             insV_Key = K;
             insV_Colorize = C_bool;
                insV_Amount = A;
        }


        /// <summary>
        /// Instruction GUID.
        /// </summary>
        public override Guid InstructionGUID
        {
            get { return GUIDs.GuidsRelease4.Instruction.Instr_KDTree_CloudCloud; }
        }

        /// <summary>
        /// Set InstructionType / String to Return. 
        /// </summary>
        public override string InstructionType
        {
            get { return "Cloud Cloud Compare"; }
        }

        // Initialize Global Variables
        PointCloud GlobalCloud = null;
        PointCloud NewCloud = null;

        int ProcCount = Environment.ProcessorCount;
        int PointCounter = 0;
        int LastPercentReported = 0;

        KDTree<int> tree = null;
        PointCloud cloudTo = null;
        Point3d[] pts = null;

        double[] Distances = null;


        /// <summary>
        /// SetUp Duplication procedure. 
        /// </summary>
        /// <returns></returns>
        public override IGH_Goo Duplicate()
        {
            Type_KDTree nKd = insV_KDTree;
            string nK = insV_Key;
            int nA = insV_Amount;
            Boolean nC = insV_Colorize;

            Instr_KDTree_CloudCloud ni = new Instr_KDTree_CloudCloud(nKd, nK, nA, nC); 
            ni.GlobalCloud = null;
            ni.NewCloud = null;
            ni.PointCounter = 0;
            ni.LastPercentReported = 0;
            ni.tree = null;
            ni.cloudTo = null;
            ni.pts = null;
            ni.Distances = null;
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

            Dict_Utils.CastDictionary_ToArrayDouble(ref pointCloud);
            GlobalCloud = pointCloud;
            NewCloud = (PointCloud)pointCloud.Duplicate();

            tree = insV_KDTree.Value.Item1;
            cloudTo = insV_KDTree.Value.Item2.Value;
            pts = pointCloud.GetPoints();
            Array.Resize(ref Distances, pointCloud.Count);

            // Setup the cancellation mechanism.
            po.CancellationToken = cts.Token;

            //Create Partitions for multithreading.
            var rangePartitioner = System.Collections.Concurrent.Partitioner.Create(0, GlobalCloud.Count, (int)Math.Ceiling((double)GlobalCloud.Count / ProcCount));


           

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
                        //Point to measure From
                        var p = pts[i];
                        //Range
                        var r = Double.MaxValue;
                        //Get one Nearest Neighbor (Part of KDTree.dll)
                        var ns = tree.NearestNeighbors(new double[] { p.X, p.Y, p.Z }, insV_Amount, r);
                        //The index of the Nearest Point in the PointCloud to Measure To.
                        var indList = ns.ToList();

                        double D = 0.0;
                        foreach (int idx in indList)
                        {
                            //Distance between Point measured From to Point meassured To.
                            //Point meassured From.
                            Point3d P2 = cloudTo[idx].Location;
                            //Distance betwen Points
                            //double D = fastDist(p, P2);
                            //Cumulative Distance
                            D = D + p.DistanceTo(P2);
                        }
                        //Average distances
                        double aveD = D / indList.Count;
                        //Add Distances to Array.
                        Distances[i] = aveD;
                    }
            


                //Enable Parrallel Computing Cancellation
                po.CancellationToken.ThrowIfCancellationRequested();
            }
            );




            //Dispose of Global Clouds.
            GlobalCloud.Dispose();
            pointCloud.Dispose();

            //Set OutputCloud
            NewCloud.UserDictionary.Set(insV_Key, Distances);
            //Colorize
            if (insV_Colorize)
            {
                List<double> colorValues = Color_Utils.ColorValues_Std_pos(NewCloud, insV_Key);
                List<Color> Colors = Color_Utils.ColorGradient_Std_GtoR();

                Instruction.Instr_Dict_Color col = new Instruction.Instr_Dict_Color(insV_Key, colorValues, Colors, -1, 0.00);
                Boolean ColResult = col.Execute(ref NewCloud);

                //Set ColorGradient UserData
                Color_Utils.Set_ColorGradient_Dict(ref NewCloud, Colors, colorValues);
            }


            pointCloud = (PointCloud)NewCloud.Duplicate();

            //Dispose of PointCloud Pieces and NewCloud. 
            GlobalCloud.Dispose();
            NewCloud.Dispose();

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
            if (NewCloud != null)
                NewCloud.Dispose();
        }
        #endregion - End of Abort Execution
    }
}