using System;
using System.Runtime;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	public class SystemHotkeys : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "ho";
			}
		}

		public SystemHotkeys(ClientMain game)
			: base(game)
		{
		}

		public override void OnBlockTexturesLoaded()
		{
			HotkeyManager hotkeyManager = ScreenManager.hotkeyManager;
			hotkeyManager.SetHotKeyHandler("decspeed", new ActionConsumable<KeyCombination>(this.KeyNormalSpeed), true);
			hotkeyManager.SetHotKeyHandler("incspeed", new ActionConsumable<KeyCombination>(this.KeyFastSpeed), true);
			hotkeyManager.SetHotKeyHandler("decspeedfrac", new ActionConsumable<KeyCombination>(this.KeyNormalSpeed), true);
			hotkeyManager.SetHotKeyHandler("incspeedfrac", new ActionConsumable<KeyCombination>(this.KeyFastSpeed), true);
			hotkeyManager.SetHotKeyHandler("cycleflymodes", new ActionConsumable<KeyCombination>(this.KeyCycleFlyModes), true);
			hotkeyManager.SetHotKeyHandler("fly", new ActionConsumable<KeyCombination>(this.KeyToggleFly), true);
			hotkeyManager.SetHotKeyHandler("dropitem", new ActionConsumable<KeyCombination>(this.KeyDropItem), true);
			hotkeyManager.SetHotKeyHandler("dropitems", new ActionConsumable<KeyCombination>(this.KeyDropItems), true);
			hotkeyManager.SetHotKeyHandler("pickblock", new ActionConsumable<KeyCombination>(this.KeyPickBlock), true);
			hotkeyManager.SetHotKeyHandler("reloadshaders", new ActionConsumable<KeyCombination>(this.KeyReloadShaders), true);
			hotkeyManager.SetHotKeyHandler("reloadtextures", new ActionConsumable<KeyCombination>(this.KeyReloadTextures), true);
			hotkeyManager.SetHotKeyHandler("togglehud", new ActionConsumable<KeyCombination>(this.KeyToggleHUD), true);
			hotkeyManager.SetHotKeyHandler("compactheap", new ActionConsumable<KeyCombination>(this.KeyCompactHeap), true);
			hotkeyManager.SetHotKeyHandler("rendermetablocks", new ActionConsumable<KeyCombination>(this.KeyRenderMetaBlocks), true);
			hotkeyManager.SetHotKeyHandler("primarymouse", new ActionConsumable<KeyCombination>(this.OnPrimaryMouseButton), true);
			hotkeyManager.SetHotKeyHandler("secondarymouse", new ActionConsumable<KeyCombination>(this.OnSecondaryMouseButton), true);
			hotkeyManager.SetHotKeyHandler("middlemouse", new ActionConsumable<KeyCombination>(this.OnMiddleMouseButton), true);
			this.game.api.RegisterLinkProtocol("hotkey", new Action<LinkTextComponent>(this.hotKeyLinkClicked));
		}

		private bool OnPrimaryMouseButton(KeyCombination mb)
		{
			return this.game.UpdateMouseButtonState(EnumMouseButton.Left, !mb.OnKeyUp);
		}

		private bool OnSecondaryMouseButton(KeyCombination mb)
		{
			return this.game.UpdateMouseButtonState(EnumMouseButton.Right, !mb.OnKeyUp);
		}

		private bool OnMiddleMouseButton(KeyCombination mb)
		{
			return this.game.UpdateMouseButtonState(EnumMouseButton.Middle, !mb.OnKeyUp);
		}

		private void hotKeyLinkClicked(LinkTextComponent comp)
		{
			string hotkey = comp.Href.Substring("hotkey://".Length);
			HotKey hk;
			if (ScreenManager.hotkeyManager.HotKeys.TryGetValue(hotkey, out hk))
			{
				hk.Handler(hk.CurrentMapping);
			}
		}

		private bool KeyRenderMetaBlocks(KeyCombination t1)
		{
			ClientSettings.RenderMetaBlocks = !ClientSettings.RenderMetaBlocks;
			this.game.ShowChatMessage("Render meta blocks now " + (ClientSettings.RenderMetaBlocks ? "on" : "off"));
			return true;
		}

		private bool KeyNormalSpeed(KeyCombination viaKeyComb)
		{
			float size = (viaKeyComb.Shift ? 0.1f : 1f);
			float speed = (float)Math.Max((double)size, Math.Round((double)(10f * (this.game.player.worlddata.MoveSpeedMultiplier - size))) / 10.0);
			this.game.player.worlddata.SetMode(this.game, speed);
			this.game.ShowChatMessage(string.Format("Movespeed: {0}", speed));
			return true;
		}

		private bool KeyFastSpeed(KeyCombination viaKeyComb)
		{
			float size = (viaKeyComb.Shift ? 0.1f : 1f);
			float speed = (float)Math.Max((double)size, Math.Round((double)(10f * (this.game.player.worlddata.MoveSpeedMultiplier + size))) / 10.0);
			this.game.player.worlddata.SetMode(this.game, speed);
			this.game.ShowChatMessage(string.Format("Movespeed: {0}", speed));
			return true;
		}

		private bool KeyToggleFly(KeyCombination t1)
		{
			if ((this.game.player.worlddata.CurrentGameMode != EnumGameMode.Creative && this.game.player.worlddata.CurrentGameMode != EnumGameMode.Spectator) || !this.game.AllowFreemove)
			{
				return false;
			}
			this.game.EntityPlayer.Pos.Motion.Set(0.0, 0.0, 0.0);
			if (!this.game.player.worlddata.FreeMove)
			{
				this.game.player.worlddata.RequestModeFreeMove(this.game, true);
			}
			else
			{
				this.game.player.worlddata.RequestMode(this.game, false, false);
			}
			return true;
		}

		private bool KeyCycleFlyModes(KeyCombination viaKeyComb)
		{
			if ((this.game.player.worlddata.CurrentGameMode != EnumGameMode.Creative && this.game.player.worlddata.CurrentGameMode != EnumGameMode.Spectator) || !this.game.AllowFreemove)
			{
				return false;
			}
			this.game.EntityPlayer.Pos.Motion.Set(0.0, 0.0, 0.0);
			if (!this.game.player.worlddata.FreeMove)
			{
				this.game.player.worlddata.RequestModeFreeMove(this.game, true);
				this.game.ShowChatMessage(Lang.Get("Fly mode on", Array.Empty<object>()));
			}
			else if (this.game.player.worlddata.FreeMove && !this.game.player.worlddata.NoClip)
			{
				this.game.player.worlddata.RequestModeNoClip(this.game, true);
				this.game.ShowChatMessage(Lang.Get("Fly mode + noclip on", Array.Empty<object>()));
			}
			else if (this.game.player.worlddata.FreeMove && this.game.player.worlddata.NoClip)
			{
				this.game.player.worlddata.RequestMode(this.game, false, false);
				this.game.ShowChatMessage(Lang.Get("Fly mode off, noclip off", Array.Empty<object>()));
			}
			return true;
		}

		private bool KeyDropItem(KeyCombination viaKeyComb)
		{
			ItemSlot slot = this.game.player.inventoryMgr.currentHoveredSlot;
			if (slot == null)
			{
				slot = this.game.player.inventoryMgr.ActiveHotbarSlot;
			}
			if (this.game.player.inventoryMgr.DropItem(slot, false))
			{
				this.game.PlaySound(new AssetLocation("sounds/player/quickthrow"), true, 1f);
			}
			return true;
		}

		private bool KeyDropItems(KeyCombination viaKeyComb)
		{
			ItemSlot slot = this.game.player.inventoryMgr.currentHoveredSlot;
			if (slot == null)
			{
				slot = this.game.player.inventoryMgr.ActiveHotbarSlot;
			}
			if (this.game.player.inventoryMgr.DropItem(slot, true))
			{
				this.game.PlaySound(new AssetLocation("sounds/player/quickthrow"), true, 1f);
			}
			return true;
		}

		private bool KeyPickBlock(KeyCombination viaKeyComb)
		{
			this.game.PickBlock = !viaKeyComb.OnKeyUp;
			return true;
		}

		private bool KeyReloadShaders(KeyCombination viaKeyComb)
		{
			bool ok = ShaderRegistry.ReloadShaders();
			bool ok2 = this.game.eventManager != null && this.game.eventManager.TriggerReloadShaders();
			this.game.Logger.Notification("Shaders reloaded.");
			ok = ok && ok2;
			this.game.ShowChatMessage("Shaders reloaded" + (ok ? "" : ". errors occured, please check client log"));
			return true;
		}

		private bool KeyReloadTextures(KeyCombination viaKeyComb)
		{
			this.game.AssetManager.Reload(AssetCategory.textures);
			this.game.ReloadTextures();
			this.game.ShowChatMessage("Textures reloaded");
			return true;
		}

		private bool KeyToggleHUD(KeyCombination viaKeyComb)
		{
			this.game.ShouldRender2DOverlays = !this.game.ShouldRender2DOverlays;
			return true;
		}

		private bool KeyCompactHeap(KeyCombination viaKeyComb)
		{
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect();
			this.game.ShowChatMessage("Compacted large object heap");
			return true;
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}
	}
}
