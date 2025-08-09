using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte.Tools
{
	public class BitList
	{
		private const int UINT_SIZE = 4;
		private const int UINT_BITS = UINT_SIZE * 8;

		private List<uint> Inner = new List<uint>();

		public BitList()
		{ }

		public BitList(IEnumerable<bool> data)
		{
			int index = 0;

			foreach (bool value in data)
			{
				this[index++] = value;
			}
		}

		public bool this[int index]
		{
			get
			{
				if (index < 0)
					throw new ArgumentException("Bad index: " + index);

				if (this.Inner.Count <= index / UINT_BITS)
					return false;

				return (this.Inner[index / UINT_BITS] & (1u << (index % UINT_BITS))) != 0u;
			}

			set
			{
				if (index < 0)
					throw new ArgumentException("Bad index: " + index);

				while (this.Inner.Count <= index / UINT_BITS)
					this.Inner.Add(0u);

				if (value)
					this.Inner[index / UINT_BITS] |= 1u << (index % UINT_BITS);
				else
					this.Inner[index / UINT_BITS] &= ~(1u << (index % UINT_BITS));
			}
		}

		public IEnumerable<bool> Iterate()
		{
			this.Trim();

			for (int index = 0; index < this.Inner.Count; index++)
			{
				for (int bit = 0; bit < UINT_BITS; bit++)
				{
					yield return (this.Inner[index] & (1u << bit)) != 0u;
				}
			}
		}

		private void Trim()
		{
			while (1 <= this.Inner.Count && this.Inner[this.Inner.Count - 1] == 0u)
			{
				this.Inner.RemoveAt(this.Inner.Count - 1);
			}
		}
	}
}
