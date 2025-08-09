using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.Win32;
using Charlotte.Commons;

namespace Charlotte
{
	public static class GUIProcMain
	{
		public static void GUIMain(Func<Form> getMainForm)
		{
			ProcMain.WriteLog = message => { };

			Application.ThreadException += new ThreadExceptionEventHandler((sender, e) => ErrorTermination(e.Exception));
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, e) => ErrorTermination(e.ExceptionObject));
			SystemEvents.SessionEnding += new SessionEndingEventHandler((sender, e) => ProgramTermination());

			KeepSingleInstance(() =>
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(getMainForm());
			});
		}

		private static void ErrorTermination(object ex)
		{
			MessageBox.Show(
				"" + ex,
				Path.GetFileNameWithoutExtension(ProcMain.SelfFile) + " / Error",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error
				);

			ProgramTermination();
		}

		private static void ProgramTermination()
		{
			Environment.Exit(1);
		}

		private static readonly string MUTEX_NAME_BASE_PART_Silvia20200001 = "Silvia20200001-{7d4fc1fc-6853-4cf2-b9eb-8bc96537671a}";

		private static void KeepSingleInstance(Action routine)
		{
			// HACK: 同じアプリケーションでもビルドしなおすと(バージョンが違うと)排他制御が効かなくなる。

			string selfFileHash = GetSelfFileHash();

			Mutex procMutex = new Mutex(
				false,
				$"{MUTEX_NAME_BASE_PART_Silvia20200001}-Proc-{selfFileHash}"
				);

			if (procMutex.WaitOne(0))
			{
				bool createdNew;
				MutexSecurity security = new MutexSecurity();
				security.AddAccessRule(
					new MutexAccessRule(
						new SecurityIdentifier(
							WellKnownSidType.WorldSid,
							null
							),
						MutexRights.FullControl,
						AccessControlType.Allow
						)
					);
				Mutex globalProcMutex = new Mutex(
					false,
					$"Global\\{MUTEX_NAME_BASE_PART_Silvia20200001}-GlobalProc-{selfFileHash}",
					out createdNew,
					security
					);

				bool globalLockFailed = false;

				if (globalProcMutex.WaitOne(0))
				{
					routine();

					globalProcMutex.ReleaseMutex();
				}
				else
				{
					globalLockFailed = true;
				}
				globalProcMutex.Close();
				globalProcMutex.Dispose();
				globalProcMutex = null;

				if (globalLockFailed)
				{
					// ダイアログの多重表示を防ぐため、
					// グローバルロック解除の後・ローカルロック解除の前に表示すること。
					MessageBox.Show(
						"This program is already running under a different user session.",
						Path.GetFileNameWithoutExtension(ProcMain.SelfFile) + " / Error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error
						);
				}
				procMutex.ReleaseMutex();
			}
			procMutex.Close();
			procMutex.Dispose();
			procMutex = null;
		}

		private static string GetSelfFileHash()
		{
			bool createdNew;
			MutexSecurity security = new MutexSecurity();
			security.AddAccessRule(
				new MutexAccessRule(
					new SecurityIdentifier(
						WellKnownSidType.WorldSid,
						null
						),
					MutexRights.FullControl,
					AccessControlType.Allow
					)
				);
			Mutex globalMutex = new Mutex(
				false,
				$"Global\\{MUTEX_NAME_BASE_PART_Silvia20200001}-Global",
				out createdNew,
				security
				);

			globalMutex.WaitOne();

			string hashStoredFile = ProcMain.SelfFile + "_sha512.dat";
			string hash;

			if (File.Exists(hashStoredFile))
			{
				hash = File.ReadAllText(hashStoredFile, Encoding.ASCII).Trim();

				if (!Regex.IsMatch(hash, "^[0-9a-f]{128}$"))
					throw new Exception("Hash file is corrupt !");
			}
			else
			{
				using (SHA512 sha512 = SHA512.Create())
				using (FileStream reader = new FileStream(ProcMain.SelfFile, FileMode.Open, FileAccess.Read))
				{
					hash = BitConverter.ToString(sha512.ComputeHash(reader)).Replace("-", "").ToLower();
				}

				File.WriteAllText(hashStoredFile, hash, Encoding.ASCII);
			}

			globalMutex.ReleaseMutex();
			globalMutex.Close();
			globalMutex.Dispose();
			globalMutex = null;

			return hash;
		}
	}
}
