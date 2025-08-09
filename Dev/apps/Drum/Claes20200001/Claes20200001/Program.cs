using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Charlotte.Commons;

namespace Charlotte
{
	class Program
	{
		static void Main(string[] args)
		{
			ProcMain.CUIMain(new Program().Main2);
		}

		private void Main2(ArgsReader ar)
		{
			if (ProcMain.DEBUG)
			{
				Main3();
			}
			else
			{
				Main4(ar);
			}
			SCommon.OpenOutputDirIfCreated();
		}

		private void Main3()
		{
#if DEBUG
			// -- choose one --

			Main4(new ArgsReader(new string[] { @"C:\temp\Input", @"C:\temp\Input.drum.gz" }));
			//Main4(new ArgsReader(new string[] { "/D", @"C:\temp\Input.drum.gz", @"C:\temp\Output" }));

			// --
#endif
			SCommon.Pause();
		}

		private void Main4(ArgsReader ar)
		{
			try
			{
				Main5(ar);
			}
			catch (Exception e)
			{
				ProcMain.WriteLog(e);
			}
		}

		private void Main5(ArgsReader ar)
		{
			if (ar.ArgIs("/D")) // 展開
			{
				string rFile = ar.NextArg();
				string wDir = ar.NextArg();

				ar.End();

				Decompressor.Run(rFile, wDir);
			}
			else // 圧縮
			{
				string rDir = ar.NextArg();
				string wFile = ar.NextArg();

				ar.End();

				Compressor.Run(rDir, wFile);
			}
		}
	}
}
