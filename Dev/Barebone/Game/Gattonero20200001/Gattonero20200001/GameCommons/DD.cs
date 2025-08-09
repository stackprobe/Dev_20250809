using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DxLibDLL;
using Charlotte.Commons;
using Charlotte.Drawings;

namespace Charlotte.GameCommons
{
	/// <summary>
	/// ゲームに関する共通機能・便利機能はできるだけこのクラスに集約する。
	/// </summary>
	public static class DD
	{
		public static Action<Action> RunOnUIThread;
		public static List<Action> Finalizers = new List<Action>();
		public static Action Save;
		public static string MainWindowTitle;
		public static I4Rect TargetMonitor;
		public static I2Size RealScreenSize;
		public static I4Rect MainScreenDrawRect;
		public static VScreen MainScreen;
		public static VScreen LastMainScreen;
		public static int ProcFrame;
		public static int FreezeInputFrame;
		public static bool WindowIsActive;
		public static List<Func<bool>> TL = new List<Func<bool>>(); // 共通タスクリスト

		public static void SetLibbon(string message) // message: メッセージ, null == 非表示にする。
		{
			LibbonDialog.SetMessage(message);
		}

		private static Lazy<Func<string, DD.LzData>> ResFileDataGetter =
			new Lazy<Func<string, DD.LzData>>(() => GetResFileDataGetter("Resource.dat", () => GetResourceDir()));

		private static Lazy<Func<string, DD.LzData>> StorageFileDataGetter =
			new Lazy<Func<string, DD.LzData>>(() => GetResFileDataGetter("Storage.dat", () => GetStorageDir()));

		private static Func<string, DD.LzData> GetResFileDataGetter(string clusterFileName, Func<string> resourceDirGetter)
		{
			string clusterFile = Path.Combine(ProcMain.SelfDir, clusterFileName);
			Func<string, DD.LzData> getter;

			if (File.Exists(clusterFile))
			{
				ResourceCluster rc = new ResourceCluster(clusterFile);
				getter = resPath => rc.GetData(resPath);
			}
			else
			{
				getter = resPath => DD.LzData.PhysicalFile(Path.Combine(resourceDirGetter(), resPath));
			}
			return getter;
		}

		private static string GetResourceDir()
		{
			string resourceDir = @"C:\home\Resource";

			if (!Directory.Exists(resourceDir))
				throw new Exception("no resourceDir");

			return resourceDir;
		}

		private static string _storageDir = null;

		private static string GetStorageDir()
		{
			if (_storageDir == null)
				_storageDir = GetStorageDir_Main();

			return _storageDir;
		}

		private static string GetStorageDir_Main()
		{
			string storageDir = SCommon.MakeFullPath(@"..\..\..\..\Storage");

			if (!Directory.Exists(storageDir))
				throw new Exception("no storageDir");

			return storageDir;
		}

		public static DD.LzData GetResFileData(string resPath)
		{
			return ResFileDataGetter.Value(resPath);
		}

		public static DD.LzData GetStorageFileData(string resPath)
		{
			return StorageFileDataGetter.Value(resPath);
		}

		public static void SetStorageFileData(string resPath, byte[] data)
		{
			File.WriteAllBytes(Path.Combine(GetStorageDir(), resPath), data);
		}

		#region Draw

		/// <summary>
		/// 描画設定クラス
		/// 全てのフィールドはデフォルト値で初期化すること。
		/// </summary>
		private class DrawSettingInfo
		{
			public bool MosaicFlag = false;
			public int R = 255;
			public int G = 255;
			public int B = 255;
			public int A = 255;
			public double Rot = 0.0;
			public double XZoom = 1.0;
			public double YZoom = 1.0;
			public double? W = null;
			public double? H = null;
			public int Add = 0;
			public int Sub = 0;
			public int Mul = 255;
			public bool Invert = false;
		}

		/// <summary>
		/// 描画設定
		/// </summary>
		private static DrawSettingInfo DrawSetting = new DrawSettingInfo();

		/// <summary>
		/// 描画設定：
		/// -- アンチエイリアシングを行わない。
		/// </summary>
		public static void SetMosaic()
		{
			DrawSetting.MosaicFlag = true;
		}

		/// <summary>
		/// 描画設定：
		/// -- 明度をセットする。
		/// </summary>
		/// <param name="color">明度</param>
		public static void SetBright(D3Color color)
		{
			DrawSetting.R = DD.RateToByte(color.R);
			DrawSetting.G = DD.RateToByte(color.G);
			DrawSetting.B = DD.RateToByte(color.B);
		}

		/// <summary>
		/// 描画設定：
		/// -- 不透明度をセットする。
		/// </summary>
		/// <param name="a">不透明度</param>
		public static void SetAlpha(double a)
		{
			DrawSetting.A = DD.RateToByte(a);
		}

		/// <summary>
		/// 描画設定：
		/// -- 回転する角度(ラジアン角)をセットする。
		/// </summary>
		/// <param name="rot">ラジアン角</param>
		public static void SetRotate(double rot)
		{
			DrawSetting.Rot = rot;
		}

		/// <summary>
		/// 描画設定：
		/// -- 拡大率をセットする。
		/// </summary>
		/// <param name="zoom">拡大率</param>
		public static void SetZoom(double zoom)
		{
			SetZoom(zoom, zoom);
		}

		/// <summary>
		/// 描画設定：
		/// -- 拡大率をセットする。
		/// </summary>
		/// <param name="xZoom">横方向の拡大率</param>
		/// <param name="yZoom">縦方向の拡大率</param>
		public static void SetZoom(double xZoom, double yZoom)
		{
			DrawSetting.XZoom = xZoom;
			DrawSetting.YZoom = yZoom;
		}

		/// <summary>
		/// 描画設定：
		/// -- 幅と高さをセットする。
		/// </summary>
		/// <param name="wh">幅と高さ</param>
		public static void SetSizeWH(double wh)
		{
			SetSize(new D2Size(wh, wh));
		}

		/// <summary>
		/// 描画設定：
		/// -- サイズをセットする。
		/// </summary>
		/// <param name="size">サイズ</param>
		public static void SetSize(D2Size size)
		{
			SetSizeW(size.W);
			SetSizeH(size.H);
		}

		/// <summary>
		/// 描画設定：
		/// -- 幅をセットする。
		/// </summary>
		/// <param name="w">幅</param>
		public static void SetSizeW(double w)
		{
			DrawSetting.W = w;
		}

		/// <summary>
		/// 描画設定：
		/// -- 高さをセットする。
		/// </summary>
		/// <param name="h">高さ</param>
		public static void SetSizeH(double h)
		{
			DrawSetting.H = h;
		}

		/// <summary>
		/// 描画設定：
		/// -- 明度(加算)をセットする。
		/// </summary>
		/// <param name="whiteLevel">明度(-1.0～1.0)</param>
		public static void AddBright(double whiteLevel)
		{
			if (whiteLevel < 0.0)
			{
				DrawSetting.Add = 0;
				DrawSetting.Sub = DD.RateToByte(-whiteLevel);
			}
			else
			{
				DrawSetting.Add = DD.RateToByte(whiteLevel);
				DrawSetting.Sub = 0;
			}
		}

		/// <summary>
		/// 描画設定：
		/// -- 明度(乗算)をセットする。
		/// </summary>
		/// <param name="whiteLevel">明度</param>
		public static void MulBright(double whiteLevel)
		{
			DrawSetting.Mul = DD.RateToByte(whiteLevel);
		}

		/// <summary>
		/// 描画設定：
		/// -- 色を反転する。
		/// </summary>
		public static void SetInvert()
		{
			DrawSetting.Invert = true;
		}

		/// <summary>
		/// 描画する。
		/// </summary>
		/// <param name="picture">画像</param>
		/// <param name="pt">描画する位置の中心座標</param>
		public static void Draw(APicture picture, D2Point pt)
		{
			D2Size size;

			// picture.W/H, DrawSetting.XZoom/YZoom, DrawSetting.W/H.Value -> size
			{
				double w;
				double h;

				if (DrawSetting.W != null && DrawSetting.H != null)
				{
					w = DrawSetting.W.Value;
					h = DrawSetting.H.Value;
				}
				else if (DrawSetting.W != null)
				{
					w = DrawSetting.W.Value;
					h = (DrawSetting.W.Value * picture.H) / picture.W;
				}
				else if (DrawSetting.H != null)
				{
					w = (DrawSetting.H.Value * picture.W) / picture.H;
					h = DrawSetting.H.Value;
				}
				else
				{
					w = picture.W;
					h = picture.H;
				}

				w *= DrawSetting.XZoom;
				h *= DrawSetting.YZoom;

				size = new D2Size(w, h);
			}

			P4Poly poly = D4Rect.XYWH(pt.X, pt.Y, size.W, size.H).Poly;

			DD.Rotate(ref poly.LT, pt, DrawSetting.Rot);
			DD.Rotate(ref poly.RT, pt, DrawSetting.Rot);
			DD.Rotate(ref poly.RB, pt, DrawSetting.Rot);
			DD.Rotate(ref poly.LB, pt, DrawSetting.Rot);

			Draw(picture, poly);
		}

		/// <summary>
		/// 描画する。
		/// 描画設定の回転・拡大率は適用されない。
		/// </summary>
		/// <param name="picture">画像</param>
		/// <param name="rect">描画する領域</param>
		public static void Draw(APicture picture, D4Rect rect)
		{
			Draw(picture, rect.Poly);
		}

		/// <summary>
		/// 描画する。
		/// 描画設定の回転・拡大率は適用されない。
		/// </summary>
		/// <param name="picture">画像</param>
		/// <param name="poly">描画する領域</param>
		public static void Draw(APicture picture, P4Poly poly)
		{
			// 画面から遠すぎたら描画しない。
			{
				double xLw = poly.LT.X;
				double xHi = poly.LT.X;
				double yLw = poly.LT.Y;
				double yHi = poly.LT.Y;

				foreach (D2Point pt in new D2Point[] { poly.RT, poly.RB, poly.LB })
				{
					xLw = Math.Min(xLw, pt.X);
					xHi = Math.Max(xHi, pt.X);
					yLw = Math.Min(yLw, pt.Y);
					yHi = Math.Max(yHi, pt.Y);
				}

				double farL = (double)-GameConfig.ScreenSize.W;
				double farT = (double)-GameConfig.ScreenSize.H;
				double farR = (double)(GameConfig.ScreenSize.W * 2);
				double farB = (double)(GameConfig.ScreenSize.H * 2);

				if (
					xHi < farL || farR < xLw ||
					yHi < farT || farB < yLw
					)
					goto endDraw;
			}

			// 描画設定の適用ここから

			if (DrawSetting.MosaicFlag)
			{
				DX.SetDrawMode(DX.DX_DRAWMODE_NEAREST);
			}
			if (DrawSetting.R != 255 || DrawSetting.G != 255 || DrawSetting.B != 255)
			{
				DX.SetDrawBright(DrawSetting.R, DrawSetting.G, DrawSetting.B);
			}
			if (DrawSetting.A != 255)
			{
				DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, DrawSetting.A);
			}
			if (DrawSetting.Add != 0)
			{
				DX.SetDrawBlendMode(DX.DX_BLENDMODE_ADD, DrawSetting.Add);
			}
			if (DrawSetting.Sub != 0)
			{
				DX.SetDrawBlendMode(DX.DX_BLENDMODE_SUB, DrawSetting.Sub);
			}
			if (DrawSetting.Mul != 255)
			{
				DX.SetDrawBlendMode(DX.DX_BLENDMODE_MUL, DrawSetting.Mul);
			}
			if (DrawSetting.Invert)
			{
				DX.SetDrawBlendMode(DX.DX_BLENDMODE_INVSRC, 255);
			}

			// 描画設定の適用ここまで

			DX.DrawModiGraphF(
				(float)poly.LT.X,
				(float)poly.LT.Y,
				(float)poly.RT.X,
				(float)poly.RT.Y,
				(float)poly.RB.X,
				(float)poly.RB.Y,
				(float)poly.LB.X,
				(float)poly.LB.Y,
				picture.GetHandle(),
				1
				);

			// 描画設定の解除ここから

			if (DrawSetting.MosaicFlag)
			{
				DX.SetDrawMode(DX.DX_DRAWMODE_ANISOTROPIC);
			}
			if (DrawSetting.R != 255 || DrawSetting.G != 255 || DrawSetting.B != 255)
			{
				DX.SetDrawBright(255, 255, 255);
			}
			if (
				DrawSetting.A != 255 ||
				DrawSetting.Add != 0 ||
				DrawSetting.Sub != 0 ||
				DrawSetting.Mul != 255 ||
				DrawSetting.Invert
				)
			{
				DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);
			}

			// 描画設定の解除ここまで

		endDraw:
			DrawSetting = new DrawSettingInfo(); // 描画設定をリセットする。
		}

		/// <summary>
		/// シンプルな描画メソッド
		/// </summary>
		/// <param name="picture">画像</param>
		/// <param name="lt">描画する位置の左上座標</param>
		public static void DrawSimple(APicture picture, I2Point lt)
		{
			DX.DrawGraph(lt.X, lt.Y, picture.GetHandle(), 1);
		}

		#endregion

		#region Print

		public enum Alignment_e
		{
			LEFT = 1,
			CENTER,
			RIGHT,
		}

		/// <summary>
		/// 文字列の描画を初期化する。
		/// </summary>
		/// <param name="l">左座標</param>
		/// <param name="t">上座標</param>
		/// <param name="yStep">行間ステップ</param>
		/// <param name="fontName">フォント名</param>
		/// <param name="fontSize">フォントサイズ</param>
		/// <param name="alignment">アライメント</param>
		public static void SetPrint(int l, int t, int yStep, string fontName = null, int fontSize = -1, Alignment_e alignment = Alignment_e.LEFT)
		{
			if (
				l < -SCommon.IMAX || SCommon.IMAX < l ||
				t < -SCommon.IMAX || SCommon.IMAX < t ||
				yStep < 0 || SCommon.IMAX < yStep
				)
				throw new Exception("Bad params");

			Prints.L = l;
			Prints.T = t;
			Prints.YStep = yStep;
			Prints.X = 0;
			Prints.Y = 0;

			if (fontName == null && fontSize == -1) // ? デフォルトのフォントを使用する。
			{
				// none
			}
			else if (!string.IsNullOrEmpty(fontName) && SCommon.IsRange(fontSize, 1, SCommon.IMAX)) // ? フォント指定
			{
				// none
			}
			else
			{
				throw new Exception("Bad font params");
			}
			Prints.FontName = fontName;
			Prints.FontSize = fontSize;

			Prints.Color = Prints.DEFAULT_COLOR;
			Prints.BorderColor = Prints.DEFAULT_BORDER_COLOR;
			Prints.BorderSize = 0;
			Prints.Alignment = alignment;
		}

		/// <summary>
		/// 文字列の描画の設定：
		/// 文字の色を設定する。
		/// 次の文字列の描画の初期化(SetPrint)でリセットされる。
		/// </summary>
		/// <param name="color"></param>
		public static void SetPrintColor(I3Color color)
		{
			Prints.Color = color;
		}

		/// <summary>
		/// 文字列の描画の設定：
		/// 文字の輪郭を設定する。
		/// 次の文字列の描画の初期化(SetPrint)でリセットされる。
		/// </summary>
		/// <param name="color">輪郭の色</param>
		/// <param name="size">輪郭の幅</param>
		public static void SetPrintBorder(I3Color color, int size)
		{
			if (size < 1 || SCommon.IMAX < size)
				throw new Exception("Bad size");

			Prints.BorderColor = color;
			Prints.BorderSize = size;
		}

		public static void Print(string line)
		{
			if (line == null)
				throw new Exception("Bad line");

			Prints.Print(line);
		}

		public static void PrintRet()
		{
			Prints.X = 0;
			Prints.Y += Prints.YStep;
		}

		public static void PrintLine(string line)
		{
			Print(line);
			PrintRet();
		}

		private static class Prints
		{
			public static I3Color DEFAULT_COLOR = new I3Color(255, 255, 255);
			public static I3Color DEFAULT_BORDER_COLOR = new I3Color(64, 64, 64);

			public static int L = 0;
			public static int T = 0;
			public static int YStep = 20;
			public static int X = 0;
			public static int Y = 0;
			public static string FontName = null;
			public static int FontSize = -1; // -1 == デフォルトのフォントを使用する。
			public static I3Color Color = DEFAULT_COLOR;
			public static I3Color BorderColor = DEFAULT_BORDER_COLOR;
			public static int BorderSize = 0; // 0 == 文字の輪郭を描画しない。
			public static DD.Alignment_e Alignment = DD.Alignment_e.LEFT;

			public static void Print(string line)
			{
				int w = GetWidth(line);
				int sx;

				switch (Alignment)
				{
					case DD.Alignment_e.LEFT: sx = 0; break;
					case DD.Alignment_e.CENTER: sx = -w / 2; break;
					case DD.Alignment_e.RIGHT: sx = -w; break;

					default:
						throw null; // never
				}

				DrawString(line, L + X + sx, T + Y);

				X += w;
			}

			private static void DrawString(string line, int x, int y)
			{
				if (BorderSize != 0)
					for (int xc = -1; xc <= 1; xc++)
						for (int yc = -1; yc <= 1; yc++)
							if (xc != 0 || yc != 0)
								DrawString_Main(line, x + xc * BorderSize, y + yc * BorderSize, BorderColor);

				DrawString_Main(line, x, y, Color);
			}

			private static void DrawString_Main(string line, int x, int y, I3Color color)
			{
				if (FontSize == -1)
					DX.DrawString(x, y, line, DD.ToDXColor(color));
				else
					DX.DrawStringToHandle(x, y, line, DD.ToDXColor(color), DD.GetFontHandle(FontName, FontSize), 0u, 0);
			}

			private static int GetWidth(string line)
			{
				int w;

				if (FontSize == -1)
					w = DX.GetDrawStringWidth(line, SCommon.ENCODING_SJIS.GetByteCount(line));
				else
					w = DX.GetDrawStringWidthToHandle(line, SCommon.ENCODING_SJIS.GetByteCount(line), DD.GetFontHandle(FontName, FontSize), 0);

				if (w < 0 || SCommon.IMAX < w)
					throw new Exception("GetDrawStringWidth or GetDrawStringWidthToHandle failed");

				return w;
			}
		}

		#endregion

		public static void EachFrame()
		{
			DD.ExecuteTasks(DD.TL);

			DD.Curtain.EachFrame();
			AMusic.EachFrame();
			ASoundEffect.EachFrame();

			VScreen.ChangeDrawScreenToBack();

			DD.SetBright(new I3Color(0, 0, 0).ToD3Color());
			DD.Draw(Pictures.WhiteBox, new I4Rect(0, 0, DD.RealScreenSize.W, DD.RealScreenSize.H).ToD4Rect());

			int mag = DD.MainScreenDrawRect.W / GameConfig.ScreenSize.W;

			if (
				1 <= mag &&
				DD.MainScreenDrawRect.W == GameConfig.ScreenSize.W * mag &&
				DD.MainScreenDrawRect.H == GameConfig.ScreenSize.H * mag
				)
				DD.SetMosaic();

			DD.DrawMainScreen();

			GC.Collect();

			DX.ScreenFlip();

			if (DX.ProcessMessage() == -1)
				throw new DD.CoffeeBreak();

			Keep60Hz();

			SCommon.Swap(ref DD.MainScreen, ref DD.LastMainScreen);
			DD.MainScreen.ChangeDrawScreenToThis();
			DX.ClearDrawScreen();

			ProcFrame++;
			DD.Countdown(ref FreezeInputFrame);
			WindowIsActive = DX.GetActiveFlag() != 0;

			if (SCommon.IMAX < ProcFrame) // 192.9 days limit
				throw new Exception("ProcFrame counter has exceeded the limit");

			AKeyboard.EachFrame();
			AMouse.EachFrame();
			APad.EachFrame();

			// エスケープキー押下 -> ゲーム終了
			//
			if (1 <= AKeyboard.GetInput(DX.KEY_INPUT_ESCAPE))
				throw new DD.CoffeeBreak();

			// ALT+エンターキー押下 -> フルスクリーンの切り替え
			//
			if ((1 <= AKeyboard.GetInput(DX.KEY_INPUT_LALT) || 1 <= AKeyboard.GetInput(DX.KEY_INPUT_RALT)) && AKeyboard.GetInput(DX.KEY_INPUT_RETURN) == 1)
			{
				// ? 現在フルスクリーン -> フルスクリーン解除
				if (
					DD.RealScreenSize.W == DD.TargetMonitor.W &&
					DD.RealScreenSize.H == DD.TargetMonitor.H
					)
				{
					DD.SetRealScreenSize(GameSetting.UserScreenSize.W, GameSetting.UserScreenSize.H);
					GameSetting.FullScreen = false;
				}
				else // ? 現在フルスクリーンではない -> フルスクリーンにする
				{
					DD.SetRealScreenSize(DD.TargetMonitor.W, DD.TargetMonitor.H);
					GameSetting.FullScreen = true;
				}
				DD.FreezeInputFrame = 30; // エンターキーの押下がゲームに影響しないように
			}
		}

		public static int MainScreenDrawOffset_L = 0;
		public static int MainScreenDrawOffset_T = 0;

		private static void DrawMainScreen()
		{
			DX.ClearDrawScreen();

			DD.Draw(
				DD.MainScreen.GetPicture(),
				new D4Rect(
					DD.MainScreenDrawRect.L + MainScreenDrawOffset_L,
					DD.MainScreenDrawRect.T + MainScreenDrawOffset_T,
					DD.MainScreenDrawRect.W,
					DD.MainScreenDrawRect.H
					)
				);
		}

		public static bool HeavyDelayForDebug = false;

		private const long HZ_1_MICROS = 16666L;
		private const long HZ_5_MICROS = 83333L;
		private const long HZ_CHASER_DELAY = 100L;
		private const long HZ_DEBUG_DELAY = 166666L;

		private static long HzChaserTime;

		private static void Keep60Hz()
		{
			long currentTime = DD.GetCurrentTime();

			HzChaserTime += HeavyDelayForDebug ? HZ_DEBUG_DELAY : HZ_1_MICROS - HZ_CHASER_DELAY;
			HzChaserTime = SCommon.ToRange(HzChaserTime, currentTime - HZ_5_MICROS, currentTime + HZ_5_MICROS); // 前後5フレームに収める。

			while (currentTime < HzChaserTime)
			{
				DD.DrawMainScreen();
				DX.ScreenFlip();

				if (DX.ProcessMessage() == -1)
					throw new DD.CoffeeBreak();

				currentTime = DD.GetCurrentTime();
			}
		}

		public static void FreezeInput(int frame = 1)
		{
			FreezeInputFrame = frame;
		}

		public static void SetRealScreenSize(int w, int h)
		{
			if (DD.RealScreenSize.W == w && DD.RealScreenSize.H == h) // ? 今のサイズと同じ
				return;

			DD.TargetMonitor = DD.GetTargetMonitor();
			DD.SetLibbon("ウィンドウのサイズと位置を調整しています...");

			GameProcMain.SetRealScreenSize(w, h);

			DD.SetLibbon(null);
		}

		public static void DrawCurtain(double whiteLevel)
		{
			if (whiteLevel == 0.0)
				return;

			whiteLevel = SCommon.ToRange(whiteLevel, -1.0, 1.0);

			if (whiteLevel < 0.0)
			{
				DD.SetAlpha(-whiteLevel);
				DD.SetBright(new I3Color(0, 0, 0).ToD3Color());
			}
			else
			{
				DD.SetAlpha(whiteLevel);
			}
			DD.Draw(Pictures.WhiteBox, new I4Rect(0, 0, GameConfig.ScreenSize.W, GameConfig.ScreenSize.H).ToD4Rect());
		}

		public static void SetCurtain(double destWhiteLevel, int frameMax = 30) // frameMax: 変更し終わるまでのフレーム数(1～), 0 == 直ちに変更する。
		{
			Curtain.NextWhiteLevels.Clear();

			if (frameMax == 0) // ? 直ちに変更する。
			{
				Curtain.CurrWhiteLevel = destWhiteLevel;
			}
			else // ? 指定フレームかけて変更する。
			{
				foreach (AScene scene in AScene.Create(frameMax))
				{
					Curtain.NextWhiteLevels.Enqueue(DD.AToBRate(Curtain.CurrWhiteLevel, destWhiteLevel, scene.Rate));
				}
			}
		}

		private static class Curtain
		{
			public static double CurrWhiteLevel = 0.0;
			public static Queue<double> NextWhiteLevels = new Queue<double>();

			public static void EachFrame()
			{
				if (1 <= NextWhiteLevels.Count)
				{
					CurrWhiteLevel = NextWhiteLevels.Dequeue();
				}
				DD.DrawCurtain(CurrWhiteLevel);
			}
		}

		public static void Rotate(ref double x, ref double y, double rot)
		{
			double w;

			w = x * Math.Cos(rot) - y * Math.Sin(rot);
			y = x * Math.Sin(rot) + y * Math.Cos(rot);
			x = w;
		}

		public static void Rotate(ref D2Point pt, D2Point origin, double rot)
		{
			pt -= origin;

			Rotate(ref pt.X, ref pt.Y, rot);

			pt += origin;
		}

		public static double GetDistance(double x, double y)
		{
			return Math.Sqrt(x * x + y * y);
		}

		public static double GetDistance(D2Point pt, D2Point origin)
		{
			pt -= origin;

			return GetDistance(pt.X, pt.Y);
		}

		public static double GetAngle(double x, double y)
		{
#if true
			double r = Math.Atan2(y, x);

			if (r < 0.0)
				return r + Math.PI * 2.0;
			else
				return r;
#else
			if (y < 0.0) return Math.PI * 2.0 - GetAngle(x, -y);
			if (x < 0.0) return Math.PI - GetAngle(-x, y);

			if (x < y) return Math.PI / 2.0 - GetAngle(y, x);
			if (x < SCommon.MICRO) return 0.0; // 極端に原点に近い座標の場合、常に角度0(X軸正方向)を返す。

			if (y == 0.0) return 0.0;
			if (y == x) return Math.PI / 4.0;

			double r1 = 0.0;
			double r2 = Math.PI / 4.0;
			double t = y / x;
			double rm;

			for (int c = 1; ; c++)
			{
				rm = (r1 + r2) / 2.0;

				if (10 <= c)
					break;

				double rmt = Math.Tan(rm);

				if (t < rmt)
					r2 = rm;
				else
					r1 = rm;
			}
			return rm;
#endif
		}

		public static double GetAngle(D2Point pt, D2Point origin)
		{
			pt -= origin;

			return GetAngle(pt.X, pt.Y);
		}

		public static D2Point AngleToPoint(double angle, double distance)
		{
			return new D2Point(
				distance * Math.Cos(angle),
				distance * Math.Sin(angle)
				);
		}

		/// <summary>
		/// (0, 0), (0.5, 1), (1, 0) を通る放物線
		/// </summary>
		/// <param name="x">X軸の値</param>
		/// <returns>Y軸の値</returns>
		public static double Parabola(double x)
		{
			return (x - x * x) * 4.0;
		}

		/// <summary>
		/// S字曲線
		/// (0, 0), (0.5, 0.5), (1, 1) を通る曲線
		/// x &lt;= 0.5 の区間は加速(等加速)する。
		/// 0.5 &lt;= x の区間は減速(等加速)する。
		/// </summary>
		/// <param name="x">X軸の値</param>
		/// <returns>Y軸の値</returns>
		public static double SCurve(double x)
		{
			if (x < 0.5)
				return (1.0 - Parabola(x + 0.5)) * 0.5;
			else
				return (1.0 + Parabola(x - 0.5)) * 0.5;
		}

		/// <summary>
		/// 始点から終点までの間の指定レートの位置の値を返す。
		/// </summary>
		/// <param name="a">始点</param>
		/// <param name="b">終点</param>
		/// <param name="rate">レート</param>
		/// <returns>レートの値</returns>
		public static double AToBRate(double a, double b, double rate)
		{
			return a + (b - a) * rate;
		}

		/// <summary>
		/// 始点から終点までの間の指定レートの位置を返す。
		/// </summary>
		/// <param name="a">始点</param>
		/// <param name="b">終点</param>
		/// <param name="rate">レート</param>
		/// <returns>レートの位置</returns>
		public static D2Point AToBRate(D2Point a, D2Point b, double rate)
		{
			return a + (b - a) * rate;
		}

		/// <summary>
		/// 始点から終点までの間の位置をレートに変換する。
		/// </summary>
		/// <param name="a">始点</param>
		/// <param name="b">終点</param>
		/// <param name="value">位置</param>
		/// <returns>レート</returns>
		public static double RateAToB(double a, double b, double value)
		{
			return (value - a) / (b - a);
		}

		/// <summary>
		/// アスペクト比を維持して指定サイズを指定領域いっぱいに広げる。
		/// 戻り値：
		/// -- new D4Rect[] { interior, exterior }
		/// ---- interior == 指定領域の内側に張り付く拡大領域
		/// ---- exterior == 指定領域の外側に張り付く拡大領域
		/// </summary>
		/// <param name="size">指定サイズ</param>
		/// <param name="rect">指定領域</param>
		/// <returns>拡大領域の配列</returns>
		public static D4Rect[] EnlargeFull(D2Size size, D4Rect rect)
		{
			double w_h = (rect.H * size.W) / size.H; // 高さを基準にした幅
			double h_w = (rect.W * size.H) / size.W; // 幅を基準にした高さ

			D4Rect rect1;
			D4Rect rect2;

			rect1.L = rect.L + (rect.W - w_h) / 2.0;
			rect1.T = rect.T;
			rect1.W = w_h;
			rect1.H = rect.H;

			rect2.L = rect.L;
			rect2.T = rect.T + (rect.H - h_w) / 2.0;
			rect2.W = rect.W;
			rect2.H = h_w;

			D4Rect interior;
			D4Rect exterior;

			if (w_h < rect.W)
			{
				interior = rect1;
				exterior = rect2;
			}
			else
			{
				interior = rect2;
				exterior = rect1;
			}
			return new D4Rect[] { interior, exterior };
		}

		/// <summary>
		/// アスペクト比を維持して指定サイズを指定領域の内側いっぱいに広げる。
		/// スライド率：
		/// -- 0.0 ～ 1.0
		/// -- 0.0 == 拡大領域を最も左上に寄せる。指定領域と拡大領域の上辺と左側面が重なる。
		/// -- 0.5 == 中央
		/// -- 1.0 == 拡大領域を最も右下に寄せる。指定領域と拡大領域の底辺と右側面が重なる。
		/// </summary>
		/// <param name="size">指定サイズ</param>
		/// <param name="rect">指定領域</param>
		/// <param name="slideRate">スライド率</param>
		/// <returns>拡大領域</returns>
		public static D4Rect EnlargeFullInterior(D2Size size, D4Rect rect, double slideRate = 0.5)
		{
			D4Rect interior = EnlargeFull(size, rect)[0];

			interior.L = rect.L + (rect.W - interior.W) * slideRate;
			interior.T = rect.T + (rect.H - interior.H) * slideRate;

			return interior;
		}

		/// <summary>
		/// アスペクト比を維持して指定サイズを指定領域の外側いっぱいに広げる。
		/// スライド率：
		/// -- 0.0 ～ 1.0
		/// -- 0.0 == 拡大領域を最も左上に寄せる。指定領域と拡大領域の底辺と右側面が重なる。
		/// -- 0.5 == 中央
		/// -- 1.0 == 拡大領域を最も右下に寄せる。指定領域と拡大領域の上辺と左側面が重なる。
		/// </summary>
		/// <param name="size">指定サイズ</param>
		/// <param name="rect">指定領域</param>
		/// <param name="slideRate">スライド率</param>
		/// <returns>拡大領域</returns>
		public static D4Rect EnlargeFullExterior(D2Size size, D4Rect rect, double slideRate = 0.5)
		{
			D4Rect exterior = EnlargeFull(size, rect)[1];

			exterior.L = rect.L - (exterior.W - rect.W) * (1.0 - slideRate);
			exterior.T = rect.T - (exterior.H - rect.H) * (1.0 - slideRate);

			return exterior;
		}

		/// <summary>
		/// 列挙中にリストを変更しても良いような列挙子を返す。
		/// </summary>
		/// <typeparam name="T">任意の型</typeparam>
		/// <param name="list">リスト</param>
		/// <returns>列挙子</returns>
		public static IEnumerable<T> Iterate<T>(IList<T> list)
		{
			for (int index = 0; index < list.Count; index++)
			{
				yield return list[index];
			}
		}

		private const int BILLION = 1000000000;

		/// <summary>
		/// レートを十億分率に変換する。
		/// </summary>
		/// <param name="rate">レート</param>
		/// <returns>十億分率</returns>
		public static int RateToPPB(double rate)
		{
			return SCommon.ToRange(SCommon.ToInt(rate * BILLION), 0, BILLION);
		}

		/// <summary>
		/// 十億分率をレートに変換する。
		/// </summary>
		/// <param name="ppb">十億分率</param>
		/// <returns>レート</returns>
		public static double PPBToRate(int ppb)
		{
			return SCommon.ToRange((double)ppb / BILLION, 0.0, 1.0);
		}

		/// <summary>
		/// レートをバイト値(0～255)に変換する。
		/// </summary>
		/// <param name="rate">レート</param>
		/// <returns>バイト値</returns>
		public static int RateToByte(double rate)
		{
			return SCommon.ToRange(SCommon.ToInt(rate * 255.0), 0, 255);
		}

		/// <summary>
		/// バイト値(0～255)をレートに変換する。
		/// </summary>
		/// <param name="value">バイト値</param>
		/// <returns>レート</returns>
		public static double ByteToRate(int value)
		{
			return SCommon.ToRange((double)value / 255.0, 0.0, 1.0);
		}

		public static uint ToDXColor(I3Color color)
		{
			return DX.GetColor(color.R, color.G, color.B);
		}

		public static void Approach(ref double value, double target, double rate)
		{
			value -= target;
			value *= rate;
			value += target;
		}

		public static void Countdown(ref int counter)
		{
			if (0 < counter)
				counter--;
			else if (counter < 0)
				counter++;
		}

		// memo:
		// タスクの型
		// -- Func<bool>
		// タスクの戻り値の意味
		// -- True:  処理を行った。タスク継続 or 最終処理
		// -- False: 処理を行わなかった。タスク終了

		/// <summary>
		/// タスクリストを実行する。
		/// -- リスト内全てのタスクを実行する。
		/// -- 終了したタスクはリストから削除する。
		/// タスクが偽を返したら終了と見なす。
		/// このメソッド自体もタスクにできるよ。
		/// </summary>
		/// <param name="tasks">タスクリスト</param>
		/// <returns>タスクの処理を実行したか(真を返すタスクが存在したか)</returns>
		public static bool ExecuteTasks(List<Func<bool>> tasks)
		{
			for (int index = 0; index < tasks.Count; index++)
				if (!tasks[index]())
					tasks[index] = null;

			tasks.RemoveAll(v => v == null);
			return 1 <= tasks.Count;
		}

		/// <summary>
		/// タスクシーケンスを実行する。
		/// -- リストの先頭のタスクのみ実行する。
		/// -- 終了したタスクはリストから削除する。
		/// タスクが偽を返したら終了と見なす。
		/// このメソッド自体もタスクにできるよ。
		/// </summary>
		/// <param name="tasks">タスクシーケンス</param>
		/// <returns>タスクの処理を実行したか(真を返すタスクが存在したか)</returns>
		public static bool ExecuteTaskSequence(Queue<Func<bool>> tasks)
		{
			while (1 <= tasks.Count && !tasks.Peek()())
				tasks.Dequeue();

			return 1 <= tasks.Count;
		}

		/// <summary>
		/// 指定処理を1度だけ実行するタスクを返す。
		/// </summary>
		/// <param name="routine">指定処理</param>
		/// <returns>指定処理を1度だけ実行するタスク</returns>
		public static Func<bool> Once(Action routine)
		{
			bool done = false;

			return () =>
			{
				if (done)
					return false;

				routine();
				done = true;
				return true;
			};
		}

		/// <summary>
		/// 指定処理を指定回数(指定フレーム)待ってから1度だけ実行するタスクを返す。
		/// </summary>
		/// <param name="delayFrame">待ち回数(待ちフレーム)</param>
		/// <param name="routine">指定処理</param>
		/// <returns>指定処理を指定回数(指定フレーム)待ってから1度だけ実行するタスク</returns>
		public static Func<bool> Delay(int delayFrame, Action routine)
		{
			int execFrame = DD.ProcFrame + delayFrame;

			return () =>
			{
				if (DD.ProcFrame < execFrame)
				{
					return true;
				}
				else if (DD.ProcFrame == execFrame)
				{
					routine();
					return true;
				}
				else
				{
					return false;
				}
			};
		}

		/// <summary>
		/// 指定処理を指定回数(指定フレーム)実行するタスクを返す。
		/// </summary>
		/// <param name="keepFrame">実行回数(実行フレーム)</param>
		/// <param name="routine">指定処理</param>
		/// <returns>指定処理を指定回数(指定フレーム)実行するタスク</returns>
		public static Func<bool> Keep(int keepFrame, Action routine)
		{
			int endFrame = DD.ProcFrame + keepFrame;

			return () =>
			{
				if (DD.ProcFrame <= endFrame)
				{
					routine();
					return true;
				}
				else
				{
					return false;
				}
			};
		}

		/// <summary>
		/// ぼかしパラメータ
		/// 100で約1ピクセル分の幅とのこと。
		/// </summary>
		private const int BOKASHI_PARAM_MAX = 5000;

		/// <summary>
		/// 描画先スクリーンをぼかす。
		/// </summary>
		/// <param name="rate">ぼかしレート</param>
		public static void Blur(double rate)
		{
			DX.GraphFilter(
				VScreen.GetCurrentDrawScreenHandle(),
				DX.DX_GRAPH_FILTER_GAUSS,
				16,
				SCommon.ToInt((double)BOKASHI_PARAM_MAX * rate)
				);
		}

		/// <summary>
		/// ゲーム中にゲームを中断するにはこの例外を投げること。
		/// </summary>
		public class CoffeeBreak : Exception
		{ }

		private static Lazy<WorkingDir> _wd = new Lazy<WorkingDir>(() => new WorkingDir());

		/// <summary>
		/// 各機能自由に使ってよい作業フォルダ
		/// </summary>
		public static WorkingDir WD
		{
			get
			{
				return _wd.Value;
			}
		}

		/// <summary>
		/// 各機能自由に使ってよいスクリーン
		/// </summary>
		public static VScreen FreeScreen = new VScreen(GameConfig.ScreenSize.W, GameConfig.ScreenSize.H);

		/// <summary>
		/// 全てのリソースを解放する。
		/// ゲームの開始時、ゲーム中に適宜呼び出すこと。
		/// </summary>
		public static void Touch()
		{
			UnloadAll();
		}

		/// <summary>
		/// 全てのリソースを解放する。
		/// ゲームの終了時に呼び出すこと。
		/// </summary>
		public static void Detach()
		{
			UnloadAll();
		}

		/// <summary>
		/// ロードされた全てのリソースを解放する。
		/// 定期的にこれを呼び出してメモリを使い果たさないようにすると吉！
		/// 注意：スクリーンの内容は失われる。
		/// </summary>
		private static void UnloadAll()
		{
			APicture.UnloadAll();
			VScreen.UnloadAll();
			DD.UnloadAllFontHandle();
			AMusic.TryUnloadAll();
			ASoundEffect.TryUnloadAll();
		}

		public static void Pin<T>(T data)
		{
			GCHandle h = GCHandle.Alloc(data, GCHandleType.Pinned);

			DD.Finalizers.Add(() =>
			{
				h.Free();
			});
		}

		public static void PinOn<T>(T data, Action<IntPtr> routine)
		{
			GCHandle pinnedData = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				routine(pinnedData.AddrOfPinnedObject());
			}
			finally
			{
				pinnedData.Free();
			}
		}

		public class Collector<T>
		{
			private List<T> Inner = new List<T>();

			public void Add(T element)
			{
				this.Inner.Add(element);
			}

			public IEnumerable<T> Iterate()
			{
				return DD.Iterate(this.Inner);
			}
		}

		private static I2Point GetMousePosition()
		{
			return new I2Point(Cursor.Position.X, Cursor.Position.Y);
		}

		private static I4Rect[] Monitors = null;

		private static I4Rect[] GetAllMonitor()
		{
			if (Monitors == null)
			{
				Monitors = Screen.AllScreens.Select(screen => new I4Rect(
					screen.Bounds.Left,
					screen.Bounds.Top,
					screen.Bounds.Width,
					screen.Bounds.Height
					))
					.ToArray();
			}
			return Monitors;
		}

		private static I2Point GetMainWindowPosition()
		{
			Win32APIWrapper.POINT p;

			p.X = 0;
			p.Y = 0;

			Win32APIWrapper.W_ClientToScreen(Win32APIWrapper.GetMainWindowHandle(), out p);

			return new I2Point(p.X, p.Y);
		}

		private static I2Point GetMainWindowCenterPosition()
		{
			I2Point p = GetMainWindowPosition();

			p.X += DD.RealScreenSize.W / 2;
			p.Y += DD.RealScreenSize.H / 2;

			return p;
		}

		/// <summary>
		/// 起動時におけるターゲット画面を取得する。
		/// </summary>
		/// <returns>画面の領域</returns>
		public static I4Rect GetTargetMonitor_Boot()
		{
			I2Point mousePos = GetMousePosition();

			foreach (I4Rect monitor in GetAllMonitor())
			{
				if (
					monitor.L <= mousePos.X && mousePos.X < monitor.R &&
					monitor.T <= mousePos.Y && mousePos.Y < monitor.B
					)
					return monitor;
			}
			return GetAllMonitor()[0]; // 何故か見つからない -> 適当な画面を返す。
		}

		/// <summary>
		/// 現在のターゲット画面を取得する。
		/// </summary>
		/// <returns>画面の領域</returns>
		public static I4Rect GetTargetMonitor()
		{
			I2Point mainWinCenterPt = GetMainWindowCenterPosition();

			foreach (I4Rect monitor in GetAllMonitor())
			{
				if (
					monitor.L <= mainWinCenterPt.X && mainWinCenterPt.X < monitor.R &&
					monitor.T <= mainWinCenterPt.Y && mainWinCenterPt.Y < monitor.B
					)
					return monitor;
			}
			return GetAllMonitor()[0]; // 何故か見つからない -> 適当な画面を返す。
		}

		public static void SetMainWindowPosition(int l, int t)
		{
			DX.SetWindowPosition(l, t);

			I2Point p = DD.GetMainWindowPosition();

			l += l - p.X;
			t += t - p.Y;

			DX.SetWindowPosition(l, t);
		}

		/// <summary>
		/// コンピュータを起動してから経過した時間を返す。
		/// 単位：マイクロ秒(0.001ミリ秒)
		/// </summary>
		/// <returns>時間(マイクロ秒)</returns>
		public static long GetCurrentTime()
		{
			return DX.GetNowHiPerformanceCount();
		}

		public static bool IsSoundPlaying(int handle)
		{
			bool playing;

			switch (DX.CheckSoundMem(handle))
			{
				case 0: // 停止中
					playing = false;
					break;

				case 1: // 再生中
					playing = true;
					break;

				default:
					throw new Exception("CheckSoundMem failed");
			}
			return playing;
		}

		public static APicture.PictureDataInfo GetPictureData(byte[] fileData)
		{
			if (fileData == null)
				throw new Exception("Bad fileData (null)");

			if (fileData.Length == 0)
				throw new Exception("Bad fileData (zero bytes)");

			int softImage = -1;

			DD.PinOn(fileData, p => softImage = DX.LoadSoftImageToMem(p, fileData.Length));

			if (softImage == -1)
				throw new Exception("LoadSoftImageToMem failed");

			int w;
			int h;

			if (DX.GetSoftImageSize(softImage, out w, out h) != 0) // ? 失敗
				throw new Exception("GetSoftImageSize failed");

			if (w < 1 || SCommon.IMAX < w)
				throw new Exception("Bad w");

			if (h < 1 || SCommon.IMAX < h)
				throw new Exception("Bad h");

			// RGB -> RGBA
			{
				int newSoftImage = DX.MakeARGB8ColorSoftImage(w, h);

				if (newSoftImage == -1) // ? 失敗
					throw new Exception("MakeARGB8ColorSoftImage failed");

				if (DX.BltSoftImage(0, 0, w, h, softImage, 0, 0, newSoftImage) != 0) // ? 失敗
					throw new Exception("BltSoftImage failed");

				if (DX.DeleteSoftImage(softImage) != 0) // ? 失敗
					throw new Exception("DeleteSoftImage failed");

				softImage = newSoftImage;
			}

			int handle = DX.CreateGraphFromSoftImage(softImage);

			if (handle == -1) // ? 失敗
				throw new Exception("CreateGraphFromSoftImage failed");

			if (DX.DeleteSoftImage(softImage) != 0) // ? 失敗
				throw new Exception("DeleteSoftImage failed");

			return new APicture.PictureDataInfo()
			{
				Handle = handle,
				W = w,
				H = h,
			};
		}

		#region Font

		public static void AddFontFile(string resPath)
		{
			string dir = DD.WD.MakePath();
			string file = Path.Combine(dir, Path.GetFileName(resPath));
			byte[] fileData = DD.GetResFileData(resPath).Data.Value;

			SCommon.CreateDir(dir);
			File.WriteAllBytes(file, fileData);

			P_AddFontFile(file);

			DD.Finalizers.Add(() => P_RemoveFontFile(file));
		}

		private static void P_AddFontFile(string file)
		{
			if (Win32APIWrapper.W_AddFontResourceEx(file, Win32APIWrapper.FR_PRIVATE, IntPtr.Zero) == 0) // ? 失敗
				throw new Exception("W_AddFontResourceEx failed");
		}

		private static void P_RemoveFontFile(string file)
		{
			UnloadAllFontHandle(); // 個別フォントの削除なのでこのフォントだけ解放したいが、面倒なので全フォント解放する。

			if (Win32APIWrapper.W_RemoveFontResourceEx(file, Win32APIWrapper.FR_PRIVATE, IntPtr.Zero) == 0) // ? 失敗
				throw new Exception("W_RemoveFontResourceEx failed");
		}

		public static int GetFontHandle(string fontName, int fontSize)
		{
			if (string.IsNullOrEmpty(fontName))
				throw new Exception("Bad fontName");

			if (fontSize < 1 || SCommon.IMAX < fontSize)
				throw new Exception("Bad fontSize");

			return Fonts.GetHandle(fontName, fontSize);
		}

		public static void UnloadAllFontHandle()
		{
			Fonts.UnloadAll();
		}

		private static class Fonts
		{
			private static Dictionary<string, int> Handles = SCommon.CreateDictionary<int>();

			private static string GetKey(string fontName, int fontSize)
			{
				return string.Join("_", fontName, fontSize);
			}

			public static int GetHandle(string fontName, int fontSize)
			{
				string key = GetKey(fontName, fontSize);

				if (!Handles.ContainsKey(key))
					Handles.Add(key, CreateHandle(fontName, fontSize));

				return Handles[key];
			}

			public static void UnloadAll()
			{
				foreach (int handle in Handles.Values)
					ReleaseHandle(handle);

				Handles.Clear();
			}

			private static int CreateHandle(string fontName, int fontSize)
			{
				int handle = DX.CreateFontToHandle(
					fontName,
					fontSize,
					6,
					DX.DX_FONTTYPE_ANTIALIASING_8X8,
					-1,
					0
					);

				if (handle == -1) // ? 失敗
					throw new Exception("CreateFontToHandle failed");

				return handle;
			}

			private static void ReleaseHandle(int handle)
			{
				if (DX.DeleteFontToHandle(handle) != 0) // ? 失敗
					throw new Exception("DeleteFontToHandle failed");
			}
		}

		#endregion

		public static void UpdateButtonCounter(ref int counter, bool status)
		{
			if (1 <= counter) // ? 前回は押していた。
			{
				if (status) // ? 今回も押している。
				{
					counter++; // 押している。
				}
				else // ? 今回は離している。
				{
					counter = -1; // 離し始めた。
				}
			}
			else // ? 前回は離していた。
			{
				if (status) // ? 今回は押している。
				{
					counter = 1; // 押し始めた。
				}
				else // ? 今回も離している。
				{
					counter = 0; // 離している。
				}
			}
		}

		private const int POUND_FIRST_DELAY = 17;
		private const int POUND_AFTER_DELAY = 4;

		public static bool IsPound(int count)
		{
			return count == 1 || POUND_FIRST_DELAY < count && (count - POUND_FIRST_DELAY) % POUND_AFTER_DELAY == 1;
		}

		public static bool IsPound(int count, int firstDelay, int afterDelay)
		{
			return count == 1 || firstDelay < count && (count - firstDelay) % afterDelay == 1;
		}

		public static class SaveDataFileFormatter
		{
			private const int SEGMENT_SIZE = 80;

			public static byte[] Encode(byte[] data)
			{
				if (data == null)
					throw new Exception("Bad data");

				using (MemoryStream mem = new MemoryStream())
				{
					for (int index = 0; index < data.Length; index++)
					{
						if (1 <= index && index % SEGMENT_SIZE == 0)
						{
							mem.WriteByte(0x0d); // CR
							mem.WriteByte(0x0a); // LF
						}
						mem.WriteByte(data[index]);
					}
					if (data.Length % SEGMENT_SIZE != 0)
					{
						int count = SEGMENT_SIZE - data.Length % SEGMENT_SIZE;

						while (0 <= --count)
						{
							mem.WriteByte((byte)'!');
						}
					}
					mem.WriteByte(0x0d); // CR
					mem.WriteByte(0x0a); // LF

					return mem.ToArray();
				}
			}

			public static byte[] Decode(byte[] data)
			{
				if (data == null)
					throw new Exception("Bad data");

				return data.Where(chr => (byte)'+' <= chr && chr <= (byte)'z').ToArray();
			}
		}

		public static class Hasher
		{
			private static byte[] COUNTER_SHUFFLE = Encoding.ASCII.GetBytes("Gattonero-2023-04-05_COUNTER_SHUFFLE_{e43e01aa-ca4f-43d3-8be7-49cd60e9415e}_");
			private const int HASH_SIZE = 20;

			public static byte[] AddHash(byte[] data)
			{
				if (data == null)
					throw new Exception("Bad data");

				return SCommon.Join(new byte[][] { GetHash(data), data });
			}

			public static byte[] UnaddHash(byte[] data)
			{
				try
				{
					return UnaddHash_Main(data);
				}
				catch (Exception ex)
				{
					throw new Exception("読み込まれたデータは破損しているかバージョンが異なります。", ex);
				}
			}

			private static byte[] UnaddHash_Main(byte[] data)
			{
				if (data == null)
					throw new Exception("Bad data");

				if (data.Length < HASH_SIZE)
					throw new Exception("Bad Length");

				byte[] hash = SCommon.GetPart(data, 0, HASH_SIZE);
				byte[] retData = SCommon.GetPart(data, HASH_SIZE);
				byte[] recalcedHash = GetHash(retData);

				if (SCommon.Comp(hash, recalcedHash, SCommon.Comp) != 0)
					throw new Exception("Bad hash");

				return retData;
			}

			private static byte[] GetHash(byte[] data)
			{
				byte[] hash = Encoding.ASCII.GetBytes(SCommon.Base64.I.Encode(SCommon.GetSHA512(new byte[][] { COUNTER_SHUFFLE, data }).Take(15).ToArray()));

				if (hash.Length != HASH_SIZE) // 2bs
					throw null; // never

				return hash;
			}
		}

		public class LzData
		{
			public readonly int Length;
			public readonly Lazy<byte[]> Data;

			public static LzData PhysicalFile(string file)
			{
				file = SCommon.ToFullPath(file);

				if (!File.Exists(file))
					throw new Exception("no file: " + file);

				FileInfo info = new FileInfo(file);

				if ((long)int.MaxValue < info.Length)
					throw new Exception("Bad file: " + file);

				return new LzData((int)info.Length, () => File.ReadAllBytes(file));
			}

			public LzData(int length, Func<byte[]> getData)
			{
				this.Length = length;
				this.Data = new Lazy<byte[]>(getData);
			}
		}

		public static bool IsOutOf(D4Rect rect, D2Point pt, double margin = 0.0)
		{
			return
				pt.X < rect.L - margin || rect.R + margin < pt.X ||
				pt.Y < rect.T - margin || rect.B + margin < pt.Y;
		}
	}
}
