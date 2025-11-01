using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class GuiDialogTickProfiler : GuiDialog
	{
		public GuiDialogTickProfiler(ICoreClientAPI capi)
			: base(capi)
		{
			this.root1sSum.Code = "root";
			capi.Event.RegisterGameTickListener(new Action<float>(this.OnEverySecond), 1000, 0);
			CairoFont font = CairoFont.WhiteDetailText();
			double lineHeight = font.GetFontExtents().Height * font.LineHeightMultiplier / (double)RuntimeEnv.GUIScale;
			ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.None, 0.0, 0.0, 450.0, 30.0 + (double)this.maxLines * lineHeight);
			ElementBounds overlayBounds = textBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
			base.SingleComposer = capi.Gui.CreateCompo("tickprofiler", dialogBounds).AddGameOverlay(overlayBounds, null).AddDynamicText("", font, textBounds, "text")
				.Compose(true);
		}

		private void OnEverySecond(float dt)
		{
			if (!this.IsOpened())
			{
				return;
			}
			StringBuilder sb = new StringBuilder();
			this.ticksToString(this.root1sSum, sb, "");
			string text = sb.ToString();
			string[] lines = text.Split('\n', StringSplitOptions.None);
			if (lines.Length > this.maxLines)
			{
				text = string.Join("\n", lines, 0, this.maxLines);
			}
			base.SingleComposer.GetDynamicText("text").SetNewText(text, true, false, false);
			this.frames = 0;
			this.root1sSum = new ProfileEntryRange();
			this.root1sSum.Code = "root";
		}

		private void ticksToString(ProfileEntryRange entry, StringBuilder strib, string indent = "")
		{
			double timeMS = (double)entry.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0 / (double)this.frames;
			if (timeMS < 0.2)
			{
				return;
			}
			string text;
			if (entry.Code.Length <= 37)
			{
				text = entry.Code;
			}
			else
			{
				string text2 = "...";
				string code2 = entry.Code;
				text = text2 + ((code2 != null) ? code2.Substring(Math.Max(0, entry.Code.Length - 40)) : null);
			}
			string code = text;
			strib.AppendLine(indent + string.Format("{0:0.00}ms, {1:####} calls/s, {2}", timeMS, entry.CallCount, code));
			List<ProfileEntryRange> profiles = new List<ProfileEntryRange>();
			if (entry.Marks != null)
			{
				profiles.AddRange(entry.Marks.Select((KeyValuePair<string, ProfileEntry> e) => new ProfileEntryRange
				{
					ElapsedTicks = (long)e.Value.ElapsedTicks,
					Code = e.Key,
					CallCount = e.Value.CallCount
				}));
			}
			if (entry.ChildRanges != null)
			{
				profiles.AddRange(entry.ChildRanges.Values);
			}
			foreach (ProfileEntryRange prof2 in profiles.OrderByDescending((ProfileEntryRange prof) => prof.ElapsedTicks).Take(25))
			{
				this.ticksToString(prof2, strib, indent + "  ");
			}
		}

		public override void OnFinalizeFrame(float dt)
		{
			if (!this.IsOpened())
			{
				return;
			}
			ProfileEntryRange entry = ScreenManager.FrameProfiler.PrevRootEntry;
			if (entry != null)
			{
				this.sumUpTickCosts(entry, this.root1sSum);
			}
			this.frames++;
			base.OnFinalizeFrame(dt);
		}

		private void sumUpTickCosts(ProfileEntryRange entry, ProfileEntryRange sumEntry)
		{
			sumEntry.ElapsedTicks += entry.ElapsedTicks;
			sumEntry.CallCount += entry.CallCount;
			if (entry.Marks != null)
			{
				if (sumEntry.Marks == null)
				{
					sumEntry.Marks = new Dictionary<string, ProfileEntry>();
				}
				foreach (KeyValuePair<string, ProfileEntry> val in entry.Marks)
				{
					ProfileEntry sumMark;
					if (!sumEntry.Marks.TryGetValue(val.Key, out sumMark))
					{
						sumMark = (sumEntry.Marks[val.Key] = new ProfileEntry(val.Value.ElapsedTicks, val.Value.CallCount));
					}
					sumMark.ElapsedTicks += val.Value.ElapsedTicks;
					sumMark.CallCount += val.Value.CallCount;
				}
			}
			if (entry.ChildRanges != null)
			{
				if (sumEntry.ChildRanges == null)
				{
					sumEntry.ChildRanges = new Dictionary<string, ProfileEntryRange>();
				}
				foreach (KeyValuePair<string, ProfileEntryRange> val2 in entry.ChildRanges)
				{
					ProfileEntryRange sumChild;
					if (!sumEntry.ChildRanges.TryGetValue(val2.Key, out sumChild))
					{
						sumChild = (sumEntry.ChildRanges[val2.Key] = new ProfileEntryRange());
						sumChild.Code = val2.Key;
					}
					this.sumUpTickCosts(val2.Value, sumChild);
				}
			}
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "tickprofiler";
			}
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			ScreenManager.FrameProfiler.Enabled = true;
		}

		public override void OnGuiClosed()
		{
			base.OnGuiClosed();
			ScreenManager.FrameProfiler.Enabled = false;
		}

		public override bool Focusable
		{
			get
			{
				return false;
			}
		}

		public override EnumDialogType DialogType
		{
			get
			{
				return EnumDialogType.HUD;
			}
		}

		private ProfileEntryRange root1sSum = new ProfileEntryRange();

		private int frames;

		private int maxLines = 20;
	}
}
