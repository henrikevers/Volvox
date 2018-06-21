using System;
using System.Runtime.CompilerServices;

namespace KDTree.Core
{
    [Serializable]
    public class KDNode<T>
	{
		protected internal int iDimensions;

		protected internal int iBucketCapacity;

        protected internal double[][] tPoints;

		protected internal T[] tData;

		protected internal KDNode<T> pLeft;

		protected internal KDNode<T> pRight;

		protected internal int iSplitDimension;

		protected internal double fSplitValue;

		protected internal double[] tMinBound;

		protected internal double[] tMaxBound;

		protected internal bool bSinglePoint;

      

        



        public bool IsLeaf
		{
			get
			{
				return this.tPoints != null;
			}
		}

		public int Size
		{
			get;
			private set;
		}

		protected KDNode(int iDimensions, int iBucketCapacity)
		{
			this.iDimensions = iDimensions;
			this.iBucketCapacity = iBucketCapacity;
			this.Size = 0;
			this.bSinglePoint = true;
			this.tPoints = new double[iBucketCapacity + 1][];
			this.tData = new T[iBucketCapacity + 1];
		}



		private void AddLeafPoint(double[] tPoint, T kValue)
		{
			this.tPoints[this.Size] = tPoint;
			this.tData[this.Size] = kValue;
			this.ExtendBounds(tPoint);
			this.Size = this.Size + 1;
			if (this.Size == (int)this.tPoints.Length - 1)
			{
				if (!this.CalculateSplit())
				{
					this.IncreaseLeafCapacity();
				}
				else
				{
					this.SplitLeafNode();
				}
			}
		}

		public void AddPoint(double[] tPoint, T kValue)
		{
			KDNode<T> kDNode = this;
			while (!kDNode.IsLeaf)
			{
				kDNode.ExtendBounds(tPoint);
				KDNode<T> size = kDNode;
				size.Size = size.Size + 1;
				kDNode = (tPoint[kDNode.iSplitDimension] <= kDNode.fSplitValue ? kDNode.pLeft : kDNode.pRight);
			}
			kDNode.AddLeafPoint(tPoint, kValue);
		}



        private bool CalculateSplit()
		{
			bool flag;
			if (!this.bSinglePoint)
			{
				double num = 0;
				for (int i = 0; i < this.iDimensions; i++)
				{
					double num1 = this.tMaxBound[i] - this.tMinBound[i];
					if (double.IsNaN(num1))
					{
						num1 = 0;
					}
					if (num1 > num)
					{
						this.iSplitDimension = i;
						num = num1;
					}
				}
				if (num != 0)
				{
					this.fSplitValue = (this.tMinBound[this.iSplitDimension] + this.tMaxBound[this.iSplitDimension]) * 0.5;
					if (this.fSplitValue == double.PositiveInfinity)
					{
						this.fSplitValue = double.MaxValue;
					}
					else if (this.fSplitValue == double.NegativeInfinity)
					{
						this.fSplitValue = double.MinValue;
					}
					if (this.fSplitValue == this.tMaxBound[this.iSplitDimension])
					{
						this.fSplitValue = this.tMinBound[this.iSplitDimension];
					}
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				flag = false;
			}
			return flag;
		}

		private bool CheckBounds(double[] tPoint)
		{
			bool flag;
			int num = 0;
			while (true)
			{
				if (num >= this.iDimensions)
				{
					flag = true;
					break;
				}
				else if (tPoint[num] > this.tMaxBound[num])
				{
					flag = false;
					break;
				}
				else if (tPoint[num] >= this.tMinBound[num])
				{
					num++;
				}
				else
				{
					flag = false;
					break;
				}
			}
			return flag;
		}

		private void ExtendBounds(double[] tPoint)
		{
			if (this.tMinBound != null)
			{
				for (int i = 0; i < this.iDimensions; i++)
				{
					if (double.IsNaN(tPoint[i]))
					{
						if ((!double.IsNaN(this.tMinBound[i]) ? true : !double.IsNaN(this.tMaxBound[i])))
						{
							this.bSinglePoint = false;
						}
						this.tMinBound[i] = double.NaN;
						this.tMaxBound[i] = double.NaN;
					}
					else if (this.tMinBound[i] > tPoint[i])
					{
						this.tMinBound[i] = tPoint[i];
						this.bSinglePoint = false;
					}
					else if (this.tMaxBound[i] < tPoint[i])
					{
						this.tMaxBound[i] = tPoint[i];
						this.bSinglePoint = false;
					}
				}
			}
			else
			{
				this.tMinBound = new double[this.iDimensions];
				this.tMaxBound = new double[this.iDimensions];
				Array.Copy(tPoint, this.tMinBound, this.iDimensions);
				Array.Copy(tPoint, this.tMaxBound, this.iDimensions);
			}
		}

		private void IncreaseLeafCapacity()
		{
			Array.Resize<double[]>(ref this.tPoints, (int)this.tPoints.Length * 2);
			Array.Resize<T>(ref this.tData, (int)this.tData.Length * 2);
		}

		private void SplitLeafNode()
		{
			this.pRight = new KDNode<T>(this.iDimensions, this.iBucketCapacity);
			this.pLeft = new KDNode<T>(this.iDimensions, this.iBucketCapacity);
			for (int i = 0; i < this.Size; i++)
			{
				double[] numArray = this.tPoints[i];
				T t = this.tData[i];
				if (numArray[this.iSplitDimension] <= this.fSplitValue)
				{
					this.pLeft.AddLeafPoint(numArray, t);
				}
				else
				{
					this.pRight.AddLeafPoint(numArray, t);
				}
			}
			this.tPoints = null;
			this.tData = null;
		}
	}
}