using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Commons;
using Charlotte.Drawings;
using Charlotte.GameCommons;

namespace Charlotte
{
	public static class GameSetting
	{
		public static I2Size UserScreenSize;
		public static bool FullScreen = false;
		public static bool MouseCursorShow = true;
		public static double MusicVolume;
		public static double SEVolume;

		public static void Initialize()
		{
			UserScreenSize = GameConfig.ScreenSize;
			MusicVolume = GameConfig.DefaultMusicVolume;
			SEVolume = GameConfig.DefaultSEVolume;
		}

		public static string Serialize()
		{
			List<object> dest = new List<object>();

			// ---- 保存データここから ----

			dest.Add(UserScreenSize.W);
			dest.Add(UserScreenSize.H);
			dest.Add(FullScreen);
			dest.Add(MouseCursorShow);
			dest.Add(DD.RateToPPB(MusicVolume));
			dest.Add(DD.RateToPPB(SEVolume));

			foreach (AInput input in Inputs.GetAllInput())
				dest.Add(input.Serialize());

			// ---- 保存データここまで ----

			dest.Insert(0, dest.Count + 1);

			return SCommon.Serializer.I.Join(dest.Select(v => v.ToString()).ToArray());
		}

		public static void Deserialize(string serializedString)
		{
			string[] src = SCommon.Serializer.I.Split(serializedString);
			int c = 0;

			if (int.Parse(src[c++]) != src.Length)
				throw new Exception("Bad Length");

			// ---- 保存データここから ----

			UserScreenSize.W = SCommon.ToRange(int.Parse(src[c++]), 1, SCommon.IMAX);
			UserScreenSize.H = SCommon.ToRange(int.Parse(src[c++]), 1, SCommon.IMAX);
			FullScreen = bool.Parse(src[c++]);
			MouseCursorShow = bool.Parse(src[c++]);
			MusicVolume = DD.PPBToRate(int.Parse(src[c++]));
			SEVolume = DD.PPBToRate(int.Parse(src[c++]));

			foreach (AInput input in Inputs.GetAllInput())
				input.Deserialize(src[c++]);

			// ---- 保存データここまで ----

			if (c != src.Length)
				throw new Exception("Length error");
		}
	}
}
