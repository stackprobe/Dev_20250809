using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte.Tools
{
	public static class StringTools
	{
		public static int IndexOfWord(string str, int fromIndex, string word, bool ignoreCase, Predicate<char> isCharOfWord)
		{
			for (int index = fromIndex; index + word.Length <= str.Length; index++)
			{
				if (NComp(str, index, word, 0, word.Length, ignoreCase) == 0)
				{
					if (
						0 < index &&
						isCharOfWord(str[index - 1])
						)
						continue;

					if (
						index + word.Length < str.Length &&
						isCharOfWord(str[index + word.Length])
						)
						continue;

					return index;
				}
			}
			return -1;
		}

		public static int NComp(string str1, int offset1, string str2, int offset2, int length, bool ignoreCase)
		{
			for (int index = 0; index < length; index++)
			{
				char chr1 = str1[offset1 + index];
				char chr2 = str2[offset2 + index];

				if (ignoreCase)
				{
					chr1 = char.ToLower(chr1);
					chr2 = char.ToLower(chr2);
				}
				int ret = (int)chr1 - (int)chr2;

				if (ret != 0)
					return ret;
			}
			return 0;
		}

		/// <summary>
		/// 複数の検索・置換パターンを用いて文字列を一括置換します。
		/// </summary>
		/// <param name="text">置換対象の文字列</param>
		/// <param name="ignoreCase">大文字小文字を区別しないか (True：大文字小文字を区別しない　False：大文字小文字を区別する)</param>
		/// <param name="patterns">検索文字列と置換文字列を交互に並べた配列。要素数は偶数でなければならず、検索文字列には空文字列を指定できません。先勝一致</param>
		/// <returns>指定されたパターンをすべて適用した後の新しい文字列</returns>
		public static string ReplaceAll(string text, bool ignoreCase, params string[] patterns)
		{
			if (text == null)
				throw new Exception("Bad text");

			if (
				patterns == null ||
				patterns.Length == 0 ||
				patterns.Length % 2 != 0 ||
				patterns.Any(pattern => pattern == null) ||
				patterns.Where((pattern, index) => index % 2 == 0).Any(pattern => pattern == "") // ? 検索文字列に空文字列がある。
				)
				throw new Exception("Bad patterns");

			StringBuilder buff = new StringBuilder();

			for (int index = 0; index < text.Length;)
			{
				for (int p = 0; ; p += 2)
				{
					if (patterns.Length <= p)
					{
						buff.Append(text[index]);
						index++;
						break;
					}

					if (
						index + patterns[p].Length <= text.Length &&
						NComp(text, index, patterns[p], 0, patterns[p].Length, ignoreCase) == 0
						)
					{
						buff.Append(patterns[p + 1]);
						index += patterns[p].Length;
						break;
					}
				}
			}
			return buff.ToString();
		}

		public static string[][] ParseIslandLines(IList<string> lines, Predicate<string> isSingleTag)
		{
			for (int index = 0; index < lines.Count; index++)
			{
				if (isSingleTag(lines[index]))
				{
					return new string[][]
					{
						lines.Take(index).ToArray(),
						lines.Skip(index).Take(1).ToArray(),
						lines.Skip(index + 1).ToArray(),
					};
				}
			}
			return null;
		}

		public static string[][] ParseEnclosedLines(IList<string> lines, Predicate<string> isOpenTag, Predicate<string> isCloseTag)
		{
			string[][] starts = ParseIslandLines(lines, isOpenTag);

			if (starts == null)
				return null;

			string[][] ends = ParseIslandLines(starts[2], isCloseTag);

			if (ends == null)
				return null;

			return new string[][]
			{
				starts[0],
				starts[1],
				ends[0],
				ends[1],
				ends[2],
			};
		}

		public static string ThousandComma(long value)
		{
			bool negative = false;

			if (value < 0L)
			{
				value *= -1L;
				negative = true;
			}
			StringBuilder buff = new StringBuilder();
			string str = value.ToString();

			for (int index = 0; index < str.Length; index++)
			{
				if (1 <= index && (str.Length - index) % 3 == 0)
					buff.Append(',');

				buff.Append(str[index]);
			}
			str = buff.ToString();

			if (negative)
				str = $"-{str}";

			return str;
		}
	}
}
