using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Commons;

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
	public struct D4Rect
	{
		public double L;
		public double T;
		public double W;
		public double H;

		public D4Rect(D2Point lt, D2Size size)
			: this(lt.X, lt.Y, size.W, size.H)
		{ }

		public D4Rect(double l, double t, double w, double h)
		{
			this.L = l;
			this.T = t;
			this.W = w;
			this.H = h;
		}

		public static D4Rect LTRB(double l, double t, double r, double b)
		{
			return new D4Rect(l, t, r - l, b - t);
		}

		public static D4Rect XYWH(double x, double y, double w, double h)
		{
			return new D4Rect(x - w / 2.0, y - h / 2.0, w, h);
		}

		public double R
		{
			get
			{
				return this.L + this.W;
			}
		}

		public double B
		{
			get
			{
				return this.T + this.H;
			}
		}

		public double X
		{
			get
			{
				return this.L + this.W / 2.0;
			}
		}

		public double Y
		{
			get
			{
				return this.T + this.H / 2.0;
			}
		}

		public D2Point LT
		{
			get
			{
				return new D2Point(this.L, this.T);
			}
		}

		public D2Point RT
		{
			get
			{
				return new D2Point(this.R, this.T);
			}
		}

		public D2Point RB
		{
			get
			{
				return new D2Point(this.R, this.B);
			}
		}

		public D2Point LB
		{
			get
			{
				return new D2Point(this.L, this.B);
			}
		}

		public D2Point XY
		{
			get
			{
				return new D2Point(this.L + this.W / 2.0, this.T + this.H / 2.0);
			}
		}

		public P4Poly Poly
		{
			get
			{
				return new P4Poly(this.LT, this.RT, this.RB, this.LB);
			}
		}

		public D2Size Size
		{
			get
			{
				return new D2Size(this.W, this.H);
			}
		}

		public I4Rect ToI4Rect()
		{
			return new I4Rect(
				SCommon.ToInt(this.L),
				SCommon.ToInt(this.T),
				SCommon.ToInt(this.W),
				SCommon.ToInt(this.H)
				);
		}
	}
}
