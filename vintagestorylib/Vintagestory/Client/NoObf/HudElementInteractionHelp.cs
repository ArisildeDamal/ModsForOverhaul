using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class HudElementInteractionHelp : HudElement
	{
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "blockinteractionhelp";
			}
		}

		public override double DrawOrder
		{
			get
			{
				return 0.05;
			}
		}

		public HudElementInteractionHelp(ICoreClientAPI capi)
			: base(capi)
		{
			this.wiUtil = new DrawWorldInteractionUtil(capi, this.Composers, "-placedBlock");
			ClientEventManager eventManager = (capi.World as ClientMain).eventManager;
			if (eventManager != null)
			{
				eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.PlayerPosDiv8, new OnPlayerPropertyChanged(this.PlayerPosDiv8Changed));
			}
			capi.Event.RegisterGameTickListener(new Action<float>(this.Every15ms), 15, 0);
			capi.Event.BlockChanged += this.OnBlockChanged;
			this.ComposeBlockWorldInteractionHelp();
			ClientSettings.Inst.AddWatcher<bool>("showBlockInteractionHelp", delegate(bool on)
			{
				if (on)
				{
					this.TryOpen();
					return;
				}
				this.TryClose();
			});
			if (ClientSettings.ShowBlockInteractionHelp)
			{
				this.TryOpen();
			}
		}

		private void ComposeBlockWorldInteractionHelp()
		{
			if (!this.IsOpened())
			{
				return;
			}
			WorldInteraction[] wis = this.getWorldInteractions();
			this.wiUtil.ComposeBlockWorldInteractionHelp(wis);
		}

		private WorldInteraction[] getWorldInteractions()
		{
			if (this.currentBlock != null)
			{
				EntityPos plrpos = this.capi.World.Player.Entity.Pos;
				BlockSelection bs = this.capi.World.Player.CurrentBlockSelection;
				if (bs == null || plrpos.XYZ.AsBlockPos.DistanceTo(bs.Position) > 8f)
				{
					return null;
				}
				return this.currentBlock.GetPlacedBlockInteractionHelp(this.capi.World, bs, this.capi.World.Player);
			}
			else
			{
				if (this.currentEntity == null)
				{
					return null;
				}
				EntityPos plrpos2 = this.capi.World.Player.Entity.Pos;
				EntitySelection es = this.capi.World.Player.CurrentEntitySelection;
				if (es == null || plrpos2.XYZ.AsBlockPos.DistanceTo(es.Position.AsBlockPos) > 8f)
				{
					return null;
				}
				return es.Entity.GetInteractionHelp(this.capi.World, es, this.capi.World.Player);
			}
		}

		private void Every15ms(float dt)
		{
			if (!this.IsOpened())
			{
				return;
			}
			if (this.capi.World.Player.CurrentEntitySelection != null)
			{
				this.EntityInView();
				return;
			}
			this.currentEntity = null;
			if (this.capi.World.Player.CurrentBlockSelection == null)
			{
				this.currentBlock = null;
				return;
			}
			this.BlockInView();
		}

		private void BlockInView()
		{
			BlockSelection bs = this.capi.World.Player.CurrentBlockSelection;
			Block block;
			if (bs.DidOffset)
			{
				BlockFacing facing = bs.Face.Opposite;
				block = this.capi.World.BlockAccessor.GetBlockOnSide(bs.Position, facing, 0);
			}
			else
			{
				block = this.capi.World.BlockAccessor.GetBlock(bs.Position);
			}
			if (block.BlockId == 0)
			{
				this.currentBlock = null;
				return;
			}
			if (block != this.currentBlock || (int)this.currentPos.X != bs.Position.X || (int)this.currentPos.Y != bs.Position.Y || (int)this.currentPos.Z != bs.Position.Z || bs.SelectionBoxIndex != this.currentBlockSelectionIndex)
			{
				this.currentBlockSelectionIndex = bs.SelectionBoxIndex;
				this.currentBlock = block;
				this.currentEntity = null;
				this.currentPos = bs.Position.ToVec3d().Add(0.5, (double)block.InteractionHelpYOffset, 0.5);
				if (this.currentBlock.RandomDrawOffset != 0)
				{
					this.currentPos.X += (double)((float)(GameMath.oaatHash(bs.Position.X, 0, bs.Position.Z) % 12) / (24f + 12f * (float)this.currentBlock.RandomDrawOffset));
					this.currentPos.Z += (double)((float)(GameMath.oaatHash(bs.Position.X, 1, bs.Position.Z) % 12) / (24f + 12f * (float)this.currentBlock.RandomDrawOffset));
				}
				this.ComposeBlockWorldInteractionHelp();
			}
		}

		private void EntityInView()
		{
			Entity nowEntity = this.capi.World.Player.CurrentEntitySelection.Entity;
			int nowSeleBox = this.capi.World.Player.CurrentEntitySelection.SelectionBoxIndex;
			if (this.entitySelectionBoxIndex == nowSeleBox && nowEntity == this.currentEntity)
			{
				bool flag = this.wasAlive;
				bool? flag2 = ((nowEntity != null) ? new bool?(nowEntity.Alive) : null);
				if ((flag == flag2.GetValueOrDefault()) & (flag2 != null))
				{
					int num = this.entityInViewCount;
					this.entityInViewCount = num + 1;
					if (num <= 20)
					{
						return;
					}
				}
			}
			this.entityInViewCount = 0;
			this.wasAlive = nowEntity.Alive;
			this.currentEntity = nowEntity;
			this.currentBlock = null;
			this.entitySelectionBoxIndex = nowSeleBox;
			this.cp = nowEntity.GetInterface<ICustomInteractionHelpPositioning>();
			this.ComposeBlockWorldInteractionHelp();
		}

		public override void OnRenderGUI(float deltaTime)
		{
			if (this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				return;
			}
			GuiComposer composer = this.wiUtil.Composer;
			ElementBounds bounds = ((composer != null) ? composer.Bounds : null);
			if (this.currentEntity != null)
			{
				bool flag = this.customCurrentPosSet;
				if (this.cp != null)
				{
					this.currentPos = this.cp.GetInteractionHelpPosition();
					this.customCurrentPosSet = this.currentPos != null;
				}
				if (this.cp == null || this.currentPos == null)
				{
					double offX = (double)(this.currentEntity.SelectionBox.X2 - this.currentEntity.OriginSelectionBox.X2);
					double offZ = (double)(this.currentEntity.SelectionBox.Z2 - this.currentEntity.OriginSelectionBox.Z2);
					this.currentPos = this.currentEntity.ServerPos.XYZ.Add(offX, (double)this.currentEntity.SelectionBox.Y2, offZ);
				}
			}
			if (bounds != null)
			{
				Vec3d pos = MatrixToolsd.Project(this.currentPos, this.capi.Render.PerspectiveProjectionMat, this.capi.Render.PerspectiveViewMat, this.capi.Render.FrameWidth, this.capi.Render.FrameHeight);
				if (pos.Z < 0.0)
				{
					return;
				}
				bounds.Alignment = EnumDialogArea.None;
				bounds.fixedOffsetX = 0.0;
				bounds.fixedOffsetY = 0.0;
				bounds.absFixedX = pos.X - this.wiUtil.ActualWidth / 2.0;
				bounds.absFixedY = (double)this.capi.Render.FrameHeight - pos.Y - bounds.OuterHeight * 0.8;
				bounds.absMarginX = 0.0;
				bounds.absMarginY = 0.0;
			}
			if ((this.capi.World as ClientMain).MouseGrabbed)
			{
				if (this.cp == null || this.cp.TransparentCenter)
				{
					this.capi.Render.CurrentActiveShader.Uniform("transparentCenter", 1);
				}
				base.OnRenderGUI(deltaTime);
				this.capi.Render.CurrentActiveShader.Uniform("transparentCenter", 0);
			}
		}

		private void PlayerPosDiv8Changed(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
		{
			this.ComposeBlockWorldInteractionHelp();
		}

		public override bool ShouldReceiveRenderEvents()
		{
			return this.currentBlock != null || this.currentEntity != null;
		}

		public override bool ShouldReceiveKeyboardEvents()
		{
			return false;
		}

		public override bool ShouldReceiveMouseEvents()
		{
			return false;
		}

		private void OnBlockChanged(BlockPos pos, Block oldBlock)
		{
			IPlayer player = this.capi.World.Player;
			if (((player != null) ? player.CurrentBlockSelection : null) != null && pos.Equals(player.CurrentBlockSelection.Position))
			{
				this.ComposeBlockWorldInteractionHelp();
			}
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			ClientSettings.ShowBlockInteractionHelp = true;
		}

		public override void OnGuiClosed()
		{
			base.OnGuiClosed();
			ClientSettings.ShowBlockInteractionHelp = false;
		}

		public override void Dispose()
		{
			base.Dispose();
			DrawWorldInteractionUtil drawWorldInteractionUtil = this.wiUtil;
			if (drawWorldInteractionUtil == null)
			{
				return;
			}
			drawWorldInteractionUtil.Dispose();
		}

		private Block currentBlock;

		private int currentBlockSelectionIndex;

		private Entity currentEntity;

		private Vec3d currentPos;

		private DrawWorldInteractionUtil wiUtil;

		private int entityInViewCount;

		private int entitySelectionBoxIndex = -1;

		private bool wasAlive;

		private ICustomInteractionHelpPositioning cp;

		private bool customCurrentPosSet;
	}
}
