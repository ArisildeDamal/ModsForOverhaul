using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	internal class GuiScreenGetUpdate : GuiScreen
	{
		public GuiScreenGetUpdate(string versionnumber, ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.versionnumber = versionnumber;
			this.ShowMainMenu = false;
			this.Compose();
			this.BeginDownload();
		}

		private void Compose()
		{
			CairoFont.WhiteSmallText().WithFontSize(17f).WithLineHeightMultiplier(1.25);
			ElementBounds titleBounds = ElementBounds.Fixed(0.0, 0.0, 500.0, 50.0);
			ElementBounds btnBounds = ElementBounds.Fixed(0.0, 90.0, 0.0, 0.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPadding(10.0, 2.0);
			this.ElementComposer = base.dialogBase("mainmenu-confirmaction", -1.0, 160.0).AddStaticText(Lang.Get("Download in progress", Array.Empty<object>()), CairoFont.WhiteSmallishText().WithWeight(FontWeight.Bold), titleBounds, null).AddDynamicText("Downloading releases meta information...", CairoFont.WhiteSmallText(), titleBounds = titleBounds.BelowCopy(0.0, 10.0, 0.0, 0.0).WithFixedWidth(500.0), "status")
				.AddButton(Lang.Get("Cancel", Array.Empty<object>()), new ActionConsumable(this.OnCancel), btnBounds.FixedUnder(titleBounds, 10.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
		}

		private bool OnCancel()
		{
			this._gameReleaseVersionCts.Cancel();
			this.ScreenManager.StartMainMenu();
			return false;
		}

		private void BeginDownload()
		{
			Task.Run(async delegate
			{
				ScreenManager.Platform.Logger.Notification("Retrieving releases meta data");
				try
				{
					this._gameReleaseVersionCts = new CancellationTokenSource();
					string stringAsync = await VSWebClient.Inst.GetStringAsync("http://api.vintagestory.at/stable-unstable.json", this._gameReleaseVersionCts.Token);
					this.releases = JsonUtil.FromString<Dictionary<string, GameReleaseVersion>>(stringAsync);
					this.onReleasesDownloadComplete(null);
				}
				catch (Exception e)
				{
					this.onReleasesDownloadComplete(e);
				}
			});
		}

		private void onReleasesDownloadComplete(Exception errorExc)
		{
			if (errorExc == null)
			{
				this.downloadInstaller(this.releases[this.versionnumber]);
				return;
			}
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				GuiElementDynamicText dynamicText = this.ElementComposer.GetDynamicText("status");
				string text = "Download failed: ";
				Exception errorExc2 = errorExc;
				dynamicText.SetNewText(text + ((errorExc2 != null) ? errorExc2.ToString() : null), false, false, false);
			});
		}

		private void downloadInstaller(GameReleaseVersion release)
		{
			GuiScreenGetUpdate.<>c__DisplayClass9_0 CS$<>8__locals1 = new GuiScreenGetUpdate.<>c__DisplayClass9_0();
			CS$<>8__locals1.<>4__this = this;
			if (release == null)
			{
				this.ElementComposer.GetDynamicText("status").SetNewText(Lang.Get("Download failed. Release {0} not found, possibly programming error. Please send us a bug report", new object[] { this.ScreenManager.newestVersion }), false, false, false);
				return;
			}
			CS$<>8__locals1.build = release.WindowsUpdate ?? release.Windows;
			CS$<>8__locals1.dstPath = Path.Combine(Path.GetTempPath(), CS$<>8__locals1.build.filename);
			CS$<>8__locals1.fileStream = File.Create(CS$<>8__locals1.dstPath);
			this._gameReleaseDownloadCts = new CancellationTokenSource();
			CS$<>8__locals1.progress = new Progress<Tuple<int, long>>();
			CS$<>8__locals1.progress.ProgressChanged += this.onProgress;
			Task.Run(delegate
			{
				GuiScreenGetUpdate.<>c__DisplayClass9_0.<<downloadInstaller>b__0>d <<downloadInstaller>b__0>d;
				<<downloadInstaller>b__0>d.<>t__builder = AsyncTaskMethodBuilder.Create();
				<<downloadInstaller>b__0>d.<>4__this = CS$<>8__locals1;
				<<downloadInstaller>b__0>d.<>1__state = -1;
				<<downloadInstaller>b__0>d.<>t__builder.Start<GuiScreenGetUpdate.<>c__DisplayClass9_0.<<downloadInstaller>b__0>d>(ref <<downloadInstaller>b__0>d);
				return <<downloadInstaller>b__0>d.<>t__builder.Task;
			});
		}

		private void onProgress(object sender, Tuple<int, long> progress)
		{
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				this.ElementComposer.GetDynamicText("status").SetNewText(Lang.Get("{0:0.#}% complete ({1:0.0} of {2:0.#} MB)", new object[]
				{
					(int)(100.0 * (double)progress.Item1 / (double)progress.Item2),
					(double)progress.Item1 / 1024.0 / 1024.0,
					(double)progress.Item2 / 1024.0 / 1024.0
				}), false, false, false);
			});
		}

		private CancellationTokenSource _gameReleaseVersionCts;

		private CancellationTokenSource _gameReleaseDownloadCts;

		private string versionnumber;

		private Dictionary<string, GameReleaseVersion> releases;
	}
}
