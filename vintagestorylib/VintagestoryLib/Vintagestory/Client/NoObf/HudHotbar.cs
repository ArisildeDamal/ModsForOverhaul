using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class HudHotbar : HudElement
	{
		public override double InputOrder
		{
			get
			{
				return 1.0;
			}
		}

		public HudHotbar(ICoreClientAPI capi)
			: base(capi)
		{
			this.wiUtil = new DrawWorldInteractionUtil(capi, this.Composers, "-heldItem");
			this.wiUtil.UnscaledLineHeight = 25.0;
			this.wiUtil.FontSize = 16f;
			capi.Event.RegisterGameTickListener(new Action<float>(this.OnGameTick), 20, 0);
			capi.Event.AfterActiveSlotChanged += delegate(ActiveSlotChangeEventArgs ev)
			{
				this.OnActiveSlotChanged(ev.ToSlot, true);
			};
			capi.Event.RegisterGameTickListener(new Action<float>(this.OnCheckToolMode), 100, 0);
		}

		public override void OnBlockTexturesLoaded()
		{
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot1", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(0, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot2", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(1, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot3", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(2, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot4", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(3, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot5", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(4, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot6", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(5, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot7", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(6, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot8", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(7, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot9", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(8, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot10", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(9, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot11", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(10, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot12", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(11, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot13", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(12, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot14", delegate(KeyCombination viaKeyComb)
			{
				this.OnKeySlot(13, true);
				return true;
			}, true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("fliphandslots", new ActionConsumable<KeyCombination>(this.KeyFlipHandSlots), true);
			ClientSettings.Inst.AddKeyCombinationUpdatedWatcher(delegate(string key, KeyCombination value)
			{
				if (key.StartsWithOrdinal("hotbarslot"))
				{
					this.shouldRecompose = true;
				}
			});
			this.temporalStabilityEnabled = this.capi.World.Config.GetBool("temporalStability", true);
			if (this.temporalStabilityEnabled)
			{
				ClientSettings.Inst.AddWatcher<float>("guiScale", delegate(float s)
				{
					this.genGearTexture();
				});
				this.genGearTexture();
			}
			int size = (int)GuiElement.scaled(32.0);
			this.toolModeBgTexture = this.capi.Gui.Icons.GenTexture(size, size, delegate(Context ctx, ImageSurface surface)
			{
				ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
				GuiElement.RoundRectangle(ctx, GuiElement.scaled(2.0), GuiElement.scaled(2.0), GuiElement.scaled(28.0), GuiElement.scaled(28.0), 1.0);
				ctx.Fill();
			});
		}

		private void genGearTexture()
		{
			int size = (int)Math.Ceiling((double)(85f * ClientSettings.GUIScale));
			LoadedTexture loadedTexture = this.gearTexture;
			if (loadedTexture != null)
			{
				loadedTexture.Dispose();
			}
			this.gearTexture = this.capi.Gui.Icons.GenTexture(size + 10, size + 10, delegate(Context ctx, ImageSurface surface)
			{
				this.capi.Gui.Icons.DrawVSGear(ctx, surface, 5, 5, (float)size, (float)size, new double[] { 0.2, 0.2, 0.2, 1.0 });
			});
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		private void OnGameTick(float dt)
		{
			if (this.itemInfoTextActiveMs > 0L && this.capi.ElapsedMilliseconds - this.itemInfoTextActiveMs > 3500L)
			{
				this.itemInfoTextActiveMs = 0L;
				this.Composers["hotbar"].GetHoverText("iteminfoHover").SetVisible(false);
				this.wiUtil.ComposeBlockWorldInteractionHelp(Array.Empty<WorldInteraction>());
			}
		}

		private void OnCheckToolMode(float dt)
		{
			this.UpdateCurrentToolMode(this.capi.World.Player.InventoryManager.ActiveHotbarSlotNumber);
		}

		private void UpdateCurrentToolMode(int newSlotIndex)
		{
			ItemSlot slot = this.capi.World.Player.InventoryManager.GetHotbarInventory()[newSlotIndex];
			SkillItem[] array;
			if (slot == null)
			{
				array = null;
			}
			else
			{
				ItemStack itemstack = slot.Itemstack;
				if (itemstack == null)
				{
					array = null;
				}
				else
				{
					CollectibleObject collectible = itemstack.Collectible;
					array = ((collectible != null) ? collectible.GetToolModes(slot, this.capi.World.Player, this.capi.World.Player.CurrentBlockSelection) : null);
				}
			}
			SkillItem[] toolModes = array;
			bool flag = toolModes != null && toolModes.Length != 0;
			this.currentToolModeTexture = null;
			if (flag)
			{
				int curModeIndex = slot.Itemstack.Collectible.GetToolMode(slot, this.capi.World.Player, this.capi.World.Player.CurrentBlockSelection);
				if (curModeIndex >= 0 && curModeIndex < toolModes.Length)
				{
					this.currentToolModeTexture = toolModes[curModeIndex].Texture;
				}
			}
		}

		public override void OnOwnPlayerDataReceived()
		{
			this.capi.Gui.Icons.CustomIcons["left_hand"] = this.capi.Gui.Icons.SvgIconSource(new AssetLocation("textures/icons/character/left_hand.svg"));
			this.ComposeGuis();
		}

		public void ComposeGuis()
		{
			this.hotbarInv = this.capi.World.Player.InventoryManager.GetOwnInventory("hotbar");
			this.backpackInv = this.capi.World.Player.InventoryManager.GetOwnInventory("backpack");
			double elementToDialogPadding = GuiStyle.ElementToDialogPadding;
			if (this.hotbarInv != null)
			{
				this.hotbarInv.Open(this.capi.World.Player);
				this.backpackInv.Open(this.capi.World.Player);
				this.hotbarInv.SlotModified += this.OnHotbarSlotModified;
				float width = 850f;
				this.dialogBounds = new ElementBounds
				{
					Alignment = EnumDialogArea.CenterBottom,
					BothSizing = ElementSizing.Fixed,
					fixedWidth = (double)width,
					fixedHeight = 80.0
				}.WithFixedAlignmentOffset(0.0, 5.0);
				ElementBounds offhandBounds = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 10.0, 10.0, 10, 1);
				ElementBounds hotBarBounds = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 110.0, 10.0, 10, 1);
				ElementBounds backpackBounds = ElementStdBounds.SlotGrid(EnumDialogArea.RightFixed, 0.0, 10.0, 4, 1).WithFixedAlignmentOffset(-10.0, 0.0);
				ElementBounds iteminfoBounds = ElementBounds.Fixed(EnumDialogArea.CenterBottom, 0.0, 0.0, 400.0, 0.0).WithFixedAlignmentOffset(0.0, -150.0);
				ElementBounds gearBounds = ElementBounds.Fixed(EnumDialogArea.CenterBottom, 0.0, 0.0, 100.0, 80.0).WithFixedAlignmentOffset(0.0, -80.0);
				double[] color = (double[])GuiStyle.DialogDefaultTextColor.Clone();
				color[0] = (color[0] + 1.0) / 2.0;
				color[1] = (color[1] + 1.0) / 2.0;
				color[2] = (color[2] + 1.0) / 2.0;
				CairoFont hoverfont = CairoFont.WhiteSmallText().WithColor(color).WithStroke(GuiStyle.DarkBrownColor, 2.0)
					.WithOrientation(EnumTextOrientation.Center);
				this.Composers["hotbar"] = this.capi.Gui.CreateCompo("inventory-hotbar", this.dialogBounds.FlatCopy().FixedGrow(0.0, 20.0)).BeginChildElements(this.dialogBounds).AddShadedDialogBG(ElementBounds.Fill, false, 5.0, 0.75f)
					.AddStaticCustomDraw(ElementBounds.Fill, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
					{
						ctx.Rectangle(0.0, 0.0, bounds.OuterWidth, (double)(25f * ClientSettings.GUIScale));
						ctx.Operator = Operator.Clear;
						ctx.Fill();
						ctx.Operator = Operator.Over;
					})
					.AddItemSlotGrid(this.hotbarInv, new Action<object>(this.SendInvPacket), 1, new int[] { 11 }, offhandBounds, "offhandgrid")
					.AddItemSlotGrid(this.hotbarInv, new Action<object>(this.SendInvPacket), 10, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, hotBarBounds, "hotbargrid")
					.AddItemSlotGrid(this.backpackInv, new Action<object>(this.SendInvPacket), 4, new int[] { 0, 1, 2, 3 }, backpackBounds, "backpackgrid")
					.AddTranspHoverText("", hoverfont, 400, iteminfoBounds, "iteminfoHover")
					.AddIf(this.temporalStabilityEnabled)
					.AddHoverText(" ", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), 50, gearBounds, "tempStabHoverText")
					.EndIf()
					.EndChildElements();
				(this.Composers["hotbar"].GetElement("element-2") as GuiElementDialogBackground).FullBlur = true;
				this.Composers["hotbar"].Tabbable = false;
				this.hotbarSlotGrid = this.Composers["hotbar"].GetSlotGrid("hotbargrid");
				this.hotbarSlotGrid.KeyboardControlEnabled = false;
				this.Composers["hotbar"].GetSlotGrid("offhandgrid").KeyboardControlEnabled = false;
				this.Composers["hotbar"].GetSlotGrid("backpackgrid").KeyboardControlEnabled = false;
				CairoFont numberFont = CairoFont.WhiteDetailText();
				numberFont.Color = GuiStyle.HotbarNumberTextColor;
				this.hotbarSlotGrid.AlwaysRenderIcon = true;
				this.hotbarSlotGrid.DrawIconHandler = delegate(Context cr, string type, int x, int y, float w, float h, double[] rgba)
				{
					HudHotbar.drawIcon(cr, "hotbarslot" + type, x, y, w, numberFont);
				};
				this.Composers["hotbar"].Compose(true);
				this.backPackGrid = this.Composers["hotbar"].GetSlotGrid("backpackgrid");
				this.OnActiveSlotChanged(this.capi.World.Player.InventoryManager.ActiveHotbarSlotNumber, false);
				GuiElementHoverText hoverText = this.Composers["hotbar"].GetHoverText("iteminfoHover");
				hoverText.ZPosition = 50f;
				hoverText.SetFollowMouse(false);
				hoverText.SetAutoWidth(true);
				hoverText.SetAutoDisplay(false);
				hoverText.fillBounds = true;
				this.TryOpen();
				return;
			}
			ScreenManager.Platform.Logger.Notification("Server did not send a hotbar inventory, so I won't display one");
		}

		private static void drawIcon(Context cr, string type, int x, int y, float w, CairoFont numberFont)
		{
			CairoFont font = numberFont;
			font.SetupContext(cr);
			string text = ScreenManager.hotkeyManager.GetHotKeyByCode(type).CurrentMapping.ToString();
			double textwidth = numberFont.GetTextExtents(text).Width;
			if (textwidth > GuiElement.scaled(20.0))
			{
				font = font.Clone();
				font.UnscaledFontsize *= 0.6;
				font.SetupContext(cr);
				textwidth = font.GetTextExtents(text).Width;
			}
			cr.MoveTo((double)((float)x + w) - textwidth + 1.0, (double)y + font.GetFontExtents().Ascent - 3.0);
			cr.ShowText(text);
		}

		private void SendInvPacket(object packet)
		{
			this.capi.Network.SendPacketClient(packet);
		}

		public override bool TryClose()
		{
			return false;
		}

		public override bool ShouldReceiveKeyboardEvents()
		{
			return true;
		}

		public override void OnMouseWheel(MouseWheelEventArgs args)
		{
			args.SetHandled(true);
			if (this.scrollWheeledSlotInFrame)
			{
				return;
			}
			IPlayer plr = this.capi.World.Player;
			if (args.delta != 0 && args.value != this.prevValue)
			{
				int skoffset = ((!this.hotbarInv[10].Empty) ? 1 : 0);
				int max = ((plr.InventoryManager.ActiveHotbarSlotNumber >= 10 + skoffset || this.capi.Input.KeyboardKeyStateRaw[3]) ? 14 : 10);
				this.OnKeySlot(GameMath.Mod(plr.InventoryManager.ActiveHotbarSlotNumber - args.delta, max + skoffset), false);
				this.prevValue = args.value;
				this.scrollWheeledSlotInFrame = true;
			}
		}

		private bool KeyFlipHandSlots(KeyCombination t1)
		{
			IPlayer plr = this.capi.World.Player;
			Packet_Client packet = (Packet_Client)this.hotbarInv.TryFlipItems(plr.InventoryManager.ActiveHotbarSlotNumber, plr.Entity.LeftHandItemSlot);
			if (packet != null)
			{
				this.capi.Network.SendPacketClient(packet);
			}
			return true;
		}

		private void OnKeySlot(int index, bool moveItems)
		{
			IPlayer plr = this.capi.World.Player;
			ItemSlot activeHotbarSlot = plr.InventoryManager.ActiveHotbarSlot;
			if (((activeHotbarSlot != null) ? activeHotbarSlot.Itemstack : null) != null && plr.Entity.Controls.HandUse != EnumHandInteract.None && index != plr.InventoryManager.ActiveHotbarSlotNumber)
			{
				EnumHandInteract beforeUseType = plr.Entity.Controls.HandUse;
				if (!plr.Entity.TryStopHandAction(false, EnumItemUseCancelReason.ChangeSlot))
				{
					return;
				}
				this.capi.Network.SendHandInteraction(2, plr.CurrentBlockSelection, plr.CurrentEntitySelection, beforeUseType, 1, false, EnumItemUseCancelReason.ChangeSlot);
			}
			plr.InventoryManager.ActiveHotbarSlotNumber = index;
			if (moveItems && plr.InventoryManager.CurrentHoveredSlot != null)
			{
				Packet_Client packet = (Packet_Client)this.hotbarInv.TryFlipItems(index, plr.InventoryManager.CurrentHoveredSlot);
				if (packet != null)
				{
					this.capi.Network.SendPacketClient(packet);
				}
			}
		}

		private void updateHotbarSounds()
		{
			ItemStack nowLeftStack = this.capi.World.Player.Entity.LeftHandItemSlot.Itemstack;
			this.updateHotbarSounds(this.prevLeftStack, nowLeftStack, this.leftActiveSlotIdleSound);
			this.prevLeftStack = nowLeftStack;
			ItemStack nowRightStack = this.capi.World.Player.Entity.RightHandItemSlot.Itemstack;
			this.updateHotbarSounds(this.prevRightStack, nowRightStack, this.rightActiveSlotIdleSound);
			this.prevRightStack = nowRightStack;
		}

		private void updateHotbarSounds(ItemStack prevStack, ItemStack nowStack, Dictionary<AssetLocation, ILoadedSound> activeSlotIdleSound)
		{
			HudHotbar.<>c__DisplayClass42_0 CS$<>8__locals1 = new HudHotbar.<>c__DisplayClass42_0();
			CS$<>8__locals1.activeSlotIdleSound = activeSlotIdleSound;
			if (nowStack != null && prevStack != null && nowStack.Equals(this.capi.World, prevStack, GlobalConstants.IgnoredStackAttributes))
			{
				return;
			}
			HeldSounds heldSounds;
			if (nowStack == null)
			{
				heldSounds = null;
			}
			else
			{
				CollectibleObject collectible = nowStack.Collectible;
				heldSounds = ((collectible != null) ? collectible.HeldSounds : null);
			}
			HeldSounds nowSounds = heldSounds;
			HudHotbar.<>c__DisplayClass42_0 CS$<>8__locals2 = CS$<>8__locals1;
			HeldSounds heldSounds2;
			if (prevStack == null)
			{
				heldSounds2 = null;
			}
			else
			{
				CollectibleObject collectible2 = prevStack.Collectible;
				heldSounds2 = ((collectible2 != null) ? collectible2.HeldSounds : null);
			}
			CS$<>8__locals2.prevSounds = heldSounds2;
			if (CS$<>8__locals1.prevSounds != null)
			{
				if (CS$<>8__locals1.prevSounds.Unequip != null)
				{
					this.capi.World.PlaySoundAt(CS$<>8__locals1.prevSounds.Unequip, 0.0, 0.0, 0.0, null, 0.9f + (float)this.capi.World.Rand.NextDouble() * 0.2f, 32f, 1f);
				}
				ILoadedSound sound;
				if (CS$<>8__locals1.prevSounds.Idle != null && CS$<>8__locals1.prevSounds.Idle != ((nowSounds != null) ? nowSounds.Idle : null) && CS$<>8__locals1.activeSlotIdleSound.TryGetValue(CS$<>8__locals1.prevSounds.Idle, out sound))
				{
					sound.FadeOut(1f, delegate(ILoadedSound s)
					{
						s.Stop();
						s.Dispose();
						CS$<>8__locals1.activeSlotIdleSound.Remove(CS$<>8__locals1.prevSounds.Idle);
					});
				}
			}
			if (nowSounds != null)
			{
				if (nowSounds.Equip != null)
				{
					this.capi.World.PlaySoundAt(nowSounds.Equip, 0.0, 0.0, 0.0, null, 0.9f + (float)this.capi.World.Rand.NextDouble() * 0.2f, 32f, 1f);
				}
				if (nowSounds.Idle != null && !CS$<>8__locals1.activeSlotIdleSound.ContainsKey(nowSounds.Idle))
				{
					ILoadedSound sound2 = this.capi.World.LoadSound(new SoundParams
					{
						Location = nowSounds.Idle.Clone().WithPathAppendixOnce(".ogg").WithPathPrefixOnce("sounds/"),
						Pitch = 0.9f + (float)this.capi.World.Rand.NextDouble() * 0.2f,
						RelativePosition = true,
						ShouldLoop = true,
						SoundType = EnumSoundType.Sound,
						Volume = 1f
					});
					CS$<>8__locals1.activeSlotIdleSound[nowSounds.Idle] = sound2;
					sound2.Start();
					sound2.FadeIn(1f, delegate(ILoadedSound s)
					{
					});
				}
			}
		}

		private void OnActiveSlotChanged(int newSlot, bool triggerUpdate = true)
		{
			if (this.hotbarSlotGrid == null)
			{
				return;
			}
			IClientPlayer player = this.capi.World.Player;
			ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
			PlayerAnimationManager playerAnimationManager = player.Entity.AnimManager as PlayerAnimationManager;
			if (playerAnimationManager != null)
			{
				playerAnimationManager.OnActiveSlotChanged(slot);
			}
			int skoffset = ((!this.hotbarInv[10].Empty) ? 1 : 0);
			if (newSlot < 10 + skoffset)
			{
				this.hotbarSlotGrid.HighlightSlot(newSlot);
				this.backPackGrid.HighlightSlot(-1);
			}
			else
			{
				this.hotbarSlotGrid.HighlightSlot(-1);
				this.backPackGrid.HighlightSlot(newSlot - 10 - skoffset);
			}
			if (triggerUpdate)
			{
				(this.capi.World as ClientMain).EnqueueMainThreadTask(delegate
				{
					this.RecomposeActiveSlotHoverText(newSlot);
				}, "recomposeslothovertext");
				this.UpdateCurrentToolMode(newSlot);
			}
			this.updateHotbarSounds();
			InventoryPlayerHotbar inventoryPlayerHotbar = slot.Inventory as InventoryPlayerHotbar;
			if (inventoryPlayerHotbar == null)
			{
				return;
			}
			inventoryPlayerHotbar.updateSlotStatMods(slot);
		}

		private void OnHotbarSlotModified(int slotId)
		{
			this.UpdateCurrentToolMode(slotId);
			this.updateHotbarSounds();
			IPlayer plr = this.capi.World.Player;
			if (slotId == plr.InventoryManager.ActiveHotbarSlotNumber)
			{
				IItemStack stack = plr.InventoryManager.ActiveHotbarSlot.Itemstack;
				if (stack == null)
				{
					return;
				}
				if (stack.StackSize == this.lastActiveSlotStacksize && stack.Id == this.lastActiveSlotItemId && stack.Class == this.lastActiveClassItemClass)
				{
					return;
				}
				(this.capi.World as ClientMain).EnqueueMainThreadTask(delegate
				{
					this.RecomposeActiveSlotHoverText(plr.InventoryManager.ActiveHotbarSlotNumber);
				}, "recomposeslothovertext");
				this.lastActiveClassItemClass = stack.Class;
				this.lastActiveSlotItemId = stack.Id;
				this.lastActiveSlotStacksize = stack.StackSize;
			}
		}

		private void RecomposeActiveSlotHoverText(int newSlotIndex)
		{
			IPlayer plr = this.capi.World.Player;
			int skoffset = ((!this.hotbarInv[10].Empty) ? 1 : 0);
			ItemSlot activeSlot;
			if (newSlotIndex >= 10 + skoffset)
			{
				activeSlot = this.backpackInv[newSlotIndex - 10 - skoffset];
			}
			else
			{
				ItemSlot itemSlot;
				if (plr == null)
				{
					itemSlot = null;
				}
				else
				{
					IPlayerInventoryManager inventoryManager = plr.InventoryManager;
					itemSlot = ((inventoryManager != null) ? inventoryManager.GetHotbarInventory()[newSlotIndex] : null);
				}
				activeSlot = itemSlot;
			}
			if (((activeSlot != null) ? activeSlot.Itemstack : null) != null)
			{
				if (this.prevIndex == newSlotIndex)
				{
					return;
				}
				this.prevIndex = newSlotIndex;
				this.itemInfoTextActiveMs = this.capi.ElapsedMilliseconds;
				GuiComposer guiComposer = this.Composers["hotbar"];
				GuiElementHoverText elem = ((guiComposer != null) ? guiComposer.GetHoverText("iteminfoHover") : null);
				if (elem != null)
				{
					elem.SetNewText(activeSlot.Itemstack.GetName());
					elem.SetVisible(true);
				}
				if (ClientSettings.ShowBlockInteractionHelp)
				{
					WorldInteraction[] wis = activeSlot.Itemstack.Collectible.GetHeldInteractionHelp(activeSlot);
					this.wiUtil.ComposeBlockWorldInteractionHelp(wis);
					return;
				}
			}
			else
			{
				this.Composers["hotbar"].GetHoverText("iteminfoHover").SetVisible(false);
				this.wiUtil.ComposeBlockWorldInteractionHelp(Array.Empty<WorldInteraction>());
				this.prevIndex = newSlotIndex;
			}
		}

		public override void OnRenderGUI(float deltaTime)
		{
			if (this.shouldRecompose)
			{
				this.ComposeGuis();
				this.shouldRecompose = false;
			}
			if (this.temporalStabilityEnabled && this.capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
			{
				this.renderGear(deltaTime);
			}
			float screenWidth = (float)this.capi.Render.FrameWidth;
			float screenHeight = (float)this.capi.Render.FrameHeight;
			GuiComposer composer = this.wiUtil.Composer;
			ElementBounds bounds = ((composer != null) ? composer.Bounds : null);
			if (bounds != null)
			{
				bounds.Alignment = EnumDialogArea.None;
				bounds.fixedOffsetX = 0.0;
				bounds.fixedOffsetY = 0.0;
				bounds.absFixedX = (double)(screenWidth / 2f) - this.wiUtil.ActualWidth / 2.0;
				bounds.absFixedY = (double)screenHeight - GuiElement.scaled(95.0) - bounds.OuterHeight;
				bounds.absMarginX = 0.0;
				bounds.absMarginY = 0.0;
			}
			if (this.capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
			{
				base.OnRenderGUI(deltaTime);
			}
			if (this.currentToolModeTexture != null)
			{
				float size = (float)GuiElement.scaled(38.0);
				float x = (float)GuiElement.scaled(200.0);
				float y = (float)GuiElement.scaled(8.0);
				this.capi.Render.Render2DTexture(this.toolModeBgTexture.TextureId, (float)this.capi.Render.FrameWidth / 2f + x, y, size, size, 50f, null);
				this.capi.Render.Render2DTexture(this.currentToolModeTexture.TextureId, (float)this.capi.Render.FrameWidth / 2f + x, y, size, size, 50f, null);
			}
			if (!this.hotbarInv[10].Empty)
			{
				float num = (float)((double)screenHeight - this.dialogBounds.OuterHeight + GuiElement.scaled(15.0));
				float posX = screenWidth / 2f - (float)GuiElement.scaled(361.0);
				float posY = num + (float)GuiElement.scaled(10.0);
				ISkillItemRenderer skillItemRenderer = this.hotbarInv[10].Itemstack.Collectible as ISkillItemRenderer;
				if (skillItemRenderer != null)
				{
					skillItemRenderer.Render(deltaTime, posX, posY, 200f);
				}
				if (this.capi.World.Player.InventoryManager.ActiveHotbarSlotNumber == 10)
				{
					ElementBounds sbounds = this.hotbarSlotGrid.SlotBounds[0];
					this.capi.Render.Render2DTexturePremultipliedAlpha(this.hotbarSlotGrid.HighlightSlotTexture.TextureId, (float)((int)(sbounds.renderX - 2.0 - (double)sbounds.OuterWidthInt - 4.0)), (float)((int)(sbounds.renderY - 2.0)), (float)(sbounds.OuterWidthInt + 4), (float)(sbounds.OuterHeightInt + 4), 50f, null);
				}
			}
		}

		private void renderGear(float deltaTime)
		{
			float screenWidth = (float)this.capi.Render.FrameWidth;
			double num = (double)((float)this.capi.Render.FrameHeight);
			IShaderProgram prevProg = this.capi.Render.CurrentActiveShader;
			prevProg.Stop();
			float yposhotbartop = (float)(num - this.dialogBounds.OuterHeight + GuiElement.scaled(15.0));
			float yposgeartop = yposhotbartop - (float)this.gearTexture.Height + (float)this.gearTexture.Height * 0.45f;
			float stabilityLevel = (float)this.capi.World.Player.Entity.WatchedAttributes.GetDouble("temporalStability", 0.0);
			float velocity = (float)this.capi.World.Player.Entity.Attributes.GetDouble("tempStabChangeVelocity", 0.0);
			GuiElementHoverText elem = this.Composers["hotbar"].GetHoverText("tempStabHoverText");
			if (elem.IsNowShown)
			{
				elem.SetNewText(((int)GameMath.Clamp(stabilityLevel * 100f, 0f, 100f)).ToString() + "%");
			}
			ShaderProgramGuigear guigear = ShaderPrograms.Guigear;
			guigear.Use();
			guigear.Tex2d2D = this.gearTexture.TextureId;
			this.modelMat.Set(this.capi.Render.CurrentModelviewMatrix);
			this.modelMat.Translate(screenWidth / 2f - (float)(this.gearTexture.Width / 2) - 1f, yposgeartop, -200f);
			this.modelMat.Scale((float)this.gearTexture.Width / 2f, (float)this.gearTexture.Height / 2f, 0f);
			float f = (float)this.capi.ElapsedMilliseconds / 1000f;
			this.modelMat.Translate(1f, 1f, 0f);
			float rndMotion = (GameMath.Sin(f / 50f) * 1f + (GameMath.Sin(f / 5f) * 0.5f + GameMath.Sin(f) * 3f + GameMath.Sin(f / 2f) * 1.5f) / 20f) / 2f;
			if ((stabilityLevel < 1f && velocity > 0f) || (stabilityLevel > 0f && velocity < 0f))
			{
				this.gearPosition += (double)velocity;
			}
			this.modelMat.RotateZ(rndMotion / 2f + (float)this.gearPosition + GlobalConstants.GuiGearRotJitter);
			guigear.GearCounter = f;
			guigear.StabilityLevel = stabilityLevel;
			guigear.ShadeYPos = yposhotbartop;
			guigear.GearHeight = (float)this.gearTexture.Height;
			guigear.HotbarYPos = yposhotbartop + (float)GuiElement.scaled(10.0);
			guigear.ProjectionMatrix = this.capi.Render.CurrentProjectionMatrix;
			guigear.ModelViewMatrix = this.modelMat.Values;
			this.capi.Render.RenderMesh(this.capi.Gui.QuadMeshRef);
			guigear.Stop();
			prevProg.Use();
		}

		public override void OnFinalizeFrame(float dt)
		{
			base.OnFinalizeFrame(dt);
			this.scrollWheeledSlotInFrame = false;
		}

		public override void Dispose()
		{
			base.Dispose();
			LoadedTexture loadedTexture = this.gearTexture;
			if (loadedTexture != null)
			{
				loadedTexture.Dispose();
			}
			LoadedTexture loadedTexture2 = this.toolModeBgTexture;
			if (loadedTexture2 != null)
			{
				loadedTexture2.Dispose();
			}
			DrawWorldInteractionUtil drawWorldInteractionUtil = this.wiUtil;
			if (drawWorldInteractionUtil == null)
			{
				return;
			}
			drawWorldInteractionUtil.Dispose();
		}

		private IInventory hotbarInv;

		private IInventory backpackInv;

		private GuiElementItemSlotGrid hotbarSlotGrid;

		private GuiElementItemSlotGrid backPackGrid;

		private long itemInfoTextActiveMs;

		private int lastActiveSlotStacksize = 99999;

		private int lastActiveSlotItemId;

		private EnumItemClass lastActiveClassItemClass;

		private bool scrollWheeledSlotInFrame;

		private bool shouldRecompose;

		private LoadedTexture currentToolModeTexture;

		private LoadedTexture toolModeBgTexture;

		private DrawWorldInteractionUtil wiUtil;

		private Matrixf modelMat = new Matrixf();

		private LoadedTexture gearTexture;

		private Dictionary<AssetLocation, ILoadedSound> leftActiveSlotIdleSound = new Dictionary<AssetLocation, ILoadedSound>();

		private Dictionary<AssetLocation, ILoadedSound> rightActiveSlotIdleSound = new Dictionary<AssetLocation, ILoadedSound>();

		private ItemStack prevLeftStack;

		private ItemStack prevRightStack;

		private ElementBounds dialogBounds;

		private bool temporalStabilityEnabled;

		private int prevValue = int.MaxValue;

		private int prevIndex = -1;

		private double gearPosition;
	}
}
