using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DxLibDLL;
using Charlotte.Commons;

namespace Charlotte.GameCommons
{
	/// <summary>
	/// 画像リソース
	/// このクラスのインスタンスはプロセスで有限個であること。
	/// 原則的に以下のクラスの静的フィールドとして植え込むこと。
	/// -- Pictures
	/// </summary>
	public class APicture
	{
		private static DD.Collector<APicture> Instances = new DD.Collector<APicture>();

		public static void UnloadAll()
		{
			foreach (APicture instance in Instances.Iterate())
				instance.Unload();
		}

		public class PictureDataInfo
		{
			public int Handle;
			public int W;
			public int H;
		}

		private Func<PictureDataInfo> PictureDataGetter;
		private Action<int> HandleUnloader;

		private PictureDataInfo PictureData = null;

		/// <summary>
		/// リソースから画像を作成する。
		/// </summary>
		/// <param name="resPath">リソースのパス</param>
		public APicture(string resPath)
			: this(() => DD.GetResFileData(resPath))
		{ }

		/// <summary>
		/// 画像データの取得メソッドから画像を作成する。
		/// </summary>
		/// <param name="getFileData">画像データの取得メソッド</param>
		public APicture(Func<DD.LzData> getFileData)
			: this(() => DD.GetPictureData(getFileData().Data.Value))
		{ }

		/// <summary>
		/// ローダーを指定して画像を作成する。
		/// </summary>
		/// <param name="loader">ローダー</param>
		public APicture(Func<PictureDataInfo> loader)
			: this(loader, handle =>
			{
				if (DX.DeleteGraph(handle) != 0) // ? 失敗
					throw new Exception("DeleteGraph failed");
			})
		{ }

		/// <summary>
		/// ローダーとアンローダーを指定して画像を作成する。
		/// </summary>
		/// <param name="loader">ローダー</param>
		/// <param name="unloader">アンローダー</param>
		public APicture(Func<PictureDataInfo> loader, Action<int> unloader)
		{
			this.PictureDataGetter = loader;
			this.HandleUnloader = unloader;

			Instances.Add(this);
		}

		private PictureDataInfo GetPictureData()
		{
			if (this.PictureData == null)
				this.PictureData = this.PictureDataGetter();

			return this.PictureData;
		}

		public void Unload()
		{
			if (this.PictureData != null)
			{
				this.HandleUnloader(this.PictureData.Handle);
				this.PictureData = null;
			}
		}

		public int GetHandle()
		{
			return this.GetPictureData().Handle;
		}

		public int W
		{
			get
			{
				return this.GetPictureData().W;
			}
		}

		public int H
		{
			get
			{
				return this.GetPictureData().H;
			}
		}
	}
}
