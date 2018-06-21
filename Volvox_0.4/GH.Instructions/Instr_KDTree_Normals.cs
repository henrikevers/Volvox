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
using KDTree.Common;

namespace Volvox.GH.Instruction
{
    public class Instr_KDTree_Normals : Instr_BaseReporting
    {
        #region - Instruction Initialization
        /// <summary>
        /// Initialize Instruction Input Variables
        /// </summary>
        private KDTree<int> insV_KDTree { get; set; }
        private Boolean insV_Colorize { get; set; }
        private Vector3d insV_GuideVector { get; set; }
        private int insV_NumN { get; set; }
        private Boolean insV_GuideStyle { get; set; }


        /// <summary>
        /// Calculate Normals
        /// </summary>
        /// <param name="kdTree"></param>
        /// <param name="GV"></param>
        /// <param name="GS"></param>
        /// <param name="C_bool"></param>
        public Instr_KDTree_Normals(KDTree<int> kdTree, Vector3d GV, Boolean GS, int NN, Boolean C_bool)
        {
            insV_KDTree = kdTree;
            insV_GuideVector = GV;
            insV_GuideStyle = GS;
            insV_NumN = NN;
            insV_Colorize = C_bool;

        }


        /// <summary>
        /// Instruction GUID.
        /// </summary>
        public override Guid InstructionGUID
        {
            get { return GUIDs.GuidsRelease4.Instruction.Instr_KDTree_Normals; }
        }

        /// <summary>
        /// Set InstructionType / String to Return. 
        /// </summary>
        public override string InstructionType
        {
            get { return "Calc Normals"; }
        }

        // Initialize Global Variables
        PointCloud GlobalCloud = null;
       //List<double[]> GlobalDict = new List<double[]>();
        PointCloud NewCloud = null;
        PointCloud[] CloudPieces = null;
        object[] DictPieces = null;

        int ProcCount = Environment.ProcessorCount;
        int PointCounter = 0;
        int LastPercentReported = 0;

        KDTree<int> tree = null;
        List<Point3d> pts = null;

        /// <summary>
        /// SetUp Duplication procedure. 
        /// </summary>
        /// <returns></returns>
        public override IGH_Goo Duplicate()
        {
            KDTree<int> nKd = insV_KDTree;
            Vector3d nGV =  insV_GuideVector;
            Boolean nGS = insV_GuideStyle;
            int nNN = insV_NumN;
            Boolean nC = insV_Colorize;

            Instr_KDTree_Normals ni = new Instr_KDTree_Normals(nKd, nGV, nGS, nNN, nC); 
            ni.GlobalCloud = null;
            ni.NewCloud = null;
            ni.CloudPieces = null;
            ni.DictPieces = null;
            ni.PointCounter = 0;
            ni.LastPercentReported = 0;

            ni.tree = null;
            ni.pts = null;

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

            if (insV_KDTree == null)
            {
                tree = KDTreeLib.ConstructKDTree(pointCloud);
            }
            else
            {
                tree = insV_KDTree;
            }

            Dict_Utils.CastDictionary_ToArrayDouble(ref pointCloud);
            GlobalCloud = pointCloud;

            NewCloud = new PointCloud();
            CloudPieces = null;
            CloudPieces = new PointCloud[ProcCount];
            DictPieces = new object[ProcCount];

            pts = pointCloud.GetPoints().ToList();

            // Setup the cancellation mechanism.
            po.CancellationToken = cts.Token;

            //Create Partitions for multithreading.
            var rangePartitioner = System.Collections.Concurrent.Partitioner.Create(0, GlobalCloud.Count, (int)Math.Ceiling((double)GlobalCloud.Count / ProcCount));

            Dict_Utils.CastDictionary_ToArrayDouble(ref GlobalCloud);

            //Search for NearestNeighbors per point in PointCloud. Gets the indices in the Cloud for the neighbors to each Point.
            List<double> NRange = new List<double> { double.MaxValue };
            List<int>[] idc = KDTreeLib.NearestNeighborSearch(tree, pts, NRange, insV_NumN);

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
                int count = 0;
                if (GlobalCloud.UserDictionary.Count > 0)
                {
                    /// Initialize Partial Dictionary Lists
                    List<double>[] MyDict = new List<double>[GlobalCloud.UserDictionary.Count];

                    //Get Dictionary Values from PointCloud
                    List<double[]> GlobalDict = new List<double[]>();
                    GlobalDict.Clear();

                    Dict_Utils.Initialize_Dictionary(ref GlobalDict, ref MyDict, GlobalCloud);

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

                        calcNormal(idc, ref MyCloud, ref count, i);
                        Dict_Utils.AddItem_FromGlobalDict(ref MyDict, GlobalDict, i);
                    }
                        
                    //Add MyCloud to CloudPieces at ProcesserIndex. 
                    CloudPieces[(int)MyIndex] = MyCloud;
                    //Set DictPieces.
                    DictPieces[(int)MyIndex] = MyDict;

                }
                else
                {
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

                        calcNormal(idc, ref MyCloud, ref count, i);
                    }
                    //Add MyCloud to CloudPieces at ProcesserIndex. 
                    CloudPieces[(int)MyIndex] = MyCloud;
                }
                //Enable Parrallel Computing Cancellation
                po.CancellationToken.ThrowIfCancellationRequested();
                
            }
            );



            //Merge PointCloud Pieces into One PointCloud. 
            Cloud_Utils.MergeClouds(ref NewCloud, CloudPieces);
            if (GlobalCloud.UserDictionary.Count > 0)
            {
                List<double>[] NewDict = new List<double>[GlobalCloud.UserDictionary.Count];
                Dict_Utils.Merge_DictPieces(ref NewDict, DictPieces);
                Dict_Utils.SetUserDict_FromDictLists(ref NewCloud, NewDict, GlobalCloud.UserDictionary.Keys);
            }
            

            //Dispose of Global Clouds.
            GlobalCloud.Dispose();
            pointCloud.Dispose();

            /*
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
            */

            pointCloud = (PointCloud)NewCloud.Duplicate();

            //Dispose of PointCloud Pieces and NewCloud. 
            GlobalCloud.Dispose();
            NewCloud.Dispose();

            //Return True on Finish
            return true;
        }

        private void calcNormal(List<int>[] idc, ref PointCloud MyCloud, ref int count, int i)
        {


                List<Point3d> normalPts = new List<Point3d>();
                normalPts.Add(GlobalCloud[i].Location);
                List<int> ids = idc[i];

                if (ids.Count >= 3)
                {
                    for (int j = 0; j < ids.Count; j++)
                    {
                        Point3d nPt = GlobalCloud[ids[j]].Location;
                        normalPts.Add(nPt);
                    }
                    //Get not-oriented normal from Z-Axis of best fit plane to NormalPoints.
                    Plane normalPln = new Plane();
                    Plane.FitPlaneToPoints(normalPts, out normalPln);

                    Vector3d notOriNormal = normalPln.ZAxis;

                    //Orient not-oriented NormalVector according to the guiding vector.
                    Vector3d guideVector = new Vector3d();
                    if (insV_GuideStyle == true) { guideVector = insV_GuideVector; }
                    else if (insV_GuideStyle == false) { guideVector = Point3d.Subtract(GlobalCloud[i].Location, new Point3d(insV_GuideVector)); }
                    guideVector.Unitize();

                    Vector3d normalVector = new Vector3d();
                    double vecAngle = Vector3d.VectorAngle(notOriNormal, guideVector);
                    if (vecAngle < Math.PI / 2) { normalVector = notOriNormal; }
                    else
                    {
                        normalVector = notOriNormal;
                        normalVector.Reverse();
                    }

                    //Add normal to Cloud.
                    Cloud_Utils.AddItem_FromOtherCloud(ref MyCloud, GlobalCloud, i);
                    MyCloud[count].Normal = normalVector;

                    if (insV_Colorize)
                    {
                        MyCloud[count].Color = KDTreeLib.normalColors(1, normalVector, GlobalCloud[i].Color);
                    }

                    count += 1;
                }

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