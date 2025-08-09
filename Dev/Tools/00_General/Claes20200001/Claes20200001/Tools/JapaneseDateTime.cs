using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public class JapaneseDateTime
	{
		private class Era_t
		{
			public SimpleDateTime FirstDateTime;
			public string Name;
			public char Alphabet;

			public Era_t(string strFirstDate, string name, char alphabet)
			{
				this.FirstDateTime = SimpleDateTime.FromString(strFirstDate + " 00:00:00");
				this.Name = name;
				this.Alphabet = alphabet;
			}
		}

		private Era_t Era;
		private SimpleDateTime DateTime;

		private static Era_t[] EraList = new Era_t[]
		{
			new Era_t("0001/01/01", "西暦", 'C'),
			new Era_t("1868/01/01", "明治", 'M'),
			new Era_t("1912/07/30", "大正", 'T'),
			new Era_t("1926/12/25", "昭和", 'S'),
			new Era_t("1989/01/08", "平成", 'H'),
			new Era_t("2019/05/01", "令和", 'R'),
			//new Era_t("3000/01/01", "英弘", 'E'), // 新しい元号をここへ追加
			//new Era_t("3100/01/01", "久化", 'K'),
			//new Era_t("3200/01/01", "万和", 'B'),
		};

		public JapaneseDateTime(SimpleDateTime dateTime)
		{
			this.Era = EraList.Reverse().First(era => era.FirstDateTime <= dateTime);
			this.DateTime = dateTime;
		}

		public string EraName
		{
			get
			{
				return this.Era.Name;
			}
		}

		public char EraAlphabet
		{
			get
			{
				return this.Era.Alphabet;
			}
		}

		public int NenAsInteger
		{
			get
			{
				return this.DateTime.Year - this.Era.FirstDateTime.Year + 1;
			}
		}

		public string Nen
		{
			get
			{
				int nen = this.NenAsInteger;
				return nen == 1 ? "元" : nen.ToString();
			}
		}

		public int Month
		{
			get
			{
				return this.DateTime.Month;
			}
		}

		public int Day
		{
			get
			{
				return this.DateTime.Day;
			}
		}

		public int Hour
		{
			get
			{
				return this.DateTime.Hour;
			}
		}

		public int Minute
		{
			get
			{
				return this.DateTime.Minute;
			}
		}

		public int Second
		{
			get
			{
				return this.DateTime.Second;
			}
		}

		public override string ToString()
		{
			return $"{this.EraName}{this.Nen}年{this.Month}月{this.Day}日 {this.Hour:D2}:{this.Minute:D2}:{this.Second:D2}";
		}
	}
}
