using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte.Tools
{
	public static class FNV1aHash
	{
		private const uint FNV_OFFSET_BASIS = 2166136261;
		private const uint FNV_PRIME = 16777619;

		public static uint ComputeHash(byte[] data)
		{
			uint hash = FNV_OFFSET_BASIS;

			foreach (byte b in data)
			{
				hash ^= b;
				hash *= FNV_PRIME;
			}
			return hash;
		}
	}
}
