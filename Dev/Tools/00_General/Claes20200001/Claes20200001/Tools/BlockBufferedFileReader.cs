using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public class BlockBufferedFileReader : IDisposable
	{
		private const int BUFFER_SIZE = 2000000;

		private FileStream Reader;
		private long FileSize;

		public BlockBufferedFileReader(string file)
		{
			this.Reader = new FileStream(file, FileMode.Open, FileAccess.Read);
			this.FileSize = this.Reader.Length;
		}

		public long Length
		{
			get
			{
				return this.FileSize;
			}
		}

		private byte[][] Buffers = new byte[][] { null, null };
		private long[] Offsets = new long[] { -1L, -1L };
		private int BufferIndex = 1;

		public byte this[long index]
		{
			get
			{
				if (index < 0 || this.FileSize <= index)
					throw new Exception("Bad index");

				long offset = (index / (long)BUFFER_SIZE) * (long)BUFFER_SIZE;

				if (this.Offsets[this.BufferIndex] != offset)
				{
					this.BufferIndex = 1 - this.BufferIndex;

					if (this.Offsets[this.BufferIndex] != offset)
					{
						if (this.Buffers[this.BufferIndex] == null)
							this.Buffers[this.BufferIndex] = new byte[BUFFER_SIZE];

						this.Reader.Seek(offset, SeekOrigin.Begin);
						SCommon.Read(this.Reader, this.Buffers[this.BufferIndex], 0, (int)Math.Min((long)BUFFER_SIZE, this.FileSize - offset));
						this.Offsets[this.BufferIndex] = offset;
					}
				}
				return this.Buffers[this.BufferIndex][(int)(index - offset)];
			}
		}

		public void Dispose()
		{
			if (this.Reader != null)
			{
				this.Reader.Dispose();
				this.Reader = null;
			}
		}
	}
}
