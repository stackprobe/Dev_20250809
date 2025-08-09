using System;
using System.Collections.Generic;
using System.Linq;
using Charlotte.Commons;

namespace Charlotte
{
	static class Program
	{
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main()
		{
			ProcMain.CUIMain(ar =>
			{
				GUIProcMain.GUIMain(() => new MainWin());
			});
		}
	}
}
