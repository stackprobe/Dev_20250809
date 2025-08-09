using Charlotte.Commons;
using Charlotte.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charlotte.Tests
{
	public class Test0005
	{
		public void Test01()
		{
			Test01_a("http://chocomintice.ccsp.mydns.jp");
			Test01_a("http://chocomintice.ccsp.mydns.jp/HPStore/");
			Test01_a("https://leiros.cloudfree.jp/usbtn/usbtn.html");
			Test01_a("https://minitokyo3d.com/");
			Test01_a("https://teratail.com/questions/211123");
		}

		private void Test01_a(string url)
		{
			HTTPClient hc = new HTTPClient(url);

			using (WorkingDir wd = new WorkingDir())
			{
				hc.ResFile = wd.MakePath();
				hc.Get();

				Console.WriteLine($"{url} ⇒ {hc.ResHeaders["Content-Type"]}, {new FileInfo(hc.ResFile).Length}");
			}
		}
	}
}
