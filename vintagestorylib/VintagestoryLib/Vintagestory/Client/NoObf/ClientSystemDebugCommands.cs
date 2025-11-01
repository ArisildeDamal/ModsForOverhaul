using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf
{
	internal class ClientSystemDebugCommands : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "debmc";
			}
		}

		public ClientSystemDebugCommands(ClientMain game)
			: base(game)
		{
			IChatCommandApi chatCommands = game.api.ChatCommands;
			CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
			chatCommands.GetOrCreate("debug").WithDescription("Debug and Developer utilities").RequiresPrivilege(Privilege.controlserver)
				.BeginSubCommand("clobjc")
				.WithDescription("clobjc")
				.HandleWith(new OnCommandDelegate(this.OnCmdClobjc))
				.EndSubCommand()
				.BeginSubCommand("self")
				.WithDescription("self")
				.RequiresPrivilege(Privilege.chat)
				.HandleWith(new OnCommandDelegate(this.OnCmdSelfDebugInfo))
				.EndSubCommand()
				.BeginSubCommand("talk")
				.WithDescription("talk")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWordRange("talk", Enum.GetNames<EnumTalkType>()) })
				.HandleWith(new OnCommandDelegate(this.OnCmdTalk))
				.EndSubCommand()
				.BeginSubCommand("normalview")
				.WithDescription("normalview")
				.HandleWith(new OnCommandDelegate(this.OnCmdNormalview))
				.EndSubCommand()
				.BeginSubCommand("perceptioneffect")
				.WithAlias(new string[] { "pc" })
				.WithDescription("perceptioneffect")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.OptionalWord("effectname"),
					parsers.OptionalFloat("intensity", 1f)
				})
				.HandleWith(new OnCommandDelegate(this.OnCmdPerceptioneffect))
				.EndSubCommand()
				.BeginSubCommand("debdc")
				.WithDescription("debdc")
				.HandleWith(new OnCommandDelegate(this.OnCmdDebdc))
				.EndSubCommand()
				.BeginSubCommand("tofb")
				.WithDescription("tofb")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("enable", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdTofb))
				.EndSubCommand()
				.BeginSubCommand("cmr")
				.WithDescription("cmr")
				.HandleWith(new OnCommandDelegate(this.OnCmdCmr))
				.EndSubCommand()
				.BeginSubCommand("us")
				.WithDescription("us")
				.HandleWith(new OnCommandDelegate(this.OnCmdUs))
				.EndSubCommand()
				.BeginSubCommand("gl")
				.WithDescription("gl")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("GlDebugMode", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdGl))
				.EndSubCommand()
				.BeginSubCommand("plranims")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("plranims")
				.HandleWith(new OnCommandDelegate(this.OnCmdPlranims))
				.EndSubCommand()
				.BeginSubCommand("uiclick")
				.WithDescription("uiclick")
				.HandleWith(new OnCommandDelegate(this.OnCmdUiclick))
				.EndSubCommand()
				.BeginSubCommand("discovery")
				.WithDescription("discovery")
				.WithArgs(new ICommandArgumentParser[] { parsers.All("text") })
				.HandleWith(new OnCommandDelegate(this.OnCmdDiscovery))
				.EndSubCommand()
				.BeginSubCommand("soundsummary")
				.WithDescription("soundsummary")
				.HandleWith(new OnCommandDelegate(this.OnCmdSoundsummary))
				.EndSubCommand()
				.BeginSubCommand("meshsummary")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("meshsummary")
				.HandleWith(new OnCommandDelegate(this.OnCmdMeshsummary))
				.EndSubCommand()
				.BeginSubCommand("chunksummary")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("chunksummary")
				.HandleWith(new OnCommandDelegate(this.OnCmdChunksummary))
				.EndSubCommand()
				.BeginSubCommand("logticks")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("logticks")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("ticksThreshold", 40) })
				.HandleWith(new OnCommandDelegate(this.OnCmdLogticks))
				.EndSubCommand()
				.BeginSubCommand("renderers")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("renderers")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("print", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdRenderers))
				.EndSubCommand()
				.BeginSubCommand("exptexatlas")
				.WithDescription("exptexatlas")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWordRange("atlas", new string[] { "block", "item", "entity" }) })
				.HandleWith(new OnCommandDelegate(this.OnCmdExptexatlas))
				.EndSubCommand()
				.BeginSubCommand("liquidselectable")
				.WithDescription("liquidselectable")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("forceLiquidSelectable", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdLiquidselectable))
				.EndSubCommand()
				.BeginSubCommand("relightchunk")
				.WithDescription("relightchunk")
				.HandleWith(new OnCommandDelegate(this.OnCmdRelightchunk))
				.EndSubCommand()
				.BeginSubCommand("fog")
				.WithDescription("fog")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.OptionalFloat("density", 0f),
					parsers.OptionalFloat("min", 1f)
				})
				.HandleWith(new OnCommandDelegate(this.OnCmdFog))
				.EndSubCommand()
				.BeginSubCommand("fov")
				.WithDescription("fov")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("fov", 0) })
				.HandleWith(new OnCommandDelegate(this.OnCmdFov))
				.EndSubCommand()
				.BeginSubCommand("wgen")
				.WithDescription("wgen")
				.HandleWith(new OnCommandDelegate(this.OnWgenCommand))
				.EndSubCommand()
				.BeginSubCommand("redrawall")
				.WithDescription("redrawall")
				.HandleWith(new OnCommandDelegate(this.OnRedrawAll))
				.EndSubCommand()
				.BeginSubCommand("ci")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("ci")
				.HandleWith(new OnCommandDelegate(this.OnChunkInfo))
				.EndSubCommand()
				.BeginSubCommand("plrattr")
				.WithDescription("plrattr")
				.WithArgs(new ICommandArgumentParser[] { parsers.All("path") })
				.HandleWith(new OnCommandDelegate(this.OnCmdPlrattr))
				.EndSubCommand()
				.BeginSubCommand("crw")
				.WithDescription("crw")
				.HandleWith(new OnCommandDelegate(this.OnCmdCrw))
				.EndSubCommand()
				.BeginSubCommand("shake")
				.WithDescription("shake")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("strength", 0.5f) })
				.HandleWith(new OnCommandDelegate(this.OnCmdShake))
				.EndSubCommand()
				.BeginSubCommand("recalctrav")
				.WithDescription("recalctrav")
				.HandleWith(new OnCommandDelegate(this.OnCmdRecalctrav))
				.EndSubCommand()
				.BeginSubCommand("wireframe")
				.WithAlias(new string[] { "wf" })
				.WithDescription("View wireframes showing various game elements")
				.BeginSubCommand("scene")
				.WithDescription("GUI elements converted to wireframe triangles")
				.HandleWith(new OnCommandDelegate(this.OnCmdScene))
				.EndSubCommand()
				.BeginSubCommand("ambsounds")
				.WithDescription("Show the current sources of ambient sounds")
				.HandleWith(new OnCommandDelegate(this.OnCmdAmbsounds))
				.EndSubCommand()
				.BeginSubCommand("entity")
				.WithDescription("For every entity, the collision box (red) and selection box (blue)")
				.HandleWith(new OnCommandDelegate(this.OnCmdEntity))
				.EndSubCommand()
				.BeginSubCommand("chunk")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("The boundaries of the current chunk")
				.HandleWith(new OnCommandDelegate(this.OnCmdChunk))
				.EndSubCommand()
				.BeginSubCommand("inside")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("The block(s) the player is currently 'inside'")
				.HandleWith(new OnCommandDelegate(this.OnCmdInside))
				.EndSubCommand()
				.BeginSubCommand("serverchunk")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("The boundaries of the current serverchunk")
				.HandleWith(new OnCommandDelegate(this.OnCmdServerchunk))
				.EndSubCommand()
				.BeginSubCommand("region")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("The boundaries of the current MapRegion")
				.HandleWith(new OnCommandDelegate(this.OnCmdRegion))
				.EndSubCommand()
				.BeginSubCommand("blockentity")
				.WithDescription("All the BlockEntities")
				.HandleWith(new OnCommandDelegate(this.OnCmdBlockentity))
				.EndSubCommand()
				.BeginSubCommand("landclaim")
				.WithDescription("All the LandClaims in the current Map region")
				.HandleWith(new OnCommandDelegate(this.OnCmdLandClaim))
				.EndSubCommand()
				.BeginSubCommand("structures")
				.WithDescription("All the Structures in the current mapregion")
				.HandleWith(new OnCommandDelegate(this.OnCmdStructure))
				.EndSubCommand()
				.BeginSubCommand("smoothstep")
				.RequiresPrivilege(Privilege.chat)
				.WithDescription("The wireframe used for smooth-stepping feature")
				.HandleWith(new OnCommandDelegate(this.OnCmdSmoothStep))
				.EndSubCommand()
				.EndSubCommand()
				.BeginSubCommand("find")
				.WithDescription("find")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("searchString") })
				.HandleWith(new OnCommandDelegate(this.OnCmdFind))
				.EndSubCommand()
				.BeginSubCommand("dumpanimstate")
				.WithDescription("Dump animation state into log file")
				.WithArgs(new ICommandArgumentParser[] { parsers.Entities("target entity") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => this.handleDumpAnimState(e, args), 0))
				.EndSubCommand();
		}

		private TextCommandResult handleDumpAnimState(Entity e, TextCommandCallingArgs args)
		{
			ILogger logger = this.game.Logger;
			IAnimationManager animManager = e.AnimManager;
			string text;
			if (animManager == null)
			{
				text = null;
			}
			else
			{
				IAnimator animator = animManager.Animator;
				text = ((animator != null) ? animator.DumpCurrentState() : null);
			}
			logger.Notification(text);
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdSelfDebugInfo(TextCommandCallingArgs args)
		{
			this.game.EntityPlayer.UpdateDebugAttributes();
			StringBuilder text = new StringBuilder();
			foreach (KeyValuePair<string, IAttribute> val in this.game.EntityPlayer.DebugAttributes)
			{
				text.AppendLine(val.Key + ": " + val.Value.ToString());
			}
			return TextCommandResult.Success(text.ToString(), null);
		}

		private TextCommandResult OnCmdFind(TextCommandCallingArgs args)
		{
			if (this.game.EntityPlayer.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				return TextCommandResult.Success(Lang.Get("Need to be in Creative mode to use the command .debug find [blockname]", Array.Empty<object>()), null);
			}
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success(Lang.Get("Specify all or part of the name of a block to find", Array.Empty<object>()), null);
			}
			this.game.FindCmd(args[0] as string);
			return TextCommandResult.Success("", null);
		}

		private WireframeModes wfmodes
		{
			get
			{
				return this.game.api.renderapi.WireframeDebugRender;
			}
		}

		private TextCommandResult OnCmdBlockentity(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.BlockEntity, "Block entity wireframes");
		}

		private TextCommandResult OnCmdStructure(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.Structures, "Structure wireframes");
		}

		private TextCommandResult OnCmdRegion(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.Region, "Region wireframe");
		}

		private TextCommandResult OnCmdLandClaim(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.LandClaim, "Land claim wireframe");
		}

		private TextCommandResult OnCmdServerchunk(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.ServerChunk, "Server chunk wireframe");
		}

		private TextCommandResult OnCmdChunk(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.Chunk, "Chunk wireframe");
		}

		private TextCommandResult OnCmdEntity(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.Entity, "Entity wireframes");
		}

		private TextCommandResult OnCmdInside(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.Inside, "Inside block wireframe");
		}

		private TextCommandResult OnCmdSmoothStep(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.Smoothstep, "Smooth-stepping wireframe");
		}

		private TextCommandResult OnCmdAmbsounds(TextCommandCallingArgs args)
		{
			return this.WireframeCommon(ref this.wfmodes.AmbientSounds, "Ambient sounds wireframes");
		}

		private TextCommandResult WireframeCommon(ref bool toggle, string name)
		{
			toggle = !toggle;
			return TextCommandResult.Success(Lang.Get(name + " now {0}", new object[] { toggle ? Lang.Get("on", Array.Empty<object>()) : Lang.Get("off", Array.Empty<object>()) }), null);
		}

		private TextCommandResult OnCmdScene(TextCommandCallingArgs args)
		{
			this.game.Platform.GLWireframes(this.wfmodes.Vertex = !this.wfmodes.Vertex);
			return TextCommandResult.Success(Lang.Get("Scene wireframes now {0}", new object[] { this.wfmodes.Vertex ? Lang.Get("on", Array.Empty<object>()) : Lang.Get("off", Array.Empty<object>()) }), null);
		}

		private TextCommandResult OnCmdRecalctrav(TextCommandCallingArgs args)
		{
			foreach (KeyValuePair<long, ClientChunk> val in this.game.WorldMap.chunks)
			{
				ChunkPos vec = this.game.WorldMap.ChunkPosFromChunkIndex3D(val.Key);
				if (vec.Dimension == 0)
				{
					object chunkPositionsLock = this.game.chunkPositionsLock;
					lock (chunkPositionsLock)
					{
						this.game.chunkPositionsForRegenTrav.Add(vec);
					}
				}
			}
			return TextCommandResult.Success("Ok queued all chunks to recalc their traverseability", null);
		}

		private TextCommandResult OnCmdCrw(TextCommandCallingArgs args)
		{
			BlockPos pos = this.game.EntityPlayer.Pos.AsBlockPos;
			this.game.WorldMap.MarkChunkDirty(pos.X / 32, pos.Y / 32, pos.Z / 32, false, false, null, true, false);
			return TextCommandResult.Success("Ok, chunk marked dirty for redraw", null);
		}

		private TextCommandResult OnCmdPlrattr(TextCommandCallingArgs args)
		{
			string path = args[0] as string;
			IAttribute attr = this.game.EntityPlayer.WatchedAttributes.GetAttributeByPath(path);
			if (attr == null)
			{
				return TextCommandResult.Success("No such path found", null);
			}
			return TextCommandResult.Success(Lang.Get("Value is: {0}", new object[] { attr.GetValue() }), null);
		}

		private TextCommandResult OnCmdRenderers(TextCommandCallingArgs args)
		{
			if (this.game.eventManager == null)
			{
				return TextCommandResult.Error("Client already shutting down", "");
			}
			List<RenderHandler>[] renderers = this.game.eventManager.renderersByStage;
			StringBuilder sb = new StringBuilder();
			bool print = (bool)args[0];
			Dictionary<string, int> rendererSummary = new Dictionary<string, int>();
			for (int i = 0; i < renderers.Length; i++)
			{
				EnumRenderStage stage = (EnumRenderStage)i;
				sb.AppendLine(stage.ToString() + ": " + renderers[i].Count.ToString());
				if (print)
				{
					foreach (RenderHandler renderHandler in renderers[i])
					{
						Type type = renderHandler.Renderer.GetType();
						string key = ((type != null) ? type.ToString() : null) ?? "";
						if (rendererSummary.ContainsKey(key))
						{
							Dictionary<string, int> dictionary = rendererSummary;
							string text = key;
							int num = dictionary[text];
							dictionary[text] = num + 1;
						}
						else
						{
							rendererSummary[key] = 1;
						}
					}
				}
			}
			this.game.ShowChatMessage("Renderers:");
			this.game.ShowChatMessage(sb.ToString());
			if (print)
			{
				this.game.Logger.Notification("Renderer summary:");
				foreach (KeyValuePair<string, int> val in rendererSummary)
				{
					this.game.Logger.Notification(val.Value.ToString() + "x " + val.Key);
				}
				this.game.ShowChatMessage("Summary printed to client log file");
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdLogticks(TextCommandCallingArgs args)
		{
			ScreenManager.FrameProfiler.PrintSlowTicks = !ScreenManager.FrameProfiler.PrintSlowTicks;
			ScreenManager.FrameProfiler.Enabled = ScreenManager.FrameProfiler.PrintSlowTicks;
			ScreenManager.FrameProfiler.PrintSlowTicksThreshold = (int)args[0];
			ScreenManager.FrameProfiler.Begin(null, Array.Empty<object>());
			this.game.ShowChatMessage("Client Tick Profiling now " + (ScreenManager.FrameProfiler.PrintSlowTicks ? ("on, threshold " + ScreenManager.FrameProfiler.PrintSlowTicksThreshold.ToString() + " ms") : "off"));
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdChunksummary(TextCommandCallingArgs args)
		{
			int total = 0;
			int packed = 0;
			int cntData = 0;
			int cntEmpty = 0;
			foreach (KeyValuePair<long, ClientChunk> val in this.game.WorldMap.chunks)
			{
				total++;
				if (val.Value.IsPacked())
				{
					packed++;
				}
				if (val.Value.Empty)
				{
					cntEmpty++;
				}
				else
				{
					cntData++;
				}
			}
			this.game.ShowChatMessage(string.Format("{0} Total chunks ({1} with data and {2} empty)\n{3} of which are packed", new object[] { total, cntData, cntEmpty, packed }));
			ClientChunkDataPool pool = this.game.WorldMap.chunkDataPool;
			this.game.ShowChatMessage(string.Format("Free pool objects {0}", pool.CountFree()));
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdMeshsummary(TextCommandCallingArgs args)
		{
			Dictionary<string, int> grouped = new Dictionary<string, int>();
			this.game.Logger.Debug("==== Mesh summary ====");
			int meshCount = 0;
			MeshData[] blockModelData = this.game.TesselatorManager.blockModelDatas;
			for (int blockid = 0; blockid < blockModelData.Length; blockid++)
			{
				MeshData mesh = blockModelData[blockid];
				if (mesh != null)
				{
					meshCount++;
					Block block = this.game.Blocks[blockid];
					int size = mesh.SizeInBytes();
					int sizeSum;
					grouped.TryGetValue(block.FirstCodePart(0), out sizeSum);
					sizeSum += size;
					MeshData[] meshes = this.game.TesselatorManager.altblockModelDatasLod1[blockid];
					int i = 0;
					while (meshes != null && i < meshes.Length)
					{
						MeshData altmesh = meshes[i];
						if (altmesh != null)
						{
							sizeSum += altmesh.SizeInBytes();
						}
						i++;
					}
					MeshData[][] altblockModelDatasLod = this.game.TesselatorManager.altblockModelDatasLod0;
					meshes = ((altblockModelDatasLod != null) ? altblockModelDatasLod[blockid] : null);
					int j = 0;
					while (meshes != null && j < meshes.Length)
					{
						MeshData altmesh2 = meshes[j];
						if (altmesh2 != null)
						{
							sizeSum += altmesh2.SizeInBytes();
						}
						j++;
					}
					MeshData[][] altblockModelDatasLod2 = this.game.TesselatorManager.altblockModelDatasLod2;
					meshes = ((altblockModelDatasLod2 != null) ? altblockModelDatasLod2[blockid] : null);
					int k = 0;
					while (meshes != null && k < meshes.Length)
					{
						MeshData altmesh3 = meshes[k];
						if (altmesh3 != null)
						{
							sizeSum += altmesh3.SizeInBytes();
						}
						k++;
					}
					grouped[block.FirstCodePart(0)] = sizeSum;
				}
			}
			int totalKb = 0;
			foreach (KeyValuePair<string, int> val in grouped)
			{
				int usedKb = val.Value / 1024;
				totalKb += usedKb;
				if (usedKb > 99)
				{
					this.game.Logger.Debug("{0}: {1} kB", new object[] { val.Key, usedKb });
				}
			}
			string result = string.Format("{0} of {1} meshes loaded, using {2} kB", meshCount, blockModelData.Length, totalKb);
			this.game.Logger.Debug("   " + result);
			return TextCommandResult.Success(result, null);
		}

		private TextCommandResult OnCmdSoundsummary(TextCommandCallingArgs args)
		{
			int loaded = 0;
			int total = ScreenManager.soundAudioData.Count;
			int memKb = 0;
			this.game.Logger.Debug("==== Sound summary ====");
			foreach (KeyValuePair<AssetLocation, AudioData> val in ScreenManager.soundAudioData)
			{
				if (val.Value.Loaded > 1)
				{
					loaded++;
					int kbUsed = (val.Value as AudioMetaData).Pcm.Length / 1024;
					memKb += kbUsed;
					if (kbUsed > 99)
					{
						this.game.Logger.Debug("{0}: {1} kB", new object[] { val.Key, kbUsed });
					}
				}
			}
			string result = string.Format("{0} of {1} sounds loaded, using {2} kB", loaded, total, memKb);
			this.game.Logger.Debug("   " + result);
			return TextCommandResult.Success(result, null);
		}

		private TextCommandResult OnCmdDiscovery(TextCommandCallingArgs args)
		{
			string text = args[0] as string;
			ClientEventManager eventManager = this.game.eventManager;
			if (eventManager != null)
			{
				eventManager.TriggerIngameDiscovery(this, "no", text);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdUiclick(TextCommandCallingArgs args)
		{
			GuiManager.DEBUG_PRINT_INTERACTIONS = !GuiManager.DEBUG_PRINT_INTERACTIONS;
			return TextCommandResult.Success("UI Debug pring interactions now " + (GuiManager.DEBUG_PRINT_INTERACTIONS ? "on" : "off"), null);
		}

		private TextCommandResult OnCmdPlranims(TextCommandCallingArgs args)
		{
			IAnimationManager AnimManager = this.game.player.Entity.AnimManager;
			string anims = "";
			int i = 0;
			foreach (string anim in AnimManager.ActiveAnimationsByAnimCode.Keys)
			{
				if (i++ > 0)
				{
					anims += ",";
				}
				anims += anim;
			}
			i = 0;
			StringBuilder runninganims = new StringBuilder();
			foreach (RunningAnimation anim2 in AnimManager.Animator.Animations)
			{
				if (anim2.Active)
				{
					if (i++ > 0)
					{
						runninganims.Append(",");
					}
					runninganims.Append(anim2.Animation.Code);
				}
			}
			this.game.ShowChatMessage("Active Animations: " + ((anims.Length > 0) ? anims : "-"));
			return TextCommandResult.Success("Running Animations: " + ((runninganims.Length > 0) ? runninganims.ToString() : "-"), null);
		}

		private TextCommandResult OnCmdGl(TextCommandCallingArgs args)
		{
			ScreenManager.Platform.GlDebugMode = (bool)args[0];
			return TextCommandResult.Success("OpenGL debug mode now " + (ScreenManager.Platform.GlDebugMode ? "on" : "off"), null);
		}

		private TextCommandResult OnCmdUs(TextCommandCallingArgs args)
		{
			this.game.unbindSamplers = !this.game.unbindSamplers;
			return TextCommandResult.Success("Unpind samplers mode now " + (this.game.unbindSamplers ? "on" : "off"), null);
		}

		private TextCommandResult OnCmdCmr(TextCommandCallingArgs args)
		{
			float[] arr = this.game.shUniforms.ColorMapRects4;
			for (int i = 0; i < arr.Length; i += 4)
			{
				this.game.Logger.Notification("x: {0}, y: {1}, w: {2}, h: {3}", new object[]
				{
					arr[i],
					arr[i + 1],
					arr[i + 2],
					arr[i + 3]
				});
			}
			return TextCommandResult.Success("Color map rects printed to client-main.log", null);
		}

		private TextCommandResult OnCmdTofb(TextCommandCallingArgs args)
		{
			ScreenManager.Platform.ToggleOffscreenBuffer((bool)args[0]);
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdDebdc(TextCommandCallingArgs args)
		{
			ScreenManager.debugDrawCallNextFrame = true;
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdPerceptioneffect(TextCommandCallingArgs args)
		{
			PerceptionEffects pcReg = this.game.api.Render.PerceptionEffects;
			if (args.Parsers[0].IsMissing)
			{
				StringBuilder sbe = new StringBuilder();
				sbe.Append("Missing effect name argument. Available: ");
				int i = 0;
				foreach (string vap in pcReg.RegisteredEffects)
				{
					if (i > 0)
					{
						sbe.Append(", ");
					}
					i++;
					sbe.Append(vap);
				}
				return TextCommandResult.Success(sbe.ToString(), null);
			}
			string effectname = args[0] as string;
			if (pcReg.RegisteredEffects.Contains(effectname))
			{
				pcReg.TriggerEffect(effectname, (float)args[1], null);
				return TextCommandResult.Success("", null);
			}
			return TextCommandResult.Success("No such effect registered.", null);
		}

		private TextCommandResult OnCmdNormalview(TextCommandCallingArgs args)
		{
			ShaderRegistry.NormalView = !ShaderRegistry.NormalView;
			bool ok = ShaderRegistry.ReloadShaders();
			bool ok2 = this.game.eventManager != null && this.game.eventManager.TriggerReloadShaders();
			ok = ok && ok2;
			return TextCommandResult.Success("Shaders reloaded" + (ok ? "" : ". errors occured, please check client log"), null);
		}

		private TextCommandResult OnCmdTalk(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				StringBuilder sbt = new StringBuilder();
				foreach (object talktype in Enum.GetValues(typeof(EnumTalkType)))
				{
					if (sbt.Length > 0)
					{
						sbt.Append(", ");
					}
					sbt.Append(talktype);
				}
				return TextCommandResult.Success(sbt.ToString(), null);
			}
			EnumTalkType tt;
			if (Enum.TryParse<EnumTalkType>(args[0] as string, true, out tt))
			{
				this.game.api.World.Player.Entity.talkUtil.Talk(tt);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdClobjc(TextCommandCallingArgs args)
		{
			this.game.api.ObjectCache.Clear();
			return TextCommandResult.Success("Ok, cleared", null);
		}

		private TextCommandResult OnCmdFog(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing && args.Parsers[1].IsMissing)
			{
				return TextCommandResult.Success("Current fog density = " + this.game.AmbientManager.Base.FogDensity.Value.ToString() + ", fog min= " + this.game.AmbientManager.Base.FogMin.Value.ToString(), null);
			}
			float density = (float)args[0];
			float min = (float)args[1];
			this.game.AmbientManager.SetFogRange(density, min);
			return TextCommandResult.Success("Fog set to density=" + density.ToString() + ", min=" + min.ToString(), null);
		}

		private TextCommandResult OnCmdFov(TextCommandCallingArgs args)
		{
			int fov = (int)args[0];
			int minfov = 1;
			int maxfov = 179;
			if (!this.game.IsSingleplayer)
			{
				minfov = 60;
			}
			if (fov < minfov || fov > maxfov)
			{
				return TextCommandResult.Success(string.Format("Valid field of view: {0}-{1}", minfov, maxfov), null);
			}
			float fov_ = 6.2831855f * ((float)fov / 360f);
			this.game.MainCamera.Fov = fov_;
			this.game.OnResize();
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdShake(TextCommandCallingArgs args)
		{
			float strength = (float)args[0];
			this.game.MainCamera.CameraShakeStrength += strength;
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdLiquidselectable(TextCommandCallingArgs args)
		{
			if (!args.Parsers[0].IsMissing)
			{
				this.game.forceLiquidSelectable = (bool)args[0];
			}
			else
			{
				this.game.forceLiquidSelectable = !this.game.forceLiquidSelectable;
			}
			return TextCommandResult.Success("Forced Liquid selectable now " + (this.game.LiquidSelectable ? "on" : "off"), null);
		}

		private TextCommandResult OnCmdRelightchunk(TextCommandCallingArgs args)
		{
			BlockPos chunkpos = this.game.EntityPlayer.Pos.AsBlockPos / 32;
			ClientChunk chunk = this.game.WorldMap.GetClientChunk(chunkpos.X, chunkpos.Y, chunkpos.Z);
			this.game.terrainIlluminator.SunRelightChunk(chunk, chunkpos.X, chunkpos.Y, chunkpos.Z);
			long chunkindex3d = this.game.WorldMap.ChunkIndex3D(chunkpos.X, chunkpos.Y, chunkpos.Z);
			this.game.WorldMap.SetChunkDirty(chunkindex3d, true, false, false);
			return TextCommandResult.Success("Chunk sunlight recaculated and queued for redrawing", null);
		}

		private TextCommandResult OnCmdExptexatlas(TextCommandCallingArgs args)
		{
			string type = args[0] as string;
			if (type == null)
			{
				return TextCommandResult.Success("", null);
			}
			TextureAtlasManager mgr = null;
			string name = "";
			if (!(type == "block"))
			{
				if (!(type == "item"))
				{
					if (type == "entity")
					{
						mgr = this.game.EntityAtlasManager;
						name = "Entity";
					}
				}
				else
				{
					mgr = this.game.ItemAtlasManager;
					name = "Item";
				}
			}
			else
			{
				mgr = this.game.BlockAtlasManager;
				name = "Block";
			}
			if (mgr == null)
			{
				return TextCommandResult.Success("Usage: /exptexatlas [block, item or entity]", null);
			}
			for (int i = 0; i < mgr.Atlasses.Count; i++)
			{
				mgr.Atlasses[i].Export(type + "Atlas-" + i.ToString(), this.game, mgr.AtlasTextures[i].TextureId);
			}
			return TextCommandResult.Success(name + " atlas(ses) exported", null);
		}

		private TextCommandResult OnChunkInfo(TextCommandCallingArgs textCommandCallingArgs)
		{
			BlockPos pos = this.game.EntityPlayer.Pos.AsBlockPos;
			ClientChunk chunk = this.game.WorldMap.GetChunkAtBlockPos(pos.X, pos.Y, pos.Z);
			if (chunk == null)
			{
				this.game.ShowChatMessage("Not loaded yet");
			}
			else
			{
				string rendering = "no";
				if (chunk.centerModelPoolLocations != null)
				{
					rendering = "center";
				}
				if (chunk.edgeModelPoolLocations != null)
				{
					rendering = ((chunk.centerModelPoolLocations != null) ? "yes" : "edge");
				}
				this.game.ShowChatMessage(string.Format("Loaded: {0}, Rendering: {1}, #Drawn: {2}, #Relit: {3}, Queued4Redraw: {4}, Queued4Upload: {5}, Packed: {6}, Empty: {7}", new object[]
				{
					chunk.loadedFromServer,
					rendering,
					chunk.quantityDrawn,
					chunk.quantityRelit,
					chunk.enquedForRedraw,
					chunk.queuedForUpload,
					chunk.IsPacked(),
					chunk.Empty
				}));
				this.game.ShowChatMessage("Traversability: " + Convert.ToString((int)chunk.traversability, 2));
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnRedrawAll(TextCommandCallingArgs textCommandCallingArgs)
		{
			this.game.RedrawAllBlocks();
			return TextCommandResult.Success("Ok, will redraw all chunks, might take some time to take effect.", null);
		}

		private TextCommandResult OnWgenCommand(TextCommandCallingArgs textCommandCallingArgs)
		{
			BlockPos pos = this.game.EntityPlayer.Pos.AsBlockPos;
			int climate = this.game.WorldMap.GetClimate(pos.X, pos.Z);
			int rain = Climate.GetRainFall((climate >> 8) & 255, pos.Y);
			int temp = Climate.GetAdjustedTemperature((climate >> 16) & 255, pos.Y - ClientWorldMap.seaLevel);
			this.game.ShowChatMessage("Rain=" + rain.ToString() + ", temp=" + temp.ToString());
			return TextCommandResult.Success("", null);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}
	}
}
