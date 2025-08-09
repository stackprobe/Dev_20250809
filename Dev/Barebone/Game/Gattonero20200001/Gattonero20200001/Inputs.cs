using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using DxLibDLL;
using Charlotte.GameCommons;

namespace Charlotte
{
	public static class Inputs
	{
		public static AInput DIR_2 = new AInput(0, DX.KEY_INPUT_DOWN);
		public static AInput DIR_4 = new AInput(1, DX.KEY_INPUT_LEFT);
		public static AInput DIR_6 = new AInput(2, DX.KEY_INPUT_RIGHT);
		public static AInput DIR_8 = new AInput(3, DX.KEY_INPUT_UP);
		public static AInput ENTER = new AInput(4, DX.KEY_INPUT_RETURN);

		public static AInput[] GetAllInput()
		{
			return typeof(Inputs)
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(field => field.FieldType == typeof(AInput))
				.OrderBy(field => field.Name)
				.Select(field => (AInput)field.GetValue(null))
				.ToArray();
		}
	}
}
