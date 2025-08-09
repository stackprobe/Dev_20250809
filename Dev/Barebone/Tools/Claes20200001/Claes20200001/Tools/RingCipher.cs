using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	/// <summary>
	/// メモリブロックの暗号化・復号を行う。
	/// memo: RingCipher.cs と RingCipherFile.cs は同じ暗号化・復号を行うのでデータ互換性がある。
	/// </summary>
	public class RingCipher : IDisposable
	{
		private AESCipher[] Transformers;

		/// <summary>
		/// 鍵の分割：
		/// --  16 --> 16
		/// --  24 --> 24
		/// --  32 --> 32
		/// --  40 --> 24, 16
		/// --  48 --> 32, 16
		/// --  56 --> 32, 24
		/// --  64 --> 32, 32
		/// --  72 --> 32, 24, 16
		/// --  80 --> 32, 32, 16
		/// --  88 --> 32, 32, 24
		/// --  96 --> 32, 32, 32
		/// -- 104 --> 32, 32, 24, 16
		/// -- 112 --> 32, 32, 32, 16
		/// -- 120 --> 32, 32, 32, 24
		/// -- 128 --> 32, 32, 32, 32
		/// -- ...
		/// </summary>
		/// <param name="rawKey">鍵</param>
		public RingCipher(byte[] rawKey)
		{
			if (
				rawKey == null ||
				rawKey.Length < 16 ||
				rawKey.Length % 8 != 0
				)
				throw new Exception("Bad rawKey");

			List<AESCipher> dest = new List<AESCipher>();

			for (int offset = 0; offset < rawKey.Length;)
			{
				int size = rawKey.Length - offset;

				if (48 <= size)
					size = 32;
				else if (size == 40)
					size = 24;

				dest.Add(new AESCipher(SCommon.GetPart(rawKey, offset, size)));
				offset += size;
			}
			this.Transformers = dest.ToArray();
		}

		public void Dispose()
		{
			if (this.Transformers != null)
			{
				foreach (AESCipher transformer in this.Transformers)
					transformer.Dispose();

				this.Transformers = null;
			}
		}

		/// <summary>
		/// 暗号化を行う。
		/// </summary>
		/// <param name="data">入力データ</param>
		/// <returns>出力データ</returns>
		public byte[] Encrypt(byte[] data)
		{
			if (data == null)
				throw new Exception("Bad data");

			data = AddPadding(data);
			data = AddCRandPart(data, 64);
			data = AddHash(data);
			data = AddCRandPart(data, 16);

			foreach (AESCipher transformer in this.Transformers)
				EncryptRingCBC(data, transformer);

			return data;
		}

		/// <summary>
		/// 復号を行う。
		/// データの破損や鍵の不一致も含め復号に失敗すると例外を投げる。
		/// </summary>
		/// <param name="data">入力データ</param>
		/// <returns>出力データ</returns>
		public byte[] Decrypt(byte[] data)
		{
			if (
				data == null ||
				data.Length < 16 + 64 + 64 + 16 || // ? AddPadding-したデータ_(最短)16 + cRandPart_64 + hash_64 + cRandPart_16 より短い
				data.Length % 16 != 0
				)
				throw new Exception("入力データの破損を検出しました。");

			data = SCommon.GetPart(data, 0, data.Length); // 複製

			foreach (AESCipher transformer in this.Transformers.Reverse())
				DecryptRingCBC(data, transformer);

			data = RemoveCRandPart(data, 16);
			data = RemoveHash(data);
			data = RemoveCRandPart(data, 64);
			data = RemovePadding(data);
			return data;
		}

		private static byte[] AddPadding(byte[] data)
		{
			int size = 16 - data.Length % 16;
			byte[] padding = SCommon.CRandom.GetBytes(size);
			size--;
			padding[size] &= 0xf0;
			padding[size] |= (byte)size;
			data = SCommon.Join(new byte[][] { data, padding });
			return data;
		}

		private static byte[] RemovePadding(byte[] data)
		{
			int size = data[data.Length - 1] & 0x0f;
			size++;
			data = SCommon.GetPart(data, 0, data.Length - size);
			return data;
		}

		private static byte[] AddCRandPart(byte[] data, int size)
		{
			byte[] padding = SCommon.CRandom.GetBytes(size);
			data = SCommon.Join(new byte[][] { data, padding });
			return data;
		}

		private static byte[] RemoveCRandPart(byte[] data, int size)
		{
			data = SCommon.GetPart(data, 0, data.Length - size);
			return data;
		}

		private const int HASH_SIZE = 64;

		private static byte[] AddHash(byte[] data)
		{
			byte[] hash = SCommon.GetSHA512(data);

			if (hash.Length != HASH_SIZE)
				throw null; // never

			data = SCommon.Join(new byte[][] { data, hash });
			return data;
		}

		private static byte[] RemoveHash(byte[] data)
		{
			byte[] hash = SCommon.GetPart(data, data.Length - HASH_SIZE, HASH_SIZE);
			data = SCommon.GetPart(data, 0, data.Length - HASH_SIZE);
			byte[] recalcHash = SCommon.GetSHA512(data);

			if (SCommon.Comp(hash, recalcHash, SCommon.Comp) != 0)
				throw new Exception("入力データの破損または鍵の不一致を検出しました。");

			return data;
		}

		private static void EncryptRingCBC(byte[] data, AESCipher transformer)
		{
			byte[] input = new byte[16];
			byte[] output = new byte[16];

			Array.Copy(data, data.Length - 16, output, 0, 16);

			for (int offset = 0; offset < data.Length; offset += 16)
			{
				Array.Copy(data, offset, input, 0, 16);
				XorBlock(input, output);
				transformer.EncryptBlock(input, output);
				Array.Copy(output, 0, data, offset, 16);
			}
		}

		private static void DecryptRingCBC(byte[] data, AESCipher transformer)
		{
			byte[] input = new byte[16];
			byte[] output = new byte[16];

			Array.Copy(data, data.Length - 16, input, 0, 16);

			for (int offset = data.Length - 16; 0 <= offset; offset -= 16)
			{
				transformer.DecryptBlock(input, output);
				Array.Copy(data, (offset + data.Length - 16) % data.Length, input, 0, 16);
				XorBlock(output, input);
				Array.Copy(output, 0, data, offset, 16);
			}
		}

		private static void XorBlock(byte[] data, byte[] maskData)
		{
			for (int index = 0; index < 16; index++)
				data[index] ^= maskData[index];
		}
	}
}
