using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Charlotte.Drawings;

namespace Charlotte
{
	public static class GameConfig
	{
		public static string GameTitle = "Gattonero-2023-04-05";
		public static string Version = "0.01";

		public static I2Size ScreenSize = new I2Size(960, 540);

		public static double DefaultMusicVolume = 0.45;
		public static double DefaultSEVolume = 0.45;

		public static Color LibbonBackColor = Color.FromArgb(96, 24, 48);
		public static Color LibbonForeColor = Color.White;
	}
}
