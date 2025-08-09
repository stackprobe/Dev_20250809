using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte.Tools
{
	public static class Adler32
	{
		private const uint MODULO = 65521;

		public static uint ComputeHash(byte[] data)
		{
			uint a = 1;
			uint b = 0;

			foreach (byte c in data)
			{
				a = (a + c) % MODULO;
				b = (b + a) % MODULO;
			}
			return a | (b << 16);
		}
	}
}
