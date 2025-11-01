using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class CmdDebug
	{
		public CmdDebug(ServerMain server)
		{
			CmdDebug <>4__this = this;
			this.MainThread = Thread.CurrentThread;
			this.server = server;
			IChatCommandApi chatCommands = server.api.ChatCommands;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			ServerCoreAPI api = server.api;
			chatCommands.GetOrCreate("debug").WithDesc("Debug and Developer utilities").RequiresPrivilege(Privilege.controlserver)
				.BeginSub("blockcodes")
				.WithDesc("Print codes of all loaded blocks to the server log file")
				.HandleWith(new OnCommandDelegate(this.printBlockCodes))
				.EndSub()
				.BeginSub("itemcodes")
				.WithDesc("Print codes of all loaded items to the server log file")
				.HandleWith(new OnCommandDelegate(this.printItemCodes))
				.EndSub()
				.BeginSub("blockstats")
				.WithDesc("Generates counds amount of block ids used, grouped by first block code part, prints it to the server log file")
				.HandleWith(new OnCommandDelegate(this.printBlockStats))
				.EndSub()
				.BeginSub("helddurability")
				.WithAlias(new string[] { "helddura" })
				.WithDesc("Set held item durability")
				.WithArgs(new ICommandArgumentParser[] { parsers.Int("durability") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					ItemSlot slot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
					if (slot.Itemstack == null)
					{
						return TextCommandResult.Error("Nothing in active hands", "");
					}
					CmdDebug.getSetItemStackAttr(slot, "durability", "int", ((int)args[0]).ToString() ?? "");
					return TextCommandResult.Success(((int)args[0]).ToString() + " durability set.", null);
				})
				.EndSub()
				.BeginSub("heldtemperature")
				.WithAlias(new string[] { "heldtemp" })
				.WithDesc("Set held item temperature")
				.WithArgs(new ICommandArgumentParser[] { parsers.Int("temperature in °C") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					ItemSlot slot2 = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
					ItemStack stack = slot2.Itemstack;
					if (stack == null)
					{
						return TextCommandResult.Error("Nothing in active hands", "");
					}
					int temp = (int)args[0];
					stack.Collectible.SetTemperature(server, stack, (float)temp, true);
					slot2.MarkDirty();
					return TextCommandResult.Success(temp.ToString() + " °C set.", null);
				})
				.EndSub()
				.BeginSub("heldcoattr")
				.WithDesc("Get/Set collectible attributes of the currently held item")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("key"),
					parsers.OptionalAll("value")
				})
				.HandleWith(new OnCommandDelegate(this.getSetCollectibleAttr))
				.EndSub()
				.BeginSub("heldstattr")
				.WithDesc("Get/Set itemstack attributes of the currently held item")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("key"),
					parsers.OptionalWordRange("type", new string[] { "int", "bool", "string", "tree", "double", "float" }),
					parsers.OptionalAll("value")
				})
				.HandleWith(new OnCommandDelegate(this.getSetItemstackAttr))
				.EndSub()
				.BeginSub("netbench")
				.WithDesc("Toggle network benchmarking mode")
				.HandleWith(new OnCommandDelegate(this.toggleNetworkBenchmarking))
				.EndSub()
				.BeginSub("rebuildlandclaimpartitions")
				.WithDesc("Rebuild land claim partitions")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					server.WorldMap.RebuildLandClaimPartitions();
					return TextCommandResult.Success("Partitioned land claim index rebuilt", null);
				})
				.EndSub()
				.BeginSub("privileges")
				.WithDesc("Toggle privileges debug mode")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("on", "on") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					if (args.Parsers[0].IsMissing)
					{
						return TextCommandResult.Success("Privilege debugging is currently " + (server.DebugPrivileges ? "on" : "off"), null);
					}
					server.DebugPrivileges = (bool)args[0];
					return TextCommandResult.Success("Privilege debugging now " + (server.DebugPrivileges ? "on" : "off"), null);
				})
				.EndSub()
				.BeginSub("cloh")
				.WithDesc("Compact the large object heap")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
					GC.Collect();
					return TextCommandResult.Success("Ok, compacted large object heap", null);
				})
				.EndSub()
				.BeginSub("logticks")
				.WithDesc("Toggle slow tick profiler")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Int("millisecond threshold"),
					parsers.OptionalBool("include offthreads", "on")
				})
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					ServerMain.FrameProfiler.PrintSlowTicks = !ServerMain.FrameProfiler.PrintSlowTicks;
					ServerMain.FrameProfiler.Enabled = ServerMain.FrameProfiler.PrintSlowTicks;
					ServerMain.FrameProfiler.PrintSlowTicksThreshold = (int)args[0];
					if ((!args.Parsers[1].IsMissing && (bool)args[1]) || !ServerMain.FrameProfiler.Enabled)
					{
						FrameProfilerUtil.PrintSlowTicks_Offthreads = ServerMain.FrameProfiler.PrintSlowTicks;
						FrameProfilerUtil.PrintSlowTicksThreshold_Offthreads = ServerMain.FrameProfiler.PrintSlowTicksThreshold;
						FrameProfilerUtil.offThreadProfiles = new ConcurrentQueue<string>();
					}
					ServerMain.FrameProfiler.Begin(null, Array.Empty<object>());
					return TextCommandResult.Success("Server Tick Profiling now " + (ServerMain.FrameProfiler.PrintSlowTicks ? ("on, threshold " + ServerMain.FrameProfiler.PrintSlowTicksThreshold.ToString() + " ms") : "off"), null);
				})
				.EndSub()
				.BeginSub("octagonpoints")
				.WithDesc("Exports a map of chunks that ought to be sent to the client as a png image")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					<>4__this.PrintOctagonPoints(args.Caller.Player.WorldData.DesiredViewDistance);
					return TextCommandResult.Success("Printed octagon points", null);
				})
				.EndSub()
				.BeginSub("tickposition")
				.WithDesc("Print current server tick position (for debugging a frozen server)")
				.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.TickPosition.ToString() ?? "", null))
				.EndSub()
				.BeginSub("mainthreadstate")
				.WithDesc("Print current main thread state")
				.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(<>4__this.MainThread.ThreadState.ToString(), null))
				.EndSub()
				.BeginSub("threadpoolstate")
				.WithDesc("Print current thread pool state")
				.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(TyronThreadPool.Inst.ListAllRunningTasks() + "\n" + TyronThreadPool.Inst.ListAllThreads(), null))
				.EndSub()
				.BeginSub("tickhandlers")
				.WithDesc("Counts amount of game tick listeners grouped by listener type")
				.HandleWith(new OnCommandDelegate(this.countTickHandlers))
				.BeginSub("dump")
				.WithDesc("Export full list of all listeners to the log file")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("Listener type") })
				.HandleWith(new OnCommandDelegate(this.exportTickHandlers))
				.EndSub()
				.EndSub()
				.BeginSub("chunk")
				.WithDesc("Chunk debug utilities")
				.BeginSub("queue")
				.WithAlias(new string[] { "q" })
				.WithDesc("Amount of generating chunks in queue")
				.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(string.Format("Currently {0} chunks in generation queue", server.chunkThread.requestedChunkColumns.Count), null))
				.EndSub()
				.BeginSub("stats")
				.WithDesc("Statics on currently loaded chunks")
				.HandleWith(new OnCommandDelegate(this.getChunkStats))
				.EndSub()
				.BeginSub("printmap")
				.WithDesc("Exports a map of loaded chunk as a png image")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					server.WorldMap.PrintChunkMap(new Vec2i(server.MapSize.X / 2 / 32, server.MapSize.Z / 2 / 32));
					return TextCommandResult.Success("Printed chunk map", null);
				})
				.EndSub()
				.BeginSub("here")
				.WithDesc("Information about the chunk at the callers position")
				.HandleWith(new OnCommandDelegate(this.getHereChunkInfo))
				.EndSub()
				.BeginSub("resend")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWorldPosition("position") })
				.WithDesc("Resend a chunk to all players")
				.HandleWith(new OnCommandDelegate(this.resendChunk))
				.EndSub()
				.BeginSub("relight")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWorldPosition("position") })
				.WithDesc("Relight a chunk for all players")
				.HandleWith(new OnCommandDelegate(this.relightChunk))
				.EndSub()
				.EndSub()
				.BeginSub("sendchunks")
				.WithDescription("Allows toggling of the normal chunk generation/sending operations to all clients.")
				.WithAdditionalInformation("Force loaded chunks are not affected by this switch.")
				.WithArgs(new ICommandArgumentParser[] { parsers.Bool("state", "on") })
				.HandleWith(new OnCommandDelegate(this.toggleSendChunks))
				.EndSub()
				.BeginSub("expclang")
				.WithDescription("Export a list of missing block and item translations, with suggestions")
				.HandleWith(new OnCommandDelegate(this.handleExpCLang))
				.EndSub()
				.BeginSub("blu")
				.WithDesc("Place every block type in the game")
				.HandleWith(new OnCommandDelegate(this.handleBlu))
				.EndSub()
				.BeginSub("dumpanimstate")
				.WithDesc("Dump animation state into log file")
				.WithArgs(new ICommandArgumentParser[] { parsers.Entities("target entity") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => <>4__this.handleDumpAnimState(e, args), 0))
				.EndSub()
				.BeginSub("dumprecipes")
				.WithDesc("Dump grid recipes into log file")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					foreach (GridRecipe rec in server.GridRecipes)
					{
						bool print = false;
						foreach (KeyValuePair<string, CraftingRecipeIngredient> val in rec.Ingredients)
						{
							ItemStack resolvedItemstack = val.Value.ResolvedItemstack;
							if (((resolvedItemstack != null) ? resolvedItemstack.TempAttributes : null) != null && val.Value.ResolvedItemstack.TempAttributes.Count > 0)
							{
								if (!print)
								{
									ServerMain.Logger.VerboseDebug(rec.Name);
								}
								print = true;
								LoggerBase logger = ServerMain.Logger;
								string[] array = new string[5];
								array[0] = val.Key;
								array[1] = ": ";
								array[2] = val.Value.ToString();
								array[3] = "/";
								int num = 4;
								ITreeAttribute tempAttributes = val.Value.ResolvedItemstack.TempAttributes;
								array[num] = ((tempAttributes != null) ? tempAttributes.ToString() : null);
								logger.VerboseDebug(string.Concat(array));
							}
						}
					}
					return TextCommandResult.Success("", null);
				})
				.EndSub()
				.BeginSub("testcond")
				.WithDescription("Test conditionals")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalAll("cond") })
				.HandleWith(new OnCommandDelegate(this.handleTestCond))
				.EndSub()
				.BeginSubCommand("spawnheatmap")
				.WithDescription("spawnheatmap")
				.HandleWith(new OnCommandDelegate(this.OnCmdSpawnHeatmap))
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.WordRange("x-axis", new string[] { "temp", "rain", "forest", "elevation" }),
					parsers.WordRange("y-axis", new string[] { "temp", "rain", "forest", "elevation" }),
					parsers.OptionalWord("entity type"),
					parsers.OptionalBool("negate entity type filter", "on")
				})
				.EndSubCommand()
				.Validate();
		}

		private TextCommandResult handleDumpAnimState(Entity e, TextCommandCallingArgs args)
		{
			LoggerBase logger = ServerMain.Logger;
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

		private TextCommandResult OnCmdSpawnHeatmap(TextCommandCallingArgs args)
		{
			byte[] data = this.server.AssetManager.TryGet("textures/environment/planttint.png", true).Data;
			BitmapExternal bmp = new BitmapExternal(data, data.Length, ServerMain.Logger);
			List<CmdDebug.Eproprs> eprops = new List<CmdDebug.Eproprs>();
			string xaxis = args[0] as string;
			string yaxis = args[1] as string;
			AssetLocation etype = (args.Parsers[2].IsMissing ? null : new AssetLocation(args[2] as string));
			bool negate = (bool)args[3];
			Random rand = new Random(0);
			foreach (EntityProperties props in this.server.EntityTypes)
			{
				EntityServerProperties entityServerProperties = props.Server;
				RuntimeSpawnConditions runtimeSpawnConditions;
				if (entityServerProperties == null)
				{
					runtimeSpawnConditions = null;
				}
				else
				{
					SpawnConditions spawnConditions = entityServerProperties.SpawnConditions;
					runtimeSpawnConditions = ((spawnConditions != null) ? spawnConditions.Runtime : null);
				}
				RuntimeSpawnConditions spawnconds = runtimeSpawnConditions;
				if (spawnconds != null && !props.Code.Path.Contains("drifter") && !props.Code.Path.Contains("butter"))
				{
					if (etype != null)
					{
						bool match = !WildcardUtil.Match(etype, props.Code);
						if ((match && !negate) || (!match && negate))
						{
							continue;
						}
					}
					if (spawnconds.MaxQuantity > 0 || (spawnconds.MaxQuantityByGroup != null && spawnconds.MaxQuantityByGroup.MaxQuantity > 0))
					{
						SKColor skcol;
						if (props.Color == null || props.Color == "")
						{
							skcol = new SKColor(((uint)rand.NextInt64() & 16777215U) | 1610612736U);
						}
						else
						{
							skcol = new SKColor((uint)((ColorUtil.Hex2Int(props.Color) & 16777215) | 1610612736));
						}
						eprops.Add(new CmdDebug.Eproprs
						{
							Props = props,
							Color = skcol
						});
						ServerMain.Logger.Notification(props.Code.ToString());
					}
				}
			}
			for (int x = 0; x < 256; x++)
			{
				for (int y = 0; y < 256; y++)
				{
					for (int i = 0; i < eprops.Count; i++)
					{
						CmdDebug.Eproprs eprop = eprops[i];
						RuntimeSpawnConditions spc = eprop.Props.Server.SpawnConditions.Runtime;
						float tempx = GameMath.Clamp(((float)x - 0f) / 4.25f - 20f, -20f, 40f);
						float tempy = GameMath.Clamp(((float)y - 0f) / 4.25f - 20f, -20f, 40f);
						float relx = (float)x / 255f;
						float rely = (float)y / 255f;
						bool xAxisOk = false;
						bool yAxisOk = false;
						if (xaxis == "temp")
						{
							xAxisOk = tempx >= spc.MinTemp && tempx <= spc.MaxTemp;
						}
						else if (xaxis == "rain")
						{
							xAxisOk = rely >= spc.MinRain && rely <= spc.MaxRain;
						}
						else if (xaxis == "forest")
						{
							xAxisOk = relx >= spc.MinForest && relx <= spc.MaxForest;
						}
						else if (xaxis == "elevation")
						{
							xAxisOk = relx >= spc.MinY - 1f && relx <= spc.MaxY - 1f;
						}
						if (yaxis == "temp")
						{
							yAxisOk = tempy >= spc.MinTemp && tempy <= spc.MaxTemp;
						}
						else if (yaxis == "rain")
						{
							yAxisOk = rely >= spc.MinRain && rely <= spc.MaxRain;
						}
						else if (yaxis == "forest")
						{
							yAxisOk = rely >= spc.MinForest && rely <= spc.MaxForest;
						}
						else if (yaxis == "elevation")
						{
							yAxisOk = rely >= spc.MinY - 1f && rely <= spc.MaxY - 1f;
						}
						if (xAxisOk && yAxisOk)
						{
							int outcol = ColorUtil.ColorOverlay(bmp.bmp.GetPixel(4 + x, 4 + y).ToInt(), eprop.Color.ToInt(), (float)eprop.Color.Alpha / 255f);
							SKColor overlaidcol = new SKColor((uint)outcol);
							bmp.bmp.SetPixel(4 + x, 4 + y, overlaidcol);
						}
					}
				}
			}
			bmp.Save("spawnheatmap.png");
			if (!(etype == null))
			{
				return TextCommandResult.Success("ok, spawnheatmap.png generated for " + etype.Path + ". Also printed matching entities to server-main.log", null);
			}
			return TextCommandResult.Success("ok, spawnheatmap.png generated. Also printed matching entities to server-main.log", null);
		}

		private TextCommandResult handleBlu(TextCommandCallingArgs args)
		{
			this.BlockLineup(args.Caller.Pos.AsBlockPos, args.RawArgs);
			return TextCommandResult.Success("Block lineup created", null);
		}

		private void BlockLineup(BlockPos pos, CmdArgs args)
		{
			IList<Block> blocks = this.server.World.Blocks;
			bool all = args.PopWord(null) == "all";
			List<Block> existingBlocks = new List<Block>();
			for (int i = 0; i < blocks.Count; i++)
			{
				Block block = blocks[i];
				if (block != null && !(block.Code == null))
				{
					if (all)
					{
						existingBlocks.Add(block);
					}
					else if (block.CreativeInventoryTabs != null && block.CreativeInventoryTabs.Length != 0)
					{
						existingBlocks.Add(block);
					}
				}
			}
			int width = (int)Math.Sqrt((double)existingBlocks.Count);
			for (int j = 0; j < existingBlocks.Count; j++)
			{
				this.server.World.BlockAccessor.SetBlock(existingBlocks[j].BlockId, pos.AddCopy(j / width, 0, j % width));
			}
		}

		private TextCommandResult printBlockCodes(TextCommandCallingArgs args)
		{
			Dictionary<string, int> blockcodes = new Dictionary<string, int>();
			foreach (Block block in this.server.Blocks)
			{
				if (!(block.Code == null))
				{
					string key = block.Code.ToShortString();
					int cnt;
					blockcodes.TryGetValue(key, out cnt);
					cnt++;
					blockcodes[key] = cnt;
				}
			}
			List<KeyValuePair<string, int>> list = blockcodes.OrderByDescending((KeyValuePair<string, int> p) => p.Value).ToList<KeyValuePair<string, int>>();
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<string, int> val in list)
			{
				sb.AppendLine(val.Key);
			}
			ServerMain.Logger.Notification(sb.ToString());
			return TextCommandResult.Success("Block codes written to log file.", null);
		}

		private TextCommandResult printItemCodes(TextCommandCallingArgs args)
		{
			Dictionary<string, int> itemcodes = new Dictionary<string, int>();
			foreach (Item item in this.server.Items)
			{
				if (!(item.Code == null))
				{
					string key = item.Code.ToShortString();
					int cnt;
					itemcodes.TryGetValue(key, out cnt);
					cnt++;
					itemcodes[key] = cnt;
				}
			}
			List<KeyValuePair<string, int>> list = itemcodes.OrderByDescending((KeyValuePair<string, int> p) => p.Value).ToList<KeyValuePair<string, int>>();
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<string, int> val in list)
			{
				sb.AppendLine(val.Key);
			}
			ServerMain.Logger.Notification(sb.ToString());
			return TextCommandResult.Success("Item codes written to log file.", null);
		}

		private TextCommandResult printBlockStats(TextCommandCallingArgs args)
		{
			Dictionary<string, int> blockcodes = new Dictionary<string, int>();
			foreach (Block block in this.server.Blocks)
			{
				if (!(block.Code == null))
				{
					string key = block.Code.Domain + ":" + block.FirstCodePart(0);
					int cnt;
					blockcodes.TryGetValue(key, out cnt);
					cnt++;
					blockcodes[key] = cnt;
				}
			}
			List<KeyValuePair<string, int>> list = blockcodes.OrderByDescending((KeyValuePair<string, int> p) => p.Value).ToList<KeyValuePair<string, int>>();
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<string, int> val in list)
			{
				sb.AppendLine(val.Key + ": " + val.Value.ToString());
			}
			ServerMain.Logger.Notification(sb.ToString());
			return TextCommandResult.Success("Block ids summary written to log file.", null);
		}

		private TextCommandResult getSetCollectibleAttr(TextCommandCallingArgs args)
		{
			ItemSlot slot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
			ItemStack stack = slot.Itemstack;
			if (stack == null)
			{
				return TextCommandResult.Success("Nothing in right hands", null);
			}
			string key = (string)args[0];
			string value = (string)args[1];
			JToken jtoken = stack.Collectible.Attributes.Token;
			if (key == null)
			{
				return TextCommandResult.Error("Syntax: /debug heldcoattr key value", "");
			}
			if (value == null)
			{
				return TextCommandResult.Success(Lang.Get("Collectible Attribute {0} has value {1}.", new object[]
				{
					key,
					jtoken[key]
				}), null);
			}
			jtoken[key] = JToken.Parse(value);
			slot.MarkDirty();
			return TextCommandResult.Success(Lang.Get("Collectible Attribute {0} set to {1}.", new object[] { key, value }), null);
		}

		private TextCommandResult getSetItemstackAttr(TextCommandCallingArgs args)
		{
			ItemSlot slot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
			if (slot.Itemstack == null)
			{
				return TextCommandResult.Error("Nothing in active hands", "");
			}
			string key = (string)args[0];
			string type = (string)args[1];
			string value = (string)args[2];
			return CmdDebug.getSetItemStackAttr(slot, key, type, value);
		}

		private static TextCommandResult getSetItemStackAttr(ItemSlot slot, string key, string type, string value)
		{
			ItemStack stack = slot.Itemstack;
			if (type != null)
			{
				if (!(type == "int"))
				{
					if (!(type == "bool"))
					{
						if (!(type == "string"))
						{
							if (!(type == "tree"))
							{
								if (!(type == "double"))
								{
									if (!(type == "float"))
									{
										return TextCommandResult.Error("Invalid type", "");
									}
									stack.Attributes.SetFloat(key, value.ToFloat(0f));
								}
								else
								{
									stack.Attributes.SetDouble(key, value.ToDouble(0.0));
								}
							}
							else
							{
								stack.Attributes[key] = new JsonObject(JObject.Parse(value)).ToAttribute();
							}
						}
						else
						{
							stack.Attributes.SetString(key, value);
						}
					}
					else
					{
						stack.Attributes.SetBool(key, value.ToBool(false));
					}
				}
				else
				{
					stack.Attributes.SetInt(key, value.ToInt(0));
				}
				slot.MarkDirty();
				return TextCommandResult.Success(string.Format("Stack Attribute {0}={1} set.", key, stack.Attributes[key].ToString()), null);
			}
			if (stack.Attributes.HasAttribute(key))
			{
				IAttribute attr = stack.Attributes[key];
				Type attrtype = TreeAttribute.AttributeIdMapping[attr.GetAttributeId()];
				return TextCommandResult.Success(Lang.Get("Attribute {0} is of type {1} and has value {2}", new object[]
				{
					attrtype,
					attr.ToString()
				}), null);
			}
			return TextCommandResult.Error(Lang.Get("Attribute {0} does not exist", Array.Empty<object>()), "");
		}

		private TextCommandResult toggleNetworkBenchmarking(TextCommandCallingArgs args)
		{
			if (this.server.doNetBenchmark)
			{
				this.server.doNetBenchmark = false;
				StringBuilder str = new StringBuilder();
				foreach (KeyValuePair<int, int> val in this.server.packetBenchmarkBytes)
				{
					string packetName;
					SystemNetworkProcess.ServerPacketNames.TryGetValue(val.Key, out packetName);
					str.AppendLine(string.Concat(new string[]
					{
						this.server.packetBenchmark[val.Key].ToString(),
						"x ",
						packetName,
						": ",
						(val.Value > 9999) ? (((float)val.Value / 1024f).ToString("#.#") + "kb") : (val.Value.ToString() + "b")
					}));
				}
				str.AppendLine("-----");
				foreach (KeyValuePair<string, int> val2 in this.server.packetBenchmarkBlockEntitiesBytes)
				{
					string packetName2 = val2.Key;
					str.AppendLine("BE " + packetName2 + ": " + ((val2.Value > 9999) ? (((float)val2.Value / 1024f).ToString("#.#") + "kb") : (val2.Value.ToString() + "b")));
				}
				str.AppendLine("-----");
				foreach (KeyValuePair<int, int> val3 in this.server.udpPacketBenchmarkBytes)
				{
					string packetName3 = val3.Key.ToString();
					str.AppendLine(string.Concat(new string[]
					{
						this.server.udpPacketBenchmark[val3.Key].ToString(),
						"x ",
						packetName3,
						": ",
						(val3.Value > 9999) ? (((float)val3.Value / 1024f).ToString("#.#") + "kb") : (val3.Value.ToString() + "b")
					}));
				}
				return TextCommandResult.Success(str.ToString(), null);
			}
			this.server.doNetBenchmark = true;
			this.server.packetBenchmark.Clear();
			this.server.packetBenchmarkBytes.Clear();
			this.server.packetBenchmarkBlockEntitiesBytes.Clear();
			this.server.udpPacketBenchmark.Clear();
			this.server.udpPacketBenchmarkBytes.Clear();
			return TextCommandResult.Success("Benchmarking started. Stop it after a while to get results.", null);
		}

		private TextCommandResult toggleSendChunks(TextCommandCallingArgs args)
		{
			this.server.SendChunks = (bool)args[0];
			return TextCommandResult.Success("Sending chunks is now " + (this.server.SendChunks ? "on" : "off"), null);
		}

		private TextCommandResult handleExpCLang(TextCommandCallingArgs args)
		{
			if (this.server.Config.HostedMode)
			{
				return TextCommandResult.Error("Can't access this feature, server is in hosted mode", "");
			}
			List<string> lines = new List<string>();
			for (int i = 0; i < this.server.Blocks.Count; i++)
			{
				Block block = this.server.Blocks[i];
				if (block != null && !(block.Code == null) && block.CreativeInventoryTabs != null && block.CreativeInventoryTabs.Length != 0)
				{
					string heldItemName = block.GetHeldItemName(new ItemStack(block, 1));
					AssetLocation code = block.Code;
					string text = ((code != null) ? code.Domain : null);
					string text2 = ":block-";
					AssetLocation code2 = block.Code;
					if (heldItemName == text + text2 + ((code2 != null) ? code2.Path : null))
					{
						string domain = block.Code.ShortDomain();
						if (domain.Length > 0)
						{
							domain += ":";
						}
						lines.Add(string.Concat(new string[]
						{
							"\t\"",
							domain,
							"block-",
							block.Code.Path,
							"\": \"",
							Lang.GetNamePlaceHolder(block.Code),
							"\","
						}));
					}
				}
			}
			for (int j = 0; j < this.server.Items.Count; j++)
			{
				Item item = this.server.Items[j];
				if (item != null && !(item.Code == null) && item.CreativeInventoryTabs != null && item.CreativeInventoryTabs.Length != 0)
				{
					string heldItemName2 = item.GetHeldItemName(new ItemStack(item, 1));
					AssetLocation code3 = item.Code;
					string text3 = ((code3 != null) ? code3.Domain : null);
					string text4 = ":item-";
					AssetLocation code4 = item.Code;
					if (heldItemName2 == text3 + text4 + ((code4 != null) ? code4.Path : null))
					{
						string domain2 = item.Code.ShortDomain();
						if (domain2.Length > 0)
						{
							domain2 += ":";
						}
						lines.Add(string.Concat(new string[]
						{
							"\t\"",
							domain2,
							"item-",
							item.Code.Path,
							"\": \"",
							Lang.GetNamePlaceHolder(item.Code),
							"\","
						}));
					}
				}
			}
			TreeAttribute tree = new TreeAttribute();
			this.server.api.eventapi.PushEvent("expclang", tree);
			foreach (KeyValuePair<string, IAttribute> val in tree)
			{
				StringAttribute stringAttribute = val.Value as StringAttribute;
				string line = ((stringAttribute != null) ? stringAttribute.value : null);
				if (line != null)
				{
					lines.Add(line);
				}
			}
			lines.Sort();
			string outfilepath = "collectiblelang.json";
			using (TextWriter textWriter = new StreamWriter(outfilepath))
			{
				textWriter.Write(string.Join("\r\n", lines));
				textWriter.Close();
			}
			return TextCommandResult.Success("Ok, Missing translations exported to " + outfilepath, null);
		}

		private TextCommandResult getChunkStats(TextCommandCallingArgs args)
		{
			int total = 0;
			int packed = 0;
			int cntData = 0;
			int cntEmpty = 0;
			this.server.loadedChunksLock.AcquireReadLock();
			try
			{
				foreach (KeyValuePair<long, ServerChunk> val in this.server.loadedChunks)
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
			}
			finally
			{
				this.server.loadedChunksLock.ReleaseReadLock();
			}
			ChunkDataPool pool = this.server.serverChunkDataPool;
			return TextCommandResult.Success(string.Format("{0} Total chunks ({1} with data and {2} empty)\n{3} of which are packed\nFree pool objects {0}", new object[]
			{
				total,
				cntData,
				cntEmpty,
				packed,
				pool.CountFree()
			}), null);
		}

		private TextCommandResult getHereChunkInfo(TextCommandCallingArgs args)
		{
			int chunkX = (int)args.Caller.Pos.X / 32;
			int chunkY = (int)args.Caller.Pos.Y / 32;
			int chunkZ = (int)args.Caller.Pos.Z / 32;
			long index3d = this.server.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ);
			long index2d = this.server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			ConnectedClient client = this.server.GetClientByUID(args.Caller.Player.PlayerUID);
			bool loaded = this.server.WorldMap.GetServerChunk(index3d) != null;
			bool didRequest = this.server.ChunkColumnRequested.ContainsKey(index2d);
			bool didSend = client.DidSendChunk(index3d);
			bool inRequestQueue = this.server.requestedChunkColumns.Contains(index2d);
			IServerChunk chunk = this.server.WorldMap.GetChunk(chunkX, chunkY, chunkZ) as IServerChunk;
			return TextCommandResult.Success(string.Format("Loaded: {0}, DidRequest: {1}, DidSend: {2}, InRequestQueue: {3}, your current chunk sent radius: {4}{5}, Player placed blocks: {6}, Player removed blocks: {7}", new object[]
			{
				loaded,
				didRequest,
				didSend,
				inRequestQueue,
				client.CurrentChunkSentRadius,
				loaded ? (", " + string.Format("Gameversioncreated: {0} , WorldGenVersion: {1}", chunk.GameVersionCreated ?? "1.10 or earlier", ((ServerMapChunk)chunk.MapChunk).WorldGenVersion)) : "",
				chunk.BlocksPlaced,
				chunk.BlocksRemoved
			}), null);
		}

		private TextCommandResult resendChunk(TextCommandCallingArgs args)
		{
			Vec3d vec3d = args[0] as Vec3d;
			int chunkX = (int)vec3d.X / 32;
			int chunkY = (int)vec3d.Y / 32;
			int chunkZ = (int)vec3d.Z / 32;
			this.server.BroadcastChunk(chunkX, chunkY, chunkZ, false);
			return TextCommandResult.Success("Ok, chunk now resent", null);
		}

		private TextCommandResult relightChunk(TextCommandCallingArgs args)
		{
			Vec3d vec3d = args[0] as Vec3d;
			int chunksize = 32;
			int chunkX = (int)vec3d.X / chunksize;
			int chunkY = (int)vec3d.Y / chunksize;
			int chunkZ = (int)vec3d.Z / chunksize;
			BlockPos minPos = new BlockPos(chunkX * chunksize, chunkY * chunksize, chunkZ * chunksize);
			BlockPos maxPos = new BlockPos((chunkX + 1) * chunksize - 1, (chunkY + 1) * chunksize - 1, (chunkZ + 1) * chunksize - 1);
			this.server.api.WorldManager.FullRelight(minPos, maxPos);
			return TextCommandResult.Success("Ok, chunk now relit", null);
		}

		private TextCommandResult exportTickHandlers(TextCommandCallingArgs args)
		{
			string type = (string)args[0];
			this.server.EventManager.defragLists();
			if (!(type == "gtblock"))
			{
				if (!(type == "gtentity"))
				{
					if (!(type == "dcblock"))
					{
						if (!(type == "sdcblock"))
						{
							if (type == "dcentity")
							{
								this.dumpList<KeyValuePair<long, DelayedCallback>>(this.server.EventManager.DelayedCallbacksEntity);
							}
						}
						else
						{
							this.dumpList<DelayedCallbackBlock>(this.server.EventManager.SingleDelayedCallbacksBlock.Values);
						}
					}
					else
					{
						this.dumpList<DelayedCallbackBlock>(this.server.EventManager.DelayedCallbacksBlock);
					}
				}
				else
				{
					this.dumpList<GameTickListener>(this.server.EventManager.GameTickListenersEntity);
				}
			}
			else
			{
				this.dumpList<GameTickListenerBlock>(this.server.EventManager.GameTickListenersBlock);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult countTickHandlers(TextCommandCallingArgs args)
		{
			this.server.EventManager.defragLists();
			return TextCommandResult.Success(Lang.Get("GameTickListenersBlock={0}, GameTickListenersEntity={1}, DelayedCallbacksBlock={2}, DelayedCallbacksEntity={3}, SingleDelayedCallbacksBlock={4}", new object[]
			{
				this.server.EventManager.GameTickListenersBlock.Count,
				this.server.EventManager.GameTickListenersEntity.Count,
				this.server.EventManager.DelayedCallbacksBlock.Count,
				this.server.EventManager.DelayedCallbacksEntity.Count,
				this.server.EventManager.SingleDelayedCallbacksBlock.Count
			}), null);
		}

		private void PrintOctagonPoints(int viewDistance)
		{
			int desiredRadius = (int)Math.Ceiling((double)((float)viewDistance / (float)MagicNum.ServerChunkSize));
			for (int r = 1; r < desiredRadius; r++)
			{
				Vec2i[] octagonPoints = ShapeUtil.GetOctagonPoints(desiredRadius / 2 + 25, desiredRadius / 2 + 25, r);
				SKBitmap bmp = new SKBitmap(desiredRadius + 50, desiredRadius + 50, false);
				foreach (Vec2i point in octagonPoints)
				{
					bmp.SetPixel(point.X, point.Y, new SKColor(byte.MaxValue, 0, 0, byte.MaxValue));
				}
				bmp.Save("octapoints" + r.ToString() + ".png");
			}
		}

		private void dumpList<T>(ICollection<T> list)
		{
			StringBuilder sb = new StringBuilder();
			foreach (T t in list)
			{
				GameTickListener gtl = t as GameTickListener;
				if (gtl != null)
				{
					sb.AppendLine(gtl.Origin().ToString() + ":" + gtl.Handler.Method.ToString());
				}
				DelayedCallback dc = t as DelayedCallback;
				if (dc != null)
				{
					sb.AppendLine(dc.Handler.Target.ToString() + ":" + dc.Handler.Method.ToString());
				}
				GameTickListenerBlock gtblock = t as GameTickListenerBlock;
				if (gtblock != null)
				{
					sb.AppendLine(gtblock.Handler.Target.ToString() + ":" + gtblock.Handler.Method.ToString());
				}
				DelayedCallbackBlock dcblock = t as DelayedCallbackBlock;
				if (dcblock != null)
				{
					sb.AppendLine(dcblock.Handler.Target.ToString() + ":" + dcblock.Handler.Method.ToString());
				}
			}
			ServerMain.Logger.VerboseDebug(sb.ToString());
		}

		private TextCommandResult handleTestCond(TextCommandCallingArgs args)
		{
			if (args == null || args.ArgCount == 0)
			{
				return TextCommandResult.Error("Need to specify a condition", "");
			}
			string allArgs = (string)args[0];
			string result = "";
			if (allArgs.StartsWith("isBlock"))
			{
				result = IsBlockArgParser.Test(this.server.api, args.Caller, allArgs);
			}
			return TextCommandResult.Success(result, null);
		}

		private StackTrace GetStackTrace(Thread targetThread)
		{
			StackTrace stackTrace = null;
			ManualResetEventSlim ready = new ManualResetEventSlim();
			new Thread(delegate
			{
				ready.Set();
				Thread.Sleep(200);
				try
				{
					targetThread.Resume();
				}
				catch
				{
				}
			}).Start();
			ready.Wait();
			targetThread.Suspend();
			try
			{
			}
			finally
			{
				try
				{
					targetThread.Resume();
				}
				catch
				{
					stackTrace = null;
				}
			}
			return stackTrace;
		}

		private Thread MainThread;

		private ServerMain server;

		private class Eproprs
		{
			public EntityProperties Props;

			public SKColor Color;
		}
	}
}
