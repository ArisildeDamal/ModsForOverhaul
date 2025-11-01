using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client
{
	internal class SystemClientCommands : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "ccom";
			}
		}

		public SystemClientCommands(ClientMain game)
			: base(game)
		{
			game.api.RegisterLinkProtocol("command", new Action<LinkTextComponent>(this.onCommandLinkClicked));
			ICoreClientAPI api = game.api;
			CommandArgumentParsers parsers = api.ChatCommands.Parsers;
			api.ChatCommands.GetOrCreate("help").RequiresPrivilege(Privilege.chat).WithArgs(new ICommandArgumentParser[]
			{
				parsers.OptionalWord("commandname"),
				parsers.OptionalWord("subcommand"),
				parsers.OptionalWord("subsubcommand")
			})
				.WithDescription("Display list of available server commands")
				.HandleWith(new OnCommandDelegate(this.handleHelp));
			api.ChatCommands.GetOrCreate("dev").BeginSubCommand("reload").WithRootAlias("reload")
				.WithDescription("Asseted reloading utility. Incase of shape reload will also Re-tesselate. Incase of textures will regenerate the texture atlasses.")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("assetcategory") })
				.HandleWith(new OnCommandDelegate(this.OnCmdReload))
				.EndSubCommand();
			api.ChatCommands.Create("clients").WithAlias(new string[] { "online" }).WithDescription("List of connected players")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWord("ping") })
				.HandleWith(new OnCommandDelegate(this.OnCmdListClients));
			api.ChatCommands.Create("freemove").WithDescription("Toggle Freemove").WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("freeMove", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdFreeMove));
			api.ChatCommands.Create("gui").WithDescription("Hide/Show all GUIs").WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("show_gui", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdToggleGUI));
			api.ChatCommands.Create("movespeed").WithDescription("Set Movespeed").WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("speed", 1f) })
				.HandleWith(new OnCommandDelegate(this.OnCmdMoveSpeed));
			api.ChatCommands.Create("noclip").WithDescription("Toggle noclip").WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("noclip", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdNoClip));
			api.ChatCommands.Create("viewdistance").WithDescription("Set view distance").WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("viewdistance", 0) })
				.HandleWith(new OnCommandDelegate(this.OnCmdViewDistance));
			api.ChatCommands.Create("lockfly").WithDescription("Locks a movement axis during flying/swimming").WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("axis", 0) })
				.HandleWith(new OnCommandDelegate(this.OnCmdLockFly));
			api.ChatCommands.Create("resolution").WithDescription("Sets the screen size to given width and height. Can be either [width] [height] or [360p|480p|720p|1080p|2160p]").WithArgs(new ICommandArgumentParser[]
			{
				parsers.OptionalWord("width"),
				parsers.OptionalWord("height")
			})
				.HandleWith(new OnCommandDelegate(this.OnCmdResolution));
			api.ChatCommands.Create("clientconfig").WithAlias(new string[] { "cf" }).WithDescription("Set/Gets a client setting")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("name") })
				.IgnoreAdditionalArgs()
				.HandleWith(new OnCommandDelegate(this.OnCmdSetting));
			api.ChatCommands.Create("clientconfigcreate").WithDescription("Create a new client setting that does not exist").WithArgs(new ICommandArgumentParser[]
			{
				parsers.Word("name"),
				parsers.Word("datatype")
			})
				.IgnoreAdditionalArgs()
				.HandleWith(new OnCommandDelegate(this.OnCmdSettingCreate));
			api.ChatCommands.Create("cp").WithDescription("Copy something to your clipboard").BeginSubCommand("posi")
				.WithDescription("Copy position as integer")
				.HandleWith(new OnCommandDelegate(this.OnCmdCpPosi))
				.EndSubCommand()
				.BeginSubCommand("aposi")
				.WithDescription("Copy position as absolute integer")
				.HandleWith(new OnCommandDelegate(this.OnCmdCpAposi))
				.EndSubCommand()
				.BeginSubCommand("apos")
				.WithDescription("Copy position as absolute floating point number")
				.HandleWith(new OnCommandDelegate(this.OnCmdCpApos))
				.EndSubCommand()
				.BeginSubCommand("chat")
				.WithDescription("Copy the chat history")
				.HandleWith(new OnCommandDelegate(this.OnCmdCpChat))
				.EndSubCommand();
			api.ChatCommands.Create("reconnect").WithDescription("Reconnect to server").HandleWith(new OnCommandDelegate(this.OnCmdReconnect));
			api.ChatCommands.Create("recordingmode").WithDescription("Makes the game brighter for recording (Sets gamma level to 1.1 and brightness level to 1.5)").HandleWith(new OnCommandDelegate(this.OnCmdRecordingMode));
			api.ChatCommands.Create("blockitempngexport").WithDescription("Export all items and blocks as png images").WithArgs(new ICommandArgumentParser[]
			{
				parsers.OptionalWordRange("exportRequest", new string[] { "inv", "all" }),
				parsers.OptionalInt("size", 100),
				parsers.OptionalWord("exportDomain")
			})
				.HandleWith(new OnCommandDelegate(this.OnCmdBlockItemPngExport));
			api.ChatCommands.Create("exponepng").BeginSubCommand("code").WithDescription("Export one items as png image")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.WordRange("exportType", new string[] { "block", "item" }),
					parsers.Word("exportCode"),
					parsers.OptionalInt("size", 100)
				})
				.HandleWith(new OnCommandDelegate(this.OnCmdOnePngExportCode))
				.EndSubCommand()
				.BeginSubCommand("hand")
				.WithDescription("Export icon for currently held item/block")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("size", 100) })
				.HandleWith(new OnCommandDelegate(this.OnCmdOnePngExportHand))
				.EndSubCommand();
			api.ChatCommands.Create("gencraftjson").WithDescription("Copies a snippet of json from your currently held item usable as a crafting recipe ingredient").HandleWith(new OnCommandDelegate(this.OnCmdGenCraftJson));
			api.ChatCommands.Create("zfar").WithDescription("Sets the zfar clipping plane. Useful when up the limit of 1km view distance.").WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("zfar", 0f) })
				.HandleWith(new OnCommandDelegate(this.OnCmdZfar));
			api.ChatCommands.Create("crash").WithDescription("Crashes the game.").HandleWith(new OnCommandDelegate(this.OnCmdCrash));
			api.ChatCommands.Create("timelapse").WithDescription("Start a sequence of timelapse photography, with specified interval (days) and duration (months)").WithArgs(new ICommandArgumentParser[]
			{
				parsers.Float("interval"),
				parsers.Float("duration")
			})
				.IgnoreAdditionalArgs()
				.HandleWith(new OnCommandDelegate(this.OnCmdTimelapse));
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderBlockItemPngs), EnumRenderStage.Ortho, "renderblockitempngs", 0.5);
		}

		private TextCommandResult OnCmdCpPosi(TextCommandCallingArgs args)
		{
			if (this.game.World.Config.GetBool("allowCoordinateHud", true))
			{
				BlockPos pos = this.game.EntityPlayer.Pos.AsBlockPos;
				pos.Sub(this.game.SpawnPosition.AsBlockPos.X, 0, this.game.SpawnPosition.AsBlockPos.Z);
				IXPlatformInterface xplatInterface = this.game.Platform.XPlatInterface;
				BlockPos blockPos = pos;
				xplatInterface.SetClipboardText(((blockPos != null) ? blockPos.ToString() : null) ?? "");
				return TextCommandResult.Success("Position as integer copied to clipboard", null);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdCpAposi(TextCommandCallingArgs args)
		{
			if (this.game.World.Config.GetBool("allowCoordinateHud", true))
			{
				IXPlatformInterface xplatInterface = this.game.Platform.XPlatInterface;
				Vec3i xyzint = this.game.EntityPlayer.Pos.XYZInt;
				xplatInterface.SetClipboardText(((xyzint != null) ? xyzint.ToString() : null) ?? "");
				return TextCommandResult.Success("Absolute Position as integer copied to clipboard", null);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdCpApos(TextCommandCallingArgs args)
		{
			if (this.game.World.Config.GetBool("allowCoordinateHud", true))
			{
				IXPlatformInterface xplatInterface = this.game.Platform.XPlatInterface;
				Vec3d xyz = this.game.EntityPlayer.Pos.XYZ;
				xplatInterface.SetClipboardText(((xyz != null) ? xyz.ToString() : null) ?? "");
				return TextCommandResult.Success("Absolute Position copied to clipboard", null);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdCpChat(TextCommandCallingArgs args)
		{
			StringBuilder b = new StringBuilder();
			foreach (object obj in this.game.ChatHistoryByPlayerGroup[this.game.currentGroupid])
			{
				string line = (string)obj;
				b.AppendLine(line);
			}
			this.game.Platform.XPlatInterface.SetClipboardText(b.ToString());
			return TextCommandResult.Success("Current chat history copied to clipboard", null);
		}

		private TextCommandResult handleHelp(TextCommandCallingArgs args)
		{
			StringBuilder text = new StringBuilder();
			Dictionary<string, IChatCommand> commands = IChatCommandApi.GetOrdered(this.game.api.chatcommandapi.AllSubcommands());
			Caller caller = args.Caller;
			if (caller.CallerPrivileges == null)
			{
				caller.CallerPrivileges = new string[] { "*" };
			}
			if (args.Parsers[0].IsMissing)
			{
				text.AppendLine("Available commands:");
				ChatCommandImpl.WriteCommandsList(text, commands, args.Caller, false);
				text.Append("\n" + Lang.Get("Type /help [commandname] to see more info about a command", Array.Empty<object>()));
				return TextCommandResult.Success(text.ToString(), null);
			}
			string arg = (string)args[0];
			if (!args.Parsers[1].IsMissing)
			{
				bool found = false;
				foreach (KeyValuePair<string, IChatCommand> entry in commands)
				{
					if (entry.Key == arg)
					{
						commands = IChatCommandApi.GetOrdered(entry.Value.AllSubcommands);
						found = true;
						break;
					}
				}
				if (!found)
				{
					return TextCommandResult.Error(string.Concat(new string[]
					{
						Lang.Get("No such sub-command found", Array.Empty<object>()),
						": ",
						arg,
						" ",
						(string)args[1]
					}), "");
				}
				arg = (string)args[1];
				if (!args.Parsers[2].IsMissing)
				{
					found = false;
					foreach (KeyValuePair<string, IChatCommand> entry2 in commands)
					{
						if (entry2.Key == arg)
						{
							commands = IChatCommandApi.GetOrdered(entry2.Value.AllSubcommands);
							found = true;
							break;
						}
					}
					if (!found)
					{
						return TextCommandResult.Error(string.Concat(new string[]
						{
							Lang.Get("No such sub-command found", Array.Empty<object>()),
							": ",
							(string)args[0],
							arg,
							" ",
							(string)args[2]
						}), "");
					}
					arg = (string)args[2];
				}
			}
			foreach (KeyValuePair<string, IChatCommand> entry3 in commands)
			{
				if (entry3.Key == arg)
				{
					IChatCommand cm = entry3.Value;
					if (cm.IsAvailableTo(args.Caller))
					{
						return TextCommandResult.Success(cm.GetFullSyntaxConsole(args.Caller), null);
					}
					return TextCommandResult.Error("Insufficient privilege to use this command", "");
				}
			}
			return TextCommandResult.Error(Lang.Get("No such command found", Array.Empty<object>()) + ": " + arg, "");
		}

		private void onCommandLinkClicked(LinkTextComponent linkComp)
		{
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, linkComp.Href.Substring("command://".Length), EnumChatType.Macro, null);
		}

		private TextCommandResult OnCmdCrash(TextCommandCallingArgs textCommandCallingArgs)
		{
			throw new Exception("Crash on request");
		}

		private TextCommandResult OnCmdRecordingMode(TextCommandCallingArgs textCommandCallingArgs)
		{
			if (ClientSettings.BrightnessLevel == 1f)
			{
				ClientSettings.BrightnessLevel = 1.2f;
				ClientSettings.ExtraGammaLevel = 1.3f;
				this.game.ShowChatMessage("Recording bright mode now on");
			}
			else
			{
				ClientSettings.BrightnessLevel = 1f;
				ClientSettings.ExtraGammaLevel = 1f;
				this.game.ShowChatMessage("Recording bright mode now off");
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdTimelapse(TextCommandCallingArgs args)
		{
			float interval = (float)args[0];
			float duration = (float)args[1];
			this.game.timelapse = interval;
			this.game.timelapseEnd = duration * (float)this.game.Calendar.DaysPerMonth;
			this.game.ShouldRender2DOverlays = false;
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdGenCraftJson(TextCommandCallingArgs args)
		{
			ItemSlot slot = this.game.player.inventoryMgr.ActiveHotbarSlot;
			if (slot.Itemstack == null)
			{
				return TextCommandResult.Success("Require something held in your hands", null);
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			StringBuilder stringBuilder = sb;
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(18, 2, stringBuilder);
			appendInterpolatedStringHandler.AppendLiteral("type: \"");
			appendInterpolatedStringHandler.AppendFormatted<EnumItemClass>(slot.Itemstack.Class);
			appendInterpolatedStringHandler.AppendLiteral("\", code: \"");
			appendInterpolatedStringHandler.AppendFormatted<AssetLocation>(slot.Itemstack.Collectible.Code);
			appendInterpolatedStringHandler.AppendLiteral("\"");
			stringBuilder2.Append(ref appendInterpolatedStringHandler);
			TreeAttribute attrs = slot.Itemstack.Attributes.Clone() as TreeAttribute;
			for (int i = 0; i < GlobalConstants.IgnoredStackAttributes.Length; i++)
			{
				attrs.RemoveAttribute(GlobalConstants.IgnoredStackAttributes[i]);
			}
			string attrjson = attrs.ToJsonToken();
			if (attrjson.Length > 0 && attrs.Count > 0)
			{
				stringBuilder = sb;
				StringBuilder stringBuilder3 = stringBuilder;
				appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder);
				appendInterpolatedStringHandler.AppendLiteral(", attributes: ");
				appendInterpolatedStringHandler.AppendFormatted(attrjson);
				stringBuilder3.Append(ref appendInterpolatedStringHandler);
			}
			sb.Append(" }");
			this.game.Platform.XPlatInterface.SetClipboardText(sb.ToString());
			return TextCommandResult.Success("Ok, copied to your clipboard", null);
		}

		private void OnRenderBlockItemPngs(float dt)
		{
			if (this.exportRequest == SystemClientCommands.EnumPngsExportRequest.None)
			{
				return;
			}
			bool all = this.exportRequest == SystemClientCommands.EnumPngsExportRequest.All;
			FrameBufferRef fb = this.game.Platform.CreateFramebuffer(new FramebufferAttrs("PngExport", this.size, this.size)
			{
				Attachments = new FramebufferAttrsAttachment[]
				{
					new FramebufferAttrsAttachment
					{
						AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
						Texture = new RawTexture
						{
							Width = this.size,
							Height = this.size,
							PixelFormat = EnumTexturePixelFormat.Rgba,
							PixelInternalFormat = EnumTextureInternalFormat.Rgba8
						}
					},
					new FramebufferAttrsAttachment
					{
						AttachmentType = EnumFramebufferAttachment.DepthAttachment,
						Texture = new RawTexture
						{
							Width = this.size,
							Height = this.size,
							PixelFormat = EnumTexturePixelFormat.DepthComponent,
							PixelInternalFormat = EnumTextureInternalFormat.DepthComponent32
						}
					}
				}
			});
			this.game.Platform.LoadFrameBuffer(fb);
			this.game.Platform.GlEnableDepthTest();
			this.game.Platform.GlDisableCullFace();
			this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			this.game.OrthoMode(this.size, this.size, false);
			float[] clearCol = new float[4];
			GamePaths.EnsurePathExists("icons/block");
			GamePaths.EnsurePathExists("icons/item");
			if (this.exportRequest == SystemClientCommands.EnumPngsExportRequest.One || this.exportRequest == SystemClientCommands.EnumPngsExportRequest.Hand)
			{
				this.game.Platform.ClearFrameBuffer(fb, clearCol, true, true);
				ItemStack stack;
				if (this.exportRequest == SystemClientCommands.EnumPngsExportRequest.One)
				{
					if (this.exportType == EnumItemClass.Item)
					{
						Item item = this.game.GetItem(new AssetLocation(this.exportCode));
						if (item == null)
						{
							this.game.ShowChatMessage("Not an item " + this.exportCode);
							this.exportRequest = SystemClientCommands.EnumPngsExportRequest.None;
							return;
						}
						stack = new ItemStack(item, 1);
					}
					else
					{
						Block block = this.game.GetBlock(new AssetLocation(this.exportCode));
						if (block == null)
						{
							this.game.ShowChatMessage("Not a block " + this.exportCode);
							this.exportRequest = SystemClientCommands.EnumPngsExportRequest.None;
							return;
						}
						stack = new ItemStack(block, 1);
					}
				}
				else
				{
					stack = this.game.player.inventoryMgr.ActiveHotbarSlot.Itemstack;
					if (stack == null)
					{
						this.game.ShowChatMessage("Nothing in hands");
						this.exportRequest = SystemClientCommands.EnumPngsExportRequest.None;
						return;
					}
				}
				this.game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(stack), (double)(this.size / 2), (double)(this.size / 2), 500.0, (float)(this.size / 2), -1, true, false, false);
				this.game.Platform.GrabScreenshot(this.size, this.size, false, true, true).Save(string.Concat(new string[]
				{
					"icons/",
					this.exportType.Name(),
					"/",
					stack.Collectible.Code.Path,
					".png"
				}));
			}
			else
			{
				for (int i = 0; i < this.game.Blocks.Count; i++)
				{
					this.game.Platform.ClearFrameBuffer(fb, clearCol, true, true);
					Block block2 = this.game.Blocks[i];
					if (!(((block2 != null) ? block2.Code : null) == null) && (all || (block2.CreativeInventoryTabs != null && block2.CreativeInventoryTabs.Length != 0)) && (this.exportDomain == null || !(block2.Code.Domain != this.exportDomain)))
					{
						this.game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(new ItemStack(block2, 1)), (double)(this.size / 2), (double)(this.size / 2), 500.0, (float)(this.size / 2), -1, true, false, false);
						this.game.Platform.GrabScreenshot(this.size, this.size, false, true, true).Save("icons/block/" + block2.Code.Path + ".png");
					}
				}
				for (int j = 0; j < this.game.Items.Count; j++)
				{
					this.game.Platform.ClearFrameBuffer(fb, clearCol, true, true);
					Item item2 = this.game.Items[j];
					if (!(((item2 != null) ? item2.Code : null) == null) && (all || (item2.CreativeInventoryTabs != null && item2.CreativeInventoryTabs.Length != 0)) && (this.exportDomain == null || !(item2.Code.Domain != this.exportDomain)))
					{
						this.game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(new ItemStack(item2, 1)), (double)(this.size / 2), (double)(this.size / 2), 500.0, (float)(this.size / 2), -1, true, false, false);
						BitmapRef bitmapRef = this.game.Platform.GrabScreenshot(this.size, this.size, false, true, true);
						string name = item2.Code.Path;
						if (name.Contains("/"))
						{
							name = name.Replace("/", "-");
						}
						bitmapRef.Save("icons/item/" + name + ".png");
					}
				}
			}
			this.exportRequest = SystemClientCommands.EnumPngsExportRequest.None;
			this.game.OrthoMode(this.game.Width, this.game.Height, false);
			this.game.Platform.UnloadFrameBuffer(fb);
			this.game.Platform.DisposeFrameBuffer(fb, true);
			this.game.ShowChatMessage("Ok, exported to " + Path.GetFullPath("icons/"));
		}

		private void cCopy(int groupId, CmdArgs args)
		{
			string text = args.PopWord(null);
			if (!(text == "posi"))
			{
				if (!(text == "apos"))
				{
					if (!(text == "aposi"))
					{
						if (!(text == "chat"))
						{
							return;
						}
						StringBuilder b = new StringBuilder();
						foreach (object obj in this.game.ChatHistoryByPlayerGroup[this.game.currentGroupid])
						{
							string line = (string)obj;
							b.AppendLine(line);
						}
						this.game.Platform.XPlatInterface.SetClipboardText(b.ToString());
						this.game.ShowChatMessage("Current chat history copied to clipboard");
					}
					else if (this.game.World.Config.GetBool("allowCoordinateHud", true))
					{
						IXPlatformInterface xplatInterface = this.game.Platform.XPlatInterface;
						Vec3i xyzint = this.game.EntityPlayer.Pos.XYZInt;
						xplatInterface.SetClipboardText(((xyzint != null) ? xyzint.ToString() : null) ?? "");
						this.game.ShowChatMessage("Absolute Position as integer copied to clipboard");
						return;
					}
				}
				else if (this.game.World.Config.GetBool("allowCoordinateHud", true))
				{
					IXPlatformInterface xplatInterface2 = this.game.Platform.XPlatInterface;
					Vec3d xyz = this.game.EntityPlayer.Pos.XYZ;
					xplatInterface2.SetClipboardText(((xyz != null) ? xyz.ToString() : null) ?? "");
					this.game.ShowChatMessage("Absolute Position copied to clipboard");
					return;
				}
			}
			else if (this.game.World.Config.GetBool("allowCoordinateHud", true))
			{
				BlockPos pos = this.game.EntityPlayer.Pos.AsBlockPos;
				pos.Sub(this.game.SpawnPosition.AsBlockPos.X, 0, this.game.SpawnPosition.AsBlockPos.Z);
				IXPlatformInterface xplatInterface3 = this.game.Platform.XPlatInterface;
				BlockPos blockPos = pos;
				xplatInterface3.SetClipboardText(((blockPos != null) ? blockPos.ToString() : null) ?? "");
				this.game.ShowChatMessage("Position as integer copied to clipboard");
				return;
			}
		}

		private TextCommandResult OnCmdSetting(TextCommandCallingArgs targs)
		{
			CmdArgs args = targs.RawArgs;
			string name = targs[0] as string;
			if (name == "sedi")
			{
				name = "showentitydebuginfo";
			}
			if (args.Length == 0)
			{
				if (!ClientSettings.Inst.HasSetting(name))
				{
					return TextCommandResult.Success("No such setting '" + name + "' (you can create setttings using .clientconfigcreate)", null);
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(11, 2);
				defaultInterpolatedStringHandler.AppendFormatted(name);
				defaultInterpolatedStringHandler.AppendLiteral(" is set to ");
				defaultInterpolatedStringHandler.AppendFormatted<object>(ClientSettings.Inst.GetSetting(name));
				return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
			}
			else
			{
				Type type = ClientSettings.Inst.GetSettingType(name);
				if (type == null)
				{
					return TextCommandResult.Success("No such setting '" + name + "'", null);
				}
				if (type == typeof(string))
				{
					string value = args.PopWord(null);
					ClientSettings.Inst.String[name] = value;
					this.game.ShowChatMessage(name + " now set to " + value);
				}
				if (type == typeof(int))
				{
					int? intVal = args.PopInt(new int?(0));
					if (intVal != null)
					{
						ClientSettings.Inst.Int[name] = intVal.Value;
						ClientMain game = this.game;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
						defaultInterpolatedStringHandler.AppendFormatted(name);
						defaultInterpolatedStringHandler.AppendLiteral(" now set to ");
						defaultInterpolatedStringHandler.AppendFormatted<int?>(intVal);
						game.ShowChatMessage(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else
					{
						this.game.ShowChatMessage("Supplied value is not an integer");
					}
				}
				if (type == typeof(float))
				{
					float? floatVal = args.PopFloat(new float?(0f));
					if (floatVal != null)
					{
						ClientSettings.Inst.Float[name] = floatVal.Value;
						ClientMain game2 = this.game;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
						defaultInterpolatedStringHandler.AppendFormatted(name);
						defaultInterpolatedStringHandler.AppendLiteral(" now set to ");
						defaultInterpolatedStringHandler.AppendFormatted<float?>(floatVal);
						game2.ShowChatMessage(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else
					{
						this.game.ShowChatMessage("Supplied value is not an integer");
					}
				}
				if (type == typeof(bool))
				{
					bool boolVal;
					if (args.PeekWord(null) == "toggle")
					{
						boolVal = !ClientSettings.Inst.Bool[name];
					}
					else
					{
						boolVal = args.PopBool(new bool?(false), "on").GetValueOrDefault();
					}
					ClientSettings.Inst.Bool[name] = boolVal;
					ClientMain game3 = this.game;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
					defaultInterpolatedStringHandler.AppendFormatted(name);
					defaultInterpolatedStringHandler.AppendLiteral(" now set to ");
					defaultInterpolatedStringHandler.AppendFormatted<bool>(boolVal);
					game3.ShowChatMessage(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				return TextCommandResult.Success("", null);
			}
		}

		private TextCommandResult OnCmdSettingCreate(TextCommandCallingArgs targs)
		{
			CmdArgs args = targs.RawArgs;
			string name = targs[0] as string;
			if (ClientSettings.Inst.HasSetting(name))
			{
				return TextCommandResult.Success("Setting '" + name + "' already exists", null);
			}
			string type = targs[1] as string;
			if (type == "string")
			{
				string value = args.PopAll();
				ClientSettings.Inst.String[name] = value;
				return TextCommandResult.Success(name + " now set to " + value, null);
			}
			if (type == "int")
			{
				int? intVal = args.PopInt(new int?(0));
				if (intVal != null)
				{
					ClientSettings.Inst.Int[name] = intVal.Value;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
					defaultInterpolatedStringHandler.AppendFormatted(name);
					defaultInterpolatedStringHandler.AppendLiteral(" now set to ");
					defaultInterpolatedStringHandler.AppendFormatted<int?>(intVal);
					return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
				}
				return TextCommandResult.Success(string.Format("Supplied value is not an integer", Array.Empty<object>()), null);
			}
			else if (type == "float")
			{
				float? floatVal = args.PopFloat(new float?(0f));
				if (floatVal != null)
				{
					ClientSettings.Inst.Float[name] = floatVal.Value;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
					defaultInterpolatedStringHandler.AppendFormatted(name);
					defaultInterpolatedStringHandler.AppendLiteral(" now set to ");
					defaultInterpolatedStringHandler.AppendFormatted<float?>(floatVal);
					return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
				}
				return TextCommandResult.Success(string.Format("Supplied value is not an integer", Array.Empty<object>()), null);
			}
			else
			{
				if (type == "bool")
				{
					bool boolVal = args.PopBool(new bool?(false), "on").GetValueOrDefault();
					ClientSettings.Inst.Bool[name] = boolVal;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
					defaultInterpolatedStringHandler.AppendFormatted(name);
					defaultInterpolatedStringHandler.AppendLiteral(" now set to ");
					defaultInterpolatedStringHandler.AppendFormatted<bool>(boolVal);
					return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
				}
				return TextCommandResult.Success("Unknown datatype: " + type + ". Must be string, int, float or bool", null);
			}
		}

		private TextCommandResult OnCmdLockFly(TextCommandCallingArgs args)
		{
			EnumFreeMovAxisLock mode = EnumFreeMovAxisLock.None;
			if (!args.Parsers[0].IsMissing)
			{
				int val = (int)args[0];
				if (val <= 3)
				{
					mode = (EnumFreeMovAxisLock)val;
				}
			}
			this.game.player.worlddata.RequestMode(this.game, mode);
			return TextCommandResult.Success("Lock fly axis " + mode.ToString(), null);
		}

		private TextCommandResult OnCmdResolution(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success("Current resolution: " + this.game.Platform.WindowSize.Width.ToString() + "x" + this.game.Platform.WindowSize.Height.ToString(), null);
			}
			bool found = true;
			int width = 0;
			int height = 0;
			string text = args[0] as string;
			if (!(text == "360p"))
			{
				if (!(text == "480p"))
				{
					if (!(text == "720p"))
					{
						if (!(text == "1080p"))
						{
							if (!(text == "1440p"))
							{
								if (!(text == "2160p"))
								{
									found = false;
								}
								else
								{
									width = 3840;
									height = 2160;
								}
							}
							else
							{
								width = 2560;
								height = 1440;
							}
						}
						else
						{
							width = 1920;
							height = 1080;
						}
					}
					else
					{
						width = 1280;
						height = 720;
					}
				}
				else
				{
					width = 854;
					height = 480;
				}
			}
			else
			{
				width = 640;
				height = 360;
			}
			if (!found && args.Parsers[1].IsMissing)
			{
				return TextCommandResult.Success("Width or Height missing", null);
			}
			if (!found)
			{
				int.TryParse(args[0] as string, out width);
				int.TryParse(args[1] as string, out height);
			}
			if (width <= 0 || height <= 0)
			{
				return TextCommandResult.Success("Width or Height not a number or 0 or below 0", null);
			}
			this.game.Platform.SetWindowSize(width, height);
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Resolution ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(width);
			defaultInterpolatedStringHandler.AppendLiteral("x");
			defaultInterpolatedStringHandler.AppendFormatted<int>(height);
			defaultInterpolatedStringHandler.AppendLiteral(" set.");
			return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
		}

		private TextCommandResult OnCmdZfar(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success("Current Zfar: " + this.game.MainCamera.ZFar.ToString(), null);
			}
			TextCommandResult textCommandResult;
			try
			{
				this.game.MainCamera.ZFar = (float)args[0];
				this.game.Reset3DProjection();
				textCommandResult = TextCommandResult.Success("Zfar is now: " + this.game.MainCamera.ZFar.ToString(), null);
			}
			catch (Exception)
			{
				textCommandResult = TextCommandResult.Success("Failed parsing param", null);
			}
			return textCommandResult;
		}

		private TextCommandResult OnCmdReload(TextCommandCallingArgs args)
		{
			AssetCategory cat;
			AssetCategory.categories.TryGetValue(args[0] as string, out cat);
			if (cat == null)
			{
				return TextCommandResult.Success("No such asset category found", null);
			}
			int reloaded = this.game.Platform.AssetManager.Reload(cat);
			if (cat == AssetCategory.shaders)
			{
				bool ok = ShaderRegistry.ReloadShaders();
				bool ok2 = this.game.eventManager != null && this.game.eventManager.TriggerReloadShaders();
				ok = ok && ok2;
				return TextCommandResult.Success("Shaders reloaded" + (ok ? "" : ". errors occured, please check client log"), null);
			}
			if (cat == AssetCategory.shapes)
			{
				ClientEventManager eventManager = this.game.eventManager;
				if (eventManager != null)
				{
					eventManager.TriggerReloadShapes();
				}
				return TextCommandResult.Success(reloaded.ToString() + " assets reloaded and shapes re-tesselated", null);
			}
			if (cat == AssetCategory.textures)
			{
				this.game.ReloadTextures();
				return TextCommandResult.Success(reloaded.ToString() + " assets reloaded and atlasses re-generated", null);
			}
			if (cat == AssetCategory.sounds)
			{
				ScreenManager.LoadSoundsInitial();
			}
			if (cat == AssetCategory.lang)
			{
				Lang.Load(this.game.Logger, this.game.AssetManager, ClientSettings.Language);
				return TextCommandResult.Success("language files reloaded", null);
			}
			return TextCommandResult.Success(reloaded.ToString() + " assets reloaded", null);
		}

		private TextCommandResult OnCmdViewDistance(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success("Current view distance: " + ClientSettings.ViewDistance.ToString(), null);
			}
			ClientSettings.ViewDistance = (int)args[0];
			return TextCommandResult.Success("View distance set", null);
		}

		private TextCommandResult OnCmdListClients(TextCommandCallingArgs args)
		{
			bool withping = args[0] as string == "ping";
			StringBuilder sb = new StringBuilder();
			int cnt = 0;
			foreach (KeyValuePair<string, ClientPlayer> val in this.game.PlayersByUid)
			{
				string name = val.Value.PlayerName;
				if (name != null)
				{
					if (sb.Length > 0)
					{
						sb.Append(", ");
					}
					if (withping)
					{
						StringBuilder stringBuilder = sb;
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder.AppendInterpolatedStringHandler appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder);
						appendInterpolatedStringHandler.AppendFormatted(name);
						appendInterpolatedStringHandler.AppendLiteral(" (");
						appendInterpolatedStringHandler.AppendFormatted<int>((int)(val.Value.Ping * 1000f));
						appendInterpolatedStringHandler.AppendLiteral("ms)");
						stringBuilder2.Append(ref appendInterpolatedStringHandler);
					}
					else
					{
						StringBuilder stringBuilder = sb;
						StringBuilder stringBuilder3 = stringBuilder;
						StringBuilder.AppendInterpolatedStringHandler appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder);
						appendInterpolatedStringHandler.AppendFormatted(name);
						stringBuilder3.Append(ref appendInterpolatedStringHandler);
					}
					cnt++;
				}
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 2);
			defaultInterpolatedStringHandler.AppendFormatted<int>(cnt);
			defaultInterpolatedStringHandler.AppendLiteral(" Players: ");
			defaultInterpolatedStringHandler.AppendFormatted<StringBuilder>(sb);
			return TextCommandResult.Success(defaultInterpolatedStringHandler.ToStringAndClear(), null);
		}

		private TextCommandResult OnCmdReconnect(TextCommandCallingArgs textCommandCallingArgs)
		{
			this.game.exitReason = "reconnect command triggered";
			this.game.DoReconnect();
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdFreeMove(TextCommandCallingArgs args)
		{
			if (this.game.AllowFreemove)
			{
				this.game.player.worlddata.RequestModeFreeMove(this.game, (bool)args[0]);
				return TextCommandResult.Success("", null);
			}
			return TextCommandResult.Success(Lang.Get("Flymode not allowed", Array.Empty<object>()), null);
		}

		private TextCommandResult OnCmdToggleGUI(TextCommandCallingArgs args)
		{
			this.game.ShouldRender2DOverlays = (bool)args[0];
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdMoveSpeed(TextCommandCallingArgs args)
		{
			if (!this.game.AllowFreemove)
			{
				return TextCommandResult.Success(Lang.Get("Flymode not allowed", Array.Empty<object>()), null);
			}
			float speed = (float)args[0];
			if (speed > 500f)
			{
				return TextCommandResult.Success("Entered movespeed to high! max. 500x", null);
			}
			this.game.player.worlddata.SetMode(this.game, speed);
			return TextCommandResult.Success("Movespeed: " + speed.ToString() + "x", null);
		}

		private TextCommandResult OnCmdNoClip(TextCommandCallingArgs args)
		{
			this.game.player.worlddata.RequestModeNoClip(this.game, (bool)args[0]);
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdBlockItemPngExport(TextCommandCallingArgs args)
		{
			this.exportRequest = ((args[0] as string == "inv") ? SystemClientCommands.EnumPngsExportRequest.CreativeInventory : SystemClientCommands.EnumPngsExportRequest.All);
			this.size = (int)args[1];
			this.exportDomain = (args.Parsers[2].IsMissing ? null : (args[2] as string));
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdOnePngExportHand(TextCommandCallingArgs args)
		{
			this.exportRequest = SystemClientCommands.EnumPngsExportRequest.Hand;
			this.size = (int)args[0];
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdOnePngExportCode(TextCommandCallingArgs args)
		{
			this.exportRequest = SystemClientCommands.EnumPngsExportRequest.One;
			this.exportType = ((args[0] as string == "block") ? EnumItemClass.Block : EnumItemClass.Item);
			this.exportCode = args[1] as string;
			this.size = (int)args[2];
			return TextCommandResult.Success("", null);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private SystemClientCommands.EnumPngsExportRequest exportRequest;

		private string exportDomain;

		private int size;

		private EnumItemClass exportType;

		private string exportCode;

		private enum EnumPngsExportRequest
		{
			None,
			CreativeInventory,
			All,
			One,
			Hand
		}
	}
}
