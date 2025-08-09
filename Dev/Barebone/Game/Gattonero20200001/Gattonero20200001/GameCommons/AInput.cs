using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Charlotte.Commons;

namespace Charlotte.GameCommons
{
	public class AInput
	{
		public int Button;
		public int[] Keys;

		public AInput(int button, params int[] keys)
		{
			this.Button = button;
			this.Keys = keys;
		}

		// MEMO: ボタン・キー押下は 1 マウス押下は -1 で判定する。

		public int GetInput()
		{
			int count = APad.GetInput(this.Button);

			for (int keyIndex = 0; keyIndex < this.Keys.Length; keyIndex++)
				if (count == 0)
					count = AKeyboard.GetInput(this.Keys[keyIndex]);

			return count;
		}

		public bool IsPound()
		{
			return DD.IsPound(this.GetInput());
		}

		public string Serialize()
		{
			return SCommon.Serializer.I.Join(new string[]
			{
				this.Button.ToString(),
				SCommon.Serializer.I.Join(this.Keys
					.Select(key => key.ToString())
					.ToArray()
					),
			});
		}

		public void Deserialize(string serializedString)
		{
			string[] src = SCommon.Serializer.I.Split(serializedString);
			int c = 0;

			this.Button = int.Parse(src[c++]);
			this.Keys = SCommon.Serializer.I.Split(src[c++])
				.Select(key => int.Parse(key))
				.ToArray();
		}
	}
}
