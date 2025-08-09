using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	/// <summary>
	/// 実行ファイル(Portable Executable 形式のファイル)に関するツール
	/// </summary>
	public static class ExecutableFileTools
	{
		/// <summary>
		/// 指定された実行ファイルのビルド日時を得る。
		/// </summary>
		/// <param name="file">実行ファイル</param>
		/// <returns>ビルド日時</returns>
		public static DateTime GetBuiltDateTime(string file)
		{
			uint peTimeDateStamp = GetPETimeDateStamp(file);

			DateTime builtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
				.AddSeconds(peTimeDateStamp)
				.ToLocalTime();

			return builtDateTime;
		}

		/// <summary>
		/// 指定された実行ファイルのビルド日時をエポック秒(UTC)で得る。
		/// </summary>
		/// <param name="file">実行ファイル</param>
		/// <returns>ビルド日時のエポック秒(UTC)</returns>
		public static uint GetPETimeDateStamp(string file)
		{
			using (FileStream reader = new FileStream(file, FileMode.Open, FileAccess.Read))
			{
				if (ReadByte(reader) != 'M') throw null;
				if (ReadByte(reader) != 'Z') throw null;

				reader.Seek(0x3c, SeekOrigin.Begin);

				uint peHedPos = (uint)ReadByte(reader);
				peHedPos |= (uint)ReadByte(reader) << 8;
				peHedPos |= (uint)ReadByte(reader) << 16;
				peHedPos |= (uint)ReadByte(reader) << 24;

				reader.Seek(peHedPos, SeekOrigin.Begin);

				if (ReadByte(reader) != 'P') throw null;
				if (ReadByte(reader) != 'E') throw null;
				if (ReadByte(reader) != 0x00) throw null;
				if (ReadByte(reader) != 0x00) throw null;

				reader.Seek(0x04, SeekOrigin.Current);

				uint timeDateStamp = (uint)ReadByte(reader);
				timeDateStamp |= (uint)ReadByte(reader) << 8;
				timeDateStamp |= (uint)ReadByte(reader) << 16;
				timeDateStamp |= (uint)ReadByte(reader) << 24;

				return timeDateStamp;
			}
		}

		private static int ReadByte(FileStream reader)
		{
			int chr = reader.ReadByte();

			if (chr == -1) // ? EOF
				throw new Exception("Read EOF");

			return chr;
		}
	}
}
