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
    public class Instr_MeshRay : Instr_BaseReporting
    {
        #region - Instruction Initialization
        /// <summary>
        /// Initialize Instruction Input Variables
        /// </summary>
        private Mesh insV_Mesh { get; set; }
        private Vector3d insV_Vector { get; set; }
        private string insV_Key { get; set; }
        private Boolean insV_Colorize { get; set; }
        //private List<double> insV_ColorValues { get; set; }
        //private Boolean insV_AngleApp { get; set; }

        /// <summary>
        /// Set Instruction Input Variables
        /// </summary>
        /// <param name="M"></param>
        /// <param name="K"></param>
        /// <param name="C_bool"></param>
        public Instr_MeshRay(Mesh M, Vector3d V, string K, Boolean C_bool) //, List<double> C_Val)
        {
             insV_Mesh = M;
            insV_Vector = V;
             insV_Key = K;
             insV_Colorize = C_bool;
            //insV_ColorValues = C_Val;
             //insV_AngleApp = A_bool;

        }


        /// <summary>
        /// Instruction GUID.
        /// </summary>
        public override Guid InstructionGUID
        {
            get { return GUIDs.GuidsRelease5.Instruction.Instr_MeshRay; }
        }

        /// <summary>
        /// Set InstructionType / String to Return. 
        /// </summary>
        public override string InstructionType
        {
            get { return "Mesh Compare"; }
        }

        // Initialize Global Variables
        PointCloud GlobalCloud = null;
        PointCloud NewCloud = null;

        int ProcCount = Environment.ProcessorCount;
        int PointCounter = 0;
        int LastPercentReported = 0;

        double[] Distances = null;

        /// <summary>
        /// SetUp Duplication procedure. 
        /// </summary>
        /// <returns></returns>
        public override IGH_Goo Duplicate()
        {
            Mesh nM = insV_Mesh;
            Vector3d nV = insV_Vector;
            string nK = insV_Key;
            Boolean nC = insV_Colorize;
            //List<double> nV  = insV_ColorValues;
            // Boolean nA = insV_AngleApp;

            Instr_MeshRay ni = new Instr_MeshRay(nM, nV, nK, nC); 
            ni.GlobalCloud = null;
            ni.NewCloud = null;
            ni.PointCounter = 0;
            ni.LastPercentReported = 0;
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
            NewCloud = new PointCloud();//(PointCloud)pointCloud.Duplicate();

            Array.Resize(ref Distances, pointCloud.Count);
            double halfpi = Math.PI / 2;

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
                        PointCloudItem GlobalCloudItem = GlobalCloud[i];
                        Point3d p3 = GlobalCloudItem.Location;
                        Point3d pm = new Point3d();
                        Vector3d pv = new Vector3d();

                    Ray3d ray = new Ray3d(p3, insV_Vector);
                    double t = Rhino.Geometry.Intersect.Intersection.MeshRay(insV_Mesh, ray);
                    if(t > 0.0)
                    {
                       
                        Point3d pt = ray.PointAt(t);
                        insV_Mesh.ClosestPoint(pt, out pm, out pv, 0);
                        double d = p3.DistanceTo(pm);

                        if (Vector3d.VectorAngle(pv, new Vector3d(pm - p3)) < halfpi)
                            d = -d;
                        Distances[i] = d;
                        NewCloud.Add(GlobalCloudItem.Location, GlobalCloudItem.Normal, GlobalCloudItem.Color);
                            
                    }
                  

                   
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
                List<double> colorValues = Color_Utils.ColorValues_Std_negpos(NewCloud, insV_Key);
                List<Color> Colors = Color_Utils.ColorGradient_Std_BtoR();

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