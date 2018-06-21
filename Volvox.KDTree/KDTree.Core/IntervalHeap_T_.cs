using System;
using System.Runtime.CompilerServices;

namespace KDTree.Core
{
	public class IntervalHeap<T>
	{
		private const int DEFAULT_SIZE = 64;

		private T[] tData;

		private double[] tKeys;

		public int Capacity
		{
			get;
			private set;
		}

		public T Max
		{
			get
			{
				T t;
				if (this.Size == 0)
				{
					throw new Exception();
				}
				t = (this.Size != 1 ? this.tData[1] : this.tData[0]);
				return t;
			}
		}

		public double MaxKey
		{
			get
			{
				double num;
				if (this.Size == 0)
				{
					throw new Exception();
				}
				num = (this.Size != 1 ? this.tKeys[1] : this.tKeys[0]);
				return num;
			}
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

		public IntervalHeap() : this(64)
		{
		}

		public IntervalHeap(int capacity)
		{
			this.tData = new T[capacity];
			this.tKeys = new double[capacity];
			this.Capacity = capacity;
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
			this.Size = this.Size + 1;
			this.tData[this.Size - 1] = value;
			this.tKeys[this.Size - 1] = key;
			this.SiftInsertedValueUp();
		}

		public void RemoveMax()
		{
			if (this.Size == 0)
			{
				throw new Exception();
			}
			if (this.Size != 1)
			{
				this.Size = this.Size - 1;
				this.tData[1] = this.tData[this.Size];
				this.tKeys[1] = this.tKeys[this.Size];
				this.tData[this.Size] = default(T);
				this.SiftDownMax(1);
			}
			else
			{
				this.RemoveMin();
			}
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
			this.SiftDownMin(0);
		}

		public void ReplaceMax(double key, T value)
		{
			if (this.Size == 0)
			{
				throw new Exception();
			}
			if (this.Size != 1)
			{
				this.tData[1] = value;
				this.tKeys[1] = key;
				if (key < this.tKeys[0])
				{
					this.Swap(0, 1);
				}
				this.SiftDownMax(1);
			}
			else
			{
				this.ReplaceMin(key, value);
			}
		}

		public void ReplaceMin(double key, T value)
		{
			if (this.Size == 0)
			{
				throw new Exception();
			}
			this.tData[0] = value;
			this.tKeys[0] = key;
			if (this.Size > 1)
			{
				if (this.tKeys[1] < key)
				{
					this.Swap(0, 1);
				}
				this.SiftDownMin(0);
			}
		}

		private void SiftDownMax(int iParent)
		{
			int num = iParent * 2 + 1;
			while (num <= this.Size)
			{
				if (num != this.Size)
				{
					if (num + 2 == this.Size)
					{
						if (this.tKeys[num + 1] > this.tKeys[num])
						{
							if (this.tKeys[num + 1] > this.tKeys[iParent])
							{
								this.Swap(iParent, num + 1);
							}
							break;
						}
					}
					else if (num + 2 < this.Size)
					{
						if (this.tKeys[num + 2] > this.tKeys[num])
						{
							num = num + 2;
						}
					}
					if (this.tKeys[num] <= this.tKeys[iParent])
					{
						break;
					}
					else
					{
						this.Swap(iParent, num);
						if (this.tKeys[num - 1] > this.tKeys[num])
						{
							this.Swap(num, num - 1);
						}
						iParent = num;
						num = iParent * 2 + 1;
					}
				}
				else
				{
					if (this.tKeys[num - 1] > this.tKeys[iParent])
					{
						this.Swap(iParent, num - 1);
					}
					break;
				}
			}
		}

		private void SiftDownMin(int iParent)
		{
			int num = iParent * 2 + 2;
			while (num < this.Size)
			{
				if ((num + 2 >= this.Size ? false : this.tKeys[num + 2] < this.tKeys[num]))
				{
					num = num + 2;
				}
				if (this.tKeys[num] >= this.tKeys[iParent])
				{
					break;
				}
				else
				{
					this.Swap(iParent, num);
					if ((num + 1 >= this.Size ? false : this.tKeys[num + 1] < this.tKeys[num]))
					{
						this.Swap(num, num + 1);
					}
					iParent = num;
					num = iParent * 2 + 2;
				}
			}
		}

		private void SiftInsertedValueUp()
		{
			int size = this.Size - 1;
			if (size != 0)
			{
				if (size == 1)
				{
					if (this.tKeys[size] < this.tKeys[size - 1])
					{
						this.Swap(size, size - 1);
					}
				}
				else if (size % 2 != 1)
				{
					int num = size / 2 - 1 | 1;
					if (this.tKeys[size] > this.tKeys[num])
					{
						size = this.Swap(size, num);
						this.SiftUpMax(size);
					}
					else if (this.tKeys[size] < this.tKeys[num - 1])
					{
						size = this.Swap(size, num - 1);
						this.SiftUpMin(size);
					}
				}
				else
				{
					int num1 = size / 2 - 1 | 1;
					if (this.tKeys[size] < this.tKeys[size - 1])
					{
						size = this.Swap(size, size - 1);
						if (this.tKeys[size] < this.tKeys[num1 - 1])
						{
							size = this.Swap(size, num1 - 1);
							this.SiftUpMin(size);
						}
					}
					else if (this.tKeys[size] > this.tKeys[num1])
					{
						size = this.Swap(size, num1);
						this.SiftUpMax(size);
					}
				}
			}
		}

		private void SiftUpMax(int iChild)
		{
			int num = iChild / 2 - 1 | 1;
			while (true)
			{
				if ((num < 0 ? true : this.tKeys[iChild] <= this.tKeys[num]))
				{
					break;
				}
				this.Swap(iChild, num);
				iChild = num;
				num = iChild / 2 - 1 | 1;
			}
		}

		private void SiftUpMin(int iChild)
		{
			int num = iChild / 2 - 1 & -2;
			while (true)
			{
				if ((num < 0 ? true : this.tKeys[iChild] >= this.tKeys[num]))
				{
					break;
				}
				this.Swap(iChild, num);
				iChild = num;
				num = iChild / 2 - 1 & -2;
			}
		}

		private int Swap(int x, int y)
		{
			T t = this.tData[y];
			double num = this.tKeys[y];
			this.tData[y] = this.tData[x];
			this.tKeys[y] = this.tKeys[x];
			this.tData[x] = t;
			this.tKeys[x] = num;
			return y;
		}
	}
}