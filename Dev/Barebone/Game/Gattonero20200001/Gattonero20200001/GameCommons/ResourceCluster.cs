using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Charlotte.Commons;

namespace Charlotte.GameCommons
{
	public class ResourceCluster
	{
		private string ClusterFile;

		private class ElementFileInfo
		{
			public string ResPath;
			public int OriginalDataSize;
			public long StartPos;
			public int Length;
		}

		private List<ElementFileInfo> ElementFiles = new List<ElementFileInfo>();

		public ResourceCluster(string clusterFile)
		{
			this.ClusterFile = clusterFile;

			using (FileStream reader = new FileStream(this.ClusterFile, FileMode.Open, FileAccess.Read))
			{
				long clusterFileSize = reader.Length;

				while (reader.Position < clusterFileSize)
				{
					string resPath = SCommon.ReadPartString(reader);
					int originalDataSize = SCommon.ReadPartInt(reader);
					int length = SCommon.ReadPartInt(reader);

					this.ElementFiles.Add(new ElementFileInfo()
					{
						ResPath = resPath,
						OriginalDataSize = originalDataSize,
						StartPos = reader.Position,
						Length = length,
					});

					reader.Seek(length, SeekOrigin.Current);
				}
			}
		}

		public DD.LzData GetData(string resPath)
		{
			int index = SCommon.GetIndex(this.ElementFiles, v => SCommon.CompIgnoreCase(v.ResPath, resPath));

			if (index == -1)
				throw new Exception("resPath: " + resPath);

			ElementFileInfo elementFile = this.ElementFiles[index];

			return new DD.LzData(elementFile.OriginalDataSize, () =>
			{
				byte[] data;

				using (FileStream reader = new FileStream(this.ClusterFile, FileMode.Open, FileAccess.Read))
				{
					reader.Seek(elementFile.StartPos, SeekOrigin.Begin);
					data = SCommon.Read(reader, elementFile.Length);
				}

				ShuffleT023109(data);
				data = SCommon.Decompress(data);

				if (data.Length != elementFile.OriginalDataSize)
					throw new Exception("Bad data.Length");

				return data;
			});
		}

		private static void ShuffleT023109(byte[] data)
		{
			int l = 0;
			int r = data.Length - 2;
			int rr = Math.Max(3, data.Length / 109);

			while (l < r)
			{
				SCommon.Swap(data, l, r);

				l++;
				r -= rr;
			}
		}
	}
}
