using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte.Tools
{
	public class XORShift
	{
		private ulong C;

		public XORShift(ulong seed = 1UL)
		{
			this.C = seed;
		}

		public ulong Next64()
		{
			this.C ^= this.C << 13;
			this.C ^= this.C >> 7;
			this.C ^= this.C << 17;
			return this.C;
		}

		public uint Next()
		{
			return (uint)(this.Next64() >> 32);
		}
	}
}
