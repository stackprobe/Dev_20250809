using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using Charlotte.Commons;

namespace Charlotte
{
	public static class Decompressor
	{
		private static Stream Reader;

		private class DirEntry
		{
			public string LocalName;
			public List<DirEntry> DirList = new List<DirEntry>();
			public List<FileEntry> FileList = new List<FileEntry>();

			public void Read()
			{
				this.LocalName = SCommon.ReadPartString(Reader);
				CheckLocalName(this.LocalName);
				this.Read_DFs();
			}

			public void Read_DFs()
			{
				for (; ; )
				{
					int chr = Reader.ReadByte();

					if (chr == CompressorConsts.SIGNATURE_DIR_ENTRY_START)
					{
						DirEntry dir = new DirEntry();
						this.DirList.Add(dir);
						dir.Read();
					}
					else if (chr == CompressorConsts.SIGNATURE_FILE_ENTRY_START)
					{
						FileEntry file = new FileEntry();
						this.FileList.Add(file);
						file.Read();
					}
					else if (chr == CompressorConsts.SIGNATURE_DIR_ENTRY_END)
					{
						break;
					}
					else
					{
						throw new Exception("Bad SIGNATURE: " + chr);
					}
				}
			}

			public void CreateDirs(string wDir)
			{
				SCommon.CreateDir(wDir);

				foreach (DirEntry dir in this.DirList)
				{
					dir.CreateDirs(Path.Combine(wDir, dir.LocalName));
				}
			}

			public void SetFullPath_All(string wDir)
			{
				foreach (DirEntry dir in this.DirList)
					dir.SetFullPath_All(Path.Combine(wDir, dir.LocalName));

				foreach (FileEntry file in this.FileList)
					file.FullPath = Path.Combine(wDir, file.LocalName);
			}

			public static List<FileEntry> P_CollectedFiles;

			public void CollectFiles()
			{
				foreach (DirEntry dir in this.DirList)
					dir.CollectFiles();

				foreach (FileEntry file in this.FileList)
					P_CollectedFiles.Add(file);
			}
		}

		private class FileEntry
		{
			public string LocalName;
			public SimpleDateTime CreationTime;
			public SimpleDateTime LastWriteTime;
			public SimpleDateTime LastAccessTime;
			public string Hash;

			public void Read()
			{
				this.LocalName = SCommon.ReadPartString(Reader);
				CheckLocalName(this.LocalName);
				this.CreationTime = SimpleDateTime.FromTimeStamp(SCommon.ReadPartLong(Reader));
				this.LastWriteTime = SimpleDateTime.FromTimeStamp(SCommon.ReadPartLong(Reader));
				this.LastAccessTime = SimpleDateTime.FromTimeStamp(SCommon.ReadPartLong(Reader));
				this.Hash = SCommon.ReadPartString(Reader);
			}

			public string FullPath;
		}

		public static void Run(string rFile, string wDir)
		{
			rFile = SCommon.MakeFullPath(rFile);
			wDir = SCommon.MakeFullPath(wDir);

			Console.WriteLine("< " + rFile);
			Console.WriteLine("> " + wDir);

			if (!File.Exists(rFile))
				throw new ArgumentException("rFile is not file");

			if (File.Exists(wDir))
				throw new ArgumentException("wDir is file");

			// 引数チェックここまで

			using (FileStream fileReader = new FileStream(rFile, FileMode.Open, FileAccess.Read))
			using (GZipStream gz = new GZipStream(fileReader, CompressionMode.Decompress, true))
			{
				ProcMain.WriteLog("Phase-1");

				DirEntry root = new DirEntry()
				{
					LocalName = null,
				};

				Reader = gz;
				root.Read_DFs();
				Reader = null;

				ProcMain.WriteLog("Phase-2");

				root.SetFullPath_All(wDir);

				DirEntry.P_CollectedFiles = new List<FileEntry>();
				root.CollectFiles();
				List<FileEntry> files = DirEntry.P_CollectedFiles;
				DirEntry.P_CollectedFiles = null;

				Dictionary<string, List<FileEntry>> hash2fileList = SCommon.CreateDictionary<List<FileEntry>>();

				foreach (FileEntry file in files)
				{
					if (!hash2fileList.ContainsKey(file.Hash))
					{
						hash2fileList.Add(file.Hash, new List<FileEntry>());
					}
					hash2fileList[file.Hash].Add(file);
				}

				ProcMain.WriteLog("Phase-3");

				root.CreateDirs(wDir);

				ProcMain.WriteLog("Phase-4");

				for (; ; )
				{
					int chr = gz.ReadByte();

					if (chr == CompressorConsts.SIGNATURE_FILE_DATA_START)
					{
						ProcFileData(gz, hash2fileList);
					}
					else if (chr == CompressorConsts.SIGNATURE_END)
					{
						break;
					}
					else
					{
						throw new Exception("Bad SIGNATURE: " + chr);
					}
				}

				ProcMain.WriteLog("Phase-5");

				foreach (FileEntry file in files)
				{
					// ? ファイルが処理されていない(ファイルが作成されていない)
					// -- ファイルエントリーのハッシュ値に対応するファイルデータが存在しなかった。
					// -- 入力ファイルが破損している可能性あり。
					if (!File.Exists(file.FullPath))
						throw new Exception("NO-PROC-FILE-ERROR " + file.FullPath);

					FileInfo info = new FileInfo(file.FullPath);

					info.CreationTime = file.CreationTime.ToDateTime();
					info.LastWriteTime = file.LastWriteTime.ToDateTime();
					info.LastAccessTime = file.LastAccessTime.ToDateTime();
				}

				ProcMain.WriteLog("Phase-6");

				if (gz.ReadByte() != -1)
					throw new Exception("NOT-EOF-ERROR");
			}
			Console.WriteLine("done!");
		}

		private static void ProcFileData(Stream reader, Dictionary<string, List<FileEntry>> hash2fileList)
		{
			string hash = SCommon.ReadPartString(reader);
			long length = SCommon.ReadPartLong(reader);

			ProcMain.WriteLog("H " + hash);
			ProcMain.WriteLog("L " + length);

			if (length < 0)
				throw new Exception("Bad length: " + length);

			if (!hash2fileList.ContainsKey(hash))
				throw new Exception("Bad hash: " + hash);

			List<FileEntry> files = hash2fileList[hash];

			using (FileStream writer = new FileStream(files[0].FullPath, FileMode.Create, FileAccess.Write))
			{
				ProcMain.WriteLog("< *Stream");
				ProcMain.WriteLog("> " + files[0].FullPath);

				SCommon.ReadToEnd(
					SCommon.GetLimitedReader(SCommon.GetReader(reader.Read), length),
					writer.Write
					);
			}

			foreach (FileEntry file in files.Skip(1))
			{
				ProcMain.WriteLog("< " + files[0].FullPath);
				ProcMain.WriteLog("> " + file.FullPath);

				File.Copy(files[0].FullPath, file.FullPath);
			}
			ProcMain.WriteLog("done");
		}

		private static void CheckLocalName(string localName)
		{
			if (
				localName == "" ||
				localName == "." ||
				localName == ".." ||
				localName.Contains(':')
				)
				throw new Exception("Bad localName");
		}
	}
}
