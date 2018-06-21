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

namespace Volvox.GH.Instruction
{
    public class Instr_BoxCrop : Instr_BaseReporting
    {

        public List<Box> BoxCrop { get; set; }
        public Boolean InsideBool { get; set; }

        public Instr_BoxCrop(List<Box> B, Boolean I)
        {
            Box[] Boxes = B.ToArray<Box>();
            double[] Volumes = new double[Boxes.Length];
            for (int i = 0; i <= Boxes.GetUpperBound(0); i += 1)
            {
                Volumes[i] = Boxes[i].Volume;
            }

            Array.Sort(Volumes, Boxes);
            Array.Reverse(Boxes);
            BoxCrop = Boxes.ToList();

            InsideBool = I;
        }

        public Instr_BoxCrop()
        {
            BoxCrop = new List<Box>();
            InsideBool = new Boolean();
        }

        public override Guid InstructionGUID
        {
            //get { return GuidsRelease1.Instr_BoxCrop; }
            get { return new Guid("26f4942e-18ee-4485-a9bb-e669d031204c"); }
        }

        public override string InstructionType
        {
            get { return "Box Crop"; }
        }

        // Global Variables
        PointCloud GlobalCloud = null;
        PointCloud NewCloud = null;
        PointCloud[] CloudPieces = null;
        int ProcCount = Environment.ProcessorCount;
        int PointCounter = 0;
        int LastPercentReported = 0;

        // Setup the cancellation mechanism.
        CancellationTokenSource cts = new CancellationTokenSource();
        ParallelOptions po = new ParallelOptions();


        public override IGH_Goo Duplicate()
        {

            List<Box> nl = new List<Box>(BoxCrop);
            Boolean nI = InsideBool;

            Instr_BoxCrop ni = new Instr_BoxCrop(nl, nI);
            ni.GlobalCloud = null;
            ni.NewCloud = null;
            ni.CloudPieces = null;
            ni.PointCounter = 0;
            ni.LastPercentReported = 0;

            return ni;

        }

        public override bool Execute(ref PointCloud pointCloud)
        {

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

            //Run MultiThreaded Loop.
            Parallel.ForEach(rangePartitioner, po, (rng, loopState) =>

            {

                //Initialize Partial PointCloud
                PointCloud MyCloud = new PointCloud();

                //Initialize Partial Dictionary Lists
                List<double>[] myDict = new List<double>[GlobalCloud.UserDictionary.Count];
                for (int L = 0; L < myDict.Length; L++)
                {
                    myDict[L] = new List<double>();
                }
                    

                double totc = (double)1 / GlobalCloud.Count;
                int MyIndex = (int)(rng.Item1 / Math.Ceiling(((double)GlobalCloud.Count / ProcCount)));

                for (int i = rng.Item1; i < rng.Item2; i++)
                {
                Interlocked.Increment(ref PointCounter);
                    double tmp = PointCounter * totc * 100;


                    if (LastPercentReported < ((PointCounter * totc) * 100))
                    {
                        LastPercentReported = (int)(5 * Math.Ceiling((double)(PointCounter * totc) * 20));
                        this.ReportPercent = LastPercentReported;

                    }

                    if(InsideBool)
                    {
                        for (int j = 0; j <= BoxCrop.Count - 1; j += 1)
                        {
                            PointCloudItem GlobalCloudItem = GlobalCloud[i];
                            if (Math_Utils.IsInBox(GlobalCloudItem.Location, BoxCrop[j]))
                            {

                                //AddPointCloudItem(ref MyCloud, GlobalCloudItem);
                                
                                MyCloud.AppendNew();
                                PointCloudItem MyCloudItem = MyCloud[MyCloud.Count - 1];
                                MyCloudItem.Location = GlobalCloudItem.Location;
                                if (GlobalCloud.ContainsColors)
                                    MyCloudItem.Color = GlobalCloudItem.Color;
                                if (GlobalCloud.ContainsNormals)
                                    MyCloudItem.Normal = GlobalCloudItem.Normal;
                                if (GlobalCloud.HasUserData)

                                    for (int k = 0; k < GlobalCloud.UserDictionary.Count; k++)
                                    {
                                        string key = GlobalCloud.UserDictionary.Keys[k];
                                        double value = ((Double[])GlobalCloud.UserDictionary[key])[i];
                                        myDict[k].Add(value);
                                    }
   
                                break; // TODO: might not be correct. Was : Exit For
                            }
                        }
                    }


                    else
                    {
                        List<Boolean> CropBools = new List<Boolean>();
                        PointCloudItem GlobalCloudItem = GlobalCloud[i];
                        for (int j = 0; j <= BoxCrop.Count - 1; j += 1)
                        {
                            CropBools.Add(Math_Utils.IsInBox(GlobalCloudItem.Location, BoxCrop[j]));
                        }
                        bool Crop = !CropBools.Any(x => x == true);

                        if (Crop)
                        {
                            MyCloud.AppendNew();
                            PointCloudItem MyCloudItem = MyCloud[MyCloud.Count - 1];
                            MyCloudItem.Location = GlobalCloudItem.Location;
                            if (GlobalCloud.ContainsColors)
                                MyCloudItem.Color = GlobalCloudItem.Color;
                            if (GlobalCloud.ContainsNormals)
                                MyCloudItem.Normal = GlobalCloudItem.Normal;
                            if (GlobalCloud.HasUserData)

                                for (int k = 0; k < GlobalCloud.UserDictionary.Count; k++)
                                {
                                    string key = GlobalCloud.UserDictionary.Keys[k];
                                    double value = ((Double[])GlobalCloud.UserDictionary[key])[i];
                                    myDict[k].Add(value);
                                }

                            //break; // TODO: might not be correct. Was : Exit For
                        }
                    }
                }

                for (int k = 0; k < myDict.Length; k++)
                {
                    string key = GlobalCloud.UserDictionary.Keys[k];
                    double[] MyDictArr = myDict[k].ToArray();
                    MyCloud.UserDictionary.Set(key, MyDictArr);
                }


                CloudPieces[(int)MyIndex] = MyCloud;

                po.CancellationToken.ThrowIfCancellationRequested();
            }
            );


            GlobalCloud.Dispose();
            pointCloud.Dispose();
            foreach (PointCloud pc in CloudPieces)
            {
                if (pc != null)
                    NewCloud.Merge(pc);
                
               
                if (pc.HasUserData)
                {
                    foreach (string key in pc.UserDictionary.Keys)
                    {
                        double[] newDict = null;
                        
                        if (NewCloud.UserDictionary.ContainsKey(key))
                        {
                            double[] Dict = (double[])NewCloud.UserDictionary[key];
                            double[] MyDict = (double[])pc.UserDictionary[key];
                            List<Double> listNewDict = new List<double>(Dict);
                            listNewDict.AddRange(MyDict);
                            newDict = listNewDict.ToArray();

                        }
                        else
                        {
                            newDict = (double[])pc.UserDictionary[key];
                        }
                   
                        NewCloud.UserDictionary.Set(key, newDict);
                        
                        newDict = null;
                    }

                }
                
            }

            pointCloud = (PointCloud)NewCloud.Duplicate();
            CloudPieces = null;
            NewCloud.Dispose();
            

            return true;

        }

        /*
public void AddPointCloudItem(ref PointCloud MyCloud, PointCloudItem GlobalCloudItem)
        {
            MyCloud.AppendNew();
            PointCloudItem MyCloudItem = MyCloud[MyCloud.Count - 1];
            MyCloudItem.Location = GlobalCloudItem.Location;
            if (GlobalCloud.ContainsColors)
                MyCloudItem.Color = GlobalCloudItem.Color;
            if (GlobalCloud.ContainsNormals)
                MyCloudItem.Normal = GlobalCloudItem.Normal;
            if (GlobalCloud.HasUserData)
                foreach (string key in GlobalCloud.UserDictionary.Keys)
                {
                    double dict = (double)GlobalCloud.UserDictionary[key];

                }

        }

    */

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

    }
}