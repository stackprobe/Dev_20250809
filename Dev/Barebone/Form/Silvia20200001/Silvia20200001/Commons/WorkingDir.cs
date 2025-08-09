using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Charlotte.Commons
{
	public class WorkingDir : IDisposable
	{
		public static RootInfo Root = null;

		public class RootInfo
		{
			public string Dir = null;

			public string GetDir()
			{
				if (this.Dir == null)
				{
					string dir = GetRootDir();

					SCommon.DeleteAndCreateDir(dir);

					this.Dir = dir;
				}
				return this.Dir;
			}

			public void Delete()
			{
				if (this.Dir != null)
				{
					try
					{
						Directory.Delete(this.Dir, true);
					}
					catch (Exception ex)
					{
						ProcMain.WriteLog(ex);
					}

					this.Dir = null;
				}
			}
		}

		private static string GetRootDir()
		{
			return Path.Combine(GetTMPDir(), GetRootDirLocalName());
		}

		private static string GetTMPDir()
		{
			foreach (string envName in new string[] { "TMP", "TEMP", "ProgramData" })
			{
				string dir = Environment.GetEnvironmentVariable(envName);

				if (
					!string.IsNullOrEmpty(dir) &&
					SCommon.IsFairFullPath(dir) &&
					!dir.Contains('\u0020') && !dir.Contains('\u3000') && // 空白を含まないこと。
					Directory.Exists(dir)
					)
					return dir;
			}
			throw new Exception("Environment variables TMP, TEMP, and ProgramData are incorrect");
		}

		private static string GetRootDirLocalName()
		{
			long timeStamp = SimpleDateTime.Now.ToTimeStamp();
			string randPart = SCommon.Base32.I.Encode(SCommon.CRandom.GetBytes(15)); // ('A'～'Z' + '2'～'7')[24] (120 bit) random pattern

			return $"Claes20200001_{timeStamp:x}_{randPart}";
		}

		private static ulong CtorCounter = 0;

		private string Dir = null;

		private string GetDir()
		{
			if (this.Dir == null)
			{
				if (Root == null)
					throw new Exception("Root is null");

				this.Dir = Path.Combine(Root.GetDir(), (CtorCounter++).ToString("x"));

				SCommon.CreateDir(this.Dir);
			}
			return this.Dir;
		}

		public string GetPath(string localName)
		{
			return Path.Combine(this.GetDir(), localName);
		}

		private ulong PathCounter = 0;

		public string MakePath()
		{
			return this.GetPath((this.PathCounter++).ToString("x"));
		}

		public void Dispose()
		{
			if (this.Dir != null)
			{
				try
				{
					Directory.Delete(this.Dir, true);
				}
				catch (Exception ex)
				{
					ProcMain.WriteLog(ex);
				}

				this.Dir = null;
			}
		}

		public static void ChangeTMPDir(string dir)
		{
			dir = SCommon.MakeFullPath(dir);

			if (!Directory.Exists(dir))
				throw new Exception("no dir");

			if (Root.Dir != null)
				throw new Exception("Root.Dir is already created");

			dir = Path.Combine(dir, GetRootDirLocalName());

			SCommon.DeleteAndCreateDir(dir);

			Root.Dir = dir;
		}
	}
}
