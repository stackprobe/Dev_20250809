using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public static class ExcelTools
	{
		#region EXCEL_TO_CSV_SCRIPT

		private static string EXCEL_TO_CSV_SCRIPT = @"

$inputExcelFile = ""<INPUT-EXCEL-FILE>""
$outputFolder   = ""<OUTPUT-FOLDER>""
$errorLogPath   = ""<ERROR-LOG-PATH>""
$successfulFile = ""<SUCCESSFUL-FILE>""

try {
	# エクセルの起動を試みる
	$excel = New-Object -ComObject Excel.Application
}
catch {
	""エクセルがインストールされていないか、使用できません。"" | Out-File -Encoding UTF8 -Append $errorLogPath
	exit 1
}

$excel.Visible = $false
$excel.DisplayAlerts = $false

try {
	# 入力ファイルを開く
	$workbook = $excel.Workbooks.Open($inputExcelFile)
}
catch {
	""指定されたエクセルファイルは破損しているか、対応していない形式です。"" | Out-File -Encoding UTF8 -Append $errorLogPath
	$excel.Quit()
	[System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
	exit 1
}

try {
	$sheetNames = @()

	for ($i = 1; $i -le $workbook.Sheets.Count; $i++) {
		$sheet = $workbook.Sheets.Item($i)
		$sheetName = $sheet.Name
		$sheetNames += $sheetName

		$csvFileName = ""{0:D4}.csv"" -f $i
		$csvPath = Join-Path $outputFolder $csvFileName

		$xlCSV = 62

		$sheet.SaveAs($csvPath, $xlCSV)
	}

	$workbook.Close($false)

	$sheetListPath = Join-Path $outputFolder ""sheet-names.txt""
	$sheetNames | Out-File -Encoding UTF8 $sheetListPath

	New-Item -ItemType File -Path $successfulFile -Force | Out-Null
}
catch {
	""エクセルファイルの読み込み中に不明なエラーが発生しました。"" | Out-File -Encoding UTF8 -Append $errorLogPath
	$workbook.Close($false)
}
finally {
	$excel.Quit()
	[System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
	[System.GC]::Collect()
	[System.GC]::WaitForPendingFinalizers()
}

";

		#endregion

		public class Sheet
		{
			public string Name;
			public string[][] Rows;

			public int Width;
			public int Height;

			public string this[int x, int y]
			{
				get
				{
					if (x < 0 || y < 0)
						throw new Exception("Bad coordinate");

					if (
						this.Rows.Length <= y ||
						this.Rows[y].Length <= x
						)
						return "";

					return this.Rows[y][x];
				}
			}
		}

		public static Sheet[] LoadSheets(string excelFile)
		{
			ProcMain.WriteLog("ExcelTools.LoadSheets-ST");

			if (string.IsNullOrEmpty(excelFile))
				throw new Exception("Bad excelFile");

			if (!File.Exists(excelFile))
				throw new Exception("no excelFile");

			// memo: .csv .txt などで空のファイルは有り得るので、空のファイルをここでエラーにしないこと。

			using (WorkingDir wd = new WorkingDir())
			{
				string inputExcelFile = wd.MakePath() + Path.GetExtension(excelFile);
				string outputDir = wd.MakePath();
				string errorLogFile = wd.MakePath();
				string successfulFile = wd.MakePath();
				string scriptFile = wd.MakePath() + ".ps1";

				File.Copy(excelFile, inputExcelFile);
				SCommon.CreateDir(outputDir);
				File.WriteAllBytes(errorLogFile, SCommon.EMPTY_BYTES);

				File.WriteAllText(
					scriptFile,
					EXCEL_TO_CSV_SCRIPT
						.Replace("<INPUT-EXCEL-FILE>", inputExcelFile)
						.Replace("<OUTPUT-FOLDER>", outputDir)
						.Replace("<ERROR-LOG-PATH>", errorLogFile)
						.Replace("<SUCCESSFUL-FILE>", successfulFile),
					Encoding.UTF8
					);

				SCommon.Batch(new string[]
				{
					string.Format(@"PowerShell.exe -ExecutionPolicy Bypass -File ""{0}""", scriptFile),
				});

				string errorLog = File.ReadAllText(errorLogFile, Encoding.UTF8).Trim();

				if (errorLog != "")
					throw new Exception(errorLog);

				if (!File.Exists(successfulFile))
					throw new Exception("パワーシェルがクラッシュしたか、起動できませんでした。");

				string[] sheetNames = File.ReadAllLines(Path.Combine(outputDir, "sheet-names.txt"), Encoding.UTF8)
					.Select(line => line.Trim())
					.Where(line => line != "")
					.ToArray();

				List<string[][]> rowsList = new List<string[][]>();

				for (int i = 1; ; i++)
				{
					string csvFile = Path.Combine(outputDir, string.Format("{0:D4}.csv", i));

					if (!File.Exists(csvFile))
						break;

					string[][] rows = CsvFileReader.ReadToEnd(csvFile);
					rows = LS_RowsFilter(rows);
					rowsList.Add(rows);
				}

				if (sheetNames.Length < 1)
					throw new Exception("Bad sheetNames.Length: " + sheetNames.Length);

				if (sheetNames.Length != rowsList.Count)
					throw new Exception("Bad sheetNames.Length: " + sheetNames.Length + ", " + rowsList.Count);

				Sheet[] sheets = Enumerable.Range(0, sheetNames.Length)
					.Select(i =>
					{
						string[][] rows = rowsList[i];

						int w = LS_GetWidth(rows);
						int h = rows.Length;

						return new Sheet()
						{
							Name = sheetNames[i],
							Rows = rows,
							Width = w,
							Height = h,
						};
					})
					.ToArray();

				ProcMain.WriteLog("ExcelTools.LoadSheets-ED");

				return sheets;
			}
		}

		private static string[][] LS_RowsFilter(string[][] rows)
		{
			for (int ri = 0; ri < rows.Length; ri++)
				rows[ri] = LS_RowFilter(rows[ri]);

			int rc = rows.Length;

			while (0 < rc && rows[rc - 1].Length == 0)
				rc--;

			if (rc < rows.Length)
				rows = rows.Take(rc).ToArray();

			return rows;
		}

		private static string[] LS_RowFilter(string[] row)
		{
			for (int ci = 0; ci < row.Length; ci++)
				row[ci] = row[ci].Trim();

			int cc = row.Length;

			while (0 < cc && row[cc - 1] == "")
				cc--;

			if (cc < row.Length)
				row = row.Take(cc).ToArray();

			return row;
		}

		private static int LS_GetWidth(string[][] rows)
		{
			return rows.Length == 0 ? 0 : rows.Max(row => row.Length);
		}
	}
}
