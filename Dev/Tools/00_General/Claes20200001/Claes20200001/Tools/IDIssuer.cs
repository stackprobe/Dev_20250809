using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public static class IDIssuer
	{
		private static string PRIMARY_COUNTER_FILE = Path.Combine(ProcMain.SelfDir, "IDIssuer_PrimeCounter.txt");

		private static long IssuePrimaryCounter()
		{
			long counter;

			if (File.Exists(PRIMARY_COUNTER_FILE))
				counter = long.Parse(File.ReadAllText(PRIMARY_COUNTER_FILE, Encoding.ASCII).Trim()) + 1;
			else
				counter = SimpleDateTime.Now.ToSec() << 24; // カンスト日時：17422/1/26 12:18:07

			SCommon.CreateDir(SCommon.ToParentPath(PRIMARY_COUNTER_FILE));
			File.WriteAllText(PRIMARY_COUNTER_FILE, counter.ToString(), Encoding.ASCII);

			return counter;
		}

		private static IEnumerable<string> E_Issuer()
		{
			for (; ; )
			{
				long c1 = IssuePrimaryCounter();

				for (int c2 = 0; c2 < 1000000; c2++)
				{
					// c1 は 1890/1/1 00:00:00 の時点で既に 19 桁 (long の最大桁数) なので、常に ^[1-9][0-9]{18}$

					yield return $"{c1}{c2:D6}"; // ^[1-9][0-9]{24}$
				}
			}
		}

		private static Lazy<Func<string>> S_Issuer = new Lazy<Func<string>>(() => SCommon.Supplier(E_Issuer()));

		public static string Issue()
		{
			return S_Issuer.Value();
		}
	}
}
