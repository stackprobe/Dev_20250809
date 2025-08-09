using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Charlotte.Commons
{
	/// <summary>
	/// 共通機能・便利機能はできるだけこのクラスに集約する。
	/// </summary>
	public static class SCommon
	{
		private class P_AnonyDisposable : IDisposable
		{
			public Action Routine;

			public void Dispose()
			{
				if (this.Routine != null)
				{
					this.Routine();
					this.Routine = null;
				}
			}
		}

		public static IDisposable GetAnonyDisposable(Action routine)
		{
			return new P_AnonyDisposable()
			{
				Routine = routine,
			};
		}

		private class P_AnonyComparer<T> : IComparer<T>
		{
			public Comparison<T> Comp;

			public int Compare(T a, T b)
			{
				return this.Comp(a, b);
			}
		}

		public static IComparer<T> GetAnonyComparer<T>(Comparison<T> comp)
		{
			return new P_AnonyComparer<T>()
			{
				Comp = comp,
			};
		}

		// memo: @ 2024.8.7
		// string[] a, string[] b ⇒ Comp(a, b, a.Length - b.Length) のとき曖昧になるので、Comp -> MltComp とした。

		public static int MltComp<T>(T a, T b, params Comparison<T>[] arrComp)
		{
			foreach (Comparison<T> comp in arrComp)
			{
				int ret = comp(a, b);

				if (ret != 0)
					return ret;
			}
			return 0;
		}

		public static int Comp<T, K>(T a, T b, Func<T, K> conv, Comparison<K> comp)
		{
			return comp(conv(a), conv(b));
		}

		public static int Comp<T>(IList<T> a, IList<T> b, Comparison<T> comp)
		{
			int minlen = Math.Min(a.Count, b.Count);

			for (int index = 0; index < minlen; index++)
			{
				int ret = comp(a[index], b[index]);

				if (ret != 0)
					return ret;
			}
			return Comp(a.Count, b.Count);
		}

		public static int IndexOf<T>(IList<T> list, Predicate<T> match, int startIndex = 0)
		{
			for (int index = startIndex; index < list.Count; index++)
				if (match(list[index]))
					return index;

			return -1; // not found
		}

		public static void Swap<T>(IList<T> list, int a, int b)
		{
			T tmp = list[a];
			list[a] = list[b];
			list[b] = tmp;
		}

		public static void Swap<T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
		}

		public static byte[] EMPTY_BYTES = new byte[0];

		public static int Comp(byte a, byte b)
		{
			return (int)a - (int)b;
		}

		public static byte[] UIntToBytes(uint value)
		{
			byte[] dest = new byte[4];
			UIntToBytes(value, dest);
			return dest;
		}

		public static void UIntToBytes(uint value, byte[] dest, int index = 0)
		{
			// Little Endian

			dest[index + 0] = (byte)((value >> 0) & 0xff);
			dest[index + 1] = (byte)((value >> 8) & 0xff);
			dest[index + 2] = (byte)((value >> 16) & 0xff);
			dest[index + 3] = (byte)((value >> 24) & 0xff);
		}

		public static uint ToUInt(byte[] src, int index = 0)
		{
			// Little Endian

			return
				((uint)src[index + 0] << 0) |
				((uint)src[index + 1] << 8) |
				((uint)src[index + 2] << 16) |
				((uint)src[index + 3] << 24);
		}

		public static byte[] ULongToBytes(ulong value)
		{
			byte[] dest = new byte[8];
			ULongToBytes(value, dest);
			return dest;
		}

		public static void ULongToBytes(ulong value, byte[] dest, int index = 0)
		{
			// Little Endian

			dest[index + 0] = (byte)((value >> 0) & 0xff);
			dest[index + 1] = (byte)((value >> 8) & 0xff);
			dest[index + 2] = (byte)((value >> 16) & 0xff);
			dest[index + 3] = (byte)((value >> 24) & 0xff);
			dest[index + 4] = (byte)((value >> 32) & 0xff);
			dest[index + 5] = (byte)((value >> 40) & 0xff);
			dest[index + 6] = (byte)((value >> 48) & 0xff);
			dest[index + 7] = (byte)((value >> 56) & 0xff);
		}

		public static ulong ToULong(byte[] src, int index = 0)
		{
			// Little Endian

			return
				((ulong)src[index + 0] << 0) |
				((ulong)src[index + 1] << 8) |
				((ulong)src[index + 2] << 16) |
				((ulong)src[index + 3] << 24) |
				((ulong)src[index + 4] << 32) |
				((ulong)src[index + 5] << 40) |
				((ulong)src[index + 6] << 48) |
				((ulong)src[index + 7] << 56);
		}

		/// <summary>
		/// バイト列を連結する。
		/// 例：{ BYTE_ARR_1, BYTE_ARR_2, BYTE_ARR_3 } -> BYTE_ARR_1 + BYTE_ARR_2 + BYTE_ARR_3
		/// </summary>
		/// <param name="src">バイト列の引数配列</param>
		/// <returns>連結したバイト列</returns>
		public static byte[] Join(IList<byte[]> src)
		{
			int offset = 0;

			foreach (byte[] block in src)
				offset += block.Length;

			byte[] dest = new byte[offset];
			offset = 0;

			foreach (byte[] block in src)
			{
				Array.Copy(block, 0, dest, offset, block.Length);
				offset += block.Length;
			}
			return dest;
		}

		/// <summary>
		/// バイト列を再分割可能なように連結する。
		/// 再分割するには SCommon.Split を使用すること。
		/// 例：{ BYTE_ARR_1, BYTE_ARR_2, BYTE_ARR_3 } -> SIZE(BYTE_ARR_1) + BYTE_ARR_1 + SIZE(BYTE_ARR_2) + BYTE_ARR_2 + SIZE(BYTE_ARR_3) + BYTE_ARR_3
		/// SIZE(b) は SCommon.ToBytes(b.Length) である。
		/// </summary>
		/// <param name="src">バイト列の引数配列</param>
		/// <returns>連結したバイト列</returns>
		public static byte[] SplittableJoin(IList<byte[]> src)
		{
			int offset = 0;

			foreach (byte[] block in src)
				offset += 4 + block.Length;

			byte[] dest = new byte[offset];
			offset = 0;

			foreach (byte[] block in src)
			{
				Array.Copy(UIntToBytes((uint)block.Length), 0, dest, offset, 4);
				offset += 4;
				Array.Copy(block, 0, dest, offset, block.Length);
				offset += block.Length;
			}
			return dest;
		}

		/// <summary>
		/// バイト列を再分割する。
		/// </summary>
		/// <param name="src">連結したバイト列</param>
		/// <returns>再分割したバイト列の配列</returns>
		public static byte[][] Split(byte[] src)
		{
			List<byte[]> dest = new List<byte[]>();

			for (int offset = 0; offset < src.Length;)
			{
				int size = (int)ToUInt(src, offset);
				offset += 4;
				dest.Add(P_GetBytesRange(src, offset, size));
				offset += size;
			}
			return dest.ToArray();
		}

		private static byte[] P_GetBytesRange(byte[] src, int offset, int size)
		{
			byte[] dest = new byte[size];
			Array.Copy(src, offset, dest, 0, size);
			return dest;
		}

		public static T[] GetPart<T>(T[] src, int offset)
		{
			return GetPart(src, offset, src.Length - offset);
		}

		public static T[] GetPart<T>(T[] src, int offset, int size)
		{
			if (
				src == null ||
				offset < 0 || src.Length < offset ||
				size < 0 || src.Length - offset < size
				)
				throw new Exception("Bad params");

			T[] dest = new T[size];
			Array.Copy(src, offset, dest, 0, size);
			return dest;
		}

		public class Serializer
		{
			private static Lazy<Serializer> _i = new Lazy<Serializer>(() => new Serializer());

			public static Serializer I
			{
				get
				{
					return _i.Value;
				}
			}

			private Regex RegexSerializedString = new Regex("^[0-9][A-Za-z0-9+/]*[0-9]$");

			/// <summary>
			/// 文字列のリストを連結してシリアライズします。
			/// シリアライズされた文字列：
			/// -- 常に空文字列ではない。
			/// -- 書式 == ^[0-9][A-Za-z0-9+/]*[0-9]$
			/// </summary>
			/// <param name="plainStrings">任意の文字列のリスト</param>
			/// <returns>シリアライズされた文字列</returns>
			public string Join(IList<string> plainStrings)
			{
				if (
					plainStrings == null ||
					plainStrings.Any(plainString => plainString == null)
					)
					throw new Exception("不正な入力文字列リスト");

				return Encode(SCommon.Base64.I.EncodeNoPadding(SCommon.Compress(
					SCommon.SplittableJoin(plainStrings.Select(plainString => Encoding.UTF8.GetBytes(plainString)).ToArray())
					)));
			}

			/// <summary>
			/// シリアライズされた文字列から文字列のリストを復元します。
			/// </summary>
			/// <param name="serializedString">シリアライズされた文字列</param>
			/// <returns>元の文字列リスト</returns>
			public string[] Split(string serializedString)
			{
				if (
					serializedString == null ||
					!RegexSerializedString.IsMatch(serializedString)
					)
					throw new Exception("シリアライズされた文字列は破損しています。");

				return SCommon.Split(SCommon.Decompress(SCommon.Base64.I.Decode(Decode(serializedString))))
					.Select(decodedBlock => Encoding.UTF8.GetString(decodedBlock))
					.ToArray();
			}

			private string Encode(string str)
			{
				int stAn = 0;
				int edAn = 0;

				// str(gz&Base64)の先頭部の想定：
				// -- ID(1f8b) + CM=DEFLATE(08) ==> H4sI
				// -- + FLG(00) + TIME-STAMP-1(0000) ==> AAAA
				// -- + TIME-STAMP-2(0000) + XFL(04) ==> AAAE
				//                               ~~
				//                               Javaでやると00になる。
				//                               04 = compressor used fastest algorithm
				// gz仕様：
				// -- https://www.ietf.org/rfc/rfc1952.txt

				if (str.StartsWith("H4sIA")) // 先頭を圧縮
				{
					for (stAn = 1; stAn < 9; stAn++)
					{
						int i = 4 + stAn;

						if (str.Length <= i || str[i] != 'A')
							break;
					}
					str = str.Substring(4 + stAn);
				}

				// 終端を圧縮
				{
					for (; edAn < 9; edAn++)
					{
						int i = str.Length - 1 - edAn;

						if (i < 0 || str[i] != 'A')
							break;
					}
					str = str.Substring(0, str.Length - edAn);
				}

				return stAn + str + edAn;
			}

			private string Decode(string str)
			{
				return
					(str[0] == '0' ? "" : "H4sI") +
					new string('A', str[0] - '0') +
					str.Substring(1, str.Length - 2) +
					new string('A', str[str.Length - 1] - '0');
			}

			/// <summary>
			/// 文字列のリストを連結してシリアライズします。
			/// </summary>
			/// <param name="plainStrings">任意の文字列のリスト</param>
			/// <returns>シリアライズされたバイト列</returns>
			public byte[] BinJoin(IList<string> plainStrings)
			{
				if (
					plainStrings == null ||
					plainStrings.Any(plainString => plainString == null)
					)
					throw new Exception("不正な入力文字列リスト");

				return Encode(SCommon.Compress(
					SCommon.SplittableJoin(plainStrings.Select(plainString => Encoding.UTF8.GetBytes(plainString)).ToArray())
					));
			}

			/// <summary>
			/// シリアライズされたバイト列から文字列のリストを復元します。
			/// </summary>
			/// <param name="serializedString">シリアライズされた文字列</param>
			/// <returns>元の文字列リスト</returns>
			public string[] Split(byte[] serializedBytes)
			{
				if (
					serializedBytes == null ||
					serializedBytes.Length < 1
					)
					throw new Exception("シリアライズされた文字列は破損しています。");

				return SCommon.Split(SCommon.Decompress(Decode(serializedBytes)))
					.Select(decodedBlock => Encoding.UTF8.GetString(decodedBlock))
					.ToArray();
			}

			private byte[] Encode(byte[] data)
			{
				int stZn = 0;
				int edZn = 0;

				// data(gz)の先頭部の想定：
				// -- ID(1f8b) + CM=DEFLATE(08)
				// -- + FLG(00) + TIME-STAMP-1(0000)
				// -- + TIME-STAMP-2(0000) + XFL(04)
				//                               ~~
				//                               Javaでやると00になる。
				//                               04 = compressor used fastest algorithm
				// gz仕様：
				// -- https://www.ietf.org/rfc/rfc1952.txt

				// 先頭を圧縮
				if (
					data.Length >= 4 &&
					data[0] == 0x1f &&
					data[1] == 0x8b &&
					data[2] == 0x08 &&
					data[3] == 0x00
					)
				{
					for (stZn = 1; stZn < 0x0f; stZn++)
					{
						int i = 3 + stZn;

						if (data.Length <= i || data[i] != 0x00)
							break;
					}
					data = SCommon.GetPart(data, 3 + stZn);
				}

				// 終端を圧縮
				{
					for (; edZn < 0x0f; edZn++)
					{
						int i = data.Length - 1 - edZn;

						if (i < 0 || data[i] != 0x00)
							break;
					}
					data = SCommon.GetPart(data, 0, data.Length - edZn);
				}

				return SCommon.Join(new byte[][]
				{
					new byte[] { (byte)((stZn << 4) | edZn) },
					data,
				});
			}

			private byte[] Decode(byte[] data)
			{
				int stZn = (data[0] & 0xf0) >> 4;
				int edZn = data[0] & 0x0f;

				return SCommon.Join(new byte[][]
				{
					stZn == 0 ?
						SCommon.EMPTY_BYTES :
						new byte[] { 0x1f, 0x8b, 0x08 }.Concat(Enumerable.Repeat((byte)0x00, stZn)).ToArray(),
					SCommon.GetPart(data, 1),
					Enumerable.Repeat((byte)0x00, edZn).ToArray(),
				});
			}
		}

		public static Dictionary<string, V> CreateDictionary<V>()
		{
			return new Dictionary<string, V>(new EqualityComparerString());
		}

		public static Dictionary<string, V> CreateDictionaryIgnoreCase<V>()
		{
			return new Dictionary<string, V>(new EqualityComparerStringIgnoreCase());
		}

		public static HashSet<string> CreateSet()
		{
			return new HashSet<string>(new EqualityComparerString());
		}

		public static HashSet<string> CreateSetIgnoreCase()
		{
			return new HashSet<string>(new EqualityComparerStringIgnoreCase());
		}

		private class EqualityComparerString : IEqualityComparer<string>
		{
			public bool Equals(string a, string b)
			{
				return a == b;
			}

			public int GetHashCode(string a)
			{
				return a.GetHashCode();
			}
		}

		private class EqualityComparerStringIgnoreCase : IEqualityComparer<string>
		{
			public bool Equals(string a, string b)
			{
				return a.ToLower() == b.ToLower();
			}

			public int GetHashCode(string a)
			{
				return a.ToLower().GetHashCode();
			}
		}

		/// <summary>
		/// とても小さい正数として慣習的に決めた値
		/// ・doubleの許容誤差として
		/// </summary>
		public const double MICRO = 1.0 / IMAX;

		private static void CheckNaN(double value)
		{
			if (double.IsNaN(value))
				throw new Exception("NaN");

			if (double.IsInfinity(value))
				throw new Exception("Infinity");
		}

		public static double ToRange(double value, double minval, double maxval)
		{
			CheckNaN(value);

			return Math.Max(minval, Math.Min(maxval, value));
		}

		public static bool IsRange(double value, double minval, double maxval)
		{
			CheckNaN(value);

			return minval <= value && value <= maxval;
		}

		public static int ToInt(double value)
		{
			CheckNaN(value);

			if (value < 0.0)
				return (int)(value - 0.5);
			else
				return (int)(value + 0.5);
		}

		public static long ToLong(double value)
		{
			CheckNaN(value);

			if (value < 0.0)
				return (long)(value - 0.5);
			else
				return (long)(value + 0.5);
		}

		/// <summary>
		/// 2次元配列を1次元配列(列挙)に変換する。
		/// 例：{{ A, B, C }, { D, E, F }, { G, H, I }} -> { A, B, C, D, E, F, G, H, I }
		/// Linearize(new T[][] { AAA, BBB, CCC }) と AAA.Concat(BBB).Concat(CCC) は同じようなもの。
		/// </summary>
		/// <typeparam name="T">要素の型</typeparam>
		/// <param name="src">列挙の列挙(2次元配列)</param>
		/// <returns>列挙(1次元配列)</returns>
		public static IEnumerable<T> Linearize<T>(IEnumerable<T[]> src)
		{
			List<T[]> srcTable = src.ToList();

			foreach (T[] srcPart in srcTable)
				foreach (T element in srcPart)
					yield return element;
		}

		/// <summary>
		/// 生成器をくり返し実行して要素を列挙する。
		/// Java の Stream.generate(generator).limit(count) と同じ。
		/// 例：Generate(3, generator); -> { generator(), generator(), generator() }
		/// 要素の個数に -1 を指定すると無限に要素を列挙する。
		/// この場合は Java の Stream.generate(generator) と同じ。
		/// 例：Generate(-1, generator); -> { generator(), generator(), generator(), ... }
		/// </summary>
		/// <typeparam name="T">要素の型</typeparam>
		/// <param name="count">要素の個数(0～), -1 == 無限</param>
		/// <param name="generator">要素の生成器</param>
		/// <returns>列挙</returns>
		public static IEnumerable<T> Generate<T>(int count, Func<T> generator)
		{
			while (count == -1 || 0 <= --count)
			{
				yield return generator();
			}
		}

		/// <summary>
		/// 列挙を逐次取得メソッドでラップします。
		/// 例：{ A, B, C } -> 呼び出し毎に右の順で戻り値を返す { A, B, C, default(T), default(T), default(T), ... }
		/// </summary>
		/// <typeparam name="T">要素の型</typeparam>
		/// <param name="src">列挙</param>
		/// <returns>逐次取得メソッド</returns>
		public static Func<T> Supplier<T>(IEnumerable<T> src)
		{
			IEnumerator<T> reader = src.GetEnumerator();

			return () =>
			{
				if (reader != null)
				{
					if (reader.MoveNext())
						return reader.Current;

					reader.Dispose();
					reader = null;
				}
				return default(T);
			};
		}

		// memo: リストの長さを変更するので、配列を指定できないように IList<T> ではなく List<T> とする。
		//
		public static T DesertElement<T>(List<T> list, int index)
		{
			T ret = list[index];
			list.RemoveAt(index);
			return ret;
		}

		public static T UnaddElement<T>(List<T> list)
		{
			return DesertElement(list, list.Count - 1);
		}

		public static T FastDesertElement<T>(List<T> list, int index)
		{
			T ret;

			if (index == list.Count - 1) // ? 終端の要素
			{
				ret = UnaddElement(list);
			}
			else
			{
				ret = list[index];
				list[index] = UnaddElement(list);
			}
			return ret;
		}

		public static T RefElement<T>(IList<T> list, int index, T defval)
		{
			if (index < list.Count)
			{
				return list[index];
			}
			else
			{
				return defval;
			}
		}

		private const int IO_TRY_MAX = 10;

		public static void DeletePath(string path)
		{
			if (path == null)
				throw new Exception("削除しようとしたパスは定義されていません。");

			if (path == "")
				throw new Exception("削除しようとしたパスは空文字列です。");

			// memo: 空白だけのファイル・フォルダ(例："\u3000")も削除できるので path.Trim() == "" はチェックしない。

			if (File.Exists(path))
			{
				for (int trycnt = 1; ; trycnt++)
				{
					try
					{
						File.Delete(path);
					}
					catch (Exception ex)
					{
						if (IO_TRY_MAX <= trycnt)
							throw new Exception("ファイルの削除に失敗しました。パス：" + path, ex);
					}
					if (!File.Exists(path))
						break;

					if (IO_TRY_MAX <= trycnt)
						throw new Exception("ファイルの削除に失敗しました。パス：" + path);

					ProcMain.WriteLog("ファイルの削除をリトライします。パス：" + path);
					Thread.Sleep(trycnt * 100);
				}
			}
			else if (Directory.Exists(path))
			{
				for (int trycnt = 1; ; trycnt++)
				{
					try
					{
						Directory.Delete(path, true);
					}
					catch (Exception ex)
					{
						if (IO_TRY_MAX <= trycnt)
							throw new Exception("ディレクトリの削除に失敗しました。パス：" + path, ex);
					}
					if (!Directory.Exists(path))
						break;

					if (IO_TRY_MAX <= trycnt)
						throw new Exception("ディレクトリの削除に失敗しました。パス：" + path);

					ProcMain.WriteLog("ディレクトリの削除をリトライします。パス：" + path);
					Thread.Sleep(trycnt * 100);
				}
			}
		}

		public static void CreateDir(string dir)
		{
			if (dir == null)
				throw new Exception("作成しようとしたディレクトリは定義されていません。");

			if (dir == "")
				throw new Exception("作成しようとしたディレクトリは空文字列です。");

			// memo: 空白だけのフォルダ(例："\u3000")も作成できるので dir.Trim() == "" はチェックしない。

			for (int trycnt = 1; ; trycnt++)
			{
				try
				{
					Directory.CreateDirectory(dir); // ディレクトリが存在するときは何もしない。
				}
				catch (Exception ex)
				{
					if (IO_TRY_MAX <= trycnt)
						throw new Exception("ディレクトリを作成できません。パス：" + dir, ex);
				}
				if (Directory.Exists(dir))
					break;

				if (IO_TRY_MAX <= trycnt)
					throw new Exception("ディレクトリを作成できません。パス：" + dir);

				ProcMain.WriteLog("ディレクトリの作成をリトライします。パス：" + dir);
				Thread.Sleep(trycnt * 100);
			}
		}

		public static void DeleteAndCreateDir(string dir)
		{
			DeletePath(dir);
			CreateDir(dir);
		}

		public static void CopyDir(string rDir, string wDir)
		{
			if (string.IsNullOrEmpty(rDir))
				throw new Exception("不正なコピー元ディレクトリ");

			if (!Directory.Exists(rDir))
				throw new Exception("コピー元ディレクトリが存在しません。");

			if (string.IsNullOrEmpty(wDir))
				throw new Exception("不正なコピー先ディレクトリ");

			if (File.Exists(wDir))
				throw new Exception("コピー先ディレクトリと同名のファイルが存在します。");

			List<string[]> dirPairs = new List<string[]>();
			List<string[]> filePairs = new List<string[]>();

			dirPairs.Add(new string[] { rDir, wDir });

			for (int index = 0; index < dirPairs.Count; index++)
			{
				rDir = dirPairs[index][0];
				wDir = dirPairs[index][1];

				foreach (string dir in Directory.GetDirectories(rDir))
					dirPairs.Add(new string[] { dir, Path.Combine(wDir, Path.GetFileName(dir)) });

				foreach (string file in Directory.GetFiles(rDir))
					filePairs.Add(new string[] { file, Path.Combine(wDir, Path.GetFileName(file)) });

				dirPairs[index][0] = null;
			}
			foreach (string[] dirPair in dirPairs)
			{
				wDir = dirPair[1];

				SCommon.CreateDir(wDir);
			}
			foreach (string[] filePair in filePairs)
			{
				string rFile = filePair[0];
				string wFile = filePair[1];

				File.Copy(rFile, wFile);
			}
		}

		public static void CopyPath(string rPath, string wPath)
		{
			if (Directory.Exists(rPath))
			{
				SCommon.CopyDir(rPath, wPath);
			}
			else if (File.Exists(rPath))
			{
				File.Copy(rPath, wPath);
			}
			else
			{
				throw new Exception("コピー元パスが存在しません。");
			}
		}

		public static void MovePath(string rPath, string wPath)
		{
			if (Directory.Exists(rPath))
			{
				Directory.Move(rPath, wPath);
			}
			else if (File.Exists(rPath))
			{
				File.Move(rPath, wPath);
			}
			else
			{
				throw new Exception("移動元パスが存在しません。");
			}
		}

		public static string ChangeRoot(string path, string oldRoot, string rootNew)
		{
			return PutYen(rootNew) + ChangeRoot(path, oldRoot);
		}

		public static string ChangeRoot(string path, string oldRoot)
		{
			oldRoot = PutYen(oldRoot);

			if (!path.StartsWithIgnoreCase(oldRoot))
				throw new Exception($"パスの配下ではありません。{oldRoot} -> {path}");

			return path.Substring(oldRoot.Length);
		}

		private static string PutYen(string path)
		{
			return PutStrIfNotEnd(path, "\\");
		}

		private static string PutStrIfNotEnd(string str, string endPtn)
		{
			if (!str.EndsWith(endPtn))
				str += endPtn;

			return str;
		}

		/// <summary>
		/// 厳しいフルパス化 (慣習的実装)
		/// </summary>
		/// <param name="path">パス</param>
		/// <returns>フルパス</returns>
		public static string MakeFullPath(string path)
		{
			if (path == null)
				throw new Exception("パスが定義されていません。(null)");

			if (path == "")
				throw new Exception("パスが定義されていません。(空文字列)");

			if (path.Replace("\u0020", "") == "")
				throw new Exception("パスが定義されていません。(空白のみ)");

			if (path.Any(chr => chr < '\u0020'))
				throw new Exception("パスに制御コードが含まれています。");

			path = Path.GetFullPath(path);

			if (path.Contains('/')) // Path.GetFullPath が '/' を '\\' に置換するはず。
				throw null;

			if (path.StartsWith("\\\\"))
				throw new Exception("ネットワークパスまたはデバイス名は使用できません。");

			if (path.Substring(1, 2) != ":\\") // ネットワークパスでないならローカルパスのはず。
				throw null;

			path = PutYen(path) + ".";
			path = Path.GetFullPath(path);

			return path;
		}

		/// <summary>
		/// ゆるいフルパス化 (慣習的実装)
		/// </summary>
		/// <param name="path">パス</param>
		/// <returns>フルパス</returns>
		public static string ToFullPath(string path)
		{
			if (path == null)
				throw new Exception("パスが定義されていません。(null)");

			if (path == "")
				throw new Exception("パスが定義されていません。(空文字列)");

			path = Path.GetFullPath(path);
			path = PutYen(path) + ".";
			path = Path.GetFullPath(path);

			return path;
		}

		public static string ToParentPath(string path)
		{
			path = Path.GetDirectoryName(path);

			// path -> Path.GetDirectoryName(path)
			// -----------------------------------
			// "C:\\ABC\\DEF" -> "C:\\ABC"
			// "C:\\ABC" -> "C:\\"
			// "C:\\" -> null
			// "" -> 例外
			// null -> null

			if (string.IsNullOrEmpty(path))
				throw new Exception("パスから親パスに変換できません。" + path);

			return path;
		}

		#region ToFairLocalPath, ToFairRelPath

		/// <summary>
		/// ローカル名に使用できない予約名のリストを返す。(慣習的実装)
		/// https://github.com/stackprobe/Factory/blob/master/Common/DataConv.c#L460-L491
		/// </summary>
		/// <returns>予約名リスト</returns>
		private static IEnumerable<string> GetReservedWordsForWindowsPath()
		{
			yield return "AUX";
			yield return "CON";
			yield return "NUL";
			yield return "PRN";

			for (int no = 1; no <= 9; no++)
			{
				yield return "COM" + no;
				yield return "LPT" + no;
			}

			// グレーゾーン
			{
				yield return "COM0";
				yield return "LPT0";
				yield return "CLOCK$";
				yield return "CONFIG$";
			}
		}

		public const int MY_PATH_MAX = 250;

		// memo: @ 2024.11.7
		// dirSize は SCommon.GetSJISBytes(dir).Length を想定していたが、長さの判定ガバガバなので dir.Length とかでも良しとする。

		/// <summary>
		/// 歴としたローカル名に変換する。(慣習的実装)
		/// https://github.com/stackprobe/Factory/blob/master/Common/DataConv.c#L503-L552
		/// </summary>
		/// <param name="str">対象文字列(対象パス)</param>
		/// <param name="dirSize">対象パスが存在するディレクトリのフルパスのバイト数または文字数(1～), -1 == バイト数または文字数を考慮しない</param>
		/// <returns>ローカル名</returns>
		public static string ToFairLocalPath(string str, int dirSize)
		{
			const string CHRS_NG = "\"*/:<>?\\|";
			const string CHR_ALT = "_";

			byte[] bytes = SCommon.GetSJISBytes(str);

			if (dirSize != -1)
			{
				int maxLen = Math.Max(0, MY_PATH_MAX - dirSize);

				if (maxLen < bytes.Length)
					bytes = SCommon.GetPart(bytes, 0, maxLen);
			}
			str = SCommon.ToJString(bytes, true, false, false, true);

			string[] words = SCommon.Tokenize(str, ".");

			for (int index = 0; index < words.Length; index++)
			{
				string word = words[index];

				word = word.Trim();

				if (index == 0 && GetReservedWordsForWindowsPath().Any(resWord => resWord.EqualsIgnoreCase(word)))
				{
					word = CHR_ALT;
				}
				else if (word == "") // added @ 2023.11.1
				{
					word = CHR_ALT;
				}
				else
				{
					word = new string(word.Select(chr => CHRS_NG.IndexOf(chr) != -1 ? CHR_ALT[0] : chr).ToArray());
				}
				words[index] = word;
			}
			str = string.Join(".", words);

			if (str == "")
				str = CHR_ALT;

			if (str.EndsWith("."))
				str = str.Substring(0, str.Length - 1) + CHR_ALT;

			return str;
		}

		/// <summary>
		/// 歴とした相対パス名に変換する。(慣習的実装)
		/// https://github.com/stackprobe/Factory/blob/master/Common/DataConv.c#L571-L593
		/// </summary>
		/// <param name="path">対象文字列(対象パス)</param>
		/// <param name="dirSize">対象パスが存在するディレクトリのフルパスのバイト数または文字数(1～), -1 == バイト数または文字数を考慮しない</param>
		/// <returns>相対パス名</returns>
		public static string ToFairRelPath(string path, int dirSize)
		{
			string[] pTkns = SCommon.Tokenize(path, "\\/", false, true);

			if (pTkns.Length == 0)
				pTkns = new string[] { "_" };

			for (int index = 0; index < pTkns.Length; index++)
				pTkns[index] = ToFairLocalPath(pTkns[index], -1);

			path = string.Join("\\", pTkns);

			if (dirSize != -1)
			{
				int maxLen = Math.Max(0, MY_PATH_MAX - dirSize);
				byte[] bytes = SCommon.GetSJISBytes(path);

				if (maxLen < bytes.Length)
					path = ToFairLocalPath(path, dirSize);
			}
			return path;
		}

		#endregion

		public static bool IsFairLocalPath(string str, int dirSize)
		{
			return ToFairLocalPath(str, dirSize) == str;
		}

		public static bool IsFairRelPath(string path, int dirSize)
		{
			return ToFairRelPath(path, dirSize) == path;
		}

		public static bool IsFairFullPath(string path)
		{
			return IsAbsRootDir(path) || IsFairFullPathWithoutAbsRootDir(path);
		}

		public static bool IsAbsRootDir(string path)
		{
			return Regex.IsMatch(path, "^[A-Za-z]:\\\\$");
		}

		public static bool IsFairFullPathWithoutAbsRootDir(string path)
		{
			return Regex.IsMatch(path, "^[A-Za-z]:\\\\.+$") && IsFairRelPath(path.Substring(3), 3);
		}

		public static bool IsExistsPath(string path)
		{
			return Directory.Exists(path) || File.Exists(path);
		}

		public static string ToCreatablePath(string path)
		{
			string newPath = path;
			int n = 1;

			while (SCommon.IsExistsPath(newPath))
			{
				if (n % 100 == 0)
					ProcMain.WriteLog("パス名の衝突回避に時間が掛かっています。" + n);

				// memo:
				// ChangeExt("C:\\xxx\\zzz", "~123.aaa") -> "C:\\xxx\zzz~123.aaa"

				newPath = SCommon.ChangeExt(path, "~" + n + Path.GetExtension(path));
				n++;
			}
			return newPath;
		}

		// 注意：
		// ChangeExt("C:\\xxx\\.zzz", "") -> "C:\\xxx"

		public static string ChangeExt(string path, string ext)
		{
			return Path.Combine(SCommon.ToParentPath(path), Path.GetFileNameWithoutExtension(path) + ext);
		}

		#region ReadPart, WritePart

		public static int ReadPartInt(Stream reader)
		{
			return (int)ReadPartLong(reader);
		}

		public static long ReadPartLong(Stream reader)
		{
			return long.Parse(ReadPartString(reader));
		}

		public static string ReadPartString(Stream reader)
		{
			return Encoding.UTF8.GetString(ReadPart(reader));
		}

		public static byte[] ReadPart(Stream reader)
		{
			int size = (int)ToUInt(Read(reader, 4));

			if (size < 0)
				throw new Exception("Bad size: " + size);

			return Read(reader, size);
		}

		public static void WritePartInt(Stream writer, int value)
		{
			WritePartLong(writer, (long)value);
		}

		public static void WritePartLong(Stream writer, long value)
		{
			WritePartString(writer, value.ToString());
		}

		public static void WritePartString(Stream writer, string str)
		{
			WritePart(writer, Encoding.UTF8.GetBytes(str));
		}

		public static void WritePart(Stream writer, byte[] data)
		{
			Write(writer, UIntToBytes((uint)data.Length));
			Write(writer, data);
		}

		#endregion

		public static byte[] Read(Stream reader, int size)
		{
			byte[] buff = new byte[size];
			Read(reader, buff);
			return buff;
		}

		public static void Read(Stream reader, byte[] buff, int offset = 0)
		{
			Read(reader, buff, offset, buff.Length - offset);
		}

		public static void Read(Stream reader, byte[] buff, int offset, int count)
		{
			if (reader.Read(buff, offset, count) != count)
			{
				throw new Exception("データの途中でストリームの終端に到達しました。");
			}
		}

		public static void Write(Stream writer, byte[] buff, int offset = 0)
		{
			writer.Write(buff, offset, buff.Length - offset);
		}

		/// <summary>
		/// 行リストをテキストに変換します。
		/// </summary>
		/// <param name="lines">行リスト</param>
		/// <returns>テキスト</returns>
		public static string LinesToText(IList<string> lines)
		{
			return lines.Count == 0 ? "" : string.Join("\r\n", lines) + "\r\n";
		}

		/// <summary>
		/// テキストを行リストに変換します。
		/// </summary>
		/// <param name="text">テキスト</param>
		/// <returns>行リスト</returns>
		public static string[] TextToLines(string text)
		{
			text = text.Replace("\r", "");

			string[] lines = Tokenize(text, "\n");

			if (1 <= lines.Length && lines[lines.Length - 1] == "")
			{
				lines = lines.Take(lines.Length - 1).ToArray();
			}
			return lines;
		}

		/// <summary>
		/// ファイル読み込みハンドルなどバイトストリーム向けのコールバック
		/// </summary>
		/// <param name="buff">読み込んだデータの書き込み先</param>
		/// <param name="offset">書き込み開始位置</param>
		/// <param name="count">書き込みサイズ</param>
		/// <returns>実際に読み込んだサイズ(1～), ～0 == これ以上読み込めない</returns>
		public delegate int Read_d(byte[] buff, int offset, int count);

		/// <summary>
		/// ファイル書き込みハンドルなどバイトストリーム向けのコールバック
		/// </summary>
		/// <param name="buff">書き込むデータの読み込み先</param>
		/// <param name="offset">読み込み開始位置</param>
		/// <param name="count">読み込みサイズ(書き込みサイズ)</param>
		public delegate void Write_d(byte[] buff, int offset, int count);

		public static void ReadToEnd(Read_d reader, Write_d writer)
		{
			byte[] buff = new byte[2 * 1024 * 1024];

			for (; ; )
			{
				int readSize = reader(buff, 0, buff.Length);

				if (readSize <= 0)
					break;

				writer(buff, 0, readSize);
			}
		}

		/// <summary>
		/// 整数の上限として慣習的に決めた値
		/// ・10億
		/// ・10桁
		/// ・9桁の最大値+1
		/// ・2倍しても int.MaxValue より小さい
		/// </summary>
		public const int IMAX = 1000000000; // 10^9

		/// <summary>
		/// 64ビット整数の上限として慣習的に決めた値
		/// ・IMAX^2
		/// ・100京
		/// ・19桁
		/// ・18桁の最大値+1
		/// ・9倍しても long.MaxValue より小さい
		/// </summary>
		public const long IMAX64 = 1000000000000000000L; // 10^18

		public static int Comp(int a, int b)
		{
			if (a < b)
				return -1;

			if (a > b)
				return 1;

			return 0;
		}

		public static int Comp(long a, long b)
		{
			if (a < b)
				return -1;

			if (a > b)
				return 1;

			return 0;
		}

		public static int ToRange(int value, int minval, int maxval)
		{
			return Math.Max(minval, Math.Min(maxval, value));
		}

		public static long ToRange(long value, long minval, long maxval)
		{
			return Math.Max(minval, Math.Min(maxval, value));
		}

		public static bool IsRange(int value, int minval, int maxval)
		{
			return minval <= value && value <= maxval;
		}

		public static bool IsRange(long value, long minval, long maxval)
		{
			return minval <= value && value <= maxval;
		}

		public static int ToInt(string str, int minval, int maxval, int defval)
		{
			try
			{
				int value = int.Parse(str);

				if (value < minval || maxval < value)
					throw new Exception("Value out of range");

				return value;
			}
			catch
			{
				return defval;
			}
		}

		public static long ToLong(string str, long minval, long maxval, long defval)
		{
			try
			{
				long value = long.Parse(str);

				if (value < minval || maxval < value)
					throw new Exception("Value out of range");

				return value;
			}
			catch
			{
				return defval;
			}
		}

		public static double ToDouble(string str, double minval, double maxval, double defval)
		{
			try
			{
				double value = double.Parse(str);

				CheckNaN(value);

				if (value < minval || maxval < value)
					throw new Exception("Value out of range");

				return value;
			}
			catch
			{
				return defval;
			}
		}

		#region UTF8Conv

		public static class UTF8Conv
		{
			/// <summary>
			/// バイト列を UTF-8 の文字列に変換する。
			/// 以下に該当する文字は除去する。
			/// -- CR
			/// 以下に該当しない文字は '?' に置き換える。
			/// -- LF
			/// -- '\t'
			/// -- ASCII == '\u0020' + SCommon.ASCII
			/// -- 半角カナ文字 == SCommon.KANA
			/// -- 全角文字 == SCommon.GetJChars()
			/// </summary>
			/// <param name="src">バイト列</param>
			/// <returns>文字列</returns>
			public static string ToJString(byte[] src)
			{
				if (src == null)
					src = SCommon.EMPTY_BYTES;

				int startPos = 0;

				// ? BOM 有り
				if (
					3 <= src.Length &&
					src[0] == 0xEF &&
					src[1] == 0xBB &&
					src[2] == 0xBF
					)
					startPos = 3; // BOM をスキップする。

				src = SCommon.GetPart(src, startPos); // BOM の除去 & バイト列の複製
				src = EraseCRIfNeeded(src);
				ToFairUTF8Bytes(src);
				return Encoding.UTF8.GetString(src);
			}

			private static byte[] EraseCRIfNeeded(byte[] src)
			{
				const int CR = 0x0d;

				for (int i = 0; i < src.Length; i++)
				{
					if (src[i] == CR)
					{
						int crCount = 1;

						for (int r = i + 1; r < src.Length; r++)
							if (src[r] == CR)
								crCount++;

						byte[] dest = new byte[src.Length - crCount];

						for (int rw = 0; rw < i; rw++)
							dest[rw] = src[rw];

						int w = i;

						for (int r = i + 1; r < src.Length; r++)
							if (src[r] != CR)
								dest[w++] = src[r];

						if (w != dest.Length) // 2bs
							throw null; // never

						return dest;
					}
				}
				return src;
			}

			private static UInt64[] UTF8CodeMap = null;

			private static UInt64[] GetUTF8CodeMap()
			{
				UInt64[] codeMap = new UInt64[0x1000000 / 64];

				foreach (char chr in "\n\t\u0020" + SCommon.ASCII + SCommon.KANA + SCommon.GetJChars())
				{
					byte[] bytes = Encoding.UTF8.GetBytes(new string(new char[] { chr }));

					if (
						bytes.Length < 1 ||
						bytes.Length > 3 ||
						bytes.Any(bChr => bChr == 0x00)
						)
						throw null; // never

					int code = bytes[0];

					for (int bOfst = 1; bOfst < bytes.Length; bOfst++)
						code |= (int)bytes[bOfst] << (bOfst * 8);

					codeMap[code / 64] |= (UInt64)1 << (code % 64);
				}
				return codeMap;
			}

			// memo: バイト列にゼロが含まれていても問題無い。@ 2024.2.3
			// -- { 存在する文字コード + 0x00... } --> 存在する文字コードの時点でヒットする。
			// -- { 存在しない文字コード + 0x00... } --> ヒットしない。

			private static void ToFairUTF8Bytes(byte[] bytes)
			{
				if (UTF8CodeMap == null)
					UTF8CodeMap = GetUTF8CodeMap();

				for (int index = 0; index < bytes.Length;)
				{
					bool matched = false;

					for (int span = 1; span <= 3 && index + span <= bytes.Length; span++)
					{
						int code = (int)bytes[index];

						for (int bOfst = 1; bOfst < span; bOfst++)
							code |= (int)bytes[index + bOfst] << (bOfst * 8);

						if ((UTF8CodeMap[code / 64] & ((UInt64)1 << (code % 64))) != 0)
						{
							matched = true;
							index += span;
							break;
						}
					}
					if (!matched)
					{
						bytes[index] = 0x3f; // '?'
						index++;
					}
				}
			}
		}

		#endregion

		/// <summary>
		/// 文字列をSJIS(CP-932)の文字列に変換する。
		/// 以下の関数を踏襲した。(慣習的実装)
		/// https://github.com/stackprobe/Factory/blob/master/Common/DataConv.c#L320-L388
		/// </summary>
		/// <param name="str">文字列</param>
		/// <param name="okJpn">日本語(2バイト文字)を許可するか</param>
		/// <param name="okRet">改行を許可するか</param>
		/// <param name="okTab">水平タブを許可するか</param>
		/// <param name="okSpc">半角空白を許可するか</param>
		/// <returns>SJIS(CP-932)の文字列</returns>
		public static string ToJString(string str, bool okJpn, bool okRet, bool okTab, bool okSpc)
		{
			if (str == null)
				str = "";

			return ToJString(GetSJISBytes(str), okJpn, okRet, okTab, okSpc);
		}

		#region GetSJISBytes

		public static byte[] GetSJISBytes(string str)
		{
			byte[] B_HAN_QUES = new byte[] { 0x3f }; // '?'
			byte[] B_ZEN_QUES = new byte[] { 0x81, 0x48 }; // '？'

			using (MemoryStream dest = new MemoryStream())
			{
				foreach (char chr in str)
				{
					byte[] chrSJIS = Unicode2SJIS.GetTable()[(int)chr];

					if (chrSJIS == null)
						chrSJIS = chr < '\u0080' ? B_HAN_QUES : B_ZEN_QUES;

					dest.Write(chrSJIS, 0, chrSJIS.Length);
				}
				return dest.ToArray();
			}
		}

		private static class Unicode2SJIS
		{
			private static Lazy<byte[][]> Table = new Lazy<byte[][]>(() => GetTable_Once());

			public static byte[][] GetTable()
			{
				return Table.Value;
			}

			private static byte[][] GetTable_Once()
			{
				byte[][] dest = new byte[0x10000][];

				dest[0x09] = new byte[] { 0x09 }; // HT
				dest[0x0a] = new byte[] { 0x0a }; // LF
				dest[0x0d] = new byte[] { 0x0d }; // CR

				for (int bChr = 0x20; bChr <= 0x7e; bChr++) // アスキー文字
				{
					dest[bChr] = new byte[] { (byte)bChr };
				}
				for (int bChr = 0xa1; bChr <= 0xdf; bChr++) // 半角カナ
				{
					dest[SJISHanKanaToUnicodeHanKana(bChr)] = new byte[] { (byte)bChr };
				}

				// 全角文字
				{
					char[] unicodes = GetJChars().ToCharArray();

					if (unicodes.Length * 2 != GetJCharBytes().Count()) // ? 文字数が合わない。-- サロゲートペアは無いはず！
						throw null; // never

					foreach (char unicode in unicodes)
					{
						byte[] bJChr = ENCODING_SJIS.GetBytes(new string(new char[] { unicode }));

						if (bJChr.Length != 2) // ? 全角文字じゃない。
							throw null; // never

						dest[(int)unicode] = bJChr;
					}
				}

				return dest;
			}

			private static int SJISHanKanaToUnicodeHanKana(int chr)
			{
				return chr + 0xfec0;
			}
		}

		#endregion

		/// <summary>
		/// バイト列をSJIS(CP-932)の文字列に変換する。
		/// 以下の関数を踏襲した。(慣習的実装)
		/// https://github.com/stackprobe/Factory/blob/master/Common/DataConv.c#L320-L388
		/// </summary>
		/// <param name="src">バイト列</param>
		/// <param name="okJpn">日本語(2バイト文字)を許可するか</param>
		/// <param name="okRet">改行を許可するか</param>
		/// <param name="okTab">水平タブを許可するか</param>
		/// <param name="okSpc">半角空白を許可するか</param>
		/// <returns>SJIS(CP-932)の文字列</returns>
		public static string ToJString(byte[] src, bool okJpn, bool okRet, bool okTab, bool okSpc)
		{
			if (src == null)
				src = EMPTY_BYTES;

			byte[] bRet;

			using (MemoryStream dest = new MemoryStream())
			{
				for (int index = 0; index < src.Length; index++)
				{
					byte chr = src[index];

					if (chr == 0x09) // ? '\t'
					{
						if (!okTab)
							continue;
					}
					else if (chr == 0x0a) // ? '\n'
					{
						if (!okRet)
							continue;
					}
					else if (chr < 0x20) // ? other control code
					{
						continue;
					}
					else if (chr == 0x20) // ? ' '
					{
						if (!okSpc)
							continue;
					}
					else if (chr <= 0x7e) // ? ascii
					{
						// none
					}
					else if (0xa1 <= chr && chr <= 0xdf) // ? kana
					{
						if (!okJpn)
							continue;
					}
					else // ? 全角文字の前半 || 破損
					{
						if (!okJpn)
							continue;

						index++;

						if (src.Length <= index) // ? 後半欠損
							break;

						if (!JCharCodes.I.Contains(chr, src[index])) // ? 破損
							continue;

						dest.WriteByte(chr);
						chr = src[index];
					}
					dest.WriteByte(chr);
				}
				bRet = dest.ToArray();
			}

			// added @ 2024.11.18
			// 空文字列ではない文字列が空文字列に変換されることは想定されないかもしれないので、これを回避する。
			{
				if (src.Length != 0 && bRet.Length == 0)
				{
					bRet = new byte[] { 0x3f }; // '?'
				}
			}

			return ENCODING_SJIS.GetString(bRet);
		}

		// memo: SJIS(CP-932)の中にサロゲートペアは無い。
		// -- なので以下は保証される。
		// ---- SCommon.GetJChars().Length == SCommon.GetJCharCodes().Count()

		// memo: GetJCharCodes()で得られる文字について
		// SJISから見ると以下の変換パターンがある。
		// -- SJIS <<---->> Unicode
		// -- SJIS ------>> Unicode <<---->> SJIS(GetJCharCodes()に含まれる別の文字)
		// Unicodeから見ると以下の変換パターンのみがある。
		// -- Unicode <<---->> SJIS
		// see: https://gist.github.com/stackprobe/02525e3e25cefdf8a4a019b2eeffd1ae

		/// <summary>
		/// SJIS(CP-932)の2バイト文字を全て返す。
		/// 戻り値の文字コード：Unicode
		/// </summary>
		/// <returns>SJIS(CP-932)の2バイト文字の文字列</returns>
		public static string GetJChars()
		{
			return ENCODING_SJIS.GetString(GetJCharBytes().ToArray());
		}

		/// <summary>
		/// SJIS(CP-932)の2バイト文字を全て返す。
		/// 戻り値の文字コード：SJIS
		/// </summary>
		/// <returns>SJIS(CP-932)の2バイト文字のバイト列</returns>
		public static IEnumerable<byte> GetJCharBytes()
		{
			foreach (UInt16 code in GetJCharCodes())
			{
				yield return (byte)(code >> 8);
				yield return (byte)(code & 0xff);
			}
		}

		/// <summary>
		/// SJIS(CP-932)の2バイト文字を全て返す。
		/// 戻り値の文字コード：SJIS
		/// </summary>
		/// <returns>SJIS(CP-932)の2バイト文字の列挙</returns>
		public static IEnumerable<UInt16> GetJCharCodes()
		{
			for (UInt16 code = JCharCodes.CODE_MIN; code <= JCharCodes.CODE_MAX; code++)
				if (JCharCodes.I.Contains(code))
					yield return code;
		}

		private class JCharCodes
		{
			private static Lazy<JCharCodes> _i = new Lazy<JCharCodes>(() => new JCharCodes());

			public static JCharCodes I
			{
				get
				{
					return _i.Value;
				}
			}

			private UInt64[] CodeMap = new UInt64[0x10000 / 64];

			private JCharCodes()
			{
				this.AddAll();
			}

			public const UInt16 CODE_MIN = 0x8140;
			public const UInt16 CODE_MAX = 0xfc4b;

			#region AddAll

			/// <summary>
			/// generated by https://github.com/stackprobe/Factory/blob/master/Labo/GenData/IsJChar.c
			/// </summary>
			private void AddAll()
			{
				this.Add(0x8140, 0x817e);
				this.Add(0x8180, 0x81ac);
				this.Add(0x81b8, 0x81bf);
				this.Add(0x81c8, 0x81ce);
				this.Add(0x81da, 0x81e8);
				this.Add(0x81f0, 0x81f7);
				this.Add(0x81fc, 0x81fc);
				this.Add(0x824f, 0x8258);
				this.Add(0x8260, 0x8279);
				this.Add(0x8281, 0x829a);
				this.Add(0x829f, 0x82f1);
				this.Add(0x8340, 0x837e);
				this.Add(0x8380, 0x8396);
				this.Add(0x839f, 0x83b6);
				this.Add(0x83bf, 0x83d6);
				this.Add(0x8440, 0x8460);
				this.Add(0x8470, 0x847e);
				this.Add(0x8480, 0x8491);
				this.Add(0x849f, 0x84be);
				this.Add(0x8740, 0x875d);
				this.Add(0x875f, 0x8775);
				this.Add(0x877e, 0x877e);
				this.Add(0x8780, 0x879c);
				this.Add(0x889f, 0x88fc);
				this.Add(0x8940, 0x897e);
				this.Add(0x8980, 0x89fc);
				this.Add(0x8a40, 0x8a7e);
				this.Add(0x8a80, 0x8afc);
				this.Add(0x8b40, 0x8b7e);
				this.Add(0x8b80, 0x8bfc);
				this.Add(0x8c40, 0x8c7e);
				this.Add(0x8c80, 0x8cfc);
				this.Add(0x8d40, 0x8d7e);
				this.Add(0x8d80, 0x8dfc);
				this.Add(0x8e40, 0x8e7e);
				this.Add(0x8e80, 0x8efc);
				this.Add(0x8f40, 0x8f7e);
				this.Add(0x8f80, 0x8ffc);
				this.Add(0x9040, 0x907e);
				this.Add(0x9080, 0x90fc);
				this.Add(0x9140, 0x917e);
				this.Add(0x9180, 0x91fc);
				this.Add(0x9240, 0x927e);
				this.Add(0x9280, 0x92fc);
				this.Add(0x9340, 0x937e);
				this.Add(0x9380, 0x93fc);
				this.Add(0x9440, 0x947e);
				this.Add(0x9480, 0x94fc);
				this.Add(0x9540, 0x957e);
				this.Add(0x9580, 0x95fc);
				this.Add(0x9640, 0x967e);
				this.Add(0x9680, 0x96fc);
				this.Add(0x9740, 0x977e);
				this.Add(0x9780, 0x97fc);
				this.Add(0x9840, 0x9872);
				this.Add(0x989f, 0x98fc);
				this.Add(0x9940, 0x997e);
				this.Add(0x9980, 0x99fc);
				this.Add(0x9a40, 0x9a7e);
				this.Add(0x9a80, 0x9afc);
				this.Add(0x9b40, 0x9b7e);
				this.Add(0x9b80, 0x9bfc);
				this.Add(0x9c40, 0x9c7e);
				this.Add(0x9c80, 0x9cfc);
				this.Add(0x9d40, 0x9d7e);
				this.Add(0x9d80, 0x9dfc);
				this.Add(0x9e40, 0x9e7e);
				this.Add(0x9e80, 0x9efc);
				this.Add(0x9f40, 0x9f7e);
				this.Add(0x9f80, 0x9ffc);
				this.Add(0xe040, 0xe07e);
				this.Add(0xe080, 0xe0fc);
				this.Add(0xe140, 0xe17e);
				this.Add(0xe180, 0xe1fc);
				this.Add(0xe240, 0xe27e);
				this.Add(0xe280, 0xe2fc);
				this.Add(0xe340, 0xe37e);
				this.Add(0xe380, 0xe3fc);
				this.Add(0xe440, 0xe47e);
				this.Add(0xe480, 0xe4fc);
				this.Add(0xe540, 0xe57e);
				this.Add(0xe580, 0xe5fc);
				this.Add(0xe640, 0xe67e);
				this.Add(0xe680, 0xe6fc);
				this.Add(0xe740, 0xe77e);
				this.Add(0xe780, 0xe7fc);
				this.Add(0xe840, 0xe87e);
				this.Add(0xe880, 0xe8fc);
				this.Add(0xe940, 0xe97e);
				this.Add(0xe980, 0xe9fc);
				this.Add(0xea40, 0xea7e);
				this.Add(0xea80, 0xeaa4);
				this.Add(0xed40, 0xed7e);
				this.Add(0xed80, 0xedfc);
				this.Add(0xee40, 0xee7e);
				this.Add(0xee80, 0xeeec);
				this.Add(0xeeef, 0xeefc);
				this.Add(0xfa40, 0xfa7e);
				this.Add(0xfa80, 0xfafc);
				this.Add(0xfb40, 0xfb7e);
				this.Add(0xfb80, 0xfbfc);
				this.Add(0xfc40, 0xfc4b);
			}

			#endregion

			private void Add(UInt16 codeMin, UInt16 codeMax)
			{
				for (UInt16 code = codeMin; code <= codeMax; code++)
				{
					this.Add(code);
				}
			}

			private void Add(UInt16 code)
			{
				this.CodeMap[code / 64] |= (UInt64)1 << (code % 64);
			}

			public bool Contains(byte lead, byte trail)
			{
				UInt16 code = lead;

				code <<= 8;
				code |= trail;

				return Contains(code);
			}

			public bool Contains(UInt16 code)
			{
				return (this.CodeMap[code / 64] & ((UInt64)1 << (code % 64))) != (UInt64)0;
			}
		}

		private static Lazy<Randomizer> _crandom = new Lazy<Randomizer>(() => new CSPRNGRandomizer());

		public static Randomizer CRandom
		{
			get
			{
				return _crandom.Value;
			}
		}

		private class CSPRNGRandomizer : Randomizer, IDisposable
		{
			private RandomNumberGenerator CSPRNG = RandomNumberGenerator.Create();
			private byte[] Cache = new byte[4096];

			protected override byte[] GetBlock()
			{
				this.CSPRNG.GetBytes(this.Cache);
				return this.Cache;
			}

			public void Dispose()
			{
				if (this.CSPRNG != null)
				{
					this.CSPRNG.Dispose();
					this.CSPRNG = null;
				}
			}
		}

		#region CryptographicHash

		#region MD5

		public static byte[] GetMD5(byte[] src)
		{
			return GetCryptographicHash(CreateMD5, src);
		}

		public static byte[] GetMD5(IEnumerable<byte[]> src)
		{
			return GetCryptographicHash(CreateMD5, src);
		}

		public static byte[] GetMD5(Read_d reader)
		{
			return GetCryptographicHash(CreateMD5, reader);
		}

		public static byte[] GetMD5(Action<Write_d> execute)
		{
			return GetCryptographicHash(CreateMD5, execute);
		}

		public static byte[] GetMD5File(string file)
		{
			return GetCryptographicHashFile(CreateMD5, file);
		}

		public static string GetMD5String(byte[] src)
		{
			return GetCryptographicHashString(CreateMD5, src);
		}

		public static string GetMD5StringFile(string file)
		{
			return GetCryptographicHashStringFile(CreateMD5, file);
		}

		#endregion

		#region SHA256

		public static byte[] GetSHA256(byte[] src)
		{
			return GetCryptographicHash(CreateSHA256, src);
		}

		public static byte[] GetSHA256(IEnumerable<byte[]> src)
		{
			return GetCryptographicHash(CreateSHA256, src);
		}

		public static byte[] GetSHA256(Read_d reader)
		{
			return GetCryptographicHash(CreateSHA256, reader);
		}

		public static byte[] GetSHA256(Action<Write_d> execute)
		{
			return GetCryptographicHash(CreateSHA256, execute);
		}

		public static byte[] GetSHA256File(string file)
		{
			return GetCryptographicHashFile(CreateSHA256, file);
		}

		public static string GetSHA256String(byte[] src)
		{
			return GetCryptographicHashString(CreateSHA256, src);
		}

		public static string GetSHA256StringFile(string file)
		{
			return GetCryptographicHashStringFile(CreateSHA256, file);
		}

		#endregion

		#region SHA512

		public static byte[] GetSHA512(byte[] src)
		{
			return GetCryptographicHash(CreateSHA512, src);
		}

		public static byte[] GetSHA512(IEnumerable<byte[]> src)
		{
			return GetCryptographicHash(CreateSHA512, src);
		}

		public static byte[] GetSHA512(Read_d reader)
		{
			return GetCryptographicHash(CreateSHA512, reader);
		}

		public static byte[] GetSHA512(Action<Write_d> execute)
		{
			return GetCryptographicHash(CreateSHA512, execute);
		}

		public static byte[] GetSHA512File(string file)
		{
			return GetCryptographicHashFile(CreateSHA512, file);
		}

		public static string GetSHA512String(byte[] src)
		{
			return GetCryptographicHashString(CreateSHA512, src);
		}

		public static string GetSHA512StringFile(string file)
		{
			return GetCryptographicHashStringFile(CreateSHA512, file);
		}

		#endregion

		private static MD5 CreateMD5()
		{
			return MD5.Create();
		}

		private static SHA256 CreateSHA256()
		{
			return SHA256.Create();
		}

		private static SHA512 CreateSHA512()
		{
			return SHA512.Create();
		}

		private static byte[] GetCryptographicHash(Func<HashAlgorithm> createHA, byte[] src)
		{
			using (HashAlgorithm ha = createHA())
			{
				return ha.ComputeHash(src);
			}
		}

		private static byte[] GetCryptographicHash(Func<HashAlgorithm> createHA, IEnumerable<byte[]> src)
		{
			return GetCryptographicHash(createHA, writePart =>
			{
				foreach (byte[] part in src)
				{
					writePart(part, 0, part.Length);
				}
			});
		}

		private static byte[] GetCryptographicHash(Func<HashAlgorithm> createHA, Read_d reader)
		{
			return GetCryptographicHash(createHA, writePart =>
			{
				SCommon.ReadToEnd(reader, writePart);
			});
		}

		private static byte[] GetCryptographicHash(Func<HashAlgorithm> createHA, Action<Write_d> execute)
		{
			using (HashAlgorithm ha = createHA())
			{
				execute((buff, offset, count) => ha.TransformBlock(buff, offset, count, null, 0));
				ha.TransformFinalBlock(EMPTY_BYTES, 0, 0);
				return ha.Hash;
			}
		}

		private static byte[] GetCryptographicHashFile(Func<HashAlgorithm> createHA, string file)
		{
			using (HashAlgorithm ha = createHA())
			using (FileStream reader = new FileStream(file, FileMode.Open, FileAccess.Read))
			{
				return ha.ComputeHash(reader);
			}
		}

		private static string GetCryptographicHashString(Func<HashAlgorithm> createHA, byte[] src)
		{
			return SCommon.Hex.I.GetString(SCommon.GetCryptographicHash(createHA, src));
		}

		private static string GetCryptographicHashStringFile(Func<HashAlgorithm> createHA, string file)
		{
			return SCommon.Hex.I.GetString(SCommon.GetCryptographicHashFile(createHA, file));
		}

		#endregion

		public class Hex
		{
			private static Lazy<Hex> _i = new Lazy<Hex>(() => new Hex());

			public static Hex I
			{
				get
				{
					return _i.Value;
				}
			}

			private int[] HexChar2Value;

			private Hex()
			{
				this.HexChar2Value = new int[(int)'f' + 1];

				for (int index = 0; index < 10; index++)
					this.HexChar2Value[(int)'0' + index] = index;

				for (int index = 0; index < 6; index++)
				{
					this.HexChar2Value[(int)'A' + index] = 10 + index;
					this.HexChar2Value[(int)'a' + index] = 10 + index;
				}
			}

			private Regex RegexHexString = new Regex("^([0-9A-Fa-f]{2})*$");

			public string GetString(byte[] src)
			{
				if (src == null)
					throw new Exception("不正な入力バイト列");

				StringBuilder buff = new StringBuilder(src.Length * 2);

				foreach (byte chr in src)
				{
					buff.Append(HEXADECIMAL_LOWER[chr >> 4]);
					buff.Append(HEXADECIMAL_LOWER[chr & 0x0f]);
				}
				return buff.ToString();
			}

			public byte[] GetBytes(string src)
			{
				if (
					src == null ||
					!RegexHexString.IsMatch(src)
					)
					throw new Exception("文字列に変換されたバイト列は破損しています。");

				byte[] dest = new byte[src.Length / 2];

				for (int index = 0; index < dest.Length; index++)
				{
					int hi = this.HexChar2Value[(int)src[index * 2 + 0]];
					int lw = this.HexChar2Value[(int)src[index * 2 + 1]];

					dest[index] = (byte)((hi << 4) | lw);
				}
				return dest;
			}
		}

		public static string[] EMPTY_STRINGS = new string[0];

		public static Encoding ENCODING_SJIS = Encoding.GetEncoding(932);

		public static string DECIMAL = "0123456789";
		public static string HEXADECIMAL_UPPER = "0123456789ABCDEF";
		public static string HEXADECIMAL_LOWER = "0123456789abcdef";
		public static string ALPHA_UPPER = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		public static string ALPHA_LOWER = "abcdefghijklmnopqrstuvwxyz";

		public static string ASCII
		{
			get
			{
				return GetString_SJISHalfRange(0x21, 0x7e); // 空白(0x20)を含まないことに注意
			}
		}

		public static string KANA
		{
			get
			{
				return GetString_SJISHalfRange(0xa1, 0xdf);
			}
		}

		public static string HALF
		{
			get
			{
				return ASCII + KANA; // 空白(0x20)を含まないことに注意
			}
		}

		private static string GetString_SJISHalfRange(int codeMin, int codeMax)
		{
			byte[] buff = new byte[codeMax - codeMin + 1];

			for (int code = codeMin; code <= codeMax; code++)
			{
				buff[code - codeMin] = (byte)code;
			}
			return ENCODING_SJIS.GetString(buff);
		}

		public static string MBC_DECIMAL
		{
			get
			{
				return ToAsciiFull(DECIMAL);
			}
		}

		public static string MBC_HEXADECIMAL_UPPER
		{
			get
			{
				return ToAsciiFull(HEXADECIMAL_UPPER);
			}
		}

		public static string MBC_HEXADECIMAL_LOWER
		{
			get
			{
				return ToAsciiFull(HEXADECIMAL_LOWER);
			}
		}

		public static string MBC_ALPHA_UPPER
		{
			get
			{
				return ToAsciiFull(ALPHA_UPPER);
			}
		}

		public static string MBC_ALPHA_LOWER
		{
			get
			{
				return ToAsciiFull(ALPHA_LOWER);
			}
		}

		public static string MBC_ASCII // 空白(0x3000)を含まないことに注意
		{
			get
			{
				return ToAsciiFull(ASCII);
			}
		}

		#region ToAsciiFull, ToAsciiHalf

		/// <summary>
		/// 文字列中のアスキーコードの文字(0x20～0x7e)を半角から全角に変換する。
		/// それ以外の文字は変換しない。
		/// </summary>
		/// <param name="str">文字列</param>
		/// <returns>変換後の文字列</returns>
		public static string ToAsciiFull(string str)
		{
			char[] buff = new char[str.Length];

			for (int index = 0; index < str.Length; index++)
				buff[index] = ToAsciiFull(str[index]);

			return new string(buff);
		}

		/// <summary>
		/// 文字列中のアスキーコードの文字(0x20～0x7e)を全角から半角に変換する。
		/// それ以外の文字は変換しない。
		/// </summary>
		/// <param name="str">文字列</param>
		/// <returns>変換後の文字列</returns>
		public static string ToAsciiHalf(string str)
		{
			char[] buff = new char[str.Length];

			for (int index = 0; index < str.Length; index++)
				buff[index] = ToAsciiHalf(str[index]);

			return new string(buff);
		}

		/// <summary>
		/// アスキーコードの文字(0x20～0x7e)を半角から全角に変換する。
		/// それ以外の文字はそのまま返す。
		/// </summary>
		/// <param name="chr">文字</param>
		/// <returns>変換後の文字</returns>
		public static char ToAsciiFull(char chr)
		{
			if (chr == (char)0x20)
			{
				chr = (char)0x3000;
			}
			else if (0x21 <= chr && chr <= 0x7e)
			{
				chr += (char)0xfee0;
			}
			return chr;
		}

		/// <summary>
		/// アスキーコードの文字(0x20～0x7e)を全角から半角に変換する。
		/// それ以外の文字はそのまま返す。
		/// </summary>
		/// <param name="chr">文字</param>
		/// <returns>変換後の文字</returns>
		public static char ToAsciiHalf(char chr)
		{
			if (chr == (char)0x3000)
			{
				chr = (char)0x20;
			}
			else if (0xff01 <= chr && chr <= 0xff5e)
			{
				chr -= (char)0xfee0;
			}
			return chr;
		}

		#endregion

		public static int Comp(char a, char b)
		{
			return (int)a - (int)b;
		}

		public static int Comp(string a, string b)
		{
			// memo: a.CompareTo(b) -- カルチャ依存文字列比較問題を避けるため使わない。

			return Comp(a.ToCharArray(), b.ToCharArray(), Comp);
		}

		public static int CompIgnoreCase(string a, string b)
		{
			return Comp(a.ToLower(), b.ToLower());
		}

		public static bool EqualsIgnoreCase(string a, string b)
		{
			return a.ToLower() == b.ToLower();
		}

		public static bool StartsWithIgnoreCase(string str, string ptn)
		{
			return str.ToLower().StartsWith(ptn.ToLower());
		}

		public static bool EndsWithIgnoreCase(string str, string ptn)
		{
			return str.ToLower().EndsWith(ptn.ToLower());
		}

		public static bool ContainsIgnoreCase(string str, string ptn)
		{
			return str.ToLower().Contains(ptn.ToLower());
		}

		public static int IndexOfIgnoreCase(string str, string ptn)
		{
			return str.ToLower().IndexOf(ptn.ToLower());
		}

		public static int IndexOfIgnoreCase(string str, char chr)
		{
			return str.ToLower().IndexOf(char.ToLower(chr));
		}

		public static int IndexOfIgnoreCase(IList<string> strs, string str)
		{
			string lwrStr = str.ToLower();

			for (int index = 0; index < strs.Count; index++)
				if (strs[index].ToLower() == lwrStr)
					return index;

			return -1; // not found
		}

		/// <summary>
		/// 文字列を区切り文字で分割する。
		/// </summary>
		/// <param name="str">文字列</param>
		/// <param name="delimiters">区切り文字の集合</param>
		/// <param name="meaningFlag">区切り文字(delimiters)以外を区切り文字とするか</param>
		/// <param name="ignoreEmpty">空文字列のトークンを除去するか</param>
		/// <param name="limit">最大トークン数(2～), -1 == 無制限</param>
		/// <returns>トークン配列</returns>
		public static string[] Tokenize(string str, string delimiters, bool meaningFlag = false, bool ignoreEmpty = false, int limit = -1)
		{
			List<string> tokens = new List<string>();
			StringBuilder buff = new StringBuilder();

			foreach (char chr in str)
			{
				if (delimiters.Contains(chr) == meaningFlag || tokens.Count + 1 == limit)
				{
					buff.Append(chr);
				}
				else
				{
					tokens.Add(buff.ToString());
					buff = new StringBuilder();
				}
			}
			tokens.Add(buff.ToString());

			if (ignoreEmpty)
				tokens.RemoveAll(token => token == "");

			return tokens.ToArray();
		}

		/// <summary>
		/// 文字列をセパレータで分割する。
		/// </summary>
		/// <param name="str">文字列</param>
		/// <param name="separator">セパレータ</param>
		/// <param name="ignoreCase">セパレータの大文字小文字を区別しないか</param>
		/// <param name="ignoreEmpty">空文字列のトークンを除去するか</param>
		/// <param name="limit">最大トークン数(2～), -1 == 無制限</param>
		/// <returns>トークン配列</returns>
		public static string[] Separate(string str, string separator, bool ignoreCase = false, bool ignoreEmpty = false, int limit = -1)
		{
			List<string> tokens = new List<string>();
			int index = 0;

			while (tokens.Count + 1 != limit)
			{
				int[] startEnd = GetIsland(str, index, separator, ignoreCase);

				if (startEnd == null)
					break;

				tokens.Add(str.Substring(index, startEnd[0] - index));
				index = startEnd[1];
			}
			tokens.Add(str.Substring(index));

			if (ignoreEmpty)
				tokens.RemoveAll(token => token == "");

			return tokens.ToArray();
		}

		public static int[] FindAllIndexes<T>(IList<T> list, T targetValue, Comparison<T> comp)
		{
			return Enumerable.Range(0, list.Count).Where(index => comp(list[index], targetValue) == 0).ToArray();
		}

		public static int[] FindAllIndexes<T>(IList<T> list, Predicate<T> match)
		{
			return Enumerable.Range(0, list.Count).Where(index => match(list[index])).ToArray();
		}

		public static bool HasSameChar(string str)
		{
			return GetSameChar(str) != null;
		}

		public static char[] GetSameChar(string str)
		{
			return GetSame(str.ToCharArray(), (a, b) => a == b);
		}

		// memo: @ 2022.10.31
		// HasSameComp, HasSame を同じ名前にすると Comparision<T>, Func<T, T, bool> の型推論の失敗を誘発する。

		public static bool HasSameComp<T>(IList<T> list, Comparison<T> comp)
		{
			return GetSameComp(list, comp) != null;
		}

		public static bool HasSame<T>(IList<T> list, Func<T, T, bool> match)
		{
			return GetSame(list, match) != null;
		}

		public static T[] GetSameComp<T>(IList<T> list, Comparison<T> comp)
		{
			return GetSame(list, (a, b) => comp(a, b) == 0);
		}

		public static T[] GetSame<T>(IList<T> list, Func<T, T, bool> match)
		{
			T[] ret = null;

			ForEachPair(list, (a, b) =>
			{
				if (match(a, b))
				{
					ret = new T[] { a, b };
					return false;
				}
				return true;
			});

			return ret;
		}

		public static void ForEachPair<T>(IList<T> list, Func<T, T, bool> routine)
		{
			for (int l = 0; l < list.Count; l++)
				for (int r = l + 1; r < list.Count; r++)
					if (!routine(list[l], list[r])) // ? 中断
						return;
		}

		// memo:
		// 戻り値の要素数：
		// ParseIsland   --> 3 -- { タグの前, タグ, タグの後 }
		// ParseEnclosed --> 5 -- { 開始タグの前, 開始タグ, タグの間, 終了タグ, 終了タグの後 }
		// GetIsland     --> 2 -- { タグの開始位置, タグの終了位置(*) }
		// GetEnclosed   --> 4 -- { 開始タグの開始位置, 開始タグの終了位置(*), 終了タグの開始位置, 終了タグの終了位置(*) }
		//
		// * 終了位置 == 最後の文字の次の位置

		public static string[] ParseIsland(string text, string singleTag, bool ignoreCase = false)
		{
			int start;

			if (ignoreCase)
				start = text.ToLower().IndexOf(singleTag.ToLower());
			else
				start = text.IndexOf(singleTag);

			if (start == -1)
				return null;

			int end = start + singleTag.Length;

			return new string[]
			{
				text.Substring(0, start),
				text.Substring(start, end - start),
				text.Substring(end),
			};
		}

		public static string[] ParseEnclosed(string text, string openTag, string closeTag, bool ignoreCase = false)
		{
			string[] starts = ParseIsland(text, openTag, ignoreCase);

			if (starts == null)
				return null;

			string[] ends = ParseIsland(starts[2], closeTag, ignoreCase);

			if (ends == null)
				return null;

			return new string[]
			{
				starts[0],
				starts[1],
				ends[0],
				ends[1],
				ends[2],
			};
		}

		public static int[] GetIsland(string text, int startIndex, string singleTag, bool ignoreCase = false)
		{
			int start;

			if (ignoreCase)
				start = text.ToLower().IndexOf(singleTag.ToLower(), startIndex);
			else
				start = text.IndexOf(singleTag, startIndex);

			if (start == -1)
				return null;

			int end = start + singleTag.Length;

			return new int[]
			{
				start,
				end,
			};
		}

		public static int[] GetEnclosed(string text, int startIndex, string openTag, string closeTag, bool ignoreCase = false)
		{
			int[] starts = GetIsland(text, startIndex, openTag, ignoreCase);

			if (starts == null)
				return null;

			int[] ends = GetIsland(text, starts[1], closeTag, ignoreCase);

			if (ends == null)
				return null;

			return new int[]
			{
				starts[0],
				starts[1],
				ends[0],
				ends[1],
			};
		}

		public static string[] ParseFirstIsland(string text, out int tagIndex, params object[] tagTable) // tagTable: (singleTag, ignoreCase)...
		{
			string[] firstIsld = null;
			tagIndex = -1;

			for (int index = 0; index * 2 < tagTable.Length; index++)
			{
				object[] tags = SCommon.GetPart(tagTable, index * 2, 2);
				string[] isld = SCommon.ParseIsland(text, (string)tags[0], (bool)tags[1]);

				if (isld == null)
					continue;

				if (firstIsld != null && firstIsld[0].Length <= isld[0].Length)
					continue;

				firstIsld = isld;
				tagIndex = index;
			}
			return firstIsld;
		}

		public static string[] ParseFirstEnclosed(string text, out int tagIndex, params object[] tagTable) // tagTable: (openTag, closeTag, ignoreCase)...
		{
			string[] firstEncl = null;
			tagIndex = -1;

			for (int index = 0; index * 3 < tagTable.Length; index++)
			{
				object[] tags = SCommon.GetPart(tagTable, index * 3, 3);
				string[] encl = SCommon.ParseEnclosed(text, (string)tags[0], (string)tags[1], (bool)tags[2]);

				if (encl == null)
					continue;

				if (firstEncl != null && firstEncl[0].Length <= encl[0].Length)
					continue;

				firstEncl = encl;
				tagIndex = index;
			}
			return firstEncl;
		}

		public static int[] GetFirstIsland(string text, int startIndex, out int tagIndex, params object[] tagTable) // tagTable: (singleTag, ignoreCase)...
		{
			int[] firstIsld = null;
			tagIndex = -1;

			for (int index = 0; index * 2 < tagTable.Length; index++)
			{
				object[] tags = SCommon.GetPart(tagTable, index * 2, 2);
				int[] isld = SCommon.GetIsland(text, startIndex, (string)tags[0], (bool)tags[1]);

				if (isld == null)
					continue;

				if (firstIsld != null && firstIsld[0] <= isld[0])
					continue;

				firstIsld = isld;
				tagIndex = index;
			}
			return firstIsld;
		}

		public static int[] GetFirstEnclosed(string text, int startIndex, out int tagIndex, params object[] tagTable) // tagTable: (openTag, closeTag, ignoreCase)...
		{
			int[] firstEncl = null;
			tagIndex = -1;

			for (int index = 0; index * 3 < tagTable.Length; index++)
			{
				object[] tags = SCommon.GetPart(tagTable, index * 3, 3);
				int[] encl = SCommon.GetEnclosed(text, startIndex, (string)tags[0], (string)tags[1], (bool)tags[2]);

				if (encl == null)
					continue;

				if (firstEncl != null && firstEncl[0] <= encl[0])
					continue;

				firstEncl = encl;
				tagIndex = index;
			}
			return firstEncl;
		}

		public static byte[] Compress(byte[] src)
		{
			using (MemoryStream reader = new MemoryStream(src))
			using (MemoryStream writer = new MemoryStream())
			{
				Compress(reader, writer);
				return writer.ToArray();
			}
		}

		public static byte[] Decompress(byte[] src, int limit = -1)
		{
			using (MemoryStream reader = new MemoryStream(src))
			using (MemoryStream writer = new MemoryStream())
			{
				Decompress(reader, writer, (long)limit);
				return writer.ToArray();
			}
		}

		public static void CompressFile(string rFile, string wFile)
		{
			using (FileStream reader = new FileStream(rFile, FileMode.Open, FileAccess.Read))
			using (FileStream writer = new FileStream(wFile, FileMode.Create, FileAccess.Write))
			{
				Compress(reader, writer);
			}
		}

		public static void DecompressFile(string rFile, string wFile, long limit = -1L)
		{
			using (FileStream reader = new FileStream(rFile, FileMode.Open, FileAccess.Read))
			using (FileStream writer = new FileStream(wFile, FileMode.Create, FileAccess.Write))
			{
				Decompress(reader, writer, limit);
			}
		}

		public static void Compress(Stream reader, Stream writer)
		{
			using (GZipStream gz = new GZipStream(writer, CompressionMode.Compress, true))
			{
				reader.CopyTo(gz);
			}
		}

		public static void Decompress(Stream reader, Stream writer, long limit = -1L)
		{
			using (GZipStream gz = new GZipStream(reader, CompressionMode.Decompress, true))
			{
				if (limit == -1L)
				{
					gz.CopyTo(writer);
				}
				else
				{
					ReadToEnd(gz.Read, GetLimitedWriter(writer.Write, limit));
				}
			}
		}

		public static Write_d GetLimitedWriter(Write_d writer, long remaining)
		{
			return (buff, offset, count) =>
			{
				if (remaining < (long)count)
					throw new Exception("ストリームに書き込めるバイト数の上限を超えようとしました。");

				remaining -= (long)count;
				writer(buff, offset, count);
			};
		}

		public static Read_d GetLimitedReader(Read_d reader, long remaining)
		{
			return (buff, offset, count) =>
			{
				if (remaining <= 0L)
					return -1;

				count = (int)Math.Min((long)count, remaining);
				count = reader(buff, offset, count);

				if (count <= 0) // ? これ以上読み込めない
					remaining = 0L;
				else
					remaining -= (long)count;

				return count;
			};
		}

		public static Read_d GetReader(Read_d reader)
		{
			return (buff, offset, count) =>
			{
				if (reader(buff, offset, count) != count)
				{
					throw new Exception("データの途中でストリームの終端に到達しました。");
				}
				return count;
			};
		}

		public static void Batch(IList<string> commands, string workingDir = "", StartProcessWindowStyle_e winStyle = StartProcessWindowStyle_e.INVISIBLE)
		{
			using (WorkingDir wd = new WorkingDir())
			{
				string batFile = wd.MakePath() + ".bat";

				File.WriteAllLines(batFile, commands, ENCODING_SJIS);

				StartProcess("cmd", $"/c \"{batFile}\"", workingDir, winStyle).WaitForExit();
			}
		}

		public enum StartProcessWindowStyle_e
		{
			INVISIBLE = 1,
			MINIMIZED,
			NORMAL,
			COEXISTENCE,
		};

		public static Process StartProcess(string file, string args, string workingDir = "", StartProcessWindowStyle_e winStyle = StartProcessWindowStyle_e.INVISIBLE)
		{
			ProcessStartInfo psi = new ProcessStartInfo();

			psi.FileName = file;
			psi.Arguments = args;
			psi.WorkingDirectory = workingDir; // 既定値 == ""

			switch (winStyle)
			{
				case StartProcessWindowStyle_e.INVISIBLE:
					psi.CreateNoWindow = true;
					psi.UseShellExecute = false;
					break;

				case StartProcessWindowStyle_e.MINIMIZED:
					psi.WindowStyle = ProcessWindowStyle.Minimized;
					break;

				case StartProcessWindowStyle_e.NORMAL:
					break;

				case StartProcessWindowStyle_e.COEXISTENCE:
					psi.UseShellExecute = false;
					break;

				default:
					throw null;
			}
			return Process.Start(psi);
		}

		#region Base32

		public class Base32
		{
			private static Lazy<Base32> _i = new Lazy<Base32>(() => new Base32());

			public static Base32 I
			{
				get
				{
					return _i.Value;
				}
			}

			private const int CHAR_MAP_SIZE = 0x80;
			private const char CHAR_PADDING = '=';

			private char[] Chars;
			private int[] CharMap;

			private Base32()
			{
				this.Chars = (SCommon.ALPHA_UPPER + SCommon.DECIMAL.Substring(2, 6)).ToCharArray();
				this.CharMap = new int[CHAR_MAP_SIZE];

				for (int index = 0; index < CHAR_MAP_SIZE; index++)
					this.CharMap[index] = -1;

				for (int index = 0; index < this.Chars.Length; index++)
					this.CharMap[(int)this.Chars[index]] = index;
			}

			public string EncodeNoPadding(byte[] data)
			{
				return Encode(data).Replace(new string(new char[] { CHAR_PADDING }), "");
			}

			public string Encode(byte[] data)
			{
				if (data == null)
					data = SCommon.EMPTY_BYTES;

				string str;

				if (data.Length % 5 == 0)
				{
					str = EncodeEven(data);
				}
				else
				{
					int padding = ((5 - data.Length % 5) * 8) / 5;

					data = SCommon.Join(new byte[][]
					{
						data,
						Enumerable.Repeat((byte)0, 5 - data.Length % 5).ToArray(),
					});

					str = EncodeEven(data);
					str = str.Substring(0, str.Length - padding) + new string(CHAR_PADDING, padding);
				}
				return str;
			}

			private string EncodeEven(byte[] data)
			{
				char[] buff = new char[(data.Length / 5) * 8];
				int reader = 0;
				int writer = 0;
				ulong value;

				while (reader < data.Length)
				{
					value = (ulong)data[reader++] << 32;
					value |= (ulong)data[reader++] << 24;
					value |= (ulong)data[reader++] << 16;
					value |= (ulong)data[reader++] << 8;
					value |= (ulong)data[reader++];

					buff[writer++] = this.Chars[(value >> 35) & 0x1f];
					buff[writer++] = this.Chars[(value >> 30) & 0x1f];
					buff[writer++] = this.Chars[(value >> 25) & 0x1f];
					buff[writer++] = this.Chars[(value >> 20) & 0x1f];
					buff[writer++] = this.Chars[(value >> 15) & 0x1f];
					buff[writer++] = this.Chars[(value >> 10) & 0x1f];
					buff[writer++] = this.Chars[(value >> 5) & 0x1f];
					buff[writer++] = this.Chars[value & 0x1f];
				}
				return new string(buff);
			}

			/// <summary>
			/// Base32をデコードする。
			/// 注意：入力文字列がでたらめな内容であっても、例外を投げずに何らかのバイト列を返す。
			/// </summary>
			/// <param name="str">入力文字列</param>
			/// <returns>バイト列</returns>
			public byte[] Decode(string str)
			{
				if (str == null)
					str = "";

				str = str.ToUpper(); // 小文字を許容する。
				str = new string(str.Where(chr => (int)chr < CHAR_MAP_SIZE && this.CharMap[(int)chr] != -1).ToArray());

				byte[] data;

				if (str.Length % 8 == 0)
				{
					data = DecodeEven(str);
				}
				else
				{
					int padding = 5 - ((str.Length % 8) * 5) / 8;

					str += new string(this.Chars[0], 8 - str.Length % 8);

					data = DecodeEven(str);
					data = SCommon.GetPart(data, 0, data.Length - padding);
				}
				return data;
			}

			private byte[] DecodeEven(string str)
			{
				byte[] data = new byte[(str.Length / 8) * 5];
				int reader = 0;
				int writer = 0;
				ulong value;

				while (reader < str.Length)
				{
					value = (ulong)(uint)this.CharMap[(int)str[reader++]] << 35;
					value |= (ulong)(uint)this.CharMap[(int)str[reader++]] << 30;
					value |= (ulong)(uint)this.CharMap[(int)str[reader++]] << 25;
					value |= (ulong)(uint)this.CharMap[(int)str[reader++]] << 20;
					value |= (ulong)(uint)this.CharMap[(int)str[reader++]] << 15;
					value |= (ulong)(uint)this.CharMap[(int)str[reader++]] << 10;
					value |= (ulong)(uint)this.CharMap[(int)str[reader++]] << 5;
					value |= (ulong)(uint)this.CharMap[(int)str[reader++]];

					data[writer++] = (byte)((value >> 32) & 0xff);
					data[writer++] = (byte)((value >> 24) & 0xff);
					data[writer++] = (byte)((value >> 16) & 0xff);
					data[writer++] = (byte)((value >> 8) & 0xff);
					data[writer++] = (byte)(value & 0xff);
				}
				return data;
			}
		}

		#endregion

		#region Base64

		public class Base64
		{
			private static Lazy<Base64> _i = new Lazy<Base64>(() => new Base64());

			public static Base64 I
			{
				get
				{
					return _i.Value;
				}
			}

			private const int CHAR_MAP_SIZE = 0x80;
			private const char CHAR_PADDING = '=';

			private char[] Chars;
			private int[] CharMap;

			private Base64()
			{
				this.Chars = (SCommon.ALPHA_UPPER + SCommon.ALPHA_LOWER + SCommon.DECIMAL + "+/").ToCharArray();
				this.CharMap = new int[CHAR_MAP_SIZE];

				for (int index = 0; index < CHAR_MAP_SIZE; index++)
					this.CharMap[index] = -1;

				for (int index = 0; index < this.Chars.Length; index++)
					this.CharMap[(int)this.Chars[index]] = index;
			}

			public string EncodeNoPadding(byte[] data)
			{
				return Encode(data).Replace(new string(new char[] { CHAR_PADDING }), "");
			}

			public string Encode(byte[] data)
			{
				if (data == null)
					data = SCommon.EMPTY_BYTES;

				return Convert.ToBase64String(data);
			}

			/// <summary>
			/// Base64をデコードする。
			/// 注意：入力文字列がでたらめな内容であっても、例外を投げずに何らかのバイト列を返す。
			/// </summary>
			/// <param name="str">入力文字列</param>
			/// <returns>バイト列</returns>
			public byte[] Decode(string str)
			{
				if (str == null)
					str = "";

				str = new string(str.Where(chr => (int)chr < CHAR_MAP_SIZE && this.CharMap[(int)chr] != -1).ToArray());

				switch (str.Length % 4)
				{
					case 0:
						break;

					case 1:
						str = str.Substring(0, str.Length - 1); // 端数1はあり得ないので切り捨てる。
						break;

					case 2:
						if (this.CharMap[(int)str[str.Length - 1]] % 16 != 0) // ? 端数2のときのあり得ない最後の文字
						{
							str = str.Substring(0, str.Length - 2); // 端数を切り捨てる。
						}
						break;

					case 3:
						if (this.CharMap[(int)str[str.Length - 1]] % 4 != 0) // ? 端数3のときのあり得ない最後の文字
						{
							str = str.Substring(0, str.Length - 3); // 端数を切り捨てる。
						}
						break;

					default:
						throw null; // never
				}

				str += new string(CHAR_PADDING, (4 - str.Length % 4) % 4);

				return Convert.FromBase64String(str);
			}
		}

		#endregion

		#region TimeStampToSec

		/// <summary>
		/// 日時を 1/1/1 00:00:00 からの経過秒数に変換およびその逆を行う。
		/// 日時のフォーマット
		/// -- YMMDDhhmmss
		/// -- YYMMDDhhmmss
		/// -- YYYMMDDhhmmss
		/// -- YYYYMMDDhhmmss
		/// -- YYYYYMMDDhhmmss
		/// -- YYYYYYMMDDhhmmss
		/// -- YYYYYYYMMDDhhmmss
		/// -- YYYYYYYYMMDDhhmmss
		/// -- YYYYYYYYYMMDDhhmmss -- 但し YYYYYYYYY == 100000000 ～ 922337203
		/// ---- 年の桁数は 1 ～ 9 桁
		/// 日時の範囲
		/// -- 最小 1/1/1 00:00:00
		/// -- 最大 922337203/12/31 23:59:59
		/// </summary>
		public static class TimeStampToSec
		{
			private const int YEAR_MIN = 1;
			private const int YEAR_MAX = 922337203;

			private const long TIME_STAMP_MIN = 10101000000L;
			private const long TIME_STAMP_MAX = 9223372031231235959L;

			private const long DEFAULT_SEC = 62135596800L; // == 1970/1/1 00:00:00

			/// <summary>
			/// 日時を 1/1/1 00:00:00 からの経過秒数に変換する。
			/// 不正な日時の場合：
			/// -- 日が月の日数より大きく 31 以下である場合 == 翌月扱い。
			/// -- それ以外の不正な日時(範囲外の日時も含む) == 1970/1/1 00:00:00 に対応する経過秒数を返す。
			/// </summary>
			/// <param name="timeStamp">日時</param>
			/// <returns>経過秒数</returns>
			public static long ToSec(long timeStamp)
			{
				if (timeStamp < TIME_STAMP_MIN || TIME_STAMP_MAX < timeStamp)
					return DEFAULT_SEC;

				int s = (int)(timeStamp % 100);
				timeStamp /= 100;
				int i = (int)(timeStamp % 100);
				timeStamp /= 100;
				int h = (int)(timeStamp % 100);
				timeStamp /= 100;
				int d = (int)(timeStamp % 100);
				timeStamp /= 100;
				int m = (int)(timeStamp % 100);
				int y = (int)(timeStamp / 100);

				if (
					//y < YEAR_MIN || YEAR_MAX < y ||
					m < 1 || 12 < m ||
					d < 1 || 31 < d ||
					h < 0 || 23 < h ||
					i < 0 || 59 < i ||
					s < 0 || 59 < s
					)
					return DEFAULT_SEC;

				if (m <= 2)
					y--;

				long ret = y / 400;
				ret *= 365 * 400 + 97;

				y %= 400;

				ret += y * 365;
				ret += y / 4;
				ret -= y / 100;

				if (2 < m)
				{
					ret -= 31 * 10 - 4;
					m -= 3;
					ret += (m / 5) * (31 * 5 - 2);
					m %= 5;
					ret += (m / 2) * (31 * 2 - 1);
					m %= 2;
					ret += m * 31;
				}
				else
				{
					ret += (m - 1) * 31;
				}
				ret += d - 1;
				ret *= 24;
				ret += h;
				ret *= 60;
				ret += i;
				ret *= 60;
				ret += s;

				return ret;
			}

			/// <summary>
			/// 1/1/1 00:00:00 からの経過秒数を日時に変換する。
			/// 不正な経過秒数の場合：
			/// -- 最小の日時(1/1/1 00:00:00)より前の日時に対応する経過秒数(つまり負の値) == 最小の日時(1/1/1 00:00:00)を返す。
			/// -- 最大の日時(922337203/12/31 23:59:59)より後の日時に対応する経過秒数 == 最大の日時(922337203/12/31 23:59:59)を返す。
			/// </summary>
			/// <param name="sec">経過秒数</param>
			/// <returns>日時</returns>
			public static long ToTimeStamp(long sec)
			{
				if (sec < 0L)
					return TIME_STAMP_MIN;

				int s = (int)(sec % 60);
				sec /= 60;
				int i = (int)(sec % 60);
				sec /= 60;
				int h = (int)(sec % 24);
				sec /= 24;

				int day = (int)(sec % 146097);
				sec /= 146097;
				sec *= 400;
				sec++;

				if (YEAR_MAX < sec)
					return TIME_STAMP_MAX;

				int y = (int)sec;
				int m = 1;
				int d;

				day += Math.Min((day + 306) / 36524, 3);
				y += (day / 1461) * 4;
				day %= 1461;

				day += Math.Min((day + 306) / 365, 3);
				y += day / 366;
				day %= 366;

				if (60 <= day)
				{
					m += 2;
					day -= 60;
					m += (day / 153) * 5;
					day %= 153;
					m += (day / 61) * 2;
					day %= 61;
				}
				m += day / 31;
				day %= 31;
				d = day + 1;

				if (y < YEAR_MIN)
					return TIME_STAMP_MIN;

				if (YEAR_MAX < y)
					return TIME_STAMP_MAX;

				if (
					//y < YEAR_MIN || YEAR_MAX < y ||
					m < 1 || 12 < m ||
					d < 1 || 31 < d ||
					h < 0 || 23 < h ||
					m < 0 || 59 < m ||
					s < 0 || 59 < s
					)
					throw null; // never

				return
					y * 10000000000L +
					m * 100000000L +
					d * 1000000L +
					h * 10000L +
					i * 100L +
					s;
			}
		}

		public static class TimeStampToSecHelper
		{
			public static bool IsFairTimeStamp(long timeStamp)
			{
				return TimeStampToSec.ToTimeStamp(TimeStampToSec.ToSec(timeStamp)) == timeStamp;
			}

			public static bool IsFairSec(long sec)
			{
				return TimeStampToSec.ToSec(TimeStampToSec.ToTimeStamp(sec)) == sec;
			}

			public static int GetDaysOfMonth(int y, int m)
			{
				if (m == 2)
				{
					if ((y % 4 == 0 && y % 100 != 0) || y % 400 == 0)
						return 29;
					else
						return 28;
				}
				else
				{
					if ((m + m / 8) % 2 == 1)
						return 31;
					else
						return 30;
				}
			}
		}

		#endregion

		/// <summary>
		/// マージする。
		/// 出力リストの配列：
		/// -- [0] == リスト1のみ存在
		/// -- [1] == 両方に存在 -- リスト1の要素を追加
		/// -- [2] == 両方に存在 -- リスト2の要素を追加
		/// -- [3] == リスト2のみ存在
		/// </summary>
		/// <typeparam name="T">任意の型</typeparam>
		/// <param name="list1">リスト1 -- 勝手にソートすることに注意！</param>
		/// <param name="list2">リスト2 -- 勝手にソートすることに注意！</param>
		/// <param name="comp">要素の比較メソッド</param>
		/// <returns>出力リストの配列</returns>
		public static List<T>[] GetMerge<T>(IList<T> list1, IList<T> list2, Comparison<T> comp)
		{
			List<T> only1 = new List<T>();
			List<T> both1 = new List<T>();
			List<T> both2 = new List<T>();
			List<T> only2 = new List<T>();

			Merge(list1, list2, comp, only1, both1, both2, only2);

			return new List<T>[]
			{
				only1,
				both1,
				both2,
				only2,
			};
		}

		/// <summary>
		/// マージする。
		/// </summary>
		/// <typeparam name="T">任意の型</typeparam>
		/// <param name="list1">リスト1 -- 勝手にソートすることに注意！</param>
		/// <param name="list2">リスト2 -- 勝手にソートすることに注意！</param>
		/// <param name="comp">要素の比較メソッド</param>
		/// <param name="only1">出力先 -- リスト1のみ存在</param>
		/// <param name="both1">出力先 -- 両方に存在 -- リスト1の要素を追加</param>
		/// <param name="both2">出力先 -- 両方に存在 -- リスト2の要素を追加</param>
		/// <param name="only2">出力先 -- リスト2のみ存在</param>
		public static void Merge<T>(IList<T> list1, IList<T> list2, Comparison<T> comp, List<T> only1, List<T> both1, List<T> both2, List<T> only2)
		{
			P_Sort(list1, comp);
			P_Sort(list2, comp);

			int index1 = 0;
			int index2 = 0;

			while (index1 < list1.Count && index2 < list2.Count)
			{
				int ret = comp(list1[index1], list2[index2]);

				if (ret < 0)
				{
					only1.Add(list1[index1++]);
				}
				else if (0 < ret)
				{
					only2.Add(list2[index2++]);
				}
				else
				{
					both1.Add(list1[index1++]);
					both2.Add(list2[index2++]);
				}
			}
			while (index1 < list1.Count)
			{
				only1.Add(list1[index1++]);
			}
			while (index2 < list2.Count)
			{
				only2.Add(list2[index2++]);
			}
		}

		private static void P_Sort<T>(IList<T> list, Comparison<T> comp)
		{
			if (list is T[])
			{
				Array.Sort((T[])list, comp);
			}
			else if (list is List<T>)
			{
				((List<T>)list).Sort(comp);
			}
			else
			{
				throw null; // never
			}
		}

		/// <summary>
		/// リスト内の特定の位置をバイナリサーチによって取得する。
		/// ★注意：指定されたリストを自動的にソートしない。
		/// 比較メソッド：
		/// -- 少なくとも以下のとおりの比較結果となること。
		/// ---- 目的位置の左側の要素 &lt; 目的位置の要素
		/// ---- 目的位置の左側の要素 &lt; 目的位置の右側の要素
		/// ---- 目的位置の要素 == 目的位置の要素
		/// ---- 目的位置の要素 &lt; 目的位置の右側の要素
		/// </summary>
		/// <typeparam name="T">要素の型</typeparam>
		/// <param name="list">検索対象のリスト</param>
		/// <param name="targetValue">目的の値</param>
		/// <param name="comp">比較メソッド</param>
		/// <returns>目的位置(見つからない場合(-1))</returns>
		public static int GetIndex<T>(IList<T> list, T targetValue, Comparison<T> comp)
		{
			return GetIndex(list, element => comp(element, targetValue));
		}

		/// <summary>
		/// リスト内の特定の位置をバイナリサーチによって取得する。
		/// ★注意：指定されたリストを自動的にソートしない。
		/// 判定メソッド：
		/// -- 目的位置の左側の要素であれば負の値を返す。
		/// -- 目的位置の右側の要素であれば正の値を返す。
		/// -- 目的位置の要素であれば 0 を返す。
		/// </summary>
		/// <typeparam name="T">要素の型</typeparam>
		/// <param name="list">検索対象のリスト</param>
		/// <param name="comp">判定メソッド</param>
		/// <returns>目的位置(見つからない場合(-1))</returns>
		public static int GetIndex<T>(IList<T> list, Func<T, int> comp)
		{
			int l = -1;
			int r = list.Count;

			while (l + 1 < r)
			{
				int m = (l + r) / 2;
				int ret = comp(list[m]);

				if (ret < 0)
				{
					l = m;
				}
				else if (0 < ret)
				{
					r = m;
				}
				else
				{
					return m;
				}
			}
			return -1; // not found
		}

		/// <summary>
		/// リスト内の範囲(開始位置と終了位置)を取得する。
		/// 戻り値を range とすると
		/// for (int index = range[0] + 1; index &lt; range[1]; index++) { T element = list[index]; ... }
		/// と廻すことで範囲内の要素を走査できる。
		/// ★注意：指定されたリストを自動的にソートしない。
		/// 比較メソッド：
		/// -- 少なくとも以下のとおりの比較結果となること。
		/// ---- 範囲の左側の要素 &lt; 範囲内の要素
		/// ---- 範囲の左側の要素 &lt; 範囲の右側の要素
		/// ---- 範囲内の要素 == 範囲内の要素
		/// ---- 範囲内の要素 &lt; 範囲の右側の要素
		/// 範囲：
		/// -- new int[] { l, r }
		/// ---- l == 範囲の開始位置の一つ前の位置_リストの最初の要素が範囲内である場合 -1 となる。
		/// ---- r == 範囲の終了位置の一つ後の位置_リストの最後の要素が範囲内である場合 list.Count となる。
		/// </summary>
		/// <typeparam name="T">要素の型</typeparam>
		/// <param name="list">検索対象のリスト</param>
		/// <param name="targetValue">範囲内の値</param>
		/// <param name="comp">比較メソッド</param>
		/// <returns>範囲</returns>
		public static int[] GetRange<T>(IList<T> list, T targetValue, Comparison<T> comp)
		{
			return GetRange(list, element => comp(element, targetValue));
		}

		/// <summary>
		/// リスト内の範囲(開始位置と終了位置)を取得する。
		/// 戻り値を range とすると
		/// for (int index = range[0] + 1; index &lt; range[1]; index++) { T element = list[index]; ... }
		/// と廻すことで範囲内の要素を走査できる。
		/// ★注意：指定されたリストを自動的にソートしない。
		/// 判定メソッド：
		/// -- 範囲の左側の要素であれば負の値を返す。
		/// -- 範囲の右側の要素であれば正の値を返す。
		/// -- 範囲内の要素であれば 0 を返す。
		/// 範囲：
		/// -- new int[] { l, r }
		/// ---- l == 範囲の開始位置の一つ前の位置_リストの最初の要素が範囲内である場合 -1 となる。
		/// ---- r == 範囲の終了位置の一つ後の位置_リストの最後の要素が範囲内である場合 list.Count となる。
		/// </summary>
		/// <typeparam name="T">要素の型</typeparam>
		/// <param name="list">検索対象のリスト</param>
		/// <param name="comp">判定メソッド</param>
		/// <returns>範囲</returns>
		public static int[] GetRange<T>(IList<T> list, Func<T, int> comp)
		{
			int l = -1;
			int r = list.Count;

			while (l + 1 < r)
			{
				int m = (l + r) / 2;
				int ret = comp(list[m]);

				if (ret < 0)
				{
					l = m;
				}
				else if (0 < ret)
				{
					r = m;
				}
				else
				{
					l = GetLeft(list, l, m, element => comp(element) < 0);
					r = GetLeft(list, m, r, element => comp(element) == 0) + 1;
					break;
				}
			}
			return new int[] { l, r };
		}

		private static int GetLeft<T>(IList<T> list, int l, int r, Predicate<T> isLeft)
		{
			while (l + 1 < r)
			{
				int m = (l + r) / 2;
				bool ret = isLeft(list[m]);

				if (ret)
				{
					l = m;
				}
				else
				{
					r = m;
				}
			}
			return l;
		}

		public static Exception ToThrow(Action routine)
		{
			try
			{
				routine();
			}
			catch (Exception ex)
			{
				return ex;
			}
			throw new Exception("例外を投げませんでした。");
		}

		public static void ToThrowPrint(Action routine)
		{
			Console.WriteLine("想定された例外：" + ToThrow(routine).Message);
		}

		#region GetOutputDir

		// memo:
		// 慣習的に C:\1, C:\2, C:\3, ... C:\999 をテンポラリ・暗黙の出力先として使用している。
		// https://github.com/stackprobe/Factory/blob/master/DevTools/zz.bat -- 定期的に全削除する運用 -- 際限なく溜まらない想定
		// あくまで個人的な慣習なので、使用する際は注意すること。

		private static Lazy<string> OutputDir = new Lazy<string>(() => GetOutputDir_Once());

		public static string GetOutputDir()
		{
			return OutputDir.Value;
		}

		private static string GetOutputDir_Once()
		{
			for (int c = 1; c <= 999; c++)
			{
				string dir = "C:\\" + c;

				if (!SCommon.IsExistsPath(dir))
				{
					SCommon.CreateDir(dir);
					return dir;
				}
			}
			throw new Exception("C:\\1 ～ 999 は使用できません。");
		}

		public static void OpenOutputDir()
		{
			SCommon.Batch(new string[] { "START " + GetOutputDir() });
		}

		public static void OpenOutputDirIfCreated()
		{
			if (OutputDir.IsValueCreated)
			{
				OpenOutputDir();
			}
		}

		private static int NOP_Count = 0;

		public static string NextOutputPath()
		{
			return Path.Combine(GetOutputDir(), (++NOP_Count).ToString("D4"));
		}

		#endregion

		public static int Pause_WaitSeconds = -1;

		public static void Pause()
		{
			if (Pause_WaitSeconds == -1)
			{
				Console.WriteLine("Press ENTER key.");
				Console.ReadLine();
			}
			else
			{
				Console.WriteLine(string.Format("Wait {0}s...", Pause_WaitSeconds));
				Thread.Sleep(Pause_WaitSeconds * 1000);
			}
		}
	}
}
