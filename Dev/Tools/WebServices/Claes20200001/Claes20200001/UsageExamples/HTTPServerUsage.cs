using Charlotte.WebServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charlotte.UsageExamples
{
	public class HTTPServerUsage
	{
		public void Test01()
		{
			HTTPServer hs = new HTTPServer()
			{
				PortNo = 80,
				Backlog = 300,
				ConnectMax = 100,
				Interlude = () => !Console.KeyAvailable,
				HTTPConnected = channel =>
				{
					// 以下は安全に表示可能な文字列であることが保証される。
					// -- FirstLine == ASCII && not-null
					// -- Method == ASCII && not-null
					// -- PathQuery == SJIS && not-null
					// -- HTTPVersion == ASCII && not-null
					// -- HeaderPairs == not-null && (全てのキーと値について ASCII && not-null)
					// ---- ASCII == [\u0020-\u007e]*
					// ---- SJIS == ToJString(, true, false, false, true)
					// 以下も保証される。
					// -- Body == not-null

					Console.WriteLine(channel.FirstLine);
					Console.WriteLine(channel.Method);
					Console.WriteLine(channel.PathQuery);
					Console.WriteLine(channel.HTTPVersion);
					Console.WriteLine(string.Join(", ", channel.HeaderPairs.Select(pair => pair[0] + "=" + pair[1])));
					Console.WriteLine(BitConverter.ToString(channel.Body.ToByteArray()));

					channel.ResStatus = 200;
					channel.ResHeaderPairs.Add(new string[] { "Content-Type", "text/plain; charset=US-ASCII" });
					channel.ResHeaderPairs.Add(new string[] { "X-ResHeader-001", "123" });
					channel.ResHeaderPairs.Add(new string[] { "X-ResHeader-002", "ABC" });
					channel.ResHeaderPairs.Add(new string[] { "X-ResHeader-003", "abc" });
					channel.ResBody = new byte[][] { Encoding.ASCII.GetBytes("Hello, Happy World!") };
					channel.ResBodyLength = -1L;
				},
			};

			SockChannel.ThreadTimeoutMillis = 100;

			HTTPServer.KeepAliveTimeoutMillis = 5000;

			HTTPServerChannel.RequestTimeoutMillis = -1;
			HTTPServerChannel.ResponseTimeoutMillis = -1;
			HTTPServerChannel.FirstLineTimeoutMillis = 2000;
			HTTPServerChannel.IdleTimeoutMillis = 180000; // 3 min
			HTTPServerChannel.BodySizeMax = 512000000; // 512 MB

			SockCommon.TimeWaitMonitor.CTR_ROT_SEC = 60;
			SockCommon.TimeWaitMonitor.COUNTER_NUM = 5;
			SockCommon.TimeWaitMonitor.COUNT_LIMIT = 10000;

			hs.Run();
		}
	}
}
