using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.Client.NoObf
{
	public class HudEntityNameTags : HudElement
	{
		public override double DrawOrder
		{
			get
			{
				return -0.1;
			}
		}

		public HudEntityNameTags(ICoreClientAPI capi)
			: base(capi)
		{
			this.TryOpen();
			this.game = (ClientMain)capi.World;
		}

		public override void OnRenderGUI(float deltaTime)
		{
			int plrDim = this.game.EntityPlayer.Pos.Dimension;
			foreach (Entity entity in this.game.LoadedEntities.Values)
			{
				if (this.game.frustumCuller.SphereInFrustum((double)((float)entity.Pos.X), (double)((float)(entity.Pos.Y + entity.LocalEyePos.Y)), (double)((float)entity.Pos.Z), 0.5) && entity.Pos.Dimension == plrDim)
				{
					EntityRenderer renderer;
					this.game.EntityRenderers.TryGetValue(entity.EntityId, out renderer);
					if (renderer != null)
					{
						renderer.DoRender2D(deltaTime);
					}
				}
			}
		}

		public override bool TryClose()
		{
			return false;
		}

		public override bool ShouldReceiveKeyboardEvents()
		{
			return false;
		}

		public override bool ShouldReceiveRenderEvents()
		{
			return true;
		}

		private ClientMain game;
	}
}
