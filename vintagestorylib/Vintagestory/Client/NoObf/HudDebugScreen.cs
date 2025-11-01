using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Client.NoObf
{
	public class HudDebugScreen : HudElement
	{
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		public HudDebugScreen(ICoreClientAPI capi)
			: base(capi)
		{
			this.displayFullDebugInfo = false;
			this.dtHistory = new float[300];
			for (int i = 0; i < 300; i++)
			{
				this.dtHistory[i] = 0f;
			}
			this.GenFrameSlicesMesh();
			CairoFont font = CairoFont.WhiteDetailText();
			this.fpsLabels = new LoadedTexture[]
			{
				capi.Gui.TextTexture.GenUnscaledTextTexture("30", font, null),
				capi.Gui.TextTexture.GenUnscaledTextTexture("60", font, null),
				capi.Gui.TextTexture.GenUnscaledTextTexture("75", font, null),
				capi.Gui.TextTexture.GenUnscaledTextTexture("150", font, null)
			};
			IChatCommand chatCommand = capi.ChatCommands.GetOrCreate("debug").BeginSubCommand("edi").RequiresPrivilege(Privilege.chat)
				.WithRootAlias("edi")
				.WithDescription("Show/Hide Extended information on debug screen");
			OnCommandDelegate onCommandDelegate;
			if ((onCommandDelegate = HudDebugScreen.<>O.<0>__ToggleExtendedDebugInfo) == null)
			{
				onCommandDelegate = (HudDebugScreen.<>O.<0>__ToggleExtendedDebugInfo = new OnCommandDelegate(HudDebugScreen.ToggleExtendedDebugInfo));
			}
			chatCommand.HandleWith(onCommandDelegate).EndSubCommand();
			capi.Event.RegisterGameTickListener(new Action<float>(this.EveryOtherSecond), 2000, 0);
			capi.Event.RegisterEventBusListener(delegate(string name, ref EnumHandling handling, IAttribute data)
			{
				this.displayOnlyFpsDebugInfoTemporary = false;
				if (!this.displayFullDebugInfo && !this.displayOnlyFpsDebugInfo)
				{
					this.TryClose();
				}
			}, 0.5, "leftGraphicsDlg");
			capi.Event.RegisterEventBusListener(delegate(string name, ref EnumHandling handling, IAttribute data)
			{
				this.displayOnlyFpsDebugInfoTemporary = true;
				this.TryOpen();
			}, 0.5, "enteredGraphicsDlg");
			this.debugTextComposer = capi.Gui.CreateCompo("debugScreenText", ElementBounds.Percentual(EnumDialogArea.RightTop, 0.5, 0.7).WithFixedAlignmentOffset(-5.0, 5.0)).AddDynamicText("", CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Right), ElementBounds.Fill, "debugScreenTextElem").OnlyDynamic()
				.Compose(true);
			this.systemInfoComposer = capi.Gui.CreateCompo("sysInfoText", ElementBounds.Percentual(EnumDialogArea.LeftTop, 0.5, 0.7).WithFixedAlignmentOffset(5.0, 5.0)).AddDynamicText(string.Concat(new string[]
			{
				"Game Version: ",
				GameVersion.LongGameVersion,
				"\n",
				ScreenManager.Platform.GetGraphicCardInfos(),
				"\n",
				ScreenManager.Platform.GetFrameworkInfos()
			}), CairoFont.WhiteSmallishText(), ElementBounds.Fill, null).OnlyDynamic()
				.Compose(true);
			this.textElement = this.debugTextComposer.GetDynamicText("debugScreenTextElem");
			ScreenManager.hotkeyManager.SetHotKeyHandler("fpsgraph", new ActionConsumable<KeyCombination>(this.OnKeyGraph), true);
			ScreenManager.hotkeyManager.SetHotKeyHandler("debugscreenandgraph", new ActionConsumable<KeyCombination>(this.OnKeyDebugScreenAndGraph), true);
		}

		private void EveryOtherSecond(float dt)
		{
			this.GenFrameSlicesMesh();
		}

		private static TextCommandResult ToggleExtendedDebugInfo(TextCommandCallingArgs textCommandCallingArgs)
		{
			ClientSettings.ExtendedDebugInfo = !ClientSettings.ExtendedDebugInfo;
			return TextCommandResult.Success("Extended debug info " + (ClientSettings.ExtendedDebugInfo ? "on" : "off"), null);
		}

		public override void OnFinalizeFrame(float dt)
		{
			this.UpdateGraph(dt);
			this.UpdateText(dt);
			this.debugTextComposer.PostRender(dt);
		}

		public override void OnRenderGUI(float deltaTime)
		{
			if (this.displayOnlyFpsDebugInfo || this.displayFullDebugInfo || this.displayOnlyFpsDebugInfoTemporary)
			{
				this.DrawGraph();
				this.debugTextComposer.Render(deltaTime);
			}
			if (this.displayFullDebugInfo)
			{
				this.systemInfoComposer.Render(deltaTime);
			}
		}

		private void UpdateText(float dt)
		{
			this.frameCount++;
			this.longestframedt = Math.Max(this.longestframedt, dt);
			float seconds = (float)((this.capi.ElapsedMilliseconds - this.lastUpdateMilliseconds) / 1000L);
			if (seconds >= 1f && (this.displayFullDebugInfo || this.displayOnlyFpsDebugInfo || this.displayOnlyFpsDebugInfoTemporary))
			{
				this.lastUpdateMilliseconds = this.capi.ElapsedMilliseconds;
				ClientMain game = this.capi.World as ClientMain;
				string fpstext = this.GetFpsText(seconds);
				RuntimeStats.drawCallsCount = 0;
				this.longestframedt = 0f;
				this.frameCount = 0;
				if (!this.displayFullDebugInfo)
				{
					this.textElement.SetNewTextAsync(fpstext, false, false);
					return;
				}
				OrderedDictionary<string, string> perfInfo = game.DebugScreenInfo;
				string managed = decimal.Round((decimal)((float)GC.GetTotalMemory(false) / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
				if (this._process == null)
				{
					this._process = Process.GetCurrentProcess();
				}
				this._process.Refresh();
				string total = decimal.Round((decimal)((float)this._process.WorkingSet64 / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
				if (ClientSettings.GlDebugMode)
				{
					fpstext += " (gl debug mode enabled!)";
				}
				if (game.extendedDebugInfo)
				{
					fpstext += " (edi enabled!)";
				}
				game.DebugScreenInfo["fps"] = fpstext;
				game.DebugScreenInfo["mem"] = string.Concat(new string[] { "CPU Mem Managed/Total: ", managed, " / ", total, " MB" });
				game.DebugScreenInfo["entitycount"] = "entities: " + RuntimeStats.renderedEntities.ToString() + " / " + game.LoadedEntities.Count.ToString();
				bool allowCoordinateHud = this.capi.World.Config.GetBool("allowCoordinateHud", true);
				if (game.EntityPlayer != null)
				{
					EntityPos pos = game.EntityPlayer.Pos;
					if (!allowCoordinateHud)
					{
						perfInfo["position"] = "(disabled)";
						perfInfo["chunkpos"] = "(disabled)";
						perfInfo["regpos"] = "(disabled)";
					}
					else
					{
						perfInfo["position"] = "Position: " + pos.OnlyPosToString() + ((pos.Dimension > 0) ? (", dim " + pos.Dimension.ToString()) : "");
						perfInfo["chunkpos"] = string.Concat(new string[]
						{
							"Chunk: ",
							((int)(pos.X / (double)game.WorldMap.ClientChunkSize)).ToString(),
							", ",
							((int)(pos.Y / (double)game.WorldMap.ClientChunkSize)).ToString(),
							", ",
							((int)(pos.Z / (double)game.WorldMap.ClientChunkSize)).ToString()
						});
						perfInfo["regpos"] = "Region: " + ((int)(pos.X / (double)game.WorldMap.RegionSize)).ToString() + ", " + ((int)(pos.Z / (double)game.WorldMap.RegionSize)).ToString();
					}
					float yaw = GameMath.Mod(game.EntityPlayer.Pos.Yaw, 6.2831855f);
					float pitch = game.EntityPlayer.Pos.Pitch;
					OrderedDictionary<string, string> orderedDictionary = perfInfo;
					string text = "orientation";
					string[] array = new string[6];
					array[0] = "Pitch: ";
					array[1] = (pitch * 57.295776f).ToString();
					array[2] = ", Yaw: ";
					array[3] = (180f * yaw / 3.1415927f).ToString("#.##", GlobalConstants.DefaultCultureInfo);
					array[4] = " deg., Facing: ";
					int num = 5;
					BlockFacing blockFacing = BlockFacing.HorizontalFromYaw(yaw);
					array[num] = ((blockFacing != null) ? blockFacing.ToString() : null);
					orderedDictionary[text] = string.Concat(array);
				}
				if (game.BlockSelection != null)
				{
					BlockPos pos2 = game.BlockSelection.Position;
					Block solid = game.WorldMap.RelaxedBlockAccess.GetBlock(pos2, 1);
					Block fluid = game.WorldMap.RelaxedBlockAccess.GetBlock(pos2, 2);
					BlockEntity be = game.WorldMap.RelaxedBlockAccess.GetBlockEntity(game.BlockSelection.Position);
					string curBlock = string.Concat(new string[]
					{
						"Selected: ",
						solid.BlockId.ToString(),
						"/",
						solid.Code,
						" @",
						allowCoordinateHud ? pos2.ToString() : "(disabled)"
					});
					if (fluid.BlockId != 0)
					{
						curBlock = string.Concat(new string[]
						{
							curBlock,
							"\nFluids layer: ",
							fluid.BlockId.ToString(),
							"/",
							fluid.Code
						});
					}
					perfInfo["curblock"] = curBlock;
					OrderedDictionary<string, string> orderedDictionary2 = perfInfo;
					string text2 = "curblockentity";
					string text3 = "Selected BE: ";
					Type type = ((be != null) ? be.GetType() : null);
					orderedDictionary2[text2] = text3 + ((type != null) ? type.ToString() : null);
				}
				else
				{
					perfInfo["curblock"] = "";
					perfInfo["curblocklight"] = "";
				}
				if (game.extendedDebugInfo)
				{
					if (game.BlockSelection != null)
					{
						BlockPos pos3 = game.BlockSelection.Position.AddCopy(game.BlockSelection.Face);
						int[] hsvvalues = game.WorldMap.GetLightHSVLevels(pos3.X, pos3.InternalY, pos3.Z);
						perfInfo["curblocklight"] = string.Concat(new string[]
						{
							"FO: Sun V: ",
							hsvvalues[0].ToString(),
							", Block H: ",
							hsvvalues[2].ToString(),
							", Block S: ",
							hsvvalues[3].ToString(),
							", Block V: ",
							hsvvalues[1].ToString()
						});
						pos3 = game.BlockSelection.Position;
						hsvvalues = game.WorldMap.GetLightHSVLevels(pos3.X, pos3.InternalY, pos3.Z);
						perfInfo["curblocklight2"] = string.Concat(new string[]
						{
							"Sun V: ",
							hsvvalues[0].ToString(),
							", Block H: ",
							hsvvalues[2].ToString(),
							", Block S: ",
							hsvvalues[3].ToString(),
							", Block V: ",
							hsvvalues[1].ToString()
						});
					}
					perfInfo["tickstopwatch"] = game.tickSummary;
				}
				else
				{
					perfInfo["curblocklight"] = "";
					perfInfo["curblocklight2"] = "";
					perfInfo["tickstopwatch"] = "";
				}
				string newfpstext = "";
				foreach (string value in game.DebugScreenInfo.Values)
				{
					newfpstext = newfpstext + value + "\n";
				}
				this.textElement.SetNewTextAsync(newfpstext, false, false);
			}
		}

		private string GetFpsText(float seconds)
		{
			if (!this.displayFullDebugInfo)
			{
				return string.Format("Avg FPS: {0}, Min FPS: {1}", (int)(1f * (float)this.frameCount / seconds), (int)(1f / this.longestframedt));
			}
			return string.Format("Avg FPS: {0}, Min FPS: {1}, DCs: {2}", (int)(1f * (float)this.frameCount / seconds), (int)(1f / this.longestframedt), (int)((float)RuntimeStats.drawCallsCount / (1f * (float)this.frameCount / seconds)));
		}

		public override bool TryClose()
		{
			return false;
		}

		public void DoClose()
		{
			base.TryClose();
		}

		private bool OnKeyDebugScreenAndGraph(KeyCombination viaKeyComb)
		{
			if (this.displayFullDebugInfo)
			{
				this.displayFullDebugInfo = false;
				this.DoClose();
				return true;
			}
			this.displayFullDebugInfo = true;
			this.TryOpen();
			return true;
		}

		private bool OnKeyGraph(KeyCombination viaKeyComb)
		{
			if (this.displayFullDebugInfo)
			{
				return true;
			}
			if (this.displayOnlyFpsDebugInfo)
			{
				this.displayOnlyFpsDebugInfo = false;
				if (!this.displayOnlyFpsDebugInfoTemporary)
				{
					this.DoClose();
				}
				return true;
			}
			this.displayOnlyFpsDebugInfo = true;
			this.TryOpen();
			return true;
		}

		private void UpdateGraph(float dt)
		{
			for (int i = 0; i < 299; i++)
			{
				this.dtHistory[i] = this.dtHistory[i + 1];
			}
			this.dtHistory[299] = dt;
		}

		private void DrawGraph()
		{
			this.updateFrameSlicesMesh();
			ClientMain game = this.capi.World as ClientMain;
			int posx = game.Width - 310;
			int posy = game.Height - this.historyheight - 40;
			game.Platform.BindTexture2d(game.WhiteTexture());
			game.guiShaderProg.RgbaIn = new Vec4f(1f, 1f, 1f, 1f);
			game.guiShaderProg.ExtraGlow = 0;
			game.guiShaderProg.ApplyColor = 1;
			game.guiShaderProg.AlphaTest = 0f;
			game.guiShaderProg.DarkEdges = 0;
			game.guiShaderProg.NoTexture = 1f;
			game.guiShaderProg.Tex2d2D = game.WhiteTexture();
			game.guiShaderProg.ProjectionMatrix = game.CurrentProjectionMatrix;
			game.guiShaderProg.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(this.frameSlicesRef);
			game.Render2DTexture(game.WhiteTexture(), (float)posx, (float)(posy - this.historyheight), 300f, 1f, 10f, null);
			game.Render2DTexture(game.WhiteTexture(), (float)posx, (float)posy - (float)(this.historyheight * 60) * 0.013333334f, 300f, 1f, 10f, null);
			game.Render2DTexture(game.WhiteTexture(), (float)posx, (float)posy - (float)(this.historyheight * 60) * 0.033333335f, 300f, 1f, 10f, null);
			game.Render2DTexture(game.WhiteTexture(), (float)posx, (float)posy - (float)(this.historyheight * 60) * 0.006666667f, 300f, 1f, 10f, null);
			game.Platform.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
			game.Render2DLoadedTexture(this.fpsLabels[0], (float)posx, (float)posy - (float)(this.historyheight * 60) * 0.033333335f, 50f, null);
			game.Render2DLoadedTexture(this.fpsLabels[1], (float)posx, (float)posy - (float)(this.historyheight * 60) * 0.016666668f, 50f, null);
			game.Render2DLoadedTexture(this.fpsLabels[2], (float)posx, (float)posy - (float)(this.historyheight * 60) * 0.013333334f, 50f, null);
			game.Render2DLoadedTexture(this.fpsLabels[3], (float)posx, (float)posy - (float)(this.historyheight * 60) * 0.006666667f, 50f, null);
			game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
		}

		private void updateFrameSlicesMesh()
		{
			int posy = this.capi.Render.FrameHeight - this.historyheight - 40;
			for (int i = 0; i < 300; i++)
			{
				float frameTime = this.dtHistory[i];
				frameTime = frameTime * 60f * (float)this.historyheight;
				int vertIndex = i * 4 * 3;
				this.frameSlicesUpdate.xyz[vertIndex + 7] = (float)posy - frameTime;
				this.frameSlicesUpdate.xyz[vertIndex + 10] = (float)posy - frameTime;
			}
			this.capi.Render.UpdateMesh(this.frameSlicesRef, this.frameSlicesUpdate);
		}

		private void GenFrameSlicesMesh()
		{
			MeshData frameSlices = new MeshData(1200, 1800, false, true, true, false);
			int posx = this.capi.Render.FrameWidth - 310;
			int posy = this.capi.Render.FrameHeight - this.historyheight - 40;
			for (int i = 0; i < 300; i++)
			{
				byte r = (byte)(255f * (float)i / 300f);
				MeshData sliceMesh = QuadMeshUtilExt.GetCustomQuadModelData((float)(posx + i), (float)posy, 50f, 1f, 1f, r, 0, 0, byte.MaxValue, 0);
				frameSlices.AddMeshData(sliceMesh);
			}
			if (this.frameSlicesRef != null)
			{
				this.capi.Render.DeleteMesh(this.frameSlicesRef);
			}
			this.frameSlicesRef = this.capi.Render.UploadMesh(frameSlices);
			this.frameSlicesUpdate = frameSlices;
			this.frameSlicesUpdate.Rgba = null;
			this.frameSlicesUpdate.Indices = null;
		}

		public override bool Focusable
		{
			get
			{
				return false;
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			GuiComposer guiComposer = this.debugTextComposer;
			if (guiComposer != null)
			{
				guiComposer.Dispose();
			}
			GuiComposer guiComposer2 = this.systemInfoComposer;
			if (guiComposer2 != null)
			{
				guiComposer2.Dispose();
			}
			MeshRef meshRef = this.frameSlicesRef;
			if (meshRef != null)
			{
				meshRef.Dispose();
			}
			int i = 0;
			while (this.fpsLabels != null && i < this.fpsLabels.Length)
			{
				LoadedTexture loadedTexture = this.fpsLabels[i];
				if (loadedTexture != null)
				{
					loadedTexture.Dispose();
				}
				i++;
			}
			Process process = this._process;
			if (process == null)
			{
				return;
			}
			process.Dispose();
		}

		private long lastUpdateMilliseconds;

		private int frameCount;

		private float longestframedt;

		private GuiComposer debugTextComposer;

		private GuiComposer systemInfoComposer;

		private GuiElementDynamicText textElement;

		private float[] dtHistory;

		private const int QuantityRenderedFrameSlices = 300;

		private MeshData frameSlicesUpdate;

		private MeshRef frameSlicesRef;

		private bool displayFullDebugInfo;

		private bool displayOnlyFpsDebugInfo;

		private bool displayOnlyFpsDebugInfoTemporary;

		private LoadedTexture[] fpsLabels;

		private Process _process;

		private int historyheight = 80;

		[CompilerGenerated]
		private static class <>O
		{
			public static OnCommandDelegate <0>__ToggleExtendedDebugInfo;
		}
	}
}
