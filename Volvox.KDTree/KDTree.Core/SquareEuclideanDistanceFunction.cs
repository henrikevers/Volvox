using System;

namespace KDTree.Core
{
	public class SquareEuclideanDistanceFunction : DistanceFunctions
	{
		public SquareEuclideanDistanceFunction()
		{
		}

		public double Distance(double[] p1, double[] p2)
		{
			double num = 0;
			for (int i = 0; i < (int)p1.Length; i++)
			{
				double num1 = p1[i] - p2[i];
				num = num + num1 * num1;
			}
			return num;
		}

		public double DistanceToRectangle(double[] point, double[] min, double[] max)
		{
			double num = 0;
			double num1 = 0;
			for (int i = 0; i < (int)point.Length; i++)
			{
				num1 = 0;
				if (point[i] > max[i])
				{
					num1 = point[i] - max[i];
				}
				else if (point[i] < min[i])
				{
					num1 = point[i] - min[i];
				}
				num = num + num1 * num1;
			}
			return num;
		}
	}
}