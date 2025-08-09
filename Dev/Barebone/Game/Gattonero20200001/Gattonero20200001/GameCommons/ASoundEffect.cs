using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DxLibDLL;
using Charlotte.Commons;

namespace Charlotte.GameCommons
{
	/// <summary>
	/// 効果音リソース
	/// このクラスのインスタンスはプロセスで有限個であること。
	/// 原則的に以下のクラスの静的フィールドとして植え込むこと。
	/// -- SoundEffects
	/// </summary>
	public class ASoundEffect
	{
		private static DD.Collector<ASoundEffect> Instances = new DD.Collector<ASoundEffect>();

		public static void TryUnloadAll()
		{
			foreach (ASoundEffect instance in Instances.Iterate())
				instance.TryUnload();
		}

		private Func<DD.LzData> FileDataGetter;

		private class HandleInfo
		{
			public int Value;
			public int LastVolume = -1;

			public HandleInfo(int value)
			{
				this.Value = value;
			}
		}

		private List<HandleInfo> Handles; // null == 未ロード
		private int PlayIndex;

		/// <summary>
		/// リソースから効果音をロードする。
		/// </summary>
		/// <param name="resPath">リソースのパス</param>
		public ASoundEffect(string resPath)
			: this(() => DD.GetResFileData(resPath))
		{ }

		/// <summary>
		/// 効果音データの取得メソッドから効果音をロードする。
		/// </summary>
		/// <param name="getFileData">効果音データの取得メソッド</param>
		public ASoundEffect(Func<DD.LzData> getFileData)
		{
			this.FileDataGetter = getFileData;
			this.Handles = null;

			Instances.Add(this);
		}

		private void LoadIfNeeded()
		{
			if (this.Handles == null)
			{
				byte[] fileData = this.FileDataGetter().Data.Value;
				int handle = -1;

				DD.PinOn(fileData, p => handle = DX.LoadSoundMemByMemImage(p, (ulong)fileData.Length));

				if (handle == -1) // ? 失敗
					throw new Exception("LoadSoundMemByMemImage failed");

				this.Handles = new List<HandleInfo>();
				this.Handles.Add(new HandleInfo(handle));
				this.PlayIndex = 0;
			}
		}

		public void TryUnload()
		{
			if (this.Handles != null)
			{
				foreach (HandleInfo handle in this.Handles)
					if (DD.IsSoundPlaying(handle.Value)) // ? 再生中 -> アンロードしない。
						return;

				// 拡張したハンドルを先に解放
				foreach (HandleInfo handle in this.Handles.Skip(1))
					if (DX.DeleteSoundMem(handle.Value) != 0) // ? 失敗
						throw new Exception("DeleteSoundMem failed");

				// 本体のハンドルを解放
				if (DX.DeleteSoundMem(this.Handles[0].Value) != 0) // ? 失敗
					throw new Exception("DeleteSoundMem failed");

				this.Handles = null;
			}
		}

		private static List<ASoundEffect> PlayList = new List<ASoundEffect>();

		public static void EachFrame()
		{
			if (1 <= PlayList.Count)
			{
				ASoundEffect se = SCommon.DesertElement(PlayList, 0);

				if (se != null)
				{
					se.DoPlay();
				}
			}
		}

		public void Play()
		{
			if (PlayList.Where(v => v == this).Count() < 2)
			{
				PlayList.Add(this);
				PlayList.Add(null);
			}
		}

		private void DoPlay()
		{
			this.LoadIfNeeded();

			this.PlayIndex++;
			this.PlayIndex %= this.Handles.Count;

			if (IsPlaying(this.Handles[this.PlayIndex].Value))
			{
				this.PlayIndex = SCommon.IndexOf(this.Handles, v => !IsPlaying(v.Value));

				if (this.PlayIndex == -1)
				{
					this.Extend();
					this.PlayIndex = this.Handles.Count - 1;
				}
			}
			PlayByHandle(this.Handles[this.PlayIndex]);
		}

		private static bool IsPlaying(int handle)
		{
			switch (DX.CheckSoundMem(handle))
			{
				case 1: // ? 再生中
					return true;

				case 0: // ? 停止
					return false;

				case -1: // ? チェック失敗
					throw new Exception("CheckSoundMem failed");

				default: // ? エラー
					throw new Exception("CheckSoundMem error");
			}
		}

		private static void PlayByHandle(HandleInfo handle)
		{
			ChangeVolumeIfNeeded(handle);

			if (DX.PlaySoundMem(handle.Value, DX.DX_PLAYTYPE_BACK, 1) != 0) // ? 失敗
				throw new Exception("PlaySoundMem failed");
		}

		private static void ChangeVolumeIfNeeded(HandleInfo handle)
		{
			int volume = DD.RateToByte(GameSetting.SEVolume);

			if (handle.LastVolume != volume) // ? 前回の音量と違う -> 音量が変更されたので、新しい音量を適用する。
			{
				if (DX.ChangeVolumeSoundMem(volume, handle.Value) != 0) // ? 失敗
					throw new Exception("ChangeVolumeSoundMem failed");

				handle.LastVolume = volume;
			}
		}

		private void Extend()
		{
			int handle = DX.DuplicateSoundMem(this.Handles[0].Value);

			if (handle == -1) // ? 失敗
				throw new Exception("DuplicateSoundMem failed");

			this.Handles.Add(new HandleInfo(handle));
		}

		/// <summary>
		/// プログラム終了時の呼び出しを想定
		/// </summary>
		public static void StopImmediately_AtExitProgram()
		{
			foreach (ASoundEffect instance in Instances.Iterate())
				instance.StopImmediatelyIfNeeded();
		}

		private void StopImmediatelyIfNeeded()
		{
			if (this.Handles != null) // ? ロードされている。
				foreach (HandleInfo handle in this.Handles)
					if (DD.IsSoundPlaying(handle.Value)) // ? 再生中
						DX.StopSoundMem(handle.Value); // 停止
		}
	}
}
