using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Commons;

namespace Charlotte.Tools
{
	public class AutoList<T>
	{
		private List<T> Inner = new List<T>();
		private Func<T> F_GetDefaultValue;

		public AutoList(Func<T> getDefaultValue)
		{
			this.F_GetDefaultValue = getDefaultValue;
		}

		public int Count
		{
			get
			{
				return this.Inner.Count;
			}
		}

		private void EnsureCapacity(int index)
		{
			if (
				index < 0 ||
				SCommon.IMAX < index
				)
				throw new Exception("Bad index");

			while (this.Inner.Count <= index)
				this.Inner.Add(this.F_GetDefaultValue());
		}

		public T this[int index]
		{
			get
			{
				this.EnsureCapacity(index);
				return this.Inner[index];
			}

			set
			{
				this.EnsureCapacity(index);
				this.Inner[index] = value;
			}
		}
	}
}
