using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public class CsvFileWriter : IDisposable
	{
		public const char DELIMITER_COMMA = ','; // for .csv
		public const char DELIMITER_SPACE = ' '; // for .ssv
		public const char DELIMITER_TAB = '\t';  // for .tsv

		private char Delimiter;
		private StreamWriter Writer;

		public CsvFileWriter(string file, bool append = false)
			: this(file, append, SCommon.ENCODING_SJIS)
		{ }

		public CsvFileWriter(string file, bool append, Encoding encoding)
			: this(file, append, encoding, DELIMITER_COMMA)
		{ }

		public CsvFileWriter(string file, bool append, Encoding encoding, char delimiter)
		{
			this.Delimiter = delimiter;
			this.Writer = new StreamWriter(file, append, encoding);
		}

		/// <summary>
		/// 次に書き込むセルが行の最初のセルか
		/// </summary>
		private bool FirstCell = true;

		public void WriteCell(string cell)
		{
			if (this.FirstCell)
				this.FirstCell = false;
			else
				this.Writer.Write(this.Delimiter);

			if (
				cell.Contains('"') ||
				cell.Contains('\n') ||
				cell.Contains(this.Delimiter)
				)
			{
				this.Writer.Write('"');
				this.Writer.Write(cell.Replace("\"", "\"\""));
				this.Writer.Write('"');
			}
			else
			{
				this.Writer.Write(cell);
			}
		}

		public void EndRow()
		{
			this.Writer.Write('\n');
			this.FirstCell = true;
		}

		public void WriteCells(IList<string> cells)
		{
			foreach (string cell in cells)
			{
				this.WriteCell(cell);
			}
		}

		public void WriteRow(IList<string> row)
		{
			foreach (string cell in row)
			{
				this.WriteCell(cell);
			}
			this.EndRow();
		}

		public void WriteRows(IList<string[]> rows)
		{
			foreach (string[] row in rows)
			{
				this.WriteRow(row);
			}
		}

		public void Dispose()
		{
			if (this.Writer != null)
			{
				this.Writer.Dispose();
				this.Writer = null;
			}
		}

		public static void WriteRows(string file, string[][] rows)
		{
			WriteRows(file, false, rows);
		}

		public static void WriteRows(string file, bool append, string[][] rows)
		{
			WriteRows(file, append, SCommon.ENCODING_SJIS, rows);
		}

		public static void WriteRows(string file, bool append, Encoding encoding, string[][] rows)
		{
			WriteRows(file, append, encoding, DELIMITER_COMMA, rows);
		}

		public static void WriteRows(string file, bool append, Encoding encoding, char delimiter, string[][] rows)
		{
			using (CsvFileWriter writer = new CsvFileWriter(file, append, encoding, delimiter))
			{
				writer.WriteRows(rows);
			}
		}
	}
}
