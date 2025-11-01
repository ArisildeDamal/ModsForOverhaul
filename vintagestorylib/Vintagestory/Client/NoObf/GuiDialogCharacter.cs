using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class GuiDialogCharacter : GuiDialogCharacterBase
	{
		public GuiDialogCharacter(ICoreClientAPI capi)
			: base(capi)
		{
			this.rendertabhandlers.Add(new Action<GuiComposer>(this.ComposeCharacterTab));
		}

		private void registerArmorIcons()
		{
			this.capi.Gui.Icons.CustomIcons["armorhead"] = this.capi.Gui.Icons.SvgIconSource(new AssetLocation("textures/icons/character/armor-helmet.svg"));
			this.capi.Gui.Icons.CustomIcons["armorbody"] = this.capi.Gui.Icons.SvgIconSource(new AssetLocation("textures/icons/character/armor-body.svg"));
			this.capi.Gui.Icons.CustomIcons["armorlegs"] = this.capi.Gui.Icons.SvgIconSource(new AssetLocation("textures/icons/character/armor-legs.svg"));
		}

		private void ComposeCharacterTab(GuiComposer compo)
		{
			if (!this.capi.Gui.Icons.CustomIcons.ContainsKey("left_hand"))
			{
				this.registerArmorIcons();
			}
			double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
			ElementBounds leftSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad, 1, 6).FixedGrow(0.0, pad);
			ElementBounds leftArmorSlotBoundsHead = null;
			ElementBounds leftArmorSlotBoundsBody = null;
			ElementBounds leftArmorSlotBoundsLegs = null;
			if (this.showArmorSlots)
			{
				leftArmorSlotBoundsHead = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad, 1, 1).FixedGrow(0.0, pad);
				leftSlotBounds.FixedRightOf(leftArmorSlotBoundsHead, 10.0);
				leftArmorSlotBoundsBody = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad + 102.0, 1, 1).FixedGrow(0.0, pad);
				leftSlotBounds.FixedRightOf(leftArmorSlotBoundsBody, 10.0);
				leftArmorSlotBoundsLegs = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad + 204.0, 1, 1).FixedGrow(0.0, pad);
				leftSlotBounds.FixedRightOf(leftArmorSlotBoundsLegs, 10.0);
			}
			this.insetSlotBounds = ElementBounds.Fixed(0.0, 22.0 + pad, 190.0, leftSlotBounds.fixedHeight - 2.0 * pad - 4.0);
			this.insetSlotBounds.FixedRightOf(leftSlotBounds, 10.0);
			ElementBounds rightSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad, 1, 6).FixedGrow(0.0, pad);
			rightSlotBounds.FixedRightOf(this.insetSlotBounds, 10.0);
			leftSlotBounds.fixedHeight -= 6.0;
			rightSlotBounds.fixedHeight -= 6.0;
			compo.AddIf(this.showArmorSlots).AddItemSlotGrid(this.characterInv, new Action<object>(this.SendInvPacket), 1, new int[] { 12 }, leftArmorSlotBoundsHead, "armorSlotsHead").AddItemSlotGrid(this.characterInv, new Action<object>(this.SendInvPacket), 1, new int[] { 13 }, leftArmorSlotBoundsBody, "armorSlotsBody")
				.AddItemSlotGrid(this.characterInv, new Action<object>(this.SendInvPacket), 1, new int[] { 14 }, leftArmorSlotBoundsLegs, "armorSlotsLegs")
				.EndIf()
				.AddItemSlotGrid(this.characterInv, new Action<object>(this.SendInvPacket), 1, new int[] { 0, 1, 2, 11, 3, 4 }, leftSlotBounds, "leftSlots")
				.AddInset(this.insetSlotBounds, 0, 0.85f)
				.AddItemSlotGrid(this.characterInv, new Action<object>(this.SendInvPacket), 1, new int[] { 6, 7, 8, 10, 5, 9 }, rightSlotBounds, "rightSlots");
		}

		protected virtual void ComposeGuis()
		{
			this.characterInv = this.capi.World.Player.InventoryManager.GetOwnInventory("character");
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			if (this.curTab == 0)
			{
				bgBounds.BothSizing = ElementSizing.FitToChildren;
			}
			else
			{
				bgBounds.BothSizing = ElementSizing.Fixed;
				bgBounds.fixedWidth = this.mainTabInnerSize.Width;
				bgBounds.fixedHeight = this.mainTabInnerSize.Height;
			}
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
			string charClass = this.capi.World.Player.Entity.WatchedAttributes.GetString("characterClass", null);
			string title = Lang.Get("characterdialog-title-nameandclass", new object[]
			{
				this.capi.World.Player.PlayerName,
				Lang.Get("characterclass-" + charClass, Array.Empty<object>())
			});
			if (!Lang.HasTranslation("characterclass-" + charClass, true, true))
			{
				title = this.capi.World.Player.PlayerName;
			}
			ElementBounds tabBounds = ElementBounds.Fixed(5.0, -24.0, 350.0, 25.0);
			base.ClearComposers();
			this.Composers["playercharacter"] = this.capi.Gui.CreateCompo("playercharacter", dialogBounds).AddShadedDialogBG(bgBounds, true, 5.0, 0.75f).AddDialogTitleBar(title, new Action(this.OnTitleBarClose), null, null, null)
				.AddHorizontalTabs(this.tabs.ToArray(), tabBounds, new Action<int>(this.onTabClicked), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), "tabs")
				.BeginChildElements(bgBounds);
			this.Composers["playercharacter"].GetHorizontalTabs("tabs").activeElement = this.curTab;
			this.rendertabhandlers[this.curTab](this.Composers["playercharacter"]);
			this.Composers["playercharacter"].EndChildElements().Compose(true);
			if (this.ComposeExtraGuis != null)
			{
				this.ComposeExtraGuis();
			}
			if (this.curTab == 0)
			{
				this.mainTabInnerSize.Width = bgBounds.InnerWidth / (double)RuntimeEnv.GUIScale;
				this.mainTabInnerSize.Height = bgBounds.InnerHeight / (double)RuntimeEnv.GUIScale;
			}
		}

		private void onTabClicked(int tabindex)
		{
			Action<int> tabClicked = this.TabClicked;
			if (tabClicked != null)
			{
				tabClicked(tabindex);
			}
			this.curTab = tabindex;
			this.ComposeGuis();
		}

		public override void OnMouseDown(MouseEvent args)
		{
			base.OnMouseDown(args);
			this.rotateCharacter = this.insetSlotBounds.PointInside(args.X, args.Y);
		}

		public override void OnMouseUp(MouseEvent args)
		{
			base.OnMouseUp(args);
			this.rotateCharacter = false;
		}

		public override void OnMouseMove(MouseEvent args)
		{
			base.OnMouseMove(args);
			if (this.rotateCharacter)
			{
				this.yaw -= (float)args.DeltaX / 100f;
			}
		}

		public override event Action ComposeExtraGuis;

		public override event Action<int> TabClicked;

		public override void OnRenderGUI(float deltaTime)
		{
			base.OnRenderGUI(deltaTime);
			if (this.curTab == 0)
			{
				this.capi.Render.GlPushMatrix();
				if (this.focused)
				{
					this.capi.Render.GlTranslate(0f, 0f, 150f);
				}
				double pad = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
				this.capi.Render.GlRotate(-14f, 1f, 0f, 0f);
				this.mat.Identity();
				this.mat.RotateXDeg(-14f);
				Vec4f lightRot = this.mat.TransformVector(this.lighPos);
				this.capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(lightRot.X, lightRot.Y, lightRot.Z));
				this.capi.Render.RenderEntityToGui(deltaTime, this.capi.World.Player.Entity, this.insetSlotBounds.renderX + pad - GuiElement.scaled(41.0), this.insetSlotBounds.renderY + pad - GuiElement.scaled(30.0), GuiElement.scaled(250.0), this.yaw, (float)GuiElement.scaled(135.0), -1);
				this.capi.Render.GlPopMatrix();
				this.capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(1f, -1f, 0f).Normalize());
				if (!this.insetSlotBounds.PointInside(this.capi.Input.MouseX, this.capi.Input.MouseY) && !this.rotateCharacter)
				{
					this.yaw += (float)(Math.Sin((double)((float)this.capi.World.ElapsedMilliseconds / 1000f)) / 200.0);
				}
			}
		}

		public override void OnGuiOpened()
		{
			this.ComposeGuis();
			if ((this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || this.capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival) && this.characterInv != null)
			{
				this.characterInv.Open(this.capi.World.Player);
			}
		}

		public override void OnGuiClosed()
		{
			if (this.characterInv != null)
			{
				this.characterInv.Close(this.capi.World.Player);
				GuiElementItemSlotGrid slotGrid = this.Composers["playercharacter"].GetSlotGrid("leftSlots");
				if (slotGrid != null)
				{
					slotGrid.OnGuiClosed(this.capi);
				}
				GuiElementItemSlotGrid slotGrid2 = this.Composers["playercharacter"].GetSlotGrid("rightSlots");
				if (slotGrid2 != null)
				{
					slotGrid2.OnGuiClosed(this.capi);
				}
			}
			this.curTab = 0;
		}

		protected void SendInvPacket(object packet)
		{
			this.capi.Network.SendPacketClient(packet);
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return "characterdialog";
			}
		}

		public override bool PrefersUngrabbedMouse
		{
			get
			{
				return false;
			}
		}

		public override float ZSize
		{
			get
			{
				return RuntimeEnv.GUIScale * 280f;
			}
		}

		public override List<GuiTab> Tabs
		{
			get
			{
				return this.tabs;
			}
		}

		public override List<Action<GuiComposer>> RenderTabHandlers
		{
			get
			{
				return this.rendertabhandlers;
			}
		}

		protected IInventory characterInv;

		protected ElementBounds insetSlotBounds;

		protected float yaw = -1.2707963f;

		protected bool rotateCharacter;

		protected bool showArmorSlots = true;

		private int curTab;

		private List<GuiTab> tabs = new List<GuiTab>(new GuiTab[]
		{
			new GuiTab
			{
				Name = Lang.Get("charactertab-character", Array.Empty<object>()),
				DataInt = 0
			}
		});

		public List<Action<GuiComposer>> rendertabhandlers = new List<Action<GuiComposer>>();

		private Size2d mainTabInnerSize = new Size2d();

		private Vec4f lighPos = new Vec4f(-1f, -1f, 0f, 0f).NormalizeXYZ();

		private Matrixf mat = new Matrixf();
	}
}
