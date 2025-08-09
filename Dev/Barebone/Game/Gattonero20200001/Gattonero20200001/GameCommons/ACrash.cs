using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Commons;
using Charlotte.Drawings;

namespace Charlotte.GameCommons
{
	/// <summary>
	/// 当たり判定
	/// </summary>
	public class ACrash
	{
		private enum Kind_e
		{
			NONE = 1,
			POINT,
			CIRCLE,
			RECT,
			MULTI,
		}

		private Kind_e Kind;
		private D2Point Pt;
		private double R;
		private D4Rect Rect;
		private ACrash[] Crashes;

		/// <summary>
		/// 何にも当たらないクラッシュ判定
		/// </summary>
		private static ACrash None = new ACrash()
		{
			Kind = Kind_e.NONE,
		};

		/// <summary>
		/// 何にも当たらないクラッシュ判定を返す。
		/// </summary>
		/// <returns>クラッシュ判定</returns>
		public static ACrash CreateNone()
		{
			return None;
		}

		/// <summary>
		/// 点のクラッシュ判定を作成する。
		/// </summary>
		/// <param name="pt">点の座標</param>
		/// <returns>クラッシュ判定</returns>
		public static ACrash CreatePoint(D2Point pt)
		{
			return new ACrash()
			{
				Kind = Kind_e.POINT,
				Pt = pt,
			};
		}

		/// <summary>
		/// 円のクラッシュ判定を作成する。
		/// </summary>
		/// <param name="pt">円の中心座標</param>
		/// <param name="r">円の半径</param>
		/// <returns>クラッシュ判定</returns>
		public static ACrash CreateCircle(D2Point pt, double r)
		{
			return new ACrash()
			{
				Kind = Kind_e.CIRCLE,
				Pt = pt,
				R = r,
			};
		}

		/// <summary>
		/// 矩形領域のクラッシュ判定を作成する。
		/// </summary>
		/// <param name="rect">矩形領域</param>
		/// <returns>クラッシュ判定</returns>
		public static ACrash CreateRect(D4Rect rect)
		{
			return new ACrash()
			{
				Kind = Kind_e.RECT,
				Rect = rect,
			};
		}

		/// <summary>
		/// 子クラッシュ判定をまとめたクラッシュ判定を作成する。
		/// </summary>
		/// <param name="crashes">子クラッシュ判定の配列</param>
		/// <returns>クラッシュ判定</returns>
		public static ACrash CreateMulti(params ACrash[] crashes)
		{
			return new ACrash()
			{
				Kind = Kind_e.MULTI,
				Crashes = crashes,
			};
		}

		private ACrash()
		{ }

		/// <summary>
		/// 当たり判定を行う。
		/// </summary>
		/// <param name="a">クラッシュ判定(A)</param>
		/// <param name="b">クラッシュ判定(B)</param>
		/// <returns>当たっているか</returns>
		public static bool IsCrashed(ACrash a, ACrash b)
		{
			if ((int)b.Kind < (int)a.Kind)
				SCommon.Swap(ref a, ref b);

			if (a.Kind == Kind_e.NONE)
				return false;

			if (b.Kind == Kind_e.MULTI)
				return IsCrashed_Any_Multi(a, b);

			if (a.Kind == Kind_e.POINT)
			{
				if (b.Kind == Kind_e.POINT)
					return IsCrashed_Point_Point(a.Pt, b.Pt);

				if (b.Kind == Kind_e.CIRCLE)
					return IsCrashed_Circle_Point(b.Pt, b.R, a.Pt);

				if (b.Kind == Kind_e.RECT)
					return IsCrashed_Rect_Point(b.Rect, a.Pt);
			}
			else if (a.Kind == Kind_e.CIRCLE)
			{
				if (b.Kind == Kind_e.CIRCLE)
					return IsCrashed_Circle_Circle(a.Pt, a.R, b.Pt, b.R);

				if (b.Kind == Kind_e.RECT)
					return IsCrashed_Circle_Rect(a.Pt, a.R, b.Rect);
			}
			else if (a.Kind == Kind_e.RECT)
			{
				if (b.Kind == Kind_e.RECT)
					return IsCrashed_Rect_Rect(a.Rect, b.Rect);
			}
			throw new Exception("Bad Kind");
		}

		private static bool IsCrashed_Any_Multi(ACrash a, ACrash b)
		{
			if (a.Kind == Kind_e.MULTI)
				return IsCrashed_Multi_Multi(a, b);

			foreach (ACrash crash in b.Crashes)
				if (IsCrashed(a, crash))
					return true;

			return false;
		}

		private static bool IsCrashed_Multi_Multi(ACrash a, ACrash b)
		{
			foreach (ACrash ac in a.Crashes)
				foreach (ACrash bc in b.Crashes)
					if (IsCrashed(ac, bc))
						return true;

			return false;
		}

		/// <summary>
		/// 点と点の当たり判定を行う。
		/// </summary>
		/// <param name="aPt">点(A)</param>
		/// <param name="bPt">点(B)</param>
		/// <returns>当たっているか</returns>
		public static bool IsCrashed_Point_Point(D2Point aPt, D2Point bPt)
		{
			return DD.GetDistance(aPt, bPt) < SCommon.MICRO;
		}

		/// <summary>
		/// 円と点の当たり判定を行う。
		/// </summary>
		/// <param name="aPt">円(A)の中心座標</param>
		/// <param name="aR">円(A)の半径</param>
		/// <param name="bPt">点(B)の座標</param>
		/// <returns>当たっているか</returns>
		public static bool IsCrashed_Circle_Point(D2Point aPt, double aR, D2Point bPt)
		{
			return DD.GetDistance(aPt, bPt) < aR;
		}

		/// <summary>
		/// 円と円の当たり判定を行う。
		/// </summary>
		/// <param name="aPt">円(A)の中心座標</param>
		/// <param name="aR">円(A)の半径</param>
		/// <param name="bPt">円(B)の中心座標</param>
		/// <param name="bR">円(B)の半径</param>
		/// <returns>当たっているか</returns>
		public static bool IsCrashed_Circle_Circle(D2Point aPt, double aR, D2Point bPt, double bR)
		{
			return DD.GetDistance(aPt, bPt) < aR + bR;
		}

		/// <summary>
		/// 円と矩形領域の当たり判定を行う。
		/// </summary>
		/// <param name="aPt">円(A)の中心座標</param>
		/// <param name="aR">円(A)の半径</param>
		/// <param name="bRect">矩形領域(B)</param>
		/// <returns>当たっているか</returns>
		public static bool IsCrashed_Circle_Rect(D2Point aPt, double aR, D4Rect bRect)
		{
			if (aPt.X < bRect.L) // 左
			{
				if (aPt.Y < bRect.T) // 左上
				{
					return IsCrashed_Circle_Point(aPt, aR, new D2Point(bRect.L, bRect.T));
				}
				else if (bRect.B < aPt.Y) // 左下
				{
					return IsCrashed_Circle_Point(aPt, aR, new D2Point(bRect.L, bRect.B));
				}
				else // 左中段
				{
					return bRect.L < aPt.X + aR;
				}
			}
			else if (bRect.R < aPt.X) // 右
			{
				if (aPt.Y < bRect.T) // 右上
				{
					return IsCrashed_Circle_Point(aPt, aR, new D2Point(bRect.R, bRect.T));
				}
				else if (bRect.B < aPt.Y) // 右下
				{
					return IsCrashed_Circle_Point(aPt, aR, new D2Point(bRect.R, bRect.B));
				}
				else // 右中段
				{
					return aPt.X - aR < bRect.R;
				}
			}
			else // 真上・真ん中・真下
			{
				return bRect.T - aR < aPt.Y && aPt.Y < bRect.B + aR;
			}
		}

		/// <summary>
		/// 矩形領域と点の当たり判定を行う。
		/// </summary>
		/// <param name="aRect">矩形領域(A)</param>
		/// <param name="bPt">点(B)</param>
		/// <returns>当たっているか</returns>
		public static bool IsCrashed_Rect_Point(D4Rect aRect, D2Point bPt)
		{
			return
				aRect.L < bPt.X && bPt.X < aRect.R &&
				aRect.T < bPt.Y && bPt.Y < aRect.B;
		}

		/// <summary>
		/// 矩形領域と矩形領域の当たり判定を行う。
		/// </summary>
		/// <param name="aRect">矩形領域(A)</param>
		/// <param name="bRect">矩形領域(B)</param>
		/// <returns>当たっているか</returns>
		public static bool IsCrashed_Rect_Rect(D4Rect aRect, D4Rect bRect)
		{
			return
				aRect.L < bRect.R && bRect.L < aRect.R &&
				aRect.T < bRect.B && bRect.T < aRect.B;
		}

		/// <summary>
		/// この当たり判定を表示する。
		/// </summary>
		/// <param name="color">色</param>
		public void Draw(D4Color color)
		{
			this.Draw(color, new D2Point(0, 0));
		}

		/// <summary>
		/// この当たり判定を表示する。
		/// </summary>
		/// <param name="color">色</param>
		/// <param name="camera">カメラ位置</param>
		public void Draw(D4Color color, D2Point camera)
		{
			if (this.Kind == Kind_e.MULTI)
			{
				foreach (ACrash crash in this.Crashes)
					crash.Draw(color, camera);

				return;
			}

			DD.SetAlpha(color.A);
			DD.SetBright(color.WithoutAlpha());

			switch (this.Kind)
			{
				case Kind_e.NONE:
					break;

				case Kind_e.POINT:
					DD.SetSizeWH(8.0);
					DD.Draw(Pictures.WhiteCircle, this.Pt - camera);
					break;

				case Kind_e.CIRCLE:
					DD.SetSizeWH(this.R * 2.0);
					DD.Draw(Pictures.WhiteCircle, this.Pt - camera);
					break;

				case Kind_e.RECT:
					DD.Draw(Pictures.WhiteBox, new D4Rect(this.Rect.LT - camera, this.Rect.Size));
					break;

				default:
					throw null; // never
			}
		}
	}
}
