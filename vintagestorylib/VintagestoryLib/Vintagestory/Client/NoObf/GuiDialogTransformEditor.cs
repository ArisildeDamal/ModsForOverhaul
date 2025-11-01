using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class GuiDialogTransformEditor : GuiDialog
	{
		public ModelTransform TargetTransform
		{
			get
			{
				List<ModelTransform> tf = new List<ModelTransform>(new ModelTransform[]
				{
					this.oldCollectible.GuiTransform,
					this.oldCollectible.FpHandTransform,
					this.oldCollectible.TpHandTransform,
					this.oldCollectible.TpOffHandTransform,
					this.oldCollectible.GroundTransform
				});
				foreach (TransformConfig extraTf in GuiDialogTransformEditor.extraTransforms)
				{
					List<ModelTransform> list = tf;
					JsonObject attributes = this.oldCollectible.Attributes;
					ModelTransform modelTransform;
					if (attributes == null || !attributes[extraTf.AttributeName].Exists)
					{
						modelTransform = new ModelTransform().EnsureDefaultValues();
					}
					else
					{
						JsonObject attributes2 = this.oldCollectible.Attributes;
						modelTransform = ((attributes2 != null) ? attributes2[extraTf.AttributeName].AsObject<ModelTransform>(null) : null);
					}
					list.Add(modelTransform);
				}
				return tf[this.target];
			}
			set
			{
				switch (this.target)
				{
				case 0:
					this.oldCollectible.GuiTransform = value;
					return;
				case 2:
					this.oldCollectible.TpHandTransform = value;
					return;
				case 3:
					this.oldCollectible.TpOffHandTransform = value;
					return;
				case 4:
					this.oldCollectible.GroundTransform = value;
					return;
				}
				if (GuiDialogTransformEditor.extraTransforms.Count >= this.target - 5)
				{
					TransformConfig extraTf = GuiDialogTransformEditor.extraTransforms[this.target - 5];
					if (this.oldCollectible.Attributes == null)
					{
						this.oldCollectible.Attributes = new JsonObject(new JObject());
					}
					this.oldCollectible.Attributes.Token[extraTf.AttributeName] = JToken.FromObject(value);
				}
			}
		}

		public GuiDialogTransformEditor(ICoreClientAPI capi)
			: base(capi)
		{
			(capi.World as ClientMain).eventManager.OnActiveSlotChanged.Add(new Action(this.OnActiveSlotChanged));
			capi.ChatCommands.GetOrCreate("dev").WithDescription("Gamedev tools").BeginSubCommand("tfedit")
				.WithRootAlias("tfedit")
				.WithDescription("Opens the Transform Editor")
				.WithArgs(new ICommandArgumentParser[] { capi.ChatCommands.Parsers.OptionalWordRange("type", new string[] { "fp", "tp", "tpo", "gui", "ground" }) })
				.HandleWith(new OnCommandDelegate(this.CmdTransformEditor))
				.EndSubCommand();
		}

		private TextCommandResult CmdTransformEditor(TextCommandCallingArgs args)
		{
			string type = args[0] as string;
			if (!(type == "gui"))
			{
				if (!(type == "tp"))
				{
					if (!(type == "tpo"))
					{
						if (type == "ground")
						{
							this.target = 4;
						}
					}
					else
					{
						this.target = 3;
					}
				}
				else
				{
					this.target = 2;
				}
			}
			else
			{
				this.target = 0;
			}
			int index = -1;
			for (int i = 0; i < GuiDialogTransformEditor.extraTransforms.Count; i++)
			{
				if (GuiDialogTransformEditor.extraTransforms[i].AttributeName == type)
				{
					index = i;
				}
			}
			if (index >= 0)
			{
				this.target = index + 5;
			}
			ItemSlot activeHotbarSlot = this.capi.World.Player.InventoryManager.ActiveHotbarSlot;
			if (((activeHotbarSlot != null) ? activeHotbarSlot.Itemstack : null) == null)
			{
				return TextCommandResult.Success("Put something in your active slot first", null);
			}
			this.TryOpen();
			return TextCommandResult.Success("", null);
		}

		public override void OnGuiOpened()
		{
			this.capi.Event.PushEvent("onedittransforms", null);
			this.currentTransform = new ModelTransform();
			this.currentTransform.Rotation = default(FastVec3f);
			this.currentTransform.Translation = default(FastVec3f);
			this.oldCollectible = this.capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible;
			this.originalTransform = this.TargetTransform;
			this.TargetTransform = (this.currentTransform = this.originalTransform.Clone());
			this.ComposeDialog();
		}

		private void OnActiveSlotChanged()
		{
			if (!this.IsOpened())
			{
				return;
			}
			this.TargetTransform = this.originalTransform;
			ItemSlot activeHotbarSlot = this.capi.World.Player.InventoryManager.ActiveHotbarSlot;
			if (((activeHotbarSlot != null) ? activeHotbarSlot.Itemstack : null) == null)
			{
				this.TryClose();
				this.capi.World.Player.ShowChatNotification("Put something in your active slot");
				return;
			}
			this.oldCollectible = this.capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible;
			this.originalTransform = this.TargetTransform;
			this.currentTransform = this.originalTransform.Clone();
			this.TargetTransform = this.currentTransform;
		}

		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		private void ComposeDialog()
		{
			base.ClearComposers();
			ElementBounds line = ElementBounds.Fixed(0.0, 22.0, 500.0, 20.0);
			ElementBounds inputBnds = ElementBounds.Fixed(0.0, 11.0, 230.0, 30.0);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(110.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
			ElementBounds tabBounds = ElementBounds.Fixed(-320.0, 35.0, 300.0, 500.0);
			ElementBounds textAreaBounds = ElementBounds.FixedSize(500.0, 200.0);
			ElementBounds clippingBounds = ElementBounds.FixedSize(500.0, 200.0);
			ElementBounds btnBounds = ElementBounds.FixedSize(200.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);
			List<GuiTab> tabs = new List<GuiTab>
			{
				new GuiTab
				{
					DataInt = 0,
					Name = "Gui"
				},
				new GuiTab
				{
					DataInt = 2,
					Name = "Main Hand"
				},
				new GuiTab
				{
					DataInt = 3,
					Name = "Off Hand"
				},
				new GuiTab
				{
					DataInt = 4,
					Name = "Ground"
				}
			};
			int i = 5;
			double padtop = GuiElement.scaled(15.0);
			foreach (TransformConfig extraTf in GuiDialogTransformEditor.extraTransforms)
			{
				tabs.Add(new GuiTab
				{
					DataInt = i++,
					Name = extraTf.Title,
					PaddingTop = padtop
				});
				padtop = 0.0;
			}
			base.SingleComposer = this.capi.Gui.CreateCompo("transformeditor", dialogBounds).AddShadedDialogBG(bgBounds, true, 5.0, 0.75f).AddDialogTitleBar("Transform Editor (" + this.target.ToString() + ")", new Action(this.OnTitleBarClose), null, null, null)
				.BeginChildElements(bgBounds)
				.AddVerticalTabs(tabs.ToArray(), tabBounds, new Action<int, GuiTab>(this.OnTabClicked), "verticalTabs")
				.AddStaticText("Translation X", CairoFont.WhiteDetailText(), line = line.FlatCopy().WithFixedWidth(230.0), null)
				.AddNumberInput(inputBnds = inputBnds.BelowCopy(0.0, 0.0, 0.0, 0.0), new Action<string>(this.OnTranslateX), CairoFont.WhiteDetailText(), "translatex")
				.AddStaticText("Origin X", CairoFont.WhiteDetailText(), line.RightCopy(40.0, 0.0, 0.0, 0.0), null)
				.AddNumberInput(inputBnds.RightCopy(40.0, 0.0, 0.0, 0.0), new Action<string>(this.OnOriginX), CairoFont.WhiteDetailText(), "originx")
				.AddStaticText("Translation Y", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 33.0, 0.0, 0.0), null)
				.AddNumberInput(inputBnds = inputBnds.BelowCopy(0.0, 22.0, 0.0, 0.0), new Action<string>(this.OnTranslateY), CairoFont.WhiteDetailText(), "translatey")
				.AddStaticText("Origin Y", CairoFont.WhiteDetailText(), line.RightCopy(40.0, 0.0, 0.0, 0.0), null)
				.AddNumberInput(inputBnds.RightCopy(40.0, 0.0, 0.0, 0.0), new Action<string>(this.OnOriginY), CairoFont.WhiteDetailText(), "originy")
				.AddStaticText("Translation Z", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0, 0.0, 0.0), null)
				.AddNumberInput(inputBnds = inputBnds.BelowCopy(0.0, 22.0, 0.0, 0.0), new Action<string>(this.OnTranslateZ), CairoFont.WhiteDetailText(), "translatez")
				.AddStaticText("Origin Z", CairoFont.WhiteDetailText(), line.RightCopy(40.0, 0.0, 0.0, 0.0), null)
				.AddNumberInput(inputBnds.RightCopy(40.0, 0.0, 0.0, 0.0), new Action<string>(this.OnOriginZ), CairoFont.WhiteDetailText(), "originz")
				.AddStaticText("Rotation X", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 33.0, 0.0, 0.0).WithFixedWidth(500.0), null)
				.AddSlider(new ActionConsumable<int>(this.OnRotateX), inputBnds = inputBnds.BelowCopy(0.0, 22.0, 0.0, 0.0).WithFixedWidth(500.0), "rotatex")
				.AddStaticText("Rotation Y", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.OnRotateY), inputBnds = inputBnds.BelowCopy(0.0, 22.0, 0.0, 0.0), "rotatey")
				.AddStaticText("Rotation Z", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.OnRotateZ), inputBnds = inputBnds.BelowCopy(0.0, 22.0, 0.0, 0.0), "rotatez")
				.AddStaticText("Scale", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 32.0, 0.0, 0.0), null)
				.AddSlider(new ActionConsumable<int>(this.OnScale), inputBnds = inputBnds.BelowCopy(0.0, 22.0, 0.0, 0.0), "scale")
				.AddSwitch(new Action<bool>(this.onFlipXAxis), inputBnds = inputBnds.BelowCopy(0.0, 10.0, 0.0, 0.0), "flipx", 20.0, 4.0)
				.AddStaticText("Flip on X-Axis", CairoFont.WhiteDetailText(), inputBnds.RightCopy(10.0, 1.0, 0.0, 0.0).WithFixedWidth(200.0), null)
				.AddStaticText("Json Code", CairoFont.WhiteDetailText(), line = line.BelowCopy(0.0, 72.0, 0.0, 0.0), null)
				.BeginClip(clippingBounds.FixedUnder(inputBnds, 37.0))
				.AddTextArea(textAreaBounds, null, CairoFont.WhiteSmallText(), "textarea")
				.EndClip()
				.AddSmallButton("Close & Apply", new ActionConsumable(this.OnApplyJson), btnBounds = btnBounds.FlatCopy().FixedUnder(clippingBounds, 15.0), EnumButtonStyle.Normal, null)
				.AddSmallButton("Copy JSON", new ActionConsumable(this.OnCopyJson), btnBounds = btnBounds.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndChildElements()
				.Compose(true);
			base.SingleComposer.GetTextInput("translatex").SetValue(this.currentTransform.Translation.X.ToString(GlobalConstants.DefaultCultureInfo), true);
			base.SingleComposer.GetTextInput("translatey").SetValue(this.currentTransform.Translation.Y.ToString(GlobalConstants.DefaultCultureInfo), true);
			base.SingleComposer.GetTextInput("translatez").SetValue(this.currentTransform.Translation.Z.ToString(GlobalConstants.DefaultCultureInfo), true);
			base.SingleComposer.GetTextInput("originx").SetValue(this.currentTransform.Origin.X.ToString(GlobalConstants.DefaultCultureInfo), true);
			base.SingleComposer.GetTextInput("originy").SetValue(this.currentTransform.Origin.Y.ToString(GlobalConstants.DefaultCultureInfo), true);
			base.SingleComposer.GetTextInput("originz").SetValue(this.currentTransform.Origin.Z.ToString(GlobalConstants.DefaultCultureInfo), true);
			base.SingleComposer.GetSlider("rotatex").SetValues((int)this.currentTransform.Rotation.X, -180, 180, 1, "");
			base.SingleComposer.GetSlider("rotatey").SetValues((int)this.currentTransform.Rotation.Y, -180, 180, 1, "");
			base.SingleComposer.GetSlider("rotatez").SetValues((int)this.currentTransform.Rotation.Z, -180, 180, 1, "");
			base.SingleComposer.GetSlider("scale").SetValues((int)Math.Abs(100f * this.currentTransform.ScaleXYZ.X), 25, 600, 1, "");
			base.SingleComposer.GetSwitch("flipx").On = this.currentTransform.ScaleXYZ.X < 0f;
			base.SingleComposer.GetVerticalTab("verticalTabs").SetValue(tabs.IndexOf((GuiTab tab) => tab.DataInt == this.target), false);
		}

		private void onFlipXAxis(bool on)
		{
			ModelTransform modelTransform = this.currentTransform;
			modelTransform.ScaleXYZ.X = modelTransform.ScaleXYZ.X * -1f;
			this.updateJson();
		}

		private void OnOriginX(string val)
		{
			this.currentTransform.Origin.X = val.ToFloat(0f);
			this.updateJson();
		}

		private void OnOriginY(string val)
		{
			this.currentTransform.Origin.Y = val.ToFloat(0f);
			this.updateJson();
		}

		private void OnOriginZ(string val)
		{
			this.currentTransform.Origin.Z = val.ToFloat(0f);
			this.updateJson();
		}

		private void OnTabClicked(int index, GuiTab tab)
		{
			this.TargetTransform = this.originalTransform;
			this.target = tab.DataInt;
			this.OnGuiOpened();
		}

		private bool OnApplyJson()
		{
			this.TargetTransform = (this.originalTransform = this.currentTransform);
			this.currentTransform = null;
			this.capi.Event.PushEvent("onapplytransforms", null);
			this.TryClose();
			return true;
		}

		private bool OnCopyJson()
		{
			ScreenManager.Platform.XPlatInterface.SetClipboardText(this.getJson());
			return true;
		}

		private void updateJson()
		{
			if (this.target >= 5)
			{
				this.TargetTransform = this.currentTransform;
			}
			base.SingleComposer.GetTextArea("textarea").SetValue(this.getJson(), true);
		}

		private string getJson()
		{
			StringBuilder json = new StringBuilder();
			ModelTransform def = new ModelTransform();
			string indent = "\t\t";
			switch (this.target)
			{
			case 0:
				json.Append("\tguiTransform: {\n");
				def = ((this.oldCollectible is Block) ? ModelTransform.BlockDefaultGui() : ModelTransform.ItemDefaultGui());
				break;
			case 2:
				json.Append("\ttpHandTransform: {\n");
				def = ((this.oldCollectible is Block) ? ModelTransform.BlockDefaultTp() : ModelTransform.ItemDefaultTp());
				break;
			case 3:
				json.Append("\ttpOffHandTransform: {\n");
				def = ((this.oldCollectible is Block) ? ModelTransform.BlockDefaultTp() : ModelTransform.ItemDefaultTp());
				break;
			case 4:
				json.Append("\tgroundTransform: {\n");
				def = ((this.oldCollectible is Block) ? ModelTransform.BlockDefaultFp() : ModelTransform.ItemDefaultFp());
				break;
			}
			if (this.target >= 5 && GuiDialogTransformEditor.extraTransforms.Count >= this.target - 5)
			{
				TransformConfig extraTf = GuiDialogTransformEditor.extraTransforms[this.target - 5];
				json.Append("\tattributes: {\n");
				json.Append("\t\t" + extraTf.AttributeName + ": {\n");
				indent = "\t\t\t";
				def = new ModelTransform().EnsureDefaultValues();
			}
			bool added = false;
			if (this.currentTransform.Translation.X != def.Translation.X || this.currentTransform.Translation.Y != def.Translation.Y || this.currentTransform.Translation.Z != def.Translation.Z)
			{
				json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "translation: {{ x: {0}, y: {1}, z: {2} }}", this.currentTransform.Translation.X, this.currentTransform.Translation.Y, this.currentTransform.Translation.Z));
				added = true;
			}
			if (this.currentTransform.Rotation.X != def.Rotation.X || this.currentTransform.Rotation.Y != def.Rotation.Y || this.currentTransform.Rotation.Z != def.Rotation.Z)
			{
				if (added)
				{
					json.Append(",\n");
				}
				json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "rotation: {{ x: {0}, y: {1}, z: {2} }}", this.currentTransform.Rotation.X, this.currentTransform.Rotation.Y, this.currentTransform.Rotation.Z));
				added = true;
			}
			if (this.currentTransform.Origin.X != def.Origin.X || this.currentTransform.Origin.Y != def.Origin.Y || this.currentTransform.Origin.Z != def.Origin.Z)
			{
				if (added)
				{
					json.Append(",\n");
				}
				json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "origin: {{ x: {0}, y: {1}, z: {2} }}", this.currentTransform.Origin.X, this.currentTransform.Origin.Y, this.currentTransform.Origin.Z));
				added = true;
			}
			if (this.currentTransform.ScaleXYZ.X != def.ScaleXYZ.X)
			{
				if (added)
				{
					json.Append(",\n");
				}
				if (this.currentTransform.ScaleXYZ.X != this.currentTransform.ScaleXYZ.Y || this.currentTransform.ScaleXYZ.X != this.currentTransform.ScaleXYZ.Z)
				{
					json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "scaleXyz: {{ x: {0}, y: {1}, z: {2} }}", this.currentTransform.ScaleXYZ.X, this.currentTransform.ScaleXYZ.Y, this.currentTransform.ScaleXYZ.Z));
				}
				else
				{
					json.Append(string.Format(GlobalConstants.DefaultCultureInfo, indent + "scale: {0}", this.currentTransform.ScaleXYZ.X));
				}
			}
			if (this.target >= 5)
			{
				json.Append("\n\t\t}");
			}
			json.Append("\n\t}");
			string jsonstr = json.ToString();
			TreeAttribute tree = new TreeAttribute();
			tree.SetString("json", jsonstr);
			this.capi.Event.PushEvent("genjsontransform", tree);
			return tree.GetString("json", null);
		}

		private bool OnScale(int val)
		{
			this.currentTransform.Scale = (float)val / 100f;
			if (base.SingleComposer.GetSwitch("flipx").On)
			{
				ModelTransform modelTransform = this.currentTransform;
				modelTransform.ScaleXYZ.X = modelTransform.ScaleXYZ.X * -1f;
			}
			this.updateJson();
			return true;
		}

		private bool OnRotateX(int deg)
		{
			this.currentTransform.Rotation.X = (float)deg;
			this.updateJson();
			return true;
		}

		private bool OnRotateY(int deg)
		{
			this.currentTransform.Rotation.Y = (float)deg;
			this.updateJson();
			return true;
		}

		private bool OnRotateZ(int deg)
		{
			this.currentTransform.Rotation.Z = (float)deg;
			this.updateJson();
			return true;
		}

		private void OnTranslateX(string val)
		{
			this.currentTransform.Translation.X = val.ToFloat(0f);
			this.updateJson();
		}

		private void OnTranslateY(string val)
		{
			this.currentTransform.Translation.Y = val.ToFloat(0f);
			this.updateJson();
		}

		private void OnTranslateZ(string val)
		{
			this.currentTransform.Translation.Z = val.ToFloat(0f);
			this.updateJson();
		}

		private void OnTitleBarClose()
		{
			this.TryClose();
		}

		public override void OnGuiClosed()
		{
			base.OnGuiClosed();
			if (this.oldCollectible != null)
			{
				this.TargetTransform = this.originalTransform;
			}
			this.capi.Event.PushEvent("oncloseedittransforms", null);
		}

		public override void OnMouseWheel(MouseWheelEventArgs args)
		{
			base.OnMouseWheel(args);
			args.SetHandled(true);
		}

		public override bool PrefersUngrabbedMouse
		{
			get
			{
				return true;
			}
		}

		private ModelTransform originalTransform;

		private CollectibleObject oldCollectible;

		private ModelTransform currentTransform = new ModelTransform();

		private int target = 2;

		public static List<TransformConfig> extraTransforms = new List<TransformConfig>
		{
			new TransformConfig
			{
				AttributeName = "toolrackTransform",
				Title = "On Tool rack"
			},
			new TransformConfig
			{
				AttributeName = "onmoldrackTransform",
				Title = "On vertical rack"
			},
			new TransformConfig
			{
				AttributeName = "onDisplayTransform",
				Title = "In display case"
			},
			new TransformConfig
			{
				AttributeName = "groundStorageTransform",
				Title = "Placed on ground"
			},
			new TransformConfig
			{
				AttributeName = "onshelfTransform",
				Title = "On shelf"
			},
			new TransformConfig
			{
				AttributeName = "onscrollrackTransform",
				Title = "On scroll rack"
			},
			new TransformConfig
			{
				AttributeName = "onAntlerMountTransform",
				Title = "On antler mount"
			},
			new TransformConfig
			{
				AttributeName = "onTongTransform",
				Title = "On tongs"
			},
			new TransformConfig
			{
				AttributeName = "inTrapTransform",
				Title = "In traps"
			},
			new TransformConfig
			{
				AttributeName = "inForgeTransform",
				Title = "In forge"
			}
		};
	}
}
