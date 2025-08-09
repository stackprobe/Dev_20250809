using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public class FlexibleTable<T>
	{
		private List<List<T>> Rows = new List<List<T>>();

		public int Width { get; private set; }

		public int Height
		{
			get
			{
				return this.Rows.Count;
			}
		}

		public T this[int x, int y]
		{
			get
			{
				if (
					x < 0 ||
					y < 0 || this.Rows.Count <= y ||
					this.Rows[y].Count <= x
					)
					return default(T);

				return this.Rows[y][x];
			}

			set
			{
				if (
					x < 0 || SCommon.IMAX < x ||
					y < 0 || SCommon.IMAX < y
					)
					throw new Exception("Bad coordinate");

				while (this.Rows.Count <= y)
					this.Rows.Add(new List<T>());

				if (this.Rows[y].Count <= x)
				{
					while (this.Rows[y].Count <= x)
						this.Rows[y].Add(default(T));

					this.Width = Math.Max(this.Width, x + 1);
				}
				this.Rows[y][x] = value;
			}
		}
	}
}
