using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Charlotte.Drawings
{
	/// <summary>
	/// アルファ値の無い色を表す。
	/// 各色は 0 ～ 255 を想定する。
	/// </summary>
	public struct I3Color
	{
		public int R;
		public int G;
		public int B;

		public I3Color(int r, int g, int b)
		{
			this.R = r;
			this.G = g;
			this.B = b;
		}

		public I3Color(Color color)
		{
			this.R = color.R;
			this.G = color.G;
			this.B = color.B;
		}

		public override string ToString()
		{
			return string.Format("{0:x2}{1:x2}{2:x2}", this.R, this.G, this.B);
		}

		public I4Color WithAlpha(int a = 255)
		{
			return new I4Color(this.R, this.G, this.B, a);
		}

		public D3Color ToD3Color()
		{
			return new D3Color(
				this.R / 255.0,
				this.G / 255.0,
				this.B / 255.0
				);
		}

		public Color ToColor()
		{
			return Color.FromArgb(this.R, this.G, this.B);
		}
	}
}
