using System;

namespace KDTree.Core
{
    [Serializable]
    public class KDTree<T> : KDNode<T>
	{
		public KDTree(int iDimensions) : base(iDimensions, 24)
		{
		}

		public KDTree(int iDimensions, int iBucketCapacity) : base(iDimensions, iBucketCapacity)
		{
		}

		public NearestNeighbour<T> NearestNeighbors(double[] tSearchPoint, int iMaxReturned, double fDistance = -1)
		{
			return this.NearestNeighbors(tSearchPoint, new SquareEuclideanDistanceFunction(), iMaxReturned, fDistance);
		}

		public NearestNeighbour<T> NearestNeighbors(double[] tSearchPoint, DistanceFunctions kDistanceFunction, int iMaxReturned, double fDistance)
		{
			return new NearestNeighbour<T>(this, tSearchPoint, kDistanceFunction, iMaxReturned, fDistance);
		}
	}
}