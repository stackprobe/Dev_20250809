using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Tools;

namespace Charlotte.Tests
{
	public class Test0002
	{
		public void Test01()
		{
			Console.WriteLine(new Fraction(1, 3).ToDouble()); // 0.333333333333333
			Console.WriteLine(new Fraction(2, 3).ToDouble()); // 0.666666666666666
			Console.WriteLine(new Fraction(3, 3).ToDouble()); // 1

			Console.WriteLine((new Fraction(1, 3) + new Fraction(2, 3))); // 1/1
			Console.WriteLine((new Fraction(2, 5) + new Fraction(3, 5))); // 1/1
			Console.WriteLine((new Fraction(3, 7) + new Fraction(4, 7))); // 1/1
		}
	}
}
