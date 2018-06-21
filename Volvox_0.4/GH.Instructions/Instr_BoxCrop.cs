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

namespace Volvox.GH.Instruction
{
    public class Instr_BoxCrop : Instr_BaseReporting
    {
        #region - Instruction Initialization
        /// <summary>
        /// Initialize Instruction Input Variables
        /// </summary>
        private List<Box> insV_Box { get; set; }
        private Boolean insV_InsideBool { get; set; }

        /// <summary>
        /// Set Instruction Input Variables
        /// </summary>
        /// <param name="B"></param>
        /// <param name="I"></param>
        public Instr_BoxCrop(List<Box> B, Boolean I)
        {
            //Order Boxes according to Volume and Set Input Variable BoxCrop. 
            Box[] Boxes = B.ToArray<Box>();
            double[] Volumes = new double[Boxes.Length];
            for (int i = 0; i <= Boxes.GetUpperBound(0); i += 1)
            {
                Volumes[i] = Boxes[i].Volume;
            }
            Array.Sort(Volumes, Boxes);
            Array.Reverse(Boxes);
            insV_Box = Boxes.ToList();

            //Set Input Variable InsideBool
            insV_InsideBool = I;
        }

        /*
        /// <summary>
        /// Set instruction.
        /// </summary>
        public Instr_BoxCrop()
        {
            insV_Box = new List<Box>();
            insV_InsideBool = new Boolean();
        }
        */

        /// <summary>
        /// Instruction GUID.
        /// </summary>
        public override Guid InstructionGUID
        {
            get { return GUIDs.GuidsRelease4.Instruction.Instr_BoxCrop; }
        }

        /// <summary>
        /// Set InstructionType / String to Return. 
        /// </summary>
        public override string InstructionType
        {
            get { return "Box Crop"; }
        }

        // Initialize Global Variables
        PointCloud GlobalCloud = null;
        PointCloud NewCloud = null;
        PointCloud[] CloudPieces = null;
        object[] DictPieces = null;

        int ProcCount = Environment.ProcessorCount;
        int PointCounter = 0;
        int LastPercentReported = 0;

        /// <summary>
        /// SetUp Duplication procedure. 
        /// </summary>
        /// <returns></returns>
        public override IGH_Goo Duplicate()
        {
            List<Box> nl = new List<Box>(insV_Box);
            Boolean nI = insV_InsideBool;

            Instr_BoxCrop ni = new Instr_BoxCrop(nl, nI);
            ni.GlobalCloud = null;
            ni.NewCloud = null;
            ni.CloudPieces = null;
            ni.DictPieces = null;
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
            NewCloud = new PointCloud();
            CloudPieces = null;
            CloudPieces = new PointCloud[ProcCount];
            DictPieces = new object[ProcCount];
            GlobalCloud = pointCloud;

            // Setup the cancellation mechanism.
            po.CancellationToken = cts.Token;

            //Create Partitions for multithreading.
            var rangePartitioner = System.Collections.Concurrent.Partitioner.Create(0, GlobalCloud.Count, (int)Math.Ceiling((double)GlobalCloud.Count / ProcCount));

            Dict_Utils.CastDictionary_ToArrayDouble(ref GlobalCloud);

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

                /// Initialize Partial Dictionary Lists
                List<double>[] MyDict = new List<double>[GlobalCloud.UserDictionary.Count];
                //Get Dictionary Values from PointCloud
                List<double[]> GlobalDict = new List<double[]>();
                GlobalDict.Clear();

                if (GlobalCloud.UserDictionary.Count > 0)
                {
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

                        //
                        if (insV_InsideBool)
                        {
                            for (int j = 0; j <= insV_Box.Count - 1; j += 1)
                            {
                                PointCloudItem GlobalCloudItem = GlobalCloud[i];
                                if (Math_Utils.IsInBox(GlobalCloudItem.Location, insV_Box[j]))
                                {
                                    Cloud_Utils.AddItem_FromOtherCloud(ref MyCloud, GlobalCloud, i);
                                    Dict_Utils.AddItem_FromGlobalDict(ref MyDict, GlobalDict, i);
                                    break; // TODO: might not be correct. Was : Exit For
                                }
                            }
                        }
                        else
                        {
                            List<Boolean> CropBools = new List<Boolean>();
                            PointCloudItem GlobalCloudItem = GlobalCloud[i];
                            for (int j = 0; j <= insV_Box.Count - 1; j += 1)
                            {
                                CropBools.Add(Math_Utils.IsInBox(GlobalCloudItem.Location, insV_Box[j]));
                            }
                            bool Crop = !CropBools.Any(x => x == true);

                            if (Crop)
                            {
                                Cloud_Utils.AddItem_FromOtherCloud(ref MyCloud, GlobalCloud, i);
                                Dict_Utils.AddItem_FromGlobalDict(ref MyDict, GlobalDict, i);
                            }
                        }

                        
                    }
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

                        //
                        if (insV_InsideBool)
                        {
                            for (int j = 0; j <= insV_Box.Count - 1; j += 1)
                            {
                                PointCloudItem GlobalCloudItem = GlobalCloud[i];
                                if (Math_Utils.IsInBox(GlobalCloudItem.Location, insV_Box[j]))
                                {
                                    Cloud_Utils.AddItem_FromOtherCloud(ref MyCloud, GlobalCloud, i);
                                    break; // TODO: might not be correct. Was : Exit For
                                }
                            }
                        }
                        else
                        {
                            List<Boolean> CropBools = new List<Boolean>();
                            PointCloudItem GlobalCloudItem = GlobalCloud[i];
                            for (int j = 0; j <= insV_Box.Count - 1; j += 1)
                            {
                                CropBools.Add(Math_Utils.IsInBox(GlobalCloudItem.Location, insV_Box[j]));
                            }
                            bool Crop = !CropBools.Any(x => x == true);

                            if (Crop)
                            {
                                Cloud_Utils.AddItem_FromOtherCloud(ref MyCloud, GlobalCloud, i);
                            }
                        }

                       
                    }
                }
                //Set DictPieces.
                if (GlobalCloud.UserDictionary.Count > 0)
                {
                    DictPieces[(int)MyIndex] = MyDict;
                }

                //Add MyCloud to CloudPieces at ProcesserIndex. 
                CloudPieces[(int)MyIndex] = MyCloud;

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

            //Set OutputCloud
            pointCloud = (PointCloud)NewCloud.Duplicate();
            
            //Dispose of PointCloud Pieces and NewCloud. 
            CloudPieces = null;
            DictPieces = null;
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

            CloudPieces = null;
            GlobalCloud = null;
            if (NewCloud != null)
                NewCloud.Dispose();
        }
        #endregion - End of Abort Execution
    }
}