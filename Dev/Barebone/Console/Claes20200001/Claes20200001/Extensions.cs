using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Commons;

namespace Charlotte
{
	public static class Extensions
	{
		public static IEnumerable<T> DistinctOrderBy<T>(this IEnumerable<T> src, Comparison<T> comp)
		{
			List<T> srcList = src.ToList();
			List<T> dest = new List<T>();

			srcList.Sort(comp);

			if (1 <= srcList.Count)
			{
				dest.Add(srcList[0]);

				for (int index = 1; index < srcList.Count; index++)
					if (comp(srcList[index - 1], srcList[index]) != 0)
						dest.Add(srcList[index]);
			}
			return dest;
		}

		public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> src, Comparison<T> comp)
		{
			List<T> list = src.ToList();
			list.Sort(comp);
			return list;
		}

		public static bool EqualsIgnoreCase(this string a, string b)
		{
			return SCommon.EqualsIgnoreCase(a, b);
		}

		public static bool StartsWithIgnoreCase(this string a, string b)
		{
			return SCommon.StartsWithIgnoreCase(a, b);
		}

		public static bool EndsWithIgnoreCase(this string a, string b)
		{
			return SCommon.EndsWithIgnoreCase(a, b);
		}

		public static bool ContainsIgnoreCase(this string a, string b)
		{
			return SCommon.ContainsIgnoreCase(a, b);
		}

		public static int IndexOfIgnoreCase(this string a, string b)
		{
			return SCommon.IndexOfIgnoreCase(a, b);
		}

		public static int IndexOfIgnoreCase(this string a, char b)
		{
			return SCommon.IndexOfIgnoreCase(a, b);
		}

		public static IEnumerable<T> WithProgressBar<T>(this IEnumerable<T> src)
		{
			List<T> list = src.ToList();

			if (list.Count == 0)
			{
				Console.Write("[*****************************************************************************]");
			}
			else
			{
				int currBarLen = 0;

				Console.Write("[-----------------------------------------------------------------------------]\r[");

				for (int index = 0; index < list.Count; index++)
				{
					int barLen = (int)(((index + 1) * 77L) / list.Count);

					while (currBarLen < barLen)
					{
						Console.Write("*");
						currBarLen++;
					}
					yield return list[index];
				}
			}
			Console.WriteLine();
		}

		public static IEnumerable<T> Linearize<T>(this IEnumerable<T[]> src)
		{
			return SCommon.Linearize(src);
		}
	}
}
