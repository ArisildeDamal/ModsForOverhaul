using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	public class CmdEntity
	{
		public CmdEntity(ServerMain server)
		{
			CmdEntity.<>c__DisplayClass1_0 CS$<>8__locals1 = new CmdEntity.<>c__DisplayClass1_0();
			CS$<>8__locals1.server = server;
			base..ctor();
			CS$<>8__locals1.<>4__this = this;
			this.server = CS$<>8__locals1.server;
			IChatCommandApi cmdapi = CS$<>8__locals1.server.api.ChatCommands;
			CommandArgumentParsers parsers = CS$<>8__locals1.server.api.ChatCommands.Parsers;
			ServerCoreAPI sapi = CS$<>8__locals1.server.api;
			cmdapi.GetOrCreate("entity").WithAlias(new string[] { "e" }).WithDesc("Entity control via entity selector")
				.RequiresPrivilege(Privilege.controlserver)
				.BeginSub("cmd")
				.WithDesc("Issue commands on existing entities")
				.WithArgs(new ICommandArgumentParser[] { parsers.Entities("target entities") })
				.BeginSub("stopanim")
				.WithDesc("Stop an entity animation")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("animation name") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
				{
					e.StopAnimation((string)args.LastArg);
					return TextCommandResult.Success("animation stopped", null);
				}, 0))
				.EndSub()
				.BeginSub("starttask")
				.WithDesc("Start an ai task")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("task id") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
				{
					e.Notify("starttask", (string)args.LastArg);
					return TextCommandResult.Success("task start executed", null);
				}, 0))
				.EndSub()
				.BeginSub("stoptask")
				.WithDesc("Stop an ai task")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("task id") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
				{
					e.Notify("stoptask", (string)args.LastArg);
					return TextCommandResult.Success("task stop executed", null);
				}, 0))
				.EndSub()
				.BeginSub("setattr")
				.WithDesc("Set entity attributes")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.WordRange("datatype", new string[] { "float", "int", "string", "bool" }),
					parsers.Word("name"),
					parsers.Word("value")
				})
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => CS$<>8__locals1.<>4__this.entitySetAttr(e, args), 0))
				.EndSub()
				.BeginSub("attr")
				.WithDesc("Read entity attributes")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("name") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => CS$<>8__locals1.<>4__this.entityReadAttr(e, args), 0))
				.EndSub()
				.BeginSub("setgen")
				.WithDesc("Set entity generation")
				.WithArgs(new ICommandArgumentParser[] { parsers.Int("generation") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
				{
					e.WatchedAttributes.SetInt("generation", (int)args[1]);
					return TextCommandResult.Success("generation set", null);
				}, 0))
				.EndSub()
				.BeginSub("rmbh")
				.WithDesc("Remove behavior")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("behavior code") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
				{
					EntityBehavior bh = e.GetBehavior((string)args[1]);
					if (bh == null)
					{
						return TextCommandResult.Error("entity " + e.Code.ToShortString() + " has no such behavior", "");
					}
					e.RemoveBehavior(bh);
					return TextCommandResult.Success("Ok, behavior removed", null);
				}, 0))
				.EndSub()
				.BeginSub("setlact")
				.WithDesc("Set entity lactating")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					CmdUtil.EntityEachDelegate entityEachDelegate;
					if ((entityEachDelegate = CS$<>8__locals1.<>9__26) == null)
					{
						entityEachDelegate = (CS$<>8__locals1.<>9__26 = delegate(Entity e)
						{
							ITreeAttribute treeAttribute = e.WatchedAttributes.GetTreeAttribute("multiply");
							if (treeAttribute != null)
							{
								treeAttribute.SetDouble("totalDaysLastBirth", CS$<>8__locals1.server.api.World.Calendar.TotalDays);
							}
							e.WatchedAttributes.MarkPathDirty("multiply");
							return TextCommandResult.Success("Ok, entity lactating set", null);
						});
					}
					return CmdUtil.EntityEach(args, entityEachDelegate, 0);
				})
				.EndSub()
				.BeginSub("move")
				.WithDesc("move a creature")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Double("delta x"),
					parsers.Double("delta y"),
					parsers.Double("delta z")
				})
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
				{
					e.ServerPos.X += (double)args[1];
					e.ServerPos.Y += (double)args[2];
					e.ServerPos.Z += (double)args[3];
					return TextCommandResult.Success("Ok, entity moved", null);
				}, 0))
				.EndSub()
				.BeginSub("kill")
				.WithDesc("kill a creature")
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
				{
					if (e == args.Caller.Entity)
					{
						return TextCommandResult.Success("Ignoring killing of caller", null);
					}
					e.Die(EnumDespawnReason.Death, new DamageSource
					{
						Source = EnumDamageSource.Player,
						SourcePos = args.Caller.Pos,
						SourceEntity = args.Caller.Entity
					});
					return TextCommandResult.Success("Ok, entity killed", null);
				}, 0))
				.EndSub()
				.BeginSub("birth")
				.WithDesc("force a creature to give birth (if it can!)")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("number", 0) })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
				{
					EntityBehavior behavior = e.GetBehavior("multiply");
					if (behavior != null)
					{
						behavior.TestCommand(args.Parsers[1].IsMissing ? 1 : args[1]);
					}
					return TextCommandResult.Success((behavior == null) ? (Lang.Get("item-creature-" + e.Code.Path, Array.Empty<object>()) + " " + Lang.Get("can't bear young!", Array.Empty<object>())) : "OK!", null);
				}, 0))
				.EndSub()
				.EndSub()
				.BeginSub("wipeall")
				.WithDesc("Removes all entities (except players) from all loaded chunks")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("killRadius", 0) })
				.HandleWith(new OnCommandDelegate(this.WipeAllHandler))
				.EndSub();
			cmdapi.GetOrCreate("entity").BeginSub("debug").WithDesc("Set entity debug mode")
				.WithArgs(new ICommandArgumentParser[] { parsers.Bool("on", "on") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					CS$<>8__locals1.server.Config.EntityDebugMode = (bool)args[0];
					CS$<>8__locals1.server.ConfigNeedsSaving = true;
					return TextCommandResult.Success(Lang.Get("Ok, entity debug mode is now {0}", new object[] { CS$<>8__locals1.server.Config.EntityDebugMode ? Lang.Get("on", Array.Empty<object>()) : Lang.Get("off", Array.Empty<object>()) }), null);
				})
				.EndSub()
				.BeginSub("spawndebug")
				.WithDesc("Set entity spawn debug mode")
				.WithArgs(new ICommandArgumentParser[] { parsers.Bool("on", "on") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					CS$<>8__locals1.server.SpawnDebug = (bool)args[0];
					return TextCommandResult.Success(Lang.Get("Ok, entity spawn debug mode is now {0}", new object[] { CS$<>8__locals1.server.SpawnDebug ? Lang.Get("on", Array.Empty<object>()) : Lang.Get("off", Array.Empty<object>()) }), null);
				})
				.EndSub()
				.BeginSub("count")
				.WithDesc("Count entities by code/filter and show a summary")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalEntities("entity filter") })
				.RequiresPrivilege(Privilege.gamemode)
				.HandleWith((TextCommandCallingArgs args) => CS$<>8__locals1.<>4__this.Count(args, false))
				.EndSub()
				.BeginSub("locateg")
				.WithDesc("Group entities together within the specified range and returns the position and amount. This is to find large groups of entities.")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.OptionalEntities("entity filter"),
					parsers.OptionalInt("range", 100)
				})
				.RequiresPrivilege(Privilege.gamemode)
				.HandleWith(new OnCommandDelegate(this.OnLocateGroup))
				.EndSub()
				.BeginSub("find")
				.WithDesc("Returns the name and position of every matching entity which is loaded on the server.")
				.WithAdditionalInformation("Limited to 100 results to prevent spam; use more precise entity filters if that is too many.")
				.WithArgs(new ICommandArgumentParser[] { parsers.Entities("entity filter") })
				.RequiresPrivilege(Privilege.gamemode)
				.HandleWith(new OnCommandDelegate(this.OnFind))
				.EndSub()
				.BeginSub("countg")
				.WithDesc("Count entities by code/filter and show a summary grouped by first code part")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalEntities("entity filter") })
				.RequiresPrivilege(Privilege.gamemode)
				.HandleWith((TextCommandCallingArgs args) => CS$<>8__locals1.<>4__this.Count(args, true))
				.EndSub()
				.BeginSub("spawn")
				.WithAlias(new string[] { "sp" })
				.WithDesc("Spawn entities at the callers position")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.EntityType("entity type"),
					parsers.Int("amount")
				})
				.HandleWith(new OnCommandDelegate(this.spawnEntities))
				.EndSub()
				.BeginSub("spawnat")
				.WithDesc("Spawn entities at given position, within a given radius")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.EntityType("entity type"),
					parsers.Int("amount"),
					parsers.WorldPosition("position"),
					parsers.Double("spawn radius")
				})
				.HandleWith(new OnCommandDelegate(this.spawnEntitiesAt))
				.EndSub()
				.BeginSub("remove")
				.WithAlias(new string[] { "re" })
				.WithDesc("remove selected creatures")
				.WithArgs(new ICommandArgumentParser[] { parsers.Entities("target entities") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, delegate(Entity e)
				{
					if (e == args.Caller.Entity)
					{
						return TextCommandResult.Success("Ignoring removal of caller", null);
					}
					e.Die(EnumDespawnReason.Removed, null);
					return TextCommandResult.Success("Ok, entity removed", null);
				}, 0))
				.EndSub()
				.BeginSub("removebyid")
				.WithDesc("remove selected creatures")
				.WithArgs(new ICommandArgumentParser[] { parsers.Long("id") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					long id = (long)args[0];
					if (id == args.Caller.Entity.EntityId)
					{
						return TextCommandResult.Success("Ignoring removal of caller", null);
					}
					Entity e;
					if (sapi.World.LoadedEntities.TryGetValue(id, out e))
					{
						e.Die(EnumDespawnReason.Removed, null);
						return TextCommandResult.Success("Ok, entity removed", null);
					}
					return TextCommandResult.Success("No entity found", null);
				})
				.EndSub()
				.BeginSub("set-angle")
				.WithAlias(new string[] { "sa" })
				.WithDesc("Set the angle of the entity")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Entities("target entities"),
					parsers.WordRange("axis", new string[] { "yaw", "pitch", "roll" }),
					parsers.Float("degrees")
				})
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => CS$<>8__locals1.<>4__this.setEntityAngle(e, args), 0))
				.EndSub()
				.BeginSub("export")
				.WithDescription("Export a entity spawnat command to server-main log file")
				.WithArgs(new ICommandArgumentParser[] { parsers.Entities("target entities") })
				.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => CS$<>8__locals1.<>4__this.exportEntity(e, args), 0))
				.EndSub()
				.BeginSub("list")
				.WithDesc("List all loaded entity types, with optional filter")
				.WithExamples(new string[] { "/entity list [type=villager*] to list all currently loaded villagers" })
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalEntities("entity filter") })
				.RequiresPrivilege(Privilege.gamemode)
				.HandleWith(new OnCommandDelegate(this.list))
				.EndSub();
		}

		private TextCommandResult list(TextCommandCallingArgs args)
		{
			StringBuilder text = new StringBuilder();
			int count = 0;
			HashSet<string> entityCodes = new HashSet<string>();
			IEnumerable<Entity> enumerable;
			if (!args.Parsers[0].IsMissing)
			{
				ICollection<Entity> collection = args[0] as Entity[];
				enumerable = collection;
			}
			else
			{
				enumerable = this.server.LoadedEntities.Values;
			}
			foreach (Entity entity in enumerable)
			{
				if (!(entity is EntityPlayer))
				{
					count++;
					string code = entity.Code.ToShortString();
					string name = entity.GetName();
					entityCodes.Add(code + "   (" + name + ")");
				}
			}
			string[] array = entityCodes.ToArray<string>();
			Array.Sort<string>(array);
			foreach (string code2 in array)
			{
				text.AppendLine(code2);
			}
			text.AppendLine("(" + count.ToString() + " entity types found)");
			return TextCommandResult.Success(text.ToString(), null);
		}

		private TextCommandResult exportEntity(Entity entity, TextCommandCallingArgs args)
		{
			ILogger logger = this.server.api.Logger;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 4);
			defaultInterpolatedStringHandler.AppendLiteral("/entity spawnat ");
			defaultInterpolatedStringHandler.AppendFormatted<AssetLocation>(entity.Code);
			defaultInterpolatedStringHandler.AppendLiteral(" 1 =");
			defaultInterpolatedStringHandler.AppendFormatted<double>(entity.ServerPos.X, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(" =");
			defaultInterpolatedStringHandler.AppendFormatted<double>(entity.ServerPos.Y, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(" =");
			defaultInterpolatedStringHandler.AppendFormatted<double>(entity.ServerPos.Z, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(" 0");
			logger.Notification(defaultInterpolatedStringHandler.ToStringAndClear());
			return TextCommandResult.Success("Ok, entity exported", null);
		}

		private TextCommandResult spawnEntitiesAt(TextCommandCallingArgs args)
		{
			EntityProperties entityType = (EntityProperties)args[0];
			int quantity = (int)args[1];
			Vec3d pos = (Vec3d)args[2];
			double radius = (double)args[3];
			Random rnd = this.server.api.World.Rand;
			long herdid = this.server.GetNextHerdId();
			int i = quantity;
			while (i-- > 0)
			{
				Entity entity = this.server.api.ClassRegistry.CreateEntity(entityType);
				if (entity is EntityAgent)
				{
					(entity as EntityAgent).HerdId = herdid;
				}
				entity.Pos.SetFrom(pos);
				entity.Pos.X += rnd.NextDouble() * 2.0 * radius - radius;
				entity.Pos.Z += rnd.NextDouble() * 2.0 * radius - radius;
				entity.Pos.Pitch = 0f;
				entity.Pos.Yaw = 0f;
				entity.ServerPos.SetFrom(entity.Pos);
				this.server.SpawnEntity(entity);
			}
			return TextCommandResult.Success(Lang.Get("{0}x{1} spawned.", new object[] { quantity, entityType.Code }), null);
		}

		private TextCommandResult spawnEntities(TextCommandCallingArgs args)
		{
			int quantity = (int)args[1];
			EntityProperties entityType = (EntityProperties)args[0];
			Random rnd = this.server.api.World.Rand;
			long herdid = this.server.GetNextHerdId();
			int i = quantity;
			while (i-- > 0)
			{
				Entity entity = this.server.api.ClassRegistry.CreateEntity(entityType);
				if (entity is EntityAgent)
				{
					(entity as EntityAgent).HerdId = herdid;
				}
				entity.Pos.SetFrom(args.Caller.Entity.Pos);
				entity.Pos.X += rnd.NextDouble() / 10.0 - 0.05;
				entity.Pos.Z += rnd.NextDouble() / 10.0 - 0.05;
				entity.Pos.Pitch = 0f;
				entity.Pos.Yaw = 0f;
				entity.Pos.Motion.Set((0.125 - 0.25 * rnd.NextDouble()) / 2.0, (0.1 + 0.1 * rnd.NextDouble()) / 2.0, (0.125 - 0.25 * rnd.NextDouble()) / 2.0);
				entity.ServerPos.SetFrom(entity.Pos);
				this.server.SpawnEntity(entity);
			}
			return TextCommandResult.Success(Lang.Get("{0}x{1} spawned.", new object[] { quantity, entityType.Code }), null);
		}

		private TextCommandResult entitySetAttr(Entity entity, TextCommandCallingArgs args)
		{
			string datatype = (string)args[1];
			string name = (string)args[2];
			string value = (string)args[3];
			ITreeAttribute attr = entity.WatchedAttributes;
			string path = null;
			if (name.Contains("/"))
			{
				string[] array = name.Split('/', StringSplitOptions.None);
				name = array[array.Length - 1];
				string[] patharr = array.RemoveAt(array.Length - 1);
				path = string.Join("/", patharr);
				attr = entity.WatchedAttributes.GetAttributeByPath(path) as ITreeAttribute;
				if (attr == null)
				{
					return TextCommandResult.Error(Lang.Get("No such path - {0}", new object[] { path }), "nosuchpath");
				}
			}
			if (path != null)
			{
				entity.WatchedAttributes.MarkPathDirty(path);
			}
			if (datatype == "float")
			{
				float val = value.ToFloat(0f);
				attr.SetFloat(name, val);
				return TextCommandResult.Success(name + " float value set to " + val.ToString(), null);
			}
			if (datatype == "int")
			{
				int val2 = value.ToInt(0);
				attr.SetInt(name, val2);
				return TextCommandResult.Success(name + " int value set to " + val2.ToString(), null);
			}
			if (datatype == "string")
			{
				string val3 = value + args.RawArgs.PopAll();
				attr.SetString(name, val3);
				return TextCommandResult.Success(name + " string value set to " + val3, null);
			}
			if (!(datatype == "bool"))
			{
				return TextCommandResult.Error("Incorrect datatype, choose float, int, string or bool", "wrongdatatype");
			}
			bool val4 = value.ToBool(false);
			attr.SetBool(name, val4);
			return TextCommandResult.Success(name + " bool value set to " + val4.ToString(), null);
		}

		private TextCommandResult entityReadAttr(Entity entity, TextCommandCallingArgs args)
		{
			string name = (string)args[1];
			IAttribute attr = entity.WatchedAttributes.GetAttributeByPath(name);
			if (attr == null)
			{
				return TextCommandResult.Error(Lang.Get("No such path - {0}", new object[] { name }), "nosuchpath");
			}
			return TextCommandResult.Success(Lang.Get("Value is: {0}", new object[] { attr.GetValue() }), null);
		}

		private TextCommandResult setEntityAngle(Entity entity, TextCommandCallingArgs args)
		{
			string axis = (string)args[1];
			float degrees = (float)args[2];
			if (!(axis == "yaw"))
			{
				if (!(axis == "pitch"))
				{
					if (axis == "roll")
					{
						entity.ServerPos.Roll = 0.017453292f * degrees;
					}
				}
				else
				{
					entity.ServerPos.Pitch = 0.017453292f * degrees;
				}
			}
			else
			{
				entity.ServerPos.Yaw = 0.017453292f * degrees;
			}
			return TextCommandResult.Success("Entity angle set", null);
		}

		private TextCommandResult OnLocateGroup(TextCommandCallingArgs args)
		{
			Dictionary<BlockPos, List<Entity>> ranged = new Dictionary<BlockPos, List<Entity>>();
			int range = (int)args[1];
			List<Entity> entities;
			if (args.Parsers[0].IsMissing)
			{
				entities = this.server.LoadedEntities.Values.ToList<Entity>();
			}
			else
			{
				entities = (args[0] as Entity[]).ToList<Entity>();
			}
			if (entities.Count != 0)
			{
				Entity first = entities.First<Entity>();
				ranged.Add(first.Pos.AsBlockPos, new List<Entity> { first });
			}
			foreach (Entity entity in entities.Skip(1))
			{
				bool found = false;
				foreach (KeyValuePair<BlockPos, List<Entity>> keyValuePair in ranged)
				{
					BlockPos blockPos;
					List<Entity> list;
					keyValuePair.Deconstruct(out blockPos, out list);
					BlockPos pos = blockPos;
					if (entity.Pos.HorDistanceTo(pos.ToVec3d()) < (double)range)
					{
						ranged[pos].Add(entity);
						found = true;
						break;
					}
				}
				if (!found)
				{
					ranged.Add(entity.Pos.AsBlockPos, new List<Entity> { entity });
				}
			}
			string result;
			if (ranged.Count == 0)
			{
				result = "No entities found";
			}
			else
			{
				StringBuilder sb = new StringBuilder();
				foreach (KeyValuePair<BlockPos, List<Entity>> val in ranged)
				{
					StringBuilder stringBuilder = sb;
					BlockPos key = val.Key;
					stringBuilder.AppendLine(((key != null) ? key.ToString() : null) + " : " + val.Value.Count.ToString());
				}
				result = sb.ToString();
			}
			return TextCommandResult.Success(result, null);
		}

		private TextCommandResult OnFind(TextCommandCallingArgs args)
		{
			IEnumerable<Entity> enumerable;
			if (!args.Parsers[0].IsMissing)
			{
				ICollection<Entity> collection = args[0] as Entity[];
				enumerable = collection;
			}
			else
			{
				enumerable = this.server.LoadedEntities.Values;
			}
			List<Entity> foundEntities = new List<Entity>(enumerable);
			EntityPlayer player = args.Caller.Entity as EntityPlayer;
			if (player != null)
			{
				EntityRangeComparer rangeComparer = new EntityRangeComparer(new Vec3d(player.ServerPos));
				foundEntities.Sort(0, foundEntities.Count, rangeComparer);
			}
			BlockPos mapcenter = this.server.api.World.DefaultSpawnPosition.AsBlockPos;
			mapcenter.Y = 0;
			StringBuilder text = new StringBuilder();
			int count = 0;
			foreach (Entity entity in foundEntities)
			{
				string name = entity.GetName();
				string text2 = "  at ";
				BlockPos blockPos = entity.ServerPos.AsBlockPos.Sub(mapcenter);
				string entry = name + text2 + ((blockPos != null) ? blockPos.ToString() : null);
				text.AppendLine(entry);
				if (++count >= 100)
				{
					break;
				}
			}
			if (text.Length == 0)
			{
				text.Append("(no matches found)");
			}
			else
			{
				text.Insert(0, "===Found entities===\n");
				if (count >= 100)
				{
					text.Append("(" + count.ToString() + " matches found, showing nearest 100)");
				}
				else if (count == 1)
				{
					text.Append("(1 match found)");
				}
				else
				{
					text.Append("(" + count.ToString() + " matches found)");
				}
			}
			return TextCommandResult.Success(text.ToString(), null);
		}

		private TextCommandResult Count(TextCommandCallingArgs args, bool grouped)
		{
			int totalCount = 0;
			int totalActiveCount = 0;
			Dictionary<string, int> quantities = new Dictionary<string, int>();
			IEnumerable<Entity> entities;
			if (args.Parsers[0].IsMissing)
			{
				entities = this.server.LoadedEntities.Values;
			}
			else
			{
				entities = args[0] as Entity[];
			}
			foreach (Entity entity in entities)
			{
				string code = entity.Code.Path;
				if (grouped)
				{
					code = entity.FirstCodePart(0);
				}
				if (quantities.ContainsKey(code))
				{
					Dictionary<string, int> dictionary = quantities;
					string text = code;
					int num = dictionary[text];
					dictionary[text] = num + 1;
				}
				else
				{
					quantities[code] = 1;
				}
				if (entity.State == EnumEntityState.Active)
				{
					totalActiveCount++;
				}
				totalCount++;
			}
			string result;
			if (quantities.Count == 0)
			{
				result = "No entities found";
			}
			else
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine(Lang.Get("{0} total entities, of which {1} active.", new object[] { totalCount, totalActiveCount }));
				foreach (KeyValuePair<string, int> val in quantities)
				{
					sb.AppendLine(val.Key + ": " + val.Value.ToString());
				}
				result = sb.ToString();
			}
			return TextCommandResult.Success(result, null);
		}

		private bool entityTypeMatches(EntityProperties type, EntityProperties referenceType, string searchCode, bool isWildcard)
		{
			if (isWildcard)
			{
				string pattern = Regex.Escape(searchCode).Replace("\\*", "(.*)");
				return Regex.IsMatch(type.Code.Path.ToLowerInvariant(), "^" + pattern + "$");
			}
			return type.Code.Path == referenceType.Code.Path;
		}

		private TextCommandResult WipeAllHandler(TextCommandCallingArgs args)
		{
			int rangeSquared;
			if (args.Parsers[0].IsMissing)
			{
				rangeSquared = 0;
			}
			else
			{
				rangeSquared = (int)args[0];
				rangeSquared *= rangeSquared + 1;
			}
			int centerX = args.Caller.Pos.XInt;
			int centerZ = args.Caller.Pos.ZInt;
			int count = 0;
			foreach (KeyValuePair<long, Entity> val in this.server.LoadedEntities)
			{
				if (!(val.Value is EntityPlayer) && (rangeSquared <= 0 || val.Value.Pos.InHorizontalRangeOf(centerX, centerZ, (float)rangeSquared)))
				{
					val.Value.Die(EnumDespawnReason.Removed, null);
					count++;
				}
			}
			return TextCommandResult.Success("Killed " + count.ToString() + " entities", null);
		}

		private ServerMain server;
	}
}
