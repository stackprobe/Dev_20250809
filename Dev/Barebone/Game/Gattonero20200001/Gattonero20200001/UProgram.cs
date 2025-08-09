using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DxLibDLL;
using Charlotte.Commons;
using Charlotte.Drawings;
using Charlotte.GameCommons;

namespace Charlotte
{
	public class UProgram
	{
		public void Run()
		{
			for (; ; )
			{
				if (Inputs.ENTER.IsPound())
					break;

				DD.EachFrame();
			}
		}
	}
}
