using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class HudDialogChat : HudElement
	{
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "beginchat";
			}
		}

		public override double InputOrder
		{
			get
			{
				return 1.1;
			}
		}

		public override double DrawOrder
		{
			get
			{
				return 0.0;
			}
		}

		public HudDialogChat(ICoreClientAPI capi)
			: base(capi)
		{
			this.eventAttr = new TreeAttribute();
			this.eventAttr["key"] = new IntAttribute();
			this.eventAttr["text"] = new StringAttribute();
			this.eventAttr["scrolltoEnd"] = new BoolAttribute();
			this.game = capi.World as ClientMain;
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.OnNewServerToClientChatLine.Add(new ChatLineDelegate(this.OnNewServerToClientChatLine));
			}
			ClientEventManager eventManager2 = this.game.eventManager;
			if (eventManager2 != null)
			{
				eventManager2.OnNewClientToServerChatLine.Add(new ChatLineDelegate(this.OnNewClientToServerChatLine));
			}
			ClientEventManager eventManager3 = this.game.eventManager;
			if (eventManager3 != null)
			{
				eventManager3.OnNewClientOnlyChatLine.Add(new ChatLineDelegate(this.OnNewClientOnlyChatLine));
			}
			this.game.ChatHistoryByPlayerGroup[GlobalConstants.GeneralChatGroup] = new LimitedList<string>(HudDialogChat.historyMax);
			this.ComposeChatGuis();
			this.Composers["chat"].UnfocusOwnElements();
			this.UpdateText();
			CommandArgumentParsers parsers = this.game.api.ChatCommands.Parsers;
			this.game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("recomposechat").RequiresPrivilege(Privilege.chat)
				.WithDescription("Recompose chat dialogs")
				.HandleWith(new OnCommandDelegate(this.CmdChatC))
				.EndSubCommand();
			this.game.api.ChatCommands.Create("clearchat").WithDescription("Clear all chat history").HandleWith(new OnCommandDelegate(this.CmdClearChat));
			this.game.api.ChatCommands.Create("chatsize").WithDescription("Set the chat dialog width and height (default 400x160)").WithArgs(new ICommandArgumentParser[]
			{
				parsers.OptionalInt("width", 700),
				parsers.OptionalInt("height", 200)
			})
				.HandleWith(new OnCommandDelegate(this.CmdChatSize));
			this.game.api.ChatCommands.Create("pastemode").WithDescription("Set the chats paste mode. If set to multi pasting multiple lines will produce multiple chat lines.").WithArgs(new ICommandArgumentParser[] { parsers.WordRange("mode", new string[] { "single", "multi" }) })
				.HandleWith(new OnCommandDelegate(this.CmdPasteMode));
			this.game.PacketHandlers[50] = new ServerPacketHandler<Packet_Server>(this.HandlePlayerGroupPacket);
			this.game.PacketHandlers[49] = new ServerPacketHandler<Packet_Server>(this.HandlePlayerGroupsPacket);
			this.game.PacketHandlers[57] = new ServerPacketHandler<Packet_Server>(this.HandleGotoGroupPacket);
			ScreenManager.hotkeyManager.SetHotKeyHandler("chatdialog", new ActionConsumable<KeyCombination>(this.OnKeyCombinationTab), true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("beginclientcommand", delegate(KeyCombination kc)
			{
				this.OnKeyCombinationTab(kc);
				this.OnKeyCombinationToggle(kc);
				this.Composers["chat"].GetChatInput("chatinput").SetValue(".", true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("beginservercommand", delegate(KeyCombination kc)
			{
				this.OnKeyCombinationTab(kc);
				this.OnKeyCombinationToggle(kc);
				this.Composers["chat"].GetChatInput("chatinput").SetValue("/", true);
				return true;
			}, true);
			this.game.api.RegisterLinkProtocol("screenshot", new Action<LinkTextComponent>(this.onLinkClicked));
			this.game.api.RegisterLinkProtocol("chattype", new Action<LinkTextComponent>(this.onChatType));
			this.game.api.RegisterLinkProtocol("datafolder", new Action<LinkTextComponent>(this.onDataFolderLinkClicked));
		}

		private void onDataFolderLinkClicked(LinkTextComponent comp)
		{
			string[] comps = comp.Href.Split(new string[] { "://" }, StringSplitOptions.RemoveEmptyEntries);
			if (comps.Length == 2 && comps[1] == "worldedit")
			{
				NetUtil.OpenUrlInBrowser(Path.Combine(GamePaths.DataPath, "WorldEdit"));
			}
		}

		private void onLinkClicked(LinkTextComponent comp)
		{
			string[] comps = comp.Href.Split(new string[] { "://" }, StringSplitOptions.RemoveEmptyEntries);
			if (comps.Length == 2 && Regex.IsMatch(comps[1], "[\\d\\w\\-]+\\.png"))
			{
				string path = Path.Combine(GamePaths.Screenshots, comps[1]);
				if (File.Exists(path))
				{
					NetUtil.OpenUrlInBrowser(path);
				}
			}
		}

		public override void OnOwnPlayerDataReceived()
		{
			if (ClientSettings.ChatDialogVisible)
			{
				this.TryOpen();
				this.UnFocus();
			}
		}

		private TextCommandResult CmdPasteMode(TextCommandCallingArgs args)
		{
			this.MultiCommandPasteMode = args[0] as string == "multi";
			string text = "Pastemode ";
			object obj = args[0];
			return TextCommandResult.Success(text + ((obj != null) ? obj.ToString() : null) + " set.", null);
		}

		private TextCommandResult CmdClearChat(TextCommandCallingArgs textCommandCallingArgs)
		{
			foreach (KeyValuePair<int, LimitedList<string>> val in this.game.ChatHistoryByPlayerGroup)
			{
				val.Value.Clear();
			}
			this.UpdateText();
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult CmdChatC(TextCommandCallingArgs textCommandCallingArgs)
		{
			this.ComposeChatGuis();
			this.UpdateText();
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult CmdChatSize(TextCommandCallingArgs args)
		{
			ClientSettings.ChatWindowWidth = (this.chatWindowInnerWidth = (int)args[0]);
			ClientSettings.ChatWindowHeight = (this.chatWindowInnerHeight = (int)args[1]);
			this.ComposeChatGuis();
			this.UpdateText();
			return TextCommandResult.Success("", null);
		}

		private void ComposeChatGuis()
		{
			base.ClearComposers();
			int outerWidth = this.horPadding + this.chatWindowInnerWidth + this.scrollbarWidth + this.horPadding;
			int outerHeight = this.tabsHeight + this.verPadding + this.chatWindowInnerHeight + this.verPadding + this.chatInputHeight + this.verPadding;
			int chatInputYPos = this.tabsHeight + this.verPadding + this.chatWindowInnerHeight + this.verPadding;
			int chatTextBottomOffset = this.bottomOffset + this.chatInputHeight + 3 * this.verPadding;
			int scrollbarHeight = this.chatWindowInnerHeight - 2 * this.scrollbarPadding + 2 * this.verPadding - 1;
			ElementBounds dialogBounds = ElementBounds.Fixed(EnumDialogArea.LeftBottom, 0.0, 0.0, (double)outerWidth, (double)outerHeight).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, (double)(-(double)this.bottomOffset));
			ElementBounds chatTextDialogBg = ElementBounds.Fixed(EnumDialogArea.LeftBottom, (double)this.horPadding, (double)this.verPadding, (double)this.chatWindowInnerWidth, (double)this.chatWindowInnerHeight).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, (double)(-(double)chatTextBottomOffset));
			ElementBounds clipBounds = ElementBounds.Fixed(0.0, 0.0, (double)this.chatWindowInnerWidth, (double)this.chatWindowInnerHeight);
			ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, (double)this.chatWindowInnerWidth, (double)this.chatWindowInnerHeight);
			this.tabs = new GuiTab[this.game.OwnPlayerGroupsById.Count];
			int i = 0;
			foreach (KeyValuePair<int, PlayerGroup> val in this.game.OwnPlayerGroupsById)
			{
				this.tabs[i++] = new GuiTab
				{
					DataInt = val.Key,
					Name = val.Value.Name
				};
			}
			CairoFont font = CairoFont.WhiteDetailText().WithFontSize(17f);
			this.Composers["chat"] = this.capi.Gui.CreateCompo("chatdialog", dialogBounds).AddGameOverlay(ElementBounds.Fixed(0.0, (double)this.tabsHeight, (double)outerWidth, (double)outerHeight), GuiStyle.DialogLightBgColor).AddChatInput(ElementBounds.Fixed(0.0, (double)chatInputYPos, (double)outerWidth, (double)this.chatInputHeight), new Action<string>(this.OnTextChanged), "chatinput")
				.AddCompactVerticalScrollbar(new Action<float>(this.OnNewScrollbarValue), ElementBounds.Fixed((double)(outerWidth - this.scrollbarWidth), (double)(this.tabsHeight + this.scrollbarPadding), (double)this.scrollbarWidth, (double)scrollbarHeight), "scrollbar")
				.AddHorizontalTabs(this.tabs, ElementBounds.Fixed(0.0, 0.0, (double)outerWidth, (double)this.tabsHeight), new Action<int>(this.OnTabClicked), font, font.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
				.Compose(true);
			CairoFont alarmFont = font.Clone().WithColor(GuiStyle.DialogDefaultTextColor);
			this.Composers["chat"].GetHorizontalTabs("tabs").WithAlarmTabs(alarmFont);
			this.Composers["chat-group-" + GlobalConstants.GeneralChatGroup.ToString()] = this.capi.Gui.CreateCompo("chat-group-" + GlobalConstants.GeneralChatGroup.ToString(), chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
				.EndClip()
				.Compose(true);
			this.Composers["chat-group-" + GlobalConstants.DamageLogChatGroup.ToString()] = this.capi.Gui.CreateCompo("chat-group-damagelog", chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
				.EndClip()
				.Compose(true);
			this.Composers["chat-group-" + GlobalConstants.InfoLogChatGroup.ToString()] = this.capi.Gui.CreateCompo("chat-group-infolog", chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
				.EndClip()
				.Compose(true);
			this.Composers["chat-group-" + GlobalConstants.ServerInfoChatGroup.ToString()] = this.capi.Gui.CreateCompo("chat-group-" + GlobalConstants.ServerInfoChatGroup.ToString(), chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
				.EndClip()
				.Compose(true);
			foreach (PlayerGroup group in this.game.OwnPlayerGroupsById.Values)
			{
				GuiComposer guiComposer = this.Composers["chat-group-" + group.Uid.ToString()];
				if (guiComposer != null)
				{
					guiComposer.Dispose();
				}
				this.Composers["chat-group-" + group.Uid.ToString()] = this.capi.Gui.CreateCompo("chat-group-" + group.Uid.ToString(), chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
					.EndClip()
					.Compose(true);
			}
			this.Composers["chat"].GetCompactScrollbar("scrollbar").SetHeights((float)scrollbarHeight, (float)scrollbarHeight);
			this.Composers["chat"].UnfocusOwnElements();
		}

		private void OnTabClicked(int groupId)
		{
			this.game.currentGroupid = groupId;
			int tabIndex = this.tabIndexByGroupId(groupId);
			this.Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex] = false;
			if (!this.game.ChatHistoryByPlayerGroup.ContainsKey(this.game.currentGroupid))
			{
				this.game.ChatHistoryByPlayerGroup[this.game.currentGroupid] = new LimitedList<string>(HudDialogChat.historyMax);
			}
			this.UpdateText();
		}

		private void OnNewScrollbarValue(float value)
		{
			GuiElementRichtext richtext = this.Composers["chat-group-" + this.game.currentGroupid.ToString()].GetRichtext("chathistory");
			richtext.Bounds.fixedY = (double)(0f - value);
			richtext.Bounds.CalcWorldBounds();
			this.lastActivityMs = this.game.Platform.EllapsedMs;
		}

		private void UpdateText()
		{
			GuiElementRichtext textElem = this.Composers["chat-group-" + this.game.currentGroupid.ToString()].GetRichtext("chathistory");
			LimitedList<string> limitedList = this.game.ChatHistoryByPlayerGroup[this.game.currentGroupid];
			StringBuilder fullchattext = new StringBuilder();
			int i = 0;
			foreach (object obj in limitedList)
			{
				string line = (string)obj;
				if (i++ > 0)
				{
					fullchattext.Append("\r\n");
				}
				fullchattext.Append(line);
			}
			textElem.SetNewText(fullchattext.ToString(), CairoFont.WhiteDetailText().WithFontSize(17f), null);
			GuiElementScrollbar scrollbarElem = this.Composers["chat"].GetCompactScrollbar("scrollbar");
			scrollbarElem.SetNewTotalHeight((float)textElem.Bounds.fixedHeight + 5f);
			if (!scrollbarElem.mouseDownOnScrollbarHandle)
			{
				scrollbarElem.ScrollToBottom();
			}
		}

		private void OnTextChanged(string text)
		{
		}

		public override void OnRenderGUI(float deltaTime)
		{
			double alpha = (this.focused ? 1.0 : Math.Max(0.5, this.lastAlpha - (double)(deltaTime / 6f)));
			this.lastAlpha = alpha;
			foreach (KeyValuePair<string, GuiComposer> val in ((IEnumerable<KeyValuePair<string, GuiComposer>>)this.Composers))
			{
				if (val.Key == "chat")
				{
					if (val.Value.Color == null)
					{
						val.Value.Color = new Vec4f(1f, 1f, 1f, 1f);
					}
					val.Value.Color.W = (float)this.lastAlpha;
					val.Value.Render(deltaTime);
				}
				else if (val.Key == "chat-group-" + this.game.currentGroupid.ToString())
				{
					val.Value.Render(deltaTime);
				}
			}
		}

		public override void OnFinalizeFrame(float dt)
		{
			foreach (KeyValuePair<string, GuiComposer> val in ((IEnumerable<KeyValuePair<string, GuiComposer>>)this.Composers))
			{
				val.Value.PostRender(dt);
			}
			if (this.Focused)
			{
				this.lastActivityMs = this.game.Platform.EllapsedMs;
			}
			if (ClientSettings.AutoChat)
			{
				if (this.IsOpened() && this.game.Platform.EllapsedMs - this.lastActivityMs > 15000L)
				{
					this.DoClose();
				}
				if (!this.IsOpened() && this.game.Platform.EllapsedMs - this.lastActivityMs < 50L && this.lastMessageInGroupId > -99)
				{
					int groupId = this.lastMessageInGroupId;
					if (groupId == GlobalConstants.CurrentChatGroup)
					{
						groupId = this.game.currentGroupid;
					}
					if (ClientSettings.AutoChatOpenSelected)
					{
						if (groupId == this.game.currentGroupid)
						{
							this.TryOpen();
							int tabIndex = this.tabIndexByGroupId(groupId);
							if (tabIndex >= 0)
							{
								this.Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex] = false;
							}
							this.UpdateText();
							return;
						}
					}
					else
					{
						this.TryOpen();
						int tabIndex2 = this.tabIndexByGroupId(groupId);
						if (tabIndex2 >= 0)
						{
							this.Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex2] = false;
							this.Composers["chat"].GetHorizontalTabs("tabs").SetValue(tabIndex2, false);
						}
						this.game.currentGroupid = groupId;
						this.UpdateText();
					}
				}
			}
		}

		public override bool OnEscapePressed()
		{
			return this.TryClose();
		}

		public override bool IsOpened(string dialogComposerName)
		{
			return this.IsOpened() && dialogComposerName == "chat-group-" + this.game.currentGroupid.ToString();
		}

		public override void UnFocus()
		{
			this.Composers["chat"].UnfocusOwnElements();
			base.UnFocus();
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
		}

		public override void OnGuiClosed()
		{
			base.OnGuiClosed();
			this.Composers["chat"].UnfocusOwnElements();
			this.typedMessagesHistoryPos = -1;
		}

		public override bool TryClose()
		{
			this.UnFocus();
			return false;
		}

		public void DoClose()
		{
			this.lastActivityMs = -100000L;
			this.lastMessageInGroupId = -9999;
			base.TryClose();
		}

		private bool OnKeyCombinationTab(KeyCombination viaKeyComb)
		{
			if (!this.IsOpened())
			{
				ClientSettings.ChatDialogVisible = true;
				this.opened = true;
				this.OnGuiOpened();
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager != null)
				{
					eventManager.TriggerDialogOpened(this);
				}
				this.lastActivityMs = this.game.Platform.EllapsedMs;
				this.lastMessageInGroupId = -9999;
			}
			else
			{
				ClientSettings.ChatDialogVisible = false;
				this.UnFocus();
				this.DoClose();
			}
			return true;
		}

		private void onChatType(LinkTextComponent link)
		{
			if (!this.IsOpened())
			{
				ClientSettings.ChatDialogVisible = true;
				this.TryOpen();
			}
			this.Focus();
			this.capi.Gui.RequestFocus(this);
			this.Composers["chat"].FocusElement(0);
			GuiElementChatInput chatInput = this.Composers["chat"].GetChatInput("chatinput");
			string text = chatInput.GetText();
			chatInput.SetValue((this.isLinkChatTyped ? "" : text) + link.Href.Substring("chattype://".Length).Replace("&lt;", "<").Replace("&gt;", ">"), true);
			this.isLinkChatTyped = true;
		}

		internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
		{
			if (!this.IsOpened())
			{
				ClientSettings.ChatDialogVisible = true;
				this.TryOpen();
			}
			this.Focus();
			this.capi.Gui.RequestFocus(this);
			this.Composers["chat"].FocusElement(0);
			string keyName = GlKeyNames.GetPrintableChar(viaKeyComb.KeyCode);
			if (!viaKeyComb.Alt && !viaKeyComb.Ctrl && !viaKeyComb.Shift && !string.IsNullOrWhiteSpace(keyName))
			{
				this.ignoreNextKeyPress = true;
			}
			return true;
		}

		public override void OnKeyPress(KeyEvent args)
		{
			if (!this.IsOpened())
			{
				return;
			}
			base.OnKeyPress(args);
		}

		public override void OnKeyDown(KeyEvent args)
		{
			if (!this.IsOpened())
			{
				return;
			}
			GuiElementChatInput elem = this.Composers["chat"].GetChatInput("chatinput");
			if (args.KeyCode == 50)
			{
				this.UnFocus();
				args.Handled = true;
				return;
			}
			string text = elem.GetText();
			if (args.KeyCode == 49 || args.KeyCode == 82)
			{
				if (text.Length != 0)
				{
					EnumHandling handling = EnumHandling.PassThrough;
					this.game.api.eventapi.TriggerSendChatMessage(this.game.currentGroupid, ref text, ref handling);
					if (handling == EnumHandling.PassThrough)
					{
						ClientEventManager eventManager = this.game.eventManager;
						if (eventManager != null)
						{
							eventManager.TriggerNewClientChatLine(this.game.currentGroupid, text, EnumChatType.OwnMessage, null);
						}
					}
					if (this.typedMessagesHistoryPos != 0 || elem.GetText() != this.GetHistoricalMessage(this.typedMessagesHistoryPos))
					{
						this.typedMessagesHistory.Add(elem.GetText());
					}
					elem.SetValue("", true);
				}
				this.UnFocus();
				this.typedMessagesHistoryPos = -1;
				args.Handled = true;
				this.isLinkChatTyped = false;
				return;
			}
			if (args.KeyCode == 45 && this.typedMessagesHistoryPos < this.typedMessagesHistory.Count - 1)
			{
				this.typedMessagesHistoryPos++;
				elem.SetValue(this.GetHistoricalMessage(this.typedMessagesHistoryPos), true);
				elem.SetCaretPos(elem.GetText().Length, 0);
				args.Handled = true;
				return;
			}
			if (args.KeyCode == 46 && this.typedMessagesHistoryPos >= 0 && this.typedMessagesHistory.Count > 0)
			{
				this.typedMessagesHistoryPos--;
				if (this.typedMessagesHistoryPos < 0)
				{
					elem.SetValue("", true);
				}
				else
				{
					elem.SetValue(this.GetHistoricalMessage(this.typedMessagesHistoryPos), true);
				}
				elem.SetCaretPos(elem.GetText().Length, 0);
				args.Handled = true;
				return;
			}
			if (args.KeyCode == 104 && args.CtrlPressed)
			{
				string insert = this.capi.Forms.GetClipboardText();
				insert = insert.Replace("\ufeff", "");
				if (this.MultiCommandPasteMode || insert.StartsWithOrdinal(".pastemode multi"))
				{
					string[] lines = Regex.Split(insert, "(\r\n|\n|\r)");
					for (int i = 0; i < lines.Length; i++)
					{
						ClientEventManager eventManager2 = this.game.eventManager;
						if (eventManager2 != null)
						{
							eventManager2.TriggerNewClientChatLine(this.game.currentGroupid, lines[i], EnumChatType.OwnMessage, null);
						}
					}
					args.Handled = true;
					return;
				}
			}
			ScalarAttribute<int> scalarAttribute = this.eventAttr["key"] as IntAttribute;
			StringAttribute textAttr = this.eventAttr["text"] as StringAttribute;
			this.eventAttr.SetInt("deltacaretpos", 0);
			scalarAttribute.value = args.KeyCode;
			textAttr.value = text;
			this.game.api.eventapi.PushEvent("chatkeydownpre", this.eventAttr);
			if (text != textAttr.value)
			{
				elem.SetValue(textAttr.value, true);
			}
			base.OnKeyDown(args);
			textAttr.value = elem.GetText();
			this.game.api.eventapi.PushEvent("chatkeydownpost", this.eventAttr);
			if (textAttr.value != elem.GetText())
			{
				elem.SetValue(textAttr.value, false);
				text = textAttr.value;
				if (this.eventAttr.GetInt("deltacaretpos", 0) != 0)
				{
					elem.SetCaretPos(elem.CaretPosInLine + this.eventAttr.GetInt("deltacaretpos", 0), 0);
				}
			}
			if (text.Length == 0)
			{
				this.isLinkChatTyped = false;
			}
			if (ScreenManager.hotkeyManager.GetHotKeyByCode("chatdialog").DidPress(args, this.game, this.game.player, true))
			{
				this.DoClose();
				this.UnFocus();
				args.Handled = true;
				return;
			}
			if (!this.focused)
			{
				return;
			}
			args.Handled = true;
		}

		public override void OnMouseUp(MouseEvent args)
		{
			base.OnMouseUp(args);
			if (this.IsOpened())
			{
				GuiElement elem = this.Composers["chat"].GetChatInput("chatinput");
				if (elem.IsPositionInside(args.X, args.Y))
				{
					this.Composers["chat"].FocusElement(elem.TabIndex);
				}
			}
		}

		public string GetHistoricalMessage(int typedMessagesHistoryPos)
		{
			int pos = this.typedMessagesHistory.Count - 1 - typedMessagesHistoryPos;
			if (pos < 0 || pos >= this.typedMessagesHistory.Count)
			{
				return null;
			}
			return this.typedMessagesHistory[pos];
		}

		private int tabIndexByGroupId(int groupId)
		{
			for (int i = 0; i < this.tabs.Length; i++)
			{
				if (this.tabs[i].DataInt == groupId)
				{
					return i;
				}
			}
			return -1;
		}

		private void OnNewServerToClientChatLine(int groupId, string message, EnumChatType chattype, string data)
		{
			if (groupId != this.game.currentGroupid)
			{
				int tabIndex = this.tabIndexByGroupId(groupId);
				if (tabIndex >= 0)
				{
					this.Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex] = true;
				}
			}
			if (message.Contains("</hk>", StringComparison.InvariantCulture))
			{
				int i = message.IndexOfOrdinal("<hk>");
				int j = message.IndexOfOrdinal("</hk>");
				if (j > i)
				{
					string hotkeycode = message.Substring(i + 4, j - i - 4);
					HotKey hotkey;
					if (this.capi.Input.HotKeys.TryGetValue(hotkeycode.ToLowerInvariant(), out hotkey))
					{
						message = message.Substring(0, i) + hotkey.CurrentMapping.ToString() + message.Substring(j + 5);
					}
				}
			}
			if ((chattype == EnumChatType.Notification || chattype == EnumChatType.CommandSuccess) && groupId != GlobalConstants.InfoLogChatGroup)
			{
				message = "<font color=\"#CCe0cfbb\">" + message + "</font>";
			}
			if (chattype != EnumChatType.OthersMessage && chattype != EnumChatType.JoinLeave && ClientSettings.AutoChat && ClientSettings.AutoChatOpenSelected && groupId != GlobalConstants.DamageLogChatGroup && groupId != GlobalConstants.AllChatGroups && groupId != GlobalConstants.ServerInfoChatGroup)
			{
				int tabIndex2 = this.tabIndexByGroupId(groupId);
				if (tabIndex2 >= 0)
				{
					this.Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex2] = false;
					this.Composers["chat"].GetHorizontalTabs("tabs").SetValue(tabIndex2, false);
				}
				if (groupId != GlobalConstants.CurrentChatGroup)
				{
					this.game.currentGroupid = groupId;
				}
			}
			if (groupId == GlobalConstants.AllChatGroups)
			{
				foreach (int memberGroupId in this.game.ChatHistoryByPlayerGroup.Keys)
				{
					this.game.ChatHistoryByPlayerGroup[memberGroupId].Add(message);
				}
				this.UpdateText();
				this.lastActivityMs = this.game.Platform.EllapsedMs;
				this.lastMessageInGroupId = this.game.currentGroupid;
				return;
			}
			if (groupId == GlobalConstants.CurrentChatGroup)
			{
				groupId = this.game.currentGroupid;
			}
			if (!this.game.ChatHistoryByPlayerGroup.ContainsKey(groupId))
			{
				this.game.ChatHistoryByPlayerGroup[groupId] = new LimitedList<string>(HudDialogChat.historyMax);
			}
			this.game.ChatHistoryByPlayerGroup[groupId].Add(message);
			if (this.game.currentGroupid == groupId)
			{
				this.UpdateText();
			}
			if (groupId != GlobalConstants.ServerInfoChatGroup && groupId != GlobalConstants.DamageLogChatGroup)
			{
				this.lastActivityMs = this.game.Platform.EllapsedMs;
				this.lastMessageInGroupId = groupId;
			}
		}

		private void OnNewClientToServerChatLine(int groupId, string message, EnumChatType chattype, string data)
		{
			this.HandleClientMessage(groupId, message);
			if (!message.StartsWithOrdinal(ChatCommandApi.ServerCommandPrefix) && !message.StartsWithOrdinal(ChatCommandApi.ClientCommandPrefix) && groupId != GlobalConstants.ServerInfoChatGroup && groupId != GlobalConstants.DamageLogChatGroup)
			{
				this.lastActivityMs = this.game.Platform.EllapsedMs;
				this.lastMessageInGroupId = groupId;
			}
		}

		private void OnNewClientOnlyChatLine(int groupId, string message, EnumChatType chattype, string data)
		{
			if (message == "" || message == null)
			{
				return;
			}
			if (message.StartsWithOrdinal(ChatCommandApi.ClientCommandPrefix))
			{
				this.HandleClientCommand(message, groupId);
				return;
			}
			this.game.ShowChatMessage(message);
		}

		public void HandleClientCommand(string message, int groupid)
		{
			message = message.Substring(1);
			int argsStart = message.IndexOf(' ');
			string args;
			string command;
			if (argsStart > 0)
			{
				args = message.Substring(argsStart + 1);
				command = message.Substring(0, argsStart);
			}
			else
			{
				args = "";
				command = message;
			}
			this.game.api.chatcommandapi.Execute(command, this.game.player, groupid, args, null);
		}

		public void HandleClientMessage(int groupid, string message)
		{
			if (message == "" || message == null)
			{
				return;
			}
			if (message.StartsWithOrdinal(ChatCommandApi.ClientCommandPrefix))
			{
				this.HandleClientCommand(message, groupid);
				return;
			}
			message = message.Substring(0, Math.Min(1024, message.Length));
			this.game.SendPacketClient(ClientPackets.Chat(groupid, message, null));
		}

		private void HandleGotoGroupPacket(Packet_Server packet)
		{
			int groupId = packet.GotoGroup.GroupId;
			if (this.game.OwnPlayerGroupsById.ContainsKey(groupId))
			{
				this.game.currentGroupid = groupId;
				if (!this.game.ChatHistoryByPlayerGroup.ContainsKey(this.game.currentGroupid))
				{
					this.game.ChatHistoryByPlayerGroup[this.game.currentGroupid] = new LimitedList<string>(HudDialogChat.historyMax);
				}
				this.UpdateText();
				GuiTab[] tabs = this.Composers["chat"].GetHorizontalTabs("tabs").tabs;
				for (int i = 0; i < tabs.Length; i++)
				{
					if (tabs[i].DataInt == this.game.currentGroupid)
					{
						this.Composers["chat"].GetHorizontalTabs("tabs").activeElement = i;
						return;
					}
				}
			}
		}

		private void HandlePlayerGroupsPacket(Packet_Server packet)
		{
			this.game.OwnPlayerGroupsById.Clear();
			this.game.OwnPlayerGroupsById[GlobalConstants.GeneralChatGroup] = new PlayerGroup
			{
				Name = Lang.Get("chattab-general", Array.Empty<object>())
			};
			this.game.OwnPlayerGroupsById[GlobalConstants.DamageLogChatGroup] = new PlayerGroup
			{
				Name = Lang.Get("chattab-damagelog", Array.Empty<object>())
			};
			this.game.OwnPlayerGroupsById[GlobalConstants.InfoLogChatGroup] = new PlayerGroup
			{
				Name = Lang.Get("chattab-infolog", Array.Empty<object>())
			};
			this.game.OwnPlayerGroupMemembershipsById.Clear();
			IClientPlayer player = this.game.Player;
			if (((player != null) ? player.Privileges : null) != null && this.game.player.Privileges.Contains("controlserver") && !this.game.IsSingleplayer)
			{
				this.game.OwnPlayerGroupsById[GlobalConstants.ServerInfoChatGroup] = new PlayerGroup
				{
					Name = Lang.Get("chattab-serverinfo", Array.Empty<object>())
				};
			}
			for (int i = 0; i < packet.PlayerGroups.GroupsCount; i++)
			{
				PlayerGroup plrGroup = this.PlayerGroupFromPacket(packet.PlayerGroups.Groups[i]);
				this.game.OwnPlayerGroupsById[plrGroup.Uid] = plrGroup;
				this.game.OwnPlayerGroupMemembershipsById[plrGroup.Uid] = new PlayerGroupMembership
				{
					GroupName = plrGroup.Name,
					GroupUid = plrGroup.Uid,
					Level = (EnumPlayerGroupMemberShip)packet.PlayerGroups.Groups[i].Membership
				};
			}
			List<int> deletedGroups = new List<int>();
			foreach (int groupId in this.game.ChatHistoryByPlayerGroup.Keys)
			{
				if (!this.game.OwnPlayerGroupsById.ContainsKey(groupId))
				{
					deletedGroups.Add(groupId);
				}
			}
			foreach (int groupId2 in deletedGroups)
			{
				this.game.ChatHistoryByPlayerGroup.Remove(groupId2);
			}
			if (!this.game.OwnPlayerGroupsById.ContainsKey(this.game.currentGroupid))
			{
				this.game.currentGroupid = GlobalConstants.GeneralChatGroup;
			}
			this.ComposeChatGuis();
		}

		private void HandlePlayerGroupPacket(Packet_Server packet)
		{
			PlayerGroup plrGroup = this.PlayerGroupFromPacket(packet.PlayerGroup);
			this.game.OwnPlayerGroupsById[plrGroup.Uid] = plrGroup;
			this.game.OwnPlayerGroupMemembershipsById[plrGroup.Uid] = new PlayerGroupMembership
			{
				GroupName = plrGroup.Name,
				GroupUid = plrGroup.Uid,
				Level = (EnumPlayerGroupMemberShip)packet.PlayerGroup.Membership
			};
			this.ComposeChatGuis();
		}

		private PlayerGroup PlayerGroupFromPacket(Packet_PlayerGroup packet)
		{
			PlayerGroup plrGroup = new PlayerGroup
			{
				Name = packet.Name,
				OwnerUID = packet.Owneruid,
				Uid = packet.Uid
			};
			for (int i = 0; i < packet.ChathistoryCount; i++)
			{
				plrGroup.ChatHistory.Add(new ChatLine
				{
					ChatType = (EnumChatType)packet.Chathistory[i].ChatType,
					Message = packet.Chathistory[i].Message
				});
			}
			if (plrGroup.ChatHistory.Count > HudDialogChat.historyMax)
			{
				plrGroup.ChatHistory.RemoveAt(0);
			}
			return plrGroup;
		}

		public override EnumDialogType DialogType
		{
			get
			{
				if (this.IsOpened() && this.focused)
				{
					return EnumDialogType.Dialog;
				}
				return EnumDialogType.HUD;
			}
		}

		private static int historyMax = 30;

		private LimitedList<string> typedMessagesHistory = new LimitedList<string>(100);

		private int typedMessagesHistoryPos = -1;

		private long lastActivityMs = -100000L;

		private int lastMessageInGroupId = -99999;

		private TreeAttribute eventAttr;

		internal bool MultiCommandPasteMode;

		private ClientMain game;

		private int chatWindowInnerWidth = ClientSettings.ChatWindowWidth;

		private int chatWindowInnerHeight = ClientSettings.ChatWindowHeight;

		private int tabsHeight = 23;

		private int chatInputHeight = 25;

		private int horPadding = 6;

		private int verPadding = 3;

		private int bottomOffset = 100;

		private int scrollbarPadding = 1;

		private int scrollbarWidth = 10;

		private GuiTab[] tabs;

		private double lastAlpha = 1.0;

		private bool isLinkChatTyped;
	}
}
