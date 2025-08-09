using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Charlotte.Commons;
using Charlotte.Drawings;

namespace Charlotte.Tools
{
	public static class CanvasTools
	{
		/// <summary>
		/// 文字列を描画した画像を得る。
		/// </summary>
		/// <param name="text">文字列</param>
		/// <param name="fontName">フォント名</param>
		/// <param name="fontStyle">フォントスタイル</param>
		/// <param name="textColor">文字色</param>
		/// <param name="backColor">背景色</param>
		/// <param name="destH">出力画像の高さ</param>
		/// <returns>出力画像</returns>
		public static Canvas GetString(string text, string fontName, FontStyle fontStyle, I3Color textColor, I4Color backColor, int destH)
		{
			const int MAG_DRAW = 3;
			const int BOKASHI_LEVEL = 3;

			int fontSize = destH;
			fontSize *= MAG_DRAW;
			fontSize = (int)(fontSize / 1.333);

			int w;
			int h;

			using (Graphics g = Graphics.FromImage(new Bitmap(1, 1)))
			{
				SizeF size = g.MeasureString(text, new Font(fontName, fontSize, fontStyle));

				w = (int)size.Width;
				h = (int)size.Height;
			}

			Canvas canvas = new Canvas(w, h);

			Fill(canvas, new I4Color(0, 0, 0, 255));
			canvas = DrawString(canvas, text, fontSize, fontName, fontStyle, new I4Color(255, 255, 255, 255), 0, 0);
			canvas = DS_SetMargin(canvas, v => v.R == 0, BOKASHI_LEVEL, new I4Color(0, 0, 0, 255));
			DS_Blur(canvas, BOKASHI_LEVEL, textColor);
			canvas = Expand(canvas, (canvas.W * destH) / canvas.H, destH);

			Canvas dest = new Canvas(canvas.W, canvas.H);
			Fill(dest, backColor);
			DrawImage(dest, canvas, 0, 0, true);
			return dest;
		}

		/// <summary>
		/// 指定した矩形領域に文字列を描画する。
		/// </summary>
		/// <param name="dest">描画先</param>
		/// <param name="text">文字列</param>
		/// <param name="fontSize">フォントサイズ</param>
		/// <param name="fontName">フォント名</param>
		/// <param name="fontStyle">フォントスタイル</param>
		/// <param name="color">色</param>
		/// <param name="rect">描画したい領域</param>
		/// <param name="blurLv">ぼかし量(0～)</param>
		public static void DrawString(Canvas dest, string text, int fontSize, string fontName, FontStyle fontStyle, I3Color color, I4Rect rect, int blurLv)
		{
			int w;
			int h;

			using (Graphics g = Graphics.FromImage(new Bitmap(1, 1)))
			{
				SizeF size = g.MeasureString(text, new Font(fontName, fontSize, fontStyle));

				w = (int)size.Width;
				h = (int)size.Height;
			}

			Canvas canvas = new Canvas(w, h);

			Fill(canvas, new I4Color(0, 0, 0, 255));
			canvas = DrawString(canvas, text, fontSize, fontName, fontStyle, new I4Color(255, 255, 255, 255), 0, 0);
			canvas = DS_SetMargin(canvas, v => v.R == 0, blurLv, new I4Color(0, 0, 0, 255));
			DS_Blur(canvas, blurLv, color);
			canvas = Expand(canvas, rect.W, rect.H);

			DrawImage(dest, canvas, rect.L, rect.T, true);
		}

		private static Canvas DS_SetMargin(Canvas canvas, Predicate<I4Color> matchOuter, int margin, I4Color outerColor)
		{
			int x1 = int.MaxValue;
			int y1 = int.MaxValue;
			int x2 = -1; // -1 == no inner
			int y2 = -1;

			for (int x = 0; x < canvas.W; x++)
			{
				for (int y = 0; y < canvas.H; y++)
				{
					if (!matchOuter(canvas[x, y]))
					{
						x1 = Math.Min(x1, x);
						y1 = Math.Min(y1, y);
						x2 = Math.Max(x2, x);
						y2 = Math.Max(y2, y);
					}
				}
			}
			if (x2 == -1)
				throw new Exception("no inner");

			int l = x1;
			int t = y1;
			int w = x2 - x1 + 1;
			int h = y2 - y1 + 1;

			Canvas dest = new Canvas(w + margin * 2, h + margin * 2);

			Fill(dest, outerColor);

			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					dest[margin + x, margin + y] = canvas[l + x, t + y];
				}
			}
			return dest;
		}

		private static void DS_Blur(Canvas canvas, int blurLv, I3Color color)
		{
			ProcMain.WriteLog("CanvasTools-DS_Blur-ST");

			double[, ,] map = new double[2, canvas.W, canvas.H];
			int r = 0;

			for (int x = 0; x < canvas.W; x++)
			{
				for (int y = 0; y < canvas.H; y++)
				{
					map[0, x, y] = canvas[x, y].R / 255.0; // RGBどれでも良い。
				}
			}
			for (int c = 0; c < blurLv; c++)
			{
				ProcMain.WriteLog("CanvasTools-DS_Blur-c: " + c + " / " + blurLv);

				int w = 1 - r;

				for (int x = 0; x < canvas.W; x++)
				{
					for (int y = 0; y < canvas.H; y++)
					{
						double d = 0.0;
						int dc = 0;

						for (int xc = -1; xc <= 1; xc++)
						{
							for (int yc = -1; yc <= 1; yc++)
							{
								int sx = x + xc;
								int sy = y + yc;

								if (
									0 <= sx && sx < canvas.W &&
									0 <= sy && sy < canvas.H
									)
								{
									d += map[r, sx, sy];
									dc++;
								}
							}
						}
						map[w, x, y] = d / dc;
					}
				}
				r = w;
			}
			for (int x = 0; x < canvas.W; x++)
			{
				for (int y = 0; y < canvas.H; y++)
				{
					canvas[x, y] = new I4Color(color.R, color.G, color.B, SCommon.ToInt(map[r, x, y] * 255.0));
				}
			}
			ProcMain.WriteLog("CanvasTools-DS_Blur-ED");
		}

		/// <summary>
		/// 文字列を描画する。
		/// フォントサイズ：
		/// -- 文字の幅(ピクセル数) =~ 文字の高さ(ピクセル数) =~ フォントサイズ * 1.333
		/// 描画位置：
		/// -- 描画領域の左上
		/// </summary>
		/// <param name="src">元画像</param>
		/// <param name="text">文字列</param>
		/// <param name="fontSize">フォントサイズ</param>
		/// <param name="fontName">フォント名</param>
		/// <param name="fontStyle">フォントスタイル</param>
		/// <param name="color">色</param>
		/// <param name="x">描画位置X座標</param>
		/// <param name="y">描画位置Y座標</param>
		/// <returns>新しい画像</returns>
		public static Canvas DrawString(Canvas src, string text, int fontSize, string fontName, FontStyle fontStyle, I4Color color, int x, int y)
		{
			Bitmap bmp = src.ToBitmap();

			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.DrawString(text, new Font(fontName, fontSize, fontStyle), new SolidBrush(color.ToColor()), new Point(x, y));
			}
			Canvas dest = Canvas.Load(bmp);
			return dest;
		}

		/// <summary>
		/// 指定位置に画像を描画する。
		/// </summary>
		/// <param name="dest">描画先</param>
		/// <param name="src">描画する画像</param>
		/// <param name="l">描画する領域の左上座標(X軸)</param>
		/// <param name="t">描画する領域の左上座標(Y軸)</param>
		/// <param name="applyAlpha">透過率を考慮するか</param>
		public static void DrawImage(Canvas dest, Canvas src, int l, int t, bool applyAlpha)
		{
			for (int x = 0; x < src.W; x++)
			{
				for (int y = 0; y < src.H; y++)
				{
					if (applyAlpha) // 透過率を考慮する。
					{
						// ? 描画するドットが透明 -> 何も描画しないし、描画先ドットも透明だと 0-Divide になるので、何もせず次のドットへ
						if (src[x, y].A == 0)
							continue; // 次のドットへ

						D4Color dCol = dest[l + x, t + y].ToD4Color();
						D4Color sCol = src[x, y].ToD4Color();

						double da = dCol.A * (1.0 - sCol.A);
						double sa = sCol.A;
						double xa = da + sa;

						D4Color xCol = new D4Color(
							(dCol.R * da + sCol.R * sa) / xa,
							(dCol.G * da + sCol.G * sa) / xa,
							(dCol.B * da + sCol.B * sa) / xa,
							xa
							);

						dest[l + x, t + y] = xCol.ToI4Color();
					}
					else // 透過率を考慮しない。
					{
						dest[l + x, t + y] = src[x, y];
					}
				}
			}
		}

		public static Canvas Expand(Canvas src, int w, int h)
		{
			//const int SAMPLING = 4;
			//const int SAMPLING = 8;
			//const int SAMPLING = 16;
			const int SAMPLING = 24;

			return Expand(src, w, h, SAMPLING);
		}

		public static Canvas Expand(Canvas src, int w, int h, int sampling)
		{
			return Expand(src, w, h, sampling, sampling);
		}

		/// <summary>
		/// 目的のサイズに拡大・縮小する。
		/// サンプリング回数：
		/// -- 出力先の１ドットの１辺につき何回サンプリングするか
		/// </summary>
		/// <param name="src">元画像</param>
		/// <param name="w">目的の幅</param>
		/// <param name="h">目的の高さ</param>
		/// <param name="xSampling">サンプリング回数(横方向)</param>
		/// <param name="ySampling">サンプリング回数(縦方向)</param>
		/// <returns>新しい画像</returns>
		public static Canvas Expand(Canvas src, int w, int h, int xSampling, int ySampling)
		{
			ProcMain.WriteLog("CanvasTools-Expand-ST");
			ProcMain.WriteLog(string.Format("W: {0:F3} ({1} / {2}) {3}", (double)w / src.W, w, src.W, xSampling));
			ProcMain.WriteLog(string.Format("H: {0:F3} ({1} / {2}) {3}", (double)h / src.H, h, src.H, ySampling));

			Canvas dest = new Canvas(w, h);

			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					int r = 0;
					int g = 0;
					int b = 0;
					int a = 0;

					for (int xc = 0; xc < xSampling; xc++)
					{
						for (int yc = 0; yc < ySampling; yc++)
						{
							double xd = x + (xc + 0.5) / xSampling;
							double yd = y + (yc + 0.5) / ySampling;
							double xs = (xd * src.W) / w;
							double ys = (yd * src.H) / h;
							int ixs = (int)xs;
							int iys = (int)ys;

							I4Color sDot = src[ixs, iys];

							r += sDot.A * sDot.R;
							g += sDot.A * sDot.G;
							b += sDot.A * sDot.B;
							a += sDot.A;
						}
					}
					if (1 <= a)
					{
						r = SCommon.ToInt((double)r / a);
						g = SCommon.ToInt((double)g / a);
						b = SCommon.ToInt((double)b / a);
						a = SCommon.ToInt((double)a / (xSampling * ySampling));
					}
					dest[x, y] = new I4Color(r, g, b, a);
				}
			}
			ProcMain.WriteLog("CanvasTools-Expand-ED");
			return dest;
		}

		/// <summary>
		/// 指定された色でキャンバス全体を塗りつぶす。
		/// </summary>
		/// <param name="canvas">編集対象</param>
		/// <param name="color">塗りつぶす色</param>
		public static void Fill(Canvas canvas, I4Color color)
		{
			FillRect(canvas, color, new I4Rect(0, 0, canvas.W, canvas.H));
		}

		/// <summary>
		/// 指定された色で矩形領域を塗りつぶす。
		/// </summary>
		/// <param name="canvas">編集対象</param>
		/// <param name="color">塗りつぶす色</param>
		/// <param name="rect">矩形領域</param>
		public static void FillRect(Canvas canvas, I4Color color, I4Rect rect)
		{
			for (int x = rect.L; x < rect.R; x++)
			{
				for (int y = rect.T; y < rect.B; y++)
				{
					canvas[x, y] = color;
				}
			}
		}

		/// <summary>
		/// 指定された色で円を塗りつぶす。
		/// </summary>
		/// <param name="canvas">編集対象</param>
		/// <param name="color">塗りつぶす色</param>
		/// <param name="pt">円の中心</param>
		/// <param name="r">円の半径</param>
		public static void FillCircle(Canvas canvas, I4Color color, I2Point pt, int r)
		{
			int x1 = pt.X - r;
			int x2 = pt.X + r;
			int y1 = pt.Y - r;
			int y2 = pt.Y + r;

			x1 = Math.Max(x1, 0);
			x2 = Math.Min(x2, canvas.W - 1);
			y1 = Math.Max(y1, 0);
			y2 = Math.Min(y2, canvas.H - 1);

			const double R_MARGIN = 0.2;

			for (int x = x1; x <= x2; x++)
			{
				for (int y = y1; y <= y2; y++)
				{
					double d = GetDistance(new D2Point(x - pt.X, y - pt.Y));

					if (d < r + R_MARGIN)
					{
						canvas[x, y] = color;
					}
				}
			}
		}

		private static double GetDistance(D2Point pt)
		{
			return Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
		}

		public static void FilterAllDot(Canvas canvas, Func<I4Color, int, int, I4Color> filter)
		{
			FilterRect(canvas, new I4Rect(0, 0, canvas.W, canvas.H), filter);
		}

		public static void FilterRect(Canvas canvas, I4Rect rect, Func<I4Color, int, int, I4Color> filter)
		{
			for (int x = rect.L; x < rect.R; x++)
			{
				for (int y = rect.T; y < rect.B; y++)
				{
					canvas[x, y] = filter(canvas[x, y], x, y);
				}
			}
		}

		public static Canvas GetClone(Canvas src)
		{
			return GetSubImage(src, new I4Rect(0, 0, src.W, src.H));
		}

		public static Canvas GetSubImage(Canvas src, I4Rect rect)
		{
			Canvas dest = new Canvas(rect.W, rect.H);

			for (int x = 0; x < rect.W; x++)
			{
				for (int y = 0; y < rect.H; y++)
				{
					dest[x, y] = src[rect.L + x, rect.T + y];
				}
			}
			return dest;
		}

		/// <summary>
		/// 時計回りに90度回転する。
		/// </summary>
		/// <param name="src">元画像</param>
		/// <returns>新しい画像</returns>
		public static Canvas Rotate90(Canvas src)
		{
			ProcMain.WriteLog("CanvasTools-Rotate-90-ST");
			Canvas dest = new Canvas(src.H, src.W);

			for (int x = 0; x < src.W; x++)
			{
				for (int y = 0; y < src.H; y++)
				{
					dest[src.H - y - 1, x] = src[x, y];
				}
			}
			ProcMain.WriteLog("CanvasTools-Rotate-90-ED");
			return dest;
		}

		/// <summary>
		/// 時計回りに180度回転する。
		/// </summary>
		/// <param name="src">元画像</param>
		/// <returns>新しい画像</returns>
		public static Canvas Rotate180(Canvas src)
		{
			ProcMain.WriteLog("CanvasTools-Rotate-180-ST");
			Canvas dest = new Canvas(src.W, src.H);

			for (int x = 0; x < src.W; x++)
			{
				for (int y = 0; y < src.H; y++)
				{
					dest[src.W - x - 1, src.H - y - 1] = src[x, y];
				}
			}
			ProcMain.WriteLog("CanvasTools-Rotate-180-ED");
			return dest;
		}

		/// <summary>
		/// 時計回りに270度回転する。
		/// </summary>
		/// <param name="src">元画像</param>
		/// <returns>新しい画像</returns>
		public static Canvas Rotate270(Canvas src)
		{
			ProcMain.WriteLog("CanvasTools-Rotate-270-ST");
			Canvas dest = new Canvas(src.H, src.W);

			for (int x = 0; x < src.W; x++)
			{
				for (int y = 0; y < src.H; y++)
				{
					dest[y, src.W - x - 1] = src[x, y];
				}
			}
			ProcMain.WriteLog("CanvasTools-Rotate-270-ED");
			return dest;
		}

		/// <summary>
		/// ぼかす
		/// 注意：アルファ値は捨てられる。
		/// </summary>
		/// <param name="canvas">編集対象</param>
		/// <param name="level">ぼかし量(1～)</param>
		public static void Blur(Canvas canvas, int level)
		{
			ProcMain.WriteLog("CanvasTools-Blur-ST");

			double[, , ,] map = new double[2, canvas.W, canvas.H, 3];
			int r = 0;

			for (int x = 0; x < canvas.W; x++)
			{
				for (int y = 0; y < canvas.H; y++)
				{
					map[0, x, y, 0] = canvas[x, y].R / 255.0;
					map[0, x, y, 1] = canvas[x, y].G / 255.0;
					map[0, x, y, 2] = canvas[x, y].B / 255.0;
				}
			}
			for (int c = 0; c < level; c++)
			{
				ProcMain.WriteLog("CanvasTools-Blur-c: " + c + " / " + level);

				int w = 1 - r;

				for (int x = 0; x < canvas.W; x++)
				{
					for (int y = 0; y < canvas.H; y++)
					{
						for (int color = 0; color < 3; color++)
						{
							double d = 0.0;
							int dc = 0;

							for (int xc = -1; xc <= 1; xc++)
							{
								for (int yc = -1; yc <= 1; yc++)
								{
									int sx = x + xc;
									int sy = y + yc;

									if (
										0 <= sx && sx < canvas.W &&
										0 <= sy && sy < canvas.H
										)
									{
										d += map[r, sx, sy, color];
										dc++;
									}
								}
							}
							map[w, x, y, color] = d / dc;
						}
					}
				}
				r = w;
			}
			for (int x = 0; x < canvas.W; x++)
			{
				for (int y = 0; y < canvas.H; y++)
				{
					canvas[x, y] = new I4Color(
						SCommon.ToInt(map[r, x, y, 0] * 255.0),
						SCommon.ToInt(map[r, x, y, 1] * 255.0),
						SCommon.ToInt(map[r, x, y, 2] * 255.0),
						255
						);
				}
			}
			ProcMain.WriteLog("CanvasTools-Blur-ED");
		}

		/// <summary>
		/// キャンバスの四隅の色を指定してグラデーションをかける。
		/// </summary>
		/// <param name="dest">描画先</param>
		/// <param name="match">グラデーションを描画するドットか</param>
		/// <param name="ltColor">左上の色</param>
		/// <param name="rtColor">右上の色</param>
		/// <param name="rbColor">右下の色</param>
		/// <param name="lbColor">左下の色</param>
		public static void Gradation(
			Canvas dest,
			Func<I4Color, int, int, bool> match,
			I4Color ltColor,
			I4Color rtColor,
			I4Color rbColor,
			I4Color lbColor
			)
		{
			for (int x = 0; x < dest.W; x++)
			{
				for (int y = 0; y < dest.H; y++)
				{
					if (match(dest[x, y], x, y))
					{
						double xRate = (double)x / (dest.W - 1);
						double yRate = (double)y / (dest.H - 1);

						D4Color tColor = new D4Color(
							ltColor.R + xRate * (rtColor.R - ltColor.R),
							ltColor.G + xRate * (rtColor.G - ltColor.G),
							ltColor.B + xRate * (rtColor.B - ltColor.B),
							ltColor.A + xRate * (rtColor.A - ltColor.A)
							);

						D4Color bColor = new D4Color(
							lbColor.R + xRate * (rbColor.R - lbColor.R),
							lbColor.G + xRate * (rbColor.G - lbColor.G),
							lbColor.B + xRate * (rbColor.B - lbColor.B),
							lbColor.A + xRate * (rbColor.A - lbColor.A)
							);

						I4Color destColor = new I4Color(
							SCommon.ToInt(tColor.R + yRate * (bColor.R - tColor.R)),
							SCommon.ToInt(tColor.G + yRate * (bColor.G - tColor.G)),
							SCommon.ToInt(tColor.B + yRate * (bColor.B - tColor.B)),
							SCommon.ToInt(tColor.A + yRate * (bColor.A - tColor.A))
							);

						dest[x, y] = destColor;
					}
				}
			}
		}

		public static Canvas SetMargin(Canvas src, Func<I4Color, int, int, bool> matchOuter, I4Color outerColor, int margin)
		{
			return SetMargin(src, matchOuter, outerColor, margin, margin, margin, margin);
		}

		/// <summary>
		/// 画像のイメージ本体に対して指定されたマージンを適用する。
		/// </summary>
		/// <param name="src">元画像</param>
		/// <param name="matchOuter">背景(イメージ本体以外)のドットか</param>
		/// <param name="outerColor">背景色</param>
		/// <param name="margin_l">左側のマージン</param>
		/// <param name="margin_t">上側のマージン</param>
		/// <param name="margin_r">右側のマージン</param>
		/// <param name="margin_b">下側のマージン</param>
		/// <returns>新しい画像</returns>
		public static Canvas SetMargin(
			Canvas src,
			Func<I4Color, int, int, bool> matchOuter,
			I4Color outerColor,
			int margin_l,
			int margin_t,
			int margin_r,
			int margin_b
			)
		{
			int x1 = int.MaxValue;
			int y1 = int.MaxValue;
			int x2 = -1; // -1 == イメージ本体未検出
			int y2 = -1;

			for (int x = 0; x < src.W; x++)
			{
				for (int y = 0; y < src.H; y++)
				{
					if (!matchOuter(src[x, y], x, y))
					{
						x1 = Math.Min(x1, x);
						y1 = Math.Min(y1, y);
						x2 = Math.Max(x2, x);
						y2 = Math.Max(y2, y);
					}
				}
			}

			if (x2 == -1) // ? イメージ本体未検出
				throw new Exception("画像のイメージ本体を検出できませんでした。");

			I4Rect rect = I4Rect.LTRB(x1, y1, x2 + 1, y2 + 1);

			Canvas dest = new Canvas(margin_l + rect.W + margin_r, margin_t + rect.H + margin_b);

			Fill(dest, outerColor);
			DrawImage(dest, GetSubImage(src, rect), margin_l, margin_t, false);

			return dest;
		}
	}
}
