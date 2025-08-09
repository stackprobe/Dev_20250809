using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte.Tools
{
	public class ArraySpliceSequencer<T>
	{
		private T[] Source;

		public ArraySpliceSequencer(T[] src)
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
			public T[] NewPartSource;
			public int NewPartStart;
			public int NewPartLength;

			public int End
			{
				get
				{
					return this.Start + this.RemoveLength;
				}
			}
		}

		public ArraySpliceSequencer<T> Splice(int start, int removeLength, T[] newPart)
		{
			return this.Splice(start, removeLength, newPart, 0);
		}

		public ArraySpliceSequencer<T> Splice(int start, int removeLength, T[] newPartSource, int newPartStart)
		{
			return this.Splice(start, removeLength, newPartSource, newPartStart, newPartSource.Length - newPartStart);
		}

		public ArraySpliceSequencer<T> Splice(int start, int removeLength, T[] newPartSource, int newPartStart, int newPartLength)
		{
			if (
				start < 0 || this.Source.Length < start ||
				removeLength < 0 || this.Source.Length - start < removeLength ||
				newPartSource == null ||
				newPartStart < 0 || newPartSource.Length < newPartStart ||
				newPartLength < 0 || newPartSource.Length - newPartStart < newPartLength
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
				NewPartSource = newPartSource,
				NewPartStart = newPartStart,
				NewPartLength = newPartLength,
			});

			return this;
		}

		public T[] GetArray()
		{
			int capacity = this.Source.Length;

			foreach (SpliceInfo info in this.SpliceInfos)
				capacity += info.NewPartLength - info.RemoveLength;

			T[] dest = new T[capacity];
			int reader = 0;
			int writer = 0;

			foreach (SpliceInfo info in this.SpliceInfos)
			{
				Array.Copy(this.Source, reader, dest, writer, info.Start - reader);
				writer += info.Start - reader;
				Array.Copy(info.NewPartSource, info.NewPartStart, dest, writer, info.NewPartLength);
				writer += info.NewPartLength;
				reader = info.End;
			}
			Array.Copy(this.Source, reader, dest, writer, this.Source.Length - reader);
			return dest;
		}
	}
}
