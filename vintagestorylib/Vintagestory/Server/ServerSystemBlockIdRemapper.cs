using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerSystemBlockIdRemapper : ServerSystem
	{
		public ServerSystemBlockIdRemapper(ServerMain server)
			: base(server)
		{
			server.ModEventManager.AssetsFirstLoaded += this.RemapBlocks;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			server.api.ChatCommands.Create("bir").RequiresPrivilege(Privilege.controlserver).WithDescription("Block id remapper info and fixing tool")
				.BeginSubCommand("list")
				.WithDescription("list")
				.HandleWith(new OnCommandDelegate(this.OnCmdList))
				.EndSubCommand()
				.BeginSubCommand("getcode")
				.WithDescription("getcode")
				.WithArgs(new ICommandArgumentParser[] { parsers.Int("blockId") })
				.HandleWith(new OnCommandDelegate(this.OnCmdGetcode))
				.EndSubCommand()
				.BeginSubCommand("getid")
				.WithDescription("getid")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("domainAndPath") })
				.HandleWith(new OnCommandDelegate(this.OnCmdGetid))
				.EndSubCommand()
				.BeginSubCommand("map")
				.WithDescription("map")
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("new_block"),
					parsers.Word("old_block"),
					parsers.OptionalWord("force")
				})
				.HandleWith(new OnCommandDelegate(this.OnCmdMap))
				.EndSubCommand()
				.BeginSubCommand("remap")
				.WithAlias(new string[] { "remapq" })
				.WithDescription("map")
				.RequiresPlayer()
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("new_block"),
					parsers.Word("old_block"),
					parsers.OptionalWord("force")
				})
				.HandleWith(new OnCommandDelegate(this.OnCmdReMap))
				.EndSubCommand();
		}

		private TextCommandResult OnCmdReMap(TextCommandCallingArgs args)
		{
			Dictionary<int, AssetLocation> storedBlockCodesById = this.LoadStoredBlockCodesById();
			bool quiet = args.SubCmdCode == "remapq";
			string newBlock = args[0] as string;
			string oldBlock = args[1] as string;
			bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			int newBlockId;
			int oldBlockId;
			if (int.TryParse(newBlock, out newBlockId) && int.TryParse(oldBlock, out oldBlockId))
			{
				this.MapById(storedBlockCodesById, newBlockId, oldBlockId, player, args.Caller.FromChatGroupId, true, force, quiet);
				return TextCommandResult.Success("", null);
			}
			this.MapByCode(storedBlockCodesById, new AssetLocation(newBlock), new AssetLocation(oldBlock), player, args.Caller.FromChatGroupId, true, force, quiet);
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdMap(TextCommandCallingArgs args)
		{
			Dictionary<int, AssetLocation> storedBlockCodesById = this.LoadStoredBlockCodesById();
			string newBlock = args[0] as string;
			string oldBlock = args[1] as string;
			bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
			IServerPlayer player = args.Caller.Player as IServerPlayer;
			int newBlockId;
			int oldBlockId;
			if (int.TryParse(newBlock, out newBlockId) && int.TryParse(oldBlock, out oldBlockId))
			{
				this.MapById(storedBlockCodesById, newBlockId, oldBlockId, player, args.Caller.FromChatGroupId, false, force, false);
				return TextCommandResult.Success("", null);
			}
			this.MapByCode(storedBlockCodesById, new AssetLocation(newBlock), new AssetLocation(oldBlock), player, args.Caller.FromChatGroupId, false, force, false);
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdGetid(TextCommandCallingArgs args)
		{
			Dictionary<int, AssetLocation> storedBlockCodesById = this.LoadStoredBlockCodesById();
			string domainAndPath = args[0] as string;
			if (!storedBlockCodesById.ContainsValue(new AssetLocation(domainAndPath)))
			{
				return TextCommandResult.Success("No mapping for blockcode " + domainAndPath + " found", null);
			}
			return TextCommandResult.Success("Blockcode " + domainAndPath + " is currently mapped to " + storedBlockCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(new AssetLocation(domainAndPath))).Key.ToString(), null);
		}

		private TextCommandResult OnCmdGetcode(TextCommandCallingArgs args)
		{
			int blockId = (int)args[0];
			Dictionary<int, AssetLocation> storedBlockCodesById = this.LoadStoredBlockCodesById();
			if (!storedBlockCodesById.ContainsKey(blockId))
			{
				return TextCommandResult.Success("No mapping for blockid " + blockId.ToString() + " found", null);
			}
			return TextCommandResult.Success("Blockid " + blockId.ToString() + " is currently mapped to " + storedBlockCodesById[blockId], null);
		}

		private TextCommandResult OnCmdList(TextCommandCallingArgs args)
		{
			Dictionary<int, AssetLocation> dictionary = this.LoadStoredBlockCodesById();
			ServerMain.Logger.Notification("Current block id mapping (issued by /bir list command)");
			foreach (KeyValuePair<int, AssetLocation> val in dictionary)
			{
				ServerMain.Logger.Notification("  " + val.Key.ToString() + ": " + val.Value);
			}
			return TextCommandResult.Success("Full mapping printed to console and main log file", null);
		}

		private void MapById(Dictionary<int, AssetLocation> storedBlockCodesById, int newBlockId, int oldBlockId, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
		{
			AssetLocation oldBlockCode;
			if (!force && storedBlockCodesById.TryGetValue(oldBlockId, out oldBlockCode))
			{
				player.SendMessage(groupId, string.Concat(new string[]
				{
					"newblockid ",
					oldBlockId.ToString(),
					" is already mapped to ",
					oldBlockCode,
					", type '/bir ",
					remap ? "remap" : "map",
					" ",
					newBlockId.ToString(),
					" ",
					oldBlockId.ToString(),
					" force' to overwrite"
				}), EnumChatType.CommandError, null);
				return;
			}
			AssetLocation newCode = storedBlockCodesById[newBlockId];
			storedBlockCodesById[oldBlockId] = newCode;
			if (remap)
			{
				storedBlockCodesById.Remove(newBlockId);
			}
			if (!quiet)
			{
				string type = (remap ? "remapped" : "mapped");
				player.SendMessage(groupId, string.Concat(new string[]
				{
					newCode,
					" is now ",
					type,
					" to id ",
					oldBlockId.ToString()
				}), EnumChatType.CommandSuccess, null);
			}
			this.StoreBlockCodesById(storedBlockCodesById);
		}

		private void MapByCode(Dictionary<int, AssetLocation> storedBlockCodesById, AssetLocation newCode, AssetLocation oldCode, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
		{
			if (!storedBlockCodesById.ContainsValue(newCode))
			{
				player.SendMessage(groupId, "No mapping for blockcode " + newCode + " found", EnumChatType.CommandError, null);
				return;
			}
			if (!storedBlockCodesById.ContainsValue(oldCode))
			{
				player.SendMessage(groupId, "No mapping for blockcode " + oldCode + " found", EnumChatType.CommandError, null);
				return;
			}
			if (!force)
			{
				player.SendMessage(groupId, string.Concat(new string[]
				{
					"Both block codes found. Type '/bir ",
					remap ? "remap" : "map",
					" ",
					newCode,
					" ",
					oldCode,
					" force' to make the remap permanent."
				}), EnumChatType.CommandError, null);
				return;
			}
			int newBlockId = storedBlockCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(newCode)).Key;
			int oldBlockId = storedBlockCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(oldCode)).Key;
			storedBlockCodesById[oldBlockId] = newCode;
			if (remap)
			{
				storedBlockCodesById.Remove(newBlockId);
			}
			if (!quiet)
			{
				string type = (remap ? "remapped" : "mapped");
				player.SendMessage(groupId, string.Concat(new string[]
				{
					newCode,
					" is now ",
					type,
					" to id ",
					oldBlockId.ToString()
				}), EnumChatType.CommandSuccess, null);
			}
			this.StoreBlockCodesById(storedBlockCodesById);
		}

		private void RemapBlocks()
		{
			ServerMain.Logger.Event("Remapping blocks and items...");
			ServerMain.Logger.VerboseDebug("BlockID Remapper: Begin");
			Dictionary<AssetLocation, int> currentBlockIdsByCode = new Dictionary<AssetLocation, int>();
			Dictionary<int, AssetLocation> newBlockCodesById = new Dictionary<int, AssetLocation>();
			Dictionary<int, AssetLocation> missingBlockCodesById = new Dictionary<int, AssetLocation>();
			Dictionary<int, int> remappedBlockIds = new Dictionary<int, int>();
			for (int i = 0; i < this.server.Blocks.Count; i++)
			{
				Block block = this.server.Blocks[i];
				if (block != null && !(block.Code == null))
				{
					currentBlockIdsByCode[block.Code] = block.BlockId;
				}
			}
			Dictionary<int, AssetLocation> storedBlockCodesById = this.LoadStoredBlockCodesById();
			if (storedBlockCodesById == null)
			{
				storedBlockCodesById = new Dictionary<int, AssetLocation>();
			}
			if (this.server.Config.RepairMode)
			{
				int maxBlockId = 0;
				Dictionary<string, int> countByDomain = new Dictionary<string, int>();
				this.server.api.Logger.Notification("Stored blocks by mod domain:");
				foreach (KeyValuePair<int, AssetLocation> val in storedBlockCodesById)
				{
					AssetLocation code = val.Value;
					int cnt;
					countByDomain.TryGetValue(code.Domain, out cnt);
					countByDomain[code.Domain] = cnt + 1;
					maxBlockId = Math.Max(maxBlockId, val.Key);
				}
				foreach (KeyValuePair<string, int> val2 in countByDomain)
				{
					ServerMain.Logger.Notification("{0}: {1}", new object[] { val2.Key, val2.Value });
				}
			}
			int maxBlockID = 0;
			foreach (KeyValuePair<int, AssetLocation> val3 in storedBlockCodesById)
			{
				AssetLocation code2 = val3.Value;
				int oldBlockId = val3.Key;
				maxBlockID = Math.Max(oldBlockId, maxBlockID);
				int newBlockId;
				if (!currentBlockIdsByCode.TryGetValue(code2, out newBlockId))
				{
					missingBlockCodesById.Add(oldBlockId, code2);
				}
				else if (newBlockId != oldBlockId)
				{
					remappedBlockIds[newBlockId] = oldBlockId;
				}
			}
			for (int j = 0; j < this.server.Blocks.Count; j++)
			{
				Block block2 = this.server.Blocks[j];
				if (block2 != null)
				{
					maxBlockID = Math.Max(block2.BlockId, maxBlockID);
				}
			}
			this.server.nextFreeBlockId = maxBlockID + 1;
			ServerMain.Logger.VerboseDebug("Max BlockID is " + maxBlockID.ToString());
			bool isNewWorld = storedBlockCodesById.Count == 0;
			HashSet<AssetLocation> storedBlockCodes = new HashSet<AssetLocation>(storedBlockCodesById.Values);
			foreach (KeyValuePair<AssetLocation, int> val4 in currentBlockIdsByCode)
			{
				AssetLocation code3 = val4.Key;
				if (!storedBlockCodes.Contains(code3))
				{
					newBlockCodesById[val4.Value] = code3;
				}
			}
			ServerMain.Logger.VerboseDebug("Found {0} blocks requiring remapping and {1} new blocks", new object[] { remappedBlockIds.Count, newBlockCodesById.Count });
			StringBuilder codes = new StringBuilder();
			List<Block> newBlocks = new List<Block>();
			foreach (KeyValuePair<int, AssetLocation> val5 in newBlockCodesById)
			{
				int curblockId = val5.Key;
				Block block3 = this.server.Blocks[curblockId];
				newBlocks.Add(block3);
				if (!isNewWorld)
				{
					this.server.Blocks[curblockId] = new Block
					{
						BlockId = curblockId
					};
				}
			}
			List<Block> blocksToRemap = new List<Block>();
			int maxNewId = 0;
			foreach (KeyValuePair<int, int> val6 in remappedBlockIds)
			{
				if (codes.Length > 0)
				{
					codes.Append(", ");
				}
				int oldBlockId2 = val6.Key;
				int newBlockId2 = val6.Value;
				maxNewId = Math.Max(maxNewId, newBlockId2);
				Block block4 = this.server.Blocks[oldBlockId2];
				block4.BlockId = newBlockId2;
				blocksToRemap.Add(block4);
				this.server.Blocks[oldBlockId2] = new Block
				{
					BlockId = oldBlockId2,
					IsMissing = true
				};
				codes.Append(oldBlockId2.ToString() + "=>" + newBlockId2.ToString());
			}
			(this.server.Blocks as BlockList).PreAlloc(maxNewId);
			foreach (Block block5 in blocksToRemap)
			{
				this.server.RemapBlock(block5);
			}
			if (!isNewWorld)
			{
				int newBlocksCount = 0;
				foreach (Block block6 in newBlocks)
				{
					if (block6.BlockId != 0)
					{
						this.server.BlocksByCode.Remove(block6.Code);
						this.server.RegisterBlock(block6);
						newBlocksCount++;
					}
				}
				ServerMain.Logger.VerboseDebug("Remapped {0} new blockids", new object[] { newBlocksCount });
			}
			maxBlockID = 0;
			for (int k = 0; k < this.server.Blocks.Count; k++)
			{
				if (this.server.Blocks[k] != null)
				{
					maxBlockID = Math.Max(this.server.Blocks[k].BlockId, maxBlockID);
				}
			}
			this.server.nextFreeBlockId = maxBlockID + 1;
			if (codes.Length > 0)
			{
				ServerMain.Logger.VerboseDebug("Remapped {0} existing blockids", new object[] { remappedBlockIds.Count });
			}
			ServerMain.Logger.Debug("Found {0} missing blocks", new object[] { missingBlockCodesById.Count });
			codes = new StringBuilder();
			FastSmallDictionary<string, CompositeTexture> unknownTex = new FastSmallDictionary<string, CompositeTexture>("all", new CompositeTexture(new AssetLocation("unknown")));
			foreach (KeyValuePair<int, AssetLocation> val7 in missingBlockCodesById)
			{
				if (codes.Length > 0)
				{
					codes.Append(", ");
				}
				this.server.FillMissingBlock(val7.Key, new Block
				{
					Textures = unknownTex,
					Code = val7.Value,
					DrawType = EnumDrawType.Cube,
					MatterState = EnumMatterState.Solid,
					IsMissing = true,
					Replaceable = 1
				});
				codes.Append(val7.Value.ToShortString());
			}
			if (codes.Length > 0)
			{
				ServerMain.Logger.Debug("Added unknown block for {0} blocks", new object[] { missingBlockCodesById.Count });
				ServerMain.Logger.Debug(codes.ToString());
			}
			foreach (Block block7 in newBlocks)
			{
				storedBlockCodesById[block7.BlockId] = block7.Code;
			}
			if (newBlocks.Count > 0)
			{
				ServerMain.Logger.Debug("Added {0} new blocks to the mapping", new object[] { newBlocks.Count });
			}
			this.StoreBlockCodesById(storedBlockCodesById);
		}

		public Dictionary<int, AssetLocation> LoadStoredBlockCodesById()
		{
			Dictionary<int, AssetLocation> blocks = new Dictionary<int, AssetLocation>();
			try
			{
				byte[] data = this.server.api.WorldManager.SaveGame.GetData("BlockIDs");
				if (data != null)
				{
					Dictionary<int, string> dictionary = Serializer.Deserialize<Dictionary<int, string>>(new MemoryStream(data));
					blocks = new Dictionary<int, AssetLocation>();
					foreach (KeyValuePair<int, string> entry in dictionary)
					{
						blocks.Add(entry.Key, new AssetLocation(entry.Value));
					}
					ServerMain.Logger.VerboseDebug(blocks.Count.ToString() + " block IDs loaded from savegame.");
				}
				else
				{
					ServerMain.Logger.Debug("Block IDs not found in savegame.");
				}
			}
			catch
			{
				throw new Exception("Error at loading blocks!");
			}
			return blocks;
		}

		public void StoreBlockCodesById(Dictionary<int, AssetLocation> storedBlockCodesById)
		{
			int maxId = 0;
			MemoryStream ms = new MemoryStream();
			Dictionary<int, string> blocksOld = new Dictionary<int, string>();
			foreach (KeyValuePair<int, AssetLocation> entry in storedBlockCodesById)
			{
				maxId = Math.Max(maxId, entry.Key);
				blocksOld.Add(entry.Key, entry.Value.ToShortString());
			}
			Serializer.Serialize<Dictionary<int, string>>(ms, blocksOld);
			this.server.api.WorldManager.SaveGame.StoreData("BlockIDs", ms.ToArray());
			ServerMain.Logger.Debug("Block IDs have been written to savegame. Saved max BlockID was " + maxId.ToString());
		}
	}
}
