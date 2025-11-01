using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf
{
	public class GuiDialogConfirmRemapping : GuiDialog
	{
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		public GuiDialogConfirmRemapping(ICoreClientAPI capi)
			: base(capi)
		{
			capi.Event.ChatMessage += this.Event_ChatMessage;
		}

		private void Compose()
		{
			ElementBounds textBounds = ElementStdBounds.Rowed(0.4f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(500.0);
			TextDrawUtil textDrawUtil = new TextDrawUtil();
			CairoFont font = CairoFont.WhiteSmallText();
			string text = Lang.Get("requireremapping-text", Array.Empty<object>());
			float y = (float)textDrawUtil.GetMultilineTextHeight(font, text, textBounds.fixedWidth, EnumLinebreakBehavior.Default) / RuntimeEnv.GUIScale;
			ElementBounds switchBounds = ElementBounds.Fixed(0.0, (double)(y + 45f), 25.0, 25.0);
			ElementBounds buttonBounds = ElementStdBounds.MenuButton((y + 150f) / 100f, EnumDialogArea.CenterFixed).WithFixedPadding(6.0);
			ElementBounds bgBounds = ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding);
			base.SingleComposer = this.capi.Gui.CreateCompo("confirmremapping", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(bgBounds, true, 5.0, 0.75f).AddDialogTitleBar(Lang.Get("Upgrade required", Array.Empty<object>()), new Action(this.OnTitleBarClose), null, null, null)
				.BeginChildElements(bgBounds)
				.AddRichtext(text, font, textBounds, null)
				.AddSwitch(new Action<bool>(this.onToggleBackup), switchBounds, "switch", 25.0, 4.0)
				.AddStaticText(Lang.Get("remapper-backup", Array.Empty<object>()), font, switchBounds.RightCopy(10.0, 3.0, 0.0, 0.0).WithFixedWidth(500.0), null)
				.AddSmallButton(Lang.Get("Remind me later", Array.Empty<object>()), new ActionConsumable(this.onRemindMeLater), buttonBounds.FlatCopy().WithAlignment(EnumDialogArea.LeftFixed), EnumButtonStyle.Normal, null)
				.AddSmallButton(Lang.Get("No, Ignore", Array.Empty<object>()), new ActionConsumable(this.onIgnore), buttonBounds.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedAlignmentOffset(-180.0, 0.0), EnumButtonStyle.Normal, null)
				.AddSmallButton(Lang.Get("Ok, Apply now", Array.Empty<object>()), new ActionConsumable(this.onApplyNow), buttonBounds.FlatCopy().WithAlignment(EnumDialogArea.RightFixed), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			base.SingleComposer.GetSwitch("switch").On = this.genBackup;
		}

		private void onToggleBackup(bool on)
		{
			this.genBackup = on;
		}

		private void Event_ChatMessage(int groupId, string message, EnumChatType chattype, string data)
		{
			if (chattype == EnumChatType.CommandSuccess && data == "backupdone" && this.genBackup)
			{
				this.genBackup = false;
				this.capi.ShowChatMessage(Lang.Get("remappingwarning-wait", Array.Empty<object>()));
				this.capi.SendChatMessage("/fixmapping applyall", null);
			}
			if (chattype == EnumChatType.CommandSuccess && data == "fixmappingdone" && this.applynow)
			{
				this.applynow = false;
				if ((this.capi.World as ClientMain).IsSingleplayer)
				{
					this.reloadnow = true;
				}
			}
		}

		public override void OnFinalizeFrame(float dt)
		{
			if (this.reloadnow)
			{
				ActionConsumable<KeyCombination> handler = this.capi.Input.HotKeys["reloadworld"].Handler;
				if (handler == null)
				{
					return;
				}
				handler(null);
			}
		}

		private bool onApplyNow()
		{
			this.applynow = true;
			if (this.genBackup)
			{
				this.capi.SendChatMessage("/genbackup", null);
			}
			else
			{
				this.capi.ShowChatMessage(Lang.Get("remappingwarning-wait", Array.Empty<object>()));
				this.capi.SendChatMessage("/fixmapping applyall", null);
			}
			this.TryClose();
			return true;
		}

		private bool onIgnore()
		{
			this.capi.SendChatMessage("/fixmapping ignoreall", null);
			this.TryClose();
			return true;
		}

		private bool onRemindMeLater()
		{
			this.TryClose();
			return true;
		}

		private void OnTitleBarClose()
		{
			this.TryClose();
		}

		public override void OnGuiOpened()
		{
			this.Compose();
			base.OnGuiOpened();
		}

		public override void OnLevelFinalize()
		{
			this.capi.Logger.VerboseDebug("Handling LevelFinalize packet; requires remapping is " + (this.capi.World as ClientMain).ServerInfo.RequiresRemappings.ToString());
			if ((this.capi.World as ClientMain).ServerInfo.RequiresRemappings)
			{
				this.Compose();
				this.TryOpen();
			}
		}

		private bool genBackup;

		private bool applynow;

		private bool reloadnow;
	}
}
