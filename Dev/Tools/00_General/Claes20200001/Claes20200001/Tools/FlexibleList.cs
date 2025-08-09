using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public class FlexibleList<T>
	{
		private List<T> Inner = new List<T>();

		public int Count
		{
			get
			{
				return this.Inner.Count;
			}
		}

		public T this[int index]
		{
			get
			{
				if (
					index < 0 ||
					this.Inner.Count <= index
					)
					return default(T);

				return this.Inner[index];
			}

			set
			{
				if (
					index < 0 ||
					SCommon.IMAX < index
					)
					throw new Exception("Bad index");

				while (this.Inner.Count <= index)
					this.Inner.Add(default(T));

				this.Inner[index] = value;
			}
		}
	}
}
