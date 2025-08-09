using Charlotte.Commons;
using Charlotte.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charlotte.UsageExamples
{
	public class HTTPClientUsage
	{
		public void Test01()
		{
			using (WorkingDir wd = new WorkingDir())
			{
				string resFile = wd.MakePath();

				HTTPClient hc = new HTTPClient("https://www.google.com")
				{
					ConnectTimeoutMillis = 43200000, // 12 hour
					TimeoutMillis = 86400000, // 1 day
					IdleTimeoutMillis = 180000, // 3 min
					ResBodySizeMax = 8000000000000000, // 8 PB (8000 TB)
					ResFile = resFile,
				};

				hc.Get();

				Console.WriteLine(Encoding.ASCII.GetString(File.ReadAllBytes(resFile)));
			}
		}
	}
}
