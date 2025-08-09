using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Charlotte.Commons;
using Charlotte.Tools;

namespace Charlotte.Tests
{
	public class Test0003
	{
		public void Test01()
		{
			Test01_a(Encoding.UTF8);
			Test01_a(SCommon.ENCODING_SJIS);

			Console.WriteLine("OK! (TEST-0003-01)");
		}

		private void Test01_a(Encoding encoding)
		{
			using (WorkingDir wd = new WorkingDir())
			{
				string file = wd.MakePath();

				File.WriteAllText(file, "テスト・てすと・TEST-ﾃｽﾄ", encoding);

				Encoding retEncoding = FileTools.GetEncoding(file);

				Console.WriteLine(encoding);
				Console.WriteLine(retEncoding);

				if (retEncoding != encoding)
					throw null; // BUG !!!
			}
		}
	}
}
