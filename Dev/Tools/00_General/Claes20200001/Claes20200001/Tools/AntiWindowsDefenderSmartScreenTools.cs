using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public static class AntiWindowsDefenderSmartScreenTools
	{
		private static string ExecutedFlagFile
		{
			get
			{
				return ProcMain.SelfFile + ".awdss.flg";
			}
		}

		public static void Run()
		{
			if (!File.Exists(ExecutedFlagFile))
			{
				foreach (string file in Directory.GetFiles(ProcMain.SelfDir, "*.exe"))
				{
					if (!file.EqualsIgnoreCase(ProcMain.SelfFile))
					{
						byte[] fileData = File.ReadAllBytes(file);
						SCommon.DeletePath(file);
						File.WriteAllBytes(file, fileData);
					}
				}
				File.WriteAllBytes(ExecutedFlagFile, SCommon.EMPTY_BYTES);
			}
		}
	}
}
