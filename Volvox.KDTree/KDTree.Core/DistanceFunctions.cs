using System;

namespace KDTree.Core
{
	public interface DistanceFunctions
	{
		double Distance(double[] p1, double[] p2);

		double DistanceToRectangle(double[] point, double[] min, double[] max);
	}
}