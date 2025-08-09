using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Charlotte
{
	public static class CompressorConsts
	{
		public const int SIGNATURE_DIR_ENTRY_START = 0x0d;
		public const int SIGNATURE_DIR_ENTRY_END = 0x0e;
		public const int SIGNATURE_FILE_ENTRY_START = 0x0f;
		public const int SIGNATURE_FILE_DATA_START = 0xfd;
		public const int SIGNATURE_END = 0xfe;
	}
}
