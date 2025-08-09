using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte.Tools
{
	public class StringSpliceSequencer
	{
		private string Source;

		public StringSpliceSequencer(string src)
		{
			if (src == null)
				throw new ArgumentException("Bad src");

			this.Source = src;
		}

		private List<SpliceInfo> SpliceInfos = new List<SpliceInfo>();

		private class SpliceInfo
		{
			public int Start;
			public int RemoveLength;
			public string NewPart;

			public int End
			{
				get
				{
					return this.Start + this.RemoveLength;
				}
			}
		}

		public StringSpliceSequencer Splice(int start, int removeLength, string newPart)
		{
			if (
				start < 0 || this.Source.Length < start ||
				removeLength < 0 || this.Source.Length - start < removeLength ||
				newPart == null
				)
				throw new ArgumentException("Bad params");

			if (
				this.SpliceInfos.Count != 0 &&
				this.SpliceInfos[this.SpliceInfos.Count - 1].End > start
				)
				throw new ArgumentException("Bad range");

			this.SpliceInfos.Add(new SpliceInfo()
			{
				Start = start,
				RemoveLength = removeLength,
				NewPart = newPart,
			});

			return this;
		}

		public string GetString()
		{
			int capacity = this.Source.Length;

			foreach (SpliceInfo info in this.SpliceInfos)
				capacity += info.NewPart.Length - info.RemoveLength;

			StringBuilder dest = new StringBuilder(capacity);
			int index = 0;

			foreach (SpliceInfo info in this.SpliceInfos)
			{
				dest.Append(this.Source, index, info.Start - index);
				dest.Append(info.NewPart);
				index = info.End;
			}
			dest.Append(this.Source, index, this.Source.Length - index);
			return dest.ToString();
		}
	}
}
