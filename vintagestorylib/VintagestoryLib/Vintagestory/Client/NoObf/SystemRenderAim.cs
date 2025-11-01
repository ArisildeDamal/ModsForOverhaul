using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderAim : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "remi";
			}
		}

		public SystemRenderAim(ClientMain game)
			: base(game)
		{
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame2DOverlay), EnumRenderStage.Ortho, this.Name, 1.02);
		}

		public override void OnBlockTexturesLoaded()
		{
			this.aimTextureId = this.game.GetOrLoadCachedTexture(new AssetLocation("gui/target.png"));
			this.aimHostileTextureId = this.game.GetOrLoadCachedTexture(new AssetLocation("gui/targethostile.png"));
		}

		public void OnRenderFrame2DOverlay(float deltaTime)
		{
			if (this.game.MouseGrabbed)
			{
				this.DrawAim(this.game);
			}
		}

		internal void DrawAim(ClientMain game)
		{
			if (game.MainCamera.CameraMode != EnumCameraMode.FirstPerson || game.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			int aimwidth = 32;
			int aimheight = 32;
			EntitySelection entitySelection = game.EntitySelection;
			Entity entity = ((entitySelection != null) ? entitySelection.Entity : null);
			IClientPlayer player = game.Player;
			ItemStack itemStack;
			if (player == null)
			{
				itemStack = null;
			}
			else
			{
				IPlayerInventoryManager inventoryManager = player.InventoryManager;
				if (inventoryManager == null)
				{
					itemStack = null;
				}
				else
				{
					ItemSlot activeHotbarSlot = inventoryManager.ActiveHotbarSlot;
					itemStack = ((activeHotbarSlot != null) ? activeHotbarSlot.Itemstack : null);
				}
			}
			ItemStack heldStack = itemStack;
			float attackRange = ((heldStack == null) ? GlobalConstants.DefaultAttackRange : heldStack.Collectible.GetAttackRange(heldStack));
			int texId = this.aimTextureId;
			if (entity != null && game.EntityPlayer != null)
			{
				Cuboidd cuboidd = entity.SelectionBox.ToDouble().Translate(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
				EntityPos pos = game.EntityPlayer.SidedPos;
				if (cuboidd.ShortestDistanceFrom(pos.X + game.EntityPlayer.LocalEyePos.X, pos.Y + game.EntityPlayer.LocalEyePos.Y, pos.Z + game.EntityPlayer.LocalEyePos.Z) <= (double)attackRange - 0.08)
				{
					texId = this.aimHostileTextureId;
				}
			}
			game.Render2DTexture(texId, (float)(game.Width / 2 - aimwidth / 2), (float)(game.Height / 2 - aimheight / 2), (float)aimwidth, (float)aimheight, 10000f, null);
		}

		public override void Dispose(ClientMain game)
		{
			game.Platform.GLDeleteTexture(this.aimTextureId);
			game.Platform.GLDeleteTexture(this.aimHostileTextureId);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		private int aimTextureId;

		private int aimHostileTextureId;
	}
}
