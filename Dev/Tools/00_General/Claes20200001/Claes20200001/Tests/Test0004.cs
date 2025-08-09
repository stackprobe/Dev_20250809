using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using Charlotte.Commons;
using Charlotte.Tools;

namespace Charlotte.Tests
{
	public class Test0004
	{
		public void Test01()
		{
			Test01_a("");
			Test01_a("a");
			Test01_a("aa");
			Test01_a("aaa");
			Test01_a("foobar");
		}

		private void Test01_a(string str)
		{
			Console.WriteLine(str);

			byte[] b = Encoding.UTF8.GetBytes(str);

			Console.WriteLine(Adler32.ComputeHash(b).ToString("x8"));
			Console.WriteLine(FNV1aHash.ComputeHash(b).ToString("x8"));
		}

		public void Test02()
		{
			Console.WriteLine(StringTools.ThousandComma(0));
			Console.WriteLine(StringTools.ThousandComma(1));
			Console.WriteLine(StringTools.ThousandComma(12));
			Console.WriteLine(StringTools.ThousandComma(123));
			Console.WriteLine(StringTools.ThousandComma(1234));
			Console.WriteLine(StringTools.ThousandComma(12345));
			Console.WriteLine(StringTools.ThousandComma(123456));
			Console.WriteLine(StringTools.ThousandComma(1234567));
			Console.WriteLine(StringTools.ThousandComma(-1));
			Console.WriteLine(StringTools.ThousandComma(-12));
			Console.WriteLine(StringTools.ThousandComma(-123));
			Console.WriteLine(StringTools.ThousandComma(-1234));
			Console.WriteLine(StringTools.ThousandComma(-12345));
			Console.WriteLine(StringTools.ThousandComma(-123456));
			Console.WriteLine(StringTools.ThousandComma(-1234567));
		}
	}
}
