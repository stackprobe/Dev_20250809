using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public static class FileTools
	{
		public static string ReadTextFile(string file)
		{
			return File.ReadAllText(file, GetEncoding(file));
		}

		public static Encoding GetEncoding(string file)
		{
			return HasUTF8Bom(file) ? Encoding.UTF8 : SCommon.ENCODING_SJIS;
		}

		private static byte[] UTF8_BOM = new byte[]
		{
			0xEF,
			0xBB,
			0xBF,
		};

		private static bool HasUTF8Bom(string file)
		{
			using (FileStream reader = new FileStream(file, FileMode.Open, FileAccess.Read))
			{
				if (
					reader.ReadByte() == UTF8_BOM[0] &&
					reader.ReadByte() == UTF8_BOM[1] &&
					reader.ReadByte() == UTF8_BOM[2]
					)
					return true;
			}
			return false;
		}
	}
}
