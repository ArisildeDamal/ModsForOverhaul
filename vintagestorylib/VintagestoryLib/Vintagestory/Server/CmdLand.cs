using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class CmdLand
	{
		public CmdLand(ServerMain server)
		{
			CmdLand <>4__this = this;
			this.server = server;
			IChatCommandApi chatCommands = server.api.ChatCommands;
			CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
			CmdLand.ClaimInProgressHandlerDelegate <>9__20;
			CmdLand.ClaimInProgressHandlerDelegate <>9__22;
			CmdLand.ClaimInProgressHandlerDelegate <>9__24;
			CmdLand.ClaimInProgressHandlerDelegate <>9__25;
			CmdLand.ClaimInProgressHandlerDelegate <>9__29;
			CmdLand.ClaimInProgressHandlerDelegate <>9__30;
			CmdLand.ClaimInProgressHandlerDelegate <>9__31;
			chatCommands.GetOrCreate("land").RequiresPrivilege(Privilege.chat).RequiresPlayer()
				.WithDesc("Manage land rights")
				.WithPreCondition(delegate(TextCommandCallingArgs args)
				{
					if (!server.SaveGameData.WorldConfiguration.GetBool("allowLandClaiming", true))
					{
						return TextCommandResult.Error(Lang.Get("Land claiming has been disabled by world configuration", Array.Empty<object>()), "");
					}
					return TextCommandResult.Success("", null);
				})
				.BeginSub("free")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Int("claim id"),
					parsers.OptionalBool("confirm", "confirm")
				})
				.HandleWith((TextCommandCallingArgs args) => <>4__this.freeLand(args.Caller.Player as IServerPlayer, (int)args[0], (bool)args[1]))
				.WithDesc("Remove a land claim of yours")
				.EndSub()
				.BeginSub("adminfree")
				.RequiresPrivilege(Privilege.commandplayer)
				.WithArgs(new ICommandArgumentParser[] { parsers.PlayerUids("player name") })
				.WithDesc("Delete all claims of selected player(s)")
				.HandleWith(new OnCommandDelegate(this.freeLandAdmin))
				.EndSub()
				.BeginSub("adminfreehere")
				.RequiresPrivilege(Privilege.commandplayer)
				.WithDesc("Remove a land claim at the calling position")
				.HandleWith(new OnCommandDelegate(this.freeLandAdminHere))
				.EndSub()
				.BeginSub("list")
				.WithDesc("List your claimed lands or retrieve information about a claim")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("land claim index", 0) })
				.HandleWith((TextCommandCallingArgs args) => <>4__this.landList(args.Caller.Player as IServerPlayer, args.Parsers[0].IsMissing ? null : ((int?)args[0])))
				.EndSub()
				.BeginSub("info")
				.WithDesc("Land rights information at your location")
				.HandleWith((TextCommandCallingArgs args) => <>4__this.landInfo(args.Caller.Player as IServerPlayer))
				.EndSub()
				.BeginSub("claim")
				.RequiresPrivilege(Privilege.claimland)
				.WithDesc("Add, Remove or Modify your claims")
				.BeginSub("load")
				.WithDesc("Load an existing claim")
				.WithArgs(new ICommandArgumentParser[] { parsers.Int("claim id") })
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					IServerPlayer plr = args.Caller.Player as IServerPlayer;
					List<LandClaim> ownclaims = CmdLand.GetPlayerClaims(server, plr.PlayerUID);
					int claimid = (int)args[0];
					if (claimid < 0 || claimid >= ownclaims.Count)
					{
						return TextCommandResult.Error(Lang.Get("Incorrect claimid, you only have {0} claims", new object[] { ownclaims.Count }), "");
					}
					<>4__this.TempClaims[plr] = new ClaimInProgress
					{
						Claim = ownclaims[claimid].Clone(),
						IsNew = false,
						OriginalClaim = ownclaims[claimid]
					};
					<>4__this.ResendHighlights(plr, <>4__this.TempClaims[plr].Claim);
					return TextCommandResult.Success(Lang.Get("Ok, claim loaded, you can now modify it", new object[] { plr.Role.LandClaimMaxAreas }), null);
				})
				.EndSub()
				.BeginSub("new")
				.WithDesc("Create a new claim")
				.HandleWith(delegate(TextCommandCallingArgs args)
				{
					IServerPlayer plr2 = args.Caller.Player as IServerPlayer;
					if (CmdLand.GetPlayerClaims(server, plr2.PlayerUID).Count >= plr2.Role.LandClaimMaxAreas + plr2.ServerData.ExtraLandClaimAreas)
					{
						return TextCommandResult.Error(Lang.Get("Sorry you can't have more than {0} separate claims", new object[] { plr2.Role.LandClaimMaxAreas }), "");
					}
					ClaimInProgress claimp = new ClaimInProgress
					{
						Claim = LandClaim.CreateClaim(plr2, plr2.Role.PrivilegeLevel),
						IsNew = true
					};
					<>4__this.TempClaims[plr2] = claimp;
					claimp.Start = plr2.Entity.Pos.XYZ.AsBlockPos;
					<>4__this.ResendHighlights(plr2, claimp.Claim);
					return TextCommandResult.Success(Lang.Get("Ok new claim initiated, use /land claim start, then /land claim end to mark an area, you can use /land claim grow [up|north|east|...] [size] to grow/shrink the selection, if you messed up use /land claim cancel, then finally /land claim add to add that area. You can add multiple areas as long as they are adjacent. Once all is ready, use /land claim save [text] to save the claim", Array.Empty<object>()), null);
				})
				.EndSub()
				.BeginSub("grant")
				.WithDesc("Grant a player access to your claim")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.PlayerUids("for player"),
					parsers.WordRange("permission type", new string[] { "traverse", "use", "all" })
				})
				.HandleWith((TextCommandCallingArgs ccargs) => CmdPlayer.Each(ccargs, new CmdPlayer.PlayerEachDelegate(<>4__this.handleGrant)))
				.EndSub()
				.BeginSub("revoke")
				.WithDesc("Revoke a player access on your claim")
				.WithArgs(new ICommandArgumentParser[] { parsers.PlayerUids("for player") })
				.HandleWith((TextCommandCallingArgs ccargs) => CmdPlayer.Each(ccargs, new CmdPlayer.PlayerEachDelegate(<>4__this.handleRevoke)))
				.EndSub()
				.BeginSub("grantgroup")
				.WithDesc("Grant a group access to your claim")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.Word("group name"),
					parsers.WordRange("permission type", new string[] { "traverse", "use", "all" })
				})
				.HandleWith(new OnCommandDelegate(this.handleGrantGroup))
				.EndSub()
				.BeginSub("revokegroup")
				.WithDesc("Revoke a group access on your claim")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("group name") })
				.HandleWith(new OnCommandDelegate(this.handleRevokeGroup))
				.EndSub()
				.BeginSub("grow")
				.WithDesc("Grow area in one of 6 directions (up/down/north/east/south/west)")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.WordRange("direction", new string[] { "up", "down", "north", "east", "south", "west" }),
					parsers.OptionalInt("amount", 1)
				})
				.HandleWith(delegate(TextCommandCallingArgs cargs)
				{
					CmdLand <>4__this8 = <>4__this;
					CmdLand.ClaimInProgressHandlerDelegate claimInProgressHandlerDelegate;
					if ((claimInProgressHandlerDelegate = <>9__20) == null)
					{
						claimInProgressHandlerDelegate = (<>9__20 = (TextCommandCallingArgs args, ClaimInProgress claimp) => <>4__this.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromCode((string)args[0]), (int)args[1]));
					}
					return <>4__this8.acquireClaimInProgress(cargs, claimInProgressHandlerDelegate);
				})
				.EndSub()
				.BeginSubs(new string[] { "gu", "gd", "gn", "ge", "gs", "gw" })
				.WithDesc("Grow area in one of 6 directions (gu/gd/gn/ge/gs/gw)")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("amount", 1) })
				.HandleWith((TextCommandCallingArgs cargs) => <>4__this.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => <>4__this.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromFirstLetter(cargs.SubCmdCode[1]), (int)args[0])))
				.EndSub()
				.BeginSub("shrink")
				.WithDesc("Shrink area in one of 6 directions (up/down/north/east/south/west)")
				.WithArgs(new ICommandArgumentParser[]
				{
					parsers.WordRange("direction", new string[] { "up", "down", "north", "east", "south", "west" }),
					parsers.OptionalInt("amount", 1)
				})
				.HandleWith(delegate(TextCommandCallingArgs cargs)
				{
					CmdLand <>4__this2 = <>4__this;
					CmdLand.ClaimInProgressHandlerDelegate claimInProgressHandlerDelegate2;
					if ((claimInProgressHandlerDelegate2 = <>9__22) == null)
					{
						claimInProgressHandlerDelegate2 = (<>9__22 = (TextCommandCallingArgs args, ClaimInProgress claimp) => <>4__this.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromCode((string)args[0]), -(int)args[1]));
					}
					return <>4__this2.acquireClaimInProgress(cargs, claimInProgressHandlerDelegate2);
				})
				.EndSub()
				.BeginSubs(new string[] { "su", "sd", "sn", "se", "ss", "sw" })
				.WithDesc("Shrink area in one of 6 directions (su/sd/sn/se/ss/sw)")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("amount", 1) })
				.HandleWith((TextCommandCallingArgs cargs) => <>4__this.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => <>4__this.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromFirstLetter(cargs.SubCmdCode[1]), -(int)args[0])))
				.EndSub()
				.BeginSub("start")
				.WithDesc("Set a start position for an area")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWorldPosition("position") })
				.HandleWith(delegate(TextCommandCallingArgs cargs)
				{
					CmdLand <>4__this3 = <>4__this;
					CmdLand.ClaimInProgressHandlerDelegate claimInProgressHandlerDelegate3;
					if ((claimInProgressHandlerDelegate3 = <>9__24) == null)
					{
						claimInProgressHandlerDelegate3 = (<>9__24 = delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
						{
							claimp.Start = (args[0] as Vec3d).AsBlockPos;
							<>4__this.ResendHighlights(args.Caller.Player, claimp.Claim, claimp.Start, claimp.End);
							return TextCommandResult.Success(Lang.Get("Ok, Land claim start position {0} set", new object[] { claimp.Start.ToLocalPosition(server.api) }), null);
						});
					}
					return <>4__this3.acquireClaimInProgress(cargs, claimInProgressHandlerDelegate3);
				})
				.EndSub()
				.BeginSub("end")
				.WithDesc("Set a end position for an area")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWorldPosition("position") })
				.HandleWith(delegate(TextCommandCallingArgs cargs)
				{
					CmdLand <>4__this4 = <>4__this;
					CmdLand.ClaimInProgressHandlerDelegate claimInProgressHandlerDelegate4;
					if ((claimInProgressHandlerDelegate4 = <>9__25) == null)
					{
						claimInProgressHandlerDelegate4 = (<>9__25 = delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
						{
							claimp.End = (args[0] as Vec3d).AsBlockPos;
							<>4__this.ResendHighlights(args.Caller.Player, claimp.Claim, claimp.Start, claimp.End);
							return TextCommandResult.Success(Lang.Get("Ok, Land claim end position {0} set", new object[] { claimp.End.ToLocalPosition(server.api) }), null);
						});
					}
					return <>4__this4.acquireClaimInProgress(cargs, claimInProgressHandlerDelegate4);
				})
				.EndSub()
				.BeginSub("add")
				.WithDesc("Add current area to the claim")
				.HandleWith(new OnCommandDelegate(this.addCurrentArea))
				.EndSub()
				.BeginSub("allowuseeveryone")
				.WithDesc("Grant use privilege to all players")
				.WithArgs(new ICommandArgumentParser[] { parsers.Bool("on/off", "on") })
				.HandleWith((TextCommandCallingArgs cargs) => <>4__this.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
				{
					claimp.Claim.AllowUseEveryone = (bool)args[0];
					return TextCommandResult.Success(Lang.Get("Ok, allow use everyone is now {0}", new object[] { claimp.Claim.AllowUseEveryone ? "on" : "off" }), null);
				}))
				.EndSub()
				.BeginSub("allowtraverseveryone")
				.WithDesc("Grant traverse privilege to all players")
				.WithArgs(new ICommandArgumentParser[] { parsers.Bool("on/off", "on") })
				.HandleWith((TextCommandCallingArgs cargs) => <>4__this.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
				{
					claimp.Claim.AllowTraverseEveryone = (bool)args[0];
					return TextCommandResult.Success(Lang.Get("Ok, allow traverse everyone is now {0}", new object[] { claimp.Claim.AllowTraverseEveryone ? "on" : "off" }), null);
				}))
				.EndSub()
				.BeginSub("plevel")
				.WithDesc("Set protection level on your current claim")
				.WithArgs(new ICommandArgumentParser[] { parsers.Int("protection level") })
				.HandleWith((TextCommandCallingArgs cargs) => <>4__this.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
				{
					claimp.Claim.ProtectionLevel = (int)args[0];
					return TextCommandResult.Success(Lang.Get("Ok, protection level set to {0}", new object[] { claimp.Claim.ProtectionLevel }), null);
				}))
				.EndSub()
				.BeginSub("fullheight")
				.WithDesc("Expand claim to cover the entire map height")
				.HandleWith(delegate(TextCommandCallingArgs cargs)
				{
					CmdLand <>4__this5 = <>4__this;
					CmdLand.ClaimInProgressHandlerDelegate claimInProgressHandlerDelegate5;
					if ((claimInProgressHandlerDelegate5 = <>9__29) == null)
					{
						claimInProgressHandlerDelegate5 = (<>9__29 = delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
						{
							if (claimp.Start == null || claimp.End == null)
							{
								return TextCommandResult.Error(Lang.Get("Define start and end position first", Array.Empty<object>()), "");
							}
							claimp.Start.Y = 0;
							claimp.End.Y = server.WorldMap.MapSizeY;
							<>4__this.ResendHighlights(args.Caller.Player, claimp.Claim, claimp.Start, claimp.End);
							return TextCommandResult.Success(Lang.Get("Ok, extended land claim to cover full world height", Array.Empty<object>()), null);
						});
					}
					return <>4__this5.acquireClaimInProgress(cargs, claimInProgressHandlerDelegate5);
				})
				.EndSub()
				.BeginSub("save")
				.WithDesc("Save your currently edited claim")
				.WithArgs(new ICommandArgumentParser[] { parsers.All("description") })
				.HandleWith(delegate(TextCommandCallingArgs cargs)
				{
					CmdLand <>4__this6 = <>4__this;
					CmdLand.ClaimInProgressHandlerDelegate claimInProgressHandlerDelegate6;
					if ((claimInProgressHandlerDelegate6 = <>9__30) == null)
					{
						claimInProgressHandlerDelegate6 = (<>9__30 = delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
						{
							if (claimp.Claim.Areas.Count == 0)
							{
								return TextCommandResult.Error(Lang.Get("Cannot save an empty claim. Did you forget to type /land claim add?", Array.Empty<object>()), "");
							}
							claimp.Claim.Description = (string)args[0];
							if (claimp.IsNew)
							{
								server.WorldMap.Add(claimp.Claim);
							}
							else
							{
								server.WorldMap.UpdateClaim(claimp.OriginalClaim, claimp.Claim);
							}
							IPlayer fromPlayer = args.Caller.Player;
							<>4__this.TempClaims[fromPlayer] = null;
							<>4__this.ResendHighlights(fromPlayer, null);
							return TextCommandResult.Success("Ok, Land claim saved on your name", null);
						});
					}
					return <>4__this6.acquireClaimInProgress(cargs, claimInProgressHandlerDelegate6);
				})
				.EndSub()
				.BeginSub("cancel")
				.WithDesc("Discard changes on currently edited claim")
				.HandleWith(delegate(TextCommandCallingArgs cargs)
				{
					CmdLand <>4__this7 = <>4__this;
					CmdLand.ClaimInProgressHandlerDelegate claimInProgressHandlerDelegate7;
					if ((claimInProgressHandlerDelegate7 = <>9__31) == null)
					{
						claimInProgressHandlerDelegate7 = (<>9__31 = delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
						{
							IPlayer fromPlayer2 = args.Caller.Player;
							if (!<>4__this.TempClaims.ContainsKey(fromPlayer2))
							{
								return TextCommandResult.Error("No current land claim changes active", "");
							}
							<>4__this.TempClaims[fromPlayer2] = null;
							<>4__this.ResendHighlights(fromPlayer2, null);
							return TextCommandResult.Success("Ok, Land claim changes cancelled", null);
						});
					}
					return <>4__this7.acquireClaimInProgress(cargs, claimInProgressHandlerDelegate7);
				})
				.EndSub()
				.EndSub()
				.Validate();
		}

		private TextCommandResult handleRevokeGroup(TextCommandCallingArgs cargs)
		{
			return this.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				string groupname = (string)args[0];
				PlayerGroup group = this.server.PlayerDataManager.GetPlayerGroupByName(groupname);
				if (group != null && claimp.Claim.PermittedPlayerGroupIds.ContainsKey(group.Uid))
				{
					claimp.Claim.PermittedPlayerGroupIds.Remove(group.Uid);
					return TextCommandResult.Success("Ok, revoked access to group " + groupname, null);
				}
				return TextCommandResult.Error("No such group has access to your claim", "");
			});
		}

		private TextCommandResult handleGrantGroup(TextCommandCallingArgs cargs)
		{
			return this.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				string groupname = (string)args[0];
				EnumBlockAccessFlags flags = EnumBlockAccessFlags.Use | EnumBlockAccessFlags.Traverse;
				string stringFlag = (string)args[1];
				if (stringFlag == "all")
				{
					flags = EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use | EnumBlockAccessFlags.Traverse;
				}
				else if (stringFlag == "traverse")
				{
					flags = EnumBlockAccessFlags.Traverse;
				}
				PlayerGroup group = this.server.PlayerDataManager.GetPlayerGroupByName(groupname);
				if (group != null)
				{
					claimp.Claim.PermittedPlayerGroupIds[group.Uid] = flags;
					return TextCommandResult.Success("Ok, granted access to group " + groupname, null);
				}
				return TextCommandResult.Error("No such group found", "");
			});
		}

		private TextCommandResult handleGrant(PlayerUidName forPlayer, TextCommandCallingArgs cargs)
		{
			return this.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				EnumBlockAccessFlags flags = EnumBlockAccessFlags.Use | EnumBlockAccessFlags.Traverse;
				string stringFlag = (string)args[1];
				if (stringFlag == "all")
				{
					flags = EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use | EnumBlockAccessFlags.Traverse;
				}
				else if (stringFlag == "traverse")
				{
					flags = EnumBlockAccessFlags.Traverse;
				}
				claimp.Claim.PermittedPlayerUids[forPlayer.Uid] = flags;
				claimp.Claim.PermittedPlayerLastKnownPlayerName[forPlayer.Uid] = forPlayer.Name;
				return TextCommandResult.Success(Lang.Get("Ok, player {0} granted {1} access to your claim.", new object[]
				{
					forPlayer.Name,
					args[1]
				}), null);
			});
		}

		private TextCommandResult handleRevoke(PlayerUidName forPlayer, TextCommandCallingArgs cargs)
		{
			return this.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				if (claimp.Claim.PermittedPlayerUids.ContainsKey(forPlayer.Uid))
				{
					claimp.Claim.PermittedPlayerUids.Remove(forPlayer.Uid);
					return TextCommandResult.Success(Lang.Get("Ok, revoked access to player {0}.", new object[] { forPlayer.Name }), null);
				}
				return TextCommandResult.Success(Lang.Get("Player {0} had no access to your claim.", new object[] { forPlayer.Name }), null);
			});
		}

		private TextCommandResult acquireClaimInProgress(TextCommandCallingArgs cargs, CmdLand.ClaimInProgressHandlerDelegate handler)
		{
			ClaimInProgress claimp;
			if (this.TempClaims.TryGetValue(cargs.Caller.Player, out claimp) && claimp != null)
			{
				return handler(cargs, claimp);
			}
			return TextCommandResult.Success(Lang.Get("No current or incomplete claim, type '/land claim new' to prepare a new one or '/land claim load [id]' to modify an existing one. The id can be retrieved from /land list", Array.Empty<object>()), null);
		}

		private TextCommandResult landInfo(IServerPlayer player)
		{
			List<LandClaim> claims = this.server.WorldMap.All;
			List<string> claimTexts = new List<string>();
			bool haveBuildAccess = false;
			bool haveUseAccess = false;
			bool inPartionedClaim = false;
			bool inListClaim = false;
			foreach (LandClaim claim in claims)
			{
				if (claim.PositionInside(player.Entity.ServerPos.XYZ))
				{
					claimTexts.Add(claim.LastKnownOwnerName);
					inListClaim = true;
					if (claim.TestPlayerAccess(player, EnumBlockAccessFlags.BuildOrBreak) != EnumPlayerAccessResult.Denied)
					{
						haveBuildAccess = true;
					}
					if (claim.TestPlayerAccess(player, EnumBlockAccessFlags.Use) != EnumPlayerAccessResult.Denied)
					{
						haveUseAccess = true;
						break;
					}
					break;
				}
			}
			long regionindex = this.server.WorldMap.MapRegionIndex2D(player.Entity.ServerPos.XInt / this.server.WorldMap.RegionSize, player.Entity.ServerPos.ZInt / this.server.WorldMap.RegionSize);
			if (this.server.WorldMap.LandClaimByRegion.ContainsKey(regionindex))
			{
				using (List<LandClaim>.Enumerator enumerator = this.server.WorldMap.LandClaimByRegion[regionindex].GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.PositionInside(player.Entity.ServerPos.XYZ))
						{
							inPartionedClaim = true;
							break;
						}
					}
				}
			}
			if (inPartionedClaim != inListClaim)
			{
				return TextCommandResult.Error(string.Format("Incorrect state. Spatially partitioned claim list not consistent with full claim list. Please contact the game developer. A server restart may temporarily fix the issue. (in partition: {0}, in listclaim: {1})", inPartionedClaim, inListClaim), "");
			}
			string privilegeInfo = "";
			if (player.HasPrivilege(Privilege.readlists))
			{
				foreach (LandClaim claim2 in claims)
				{
					if (claim2.PositionInside(player.Entity.ServerPos.XYZ))
					{
						int protLevel = claim2.ProtectionLevel;
						StringBuilder plrsReadable = new StringBuilder();
						foreach (KeyValuePair<string, EnumBlockAccessFlags> val in claim2.PermittedPlayerUids)
						{
							if (plrsReadable.Length > 0)
							{
								plrsReadable.Append(", ");
							}
							ServerPlayerData plrdata = this.server.GetServerPlayerData(val.Key);
							if (plrdata != null)
							{
								plrsReadable.Append(string.Format("{0} can {1}", plrdata.LastKnownPlayername, val.Value));
							}
							else
							{
								plrsReadable.Append(string.Format("{0} can {1}", val.Key, val.Value));
							}
						}
						if (plrsReadable.Length == 0)
						{
							plrsReadable.Append("None.");
						}
						StringBuilder groupsReadable = new StringBuilder();
						foreach (KeyValuePair<int, EnumBlockAccessFlags> val2 in claim2.PermittedPlayerGroupIds)
						{
							if (groupsReadable.Length > 0)
							{
								groupsReadable.Append(", ");
							}
							PlayerGroup group;
							this.server.PlayerDataManager.PlayerGroupsById.TryGetValue(val2.Key, out group);
							if (group != null)
							{
								groupsReadable.Append(string.Format("{0} can {1}", group.Name, val2.Value));
							}
							else
							{
								groupsReadable.Append(string.Format("{0} can {1}", val2.Key, val2.Value));
							}
						}
						if (groupsReadable.Length == 0)
						{
							groupsReadable.Append("None.");
						}
						string text = "\n";
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 2);
						defaultInterpolatedStringHandler.AppendLiteral("AllowUseEveryone: ");
						defaultInterpolatedStringHandler.AppendFormatted<bool>(claim2.AllowUseEveryone);
						defaultInterpolatedStringHandler.AppendLiteral(", AllowTraverseEveryone: ");
						defaultInterpolatedStringHandler.AppendFormatted<bool>(claim2.AllowTraverseEveryone);
						privilegeInfo = text + defaultInterpolatedStringHandler.ToStringAndClear() + "\n" + string.Format("Protection level: {0}, Granted Players: {1}, Granted Groups: {2}", protLevel, plrsReadable.ToString(), groupsReadable.ToString());
					}
				}
			}
			if (claimTexts.Count > 0)
			{
				string useText = Lang.Get("You don't have access to it.", Array.Empty<object>());
				if (haveBuildAccess && haveUseAccess)
				{
					useText = Lang.Get("You have build and use access.", Array.Empty<object>());
				}
				else
				{
					if (haveBuildAccess)
					{
						useText = Lang.Get("You have build access.", Array.Empty<object>());
					}
					if (haveUseAccess)
					{
						useText = Lang.Get("You have use access.", Array.Empty<object>());
					}
				}
				return TextCommandResult.Success(Lang.Get("These lands are claimed by {0}. {1}", new object[]
				{
					string.Join(", ", claimTexts),
					useText
				}) + privilegeInfo, null);
			}
			return TextCommandResult.Success(Lang.Get("These lands are not claimed by anybody", Array.Empty<object>()) + privilegeInfo, null);
		}

		private TextCommandResult freeLand(IServerPlayer player, int claimid, bool confirm)
		{
			List<LandClaim> all = this.server.WorldMap.All;
			List<LandClaim> ownclaims = new List<LandClaim>();
			foreach (LandClaim claim in all)
			{
				if (claim.OwnedByPlayerUid == player.PlayerUID)
				{
					ownclaims.Add(claim);
				}
			}
			if (claimid < 0 || claimid >= ownclaims.Count)
			{
				return TextCommandResult.Error(Lang.Get("Claim number too wrong, you only have {0} claims", new object[] { ownclaims.Count }), "");
			}
			LandClaim todeleteclaim = ownclaims[claimid];
			if (!confirm)
			{
				return TextCommandResult.Success(Lang.Get("command-deleteclaim-confirmation", new object[] { todeleteclaim.Description, todeleteclaim.SizeXYZ, claimid }), null);
			}
			this.server.WorldMap.Remove(todeleteclaim);
			return TextCommandResult.Success(Lang.Get("Ok, claim removed", Array.Empty<object>()), null);
		}

		private TextCommandResult freeLandAdmin(TextCommandCallingArgs args)
		{
			PlayerUidName[] array = (PlayerUidName[])args[0];
			List<LandClaim> allclaims = this.server.WorldMap.All;
			int qremoved = 0;
			List<string> playernames = new List<string>();
			foreach (PlayerUidName player in array)
			{
				playernames.Add(player.Name);
				foreach (LandClaim claim in new List<LandClaim>(allclaims))
				{
					if (claim.OwnedByPlayerUid == player.Uid && this.server.WorldMap.Remove(claim))
					{
						qremoved++;
					}
				}
			}
			return TextCommandResult.Success(Lang.Get("Ok, {0} claims removed from {1}", new object[]
			{
				qremoved,
				string.Join(", ", playernames)
			}), null);
		}

		private TextCommandResult freeLandAdminHere(TextCommandCallingArgs args)
		{
			Vec3d srcPos = args.Caller.Pos;
			long regionindex = this.server.WorldMap.MapRegionIndex2D(srcPos.XInt / this.server.WorldMap.RegionSize, srcPos.ZInt / this.server.WorldMap.RegionSize);
			if (this.server.WorldMap.LandClaimByRegion.ContainsKey(regionindex))
			{
				foreach (LandClaim claim in this.server.WorldMap.LandClaimByRegion[regionindex])
				{
					if (claim.PositionInside(srcPos))
					{
						this.server.WorldMap.Remove(claim);
						return TextCommandResult.Success(Lang.Get("Ok, Removed claim from {0}", new object[] { claim.LastKnownOwnerName }), null);
					}
				}
			}
			return TextCommandResult.Error(Lang.Get("No claim found at this position", Array.Empty<object>()), "nonefound");
		}

		private TextCommandResult landList(IServerPlayer player, int? index)
		{
			List<LandClaim> claims = this.server.WorldMap.All;
			if (index == null)
			{
				List<string> playerOwnedTexts = new List<string>();
				List<string> groupOwnedTexts = new List<string>();
				int i = 0;
				PlayerGroupMembership[] groups = player.Groups;
				bool allowCoordinateHud = this.server.api.World.Config.GetBool("allowCoordinateHud", true);
				using (List<LandClaim>.Enumerator enumerator = claims.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						LandClaim claim = enumerator.Current;
						BlockPos center = claim.Center;
						center = center.Copy().Sub(this.server.DefaultSpawnPosition.XYZ.AsBlockPos);
						if (claim.OwnedByPlayerUid == player.PlayerUID)
						{
							if (allowCoordinateHud)
							{
								playerOwnedTexts.Add(Lang.Get("{0}: {1} ({2}m³ at {3})", new object[] { i, claim.Description, claim.SizeXYZ, center }));
							}
							else
							{
								playerOwnedTexts.Add(Lang.Get("{0}: {1} ({2}m³)", new object[] { i, claim.Description, claim.SizeXYZ }));
							}
							i++;
						}
						if (groups.Any((PlayerGroupMembership g) => g.GroupName.Equals(claim.OwnedByPlayerGroupUid)))
						{
							if (allowCoordinateHud)
							{
								groupOwnedTexts.Add(Lang.Get("{0}: {1} ({2}m³ at {3}) (group owned)", new object[] { i, claim.Description, claim.SizeXYZ, center }));
							}
							else
							{
								groupOwnedTexts.Add(Lang.Get("{0}: {1} ({2}m³) (group owned)", new object[] { i, claim.Description, claim.SizeXYZ }));
							}
						}
					}
				}
				return TextCommandResult.Success(Lang.Get("land-claim-list", new object[] { string.Join("\n", playerOwnedTexts) }), null);
			}
			LandClaim ownClaim = null;
			int j = 0;
			int claimId = index.Value;
			foreach (LandClaim claim2 in claims)
			{
				if (!(claim2.OwnedByPlayerUid != player.PlayerUID))
				{
					if (claimId == j)
					{
						ownClaim = claim2;
						break;
					}
					j++;
				}
			}
			if (ownClaim == null)
			{
				return TextCommandResult.Error("No such claim", "");
			}
			BlockPos center2 = ownClaim.Center;
			center2 = center2.Copy().Sub(this.server.DefaultSpawnPosition.XYZ.AsBlockPos);
			string claimInfo = Lang.Get("{0} ({1}m³ at {2})", new object[] { ownClaim.Description, ownClaim.SizeXYZ, center2 });
			StringBuilder extPrivs = new StringBuilder();
			if (ownClaim.PermittedPlayerUids.Count > 0)
			{
				foreach (KeyValuePair<string, EnumBlockAccessFlags> val in ownClaim.PermittedPlayerUids)
				{
					string playeruid = val.Key;
					string playername;
					if (!ownClaim.PermittedPlayerLastKnownPlayerName.TryGetValue(playeruid, out playername))
					{
						playername = playeruid;
					}
					bool build = (val.Value & EnumBlockAccessFlags.BuildOrBreak) > EnumBlockAccessFlags.None;
					bool use = (val.Value & EnumBlockAccessFlags.Use) > EnumBlockAccessFlags.None;
					string privs = ((build && use) ? Lang.Get("Player {0} can build/break and use blocks", new object[] { playername }) : (build ? Lang.Get("Player {0} can build/break but not use blocks", new object[] { playername }) : Lang.Get("Player {0} can use but not build/break blocks", new object[] { playername })));
					extPrivs.AppendLine(privs);
				}
			}
			if (ownClaim.PermittedPlayerGroupIds.Count > 0)
			{
				Dictionary<int, PlayerGroup> allGroups = this.server.PlayerDataManager.PlayerGroupsById;
				foreach (KeyValuePair<int, EnumBlockAccessFlags> val2 in ownClaim.PermittedPlayerGroupIds)
				{
					int groupid = val2.Key;
					PlayerGroup group;
					if (allGroups.TryGetValue(groupid, out group))
					{
						bool build2 = (val2.Value & EnumBlockAccessFlags.BuildOrBreak) > EnumBlockAccessFlags.None;
						bool use2 = (val2.Value & EnumBlockAccessFlags.Use) > EnumBlockAccessFlags.None;
						string privs2 = ((build2 && use2) ? Lang.Get("Group {0} can build/break and use blocks", new object[] { group.Name }) : (build2 ? Lang.Get("Group {0} can build/break but not use blocks", new object[] { group.Name }) : Lang.Get("Group {0} can use but not build/break blocks", new object[] { group.Name })));
						extPrivs.AppendLine(privs2);
					}
				}
			}
			return TextCommandResult.Success(claimInfo + "\r\n" + ((extPrivs.Length == 0) ? Lang.Get("No other players/groups have access to this claim", Array.Empty<object>()) : extPrivs.ToString()), null);
		}

		private TextCommandResult addCurrentArea(TextCommandCallingArgs cargs)
		{
			List<LandClaim> allclaims = this.server.WorldMap.All;
			IServerPlayer fromPlayer = cargs.Caller.Player as IServerPlayer;
			List<LandClaim> ownclaims = CmdLand.GetPlayerClaims(this.server, cargs.Caller.Player.PlayerUID);
			return this.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				if (claimp.Start == null || claimp.End == null)
				{
					return TextCommandResult.Error(Lang.Get("Start or End not marked", Array.Empty<object>()), "");
				}
				Cuboidi area = new Cuboidi(claimp.Start, claimp.End);
				if (area.SizeX < fromPlayer.Role.LandClaimMinSize.X || area.SizeY < fromPlayer.Role.LandClaimMinSize.Y || area.SizeZ < fromPlayer.Role.LandClaimMinSize.Z)
				{
					return TextCommandResult.Error(Lang.Get("Cannot add area. Your marked area has a size of {0}x{1}x{2} which is to small, needs to be at least {3}x{4}x{5}", new object[]
					{
						area.SizeX,
						area.SizeY,
						area.SizeZ,
						fromPlayer.Role.LandClaimMinSize.X,
						fromPlayer.Role.LandClaimMinSize.Y,
						fromPlayer.Role.LandClaimMinSize.Z
					}), "");
				}
				int totalSize = area.SizeXYZ;
				foreach (LandClaim claim in ownclaims)
				{
					totalSize += claim.SizeXYZ;
				}
				if ((long)totalSize > (long)fromPlayer.Role.LandClaimAllowance + (long)fromPlayer.ServerData.ExtraLandClaimAllowance)
				{
					return TextCommandResult.Error(Lang.Get("Cannot add area. Adding this area of size {0}m³ would bring your total claim size up to {1}m³, but your max allowance is {2}m³", new object[]
					{
						area.SizeXYZ,
						totalSize,
						fromPlayer.Role.LandClaimAllowance
					}), "");
				}
				for (int i = 0; i < allclaims.Count; i++)
				{
					if (allclaims[i].Intersects(area))
					{
						return TextCommandResult.Error(Lang.Get("Cannot add area. This area overlaps with with another claim by {0}. Please correct your start/end position", new object[] { allclaims[i].LastKnownOwnerName }), "");
					}
				}
				EnumClaimError err = claimp.Claim.AddArea(area);
				if (err != EnumClaimError.NoError)
				{
					return TextCommandResult.Error((err == EnumClaimError.Overlapping) ? Lang.Get("Cannot add area. This area overlaps with your other claims. Please correct your start/end position", Array.Empty<object>()) : Lang.Get("Cannot add area. This area is not adjacent to other claims. Please correct your start/end position", Array.Empty<object>()), "");
				}
				claimp.Start = null;
				claimp.End = null;
				this.ResendHighlights(fromPlayer, claimp.Claim, claimp.Start, claimp.End);
				return TextCommandResult.Success(Lang.Get("Ok, Land claim area added", Array.Empty<object>()), null);
			});
		}

		private TextCommandResult GrowSelection(IPlayer plr, ClaimInProgress claimp, BlockFacing facing, int size)
		{
			if (claimp.Start == null || claimp.End == null)
			{
				return TextCommandResult.Error(Lang.Get("Define start and end position first", Array.Empty<object>()), "");
			}
			if (facing == BlockFacing.UP)
			{
				if (claimp.Start.Y < claimp.End.Y)
				{
					claimp.End.Y += size;
				}
				else
				{
					claimp.Start.Y += size;
				}
			}
			if (facing == BlockFacing.DOWN)
			{
				if (claimp.Start.Y < claimp.End.Y)
				{
					claimp.Start.Y -= size;
				}
				else
				{
					claimp.End.Y -= size;
				}
			}
			if (facing == BlockFacing.NORTH)
			{
				if (claimp.Start.Z < claimp.End.Z)
				{
					claimp.Start.Z -= size;
				}
				else
				{
					claimp.End.Z -= size;
				}
			}
			if (facing == BlockFacing.EAST)
			{
				if (claimp.Start.X > claimp.End.X)
				{
					claimp.Start.X += size;
				}
				else
				{
					claimp.End.X += size;
				}
			}
			if (facing == BlockFacing.WEST)
			{
				if (claimp.Start.X < claimp.End.X)
				{
					claimp.Start.X -= size;
				}
				else
				{
					claimp.End.X -= size;
				}
			}
			if (facing == BlockFacing.SOUTH)
			{
				if (claimp.Start.Z > claimp.End.Z)
				{
					claimp.Start.Z += size;
				}
				else
				{
					claimp.End.Z += size;
				}
			}
			this.ResendHighlights(plr, claimp.Claim, claimp.Start, claimp.End);
			return TextCommandResult.Success(Lang.Get("Ok, area extended {0} by {1} blocks", new object[] { facing, size }), null);
		}

		private void ResendHighlights(IPlayer toPlayer, LandClaim claim)
		{
			this.ResendHighlights(toPlayer, claim, null, null);
		}

		private void ResendHighlights(IPlayer toPlayer, LandClaim claim, BlockPos claimingStartPos, BlockPos claimingEndPos)
		{
			List<BlockPos> startEnds = new List<BlockPos>();
			List<int> colors = new List<int>();
			if (claim != null)
			{
				foreach (Cuboidi area in claim.Areas)
				{
					startEnds.Add(area.Start.ToBlockPos());
					startEnds.Add(area.End.ToBlockPos());
					colors.Add(this.claimedColor);
				}
			}
			if (claimingStartPos != null && claimingEndPos != null)
			{
				startEnds.Add(claimingStartPos);
				startEnds.Add(claimingEndPos);
				colors.Add(this.claimingColor);
			}
			this.server.api.World.HighlightBlocks(toPlayer, 3, startEnds, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes, 1f);
		}

		public static List<LandClaim> GetPlayerClaims(ServerMain server, string playerUid)
		{
			List<LandClaim> all = server.WorldMap.All;
			List<LandClaim> ownclaims = new List<LandClaim>();
			foreach (LandClaim claim in all)
			{
				if (claim.OwnedByPlayerUid == playerUid)
				{
					ownclaims.Add(claim);
				}
			}
			return ownclaims;
		}

		private ServerMain server;

		private Dictionary<IPlayer, ClaimInProgress> TempClaims = new Dictionary<IPlayer, ClaimInProgress>();

		private int claimedColor = ColorUtil.ToRgba(64, 100, 255, 100);

		private int claimingColor = ColorUtil.ToRgba(64, 148, 210, 246);

		public delegate TextCommandResult ClaimInProgressHandlerDelegate(TextCommandCallingArgs args, ClaimInProgress claimp);
	}
}
