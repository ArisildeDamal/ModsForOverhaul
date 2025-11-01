using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class HudElementBlockAndEntityInfo : HudElement
	{
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "blockinfohud";
			}
		}

		public HudElementBlockAndEntityInfo(ICoreClientAPI capi)
			: base(capi)
		{
			capi.Event.RegisterGameTickListener(new Action<float>(this.Every15ms), 15, 0);
			capi.Event.RegisterGameTickListener(new Action<float>(this.Every500ms), 500, 0);
			capi.Event.BlockChanged += this.OnBlockChanged;
			this.ComposeBlockInfoHud();
			if (ClientSettings.ShowBlockInfoHud)
			{
				this.TryOpen();
			}
			ClientSettings.Inst.AddWatcher<bool>("showBlockInfoHud", delegate(bool on)
			{
				if (on)
				{
					this.TryOpen();
					return;
				}
				this.TryClose();
			});
		}

		private void ComposeBlockInfoHud()
		{
			string newTitle = "";
			string newDetail = "";
			if (this.currentBlock != null)
			{
				if (this.currentBlock.Code == null)
				{
					newTitle = "Unknown block ID " + this.capi.World.BlockAccessor.GetBlockId(this.currentPos).ToString();
					newDetail = "";
				}
				else
				{
					newTitle = this.currentBlock.GetPlacedBlockName(this.capi.World, this.currentPos);
					newDetail = this.currentBlock.GetPlacedBlockInfo(this.capi.World, this.currentPos, this.capi.World.Player);
					if (newDetail == null)
					{
						newDetail = "";
					}
					if (newTitle == null)
					{
						newTitle = "Unknown";
					}
				}
			}
			if (this.currentEntity != null)
			{
				newTitle = this.currentEntity.GetName();
				newDetail = this.currentEntity.GetInfoText();
				if (newDetail == null)
				{
					newDetail = "";
				}
				if (newTitle == null)
				{
					newTitle = "Unknown Entity code " + this.currentEntity.Code;
				}
			}
			if (this.title == newTitle && this.detail == newDetail)
			{
				return;
			}
			this.title = newTitle;
			this.detail = newDetail;
			ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 0.0, 500.0, 24.0);
			ElementBounds detailTextBounds = textBounds.BelowCopy(0.0, 10.0, 0.0, 0.0);
			detailTextBounds.Alignment = EnumDialogArea.None;
			ElementBounds overlayBounds = new ElementBounds();
			overlayBounds.BothSizing = ElementSizing.FitToChildren;
			overlayBounds.WithFixedPadding(5.0, 5.0);
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0.0, GuiStyle.DialogToScreenPadding);
			LoadedTexture reuseRichTextTexture = null;
			GuiElementRichtext rtElem;
			if (this.composer == null)
			{
				this.composer = this.capi.Gui.CreateCompo("blockinfohud", dialogBounds);
			}
			else
			{
				rtElem = this.composer.GetRichtext("rt");
				reuseRichTextTexture = rtElem.richtTextTexture;
				rtElem.richtTextTexture = null;
				this.composer.Clear(dialogBounds);
			}
			this.Composers["blockinfohud"] = this.composer;
			this.composer.AddGameOverlay(overlayBounds, null).BeginChildElements(overlayBounds).AddStaticTextAutoBoxSize(this.title, CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, textBounds, null)
				.AddRichtext(this.detail, CairoFont.WhiteDetailText(), detailTextBounds, "rt")
				.EndChildElements();
			rtElem = this.composer.GetRichtext("rt");
			if (this.detail.Length == 0)
			{
				detailTextBounds.fixedY = 0.0;
				detailTextBounds.fixedHeight = 0.0;
			}
			if (reuseRichTextTexture != null)
			{
				rtElem.richtTextTexture = reuseRichTextTexture;
			}
			rtElem.BeforeCalcBounds();
			detailTextBounds.fixedWidth = Math.Min(500.0, rtElem.MaxLineWidth / (double)RuntimeEnv.GUIScale + 1.0);
			this.composer.Compose(true);
		}

		private void Every15ms(float dt)
		{
			if (!this.IsOpened())
			{
				return;
			}
			if (this.capi.World.Player.CurrentEntitySelection != null)
			{
				this.currentBlock = null;
				this.EntityInView();
				return;
			}
			this.currentEntity = null;
			if (this.capi.World.Player.CurrentBlockSelection == null)
			{
				this.currentBlock = null;
				return;
			}
			this.currentEntity = null;
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
			if (block != this.currentBlock || !this.currentPos.Equals(bs.Position) || this.currentSelectionIndex != bs.SelectionBoxIndex)
			{
				this.currentBlock = block;
				this.currentSelectionIndex = bs.SelectionBoxIndex;
				this.currentPos = (bs.DidOffset ? bs.Position.Copy().Add(bs.Face.Opposite, 1) : bs.Position.Copy());
				this.ComposeBlockInfoHud();
			}
		}

		private void EntityInView()
		{
			Entity nowEntity = this.capi.World.Player.CurrentEntitySelection.Entity;
			if (nowEntity != this.currentEntity)
			{
				this.currentEntity = nowEntity;
				this.ComposeBlockInfoHud();
			}
		}

		public override bool ShouldReceiveRenderEvents()
		{
			return this.currentBlock != null || this.currentEntity != null;
		}

		private void OnBlockChanged(BlockPos pos, Block oldBlock)
		{
			IPlayer player = this.capi.World.Player;
			if (((player != null) ? player.CurrentBlockSelection : null) != null && pos.Equals(player.CurrentBlockSelection.Position))
			{
				this.ComposeBlockInfoHud();
			}
		}

		private void Every500ms(float dt)
		{
			this.Every15ms(dt);
			this.ComposeBlockInfoHud();
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			ClientSettings.ShowBlockInfoHud = true;
		}

		public override void OnGuiClosed()
		{
			base.OnGuiClosed();
			ClientSettings.ShowBlockInfoHud = false;
		}

		public override void Dispose()
		{
			base.Dispose();
			GuiComposer guiComposer = this.composer;
			if (guiComposer == null)
			{
				return;
			}
			guiComposer.Dispose();
		}

		private Block currentBlock;

		private int currentSelectionIndex;

		private Entity currentEntity;

		private BlockPos currentPos;

		private string title;

		private string detail;

		private GuiComposer composer;
	}
}
