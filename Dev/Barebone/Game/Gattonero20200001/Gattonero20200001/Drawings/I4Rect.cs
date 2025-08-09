using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte.Drawings
{
	/// <summary>
	/// 矩形領域
	/// LT : 左上座標
	/// XY : 中心座標
	/// RB : 右下座標
	/// W : 幅
	/// H : 高さ
	/// </summary>
	public struct I4Rect
	{
		public int L;
		public int T;
		public int W;
		public int H;

		public I4Rect(I2Point lt, I2Size size)
			: this(lt.X, lt.Y, size.W, size.H)
		{ }

		public I4Rect(int l, int t, int w, int h)
		{
			this.L = l;
			this.T = t;
			this.W = w;
			this.H = h;
		}

		public static I4Rect LTRB(int l, int t, int r, int b)
		{
			return new I4Rect(l, t, r - l, b - t);
		}

		public static I4Rect XYWH(int x, int y, int w, int h)
		{
			return new I4Rect(x - w / 2, y - h / 2, w, h);
		}

		public int R
		{
			get
			{
				return this.L + this.W;
			}
		}

		public int B
		{
			get
			{
				return this.T + this.H;
			}
		}

		public int X
		{
			get
			{
				return this.L + this.W / 2;
			}
		}

		public int Y
		{
			get
			{
				return this.T + this.H / 2;
			}
		}

		public I2Point LT
		{
			get
			{
				return new I2Point(this.L, this.T);
			}
		}

		public I2Point RT
		{
			get
			{
				return new I2Point(this.R, this.T);
			}
		}

		public I2Point RB
		{
			get
			{
				return new I2Point(this.R, this.B);
			}
		}

		public I2Point LB
		{
			get
			{
				return new I2Point(this.L, this.B);
			}
		}

		public I2Point XY
		{
			get
			{
				return new I2Point(this.L + this.W / 2, this.T + this.H / 2);
			}
		}

		public I2Size Size
		{
			get
			{
				return new I2Size(this.W, this.H);
			}
		}

		public D4Rect ToD4Rect()
		{
			return new D4Rect(this.L, this.T, this.W, this.H);
		}
	}
}
