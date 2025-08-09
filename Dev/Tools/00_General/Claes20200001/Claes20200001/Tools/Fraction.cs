using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace Charlotte.Tools
{
	public struct Fraction
	{
		public BigInteger Numer;
		public BigInteger Denom;

		public Fraction(BigInteger numer, BigInteger denom)
		{
			this.Numer = numer;
			this.Denom = denom;

			this.Simplify();
		}

		public static implicit operator Fraction(long value)
		{
			return new Fraction(value, 1);
		}

		public static Fraction operator ++(Fraction instance)
		{
			return instance + new Fraction(1, 1);
		}

		public static Fraction operator --(Fraction instance)
		{
			return instance - new Fraction(1, 1);
		}

		public static Fraction operator +(Fraction a, Fraction b)
		{
			Reduction(ref a, ref b);
			return new Fraction(a.Numer + b.Numer, b.Denom).ToSimply();
		}

		public static Fraction operator -(Fraction a, Fraction b)
		{
			Reduction(ref a, ref b);
			return new Fraction(a.Numer - b.Numer, b.Denom).ToSimply();
		}

		public static Fraction operator *(Fraction a, Fraction b)
		{
			return new Fraction(a.Numer * b.Numer, a.Denom * b.Denom).ToSimply();
		}

		public static Fraction operator /(Fraction a, Fraction b)
		{
			return new Fraction(a.Numer * b.Denom, a.Denom * b.Numer).ToSimply();
		}

		public static bool operator ==(Fraction a, Fraction b)
		{
			Reduction(ref a, ref b);
			return a.Numer == b.Numer;
		}

		public static bool operator !=(Fraction a, Fraction b)
		{
			Reduction(ref a, ref b);
			return a.Numer != b.Numer;
		}

		public override bool Equals(object another)
		{
			return another is Fraction && this == (Fraction)another;
		}

		public override int GetHashCode()
		{
			return (this.Numer.GetHashCode() + ":" + this.Denom.GetHashCode()).GetHashCode();
		}

		public static bool operator <(Fraction a, Fraction b)
		{
			Reduction(ref a, ref b);
			return a.Numer < b.Numer;
		}

		public static bool operator >(Fraction a, Fraction b)
		{
			Reduction(ref a, ref b);
			return a.Numer > b.Numer;
		}

		public static bool operator <=(Fraction a, Fraction b)
		{
			Reduction(ref a, ref b);
			return a.Numer <= b.Numer;
		}

		public static bool operator >=(Fraction a, Fraction b)
		{
			Reduction(ref a, ref b);
			return a.Numer >= b.Numer;
		}

		public static void Reduction(ref Fraction a, ref Fraction b) // 通分
		{
			BigInteger dGCD = GetGCD(a.Denom, b.Denom);
			BigInteger mulA = b.Denom / dGCD;
			BigInteger d = a.Denom * mulA;

			a.Numer *= mulA;
			b.Numer *= a.Denom / dGCD;
			a.Denom = d;
			b.Denom = d;
		}

		public Fraction ToSimply()
		{
			Fraction ret = this;
			ret.Simplify();
			return ret;
		}

		public void Simplify() // 約分とか
		{
			if (this.Denom == 0)
				throw new Exception("Bad denominator");

			if (this.Numer == 0)
			{
				this.Denom = 1;
				return;
			}
			if (this.Numer < 0)
			{
				this.Numer *= -1;
				this.Denom *= -1;
			}
			int sign = 1;

			if (this.Denom < 0)
			{
				sign = -1;
				this.Denom *= -1;
			}
			if (this.Numer == this.Denom)
			{
				this.Numer = sign;
				this.Denom = 1;
				return;
			}
			BigInteger d = GetGCD(this.Numer, this.Denom);

			this.Numer /= d;
			this.Denom /= d;

			this.Numer *= sign;
		}

		private static BigInteger GetGCD(BigInteger a, BigInteger b)
		{
			if (a < b)
			{
				BigInteger t = a;
				a = b;
				b = t;
			}
			while (b != 0)
			{
				BigInteger r = a % b;
				a = b;
				b = r;
			}
			return a;
		}

		public override string ToString()
		{
			return this.Numer + "/" + this.Denom;
		}

		public double ToDouble()
		{
			long PRECISION = 1L << 50;

			return (double)((this.Numer * PRECISION) / this.Denom) / PRECISION;
		}
	}
}
