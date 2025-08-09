using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;
using Charlotte.Commons;
using Charlotte.Tools;

namespace Charlotte.Tests
{
	public class Test0001
	{
		public void Test01()
		{
			for (int c = 0; c < 100; c++)
			{
				Console.WriteLine(IDIssuer.Issue());
			}
		}

		public void Test02()
		{
			string lastID = "";

			for (int c = 0; c < 3000100; c++)
			{
				string id = IDIssuer.Issue();

				if ((c + 10) % 1000000 < 20)
				{
					Console.WriteLine(id);
				}

				if (!Regex.IsMatch(id, "^[0-9]{25}$")) // 2bs
				{
					throw null;
				}
				if (!Regex.IsMatch(id, "^[1-9][0-9]{24}$"))
				{
					throw null;
				}
				if (SCommon.Comp(lastID, id) >= 0)
				{
					throw null;
				}
				lastID = id;
			}
		}

		public void Test03()
		{
			Console.WriteLine(SimpleDateTime.FromString("1889/1/1 00:00:00").ToSec() << 24); // 18 桁
			Console.WriteLine(SimpleDateTime.FromString("1890/1/1 00:00:00").ToSec() << 24); // 19 桁 (long の最大桁数)
			Console.WriteLine(SimpleDateTime.FromString("2024/1/1 00:00:00").ToSec() << 24); // 1071051832295424000
			Console.WriteLine(SimpleDateTime.FromString("2025/1/1 00:00:00").ToSec() << 24); // 1071582368130662400
		}
	}
}
