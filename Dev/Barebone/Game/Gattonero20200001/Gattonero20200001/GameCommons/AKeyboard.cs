using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using DxLibDLL;

namespace Charlotte.GameCommons
{
	public static class AKeyboard
	{
		public const int KEY_MAX = 256;

		private static int[] Counters = new int[KEY_MAX];
		private static byte[] Statuses = new byte[KEY_MAX];

		public static void Initialize()
		{
			DD.Pin(Statuses);
		}

		public static void EachFrame()
		{
			if (DD.WindowIsActive)
			{
				if (DX.GetHitKeyStateAll(Statuses) != 0) // ? 失敗
					throw new Exception("GetHitKeyStateAll failed");

				for (int index = 0; index < KEY_MAX; index++)
					DD.UpdateButtonCounter(ref Counters[index], Statuses[index] != 0);
			}
			else
			{
				for (int index = 0; index < KEY_MAX; index++)
					DD.UpdateButtonCounter(ref Counters[index], false);
			}
		}

		// MEMO: ボタン・キー押下は 1 マウス押下は -1 で判定する。

		private const int _DUMMY_キーのフィールド名を探すためにF12で飛んで行く用 = DX.KEY_INPUT_0;

		public static int GetInput(int key)
		{
			// key == DX.KEY_INPUT_RETURN etc.

			return DD.FreezeInputFrame != 0 ? 0 : Counters[key];
		}

		public static bool IsPound(int key)
		{
			return DD.IsPound(GetInput(key));
		}

		public static class Keys
		{
			private static Lazy<string[]> Names = new Lazy<string[]>(() => GetNames_Once());

			public static string[] GetNames()
			{
				return Names.Value;
			}

			private static string[] GetNames_Once()
			{
				string[] names = new string[AKeyboard.KEY_MAX];

				for (int index = 0; index < AKeyboard.KEY_MAX; index++)
					names[index] = "(" + index + ")";

				foreach (KeyInfo info in GetKeys())
					names[info.Value] = info.Name;

				return names;
			}

			private class KeyInfo
			{
				public string Name;
				public int Value;
			}

			private static KeyInfo[] GetKeys()
			{
				return typeof(DX).GetFields(BindingFlags.Public | BindingFlags.Static)
					.Where(field => field.IsLiteral && !field.IsInitOnly && field.Name.StartsWith("KEY_INPUT_"))
					.Select(field => new KeyInfo() { Name = field.Name.Substring(10), Value = (int)field.GetValue(null) })
					.ToArray();
			}
		}
	}
}
