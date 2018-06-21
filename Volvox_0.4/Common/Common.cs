using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Rhino.Geometry;
using System.Linq;
using System.Drawing;

namespace Volvox.Common
{

    /// <summary>
    /// PointCloud Utility Functions
    /// </summary>
    static class Cloud_Utils
    {
        /// <summary>
        /// Adds a PointCloudItem from OtherCloud to the end of MyCloud 
        /// and Adds Dictionary Values from OtherCloud to Dictionary Lists. 
        /// </summary>
        /// <param name="MyCloud"> Cloud to Add to.</param>
        /// <param name="OtherCloud"> Cloud to Add from.</param>
        /// <param name="index"> Index of CloudItem in OtherCloud to Add to end end of MyCloud. </param>
        public static void AddItem_FromOtherCloud(ref PointCloud MyCloud, PointCloud OtherCloud, int index)
        {
            PointCloudItem OtherCloudItem = OtherCloud[index];
            MyCloud.AppendNew();
            PointCloudItem MyCloudItem = MyCloud[MyCloud.Count - 1];
            MyCloudItem.Location = OtherCloudItem.Location;
            if (OtherCloud.ContainsColors)
                MyCloudItem.Color = OtherCloudItem.Color;
            if (OtherCloud.ContainsNormals)
                MyCloudItem.Normal = OtherCloudItem.Normal;
        }
        

        /// <summary>
        /// Merge PointCloud Pieces to NewCloud with UserDictionaries. 
        /// </summary>
        /// <param name="NewCloud"> PointCloud to Merge (add) to. </param>
        /// <param name="CloudPieces">PointCloud Pieces to Merge. </param>
        public static void MergeClouds(ref PointCloud NewCloud, PointCloud[] CloudPieces)
        {
            foreach (PointCloud pc in CloudPieces)
            {
                //Add PointCloud Pieces to NewCloud.
                if (pc != null)
                    NewCloud.Merge(pc);

                //Add UserDictionary from PointCloud Pieces to NewCloud. 
                if (pc.HasUserData)
                {
                    foreach (string key in pc.UserDictionary.Keys)
                    {
                        double[] newDict = null;
                        //Add to Dictionary if NewCloud already has a Dictionary with Key
                        //Else Create Dictionary with Key. 
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
        }
    }



    /// <summary>
    /// Volvox Cloud Dictionary Utility Functions
    /// </summary>
    static class Dict_Utils
    {
        /// <summary>
        /// Cast Cloud Dictionary to Array of Double.
        /// </summary>
        /// <param name="Mycloud"></param>
        public static void CastDictionary_ToArrayDouble(ref PointCloud Mycloud)
        {
            foreach (string Key in Mycloud.UserDictionary.Keys)

            {
                object obj = Mycloud.UserDictionary[Key];
                switch (obj)
                {
                    case double[] arr:
                        break;
                    case List<double> liDo:
                        double[] liDoArr = liDo.ToArray();
                        Mycloud.UserDictionary.Remove(Key);
                        Mycloud.UserDictionary.Set(Key, liDoArr);
                        break;
                    case List<int> liInt:
                        double[] liIntArr = liInt.Select(i => (double)i).ToArray();
                        Mycloud.UserDictionary.Remove(Key);
                        Mycloud.UserDictionary.Set(Key, liIntArr);
                        break;
                }

            }

        }

        /// <summary>
        /// Set USerDictionary from another cloud to this. 
        /// </summary>
        /// <param name="MyCloud"></param>
        /// <param name="OtherCloud"></param>
        public static void SetUserDict_FromOtherCloud(ref PointCloud MyCloud, PointCloud OtherCloud)
        {
            string[] keys = OtherCloud.UserDictionary.Keys;
            for (int k = 0; k < keys.Length; k++)
            {
                string key = OtherCloud.UserDictionary.Keys[k];
                if (!(key == "ColorGradient"))
                {
                    MyCloud.UserDictionary.Set(key, (double[])OtherCloud.UserDictionary[key]);
                }
                
            }
        }

        

        // --- Mainly Used in Multi-threading. ---
        /// <summary>
        /// Initialise Cloud Dictionary Lists for multi-threaded run.
        /// </summary>
        /// <param name="GlobalDict"></param>
        /// <param name="MyDict"></param>
        /// <param name="GlobalCloud"></param>
        public static void Initialize_Dictionary(ref List<double[]> GlobalDict, ref List<double>[] MyDict, PointCloud GlobalCloud)
        {

            for (int L = 0; L < MyDict.Length; L++)
            {
                MyDict[L] = new List<double>();
                string key = GlobalCloud.UserDictionary.Keys[L];
                double[] vals = (double[])GlobalCloud.UserDictionary[key];
                GlobalDict.Add(vals);
            }
        }

        /// <summary>
        /// <param name="MyDict"> Dictionaly Lists to Add to.</param>
        /// <param name="OtherCloud"> Cloud to Add from.</param>
        /// <param name="index"> Index of CloudItem in OtherCloud to Add to end end of MyCloud. </param>
        public static void AddItem_FromGlobalDict(ref List<double>[] MyDict, List<double[]> GlobalDict, int index)
        {
            for (int k = 0; k < GlobalDict.Count; k++)
            {
                if (index < GlobalDict[k].Length)
                {
                    double value = GlobalDict[k][index];
                    MyDict[k].Add(value);
                }
                else { break; }
            }
        }

        /// <summary>
        /// Sets the UserDictionary from a set of Value Lists and Keys. 
        /// </summary>
        /// <param name="MyCloud">Cloud to Set UserDictionary to. </param>
        /// <param name="MyDict"> Values to set as Lists of Lists. </param>
        /// <param name="Keys">Keys to Set for each Value List (Make sure to have same amount of Keys as lists). </param>
        public static void SetUserDict_FromDictLists(ref PointCloud MyCloud, List<double>[] MyDict, string[] Keys)
        {
            for (int k = 0; k < MyDict.Length; k++)
            {
                string key = Keys[k];
                double[] MyDictArr = MyDict[k].ToArray();
                MyCloud.UserDictionary.Set(key, MyDictArr);
            }
        }

        /// <summary>
        /// Merge Dictionary Pieces (E.G From when cropped cloud in multi-threded run).
        /// </summary>
        /// <param name="NewDict"></param>
        /// <param name="DictPieces"></param>
        public static void Merge_DictPieces(ref List<double>[] NewDict, object[] DictPieces)
        {
            for (int k = 0; k < NewDict.Length; k++)
            {
                NewDict[k] = new List<double>();
            }
            for (int a = 0; a < DictPieces.Length; a++)
            {
                List<double>[] dict = (List<double>[])DictPieces[a];
                if(dict != null)
                {
                    for (int key = 0; key < dict.Length; key++)
                    {
                        NewDict[key].AddRange(dict[key]);
                    }
                }
                
            }
        }





    }
    
    /// <summary>
    /// Volvox Cloud Color Utility Functions
    /// </summary>
    static class Color_Utils
    {

       
        /// <summary>
        /// Blend Color
        /// </summary>
        /// <param name="color"></param>
        /// <param name="backColor"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static Color Blend(Color color, Color backColor, double amount)
        {
            byte r = (byte)((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backColor.B * (1 - amount));
            return Color.FromArgb(r, g, b);
        }

       

        /// <summary>
        /// Convert RGB Color to GrayScale.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color GrayScale(Color color)
        {
            int grey = (int)(color.R * 0.3 + color.G * 0.59 + color.B * 0.11);
            Color greyCol = Color.FromArgb(grey, grey, grey);

            return greyCol;
        }

        /// <summary>
        /// Get the standard Values (5 values) for a UserDictionary.
        /// From Negative to Positive of abosolute maximum values of the userDictionary. 
        /// </summary>
        /// <param name="Cloud"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static List<double> ColorValues_Std_negpos(PointCloud Cloud, string Key)
        {
            List<double> nl = new List<double>();
            nl.AddRange((double[])Cloud.UserDictionary[Key]);

            double colorMaxValue = nl[0];

            foreach (double n in nl)
            {
                if (Math.Abs(n) > colorMaxValue) { colorMaxValue = Math.Abs(n); }
            }

            List<double> Values = new List<double>()
                    {
                        (double)-colorMaxValue,
                        (double)-colorMaxValue/2,
                        0.00,
                        (double)colorMaxValue/2,
                        (double)colorMaxValue
                    };

            return Values;
        }

        /// <summary>
        /// Get the standard Values (5 values) for a UserDictionary.
        /// From Zero to Positive of maximum values of the userDictionary. 
        /// </summary>
        /// <param name="Cloud"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static List<double> ColorValues_Std_pos(PointCloud Cloud, string Key)
        {
            List<double> nl = new List<double>();
            nl.AddRange((double[])Cloud.UserDictionary[Key]);

            double colorMaxValue = nl[0];

            foreach (double n in nl)
            {
                if (Math.Abs(n) > colorMaxValue) { colorMaxValue = Math.Abs(n); }
            }

            List<double> Values = new List<double>()
                    {
                        0.00,
                        (double)colorMaxValue/4,
                        (double)colorMaxValue/2,
                        (double)colorMaxValue*3/4,
                        (double)colorMaxValue
                    };

            return Values;
        }

        /// <summary>
        /// Get the standard Color Gradient colors of 5 colors from blue to red. 
        /// </summary>
        /// <returns></returns>
        public static List<Color> ColorGradient_Std_BtoR()
        {
            return new List<Color>()
                    {
                        Color.FromArgb(0,46,171),
                        Color.FromArgb(1,192,229),
                        Color.FromArgb(18,178,90),
                        Color.FromArgb(220,237,32),
                        Color.FromArgb(243,57,0)
                    };
        }

        /// <summary>
        /// Get the standard Color Gradient colors of 5 colors from blue to red. 
        /// </summary>
        /// <returns></returns>
        public static List<Color> ColorGradient_Std_GtoR()
        {
            return new List<Color>()
                    {
                        Color.FromArgb(0,173,78),
                        Color.FromArgb(120,211,69),
                        Color.FromArgb(229,245,61),
                        Color.FromArgb(243,149,29),
                        Color.FromArgb(255,60,0)
                    };
        }

        /// <summary>
        /// Set UserDictionry of Cloud for ColorGradient Used. 
        /// </summary>
        public static void Set_ColorGradient_Dict(ref PointCloud Cloud, List<Color> Colors, List<double> ColorValues)
        {
            List<double> R = new List<double>();
            List<double> G = new List<double>();
            List<double> B = new List<double>();
            foreach (Color color in Colors)
            {
                R.Add(color.R);
                G.Add(color.G);
                B.Add(color.B);
            }
            Rhino.Collections.ArchivableDictionary ColorDict = new Rhino.Collections.ArchivableDictionary();
            ColorDict.Set("R", R);
            ColorDict.Set("G", G);
            ColorDict.Set("B", B);
            ColorDict.Set("Val", ColorValues);
            Cloud.UserDictionary.Set("ColorGradient", ColorDict);
        }

        /// <summary>
        /// Get the Colors of The ColorGradient UserDictionary. 
        /// </summary>
        /// <param name="Cloud">Input PointCloud</param>
        /// <returns></returns>
        public static List<Color> Get_Colors(PointCloud Cloud)
        {

                Rhino.Collections.ArchivableDictionary ColorDict = (Rhino.Collections.ArchivableDictionary)Cloud.UserDictionary["ColorGradient"];

                List<double> Rl = new List<double>();
                List<double> Gl = new List<double>();
                List<double> Bl = new List<double>();

                Rl.AddRange((List<double>)ColorDict["R"]);
                Gl.AddRange((List<double>)ColorDict["G"]);
                Bl.AddRange((List<double>)ColorDict["B"]);

                List<Color> Colors = new List<Color>();
                for (int i = 0; i < Rl.Count; i++)
                {
                    Color color = Color.FromArgb((int)Rl[i], (int)Gl[i], (int)Bl[i]);
                    Colors.Add(color);
                }
                return Colors;

        }

        /// <summary>
        /// Get the Values of The ColorGradient UserDictionary. 
        /// </summary>
        /// <param name="Cloud">Input PointCloud</param>
        /// <returns></returns>
        public static List<double> Get_ColorValues(PointCloud Cloud)
        {
                Rhino.Collections.ArchivableDictionary ColorDict = (Rhino.Collections.ArchivableDictionary)Cloud.UserDictionary["ColorGradient"];

                List<double> Values = new List<double>();
                Values.AddRange((List<double>)ColorDict["Val"]);
                return Values;
        }

    }

}

