using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Charlotte.Commons;
using Charlotte.Drawings;

namespace Charlotte.Tools
{
	public class Canvas
	{
		private I4Color[,] Dots;
		public int W { get; private set; }
		public int H { get; private set; }

		public Canvas(int w, int h)
		{
			if (w < 1)
				throw new Exception("Bad w");

			if (h < 1)
				throw new Exception("Bad h");

			this.Dots = new I4Color[w, h];
			this.W = w;
			this.H = h;
		}

		public I4Color this[int x, int y]
		{
			get
			{
				return this.Dots[x, y];
			}

			set
			{
				this.Dots[x, y] = value;
			}
		}

		public static Canvas LoadFromFile(string imageFile)
		{
			using (Bitmap bmp = (Bitmap)Bitmap.FromFile(imageFile))
			{
				return Load(bmp);
			}
		}

		public static Canvas Load(Bitmap bmp)
		{
			ProcMain.WriteLog("Canvas-Load-ST");
			Canvas canvas = new Canvas(bmp.Width, bmp.Height);

			for (int x = 0; x < bmp.Width; x++)
			{
				for (int y = 0; y < bmp.Height; y++)
				{
					Color color = bmp.GetPixel(x, y);

					canvas.Dots[x, y] = new I4Color(
						color.R,
						color.G,
						color.B,
						color.A
						);
				}
			}
			ProcMain.WriteLog("Canvas-Load-ED");
			return canvas;
		}

		/// <summary>
		/// Pngとして保存します。
		/// </summary>
		/// <param name="pngFile">保存先ファイル名</param>
		public void Save(string pngFile)
		{
			this.ToBitmap().Save(pngFile, ImageFormat.Png);
		}

		/// <summary>
		/// Bmpとして保存します。
		/// </summary>
		/// <param name="bmpFile">保存先ファイル名</param>
		public void SaveAsBmp(string bmpFile)
		{
			ProcMain.WriteLog("Canvas-SaveAsBmp-ST");
			using (FileStream writer = new FileStream(bmpFile, FileMode.Create, FileAccess.Write))
			{
				int imageSize = ((this.W * 3 + 3) / 4) * 4 * this.H;

				// BFH
				writer.WriteByte(0x42); // 'B'
				writer.WriteByte(0x4d); // 'M'
				WriteUInt(writer, (uint)imageSize + 0x36);
				WriteUInt(writer, 0); // Reserved_01 + Reserved_02
				WriteUInt(writer, 0x36);

				// BFI
				WriteUInt(writer, 0x28);
				WriteUInt(writer, (uint)this.W);
				WriteUInt(writer, (uint)this.H);
				WriteUInt(writer, 0x00180001); // Planes + BitCount
				WriteUInt(writer, 0);
				WriteUInt(writer, (uint)imageSize);
				WriteUInt(writer, 0);
				WriteUInt(writer, 0);
				WriteUInt(writer, 0);
				WriteUInt(writer, 0);

				int xOdd = this.W % 4; // (4 - (this.W * 3) % 4) % 4

				for (int y = this.H - 1; 0 <= y; y--)
				{
					for (int x = 0; x < this.W; x++)
					{
						I4Color dot = this.Dots[x, y];
						byte r = (byte)dot.R;
						byte g = (byte)dot.G;
						byte b = (byte)dot.B;

						// BGR 注意
						writer.WriteByte(b);
						writer.WriteByte(g);
						writer.WriteByte(r);
					}
					for (int x = 0; x < xOdd; x++)
					{
						writer.WriteByte(0x00);
					}
				}
			}
			ProcMain.WriteLog("Canvas-SaveAsBmp-ED");
		}

		private static void WriteUInt(Stream writer, uint value)
		{
			SCommon.Write(writer, SCommon.UIntToBytes(value));
		}

		/// <summary>
		/// Jpegとして保存します。
		/// </summary>
		/// <param name="jpegFile">保存先ファイル名</param>
		/// <param name="qualityLevel">Jpegのクオリティ(0～100)</param>
		public void SaveAsJpeg(string jpegFile, int qualityLevel)
		{
			ImageCodecInfo ici = ImageCodecInfo.GetImageEncoders().First(v => v.FormatID == ImageFormat.Jpeg.Guid);
			EncoderParameter ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qualityLevel);
			EncoderParameters eps = new EncoderParameters(1);
			eps.Param[0] = ep;
			this.ToBitmap().Save(jpegFile, ici, eps);
		}

		public Bitmap ToBitmap()
		{
			ProcMain.WriteLog("Canvas-ToBitmap-ST");
			Bitmap bmp = new Bitmap(this.W, this.H);

			for (int x = 0; x < this.W; x++)
			{
				for (int y = 0; y < this.H; y++)
				{
					bmp.SetPixel(x, y, this.Dots[x, y].ToColor());
				}
			}
			ProcMain.WriteLog("Canvas-ToBitmap-ED");
			return bmp;
		}
	}
}
