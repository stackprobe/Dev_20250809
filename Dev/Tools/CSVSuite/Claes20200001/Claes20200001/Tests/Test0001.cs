using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			//Test01_a(@"C:\Work\20250712_data\Complex-0001.xlsx");
			//Test01_a(@"C:\home\仕事関係\仕事関係\作業報告書\作業報告書_mitsuma_202504.xlsx");
			//Test01_a(@"C:\home\仕事関係\仕事関係\作業報告書\作業報告書_mitsuma_202505.xlsx");
			Test01_a(@"C:\home\仕事関係\仕事関係\作業報告書\作業報告書_mitsuma_202506.xlsx");
		}

		private void Test01_a(string excelFile)
		{
			ExcelTools.Sheet[] sheets = ExcelTools.LoadSheets(excelFile);

			foreach (ExcelTools.Sheet sheet in sheets)
				Console.WriteLine(sheet.Name);

			foreach (ExcelTools.Sheet sheet in sheets)
			{
				for (int y = 0; y < sheet.Height; y++)
				{
					for (int x = 0; x < sheet.Width; x++)
					{
						if (1 <= x)
							Console.Write(", ");

						Console.Write("[" + sheet[x, y] + "]");
					}
					Console.WriteLine();
				}
			}
		}

		public void Test02()
		{
			using (WorkingDir wd = new WorkingDir())
			{
				string file = wd.MakePath() + ".txt";

				File.WriteAllBytes(
					file,
					SCommon.EMPTY_BYTES
					);

				ExcelTools.LoadSheets(file);
			}

			SCommon.ToThrowPrint(() => ExcelTools.LoadSheets(null));
			SCommon.ToThrowPrint(() => ExcelTools.LoadSheets(@"C:\temp\****存在しないファイル****"));
			SCommon.ToThrowPrint(() =>
			{
				using (WorkingDir wd = new WorkingDir())
				{
					string file = wd.MakePath() + ".xlsx";

					File.WriteAllBytes(
						file,
						SCommon.Compress(Encoding.ASCII.GetBytes("AAAAABBBBBAAAAABBBBBAAAAABBBBBAAAAABBBBBAAAAABBBBB"))
						);

					ExcelTools.LoadSheets(file);
				}
			});
		}
	}
}
