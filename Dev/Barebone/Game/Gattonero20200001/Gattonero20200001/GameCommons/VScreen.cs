using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using DxLibDLL;
using Charlotte.Commons;
using Charlotte.Drawings;

namespace Charlotte.GameCommons
{
	/// <summary>
	/// スクリーン
	/// このクラスのインスタンスはプロセスで有限個であること。
	/// 原則的に任意のクラスの静的フィールドとして植え込むこと。
	/// </summary>
	public class VScreen
	{
		private static DD.Collector<VScreen> Instances = new DD.Collector<VScreen>();

		public static void UnloadAll()
		{
			ChangeDrawScreenToBack(); // 2bs

			foreach (VScreen instance in Instances.Iterate())
				instance.Unload();
		}

		public int W { get; private set; }
		public int H { get; private set; }

		private int Handle; // -1 == 未ロード

		public VScreen(int w, int h)
		{
			if (w < 1 || SCommon.IMAX < w)
				throw new Exception("Bad w");

			if (h < 1 || SCommon.IMAX < h)
				throw new Exception("Bad h");

			this.W = w;
			this.H = h;
			this.Handle = -1;

			Instances.Add(this);
		}

		public int GetHandle()
		{
			if (this.Handle == -1)
			{
				this.Handle = DX.MakeScreen(this.W, this.H, 0); // 幅, 高さ, 画像の透明度を有効にするか/1:有効/0:無効

				if (this.Handle == -1) // ? 失敗
					throw new Exception("MakeScreen failed");
			}
			return this.Handle;
		}

		public void Unload()
		{
			if (this.Handle != -1)
			{
				if (CurrentDrawScreen == this)
					throw new Exception("描画先スクリーンをアンロードしようとした。");

				if (DX.DeleteGraph(this.Handle) != 0) // ? 失敗
					throw new Exception("DeleteGraph failed");

				this.Handle = -1;

				if (this.Picture != null)
					this.Picture.Unload();
			}
		}

		public static VScreen CurrentDrawScreen = null; // null == DX.DX_SCREEN_BACK

		public static int GetCurrentDrawScreenHandle()
		{
			return CurrentDrawScreen == null ? DX.DX_SCREEN_BACK : VScreen.CurrentDrawScreen.GetHandle();
		}

		public void ChangeDrawScreenToThis()
		{
			if (DX.SetDrawScreen(this.GetHandle()) != 0) // ? 失敗
				throw new Exception("SetDrawScreen failed");

			CurrentDrawScreen = this;
		}

		public static void ChangeDrawScreenToBack()
		{
			if (DX.SetDrawScreen(DX.DX_SCREEN_BACK) != 0) // ? 失敗
				throw new Exception("SetDrawScreen failed");

			CurrentDrawScreen = null;
		}

		public IDisposable Section()
		{
			if (CurrentDrawScreen == this)
			{
				return SCommon.GetAnonyDisposable(() => { });
			}
			else
			{
				VScreen homeDrawScreen = CurrentDrawScreen;
				this.ChangeDrawScreenToThis();
				return SCommon.GetAnonyDisposable(() => ChangeDrawScreenTo(homeDrawScreen));
			}
		}

		private static void ChangeDrawScreenTo(VScreen screen)
		{
			if (screen != null)
				screen.ChangeDrawScreenToThis();
			else
				ChangeDrawScreenToBack();
		}

		private APicture Picture = null;

		public APicture GetPicture()
		{
			if (this.Picture == null)
			{
				this.Picture = new APicture(() => new APicture.PictureDataInfo()
				{
					Handle = this.GetHandle(),
					W = this.W,
					H = this.H,
				},
				handle => { }
				);
			}
			return this.Picture;
		}

		/// <summary>
		/// このスクリーンの内容を画像データに変換する。
		/// </summary>
		/// <returns>画像データ</returns>
		public byte[] GetImageData()
		{
			string bmpFile = DD.WD.MakePath() + ".bmp";
			string pngFile = DD.WD.MakePath() + ".png";

			using (this.Section())
			{
				DX.SaveDrawScreenToBMP(0, 0, this.W, this.H, bmpFile);
			}

			using (Bitmap bmp = (Bitmap)Bitmap.FromFile(bmpFile))
			{
				bmp.Save(pngFile, ImageFormat.Png);
			}

			byte[] imageData = File.ReadAllBytes(pngFile);

			SCommon.DeletePath(bmpFile);
			SCommon.DeletePath(pngFile);

			return imageData;
		}

		/// <summary>
		/// 画像データをこのスクリーンに描画する。
		/// </summary>
		/// <param name="imageData">画像データ</param>
		public void SetImageData(byte[] imageData)
		{
			int handle = DD.GetPictureData(imageData).Handle;

			using (this.Section())
			{
				DX.DrawExtendGraph(0, 0, this.W, this.H, handle, 0);
			}

			DX.DeleteGraph(handle);
		}

		private string StoredImageFile = null;

		private void StoreImageDataIfLoaded()
		{
			if (this.Handle != -1)
			{
				this.StoredImageFile = DD.WD.MakePath();
				File.WriteAllBytes(this.StoredImageFile, this.GetImageData());
			}
		}

		private void RestoreImageDataIfStored()
		{
			if (this.StoredImageFile != null)
			{
				this.SetImageData(File.ReadAllBytes(this.StoredImageFile));
				SCommon.DeletePath(this.StoredImageFile);
				this.StoredImageFile = null;
			}
		}

		public static void StoreImageDataIfLoadedForAll()
		{
			foreach (VScreen instance in Instances.Iterate())
			{
				instance.StoreImageDataIfLoaded();
			}
		}

		public static void RestoreImageDataIfStoredForAll()
		{
			foreach (VScreen instance in Instances.Iterate())
			{
				instance.RestoreImageDataIfStored();
			}
		}
	}
}
