using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DxLibDLL;
using Charlotte.Commons;
using Charlotte.Drawings;

namespace Charlotte.GameCommons
{
	public static class AMouse
	{
		private static int P_Rot = 0;

		public static int Rot
		{
			get
			{
				return P_Rot;
			}
		}

		public class Button
		{
			public int Status = 0;

			// MEMO: ボタン・キー押下は 1 マウス押下は -1 で判定する。

			public int GetInput()
			{
				return DD.FreezeInputFrame != 0 ? 0 : this.Status;
			}
		}

		public static Button L = new Button();
		public static Button R = new Button();
		public static Button M = new Button();

		private static I2Point P_Position = new I2Point(0, 0);

		public static I2Point Position
		{
			get
			{
				return P_Position;
			}

			set
			{
				SetMousePosition(value);
			}
		}

		public static void EachFrame()
		{
			int status;

			if (DD.WindowIsActive)
			{
				P_Rot = DX.GetMouseWheelRotVol();
				status = DX.GetMouseInput();
			}
			else
			{
				P_Rot = 0;
				status = 0;
			}

			P_Rot = SCommon.ToRange(P_Rot, -SCommon.IMAX, SCommon.IMAX);

			DD.UpdateButtonCounter(ref L.Status, (status & DX.MOUSE_INPUT_LEFT) != 0);
			DD.UpdateButtonCounter(ref R.Status, (status & DX.MOUSE_INPUT_RIGHT) != 0);
			DD.UpdateButtonCounter(ref M.Status, (status & DX.MOUSE_INPUT_MIDDLE) != 0);

			int x;
			int y;

			if (DX.GetMousePoint(out x, out y) != 0) // ? 失敗
				throw new Exception("GetMousePoint failed");

			x -= DD.MainScreenDrawRect.L;
			x *= GameConfig.ScreenSize.W;
			x /= DD.MainScreenDrawRect.W;
			y -= DD.MainScreenDrawRect.T;
			y *= GameConfig.ScreenSize.H;
			y /= DD.MainScreenDrawRect.H;

			x = SCommon.ToRange(x, 0, GameConfig.ScreenSize.W - 1);
			y = SCommon.ToRange(y, 0, GameConfig.ScreenSize.H - 1);

			P_Position = new I2Point(x, y);
		}

		private static void SetMousePosition(I2Point pt)
		{
			int x = pt.X;
			int y = pt.Y;

			x = SCommon.ToRange(x, 0, GameConfig.ScreenSize.W - 1);
			y = SCommon.ToRange(y, 0, GameConfig.ScreenSize.H - 1);

			P_Position = new I2Point(x, y);

			x *= DD.MainScreenDrawRect.W;
			x /= GameConfig.ScreenSize.W;
			x += DD.MainScreenDrawRect.L;
			y *= DD.MainScreenDrawRect.H;
			y /= GameConfig.ScreenSize.H;
			y += DD.MainScreenDrawRect.T;

			x = SCommon.ToRange(x, 0, DD.RealScreenSize.W - 1); // 2bs
			y = SCommon.ToRange(y, 0, DD.RealScreenSize.H - 1); // 2bs

			if (DX.SetMousePoint(x, y) != 0) // ? 失敗
				throw new Exception("SetMousePoint failed");
		}
	}
}
