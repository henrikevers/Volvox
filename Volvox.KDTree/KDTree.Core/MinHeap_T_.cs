using System;
using System.Runtime.CompilerServices;

namespace KDTree.Core
{
	public class MinHeap<T>
	{
		private static int DEFAULT_SIZE;

		private T[] tData;

		private double[] tKeys;

		public int Capacity
		{
			get;
			private set;
		}

		public T Min
		{
			get
			{
				if (this.Size == 0)
				{
					throw new Exception();
				}
				return this.tData[0];
			}
		}

		public double MinKey
		{
			get
			{
				if (this.Size == 0)
				{
					throw new Exception();
				}
				return this.tKeys[0];
			}
		}

		public int Size
		{
			get;
			private set;
		}

		static MinHeap()
		{
			MinHeap<T>.DEFAULT_SIZE = 64;
		}

		public MinHeap() : this(MinHeap<T>.DEFAULT_SIZE)
		{
		}

		public MinHeap(int iCapacity)
		{
			this.tData = new T[iCapacity];
			this.tKeys = new double[iCapacity];
			this.Capacity = iCapacity;
			this.Size = 0;
		}

		public void Insert(double key, T value)
		{
			if (this.Size >= this.Capacity)
			{
				this.Capacity = this.Capacity * 2;
				T[] tArray = new T[this.Capacity];
				Array.Copy(this.tData, tArray, (int)this.tData.Length);
				this.tData = tArray;
				double[] numArray = new double[this.Capacity];
				Array.Copy(this.tKeys, numArray, (int)this.tKeys.Length);
				this.tKeys = numArray;
			}
			this.tData[this.Size] = value;
			this.tKeys[this.Size] = key;
			this.SiftUp(this.Size);
			this.Size = this.Size + 1;
		}

		public void RemoveMin()
		{
			if (this.Size == 0)
			{
				throw new Exception();
			}
			this.Size = this.Size - 1;
			this.tData[0] = this.tData[this.Size];
			this.tKeys[0] = this.tKeys[this.Size];
			this.tData[this.Size] = default(T);
			this.SiftDown(0);
		}

		private void SiftDown(int iParent)
		{
			int num = iParent * 2 + 1;
			while (num < this.Size)
			{
				if ((num + 1 >= this.Size ? false : this.tKeys[num] > this.tKeys[num + 1]))
				{
					num++;
				}
				if (this.tKeys[iParent] <= this.tKeys[num])
				{
					break;
				}
				else
				{
					T t = this.tData[iParent];
					double num1 = this.tKeys[iParent];
					this.tData[iParent] = this.tData[num];
					this.tKeys[iParent] = this.tKeys[num];
					this.tData[num] = t;
					this.tKeys[num] = num1;
					iParent = num;
					num = iParent * 2 + 1;
				}
			}
		}

		private void SiftUp(int iChild)
		{
			int num = (iChild - 1) / 2;
			while (true)
			{
				if ((iChild == 0 ? true : this.tKeys[iChild] >= this.tKeys[num]))
				{
					break;
				}
				T t = this.tData[num];
				double num1 = this.tKeys[num];
				this.tData[num] = this.tData[iChild];
				this.tKeys[num] = this.tKeys[iChild];
				this.tData[iChild] = t;
				this.tKeys[iChild] = num1;
				iChild = num;
				num = (iChild - 1) / 2;
			}
		}
	}
}