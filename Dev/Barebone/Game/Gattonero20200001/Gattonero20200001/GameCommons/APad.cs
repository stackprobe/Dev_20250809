using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DxLibDLL;

namespace Charlotte.GameCommons
{
	public static class APad
	{
		public const int PAD_MAX = 16;
		public const int BUTTON_MAX = 32;

		public static int PadCount { get; private set; }

		private static int[,] Counters = new int[PAD_MAX, BUTTON_MAX];
		private static uint[] Statuses = new uint[PAD_MAX];

		public static int PrimaryPad = -1; // -1 == 未確定

		public static void Initialize()
		{
			PadCount = DX.GetJoypadNum();

			if (PadCount < 0 || PAD_MAX < PadCount)
				throw new Exception("GetJoypadNum failed");
		}

		public static void EachFrame()
		{
			for (int pad = 0; pad < PadCount; pad++)
			{
				uint status;

				if (DD.WindowIsActive)
					status = (uint)DX.GetJoypadInputState(pad + 1);
				else
					status = 0u;

				for (int button = 0; button < BUTTON_MAX; button++)
					DD.UpdateButtonCounter(ref Counters[pad, button], (status & (1u << button)) != 0u);

				if (PrimaryPad == -1 && 10 < DD.ProcFrame && Statuses[pad] == 0u && status != 0u) // 最初にボタンを押下したパッドをプライマリーパッドとする。
					PrimaryPad = pad;

				Statuses[pad] = status;
			}
		}

		// MEMO: ボタン・キー押下は 1 マウス押下は -1 で判定する。

		public static int GetInput(int button, int pad)
		{
			return pad == -1 || DD.FreezeInputFrame != 0 ? 0 : Counters[pad, button];
		}

		public static bool IsPound(int button, int pad)
		{
			return DD.IsPound(GetInput(pad, button));
		}

		public static int GetInput(int button)
		{
			return GetInput(button, PrimaryPad);
		}

		public static bool IsPound(int button)
		{
			return IsPound(button, PrimaryPad);
		}
	}
}
