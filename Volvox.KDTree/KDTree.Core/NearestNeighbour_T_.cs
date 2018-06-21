using System;
using System.Collections;
using System.Collections.Generic;

namespace KDTree.Core
{
	public class NearestNeighbour<T> : IEnumerator<T>, IDisposable, IEnumerator, IEnumerable<T>, IEnumerable
	{
		private double[] tSearchPoint;

		private DistanceFunctions kDistanceFunction;

		private MinHeap<KDNode<T>> pPending;

		private IntervalHeap<T> pEvaluated;

		private KDNode<T> pRoot;

		private int iMaxPointsReturned;

		private int iPointsRemaining;

		private double fThreshold;

		private double _CurrentDistance;

		private T _Current;

		public double CurrentDistance
		{
			get
			{
				return this._CurrentDistance;
			}
		}

		T IEnumerator<T>.Current
		{
			get
			{
				return this._Current;
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return this._Current;
			}
		}

		public NearestNeighbour(KDNode<T> pRoot, double[] tSearchPoint, DistanceFunctions kDistance, int iMaxPoints, double fThreshold)
		{
			if ((int)tSearchPoint.Length != pRoot.iDimensions)
			{
				throw new Exception("Dimensionality of search point and kd-tree are not the same.");
			}
			this.tSearchPoint = new double[(int)tSearchPoint.Length];
			Array.Copy(tSearchPoint, this.tSearchPoint, (int)tSearchPoint.Length);
			this.iPointsRemaining = Math.Min(iMaxPoints, pRoot.Size);
			this.fThreshold = fThreshold;
			this.kDistanceFunction = kDistance;
			this.pRoot = pRoot;
			this.iMaxPointsReturned = iMaxPoints;
			this._CurrentDistance = -1;
			this.pEvaluated = new IntervalHeap<T>();
			this.pPending = new MinHeap<KDNode<T>>();
			this.pPending.Insert(0, pRoot);
		}

		public void Dispose()
		{
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this;
		}

		public bool MoveNext()
		{
			bool flag;
			KDNode<T> kDNode;
			bool flag1;
			if (this.iPointsRemaining != 0)
			{
				while (true)
				{
					if (this.pPending.Size <= 0)
					{
						flag1 = false;
					}
					else
					{
						flag1 = (this.pEvaluated.Size == 0 ? true : this.pPending.MinKey < this.pEvaluated.MinKey);
					}
					if (!flag1)
					{
						break;
					}
					KDNode<T> min = this.pPending.Min;
					this.pPending.RemoveMin();
					while (!min.IsLeaf)
					{
						if (this.tSearchPoint[min.iSplitDimension] <= min.fSplitValue)
						{
							kDNode = min.pRight;
							min = min.pLeft;
						}
						else
						{
							kDNode = min.pLeft;
							min = min.pRight;
						}
						double rectangle = this.kDistanceFunction.DistanceToRectangle(this.tSearchPoint, kDNode.tMinBound, kDNode.tMaxBound);
						if ((this.fThreshold < 0 ? true : rectangle <= this.fThreshold))
						{
							if ((this.pEvaluated.Size < this.iPointsRemaining ? true : rectangle <= this.pEvaluated.MaxKey))
							{
								this.pPending.Insert(rectangle, kDNode);
							}
						}
					}
					if (!min.bSinglePoint)
					{
						for (int i = 0; i < min.Size; i++)
						{
							double num = this.kDistanceFunction.Distance(min.tPoints[i], this.tSearchPoint);
							if ((this.fThreshold < 0 ? true : num < this.fThreshold))
							{
								if (this.pEvaluated.Size < this.iPointsRemaining)
								{
									this.pEvaluated.Insert(num, min.tData[i]);
								}
								else if (num < this.pEvaluated.MaxKey)
								{
									this.pEvaluated.ReplaceMax(num, min.tData[i]);
								}
							}
						}
					}
					else
					{
						double num1 = this.kDistanceFunction.Distance(min.tPoints[0], this.tSearchPoint);
						if ((this.fThreshold < 0 ? false : num1 >= this.fThreshold))
						{
							continue;
						}
						else if ((this.pEvaluated.Size < this.iPointsRemaining ? true : num1 <= this.pEvaluated.MaxKey))
						{
							for (int j = 0; j < min.Size; j++)
							{
								if (this.pEvaluated.Size != this.iPointsRemaining)
								{
									this.pEvaluated.Insert(num1, min.tData[j]);
								}
								else
								{
									this.pEvaluated.ReplaceMax(num1, min.tData[j]);
								}
							}
						}
					}
				}
				if (this.pEvaluated.Size != 0)
				{
					this.iPointsRemaining = this.iPointsRemaining - 1;
					this._CurrentDistance = this.pEvaluated.MinKey;
					this._Current = this.pEvaluated.Min;
					this.pEvaluated.RemoveMin();
					flag = true;
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				this._Current = default(T);
				flag = false;
			}
			return flag;
		}

		public void Reset()
		{
			this.iPointsRemaining = Math.Min(this.iMaxPointsReturned, this.pRoot.Size);
			this._CurrentDistance = -1;
			this.pEvaluated = new IntervalHeap<T>();
			this.pPending = new MinHeap<KDNode<T>>();
			this.pPending.Insert(0, this.pRoot);
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}