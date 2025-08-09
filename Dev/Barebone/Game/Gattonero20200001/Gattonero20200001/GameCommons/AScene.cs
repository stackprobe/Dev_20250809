using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte.GameCommons
{
	public class AScene
	{
		public int Numer;
		public int Denom;

		public double Rate
		{
			get
			{
				return (double)this.Numer / this.Denom;
			}
		}

		public static IEnumerable<AScene> Create(int frameMax)
		{
			for (int frame = 0; frame <= frameMax; frame++)
			{
				yield return new AScene()
				{
					Numer = frame,
					Denom = frameMax,
				};
			}
		}
	}
}
