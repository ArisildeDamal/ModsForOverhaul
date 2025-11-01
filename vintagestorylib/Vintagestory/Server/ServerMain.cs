using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server.Network;
using Vintagestory.Server.Systems;
using VintagestoryLib.Server.Systems;
using VSPlatform;

namespace Vintagestory.Server
{
	public sealed class ServerMain : GameMain, IServerWorldAccessor, IWorldAccessor, IShutDownMonitor
	{
		private void HandleRequestJoin(Packet_Client packet, ConnectedClient client)
		{
			ServerMain.FrameProfiler.Mark("reqjoin-before");
			ServerMain.Logger.VerboseDebug("HandleRequestJoin: Begin. Player: {0}", new object[] { (client != null) ? client.PlayerName : null });
			ServerPlayer player = client.Player;
			player.LanguageCode = packet.RequestJoin.Language ?? Lang.CurrentLocale;
			if (client.IsSinglePlayerClient)
			{
				player.serverdata.RoleCode = this.Config.Roles.MaxBy((PlayerRole v) => v.PrivilegeLevel).Code;
			}
			ServerMain.Logger.VerboseDebug("HandleRequestJoin: Before set name");
			client.Entityplayer.SetName(player.PlayerName);
			this.api.networkapi.SendChannelsPacket(player);
			this.SendPacket(player, ServerPackets.LevelInitialize(this.Config.MaxChunkRadius * MagicNum.ServerChunkSize));
			ServerMain.Logger.VerboseDebug("HandleRequestJoin: After Level initialize");
			this.SendLevelProgress(player, 100, Lang.Get("Generating world...", Array.Empty<object>()));
			ServerMain.FrameProfiler.Mark("reqjoin-1");
			this.SendWorldMetaData(player);
			ServerMain.FrameProfiler.Mark("reqjoin-2");
			this.SendServerAssets(player);
			ServerMain.FrameProfiler.Mark("reqjoin-3");
			client.ServerAssetsSent = true;
			using (FastMemoryStream ms = new FastMemoryStream())
			{
				this.SendPlayerEntities(player, ms);
				ServerMain.FrameProfiler.Mark("reqjoin-4");
				ServerSystem[] array = this.Systems;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].OnPlayerJoin(player);
				}
				this.EventManager.TriggerPlayerJoin(player);
				this.BroadcastPlayerData(player, true, true);
				foreach (ConnectedClient oclient in this.Clients.Values)
				{
					if (oclient != client && oclient.Entityplayer != null)
					{
						ms.Reset();
						this.SendInitialPlayerDataForOthers(oclient.Player, player, ms);
					}
				}
				LoggerBase logger = ServerMain.Logger;
				string text = "HandleRequestJoin: After broadcastplayerdata. hotbarslot: ";
				ItemSlot activeHotbarSlot = player.inventoryMgr.ActiveHotbarSlot;
				logger.VerboseDebug(text + ((activeHotbarSlot != null) ? activeHotbarSlot.ToString() : null));
				PlayerInventoryManager inventoryMgr = player.inventoryMgr;
				ItemStack itemStack;
				if (inventoryMgr == null)
				{
					itemStack = null;
				}
				else
				{
					ItemSlot activeHotbarSlot2 = inventoryMgr.ActiveHotbarSlot;
					itemStack = ((activeHotbarSlot2 != null) ? activeHotbarSlot2.Itemstack : null);
				}
				ItemStack hotbarstack = itemStack;
				EntityPlayer entity = player.Entity;
				ItemStack itemStack2;
				if (entity == null)
				{
					itemStack2 = null;
				}
				else
				{
					ItemSlot leftHandItemSlot = entity.LeftHandItemSlot;
					itemStack2 = ((leftHandItemSlot != null) ? leftHandItemSlot.Itemstack : null);
				}
				ItemStack offstack = itemStack2;
				this.SendPacket(player, new Packet_Server
				{
					SelectedHotbarSlot = new Packet_SelectedHotbarSlot
					{
						SlotNumber = player.InventoryManager.ActiveHotbarSlotNumber,
						ClientId = player.ClientId,
						Itemstack = ((hotbarstack == null) ? null : StackConverter.ToPacket(hotbarstack)),
						OffhandStack = ((offstack == null) ? null : StackConverter.ToPacket(offstack))
					},
					Id = 53
				});
				this.SendPacket(player, ServerPackets.LevelFinalize());
				ServerMain.Logger.VerboseDebug("HandleRequestJoin: After LevelFinalize");
				if (client.IsNewEntityPlayer)
				{
					this.EventManager.TriggerPlayerCreate(client.Player);
				}
				array = this.Systems;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].OnPlayerJoinPost(player);
				}
				ServerMain.FrameProfiler.Mark("reqjoin-after");
			}
		}

		private void HandleClientLoaded(Packet_Client packet, ConnectedClient client)
		{
			ServerPlayer player = client.Player;
			player.Entity.WatchedAttributes.MarkAllDirty();
			client.State = EnumClientState.Connected;
			client.MillisecsAtConnect = this.totalUnpausedTime.ElapsedMilliseconds;
			this.SendMessageToGeneral(Lang.Get("{0} joined. Say hi :)", new object[] { player.PlayerName }), EnumChatType.JoinLeave, player, null);
			ServerMain.Logger.Event(string.Format("{0} {1} joins.", player.PlayerName, client.Socket.RemoteEndPoint()));
			string msg = string.Format(this.Config.WelcomeMessage.Replace("{playername}", "{0}"), player.PlayerName);
			this.SendMessage(player, GlobalConstants.GeneralChatGroup, msg, EnumChatType.Notification, null);
			this.EventManager.TriggerPlayerNowPlaying(client.Player);
			if (this.Config.RepairMode)
			{
				this.SendMessage(player, GlobalConstants.GeneralChatGroup, "Server is in repair mode, you are now in spectator mode. If you are not already there, fly to the area that crashes and let the chunks load, then exit the game and run in normal mode.", EnumChatType.Notification, null);
				client.Player.WorldData.CurrentGameMode = EnumGameMode.Spectator;
				client.Player.WorldData.NoClip = true;
				client.Player.WorldData.FreeMove = true;
				client.Player.WorldData.MoveSpeedMultiplier = 1f;
				this.broadCastModeChange(client.Player);
			}
			this.SendRoles(player);
		}

		public void SendRoles(IServerPlayer player)
		{
			Packet_Roles roles = new Packet_Roles();
			roles.SetRoles(this.Config.RolesByCode.Select((KeyValuePair<string, PlayerRole> val) => new Packet_Role
			{
				Code = val.Value.Code,
				PrivilegeLevel = val.Value.PrivilegeLevel
			}).ToArray<Packet_Role>());
			this.SendPacket(player, new Packet_Server
			{
				Id = 76,
				Roles = roles
			});
		}

		public void BroadcastRoles()
		{
			Packet_Roles roles = new Packet_Roles();
			roles.SetRoles(this.Config.RolesByCode.Select((KeyValuePair<string, PlayerRole> val) => new Packet_Role
			{
				Code = val.Value.Code,
				PrivilegeLevel = val.Value.PrivilegeLevel
			}).ToArray<Packet_Role>());
			this.BroadcastPacket(new Packet_Server
			{
				Id = 76,
				Roles = roles
			}, Array.Empty<IServerPlayer>());
		}

		public void broadCastModeChange(IServerPlayer player)
		{
			this.BroadcastPacket(new Packet_Server
			{
				Id = 46,
				ModeChange = new Packet_PlayerMode
				{
					PlayerUID = player.PlayerUID,
					FreeMove = ((player.WorldData.FreeMove > false) ? 1 : 0),
					GameMode = (int)player.WorldData.CurrentGameMode,
					MoveSpeed = CollectibleNet.SerializeFloat(player.WorldData.MoveSpeedMultiplier),
					NoClip = ((player.WorldData.NoClip > false) ? 1 : 0),
					ViewDistance = player.WorldData.LastApprovedViewDistance,
					PickingRange = CollectibleNet.SerializeFloat(player.WorldData.PickingRange),
					FreeMovePlaneLock = (int)player.WorldData.FreeMovePlaneLock
				}
			}, Array.Empty<IServerPlayer>());
		}

		private void HandleClientPlaying(Packet_Client packet, ConnectedClient client)
		{
			client.State = EnumClientState.Playing;
			this.WorldMap.SendClaims(client.Player, this.WorldMap.All, null);
		}

		private void HandleRequestModeChange(Packet_Client p, ConnectedClient client)
		{
			Packet_PlayerMode packet = p.RequestModeChange;
			int clientid = client.Id;
			string playerUid = packet.PlayerUID;
			ConnectedClient targetClient = this.GetClientByUID(playerUid);
			if (client.Player == null)
			{
				ServerMain.Logger.Notification("Mode change request from a player without player object?! Ignoring.");
				return;
			}
			if (targetClient == null)
			{
				this.ReplyMessage(client.Player, "No such target client found.", EnumChatType.CommandError, null);
				return;
			}
			ServerWorldPlayerData playerData = targetClient.WorldData;
			playerData.DesiredViewDistance = packet.ViewDistance;
			if (playerData.Viewdistance != packet.ViewDistance)
			{
				ServerMain.Logger.Notification("Player {0} requested new view distance: {1}", new object[] { client.PlayerName, packet.ViewDistance });
				if (targetClient.IsSinglePlayerClient)
				{
					this.Config.MaxChunkRadius = Math.Max(this.Config.MaxChunkRadius, packet.ViewDistance / 32);
					LoggerBase logger = ServerMain.Logger;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(66, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Upped server view distance to: ");
					defaultInterpolatedStringHandler.AppendFormatted<int>(packet.ViewDistance);
					defaultInterpolatedStringHandler.AppendLiteral(", because player is in singleplayer");
					logger.Notification(defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			playerData.Viewdistance = packet.ViewDistance;
			bool freeMoveAllowed;
			bool gameModeAllowed;
			bool pickRangeAllowed;
			if (playerUid != client.WorldData.PlayerUID)
			{
				freeMoveAllowed = this.PlayerHasPrivilege(clientid, Privilege.freemove) && this.PlayerHasPrivilege(clientid, Privilege.commandplayer);
				gameModeAllowed = this.PlayerHasPrivilege(clientid, Privilege.gamemode) && this.PlayerHasPrivilege(clientid, Privilege.commandplayer);
				pickRangeAllowed = this.PlayerHasPrivilege(clientid, Privilege.pickingrange) && this.PlayerHasPrivilege(clientid, Privilege.commandplayer);
			}
			else
			{
				freeMoveAllowed = this.PlayerHasPrivilege(clientid, Privilege.freemove);
				gameModeAllowed = this.PlayerHasPrivilege(clientid, Privilege.gamemode);
				pickRangeAllowed = this.PlayerHasPrivilege(clientid, Privilege.pickingrange);
			}
			if (freeMoveAllowed)
			{
				playerData.FreeMove = packet.FreeMove > 0;
				playerData.NoClip = packet.NoClip > 0;
				playerData.MoveSpeedMultiplier = CollectibleNet.DeserializeFloat(packet.MoveSpeed);
				try
				{
					playerData.FreeMovePlaneLock = (EnumFreeMovAxisLock)packet.FreeMovePlaneLock;
					goto IL_0249;
				}
				catch (Exception)
				{
					goto IL_0249;
				}
			}
			if (((packet.FreeMove > 0) ^ playerData.FreeMove) || (playerData.NoClip ^ (packet.NoClip > 0)) || playerData.MoveSpeedMultiplier != CollectibleNet.DeserializeFloat(packet.MoveSpeed))
			{
				this.ReplyMessage(client.Player, "Not allowed to change fly mode, noclip or move speed. Missing privilege or not allowed in this world.", EnumChatType.CommandError, null);
			}
			IL_0249:
			EnumGameMode requestedMode = EnumGameMode.Guest;
			try
			{
				requestedMode = (EnumGameMode)packet.GameMode;
			}
			catch (Exception)
			{
			}
			if (gameModeAllowed)
			{
				EnumGameMode gameMode = playerData.GameMode;
				playerData.GameMode = requestedMode;
				if (gameMode != requestedMode)
				{
					for (int i = 0; i < this.Systems.Length; i++)
					{
						this.Systems[i].OnPlayerSwitchGameMode(targetClient.Player);
					}
					this.EventManager.TriggerPlayerChangeGamemode(targetClient.Player);
					if (requestedMode == EnumGameMode.Guest || requestedMode == EnumGameMode.Survival)
					{
						playerData.MoveSpeedMultiplier = 1f;
					}
				}
			}
			else if (playerData.GameMode != requestedMode)
			{
				this.ReplyMessage(client.Player, "Not allowed to change game mode. Missing privilege or not allowed in this world.", EnumChatType.CommandError, null);
			}
			if (pickRangeAllowed)
			{
				playerData.PickingRange = CollectibleNet.DeserializeFloat(packet.PickingRange);
			}
			else if (playerData.PickingRange != CollectibleNet.DeserializeFloat(packet.PickingRange))
			{
				this.ReplyMessage(client.Player, "Not allowed to change picking range. Missing privilege or not allowed in this world.", EnumChatType.CommandError, null);
			}
			bool canFreeMove = playerData.GameMode == EnumGameMode.Creative || playerData.GameMode == EnumGameMode.Spectator;
			playerData.FreeMove = (playerData.FreeMove && canFreeMove) || playerData.GameMode == EnumGameMode.Spectator;
			playerData.NoClip = playerData.NoClip && canFreeMove;
			playerData.RenderMetaBlocks = packet.RenderMetaBlocks > 0;
			targetClient.Entityplayer.Controls.IsFlying = playerData.FreeMove || targetClient.Entityplayer.Controls.Gliding;
			targetClient.Entityplayer.Controls.MovespeedMultiplier = playerData.MoveSpeedMultiplier;
			this.BroadcastPacket(new Packet_Server
			{
				Id = 46,
				ModeChange = new Packet_PlayerMode
				{
					PlayerUID = playerUid,
					FreeMove = ((playerData.FreeMove > false) ? 1 : 0),
					GameMode = (int)playerData.GameMode,
					MoveSpeed = CollectibleNet.SerializeFloat(playerData.MoveSpeedMultiplier),
					NoClip = ((playerData.NoClip > false) ? 1 : 0),
					ViewDistance = playerData.LastApprovedViewDistance,
					PickingRange = CollectibleNet.SerializeFloat(playerData.PickingRange),
					FreeMovePlaneLock = (int)playerData.FreeMovePlaneLock
				}
			}, Array.Empty<IServerPlayer>());
			targetClient.Player.Entity.UpdatePartitioning();
			targetClient.Player.Entity.Controls.NoClip = playerData.NoClip;
		}

		private void HandleChatLine(Packet_Client packet, ConnectedClient client)
		{
			string message = packet.Chatline.Message.Trim();
			int groupId = packet.Chatline.Groupid;
			if (groupId < -1)
			{
				groupId = 0;
			}
			this.HandleChatMessage(client.Player, groupId, message);
		}

		private void HandleSelectedHotbarSlot(Packet_Client packet, ConnectedClient client)
		{
			int fromSlot = client.Player.ActiveSlot;
			int toSlot = packet.SelectedHotbarSlot.SlotNumber;
			if (this.EventManager.TriggerBeforeActiveSlotChanged(client.Player, fromSlot, toSlot))
			{
				client.Player.ActiveSlot = toSlot;
				client.Player.InventoryManager.ActiveHotbarSlot.Inventory.DropSlotIfHot(client.Player.InventoryManager.ActiveHotbarSlot, client.Player);
				this.BroadcastHotbarSlot(client.Player, true);
				PlayerAnimationManager playerAnimationManager = client.Player.Entity.AnimManager as PlayerAnimationManager;
				if (playerAnimationManager != null)
				{
					playerAnimationManager.OnActiveSlotChanged(client.Player.InventoryManager.ActiveHotbarSlot);
				}
				this.EventManager.TriggerAfterActiveSlotChanged(client.Player, fromSlot, toSlot);
				return;
			}
			this.BroadcastHotbarSlot(client.Player, false);
		}

		public void BroadcastHotbarSlot(IServerPlayer ofPlayer, bool skipSelf = true)
		{
			IServerPlayer[] array;
			if (!skipSelf)
			{
				array = Array.Empty<IServerPlayer>();
			}
			else
			{
				(array = new IServerPlayer[1])[0] = ofPlayer;
			}
			IServerPlayer[] skipPlayers = array;
			IPlayerInventoryManager inventoryManager = ofPlayer.InventoryManager;
			if (((inventoryManager != null) ? inventoryManager.ActiveHotbarSlot : null) != null)
			{
				ItemStack stack = ofPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
				EntityPlayer entity = ofPlayer.Entity;
				ItemStack itemStack;
				if (entity == null)
				{
					itemStack = null;
				}
				else
				{
					ItemSlot leftHandItemSlot = entity.LeftHandItemSlot;
					itemStack = ((leftHandItemSlot != null) ? leftHandItemSlot.Itemstack : null);
				}
				ItemStack offstack = itemStack;
				this.BroadcastPacket(new Packet_Server
				{
					SelectedHotbarSlot = new Packet_SelectedHotbarSlot
					{
						ClientId = ofPlayer.ClientId,
						SlotNumber = ofPlayer.InventoryManager.ActiveHotbarSlotNumber,
						Itemstack = ((stack == null) ? null : StackConverter.ToPacket(stack)),
						OffhandStack = ((offstack == null) ? null : StackConverter.ToPacket(offstack))
					},
					Id = 53
				}, skipPlayers);
				return;
			}
			if (ofPlayer.InventoryManager == null)
			{
				ServerMain.Logger.Error("BroadcastHotbarSlot: InventoryManager is null?! Ignoring.");
				return;
			}
			ServerMain.Logger.Error("BroadcastHotbarSlot: ActiveHotbarSlot is null?! Ignoring.");
		}

		private void HandleLeave(Packet_Client packet, ConnectedClient client)
		{
			this.DisconnectPlayer(client, (packet.Leave.Reason == 1) ? Lang.Get("The Players client crashed", Array.Empty<object>()) : null, null);
		}

		private void HandleMoveKeyChange(Packet_Client packet, ConnectedClient client)
		{
			EntityControls controls = ((client.Entityplayer.MountedOn == null) ? client.Entityplayer.Controls : client.Entityplayer.MountedOn.Controls);
			if (controls != null)
			{
				client.previousControls.SetFrom(controls);
				controls.UpdateFromPacket(packet.MoveKeyChange.Down > 0, packet.MoveKeyChange.Key);
				if (client.previousControls.ToInt() != controls.ToInt())
				{
					controls.Dirty = true;
					client.Player.TriggerInWorldAction((EnumEntityAction)packet.MoveKeyChange.Key, packet.MoveKeyChange.Down > 0);
				}
			}
		}

		private void HandleEntityPacket(Packet_Client packet, ConnectedClient client)
		{
			Packet_EntityPacket p = packet.EntityPacket;
			Entity entity;
			if (!this.LoadedEntities.TryGetValue(p.EntityId, out entity))
			{
				return;
			}
			entity.OnReceivedClientPacket(client.Player, p.Packetid, p.Data);
		}

		public void HandleChatMessage(IServerPlayer player, int groupid, string message)
		{
			if (groupid > 0 && !this.PlayerDataManager.PlayerGroupsById.ContainsKey(groupid))
			{
				this.SendMessage(player, GlobalConstants.ServerInfoChatGroup, "No such group exists on this server.", EnumChatType.CommandError, null);
				return;
			}
			if (string.IsNullOrEmpty(message))
			{
				return;
			}
			if (message.StartsWith('/'))
			{
				string command = message.Split(new char[] { ' ' })[0].Replace("/", "");
				command = command.ToLowerInvariant();
				string arguments = ((message.IndexOf(' ') < 0) ? "" : message.Substring(message.IndexOf(' ') + 1));
				this.api.commandapi.Execute(command, player, groupid, arguments, null);
				return;
			}
			if (message.StartsWith('.'))
			{
				return;
			}
			if (!player.HasPrivilege(Privilege.chat))
			{
				this.SendMessage(player, groupid, Lang.Get("No privilege to chat", Array.Empty<object>()), EnumChatType.CommandError, null);
				return;
			}
			if (this.ElapsedMilliseconds - this.Clients[player.ClientId].LastChatMessageTotalMs < (long)this.Config.ChatRateLimitMs)
			{
				this.SendMessage(player, groupid, Lang.Get("Chat not sent. Rate limited to 1 chat message per {0} seconds", new object[] { (float)this.Config.ChatRateLimitMs / 1000f }), EnumChatType.CommandError, null);
				return;
			}
			this.Clients[player.ClientId].LastChatMessageTotalMs = this.ElapsedMilliseconds;
			message = message.Replace(">", "&gt;").Replace("<", "&lt;");
			string data = "from: " + player.Entity.EntityId.ToString() + ",withoutPrefix:" + message;
			string originalMessage = message;
			BoolRef consumed = new BoolRef();
			this.EventManager.TriggerOnplayerChat(player, groupid, ref message, ref data, consumed);
			if (consumed.value)
			{
				return;
			}
			this.SendMessageToGroup(groupid, message, EnumChatType.OthersMessage, player, data);
			player.SendMessage(groupid, message, EnumChatType.OwnMessage, data);
			LoggerBase logger = ServerMain.Logger;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 3);
			defaultInterpolatedStringHandler.AppendFormatted<int>(groupid);
			defaultInterpolatedStringHandler.AppendLiteral(" | ");
			defaultInterpolatedStringHandler.AppendFormatted(player.PlayerName);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(originalMessage.Replace("{", "{{").Replace("}", "}}"));
			logger.Chat(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		private void HandleQueryClientPacket(ConnectedClient client, Packet_Client packet)
		{
			if (packet.Id == 33)
			{
				if (this.Config.LoginFloodProtection)
				{
					int clientIpHash = client.Socket.RemoteEndPoint().Address.GetHashCode();
					int now = Environment.TickCount;
					ClientLastLogin block;
					if (this.RecentClientLogins.TryGetValue(clientIpHash, out block))
					{
						if (now - block.LastTickCount < 500 && now - block.LastTickCount >= 0)
						{
							block.LastTickCount = now;
							block.Times += 1;
							this.RecentClientLogins[clientIpHash] = block;
							if (this.Config.TemporaryIpBlockList && block.Times > 50)
							{
								string ipString = client.Socket.RemoteEndPoint().Address.ToString();
								TcpNetConnection.blockedIps.Add(ipString);
								LoggerBase logger = ServerMain.Logger;
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(56, 2);
								defaultInterpolatedStringHandler.AppendLiteral("Client ");
								defaultInterpolatedStringHandler.AppendFormatted<int>(client.Id);
								defaultInterpolatedStringHandler.AppendLiteral(" | ");
								defaultInterpolatedStringHandler.AppendFormatted(ipString);
								defaultInterpolatedStringHandler.AppendLiteral(" send too many request. Adding to blocked IP's");
								logger.Notification(defaultInterpolatedStringHandler.ToStringAndClear());
							}
							this.DisconnectPlayer(client, "Too many requests", "Your client is sending too many requests");
							return;
						}
						block.LastTickCount = now;
						block.Times = 0;
						this.RecentClientLogins[clientIpHash] = block;
					}
					else
					{
						this.RecentClientLogins[clientIpHash] = new ClientLastLogin
						{
							LastTickCount = now,
							Times = 1
						};
					}
				}
				client.LoginToken = Guid.NewGuid().ToString();
				this.ServerUdpNetwork.connectingClients.Add(client.LoginToken, client);
				this.SendPacket(client.Id, new Packet_Server
				{
					Id = 77,
					Token = new Packet_LoginTokenAnswer
					{
						Token = client.LoginToken
					}
				});
				return;
			}
			this.DisconnectPlayer(client, "", "Query complete");
		}

		private void HandlePlayerIdentification(Packet_Client p, ConnectedClient client)
		{
			Packet_ClientIdentification packet = p.Identification;
			if (packet == null)
			{
				this.DisconnectPlayer(client, null, Lang.Get("Invalid join data!", Array.Empty<object>()));
				return;
			}
			if ("1.21.9" != packet.NetworkVersion)
			{
				this.DisconnectPlayer(client, null, Lang.Get("disconnect-wrongversion", new object[] { packet.ShortGameVersion, packet.NetworkVersion, "1.21.5", "1.21.9" }));
				return;
			}
			if (!client.IsSinglePlayerClient && this.Config.IsPasswordProtected() && packet.ServerPassword != this.Config.Password)
			{
				ServerMain.Logger.Event(string.Format("{0} fails to join (invalid server password).", packet.Playername));
				this.DisconnectPlayer(client, null, Lang.Get("Password is invalid", Array.Empty<object>()));
				return;
			}
			if ((this.Config.WhitelistMode == EnumWhitelistMode.On || (this.Config.WhitelistMode == EnumWhitelistMode.Default && this.IsDedicatedServer)) && !client.IsLocalConnection)
			{
				PlayerEntry playerWhitelist = this.PlayerDataManager.GetPlayerWhitelist(packet.Playername, packet.PlayerUID);
				if (playerWhitelist == null)
				{
					this.DisconnectPlayer(client, null, "This server only allows whitelisted players to join. You are not on the whitelist.");
					return;
				}
				if (playerWhitelist.UntilDate < DateTime.Now)
				{
					this.DisconnectPlayer(client, null, "This server only allows whitelisted players to join. Your whitelist entry has expired.");
					return;
				}
			}
			if (packet.Playername == null || packet.PlayerUID == null)
			{
				client.IsNewClient = true;
				ServerMain.Logger.Event(string.Format("{0} fails to join (player name or playeruid null value sent).", packet.Playername));
				this.DisconnectPlayer(client, null, "Invalid join data");
			}
			PlayerEntry playerBan = this.PlayerDataManager.GetPlayerBan(packet.Playername, packet.PlayerUID);
			if (playerBan != null && playerBan.UntilDate > DateTime.Now)
			{
				ServerMain.Logger.Event(string.Format("{0} fails to join (banned).", packet.Playername));
				this.DisconnectPlayer(client, null, Lang.Get("banned-until-reason", new object[] { playerBan.IssuedByPlayerName, playerBan.UntilDate, playerBan.Reason }));
				return;
			}
			client.SentPlayerUid = packet.PlayerUID;
			ServerMain.Logger.Notification("Client {0} uid {1} attempting identification. Name: {2}", new object[] { client.Id, packet.PlayerUID, packet.Playername });
			string playername = packet.Playername;
			Regex allowedPlayername = new Regex("^(\\w|-){1,16}$");
			if (string.IsNullOrEmpty(playername) || !allowedPlayername.IsMatch(playername))
			{
				ServerMain.Logger.Event(string.Format("{0} can't join (invalid Playername: {1}).", client.Socket.RemoteEndPoint(), playername));
				this.DisconnectPlayer(client, null, Lang.Get("Your playername contains not allowed characters or is not set. Are you using a hacked client?", Array.Empty<object>()));
				return;
			}
			if (client.IsSinglePlayerClient || !this.Config.VerifyPlayerAuth)
			{
				string entitlements = (client.IsSinglePlayerClient ? GlobalConstants.SinglePlayerEntitlements : null);
				this.PreFinalizePlayerIdentification(packet, client, entitlements);
				return;
			}
			this.VerifyPlayerWithAuthServer(packet, client);
		}

		public override IWorldAccessor World
		{
			get
			{
				return this;
			}
		}

		protected override WorldMap worldmap
		{
			get
			{
				return this.WorldMap;
			}
		}

		public int Seed
		{
			get
			{
				return this.SaveGameData.Seed;
			}
		}

		public string SavegameIdentifier
		{
			get
			{
				return this.SaveGameData.SavegameIdentifier;
			}
		}

		public bool Suspended
		{
			get
			{
				return this.suspended;
			}
		}

		FrameProfilerUtil IWorldAccessor.FrameProfiler
		{
			get
			{
				return ServerMain.FrameProfiler;
			}
		}

		public override ClassRegistry ClassRegistryInt
		{
			get
			{
				return ServerMain.ClassRegistry;
			}
			set
			{
				ServerMain.ClassRegistry = value;
			}
		}

		public int ServerConsoleId
		{
			get
			{
				return this.serverConsoleId;
			}
		}

		public NetServer[] MainSockets { get; set; } = new NetServer[2];

		public UNetServer[] UdpSockets { get; set; } = new UNetServer[2];

		ILogger IWorldAccessor.Logger
		{
			get
			{
				return ServerMain.Logger;
			}
		}

		IAssetManager IWorldAccessor.AssetManager
		{
			get
			{
				return this.AssetManager;
			}
		}

		public EnumAppSide Side
		{
			get
			{
				return EnumAppSide.Server;
			}
		}

		public ICoreAPI Api
		{
			get
			{
				return this.api;
			}
		}

		public IChunkProvider ChunkProvider
		{
			get
			{
				return this.WorldMap;
			}
		}

		public ILandClaimAPI Claims
		{
			get
			{
				return this.WorldMap;
			}
		}

		public ServerMain(StartServerArgs serverargs, string[] cmdlineArgsRaw, ServerProgramArgs progArgs, bool isDedicatedServer = true)
		{
			this.IsDedicatedServer = isDedicatedServer;
			if (ServerMain.Logger == null)
			{
				ServerMain.Logger = new ServerLogger(progArgs);
			}
			this.serverStartArgs = serverargs;
			this._consoleThreadsCts = new CancellationTokenSource();
			this.ServerThreadsCts = new CancellationTokenSource();
			ServerMain.Logger.TraceLog = progArgs.TraceLog;
			this.RawCmdLineArgs = cmdlineArgsRaw;
			this.progArgs = progArgs;
			if (progArgs.SetConfigAndExit != null)
			{
				string filename = "serverconfig.json";
				if (!File.Exists(Path.Combine(GamePaths.Config, filename)))
				{
					Logger logger = ServerMain.Logger;
					if (logger != null)
					{
						logger.Notification("serverconfig.json not found, creating new one");
					}
					ServerSystemLoadConfig.GenerateConfig(this);
				}
				else
				{
					ServerSystemLoadConfig.LoadConfig(this);
				}
				JObject jobject = JToken.Parse(progArgs.SetConfigAndExit) as JObject;
				JObject tokcfg = JToken.FromObject(this.Config) as JObject;
				foreach (KeyValuePair<string, JToken> val in jobject)
				{
					JToken tok = tokcfg[val.Key];
					if (tok == null)
					{
						Logger logger2 = ServerMain.Logger;
						if (logger2 != null)
						{
							logger2.Notification("No such setting '" + val.Key + "'. Ignoring.");
						}
						this.ExitCode = 404;
						return;
					}
					JObject tokobj = tok as JObject;
					if (tokobj != null)
					{
						tokobj.Merge(val.Value);
						Logger logger3 = ServerMain.Logger;
						if (logger3 != null)
						{
							logger3.Notification("Ok, values merged for {0}.", new object[] { val.Key });
						}
					}
					else
					{
						tokcfg[val.Key] = val.Value;
						Logger logger4 = ServerMain.Logger;
						if (logger4 != null)
						{
							logger4.Notification("Ok, value {0} set for {1}.", new object[] { val.Value, val.Key });
						}
					}
				}
				try
				{
					this.Config = tokcfg.ToObject<ServerConfig>();
				}
				catch (Exception e)
				{
					Logger logger5 = ServerMain.Logger;
					if (logger5 != null)
					{
						logger5.Notification("Failed saving config, you are likely suppling an incorrect value type (e.g. a number for a boolean setting). See server-debug.log for exception.");
					}
					Logger logger6 = ServerMain.Logger;
					if (logger6 != null)
					{
						logger6.VerboseDebug("Failed saving config from --setConfig. Exception:");
					}
					Logger logger7 = ServerMain.Logger;
					if (logger7 != null)
					{
						logger7.VerboseDebug(LoggerBase.CleanStackTrace(e.ToString()));
					}
					this.ExitCode = 500;
					return;
				}
				this.ExitCode = 200;
				ServerSystemLoadConfig.SaveConfig(this);
				Logger logger8 = ServerMain.Logger;
				if (logger8 == null)
				{
					return;
				}
				logger8.Dispose();
				return;
			}
			else
			{
				if (progArgs.GenConfigAndExit)
				{
					ServerSystemLoadConfig.GenerateConfig(this);
					ServerSystemLoadConfig.SaveConfig(this);
					if (ServerMain.Logger != null)
					{
						ServerMain.Logger.Notification("Config generated.");
						ServerMain.Logger.Dispose();
					}
					return;
				}
				if (progArgs.ReducedThreads)
				{
					this.ReducedServerThreads = true;
				}
				this.ServerConsoleClient = new ServerConsoleClient(this.serverConsoleId)
				{
					FallbackPlayerName = "Admin",
					IsNewClient = false
				};
				this.ServerConsoleClient.WorldData = new ServerWorldPlayerData
				{
					PlayerUID = "Admin"
				};
				ServerMain.FrameProfiler = new FrameProfilerUtil(new Action<string>(ServerMain.Logger.Notification));
				AnimatorBase.logAntiSpam.Clear();
				if (this.IsDedicatedServer)
				{
					this.serverConsole = new ServerConsole(this, this._consoleThreadsCts.Token);
				}
				foreach (Thread thread in this.Serverthreads)
				{
					if (thread != null)
					{
						thread.Start();
					}
				}
				this.TagRegistry.Side = EnumAppSide.Server;
				this.totalUpTime.Start();
			}
		}

		private Thread CreateThread(string name, ServerSystem[] serversystems, CancellationToken cancellationToken)
		{
			ServerThread serverThread = new ServerThread(this, name, cancellationToken);
			this.ServerThreadLoops.Add(serverThread);
			serverThread.serversystems = serversystems;
			return TyronThreadPool.CreateDedicatedThread(new ThreadStart(serverThread.Process), name);
		}

		public Thread CreateBackgroundThread(string name, ThreadStart starter)
		{
			Thread t = TyronThreadPool.CreateDedicatedThread(starter, name);
			t.Priority = Thread.CurrentThread.Priority;
			this.Serverthreads.Add(t);
			return t;
		}

		public void AddServerThread(string name, IAsyncServerSystem modsystem)
		{
			ServerSystem serverSystem = new ServerSystemAsync(this, name, modsystem);
			Thread thread = this.CreateThread(name, new ServerSystem[] { serverSystem }, this.ServerThreadsCts.Token);
			this.Serverthreads.Add(thread);
			Array.Resize<ServerSystem>(ref this.Systems, this.Systems.Length + 1);
			this.Systems[this.Systems.Length - 1] = serverSystem;
			if (this.RunPhase >= EnumServerRunPhase.RunGame)
			{
				thread.Start();
			}
		}

		public void PreLaunch()
		{
			if (!this.ReducedServerThreads)
			{
				this.ClientPacketParsingThread = this.CreateBackgroundThread("clientPacketsParser", new ThreadStart(new ClientPacketParserOffthread(this).Start));
			}
		}

		public void StandbyLaunch()
		{
			this.MainSockets[1] = new TcpNetServer();
			this.UdpSockets[1] = new UdpNetServer(this.Clients);
			ServerSystemLoadConfig.EnsureConfigExists(this);
			ServerSystemLoadConfig.LoadConfig(this);
			this.startSockets();
			ServerMain.Logger.Event("Server launched in standby mode. Full launch will commence on first connection attempt. Only /stop and /stats commands will be functioning");
		}

		public void Launch()
		{
			this.loadedChunksLock = new FastRWLock(this);
			this.serverChunkDataPool = new ChunkDataPool(MagicNum.ServerChunkSize, this);
			this.InitBasicPacketHandlers();
			RuntimeEnv.ServerMainThreadId = Environment.CurrentManagedThreadId;
			this.ModEventManager = new ServerEventManager(this);
			this.EventManager = new CoreServerEventManager(this, this.ModEventManager);
			this.PlayerDataManager = new PlayerDataManager(this);
			ServerSystemModHandler modhandler = new ServerSystemModHandler(this);
			this.EnterRunPhase(EnumServerRunPhase.Start);
			ServerSystemCompressChunks compresschunks = new ServerSystemCompressChunks(this);
			ServerSystemRelight relight = new ServerSystemRelight(this);
			this.chunkThread = new ChunkServerThread(this, "chunkdbthread", this.ServerThreadsCts.Token);
			this.ServerThreadLoops.Add(this.chunkThread);
			ServerSystemSupplyChunkCommands supplychunkcpommands = new ServerSystemSupplyChunkCommands(this, this.chunkThread);
			ServerThread serverThread = this.chunkThread;
			ServerSystem[] array = new ServerSystem[3];
			ServerSystemSupplyChunks supplychunks = (array[0] = new ServerSystemSupplyChunks(this, this.chunkThread));
			ServerSystemLoadAndSaveGame loadsavegame = (array[1] = new ServerSystemLoadAndSaveGame(this, this.chunkThread));
			ServerSystemUnloadChunks unloadchunks = (array[2] = new ServerSystemUnloadChunks(this, this.chunkThread));
			serverThread.serversystems = array;
			Thread chunkdbthread = new Thread(new ThreadStart(this.chunkThread.Process));
			chunkdbthread.Name = "chunkdbthread";
			chunkdbthread.IsBackground = true;
			ServerSystemBlockSimulation serverBlockSimulation = new ServerSystemBlockSimulation(this);
			this.ServerUdpNetwork = new ServerUdpNetwork(this);
			Thread serverUdpQueueThread = new Thread(new ThreadStart(new ServerUdpQueue(this, this.ServerUdpNetwork).DedicatedThreadLoop));
			serverUdpQueueThread.Name = "UdpSending";
			serverUdpQueueThread.IsBackground = true;
			this.Serverthreads.AddRange(new Thread[]
			{
				chunkdbthread,
				this.CreateThread("CompressChunks", new ServerSystem[] { compresschunks }, this.ServerThreadsCts.Token),
				this.CreateThread("Relight", new ServerSystem[] { relight }, this.ServerThreadsCts.Token),
				this.CreateThread("ServerBlockTicks", new ServerSystem[] { serverBlockSimulation }, this.ServerThreadsCts.Token),
				serverUdpQueueThread
			});
			this.Systems = new ServerSystem[]
			{
				new ServerSystemUpnp(this),
				this.clientAwarenessSystem = new ServerSystemClientAwareness(this),
				new ServerSystemLoadConfig(this),
				new ServerSystemNotifyPing(this),
				modhandler,
				new ServerySystemPlayerGroups(this),
				new ServerSystemEntitySimulation(this),
				new ServerSystemCalendar(this),
				new ServerSystemCommands(this),
				new CmdPlayer(this),
				new ServerSystemInventory(this),
				new ServerSystemAutoSaveGame(this),
				compresschunks,
				supplychunks,
				supplychunkcpommands,
				relight,
				new ServerSystemSendChunks(this),
				unloadchunks,
				new ServerSystemBlockIdRemapper(this),
				new ServerSystemItemIdRemapper(this),
				new ServerSystemEntityCodeRemapper(this),
				new ServerSystemMacros(this),
				new ServerSystemEntitySpawner(this),
				new ServerSystemWorldAmbient(this),
				new ServerSystemHeartbeat(this),
				new ServerSystemRemapperAssistant(this),
				loadsavegame,
				serverBlockSimulation,
				this.ServerUdpNetwork,
				new ServerSystemBlockLogger(this),
				new ServerSystemMonitor(this)
			};
			if (ServerMain.xPlatInterface == null)
			{
				ServerMain.xPlatInterface = XPlatformInterfaces.GetInterface();
			}
			ServerMain.Logger.StoryEvent(Lang.Get("It begins...", Array.Empty<object>()));
			ServerMain.Logger.Event("Launching server...");
			this.PlayerDataManager.Load();
			ServerMain.Logger.StoryEvent(Lang.Get("It senses...", Array.Empty<object>()));
			ServerMain.Logger.Event("Server v1.21.5, network v1.21.9, api v1.21.0");
			this.totalUnpausedTime.Start();
			this.AssetManager = new AssetManager(GamePaths.AssetsPath, EnumAppSide.Server);
			if (this.progArgs.AddOrigin != null)
			{
				foreach (string text in this.progArgs.AddOrigin)
				{
					string[] domainPaths = Directory.GetDirectories(text);
					for (int i = 0; i < domainPaths.Length; i++)
					{
						string domain = new DirectoryInfo(domainPaths[i]).Name;
						this.AssetManager.CustomAppOrigins.Add(new PathOrigin(domain, domainPaths[i]));
					}
				}
			}
			this.EnterRunPhase(EnumServerRunPhase.Initialization);
			this.WorldMap = new ServerWorldMap(this);
			ServerMain.Logger.Event("Loading configuration...");
			this.EnterRunPhase(EnumServerRunPhase.Configuration);
			if (this.RunPhase == EnumServerRunPhase.Exit)
			{
				return;
			}
			this.AfterConfigLoaded();
			this.LoadAssets();
			ServerMain.Logger.Event("Building assets...");
			this.EnterRunPhase(EnumServerRunPhase.LoadAssets);
			if (this.RunPhase == EnumServerRunPhase.Exit)
			{
				return;
			}
			if (this.AssetManager.TryGet("blocktypes/plant/reedpapyrus-free.json", false) != null)
			{
				string msg = (this.Standalone ? "blocktypes/plant/reedpapyrus-free.json file detected, which breaks stuff. That means this is an incorrectly updated 1.16 server! When up update a server, make sure to delete the old server installation files (but keep the data folder)" : "blocktypes/plant/reedpapyrus-free.json file detected, which breaks the game. Possible corrupted installation. Please uninstall the game, delete the folder %appdata%/VintageStory, then reinstall.");
				ServerMain.Logger.Fatal(msg);
				throw new ApplicationException(msg);
			}
			this.FinalizeAssets();
			ServerMain.Logger.Event("Server assets loaded, parsed, registered and finalized");
			ServerMain.Logger.Event("Initialising systems...");
			this.EnterRunPhase(EnumServerRunPhase.LoadGamePre);
			if (this.RunPhase == EnumServerRunPhase.Exit)
			{
				return;
			}
			this.AfterSaveGameLoaded();
			if (this.RunPhase == EnumServerRunPhase.Exit)
			{
				return;
			}
			ServerMain.Logger.StoryEvent(Lang.Get("A world unbroken...", Array.Empty<object>()));
			this.EnterRunPhase(EnumServerRunPhase.GameReady);
			if (this.RunPhase == EnumServerRunPhase.Exit)
			{
				return;
			}
			this.EnterRunPhase(EnumServerRunPhase.WorldReady);
			if (this.RunPhase == EnumServerRunPhase.Exit)
			{
				return;
			}
			this.StartBuildServerAssetsPacket();
			ServerMain.Logger.StoryEvent(Lang.Get("The center unfolding...", Array.Empty<object>()));
			ServerMain.Logger.Event("Starting world generators...");
			this.ModEventManager.TriggerWorldgenStartup();
			if (this.RunPhase == EnumServerRunPhase.Exit)
			{
				return;
			}
			ServerMain.Logger.Event("Begin game ticking...");
			ServerMain.Logger.StoryEvent(Lang.Get("...and calls to you.", Array.Empty<object>()));
			this.EnterRunPhase(EnumServerRunPhase.RunGame);
			if (this.RunPhase == EnumServerRunPhase.Exit)
			{
				return;
			}
			ServerMain.Logger.Notification("Starting server threads");
			foreach (Thread thread in this.Serverthreads)
			{
				thread.TryStart();
			}
			bool networkedServer = this.IsDedicatedServer || this.MainSockets[1] != null;
			string type = (this.IsDedicatedServer ? Lang.Get("Dedicated Server", Array.Empty<object>()) : (networkedServer ? Lang.Get("Threaded Server", Array.Empty<object>()) : Lang.Get("Singleplayer Server", Array.Empty<object>())));
			string bind = (networkedServer ? ((this.CurrentIp == null) ? Lang.Get(" on Port {0} and all ips", new object[] { this.CurrentPort }) : Lang.Get(" on Port {0} and ip {1}", new object[] { this.CurrentPort, this.CurrentIp })) : "");
			ServerMain.Logger.Event("{0} now running{1}!", new object[] { type, bind });
			ServerMain.Logger.StoryEvent(Lang.Get("Return again.", Array.Empty<object>()));
			if ((this.Config.WhitelistMode == EnumWhitelistMode.Default || this.Config.WhitelistMode == EnumWhitelistMode.On) && !this.Config.AdvertiseServer)
			{
				ServerMain.Logger.Notification("Please be aware that as of 1.20, servers default configurations have changed - servers no longer register themselves to the public servers list and are invite-only (whitelisted) out of the box. If you desire so, you can enable server advertising with '/serverconfig advertise on' and disable the whitelist mode with '/serverconfig whitelistmode off'");
			}
			this.AssetManager.UnloadUnpatchedAssets();
		}

		internal void EnterRunPhase(EnumServerRunPhase runPhase)
		{
			this.RunPhase = runPhase;
			if (runPhase == EnumServerRunPhase.Start || runPhase == EnumServerRunPhase.Exit)
			{
				return;
			}
			ServerMain.Logger.Notification("Entering runphase " + runPhase.ToString());
			ServerMain.Logger.VerboseDebug("Entering runphase " + runPhase.ToString());
			foreach (ServerSystem system in this.Systems)
			{
				switch (runPhase)
				{
				case EnumServerRunPhase.Initialization:
					this.suspended = true;
					system.OnBeginInitialization();
					break;
				case EnumServerRunPhase.Configuration:
					system.OnBeginConfiguration();
					break;
				case EnumServerRunPhase.LoadAssets:
					system.OnLoadAssets();
					break;
				case EnumServerRunPhase.AssetsFinalize:
					system.OnFinalizeAssets();
					break;
				case EnumServerRunPhase.LoadGamePre:
					system.OnBeginModsAndConfigReady();
					break;
				case EnumServerRunPhase.GameReady:
					system.OnBeginGameReady(this.SaveGameData);
					break;
				case EnumServerRunPhase.WorldReady:
					system.OnBeginWorldReady();
					break;
				case EnumServerRunPhase.RunGame:
					this.suspended = false;
					system.OnBeginRunGame();
					break;
				case EnumServerRunPhase.Shutdown:
					system.OnBeginShutdown();
					break;
				}
			}
		}

		public void AfterConfigLoaded()
		{
			this.ServerConsoleClient.Player = new ServerConsolePlayer(this, this.ServerConsoleClient.WorldData);
			if (this.IsDedicatedServer && this.MainSockets[1] == null && this.UdpSockets[1] == null)
			{
				this.MainSockets[1] = new TcpNetServer();
				this.UdpSockets[1] = new UdpNetServer(this.Clients);
				this.startSockets();
			}
			string[] allPrivs = Privilege.AllCodes();
			for (int i = 0; i < allPrivs.Length; i++)
			{
				this.AllPrivileges.Add(allPrivs[i]);
				this.PrivilegeDescriptions.Add(allPrivs[i], allPrivs[i]);
			}
		}

		private void FinalizeAssets()
		{
			foreach (EntityProperties entityType in this.EntityTypes)
			{
				BlockDropItemStack[] entityDrops = entityType.Drops;
				if (entityDrops != null)
				{
					for (int i = 0; i < entityDrops.Length; i++)
					{
						if (!entityDrops[i].Resolve(this, "Entity ", entityType.Code))
						{
							entityDrops = (entityType.Drops = entityDrops.RemoveAt(i));
							i--;
						}
					}
				}
			}
			this.ModEventManager.TriggerFinalizeAssets();
			this.EnterRunPhase(EnumServerRunPhase.AssetsFinalize);
		}

		private void AfterSaveGameLoaded()
		{
			this.WorldMap.Init(this.SaveGameData.MapSizeX, this.SaveGameData.MapSizeY, this.SaveGameData.MapSizeZ);
			ServerMain.Logger.Notification("Server map set");
			if (this.MainSockets[1] == null)
			{
				this.startSockets();
			}
			PlayerRole serverGroup = new PlayerRole
			{
				Name = "Server",
				Code = "server",
				PrivilegeLevel = 9999,
				Privileges = this.AllPrivileges.ToList<string>(),
				Color = Color.LightSteelBlue
			};
			this.Config.RolesByCode.Add("server", serverGroup);
			this.ServerConsoleClient.serverdata = new ServerPlayerData();
			this.ServerConsoleClient.serverdata.SetRole(serverGroup);
		}

		private void startSockets()
		{
			if (this.progArgs.Ip != null)
			{
				this.CurrentIp = this.progArgs.Ip;
			}
			else if (this.Config.Ip != null)
			{
				this.CurrentIp = this.Config.Ip;
			}
			if (this.progArgs.Port != null)
			{
				if (!int.TryParse(this.progArgs.Port, out this.CurrentPort))
				{
					this.CurrentPort = this.Config.Port;
				}
			}
			else
			{
				this.CurrentPort = this.Config.Port;
			}
			if (!this.ReducedServerThreads)
			{
				this.ClientPacketParsingThread.TryStart();
			}
			NetServer netServer = this.MainSockets[1];
			if (netServer != null)
			{
				netServer.SetIpAndPort(this.CurrentIp, this.CurrentPort);
			}
			NetServer netServer2 = this.MainSockets[1];
			if (netServer2 != null)
			{
				netServer2.Start();
			}
			UNetServer unetServer = this.UdpSockets[1];
			if (unetServer != null)
			{
				unetServer.SetIpAndPort(this.CurrentIp, this.CurrentPort);
			}
			UNetServer unetServer2 = this.UdpSockets[1];
			if (unetServer2 == null)
			{
				return;
			}
			unetServer2.Start();
		}

		public void Process()
		{
			this.TickPosition = 0;
			if (this.Suspended)
			{
				Thread.Sleep(2);
				return;
			}
			if (this.RunPhase == EnumServerRunPhase.Standby)
			{
				this.ProcessMain();
				Thread.Sleep(5);
				return;
			}
			ServerMain.FrameProfiler.Begin("{0} players online - ", new object[] { this.Clients.Count - this.ConnectionQueue.Count });
			this.TickPosition++;
			ServerThread.SleepMs = ((!this.Clients.IsEmpty) ? 2 : ((int)this.Config.TickTime));
			this.lastFramePassedTime.Restart();
			this.TickPosition++;
			try
			{
				long elapsedMs = this.totalUnpausedTime.ElapsedMilliseconds;
				if (ServerMain.FrameProfiler.Enabled)
				{
					for (int i = 0; i < this.Systems.Length; i++)
					{
						ServerSystem system = this.Systems[i];
						long diff = elapsedMs - system.millisecondsSinceStart;
						if (diff > (long)system.GetUpdateInterval())
						{
							system.millisecondsSinceStart = elapsedMs;
							system.OnServerTick((float)diff / 1000f);
							ServerMain.FrameProfiler.Mark(system.FrameprofilerName);
						}
						this.TickPosition++;
					}
					ServerMain.FrameProfiler.Mark("ss-tick");
					this.EventManager.TriggerGameTickDebug(elapsedMs, this);
					this.TickPosition++;
					ServerMain.FrameProfiler.Mark("ev-tick");
					int j = 0;
					while (!FrameProfilerUtil.offThreadProfiles.IsEmpty)
					{
						if (j++ >= 25)
						{
							break;
						}
						string tickResults;
						FrameProfilerUtil.offThreadProfiles.TryDequeue(out tickResults);
						ServerMain.Logger.Notification(tickResults);
					}
				}
				else
				{
					for (int k = 0; k < this.Systems.Length; k++)
					{
						ServerSystem system2 = this.Systems[k];
						long diff = elapsedMs - system2.millisecondsSinceStart;
						if (diff > (long)system2.GetUpdateInterval())
						{
							system2.millisecondsSinceStart = elapsedMs;
							system2.OnServerTick((float)diff / 1000f);
						}
						this.TickPosition++;
					}
					this.EventManager.TriggerGameTick(elapsedMs, this);
					this.TickPosition++;
				}
				this.ProcessMain();
				this.TickPosition++;
				if ((DateTime.UtcNow - this.statsupdate).TotalSeconds >= 2.0)
				{
					this.statsupdate = DateTime.UtcNow;
					this.StatsCollectorIndex = (this.StatsCollectorIndex + 1) % 4;
					this.StatsCollector[this.StatsCollectorIndex].statTotalPackets = 0;
					this.StatsCollector[this.StatsCollectorIndex].statTotalUdpPackets = 0;
					this.StatsCollector[this.StatsCollectorIndex].statTotalPacketsLength = 0;
					this.StatsCollector[this.StatsCollectorIndex].statTotalUdpPacketsLength = 0;
					this.StatsCollector[this.StatsCollectorIndex].tickTimeTotal = 0L;
					this.StatsCollector[this.StatsCollectorIndex].ticksTotal = 0L;
					for (int l = 0; l < 10; l++)
					{
						this.StatsCollector[this.StatsCollectorIndex].tickTimes[l] = 0L;
					}
				}
				long lastServerTick = this.lastFramePassedTime.ElapsedMilliseconds;
				StatsCollection coll = this.StatsCollector[this.StatsCollectorIndex];
				coll.tickTimeTotal += lastServerTick;
				coll.ticksTotal += 1L;
				coll.tickTimes[coll.tickTimeIndex] = lastServerTick;
				coll.tickTimeIndex = (coll.tickTimeIndex + 1) % coll.tickTimes.Length;
				if (lastServerTick > 500L && this.totalUnpausedTime.ElapsedMilliseconds > 5000L && !this.stopped)
				{
					ServerMain.Logger.Warning("Server overloaded. A tick took {0}ms to complete.", new object[] { lastServerTick });
				}
				ServerMain.FrameProfiler.Mark("timers-updated");
				int millisecondsToSleep = (int)Math.Max(0f, this.Config.TickTime - (float)lastServerTick);
				if (millisecondsToSleep > 0)
				{
					Thread.Sleep(millisecondsToSleep);
					ServerMain.FrameProfiler.Mark("sleep");
				}
				this.TickPosition++;
			}
			catch (Exception e)
			{
				ServerMain.Logger.Fatal(e);
			}
			ServerMain.FrameProfiler.End();
		}

		public void ProcessMain()
		{
			if (this.MainSockets == null)
			{
				return;
			}
			this.ProcessMainThreadTasks();
			ServerMain.FrameProfiler.Mark("mtasks");
			if (this.ReducedServerThreads)
			{
				this.PacketParsingLoop();
			}
			ReceivedClientPacket cpk;
			while (this.ClientPackets.TryDequeue(out cpk))
			{
				try
				{
					this.HandleClientPacket_mainthread(cpk);
					continue;
				}
				catch (Exception e)
				{
					if (this.IsDedicatedServer)
					{
						ServerMain.Logger.Warning("Exception at client " + cpk.client.Id.ToString() + ". Disconnecting client.");
						this.DisconnectPlayer(cpk.client, "Threw an exception at the server", "An action you (or your client) did caused an unhandled exception");
					}
					ServerMain.Logger.Error(e);
					continue;
				}
				break;
			}
			this.DisconnectedClientsThisTick.Clear();
			ServerMain.FrameProfiler.Mark("net-read-done");
			this.TickPosition++;
			foreach (KeyValuePair<Timer, Timer.Tick> i in this.Timers)
			{
				i.Key.Update(i.Value);
			}
			this.TickPosition++;
		}

		public bool Suspend(bool newSuspendState, int maxWaitMilliseconds = 60000)
		{
			if (newSuspendState == this.suspended)
			{
				return true;
			}
			if (Monitor.TryEnter(this.suspendLock, 10000))
			{
				try
				{
					this.suspended = newSuspendState;
					if (this.suspended)
					{
						this.totalUnpausedTime.Stop();
						ServerSystem[] array = this.Systems;
						for (int i = 0; i < array.Length; i++)
						{
							array[i].OnServerPause();
						}
						while (maxWaitMilliseconds > 0)
						{
							if (!this.ServerThreadLoops.Any((ServerThread st) => !st.paused && st.Alive && st.threadName != "ServerConsole") && this.api.eventapi.CanSuspendServer())
							{
								break;
							}
							Thread.Sleep(10);
							maxWaitMilliseconds -= 10;
						}
					}
					else
					{
						this.totalUnpausedTime.Start();
						ServerSystem[] array = this.Systems;
						for (int i = 0; i < array.Length; i++)
						{
							array[i].OnServerResume();
						}
						this.api.eventapi.ResumeServer();
					}
					if (maxWaitMilliseconds <= 0 && this.suspended)
					{
						ServerMain.Logger.Warning("Server suspend requested, but reached max wait time. Server is only partially suspended.");
					}
					else
					{
						ServerMain.Logger.Notification("Server ticking has been {0}", new object[] { this.suspended ? "suspended" : "resumed" });
					}
					return maxWaitMilliseconds > 0;
				}
				finally
				{
					Monitor.Exit(this.suspendLock);
				}
				return false;
			}
			return false;
		}

		public void AttemptShutdown(string reason, int timeout)
		{
			if (Environment.CurrentManagedThreadId == RuntimeEnv.MainThreadId)
			{
				this.Stop(reason, null, EnumLogType.Notification);
				return;
			}
			if (this.RunPhase == EnumServerRunPhase.RunGame)
			{
				this.EnqueueMainThreadTask(delegate
				{
					this.Stop(reason, null, EnumLogType.Notification);
				});
				for (int i = 0; i < timeout / 15; i++)
				{
					if (this.stopped)
					{
						return;
					}
					Thread.Sleep(15);
				}
			}
			this.Stop("Forced: " + reason, null, EnumLogType.Notification);
		}

		public void Stop(string reason, string finalLogMessage = null, EnumLogType finalLogType = EnumLogType.Notification)
		{
			if (this.RunPhase == EnumServerRunPhase.Exit || this.stopped)
			{
				return;
			}
			this.stopped = true;
			if (ServerMain.FrameProfiler == null)
			{
				ServerMain.FrameProfiler = new FrameProfilerUtil(delegate(string text)
				{
					ServerMain.Logger.Notification(text);
				});
				ServerMain.FrameProfiler.Begin(null, Array.Empty<object>());
			}
			try
			{
				ServerConfig config = this.Config;
				if (config != null && config.RepairMode)
				{
					foreach (ConnectedClient client in this.Clients.Values)
					{
						ServerPlayer player = client.Player;
						if (((player != null) ? player.WorldData : null) != null)
						{
							client.Player.WorldData.CurrentGameMode = EnumGameMode.Survival;
							client.Player.WorldData.FreeMove = false;
							client.Player.WorldData.NoClip = false;
						}
					}
				}
				foreach (ConnectedClient client2 in this.Clients.Values.ToArray<ConnectedClient>())
				{
					string msg = "Server shutting down - " + reason;
					this.DisconnectPlayer(client2, msg, msg);
				}
			}
			catch (Exception e)
			{
				this.LogShutdownException(e);
			}
			ServerMain.Logger.Notification("Server stop requested, begin shutdown sequence. Stop reason: {0}", new object[] { reason });
			if (reason.Contains("Exception"))
			{
				ServerMain.Logger.StoryEvent(Lang.Get("Something went awry...please check the program logs... ({0})", new object[] { reason }));
			}
			try
			{
				this.Suspend(true, 10000);
			}
			catch (Exception e2)
			{
				this.LogShutdownException(e2);
			}
			new Stopwatch().Start();
			Thread.Sleep(20);
			try
			{
				this.EnterRunPhase(EnumServerRunPhase.Shutdown);
			}
			catch (Exception e3)
			{
				this.LogShutdownException(e3);
			}
			try
			{
				if (this.Blocks != null)
				{
					foreach (Block block in this.Blocks)
					{
						if (block != null)
						{
							block.OnUnloaded(this.api);
						}
					}
				}
			}
			catch (Exception e4)
			{
				this.LogShutdownException(e4);
			}
			try
			{
				if (this.Items != null)
				{
					foreach (Item item in this.Items)
					{
						if (item != null)
						{
							item.OnUnloaded(this.api);
						}
					}
				}
			}
			catch (Exception e5)
			{
				this.LogShutdownException(e5);
			}
			ServerMain.Logger.Event("Shutting down {0} server threads... ", new object[] { this.Serverthreads.Count });
			this._consoleThreadsCts.Cancel();
			ServerMain.Logger.Event("Killed console thread");
			ServerMain.Logger.StoryEvent(Lang.Get("Alone again...", Array.Empty<object>()));
			ServerThread.shouldExit = true;
			int shutDownGraceTimer = 120;
			bool anyThreadAlive = false;
			int timer = shutDownGraceTimer;
			while (timer-- > 0)
			{
				Thread.Sleep(500);
				anyThreadAlive = this.Serverthreads.Aggregate(false, (bool current, Thread t) => current || t.IsAlive);
				if (!anyThreadAlive)
				{
					break;
				}
				if (timer < shutDownGraceTimer - 10 && timer % 4 == 0)
				{
					string firstAliveThreadName = "";
					for (int i = 0; i < this.Serverthreads.Count; i++)
					{
						if (this.Serverthreads[i].IsAlive)
						{
							firstAliveThreadName = this.Serverthreads[i].Name;
							break;
						}
					}
					ServerMain.Logger.Event("Waiting for a server thread ({2}) to shut down ({0}/{1})...", new object[]
					{
						timer / 2,
						shutDownGraceTimer / 2,
						firstAliveThreadName
					});
				}
			}
			if (anyThreadAlive)
			{
				string threadnames = string.Join(", ", from t in this.Serverthreads
					where t.IsAlive
					select t.Name);
				ServerMain.Logger.Event("One or more server threads {0} didn't shut down within {1}ms, forcefully shutting them down...", new object[]
				{
					threadnames,
					shutDownGraceTimer * 500
				});
				this.ServerThreadsCts.Cancel();
			}
			else
			{
				ServerMain.Logger.Event("All threads gracefully shut down");
			}
			ServerMain.Logger.StoryEvent(Lang.Get("Time to rest.", Array.Empty<object>()));
			ServerMain.Logger.Event("Doing last tick...");
			try
			{
				this.ProcessMain();
			}
			catch (Exception e6)
			{
				this.LogShutdownException(e6);
			}
			ServerMain.Logger.Event("Stopped the server!");
			ServerThread.shouldExit = false;
			for (int j = 0; j < this.MainSockets.Length; j++)
			{
				NetServer netServer = this.MainSockets[j];
				if (netServer != null)
				{
					netServer.Dispose();
				}
			}
			for (int k = 0; k < this.UdpSockets.Length; k++)
			{
				UNetServer unetServer = this.UdpSockets[k];
				if (unetServer != null)
				{
					unetServer.Dispose();
				}
			}
			this.EnterRunPhase(EnumServerRunPhase.Exit);
			this.exit.SetExit(true);
			if (finalLogMessage != null)
			{
				ServerMain.Logger.Log(finalLogType, finalLogMessage);
			}
			ServerMain.Logger.ClearWatchers();
		}

		private void LogShutdownException(Exception exception)
		{
			ServerMain.Logger.Error("While shutting down the server:");
			ServerMain.Logger.Error(exception);
		}

		public void Dispose()
		{
			this.serverAssetsPacket.Dispose();
			this.serverAssetsSentLocally = false;
			this.worldMetaDataPacketAlreadySentToSinglePlayer = false;
			ServerSystem[] systems = this.Systems;
			for (int i = 0; i < systems.Length; i++)
			{
				systems[i].Dispose();
			}
			List<BoxedArray> list = this.reusableBuffersDisposalList;
			lock (list)
			{
				foreach (BoxedArray boxedArray in this.reusableBuffersDisposalList)
				{
					boxedArray.Dispose();
				}
				this.reusableBuffersDisposalList.Clear();
			}
			TyronThreadPool.Inst.Dispose();
			ServerMain.ClassRegistry = null;
			Logger logger = ServerMain.Logger;
			if (logger != null)
			{
				logger.Dispose();
			}
			ServerMain.Logger = null;
			this._consoleThreadsCts.Dispose();
			ServerConsole serverConsole = this.serverConsole;
			if (serverConsole != null)
			{
				serverConsole.Dispose();
			}
			this.ServerThreadsCts.Dispose();
			ThreadLocal<Random> threadLocal = this.rand;
			if (threadLocal == null)
			{
				return;
			}
			threadLocal.Dispose();
		}

		public bool DidExit()
		{
			return this.RunPhase == EnumServerRunPhase.Exit;
		}

		public void ReceiveServerConsole(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return;
			}
			if (message.StartsWith('/'))
			{
				string command = message.Split(new char[] { ' ' })[0].Replace("/", "");
				string args = ((message.IndexOf(' ') < 0) ? "" : message.Substring(message.IndexOf(' ') + 1));
				ServerMain.Logger.Notification("Handling Console Command /{0} {1}", new object[] { command, args });
				this.api.commandapi.Execute(command, new TextCommandCallingArgs
				{
					Caller = new Caller
					{
						Type = EnumCallerType.Console,
						CallerRole = "admin",
						CallerPrivileges = new string[] { "*" },
						FromChatGroupId = GlobalConstants.ConsoleGroup
					},
					RawArgs = new CmdArgs(args)
				}, delegate(TextCommandResult result)
				{
					if (result.StatusMessage != null)
					{
						ServerMain.Logger.Notification(result.StatusMessage);
					}
				});
				return;
			}
			if (message.StartsWith('.'))
			{
				return;
			}
			this.BroadcastMessageToAllGroups(string.Format("<strong>Admin:</strong>{0}", message), EnumChatType.AllGroups, null);
			ServerMain.Logger.Chat(string.Format("{0}: {1}", this.ServerConsoleClient.PlayerName, message.Replace("{", "{{").Replace("}", "}}")));
		}

		public string GetSaveFilename()
		{
			if (this.Config.WorldConfig.SaveFileLocation != null)
			{
				return this.Config.WorldConfig.SaveFileLocation;
			}
			return Path.Combine(GamePaths.Saves, GamePaths.DefaultSaveFilenameWithoutExtension + ".vcdbs");
		}

		public int GenerateClientId()
		{
			if (this.nextClientID + 1 < 0)
			{
				this.nextClientID = 1;
			}
			int num = this.nextClientID;
			this.nextClientID = num + 1;
			return num;
		}

		public void DisconnectPlayer(ConnectedClient client, string othersKickmessage = null, string hisKickMessage = null)
		{
			if (client == null || this.ignoreDisconnectCalls)
			{
				return;
			}
			if (!this.Clients.ContainsKey(client.Id))
			{
				return;
			}
			ServerPlayer player = client.Player;
			if (!client.IsNewClient || player != null || !string.IsNullOrEmpty(hisKickMessage))
			{
				this.ignoreDisconnectCalls = true;
				try
				{
					this.SendPacket(client.Id, ServerPackets.DisconnectPlayer(hisKickMessage));
				}
				catch
				{
				}
				LoggerBase logger = ServerMain.Logger;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(22, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Client ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(client.Id);
				defaultInterpolatedStringHandler.AppendLiteral(" disconnected: ");
				defaultInterpolatedStringHandler.AppendFormatted(hisKickMessage);
				logger.Notification(defaultInterpolatedStringHandler.ToStringAndClear());
				this.ignoreDisconnectCalls = false;
			}
			this.lastDisconnectTotalMs = this.totalUpTime.ElapsedMilliseconds;
			if (client.IsNewClient)
			{
				this.Clients.Remove(client.Id);
				client.CloseConnection();
				this.UpdateQueuedPlayersAfterDisconnect(client);
				return;
			}
			if (player != null)
			{
				if (othersKickmessage != null && othersKickmessage.Length > 0)
				{
					ServerMain.Logger.Audit("Client {0} got removed: '{1}' ({2})", new object[] { client.PlayerName, othersKickmessage, hisKickMessage });
				}
				else
				{
					ServerMain.Logger.Audit("Client {0} disconnected.", new object[] { client.PlayerName });
				}
				this.EventManager.TriggerPlayerDisconnect(player);
				ServerSystem[] systems = this.Systems;
				for (int i = 0; i < systems.Length; i++)
				{
					systems[i].OnPlayerDisconnect(player);
				}
				this.BroadcastPacket(new Packet_Server
				{
					Id = 41,
					PlayerData = new Packet_PlayerData
					{
						PlayerUID = player.PlayerUID,
						ClientId = -99
					}
				}, new IServerPlayer[] { player });
				EntityPlayer playerEntity = player.Entity;
				if (playerEntity != null)
				{
					this.DespawnEntity(playerEntity, new EntityDespawnData
					{
						Reason = EnumDespawnReason.Disconnect
					});
				}
				string playerName = client.PlayerName;
				this.Clients.Remove(client.Id);
				player.client = null;
				if (client.State == EnumClientState.Connected || client.State == EnumClientState.Playing)
				{
					if (othersKickmessage == null)
					{
						othersKickmessage = string.Format(Lang.Get("Player {0} left.", new object[] { playerName }), Array.Empty<object>());
					}
					else
					{
						othersKickmessage = string.Format(Lang.Get("Player {0} got removed. Reason: {1}", new object[] { playerName, othersKickmessage }), Array.Empty<object>());
					}
					this.SendMessageToGeneral(othersKickmessage, EnumChatType.JoinLeave, null, null);
					ServerMain.Logger.Event(othersKickmessage);
				}
				client.CloseConnection();
				this.UpdateQueuedPlayersAfterDisconnect(client);
				return;
			}
			this.Clients.Remove(client.Id);
			client.CloseConnection();
		}

		private void UpdateQueuedPlayersAfterDisconnect(ConnectedClient client)
		{
			if (this.Config.MaxClientsInQueue <= 0 || this.stopped)
			{
				return;
			}
			List<QueuedClient> nextPlayers = null;
			QueuedClient[] updatedPositions = null;
			List<QueuedClient> connectionQueue = this.ConnectionQueue;
			int count;
			lock (connectionQueue)
			{
				if (client.State == EnumClientState.Queued)
				{
					this.ConnectionQueue.RemoveAll((QueuedClient e) => e.Client.Id == client.Id);
				}
				count = this.ConnectionQueue.Count;
				if (count > 0)
				{
					int maxClients = this.Config.MaxClients;
					int clientsCount = this.Clients.Count - count;
					int clientsToConnect = Math.Max(0, maxClients - clientsCount);
					if (clientsToConnect > 0)
					{
						nextPlayers = new List<QueuedClient>();
						for (int i = 0; i < clientsToConnect; i++)
						{
							if (this.ConnectionQueue.Count > 0)
							{
								QueuedClient nextPlayer = this.ConnectionQueue.First<QueuedClient>();
								this.ConnectionQueue.RemoveAll((QueuedClient e) => e.Client.Id == nextPlayer.Client.Id);
								nextPlayers.Add(nextPlayer);
							}
						}
					}
					updatedPositions = this.ConnectionQueue.ToArray();
				}
			}
			if (count <= 0)
			{
				return;
			}
			if (nextPlayers != null)
			{
				foreach (QueuedClient nextPlayer2 in nextPlayers)
				{
					this.FinalizePlayerIdentification(nextPlayer2.Identification, nextPlayer2.Client, nextPlayer2.Entitlements);
				}
			}
			if (updatedPositions != null)
			{
				for (int j = 0; j < updatedPositions.Length; j++)
				{
					QueuedClient queuedClient = updatedPositions[j];
					Packet_Server pq = new Packet_Server
					{
						Id = 82,
						QueuePacket = new Packet_QueuePacket
						{
							Position = j + 1
						}
					};
					this.SendPacket(queuedClient.Client.Id, pq);
				}
			}
		}

		public int GetPlayingClients()
		{
			return this.Clients.Count((KeyValuePair<int, ConnectedClient> c) => c.Value.State == EnumClientState.Playing);
		}

		public int GetAllowedChunkRadius(ConnectedClient client)
		{
			int desiredChunkRadius = (int)Math.Ceiling((double)((float)((client.WorldData == null) ? 128 : client.WorldData.Viewdistance) / (float)MagicNum.ServerChunkSize));
			int reducedChunkRadius = Math.Min(this.Config.MaxChunkRadius, desiredChunkRadius);
			if (client.IsSinglePlayerClient)
			{
				return desiredChunkRadius;
			}
			return reducedChunkRadius;
		}

		public EntityPos DefaultSpawnPosition
		{
			get
			{
				return this.EntityPosFromSpawnPos((this.SaveGameData.DefaultSpawn == null) ? this.mapMiddleSpawnPos : this.SaveGameData.DefaultSpawn);
			}
		}

		public FuzzyEntityPos GetSpawnPosition(string playerUID = null, bool onlyGlobalDefaultSpawn = false, bool consumeSpawn = false)
		{
			PlayerSpawnPos playerSpawn = null;
			ServerPlayerData serverPlayerData = this.GetServerPlayerData(playerUID);
			ServerPlayer plrdata = this.PlayerByUid(playerUID) as ServerPlayer;
			PlayerRole plrrole = serverPlayerData.GetPlayerRole(this);
			float radius = 0f;
			if (plrrole.ForcedSpawn != null && !onlyGlobalDefaultSpawn)
			{
				playerSpawn = plrrole.ForcedSpawn;
				if (consumeSpawn && playerSpawn != null && playerSpawn.RemainingUses > 0)
				{
					playerSpawn.RemainingUses--;
					if (playerSpawn.RemainingUses <= 0)
					{
						plrrole.ForcedSpawn = null;
					}
				}
			}
			if (playerSpawn == null && ((plrdata != null) ? plrdata.WorldData : null) != null && !onlyGlobalDefaultSpawn)
			{
				playerSpawn = (plrdata.WorldData as ServerWorldPlayerData).SpawnPosition;
				if (consumeSpawn && playerSpawn != null && playerSpawn.RemainingUses > 0)
				{
					playerSpawn.RemainingUses--;
					if (playerSpawn.RemainingUses <= 0)
					{
						(plrdata.WorldData as ServerWorldPlayerData).SpawnPosition = null;
					}
				}
			}
			if (playerSpawn == null && !onlyGlobalDefaultSpawn)
			{
				playerSpawn = plrrole.DefaultSpawn;
				if (consumeSpawn && playerSpawn != null && playerSpawn.RemainingUses > 0)
				{
					playerSpawn.RemainingUses--;
					if (playerSpawn.RemainingUses <= 0)
					{
						plrrole.DefaultSpawn = null;
					}
				}
			}
			if (playerSpawn == null)
			{
				playerSpawn = this.SaveGameData.DefaultSpawn;
				if (playerSpawn != null)
				{
					playerSpawn.RemainingUses = 99;
				}
				radius = (float)this.World.Config.GetString("spawnRadius", null).ToInt(0);
			}
			if (playerSpawn == null)
			{
				playerSpawn = this.mapMiddleSpawnPos;
				if (playerSpawn != null)
				{
					playerSpawn.RemainingUses = 99;
				}
				radius = (float)this.World.Config.GetString("spawnRadius", null).ToInt(0);
			}
			FuzzyEntityPos fuzzyEntityPos = this.EntityPosFromSpawnPos(playerSpawn);
			fuzzyEntityPos.Radius = radius;
			fuzzyEntityPos.UsesLeft = playerSpawn.RemainingUses;
			return fuzzyEntityPos;
		}

		public EntityPos GetJoinPosition(ConnectedClient client)
		{
			PlayerRole plrgroup = client.ServerData.GetPlayerRole(this);
			if (plrgroup.ForcedSpawn != null)
			{
				return this.EntityPosFromSpawnPos(plrgroup.ForcedSpawn);
			}
			EntityPos serverPos = client.Entityplayer.ServerPos;
			EntityPos pos = client.Entityplayer.Pos;
			if (serverPos.AnyNaN())
			{
				ServerMain.Logger.Error("Player " + client.PlayerName + " has an impossible (bugged) ServerPos, placing player at world spawn.");
				serverPos.SetFrom(this.DefaultSpawnPosition);
				pos.SetFrom(this.DefaultSpawnPosition);
			}
			if (pos.AnyNaN())
			{
				ServerMain.Logger.Error("Player " + client.PlayerName + " has an impossible (bugged) Pos, placing player at world spawn.");
				serverPos.SetFrom(this.DefaultSpawnPosition);
				pos.SetFrom(this.DefaultSpawnPosition);
			}
			return serverPos;
		}

		private FuzzyEntityPos EntityPosFromSpawnPos(PlayerSpawnPos playerSpawn)
		{
			if (playerSpawn.y != null)
			{
				int? y = playerSpawn.y;
				int num = 0;
				if (!((y.GetValueOrDefault() == num) & (y != null)))
				{
					goto IL_005B;
				}
			}
			playerSpawn.y = new int?(this.WorldMap.GetTerrainGenSurfacePosY(playerSpawn.x, playerSpawn.z));
			if (playerSpawn.y == null)
			{
				return null;
			}
			IL_005B:
			if (this.WorldMap.IsValidPos(playerSpawn.x, playerSpawn.y.Value, playerSpawn.z))
			{
				return new FuzzyEntityPos((double)playerSpawn.x + 0.5, (double)playerSpawn.y.Value, (double)playerSpawn.z + 0.5, 0f, 0f, 0f)
				{
					Pitch = 3.1415927f,
					Yaw = ((playerSpawn.yaw == null) ? ((float)this.rand.Value.NextDouble() * 2f * 3.1415927f) : playerSpawn.yaw.Value)
				};
			}
			if (this.Config.RepairMode)
			{
				int x = this.SaveGameData.MapSizeX / 2;
				int z = this.SaveGameData.MapSizeZ / 2;
				return new FuzzyEntityPos((double)x, (double)this.WorldMap.GetTerrainGenSurfacePosY(x, z), (double)z, 0f, 0f, 0f);
			}
			throw new Exception("Invalid spawn coordinates found. It is outside the world map.");
		}

		private void LoadAssets()
		{
			ServerMain.Logger.Notification("Start discovering assets");
			int quantity = this.AssetManager.InitAndLoadBaseAssets(ServerMain.Logger);
			ServerMain.Logger.Notification("Found {0} base assets in total", new object[] { quantity });
		}

		public ConnectedClient GetClientByPlayername(string playerName)
		{
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if (client.PlayerName.ToLowerInvariant() == playerName.ToLowerInvariant())
				{
					return client;
				}
			}
			return null;
		}

		public void GetOnlineOrOfflinePlayer(string targetPlayerName, Action<EnumServerResponse, string> onPlayerReceived)
		{
			ConnectedClient targetClient = this.GetClientByPlayername(targetPlayerName);
			if (targetClient == null)
			{
				AuthServerComm.ResolvePlayerName(targetPlayerName, delegate(EnumServerResponse result, string playeruid)
				{
					this.EnqueueMainThreadTask(delegate
					{
						onPlayerReceived(result, playeruid);
						ServerMain.FrameProfiler.Mark("onplayerreceived");
					});
				});
				return;
			}
			onPlayerReceived(EnumServerResponse.Good, targetClient.WorldData.PlayerUID);
		}

		public void GetOnlineOrOfflinePlayerByUid(string targetPlayeruid, Action<EnumServerResponse, string> onPlayerReceived)
		{
			ConnectedClient targetClient = this.GetClientByUID(targetPlayeruid);
			if (targetClient == null)
			{
				AuthServerComm.ResolvePlayerUid(targetPlayeruid, delegate(EnumServerResponse result, string playername)
				{
					this.EnqueueMainThreadTask(delegate
					{
						onPlayerReceived(result, playername);
						ServerMain.FrameProfiler.Mark("onplayerreceived");
					});
				});
				return;
			}
			onPlayerReceived(EnumServerResponse.Good, targetClient.WorldData.PlayerUID);
		}

		public ConnectedClient GetClient(int id)
		{
			if (id == this.serverConsoleId)
			{
				return this.ServerConsoleClient;
			}
			if (!this.Clients.ContainsKey(id))
			{
				return null;
			}
			return this.Clients[id];
		}

		public ConnectedClient GetClientByUID(string playerUID)
		{
			if (this.ServerConsoleClient.WorldData.PlayerUID.Equals(playerUID, StringComparison.InvariantCultureIgnoreCase))
			{
				return this.ServerConsoleClient;
			}
			foreach (ConnectedClient client in this.Clients.Values)
			{
				ServerWorldPlayerData worldData = client.WorldData;
				if (((worldData != null) ? worldData.PlayerUID : null) != null && client.WorldData.PlayerUID.Equals(playerUID, StringComparison.InvariantCultureIgnoreCase))
				{
					return client;
				}
			}
			return null;
		}

		internal void RemapItem(Item removedItem)
		{
			while (removedItem.ItemId >= this.Items.Count)
			{
				this.Items.Add(new Item
				{
					ItemId = this.Items.Count,
					IsMissing = true
				});
			}
			if (this.Items[removedItem.ItemId] != null && this.Items[removedItem.ItemId].Code != null)
			{
				Item prevItem = this.Items[removedItem.ItemId];
				this.Items[removedItem.ItemId] = new Item();
				this.ItemsByCode.Remove(prevItem.Code);
				this.RegisterItem(prevItem);
			}
			this.Items[removedItem.ItemId] = removedItem;
			this.nextFreeItemId = Math.Max(this.nextFreeItemId, removedItem.ItemId + 1);
		}

		internal void FillMissingItem(int ItemId, Item Item)
		{
			Item noitem = new Item(0);
			while (ItemId >= this.Items.Count)
			{
				this.Items.Add(noitem);
			}
			Item.ItemId = ItemId;
			this.Items[ItemId] = Item;
			this.ItemsByCode[Item.Code] = Item;
			this.nextFreeItemId = Math.Max(this.nextFreeItemId, Item.ItemId + 1);
		}

		internal void RemapBlock(Block removedBlock)
		{
			new FastSmallDictionary<string, CompositeTexture>("all", new CompositeTexture(new AssetLocation("unknown")));
			while (removedBlock.BlockId >= this.Blocks.Count)
			{
				this.Blocks.Add(new Block
				{
					BlockId = this.Blocks.Count,
					IsMissing = true
				});
			}
			if (this.Blocks[removedBlock.BlockId] != null && this.Blocks[removedBlock.BlockId].Code != null)
			{
				Block prevBlock = this.Blocks[removedBlock.BlockId];
				this.Blocks[removedBlock.BlockId] = new Block
				{
					BlockId = removedBlock.Id
				};
				this.BlocksByCode.Remove(prevBlock.Code);
				this.RegisterBlock(prevBlock);
			}
			this.Blocks[removedBlock.BlockId] = removedBlock;
			this.nextFreeBlockId = Math.Max(this.nextFreeBlockId, removedBlock.BlockId + 1);
		}

		internal void FillMissingBlock(int blockId, Block block)
		{
			block.BlockId = blockId;
			this.Blocks[blockId] = block;
			this.BlocksByCode[block.Code] = block;
			this.nextFreeBlockId = Math.Max(this.nextFreeBlockId, block.BlockId + 1);
		}

		public void RegisterBlock(Block block)
		{
			if (block.Code == null || block.Code.Path.Length == 0)
			{
				throw new Exception(Lang.Get("Attempted to register Block with no code. Must use a unique code", Array.Empty<object>()));
			}
			if (this.BlocksByCode.ContainsKey(block.Code))
			{
				throw new Exception(Lang.Get("Block must have a unique code ('{0}' is already in use). This is often caused right after a game update when there are old installation files left behind. Try full uninstall and reinstall.", new object[] { block.Code }));
			}
			if (block.Sounds == null)
			{
				block.Sounds = new BlockSounds();
			}
			if (this.nextFreeBlockId >= this.Blocks.Count)
			{
				FastSmallDictionary<string, CompositeTexture> unknownTex = new FastSmallDictionary<string, CompositeTexture>("all", new CompositeTexture(new AssetLocation("unknown")));
				(this.Blocks as BlockList).PreAlloc(this.nextFreeBlockId + 1);
				while (this.Blocks.Count <= this.nextFreeBlockId)
				{
					this.Blocks.Add(new Block
					{
						Textures = unknownTex,
						Code = new AssetLocation("unknown"),
						BlockId = this.Blocks.Count,
						DrawType = EnumDrawType.Cube,
						MatterState = EnumMatterState.Solid,
						IsMissing = true,
						Replaceable = 1
					});
				}
			}
			block.BlockId = this.nextFreeBlockId;
			this.Blocks[this.nextFreeBlockId] = block;
			this.BlocksByCode.Add(block.Code, block);
			this.nextFreeBlockId++;
		}

		internal void RegisterItem(Item item)
		{
			if (item.Code == null || item.Code.Path.Length == 0)
			{
				throw new Exception(Lang.Get("Attempted to register Item with no code. Must use a unique code", Array.Empty<object>()));
			}
			if (this.ItemsByCode.ContainsKey(item.Code))
			{
				throw new Exception(Lang.Get("Attempted to register Item with code {0}, but an item with such code already exists. Must use a unique code", new object[] { item.Code }));
			}
			if (this.nextFreeItemId >= this.Items.Count)
			{
				while (this.Items.Count <= this.nextFreeItemId)
				{
					this.Items.Add(new Item
					{
						Textures = new Dictionary<string, CompositeTexture> { 
						{
							"all",
							new CompositeTexture(new AssetLocation("unknown"))
						} },
						Code = new AssetLocation("unknown"),
						ItemId = this.Items.Count,
						MatterState = EnumMatterState.Solid,
						IsMissing = true
					});
				}
			}
			item.ItemId = this.nextFreeItemId;
			this.Items[this.nextFreeItemId] = item;
			this.ItemsByCode.Add(item.Code, item);
			this.nextFreeItemId++;
		}

		public Item GetItem(int itemId)
		{
			if (this.Items.Count <= itemId)
			{
				return null;
			}
			return this.Items[itemId];
		}

		public Block GetBlock(int blockId)
		{
			return this.Blocks[blockId];
		}

		public EntityProperties GetEntityType(AssetLocation entityCode)
		{
			EntityProperties eclass;
			this.EntityTypesByCode.TryGetValue(entityCode, out eclass);
			return eclass;
		}

		public float[] BlockLightLevels
		{
			get
			{
				return this.blockLightLevels;
			}
		}

		public float[] SunLightLevels
		{
			get
			{
				return this.sunLightLevels;
			}
		}

		public int SeaLevel
		{
			get
			{
				return this.seaLevel;
			}
		}

		public int SunBrightness
		{
			get
			{
				return this.sunBrightness;
			}
		}

		public void SetSeaLevel(int seaLevel)
		{
			this.seaLevel = seaLevel;
		}

		public void SetBlockLightLevels(float[] lightLevels)
		{
			this.blockLightLevels = lightLevels;
		}

		public void SetSunLightLevels(float[] lightLevels)
		{
			this.sunLightLevels = lightLevels;
		}

		internal void SetSunBrightness(int lightlevel)
		{
			this.sunBrightness = lightlevel;
		}

		public bool IsDedicatedServer { get; }

		public IBlockAccessor BlockAccessor
		{
			get
			{
				return this.WorldMap.RelaxedBlockAccess;
			}
		}

		public IBulkBlockAccessor BulkBlockAccessor
		{
			get
			{
				return this.WorldMap.BulkBlockAccess;
			}
		}

		Random IWorldAccessor.Rand
		{
			get
			{
				return this.rand.Value;
			}
		}

		public long ElapsedMilliseconds
		{
			get
			{
				return this.totalUnpausedTime.ElapsedMilliseconds;
			}
		}

		public List<EntityProperties> EntityTypes
		{
			get
			{
				List<EntityProperties> list;
				if ((list = this.entityTypesCached) == null)
				{
					list = (this.entityTypesCached = this.EntityTypesByCode.Values.ToList<EntityProperties>());
				}
				return list;
			}
		}

		public List<string> EntityTypeCodes
		{
			get
			{
				List<string> list;
				if ((list = this.entityCodesCached) == null)
				{
					list = (this.entityCodesCached = this.makeEntityCodesCache());
				}
				return list;
			}
		}

		private List<string> makeEntityCodesCache()
		{
			ICollection<AssetLocation> keys = this.EntityTypesByCode.Keys;
			List<string> list = new List<string>(keys.Count);
			foreach (AssetLocation key in keys)
			{
				list.Add(key.ToShortString());
			}
			return list;
		}

		public int DefaultEntityTrackingRange
		{
			get
			{
				return MagicNum.DefaultEntityTrackingRange;
			}
		}

		List<GridRecipe> IWorldAccessor.GridRecipes
		{
			get
			{
				return this.GridRecipes;
			}
		}

		List<CollectibleObject> IWorldAccessor.Collectibles
		{
			get
			{
				return this.Collectibles;
			}
		}

		IList<Block> IWorldAccessor.Blocks
		{
			get
			{
				return this.Blocks;
			}
		}

		IList<Item> IWorldAccessor.Items
		{
			get
			{
				return this.Items;
			}
		}

		List<EntityProperties> IWorldAccessor.EntityTypes
		{
			get
			{
				return this.EntityTypes;
			}
		}

		List<string> IWorldAccessor.EntityTypeCodes
		{
			get
			{
				return this.EntityTypeCodes;
			}
		}

		public Dictionary<string, string> RemappedEntities
		{
			get
			{
				return this.EntityCodeRemappings;
			}
		}

		public string WorldName
		{
			get
			{
				return this.SaveGameData.WorldName;
			}
		}

		public OrderedDictionary<AssetLocation, ITreeGenerator> TreeGenerators
		{
			get
			{
				return this.TreeGeneratorsByTreeCode;
			}
		}

		public IPlayer[] AllOnlinePlayers
		{
			get
			{
				return (from c in this.Clients.Values
					select c.Player into c
					where c != null
					select c).ToArray<ServerPlayer>();
			}
		}

		public IPlayer[] AllPlayers
		{
			get
			{
				return this.PlayersByUid.Values.ToArray<ServerPlayer>();
			}
		}

		public bool EntityDebugMode
		{
			get
			{
				return this.Config.EntityDebugMode;
			}
		}

		IClassRegistryAPI IWorldAccessor.ClassRegistry
		{
			get
			{
				return this.api.ClassRegistry;
			}
		}

		public CollisionTester CollisionTester
		{
			get
			{
				return this.collTester;
			}
		}

		ConcurrentDictionary<long, Entity> IServerWorldAccessor.LoadedEntities
		{
			get
			{
				return this.LoadedEntities;
			}
		}

		public ConnectedClient GetConnectedClient(string playerUID)
		{
			foreach (ConnectedClient client in this.Clients.Values)
			{
				ServerWorldPlayerData worldData = client.WorldData;
				if (((worldData != null) ? worldData.PlayerUID : null) == playerUID)
				{
					return client;
				}
			}
			return null;
		}

		public IWorldPlayerData GetWorldPlayerData(string playerUID)
		{
			if (playerUID == null)
			{
				return null;
			}
			ServerWorldPlayerData plrdata;
			this.PlayerDataManager.WorldDataByUID.TryGetValue(playerUID, out plrdata);
			if (plrdata != null)
			{
				return plrdata;
			}
			ConnectedClient client = this.GetConnectedClient(playerUID);
			if (client != null)
			{
				return client.WorldData;
			}
			return null;
		}

		public ServerPlayerData FindServerPlayerDataByLastKnownPlayerName(string playerName)
		{
			foreach (ServerPlayerData plrData in this.PlayerDataManager.PlayerDataByUid.Values)
			{
				if (plrData.LastKnownPlayername.ToLowerInvariant() == playerName.ToLowerInvariant())
				{
					return plrData;
				}
			}
			return null;
		}

		public ServerPlayerData GetServerPlayerData(string playeruid)
		{
			ServerPlayerData plrData;
			this.PlayerDataManager.PlayerDataByUid.TryGetValue(playeruid, out plrData);
			return plrData;
		}

		public bool PlayerHasPrivilege(int clientid, string privilege)
		{
			return privilege == null || clientid == this.serverConsoleId || (this.Clients.ContainsKey(clientid) && this.Clients[clientid].ServerData.HasPrivilege(privilege, this.Config.RolesByCode));
		}

		public void PlaySoundAt(string location, IPlayer atPlayer, IPlayer ignorePlayerUID = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAt(new AssetLocation(location), atPlayer, ignorePlayerUID, randomizePitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, EnumSoundType soundType, float pitch, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAtExceptPlayer(location, posx, posy, posz, (dualCallByPlayer != null) ? new int?(dualCallByPlayer.ClientId) : null, pitch, range, volume, soundType);
		}

		public void PlaySoundAt(AssetLocation location, Entity entity, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			float yoff = 0f;
			if (entity.SelectionBox != null)
			{
				yoff = entity.SelectionBox.Y2 / 2f;
			}
			else
			{
				EntityProperties properties = entity.Properties;
				if (((properties != null) ? properties.CollisionBoxSize : null) != null)
				{
					yoff = entity.Properties.CollisionBoxSize.Y / 2f;
				}
			}
			this.PlaySoundAt(location, entity.ServerPos.X, entity.ServerPos.InternalY + (double)yoff, entity.ServerPos.Z, dualCallByPlayer, randomizePitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, IPlayer atPlayer, IPlayer ignorePlayerUID = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			if (atPlayer == null)
			{
				return;
			}
			int? clientId = null;
			if (ignorePlayerUID != null)
			{
				ConnectedClient connectedClient = this.GetConnectedClient(ignorePlayerUID.PlayerUID);
				clientId = ((connectedClient != null) ? new int?(connectedClient.Id) : null);
			}
			float pitch = (randomizePitch ? base.RandomPitch() : 1f);
			this.PlaySoundAtExceptPlayer(location, atPlayer.Entity.Pos.X, atPlayer.Entity.Pos.InternalY, atPlayer.Entity.Pos.Z, clientId, pitch, range, volume, EnumSoundType.Sound);
		}

		public void PlaySoundAt(AssetLocation location, Entity entity, IPlayer ignorePlayerUID, float pitch, float range = 32f, float volume = 1f)
		{
			float yoff = 0f;
			if (entity.SelectionBox != null)
			{
				yoff = entity.SelectionBox.Y2 / 2f;
			}
			else
			{
				EntityProperties properties = entity.Properties;
				if (((properties != null) ? properties.CollisionBoxSize : null) != null)
				{
					yoff = entity.Properties.CollisionBoxSize.Y / 2f;
				}
			}
			this.PlaySoundAt(location, entity.ServerPos.X, entity.ServerPos.InternalY + (double)yoff, entity.ServerPos.Z, ignorePlayerUID, pitch, range, volume);
		}

		public void PlaySoundAt(string location, double posx, double posy, double posz, IPlayer ignorePlayerUID = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAt(new AssetLocation(location), posx, posy, posz, ignorePlayerUID, randomizePitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, BlockPos pos, double yOffsetFromCenter, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAt(location, (double)pos.X + 0.5, (double)pos.InternalY + 0.5 + yOffsetFromCenter, (double)pos.Z + 0.5, ignorePlayerUid, randomizePitch, range, volume);
		}

		public void PlaySoundAt(string location, Entity entity, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			this.PlaySoundAt(new AssetLocation(location), entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, dualCallByPlayer, randomizePitch, range, volume);
		}

		public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer ignorePlayerUID = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			if (location == null)
			{
				return;
			}
			int? clientId = null;
			if (ignorePlayerUID != null)
			{
				ConnectedClient connectedClient = this.GetConnectedClient(ignorePlayerUID.PlayerUID);
				clientId = ((connectedClient != null) ? new int?(connectedClient.Id) : null);
			}
			float pitch = (randomizePitch ? base.RandomPitch() : 1f);
			this.PlaySoundAtExceptPlayer(location, posx, posy, posz, clientId, pitch, range, volume, EnumSoundType.Sound);
		}

		public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer ignorePlayerUID, float pitch, float range = 32f, float volume = 1f)
		{
			if (location == null)
			{
				return;
			}
			int? clientId = null;
			if (ignorePlayerUID != null)
			{
				ConnectedClient connectedClient = this.GetConnectedClient(ignorePlayerUID.PlayerUID);
				clientId = ((connectedClient != null) ? new int?(connectedClient.Id) : null);
			}
			this.PlaySoundAtExceptPlayer(location, posx, posy, posz, clientId, pitch, range, volume, EnumSoundType.Sound);
		}

		public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, bool randomizePitch = true, float range = 32f, float volume = 1f)
		{
			float pitch = (randomizePitch ? base.RandomPitch() : 1f);
			this.SendSound(forPlayer as IServerPlayer, location, 0.0, 0.0, 0.0, pitch, range, volume, EnumSoundType.Sound);
		}

		public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, float pitch, float range = 32f, float volume = 1f)
		{
			this.SendSound(forPlayer as IServerPlayer, location, 0.0, 0.0, 0.0, pitch, range, volume, EnumSoundType.Sound);
		}

		public void PlaySoundAtExceptPlayer(AssetLocation location, double posx, double posy, double posz, int? clientId = null, float pitch = 1f, float range = 32f, float volume = 1f, EnumSoundType soundType = EnumSoundType.Sound)
		{
			if (location == null)
			{
				return;
			}
			foreach (ConnectedClient client in this.Clients.Values)
			{
				int? num = clientId;
				int id = client.Id;
				if (!((num.GetValueOrDefault() == id) & (num != null)) && client.State == EnumClientState.Playing && client.Position.InRangeOf(posx, posy, posz, range * range))
				{
					this.SendSound(client.Player, location, posx, posy, posz, pitch, range, volume, soundType);
				}
			}
		}

		public void TriggerNeighbourBlocksUpdate(BlockPos pos)
		{
			Block liquidBlock = this.WorldMap.RelaxedBlockAccess.GetBlock(pos, 2);
			if (liquidBlock.IsLiquid())
			{
				liquidBlock.OnNeighbourBlockChange(this, pos, pos);
			}
			BlockPos neibPos = new BlockPos(pos.dimension);
			foreach (BlockFacing facing in BlockFacing.ALLFACES)
			{
				neibPos.Set(pos).Offset(facing);
				if (this.worldmap.IsValidPos(neibPos))
				{
					Block block = this.WorldMap.RelaxedBlockAccess.GetBlock(neibPos);
					block.OnNeighbourBlockChange(this, neibPos, pos);
					if (!block.ForFluidsLayer)
					{
						liquidBlock = this.WorldMap.RelaxedBlockAccess.GetBlock(neibPos, 2);
						if (liquidBlock.BlockId != 0)
						{
							EnumHandling handled = EnumHandling.PassThrough;
							BlockBehavior[] blockBehaviors = liquidBlock.BlockBehaviors;
							for (int j = 0; j < blockBehaviors.Length; j++)
							{
								blockBehaviors[j].OnNeighbourBlockChange(this, neibPos, pos, ref handled);
								if (handled == EnumHandling.PreventSubsequent)
								{
									break;
								}
							}
						}
					}
				}
			}
		}

		internal Entity GetEntity(long entityId)
		{
			Entity entity;
			this.LoadedEntities.TryGetValue(entityId, out entity);
			return entity;
		}

		public override bool IsValidPos(BlockPos pos)
		{
			return this.WorldMap.IsValidPos(pos);
		}

		public override Vec3i MapSize
		{
			get
			{
				return this.WorldMap.MapSize;
			}
		}

		ITreeAttribute IWorldAccessor.Config
		{
			get
			{
				SaveGame saveGameData = this.SaveGameData;
				if (saveGameData == null)
				{
					return null;
				}
				return saveGameData.WorldConfiguration;
			}
		}

		public override IBlockAccessor blockAccessor
		{
			get
			{
				return this.WorldMap.RelaxedBlockAccess;
			}
		}

		public override Block GetBlock(BlockPos pos)
		{
			return this.WorldMap.RelaxedBlockAccess.GetBlock(pos);
		}

		public bool IsFullyLoadedChunk(BlockPos pos)
		{
			ServerChunk chunk = (ServerChunk)this.WorldMap.GetChunk(pos);
			return chunk != null && chunk.NotAtEdge;
		}

		public Entity SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d velocity = null)
		{
			if (itemstack == null || itemstack.StackSize <= 0)
			{
				return null;
			}
			Entity entity = EntityItem.FromItemstack(itemstack, position, velocity, this);
			this.SpawnEntity(entity);
			return entity;
		}

		public Entity SpawnItemEntity(ItemStack itemstack, BlockPos pos, Vec3d velocity = null)
		{
			return this.SpawnItemEntity(itemstack, pos.ToVec3d().Add(0.5), velocity);
		}

		public bool LoadEntity(Entity entity, long fromChunkIndex3d)
		{
			bool flag;
			try
			{
				if (this.Config.RepairMode)
				{
					this.SaveGameData.LastEntityId = Math.Max(this.SaveGameData.LastEntityId, entity.EntityId);
				}
				EntityProperties type = this.api.World.GetEntityType(entity.Code);
				if (type == null)
				{
					ServerMain.Logger.Warning("Couldn't load entity class {0} saved type code {1} - its Type is null! Will remove from chunk, sorry!", new object[]
					{
						entity.GetType(),
						entity.Code
					});
					flag = false;
				}
				else
				{
					entity.Initialize(type.Clone(), this.api, fromChunkIndex3d);
					entity.AfterInitialized(false);
					if (!this.LoadedEntities.TryAdd(entity.EntityId, entity))
					{
						ServerMain.Logger.Warning("Couldn't add entity {0}, type {1} to list of loaded entities (duplicate entityid)! Will remove from chunk, sorry!", new object[]
						{
							entity.EntityId,
							entity.Properties.Code
						});
						flag = false;
					}
					else
					{
						entity.OnEntityLoaded();
						this.EventManager.TriggerEntityLoaded(entity);
						flag = true;
					}
				}
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Couldn't add entity type {0} at {1} due to exception in code. Will remove from chunk, sorry!", new object[]
				{
					entity.Code,
					entity.ServerPos.OnlyPosToString()
				});
				ServerMain.Logger.Error(e);
				flag = false;
			}
			return flag;
		}

		public void SpawnEntity(Entity entity)
		{
			this.SpawnEntity(entity, this.GetEntityType(entity.Code));
		}

		public void SpawnPriorityEntity(Entity entity)
		{
			this.SpawnEntity_internal(this.GetEntityType(entity.Code), entity);
			this.ServerUdpNetwork.physicsManager.SendPrioritySpawn(entity, this.Clients.Values);
		}

		public void SpawnEntity(Entity entity, EntityProperties type)
		{
			this.SpawnEntity_internal(this.GetEntityType(entity.Code), entity);
			List<Entity> entitySpawnSendQueue = this.EntitySpawnSendQueue;
			lock (entitySpawnSendQueue)
			{
				this.EntitySpawnSendQueue.Add(entity);
			}
		}

		private void SpawnEntity_internal(EntityProperties type, Entity entity)
		{
			if (this.Config.RepairMode && !(entity is EntityPlayer))
			{
				ServerMain.Logger.Warning("Rejected one entity spawn. Server in repair mode. Will not spawn new entities.");
				return;
			}
			SaveGame saveGameData = this.SaveGameData;
			long num = saveGameData.LastEntityId + 1L;
			saveGameData.LastEntityId = num;
			long entityid = num;
			long chunkindex3d = this.WorldMap.ChunkIndex3D(entity.ServerPos);
			entity.EntityId = entityid;
			entity.DespawnReason = null;
			if (type == null)
			{
				ServerMain.Logger.Error("Couldn't spawn entity {0} with id {1} and code {2} - it's Type is null!", new object[]
				{
					entity.GetType(),
					entityid,
					entity.Code
				});
				return;
			}
			entity.Initialize(type.Clone(), this.api, chunkindex3d);
			entity.AfterInitialized(true);
			this.AddEntityToChunk(entity, (int)entity.ServerPos.X, (int)entity.ServerPos.InternalY, (int)entity.ServerPos.Z);
			if (!this.LoadedEntities.TryAdd(entityid, entity))
			{
				ServerMain.Logger.Warning("SpawnEntity: Duplicate entity id discovered, will updating SaveGameData.LastEntityId to reflect this. This was likely caused by an ungraceful server exit.");
				this.RemoveEntityFromChunk(entity, (int)entity.ServerPos.X, (int)entity.ServerPos.InternalY, (int)entity.ServerPos.Z);
				this.SaveGameData.LastEntityId = this.LoadedEntities.Max((KeyValuePair<long, Entity> val) => val.Value.EntityId);
				SaveGame saveGameData2 = this.SaveGameData;
				num = saveGameData2.LastEntityId + 1L;
				saveGameData2.LastEntityId = num;
				entityid = (entity.EntityId = num);
				if (!this.LoadedEntities.TryAdd(entityid, entity))
				{
					ServerMain.Logger.Warning("SpawnEntity: Still not able to add entity after updating LastEntityId. Looks like a programming error. Killing server...");
					throw new Exception("Unable to spawn entity");
				}
				this.AddEntityToChunk(entity, (int)entity.ServerPos.X, (int)entity.ServerPos.InternalY, (int)entity.ServerPos.Z);
			}
			entity.OnEntitySpawn();
			this.EventManager.TriggerEntitySpawned(entity);
		}

		public long GetNextHerdId()
		{
			SaveGame saveGameData = this.SaveGameData;
			long num = saveGameData.LastHerdId + 1L;
			saveGameData.LastHerdId = num;
			return num;
		}

		public void DespawnEntity(Entity entity, EntityDespawnData despawnData)
		{
			entity.OnEntityDespawn(despawnData);
			ServerMain.FrameProfiler.Mark("despawned-1-", entity.Code.Path);
			Entity useless;
			this.LoadedEntities.TryRemove(entity.EntityId, out useless);
			if (despawnData == null || despawnData.Reason != EnumDespawnReason.Unload)
			{
				this.RemoveEntityFromChunk(entity, (int)entity.ServerPos.X, (int)entity.ServerPos.Y, (int)entity.ServerPos.Z);
			}
			this.EntityDespawnSendQueue.Add(new KeyValuePair<Entity, EntityDespawnData>(entity, entity.DespawnReason));
			entity.State = EnumEntityState.Despawned;
			ServerMain.FrameProfiler.Mark("despawned-2-", entity.Code.Path);
			this.EventManager.TriggerEntityDespawned(entity, despawnData);
		}

		private void AddEntityToChunk(Entity entity, int x, int y, int z)
		{
			ServerChunk c = this.WorldMap.GetServerChunk(x / MagicNum.ServerChunkSize, y / MagicNum.ServerChunkSize, z / MagicNum.ServerChunkSize);
			if (c != null)
			{
				c.AddEntity(entity);
			}
		}

		private void RemoveEntityFromChunk(Entity entity, int x, int y, int z)
		{
			ServerChunk c = this.WorldMap.GetServerChunk(entity.InChunkIndex3d);
			if (c == null)
			{
				return;
			}
			c.RemoveEntity(entity.EntityId);
		}

		public Entity GetNearestEntity(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
		{
			return base.GetEntitiesAround(position, horRange, vertRange, matches).MinBy((Entity entity) => entity.Pos.SquareDistanceTo(position));
		}

		public Entity GetEntityById(long entityId)
		{
			Entity entity;
			this.LoadedEntities.TryGetValue(entityId, out entity);
			return entity;
		}

		public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.EventManager.AddGameTickListener(OnGameTick, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterGameTickListener(Action<float> OnGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.EventManager.AddGameTickListener(OnGameTick, errorHandler, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
		{
			return this.EventManager.AddDelayedCallback(OnTimePassed, (long)millisecondDelay);
		}

		public long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
		{
			return this.EventManager.AddGameTickListener(OnGameTick, pos, millisecondInterval, initialDelayOffsetMs);
		}

		public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
		{
			return this.EventManager.AddDelayedCallback(OnTimePassed, pos, (long)millisecondDelay);
		}

		public long RegisterCallbackUnique(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval)
		{
			return this.EventManager.AddSingleDelayedCallback(OnGameTick, pos, (long)millisecondInterval);
		}

		public void UnregisterCallback(long callbackId)
		{
			if (callbackId > 0L)
			{
				this.EventManager.RemoveDelayedCallback(callbackId);
			}
		}

		public void UnregisterGameTickListener(long listenerId)
		{
			if (listenerId > 0L)
			{
				this.EventManager.RemoveGameTickListener(listenerId);
			}
		}

		public void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale = 1f, EnumParticleModel model = EnumParticleModel.Quad, IPlayer dualCallByPlayer = null)
		{
			SimpleParticleProperties props = new SimpleParticleProperties(quantity, quantity, color, minPos, maxPos, minVelocity, maxVelocity, lifeLength, gravityEffect, 1f, 1f, EnumParticleModel.Cube);
			props.ParticleModel = model;
			SimpleParticleProperties simpleParticleProperties = props;
			props.MaxSize = scale;
			simpleParticleProperties.MinSize = scale;
			this.SpawnParticles(props, dualCallByPlayer);
		}

		public void SpawnParticles(IParticlePropertiesProvider provider, IPlayer dualCallByPlayer = null)
		{
			string className = ServerMain.ClassRegistry.ParticleProviderTypeToClassnameMapping[provider.GetType()];
			Packet_SpawnParticles p = new Packet_SpawnParticles();
			p.ParticlePropertyProviderClassName = className;
			using (MemoryStream ms = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(ms);
				provider.ToBytes(writer);
				p.SetData(ms.ToArray());
			}
			Packet_Server packet = new Packet_Server
			{
				Id = 61,
				SpawnParticles = p
			};
			provider.BeginParticle();
			Vec3d pos = provider.Pos;
			long chunkindex3d = this.WorldMap.ChunkIndex3D((int)pos.X / 32, (int)pos.Y / 32, (int)pos.Z / 32);
			this.Serialize_(packet);
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if (client.IsPlayingClient && client.Player != dualCallByPlayer && client.DidSendChunk(chunkindex3d))
				{
					this.SendPacket(client.Id, ServerMain.reusableBuffer);
				}
			}
		}

		public void SpawnCubeParticles(Vec3d pos, ItemStack stack, float radius, int quantity, float scale = 0.5f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
		{
			this.SpawnParticles(new StackCubeParticles(pos, stack, radius, quantity, scale, velocity), dualCallByPlayer);
		}

		public void SpawnCubeParticles(BlockPos blockpos, Vec3d pos, float radius, int quantity, float scale = 0.5f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
		{
			this.SpawnParticles(new BlockCubeParticles(this, blockpos, pos, radius, quantity, scale, velocity), dualCallByPlayer);
		}

		public void CreateExplosion(BlockPos pos, EnumBlastType blastType, double destructionRadius, double injureRadius, float blockDropChanceMultiplier = 1f, string ignitedByPlayerUid = null)
		{
			destructionRadius = GameMath.Clamp(destructionRadius, 1.0, 16.0);
			double num = Math.Max(1.2000000476837158 * destructionRadius, injureRadius);
			if (num > (double)ShapeUtil.MaxShells)
			{
				throw new ArgumentOutOfRangeException("Radius cannot be greater than " + ((int)((float)ShapeUtil.MaxShells / 1.2f)).ToString());
			}
			Vec3f[] shellPositions = ShapeUtil.GetCachedCubicShellNormalizedVectors((int)num);
			double minDestroRadius = 0.800000011920929 * destructionRadius;
			double addDestroRadius = 0.4000000059604645 * destructionRadius;
			BlockPos tmpPos = new BlockPos();
			int maxRadiusCeil = (int)Math.Ceiling(num);
			BlockPos minPos = pos.AddCopy(-maxRadiusCeil);
			BlockPos maxPos = pos.AddCopy(maxRadiusCeil);
			this.WorldMap.PrefetchBlockAccess.PrefetchBlocks(minPos, maxPos);
			DamageSource testSrc = new DamageSource
			{
				Source = EnumDamageSource.Explosion,
				SourcePos = pos.ToVec3d(),
				Type = EnumDamageType.BluntAttack
			};
			Entity[] entities = base.GetEntitiesAround(pos.ToVec3d(), (float)injureRadius + 2f, (float)injureRadius + 2f, (Entity e) => e.ShouldReceiveDamage(testSrc, (float)injureRadius));
			Dictionary<long, double> strongestRayOnEntity = new Dictionary<long, double>();
			for (int i = 0; i < entities.Length; i++)
			{
				strongestRayOnEntity[entities[i].EntityId] = 0.0;
			}
			ExplosionSmokeParticles particleProvider = new ExplosionSmokeParticles();
			particleProvider.basePos = new Vec3d((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5);
			Dictionary<BlockPos, Block> explodedBlocks = new Dictionary<BlockPos, Block>();
			Cuboidd testBox = Block.DefaultCollisionBox.ToDouble();
			for (int j = 0; j < shellPositions.Length; j++)
			{
				double curDestroStrength;
				double num2 = (curDestroStrength = minDestroRadius + this.rand.Value.NextDouble() * addDestroRadius);
				double curInjureStrength = injureRadius;
				double maxStrength = Math.Max(num2, injureRadius);
				Vec3f vec = shellPositions[j];
				for (double r = 0.0; r < maxStrength; r += 0.25)
				{
					tmpPos.Set(pos.X + (int)((double)vec.X * r + 0.5), pos.Y + (int)((double)vec.Y * r + 0.5), pos.Z + (int)((double)vec.Z * r + 0.5));
					if (!this.worldmap.IsValidPos(tmpPos))
					{
						break;
					}
					curDestroStrength -= 0.25;
					curInjureStrength -= 0.25;
					if (!explodedBlocks.ContainsKey(tmpPos))
					{
						Block block = this.WorldMap.PrefetchBlockAccess.GetBlock(tmpPos);
						double resist = block.GetBlastResistance(this, tmpPos, vec, blastType);
						curDestroStrength -= resist;
						if (curDestroStrength > 0.0)
						{
							explodedBlocks[tmpPos.Copy()] = block;
							curInjureStrength -= resist;
						}
						if (curDestroStrength <= 0.0 && resist > 0.0)
						{
							curInjureStrength = 0.0;
						}
					}
					if (curDestroStrength <= 0.0 && curInjureStrength <= 0.0)
					{
						break;
					}
					if (curInjureStrength > 0.0)
					{
						foreach (Entity entity in entities)
						{
							testBox.Set((double)tmpPos.X, (double)tmpPos.Y, (double)tmpPos.Z, (double)(tmpPos.X + 1), (double)(tmpPos.Y + 1), (double)(tmpPos.Z + 1));
							if (testBox.IntersectsOrTouches(entity.SelectionBox, entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z))
							{
								strongestRayOnEntity[entity.EntityId] = Math.Max(strongestRayOnEntity[entity.EntityId], curInjureStrength);
							}
						}
					}
				}
			}
			foreach (Entity entity2 in entities)
			{
				double strength = strongestRayOnEntity[entity2.EntityId];
				if (strength != 0.0)
				{
					double damage = Math.Max(injureRadius / Math.Max(0.5, injureRadius - strength), strength);
					if (damage >= 0.25)
					{
						DamageSource src = new DamageSource
						{
							Source = EnumDamageSource.Explosion,
							Type = EnumDamageType.BluntAttack,
							SourcePos = new Vec3d((double)pos.X + 0.5, (double)pos.Y, (double)pos.Z + 0.5)
						};
						entity2.ReceiveDamage(src, (float)damage);
					}
				}
			}
			particleProvider.AddBlocks(explodedBlocks);
			foreach (KeyValuePair<BlockPos, Block> val in explodedBlocks)
			{
				if (val.Value.BlockMaterial != EnumBlockMaterial.Air)
				{
					val.Value.OnBlockExploded(this, val.Key, pos, blastType, ignitedByPlayerUid);
				}
			}
			this.WorldMap.BulkBlockAccess.Commit();
			foreach (KeyValuePair<BlockPos, Block> val2 in explodedBlocks)
			{
				this.TriggerNeighbourBlocksUpdate(val2.Key);
			}
			string soundName = "effect/smallexplosion";
			if (destructionRadius > 12.0)
			{
				soundName = "effect/largeexplosion";
			}
			else if (destructionRadius > 6.0)
			{
				soundName = "effect/mediumexplosion";
			}
			this.PlaySoundAt("sounds/" + soundName, (double)pos.X + 0.5, (double)pos.InternalY + 0.5, (double)pos.Z + 0.5, null, false, (float)(24.0 * Math.Pow(destructionRadius, 0.5)), 1f);
			SimpleParticleProperties p = ExplosionParticles.ExplosionFireParticles;
			float mul = (float)destructionRadius / 3f;
			p.MinPos.Set((double)pos.X, (double)pos.Y, (double)pos.Z);
			p.MinQuantity = 100f * mul;
			p.AddQuantity = (float)((int)(20.0 * Math.Pow(destructionRadius, 0.75)));
			this.SpawnParticles(p, null);
			AdvancedParticleProperties p2 = ExplosionParticles.ExplosionFireTrailCubicles;
			p2.Velocity = new NatFloat[]
			{
				NatFloat.createUniform(0f, 8f + mul),
				NatFloat.createUniform(3f + mul, 3f + mul),
				NatFloat.createUniform(0f, 8f + mul)
			};
			p2.basePos.Set((double)pos.X + 0.5, (double)pos.InternalY + 0.5, (double)pos.Z + 0.5);
			p2.GravityEffect = NatFloat.createUniform(0.5f, 0f);
			p2.LifeLength = NatFloat.createUniform(1.5f * mul, 0.5f);
			p2.Quantity = NatFloat.createUniform(30f * mul, 10f);
			float f2 = (float)Math.Pow((double)mul, 0.75);
			p2.Size = NatFloat.createUniform(0.5f * f2, 0.2f * f2);
			p2.SecondaryParticles[0].Size = NatFloat.createUniform(0.25f * (float)Math.Pow((double)mul, 0.5), 0.05f * f2);
			this.SpawnParticles(p2, null);
			this.SpawnParticles(particleProvider, null);
			TreeAttribute tree = new TreeAttribute();
			tree.SetBlockPos("pos", pos);
			tree.SetInt("blasttype", (int)blastType);
			tree.SetDouble("destructionRadius", destructionRadius);
			tree.SetDouble("injureRadius", injureRadius);
			this.api.eventapi.PushEvent("onexplosion", tree);
		}

		public IWorldPlayerData GetWorldPlayerData(int clientID)
		{
			if (!this.Clients.ContainsKey(clientID))
			{
				return null;
			}
			return this.Clients[clientID].WorldData;
		}

		public IPlayer NearestPlayer(double x, double y, double z)
		{
			IPlayer closestplayer = null;
			float closestSqDistance = float.MaxValue;
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if (client.State == EnumClientState.Playing && client.Entityplayer != null)
				{
					float distanceSq = client.Position.SquareDistanceTo(x, y, z);
					if (distanceSq < closestSqDistance)
					{
						closestSqDistance = distanceSq;
						closestplayer = client.Player;
					}
				}
			}
			return closestplayer;
		}

		public IPlayer[] GetPlayersAround(Vec3d position, float horRange, float vertRange, ActionConsumable<IPlayer> matches = null)
		{
			List<IPlayer> players = new List<IPlayer>();
			float horRangeSq = horRange * horRange;
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if (client.State == EnumClientState.Playing && client.Entityplayer != null && client.Position.InRangeOf(position, horRangeSq, vertRange) && (matches == null || matches(client.Player)))
				{
					players.Add(client.Player);
				}
			}
			return players.ToArray();
		}

		public IPlayer PlayerByUid(string playerUid)
		{
			if (playerUid == null)
			{
				return null;
			}
			ServerPlayer plr;
			this.PlayersByUid.TryGetValue(playerUid, out plr);
			return plr;
		}

		public void EnqueueMainThreadTask(Action task)
		{
			if (task == null)
			{
				throw new ArgumentNullException();
			}
			object obj = this.mainThreadTasksLock;
			lock (obj)
			{
				this.mainThreadTasks.Enqueue(task);
			}
		}

		public void ProcessMainThreadTasks()
		{
			if (ServerMain.FrameProfiler != null && ServerMain.FrameProfiler.Enabled)
			{
				ServerMain.FrameProfiler.Enter("mainthreadtasks");
				while (this.mainThreadTasks.Count > 0)
				{
					object obj = this.mainThreadTasksLock;
					Action task;
					lock (obj)
					{
						task = this.mainThreadTasks.Dequeue();
					}
					task();
					if (task.Target != null)
					{
						string code = task.Target.GetType().ToString();
						ServerMain.FrameProfiler.Mark(code);
					}
				}
				ServerMain.FrameProfiler.Leave();
				return;
			}
			while (this.mainThreadTasks.Count > 0)
			{
				object obj = this.mainThreadTasksLock;
				Action task2;
				lock (obj)
				{
					task2 = this.mainThreadTasks.Dequeue();
				}
				task2();
			}
		}

		public void HighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
		{
			this.SendHighlightBlocksPacket((IServerPlayer)player, slotId, blocks, colors, mode, shape, scale);
		}

		public void HighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary)
		{
			this.SendHighlightBlocksPacket((IServerPlayer)player, slotId, blocks, null, mode, shape, 1f);
		}

		private void InitBasicPacketHandlers()
		{
			this.PacketHandlers[1] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandlePlayerIdentification);
			this.PacketHandlers[11] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleRequestJoin);
			this.PacketHandlers[20] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleRequestModeChange);
			this.PacketHandlers[4] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleChatLine);
			this.PacketHandlers[13] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleSelectedHotbarSlot);
			this.PacketHandlers[14] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleLeave);
			this.PacketHandlers[21] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleMoveKeyChange);
			this.PacketHandlers[31] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleEntityPacket);
			this.PacketHandlers[26] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleClientLoaded);
			this.PacketHandlers[29] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleClientPlaying);
			this.PacketHandlers[34] = new ClientPacketHandler<Packet_Client, ConnectedClient>(this.HandleRequestPositionTcp);
			this.PacketHandlingOnConnectingAllowed[1] = true;
			this.PacketHandlingOnConnectingAllowed[14] = true;
			this.PacketHandlingOnConnectingAllowed[34] = true;
		}

		private void HandleRequestPositionTcp(Packet_Client packet, ConnectedClient player)
		{
			player.FallBackToTcp = true;
			LoggerBase logger = ServerMain.Logger;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 2);
			defaultInterpolatedStringHandler.AppendLiteral("UDP: Client ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(player.Id);
			defaultInterpolatedStringHandler.AppendLiteral(" [");
			defaultInterpolatedStringHandler.AppendFormatted(player.PlayerName);
			defaultInterpolatedStringHandler.AppendLiteral("] requests to get positions over TCP.");
			logger.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public void PacketParsingLoop()
		{
			for (int i = 0; i < this.MainSockets.Length; i++)
			{
				NetServer mainSocket = this.MainSockets[i];
				if (mainSocket != null)
				{
					NetIncomingMessage msg;
					while ((msg = mainSocket.ReadMessage()) != null)
					{
						this.ProcessNetMessage(msg, mainSocket);
					}
				}
			}
		}

		private void ProcessNetMessage(NetIncomingMessage msg, NetServer mainSocket)
		{
			if (this.RunPhase == EnumServerRunPhase.Shutdown || this.exit.exit)
			{
				return;
			}
			if (msg.SenderConnection == null)
			{
				return;
			}
			switch (msg.Type)
			{
			case NetworkMessageType.Data:
			{
				ConnectedClient client = msg.SenderConnection.client;
				if (client == null)
				{
					return;
				}
				this.TotalReceivedBytes += (long)msg.messageLength;
				this.ParseClientPacket_offthread(client, msg.message, msg.messageLength);
				return;
			}
			case NetworkMessageType.Connect:
			{
				if (this.RunPhase == EnumServerRunPhase.Standby)
				{
					this.EnqueueMainThreadTask(delegate
					{
						this.Launch();
					});
				}
				NetConnection clientConnection = msg.SenderConnection;
				this.lastClientId = this.GenerateClientId();
				ConnectedClient newClient = new ConnectedClient(this.lastClientId)
				{
					Socket = clientConnection
				};
				clientConnection.client = newClient;
				newClient.Ping.SetTimeoutThreshold(this.Config.ClientConnectionTimeout);
				newClient.Ping.TimeReceivedUdp = this.ElapsedMilliseconds;
				this.ClientPackets.Enqueue(new ReceivedClientPacket(newClient));
				return;
			}
			case NetworkMessageType.Disconnect:
			{
				ConnectedClient client2 = msg.SenderConnection.client;
				if (client2 == null)
				{
					return;
				}
				this.DisconnectedClientsThisTick.Add(client2.Id);
				this.ClientPackets.Enqueue(new ReceivedClientPacket(client2, ""));
				return;
			}
			default:
				return;
			}
		}

		private void ParseClientPacket_offthread(ConnectedClient client, byte[] data, int length)
		{
			Packet_Client packet = new Packet_Client();
			try
			{
				Packet_ClientSerializer.DeserializeBuffer(data, length, packet);
			}
			catch
			{
				packet = null;
			}
			ReceivedClientPacket cpk;
			if (packet == null)
			{
				this.DisconnectedClientsThisTick.Add(client.Id);
				cpk = new ReceivedClientPacket(client, (client.Player == null) ? "" : "Network error: invalid client packet");
			}
			else
			{
				cpk = new ReceivedClientPacket(client, packet);
			}
			this.ClientPackets.Enqueue(cpk);
		}

		private void HandleClientPacket_mainthread(ReceivedClientPacket cpk)
		{
			ConnectedClient client = cpk.client;
			Packet_Client packet = cpk.packet;
			if (cpk.type == ReceivedClientPacketType.NewConnection)
			{
				if (this.DisconnectedClientsThisTick.Contains(client.Id))
				{
					return;
				}
				client.Initialise();
				this.Clients[client.Id] = client;
				string connection = (client.IsSinglePlayerClient ? "Dummy connection" : "TCP");
				LoggerBase logger = ServerMain.Logger;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 2);
				defaultInterpolatedStringHandler.AppendLiteral("A Client attempts connecting via ");
				defaultInterpolatedStringHandler.AppendFormatted(connection);
				defaultInterpolatedStringHandler.AppendLiteral(" on ");
				defaultInterpolatedStringHandler.AppendFormatted<IPEndPoint>(client.Socket.RemoteEndPoint());
				defaultInterpolatedStringHandler.AppendLiteral(", assigning client id ");
				logger.Notification(defaultInterpolatedStringHandler.ToStringAndClear() + client.Id.ToString());
				return;
			}
			else
			{
				if (cpk.type == ReceivedClientPacketType.Disconnect)
				{
					if (client.Player != null && cpk.disconnectReason.Length == 0)
					{
						ServerMain.Logger.Event("Client " + client.Id.ToString() + " disconnected.");
						this.EventManager.TriggerPlayerLeave(client.Player);
					}
					this.DisconnectPlayer(client, null, cpk.disconnectReason);
					return;
				}
				if (client.IsNewClient && packet.Id != 1 && packet.Id != 2 && packet.Id != 14 && packet.Id != 34)
				{
					this.HandleQueryClientPacket(client, packet);
					if (ServerMain.FrameProfiler.Enabled)
					{
						ServerMain.FrameProfiler.Mark("net-read-", packet.Id);
					}
					return;
				}
				ClientPacketHandler<Packet_Client, ConnectedClient> handler = this.PacketHandlers[packet.Id];
				if (handler == null || (client.Player == null && !this.PacketHandlingOnConnectingAllowed[packet.Id]))
				{
					ServerMain.Logger.Error("Unhandled player packet: {0}, clientid:{1}", new object[] { packet.Id, client.Id });
					if (ServerMain.FrameProfiler.Enabled)
					{
						ServerMain.FrameProfiler.Mark("net-readerror-", packet.Id);
					}
					return;
				}
				if (client.Player != null && client.Player.client != client)
				{
					return;
				}
				handler(packet, client);
				if (ServerMain.FrameProfiler.Enabled)
				{
					ServerMain.FrameProfiler.Mark("net-read-", packet.Id);
				}
				return;
			}
		}

		private void VerifyPlayerWithAuthServer(Packet_ClientIdentification packet, ConnectedClient client)
		{
			ServerMain.Logger.Debug("Client uid {0}, mp token {1}: Verifying with auth server", new object[] { packet.PlayerUID, packet.MpToken, packet.Playername });
			AuthServerComm.ValidatePlayerWithServer(packet.MpToken, packet.Playername, packet.PlayerUID, client.LoginToken, delegate(EnumServerResponse result, string entitlements, string errorReason)
			{
				this.EnqueueMainThreadTask(delegate
				{
					if (!this.Clients.ContainsKey(client.Id))
					{
						return;
					}
					if (result == EnumServerResponse.Good)
					{
						this.PreFinalizePlayerIdentification(packet, client, entitlements);
						ServerMain.FrameProfiler.Mark("finalizeplayeridentification");
						return;
					}
					if (result != EnumServerResponse.Bad)
					{
						this.DisconnectPlayer(client, null, Lang.Get("Unable to check wether your game session is ok, auth server probably offline. Please try again later. If you are the server owner, check server-main.log and server-debug.log for details", Array.Empty<object>()));
						return;
					}
					string errorReason = errorReason;
					if (errorReason == "missingmptoken" || errorReason == "missingmptokenv2" || errorReason == "missingaccount" || errorReason == "banned" || errorReason == "serverbanned" || errorReason == "badplayeruid")
					{
						this.DisconnectPlayer(client, null, Lang.Get("servervalidate-error-" + errorReason, Array.Empty<object>()));
						return;
					}
					this.DisconnectPlayer(client, null, Lang.Get("Auth server reports issue " + errorReason, Array.Empty<object>()));
				});
			});
		}

		private void PreFinalizePlayerIdentification(Packet_ClientIdentification packet, ConnectedClient client, string entitlements)
		{
			int maxClients = this.Config.MaxClients;
			if (this.Clients.Count - 1 >= maxClients)
			{
				ServerPlayerData data = this.PlayerDataManager.GetOrCreateServerPlayerData(packet.PlayerUID, null);
				if (!data.HasPrivilege(Privilege.controlserver, this.Config.RolesByCode) && !data.HasPrivilege("ignoremaxclients", this.Config.RolesByCode))
				{
					if (this.Config.MaxClientsInQueue > 0)
					{
						List<QueuedClient> list = this.ConnectionQueue;
						int connectionQueueCount;
						lock (list)
						{
							connectionQueueCount = this.ConnectionQueue.Count;
						}
						if (connectionQueueCount < this.Config.MaxClientsInQueue)
						{
							client.State = EnumClientState.Queued;
							list = this.ConnectionQueue;
							int pos;
							lock (list)
							{
								this.ConnectionQueue.Add(new QueuedClient(client, packet, entitlements));
								pos = this.ConnectionQueue.Count;
							}
							Packet_Server pq = new Packet_Server
							{
								Id = 82,
								QueuePacket = new Packet_QueuePacket
								{
									Position = pos
								}
							};
							LoggerBase logger = ServerMain.Logger;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 2);
							defaultInterpolatedStringHandler.AppendLiteral("Player ");
							defaultInterpolatedStringHandler.AppendFormatted(packet.Playername);
							defaultInterpolatedStringHandler.AppendLiteral(" was put into the connection queue at position ");
							defaultInterpolatedStringHandler.AppendFormatted<int>(pos);
							logger.Notification(defaultInterpolatedStringHandler.ToStringAndClear());
							this.SendPacket(client.Id, pq);
							return;
						}
					}
					this.DisconnectPlayer(client, null, Lang.Get("Server is full ({0} max clients)", new object[] { maxClients }));
					return;
				}
			}
			this.FinalizePlayerIdentification(packet, client, entitlements);
		}

		private void FinalizePlayerIdentification(Packet_ClientIdentification packet, ConnectedClient client, string entitlements)
		{
			ServerMain.<>c__DisplayClass321_0 CS$<>8__locals1 = new ServerMain.<>c__DisplayClass321_0();
			CS$<>8__locals1.client = client;
			CS$<>8__locals1.<>4__this = this;
			if (this.RunPhase == EnumServerRunPhase.Shutdown)
			{
				return;
			}
			CS$<>8__locals1.playername = packet.Playername;
			ServerMain.Logger.VerboseDebug("Received identification packet from " + CS$<>8__locals1.playername);
			bool found = false;
			foreach (ConnectedClient oldclient in this.Clients.Values)
			{
				bool equaluid = packet.PlayerUID.Equals(oldclient.SentPlayerUid, StringComparison.InvariantCultureIgnoreCase);
				if (equaluid && CS$<>8__locals1.client.Id != oldclient.Id)
				{
					ServerMain.Logger.Event(string.Format("{0} joined again, killing previous client.", packet.Playername));
					this.DisconnectPlayer(oldclient, null, null);
					break;
				}
				if (equaluid)
				{
					found = true;
				}
			}
			if (!found)
			{
				ServerMain.Logger.Notification("Was about to finalize player ident, but player {0} is no longer online. Ignoring.", new object[] { packet.Playername });
				return;
			}
			if (CS$<>8__locals1.client.ServerDidReceiveUdp)
			{
				LoggerBase logger = ServerMain.Logger;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 1);
				defaultInterpolatedStringHandler.AppendLiteral("UDP: Client ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(CS$<>8__locals1.client.Id);
				defaultInterpolatedStringHandler.AppendLiteral(" did send UDP");
				logger.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else if (!CS$<>8__locals1.client.FallBackToTcp)
			{
				Task.Run(delegate
				{
					ServerMain.<>c__DisplayClass321_0.<<FinalizePlayerIdentification>b__0>d <<FinalizePlayerIdentification>b__0>d;
					<<FinalizePlayerIdentification>b__0>d.<>t__builder = AsyncTaskMethodBuilder.Create();
					<<FinalizePlayerIdentification>b__0>d.<>4__this = CS$<>8__locals1;
					<<FinalizePlayerIdentification>b__0>d.<>1__state = -1;
					<<FinalizePlayerIdentification>b__0>d.<>t__builder.Start<ServerMain.<>c__DisplayClass321_0.<<FinalizePlayerIdentification>b__0>d>(ref <<FinalizePlayerIdentification>b__0>d);
					return <<FinalizePlayerIdentification>b__0>d.<>t__builder.Task;
				});
			}
			CS$<>8__locals1.playerUID = packet.PlayerUID;
			CS$<>8__locals1.client.LoadOrCreatePlayerData(this, CS$<>8__locals1.playername, CS$<>8__locals1.playerUID);
			CS$<>8__locals1.client.Player.client = CS$<>8__locals1.client;
			CS$<>8__locals1.client.WorldData.EntityPlayer.WatchedAttributes.SetString("playerUID", CS$<>8__locals1.playerUID);
			CS$<>8__locals1.client.WorldData.Viewdistance = packet.ViewDistance;
			CS$<>8__locals1.client.WorldData.RenderMetaBlocks = packet.RenderMetaBlocks > 0;
			TcpNetConnection tcpSocket = CS$<>8__locals1.client.Socket as TcpNetConnection;
			if (tcpSocket != null)
			{
				tcpSocket.SetLengthLimit(CS$<>8__locals1.client.WorldData.GameMode == EnumGameMode.Creative);
				tcpSocket.TcpSocket.ReceiveBufferSize = 65536;
				if (tcpSocket.TcpSocket.ReceiveBufferSize > 65536)
				{
					tcpSocket.TcpSocket.ReceiveBufferSize = 32768;
				}
			}
			if (entitlements != null)
			{
				foreach (string entitlement in entitlements.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					CS$<>8__locals1.client.Player.Entitlements.Add(new Entitlement
					{
						Code = entitlement,
						Name = Lang.Get("entitlement-" + entitlement, Array.Empty<object>())
					});
				}
			}
			this.PlayersByUid[CS$<>8__locals1.playerUID] = CS$<>8__locals1.client.Player;
			EntityPos targetPos = (CS$<>8__locals1.client.IsNewEntityPlayer ? this.GetSpawnPosition(CS$<>8__locals1.playerUID, false, false) : this.GetJoinPosition(CS$<>8__locals1.client));
			if (!CS$<>8__locals1.client.IsNewEntityPlayer && targetPos.X == 0.0 && targetPos.Y == 0.0 && targetPos.Z == 0.0 && targetPos.Pitch == 0f && targetPos.Roll == 0f)
			{
				ServerMain.Logger.Warning("Player {0} is at position 0/0/0? Did something get corrupted? Placing player to the global default spawn position...", new object[] { CS$<>8__locals1.client.PlayerName });
				targetPos = this.GetSpawnPosition(CS$<>8__locals1.playerUID, false, false);
			}
			if (CS$<>8__locals1.client.IsSinglePlayerClient && this.Config.MaxChunkRadius != packet.ViewDistance / 32)
			{
				this.Config.MaxChunkRadius = Math.Max(this.Config.MaxChunkRadius, packet.ViewDistance / 32);
				LoggerBase logger2 = ServerMain.Logger;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(66, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Upped server view distance to: ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(packet.ViewDistance);
				defaultInterpolatedStringHandler.AppendLiteral(", because player is in singleplayer");
				logger2.Notification(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			this.SendPacket(CS$<>8__locals1.client.Player, new Packet_Server
			{
				Id = 51,
				EntityPosition = ServerPackets.getEntityPositionPacket(this.GetSpawnPosition(CS$<>8__locals1.playerUID, true, false), CS$<>8__locals1.client.Entityplayer, 0)
			});
			if (this.World.Config.GetString("spawnRadius", null).ToInt(0) > 0 && CS$<>8__locals1.client.IsNewEntityPlayer)
			{
				ServerMain.Logger.Notification("Delayed join, attempt random spawn position.");
				this.SendLevelProgress(CS$<>8__locals1.client.Player, 99, Lang.Get("Loading spawn chunk...", Array.Empty<object>()));
				this.SendServerIdentification(CS$<>8__locals1.client.Player);
				this.SpawnPlayerRandomlyAround(CS$<>8__locals1.client, CS$<>8__locals1.playername, targetPos, 10);
				CS$<>8__locals1.client.IsNewClient = false;
				return;
			}
			if (this.WorldMap.IsPosLoaded(targetPos.AsBlockPos))
			{
				this.SendServerIdentification(CS$<>8__locals1.client.Player);
				CS$<>8__locals1.client.Entityplayer.ServerPos.SetFrom(targetPos);
				this.SpawnEntity(CS$<>8__locals1.client.Entityplayer);
				CS$<>8__locals1.client.Entityplayer.SetName(CS$<>8__locals1.playername);
				ServerMain.Logger.Notification("Placing {0} at {1} {2} {3}", new object[] { CS$<>8__locals1.playername, targetPos.X, targetPos.Y, targetPos.Z });
				this.SendServerReady(CS$<>8__locals1.client.Player);
			}
			else
			{
				ServerMain.Logger.Notification("Delayed join, need to load one spawn chunk first.");
				this.SendLevelProgress(CS$<>8__locals1.client.Player, 99, Lang.Get("Loading spawn chunk...", Array.Empty<object>()));
				this.SendServerIdentification(CS$<>8__locals1.client.Player);
				KeyValuePair<HorRectanglei, ChunkLoadOptions> data = new KeyValuePair<HorRectanglei, ChunkLoadOptions>(new HorRectanglei((int)targetPos.X / 32, (int)targetPos.Z / 32, (int)targetPos.X / 32, (int)targetPos.Z / 32), new ChunkLoadOptions
				{
					OnLoaded = delegate
					{
						ConnectedClient finalClient;
						CS$<>8__locals1.<>4__this.Clients.TryGetValue(CS$<>8__locals1.client.Id, out finalClient);
						if (finalClient != null)
						{
							CS$<>8__locals1.client.CurrentChunkSentRadius = 0;
							EntityPos finalPos = (CS$<>8__locals1.client.IsNewEntityPlayer ? CS$<>8__locals1.<>4__this.GetSpawnPosition(CS$<>8__locals1.playerUID, false, false) : CS$<>8__locals1.<>4__this.GetJoinPosition(finalClient));
							CS$<>8__locals1.client.Entityplayer.ServerPos.SetFrom(finalPos);
							CS$<>8__locals1.<>4__this.SpawnEntity(CS$<>8__locals1.client.Entityplayer);
							CS$<>8__locals1.client.WorldData.EntityPlayer.SetName(CS$<>8__locals1.playername);
							ServerMain.Logger.Notification("Placing {0} at {1} {2} {3}", new object[] { CS$<>8__locals1.playername, finalPos.X, finalPos.Y, finalPos.Z });
							CS$<>8__locals1.<>4__this.SendServerReady(CS$<>8__locals1.client.Player);
						}
					}
				});
				this.fastChunkQueue.Enqueue(data);
				ServerMain.Logger.VerboseDebug("Spawn chunk load request enqueued.");
			}
			CS$<>8__locals1.client.IsNewClient = false;
		}

		public void LocateRandomPosition(Vec3d centerPos, float radius, int tries, ActionConsumable<BlockPos> testThisPosition, Action<BlockPos> onSearchOver)
		{
			Vec3d targetPos = centerPos.Clone();
			targetPos.X += this.rand.Value.NextDouble() * 2.0 * (double)radius - (double)radius;
			targetPos.Z += this.rand.Value.NextDouble() * 2.0 * (double)radius - (double)radius;
			BlockPos pos = targetPos.AsBlockPos;
			if (tries <= 0)
			{
				onSearchOver(null);
				return;
			}
			if (this.WorldMap.IsPosLoaded(pos) && testThisPosition(pos))
			{
				onSearchOver(pos);
				return;
			}
			KeyValuePair<HorRectanglei, ChunkLoadOptions> data = new KeyValuePair<HorRectanglei, ChunkLoadOptions>(new HorRectanglei((int)targetPos.X / 32, (int)targetPos.Z / 32, (int)targetPos.X / 32, (int)targetPos.Z / 32), new ChunkLoadOptions
			{
				OnLoaded = delegate
				{
					BlockPos bpos = targetPos.AsBlockPos;
					if (this.WorldMap.IsPosLoaded(bpos) && testThisPosition(bpos))
					{
						onSearchOver(bpos);
						return;
					}
					this.LocateRandomPosition(targetPos, radius, tries - 1, testThisPosition, onSearchOver);
				}
			});
			ServerMain.Logger.Event("Searching for chunk column suitable for player spawn");
			ServerMain.Logger.StoryEvent("...");
			this.fastChunkQueue.Enqueue(data);
		}

		private void SpawnPlayerRandomlyAround(ConnectedClient client, string playername, EntityPos centerPos, int tries)
		{
			float radius = (float)this.World.Config.GetString("spawnRadius", null).ToInt(0);
			this.LocateRandomPosition(centerPos.XYZ, radius, tries, (BlockPos pos) => ServerSystemSupplyChunks.AdjustForSaveSpawnSpot(this, pos, client.Player, this.rand.Value), delegate(BlockPos pos)
			{
				EntityPos targetPos = centerPos.Copy();
				if (pos == null)
				{
					targetPos.X += this.rand.Value.NextDouble() * 2.0 * (double)radius - (double)radius;
					targetPos.Z += this.rand.Value.NextDouble() * 2.0 * (double)radius - (double)radius;
				}
				else
				{
					targetPos.X += (double)pos.X + 0.5 - targetPos.X;
					targetPos.Y += (double)pos.Y - targetPos.Y;
					targetPos.Z += (double)pos.Z + 0.5 - targetPos.Z;
				}
				this.SpawnPlayerHere(client, playername, targetPos);
			});
		}

		private void SpawnPlayerHere(ConnectedClient client, string playername, EntityPos targetPos)
		{
			ConnectedClient finalClient;
			this.Clients.TryGetValue(client.Id, out finalClient);
			if (finalClient != null)
			{
				client.CurrentChunkSentRadius = 0;
				client.Entityplayer.ServerPos.SetFrom(targetPos);
				this.SpawnEntity(client.Entityplayer);
				client.WorldData.EntityPlayer.SetName(playername);
				ServerMain.Logger.Notification("Placing {0} at {1} {2} {3}", new object[] { playername, targetPos.X, targetPos.Y, targetPos.Z });
				this.SendServerReady(client.Player);
			}
		}

		public void SendArbitraryUdpPacket(Packet_UdpPacket packet, params IServerPlayer[] players)
		{
			for (int i = 0; i < players.Length; i++)
			{
				this.SendPacket(((ServerPlayer)players[i]).client, packet);
			}
		}

		public void SendArbitraryPacket(byte[] data, params IServerPlayer[] players)
		{
			for (int i = 0; i < players.Length; i++)
			{
				this.SendPacket(players[i], data);
			}
		}

		public void SendArbitraryPacket(Packet_Server packet, params IServerPlayer[] players)
		{
			this.Serialize_(packet);
			foreach (IServerPlayer player in players)
			{
				if (player == null || player.ConnectionState == EnumClientState.Offline)
				{
					return;
				}
				this.SendPacket(player.ClientId, ServerMain.reusableBuffer);
			}
		}

		internal void SendBlockEntity(IServerPlayer targetPlayer, BlockEntity blockentity)
		{
			Packet_BlockEntity[] blockentitiespackets = new Packet_BlockEntity[1];
			int i = 0;
			MemoryStream ms = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(ms);
			TreeAttribute tree = new TreeAttribute();
			blockentity.ToTreeAttributes(tree);
			tree.ToBytes(writer);
			blockentitiespackets[i] = new Packet_BlockEntity
			{
				Classname = ServerMain.ClassRegistry.blockEntityTypeToClassnameMapping[blockentity.GetType()],
				Data = ms.ToArray(),
				PosX = blockentity.Pos.X,
				PosY = blockentity.Pos.InternalY,
				PosZ = blockentity.Pos.Z
			};
			Packet_BlockEntities packet = new Packet_BlockEntities();
			packet.SetBlockEntitites(blockentitiespackets);
			this.SendPacket(targetPlayer, new Packet_Server
			{
				Id = 48,
				BlockEntities = packet
			});
		}

		public void SendBlockEntityMessagePacket(IServerPlayer player, int x, int y, int z, int packetId, byte[] data = null)
		{
			Packet_BlockEntityMessage packet = new Packet_BlockEntityMessage
			{
				PacketId = packetId,
				X = x,
				Y = y,
				Z = z
			};
			packet.SetData(data);
			this.SendPacket(player, new Packet_Server
			{
				Id = 44,
				BlockEntityMessage = packet
			});
		}

		public void SendEntityPacket(IServerPlayer player, long entityId, int packetId, byte[] data = null)
		{
			Packet_EntityPacket packet = new Packet_EntityPacket
			{
				Packetid = packetId,
				EntityId = entityId
			};
			packet.SetData(data);
			this.SendPacket(player, new Packet_Server
			{
				Id = 67,
				EntityPacket = packet
			});
		}

		public void BroadcastEntityPacket(long entityId, int packetId, byte[] data = null)
		{
			Packet_EntityPacket packet = new Packet_EntityPacket
			{
				Packetid = packetId,
				EntityId = entityId
			};
			packet.SetData(data);
			this.BroadcastPacket(new Packet_Server
			{
				Id = 67,
				EntityPacket = packet
			}, Array.Empty<IServerPlayer>());
		}

		public void BroadcastBlockEntityPacket(int x, int y, int z, int packetId, byte[] data = null, params IServerPlayer[] skipPlayers)
		{
			Packet_BlockEntityMessage packet = new Packet_BlockEntityMessage
			{
				PacketId = packetId,
				X = x,
				Y = y,
				Z = z
			};
			packet.SetData(data);
			this.BroadcastPacket(new Packet_Server
			{
				Id = 44,
				BlockEntityMessage = packet
			}, skipPlayers);
		}

		public void SendMessageToGeneral(string message, EnumChatType chatType, IServerPlayer exceptPlayer = null, string data = null)
		{
			this.SendMessageToGroup(GlobalConstants.GeneralChatGroup, message, chatType, exceptPlayer, data);
		}

		public void SendMessageToGroup(int groupid, string message, EnumChatType chatType, IServerPlayer exceptPlayer = null, string data = null)
		{
			bool isCommonGroup = groupid == GlobalConstants.AllChatGroups || groupid == GlobalConstants.GeneralChatGroup || groupid == GlobalConstants.CurrentChatGroup || groupid == GlobalConstants.ServerInfoChatGroup || groupid == GlobalConstants.InfoLogChatGroup;
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if ((exceptPlayer == null || client.Id != exceptPlayer.ClientId) && client.State != EnumClientState.Offline && client.State != EnumClientState.Connecting && client.State != EnumClientState.Queued && (isCommonGroup || client.ServerData.PlayerGroupMemberShips.ContainsKey(groupid)))
				{
					this.SendMessage(client.Player, groupid, message, chatType, data);
				}
			}
		}

		public void BroadcastMessageToAllGroups(string message, EnumChatType chatType, string data = null)
		{
			ServerMain.Logger.Notification("Message to all in group " + GlobalConstants.GeneralChatGroup.ToString() + ": {0}", new object[] { message });
			foreach (ConnectedClient client in this.Clients.Values)
			{
				this.SendMessage(client.Player, GlobalConstants.AllChatGroups, message, chatType, data);
			}
		}

		public void SendMessageToCurrentCh(IServerPlayer player, string message, EnumChatType chatType, string data = null)
		{
			this.SendMessage(player, GlobalConstants.CurrentChatGroup, message, chatType, null);
		}

		public void ReplyMessage(IServerPlayer player, string message, EnumChatType chatType, string data = null)
		{
			this.SendMessage(player, GlobalConstants.CurrentChatGroup, message, chatType, data);
		}

		public void SendMessage(Caller caller, string message, EnumChatType chatType, string data = null)
		{
			this.SendMessage(caller.Player as IServerPlayer, caller.FromChatGroupId, message, chatType, data);
		}

		public void SendMessage(IServerPlayer player, int groupid, string message, EnumChatType chatType, string data = null)
		{
			if (groupid == GlobalConstants.ConsoleGroup)
			{
				ServerMain.Logger.Notification(message);
				return;
			}
			this.SendPacket(player, ServerPackets.ChatLine(groupid, message, chatType, data));
		}

		public void SendIngameError(IServerPlayer player, string errorCode, string text = null, params object[] langparams)
		{
			this.SendPacket(player, ServerPackets.IngameError(errorCode, text, langparams));
		}

		public void SendIngameDiscovery(IServerPlayer player, string discoveryCode, string text = null, params object[] langparams)
		{
			this.SendPacket(player, ServerPackets.IngameDiscovery(discoveryCode, text, langparams));
		}

		[Obsolete("Use Serialize_ and reusableBuffer where possible, for better performance")]
		public byte[] Serialize(Packet_Server p)
		{
			return Packet_ServerSerializer.SerializeToBytes(p);
		}

		internal int Serialize_(Packet_Server p)
		{
			if (ServerMain.reusableBuffer == null)
			{
				ServerMain.reusableBuffer = new BoxedPacket();
				List<BoxedArray> list = this.reusableBuffersDisposalList;
				lock (list)
				{
					this.reusableBuffersDisposalList.Add(ServerMain.reusableBuffer);
				}
			}
			return ServerMain.reusableBuffer.Serialize(p);
		}

		internal void SendSetBlock(int blockId, int posX, int posY, int posZ, int exceptClientid = -1, bool exchangeOnly = false)
		{
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if (exceptClientid != client.Id && client.State != EnumClientState.Connecting && client.State != EnumClientState.Queued && client.Player != null)
				{
					this.SendSetBlock(client.Player, blockId, posX, posY, posZ, exchangeOnly);
				}
			}
		}

		internal void BroadcastUnloadMapRegion(long index)
		{
			int rx;
			int rz;
			this.WorldMap.MapRegionPosFromIndex2D(index, out rx, out rz);
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if (client.State != EnumClientState.Connecting && client.State != EnumClientState.Queued && client.Player != null)
				{
					Packet_UnloadMapRegion p = new Packet_UnloadMapRegion
					{
						RegionX = rx,
						RegionZ = rz
					};
					this.SendPacket(client.Player, new Packet_Server
					{
						Id = 74,
						UnloadMapRegion = p
					});
					client.RemoveMapRegionSent(index);
				}
			}
		}

		internal void SendSetBlock(IServerPlayer player, int blockId, int posX, int posY, int posZ, bool exchangeOnly = false)
		{
			if (!this.Clients[player.ClientId].DidSendChunk(this.WorldMap.ChunkIndex3D(posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize)))
			{
				return;
			}
			if (exchangeOnly)
			{
				Packet_ServerExchangeBlock p = new Packet_ServerExchangeBlock
				{
					X = posX,
					Y = posY,
					Z = posZ,
					BlockType = blockId
				};
				this.SendPacket(player, new Packet_Server
				{
					Id = 58,
					ExchangeBlock = p
				});
				return;
			}
			Packet_ServerSetBlock p2 = new Packet_ServerSetBlock
			{
				X = posX,
				Y = posY,
				Z = posZ,
				BlockType = blockId
			};
			this.SendPacket(player, new Packet_Server
			{
				Id = 7,
				SetBlock = p2
			});
		}

		public void SendSetBlocksPacket(List<BlockPos> positions, int packetId)
		{
			if (positions.Count == 0)
			{
				return;
			}
			byte[] compressedBlocks = BlockTypeNet.PackSetBlocksList(positions, this.WorldMap.RelaxedBlockAccess);
			Packet_ServerSetBlocks setblocks = new Packet_ServerSetBlocks();
			setblocks.SetSetBlocks(compressedBlocks);
			this.BroadcastPacket(new Packet_Server
			{
				Id = packetId,
				SetBlocks = setblocks
			}, Array.Empty<IServerPlayer>());
		}

		public void SendSetDecorsPackets(List<BlockPos> positions)
		{
			if (positions.Count == 0)
			{
				return;
			}
			foreach (KeyValuePair<long, WorldChunk> val in this.WorldMap.PositionsToUniqueChunks(positions))
			{
				if (val.Value != null)
				{
					byte[] compressedDecors = BlockTypeNet.PackSetDecorsList(val.Value, val.Key, this.WorldMap.RelaxedBlockAccess);
					Packet_ServerSetDecors setdecors = new Packet_ServerSetDecors();
					setdecors.SetSetDecors(compressedDecors);
					this.BroadcastPacket(new Packet_Server
					{
						Id = 71,
						SetDecors = setdecors
					}, Array.Empty<IServerPlayer>());
				}
			}
		}

		public void SendHighlightBlocksPacket(IServerPlayer player, int slotId, List<BlockPos> justBlocks, List<int> colors, EnumHighlightBlocksMode mode, EnumHighlightShape shape, float scale = 1f)
		{
			byte[] compressedBlocks = BlockTypeNet.PackBlocksPositions(justBlocks);
			Packet_HighlightBlocks setblocks = new Packet_HighlightBlocks();
			setblocks.SetBlocks(compressedBlocks);
			setblocks.Mode = (int)mode;
			setblocks.Shape = (int)shape;
			setblocks.Slotid = slotId;
			setblocks.Scale = CollectibleNet.SerializeFloatVeryPrecise(scale);
			if (colors != null)
			{
				setblocks.SetColors(colors.ToArray());
			}
			this.SendPacket(player, new Packet_Server
			{
				Id = 52,
				HighlightBlocks = setblocks
			});
		}

		public void SendSound(IServerPlayer player, AssetLocation location, double x, double y, double z, float pitch, float range, float volume, EnumSoundType soundType = EnumSoundType.Sound)
		{
			Packet_ServerSound p = new Packet_ServerSound
			{
				Name = location.ToString(),
				X = CollectibleNet.SerializeFloat((float)x),
				Y = CollectibleNet.SerializeFloat((float)y),
				Z = CollectibleNet.SerializeFloat((float)z),
				Range = CollectibleNet.SerializeFloat(range),
				Pitch = CollectibleNet.SerializeFloatPrecise(pitch),
				Volume = CollectibleNet.SerializeFloatPrecise(volume),
				SoundType = (int)soundType
			};
			this.SendPacket(player, new Packet_Server
			{
				Id = 18,
				Sound = p
			});
		}

		public void BroadcastPacket(Packet_Server packet, params IServerPlayer[] skipPlayers)
		{
			this.BroadcastArbitraryPacket(packet, skipPlayers);
			if (this.doNetBenchmark)
			{
				this.recordInBenchmark(packet.Id, ServerMain.reusableBuffer.Length);
			}
		}

		internal void BroadcastArbitraryPacket(byte[] data, params IServerPlayer[] skipPlayers)
		{
			using (IEnumerator<ConnectedClient> enumerator = this.Clients.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ConnectedClient client = enumerator.Current;
					if (client.State != EnumClientState.Offline && client.State != EnumClientState.Queued && (skipPlayers == null || !skipPlayers.Any(delegate(IServerPlayer plr)
					{
						int? num = ((plr != null) ? new int?(plr.ClientId) : null);
						int id = client.Id;
						return (num.GetValueOrDefault() == id) & (num != null);
					})))
					{
						this.SendPacket(client.Player, data);
					}
				}
			}
		}

		internal void BroadcastArbitraryPacket(Packet_Server packet, params IServerPlayer[] skipPlayers)
		{
			this.Serialize_(packet);
			byte[] packetBytes = null;
			bool compressed = false;
			using (IEnumerator<ConnectedClient> enumerator = this.Clients.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ConnectedClient client = enumerator.Current;
					if (client.State != EnumClientState.Offline && client.State != EnumClientState.Queued && (skipPlayers == null || !skipPlayers.Any(delegate(IServerPlayer plr)
					{
						int? num = ((plr != null) ? new int?(plr.ClientId) : null);
						int id = client.Id;
						return (num.GetValueOrDefault() == id) & (num != null);
					})))
					{
						if (packetBytes == null)
						{
							packetBytes = client.Socket.PreparePacketForSending(ServerMain.reusableBuffer, this.Config.CompressPackets, out compressed);
						}
						this.SendPreparedPacket(client, packetBytes, compressed);
						if (client.Socket is DummyNetConnection)
						{
							packetBytes = null;
						}
					}
				}
			}
		}

		internal void BroadcastArbitraryUdpPacket(Packet_UdpPacket data, params IServerPlayer[] skipPlayers)
		{
			using (IEnumerator<ConnectedClient> enumerator = this.Clients.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ConnectedClient client = enumerator.Current;
					if (client.State != EnumClientState.Offline && client.State != EnumClientState.Queued && (skipPlayers == null || skipPlayers.All(delegate(IServerPlayer plr)
					{
						int? num = ((plr != null) ? new int?(plr.ClientId) : null);
						int id = client.Id;
						return !((num.GetValueOrDefault() == id) & (num != null));
					})))
					{
						this.SendPacket(client, data);
					}
				}
			}
		}

		private void recordInBenchmark(int packetId, int dataLength)
		{
			if (this.packetBenchmark.ContainsKey(packetId))
			{
				SortedDictionary<int, int> sortedDictionary = this.packetBenchmark;
				int num = sortedDictionary[packetId];
				sortedDictionary[packetId] = num + 1;
				SortedDictionary<int, int> sortedDictionary2 = this.packetBenchmarkBytes;
				sortedDictionary2[packetId] += dataLength;
				return;
			}
			this.packetBenchmark[packetId] = 1;
			this.packetBenchmarkBytes[packetId] = dataLength;
		}

		public void SendPacket(int clientId, Packet_Server packet)
		{
			int len = this.Serialize_(packet);
			if (this.doNetBenchmark)
			{
				this.recordInBenchmark(packet.Id, len);
			}
			this.SendPacket(clientId, ServerMain.reusableBuffer);
		}

		public void SendPacketFast(int clientId, Packet_Server packet)
		{
			ConnectedClient client;
			if (this.Clients.TryGetValue(clientId, out client) && client.IsSinglePlayerClient && DummyNetConnection.SendServerPacketDirectly(packet))
			{
				return;
			}
			this.SendPacket(clientId, packet);
		}

		public void SendPacket(ConnectedClient client, Packet_UdpPacket packet)
		{
			this.ServerUdpNetwork.SendPacket_Threadsafe(client, packet);
		}

		internal void SendPacketBlocking(ConnectedClient client, Packet_UdpPacket packet)
		{
			if (client.FallBackToTcp)
			{
				Packet_Server packetServer = new Packet_Server
				{
					Id = 79,
					UdpPacket = packet
				};
				this.SendPacket(client.Id, packetServer);
				return;
			}
			if (client.IsSinglePlayerClient)
			{
				this.UdpSockets[0].SendToClient(client.Id, packet);
				return;
			}
			int bytesSend = this.UdpSockets[1].SendToClient(client.Id, packet);
			this.UpdateUdpStatsAndBenchmark(packet, bytesSend);
		}

		internal void UpdateUdpStatsAndBenchmark(Packet_UdpPacket packet, int byteCount)
		{
			this.StatsCollector[this.StatsCollectorIndex].statTotalUdpPackets++;
			this.StatsCollector[this.StatsCollectorIndex].statTotalUdpPacketsLength += byteCount;
			this.TotalSentBytesUdp += (long)byteCount;
			if (this.doNetBenchmark)
			{
				if (!this.udpPacketBenchmark.TryAdd(packet.Id, 1))
				{
					SortedDictionary<int, int> sortedDictionary = this.udpPacketBenchmark;
					int id = packet.Id;
					int num = sortedDictionary[id];
					sortedDictionary[id] = num + 1;
					SortedDictionary<int, int> sortedDictionary2 = this.udpPacketBenchmarkBytes;
					num = packet.Id;
					sortedDictionary2[num] += byteCount;
					return;
				}
				this.udpPacketBenchmarkBytes[packet.Id] = byteCount;
			}
		}

		public void SendPacket(IServerPlayer player, Packet_Server packet)
		{
			if (player == null || player.ConnectionState == EnumClientState.Offline)
			{
				return;
			}
			this.SendPacket(player.ClientId, packet);
		}

		private void SendPacket(IServerPlayer player, byte[] packetBytes)
		{
			if (player == null || player.ConnectionState == EnumClientState.Offline)
			{
				return;
			}
			this.SendPacket(player.ClientId, packetBytes);
		}

		private void SendPacket(int clientId, byte[] packetBytes)
		{
			bool compressed = false;
			if (packetBytes.Length > 5120 && this.Config.CompressPackets && !this.Clients[clientId].IsSinglePlayerClient)
			{
				packetBytes = Compression.Compress(packetBytes);
				compressed = true;
			}
			StatsCollection statsCollection = this.StatsCollector[this.StatsCollectorIndex];
			statsCollection.statTotalPackets++;
			statsCollection.statTotalPacketsLength += packetBytes.Length;
			this.TotalSentBytes += (long)packetBytes.Length;
			EnumSendResult result = EnumSendResult.Ok;
			ConnectedClient client;
			if (!this.Clients.TryGetValue(clientId, out client))
			{
				return;
			}
			try
			{
				result = client.Socket.Send(packetBytes, compressed);
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Network exception:.");
				ServerMain.Logger.Error(e);
				this.DisconnectPlayer(client, "Lost connection", null);
				return;
			}
			if (result == EnumSendResult.Disconnected)
			{
				this.EnqueueMainThreadTask(delegate
				{
					this.DisconnectPlayer(client, "Lost connection/disconnected", null);
					ServerMain.FrameProfiler.Mark("disconnectplayer");
				});
			}
		}

		private void SendPacket(int clientId, BoxedPacket box)
		{
			ConnectedClient client;
			if (!this.Clients.TryGetValue(clientId, out client))
			{
				return;
			}
			EnumSendResult result = client.Socket.HiPerformanceSend(box, ServerMain.Logger, this.Config.CompressPackets);
			this.HandleSendingResult(result, box.LengthSent, client);
		}

		private void SendPreparedPacket(ConnectedClient client, byte[] packetBytes, bool compressed)
		{
			EnumSendResult result = client.Socket.SendPreparedPacket(packetBytes, compressed, ServerMain.Logger);
			this.HandleSendingResult(result, packetBytes.Length, client);
		}

		private void HandleSendingResult(EnumSendResult result, int lengthSent, ConnectedClient client)
		{
			if (result == EnumSendResult.Ok)
			{
				StatsCollection statsCollection = this.StatsCollector[this.StatsCollectorIndex];
				statsCollection.statTotalPackets++;
				statsCollection.statTotalPacketsLength += lengthSent;
				this.TotalSentBytes += (long)lengthSent;
				return;
			}
			if (result == EnumSendResult.Error)
			{
				this.DisconnectPlayer(client, "Lost connection", null);
				return;
			}
			this.EnqueueMainThreadTask(delegate
			{
				this.DisconnectPlayer(client, "Lost connection/disconnected", null);
				ServerMain.FrameProfiler.Mark("disconnectplayer");
			});
		}

		private void SendPlayerEntities(IServerPlayer player, FastMemoryStream ms)
		{
			ICollection<ConnectedClient> values = this.Clients.Values;
			int clientsCount = values.Count;
			Packet_Entities entitiesPacket = new Packet_Entities
			{
				Entities = new Packet_Entity[clientsCount],
				EntitiesCount = clientsCount,
				EntitiesLength = clientsCount
			};
			BinaryWriter writer = new BinaryWriter(ms);
			int i = 0;
			foreach (ConnectedClient client in values)
			{
				if (client.Entityplayer != null)
				{
					entitiesPacket.Entities[i] = ServerPackets.GetEntityPacket(client.Entityplayer, ms, writer);
					i++;
				}
			}
			entitiesPacket.EntitiesCount = i;
			this.SendPacket(player, new Packet_Server
			{
				Id = 40,
				Entities = entitiesPacket
			});
		}

		public void SendServerAssets(IServerPlayer player)
		{
			if (player == null || player.ConnectionState == EnumClientState.Offline)
			{
				return;
			}
			if (this.serverAssetsPacket.Length == 0)
			{
				if (this.serverAssetsPacket.packet == null)
				{
					this.WaitOnBuildServerAssetsPacket();
				}
				if (this.serverAssetsPacket.Length == 0)
				{
					ConnectedClient client;
					if (this.Clients.TryGetValue(player.ClientId, out client) && client.IsSinglePlayerClient)
					{
						if (this.serverAssetsSentLocally)
						{
							return;
						}
						if (DummyNetConnection.SendServerAssetsPacketDirectly(this.serverAssetsPacket.packet))
						{
							return;
						}
					}
					else
					{
						this.serverAssetsPacket.Serialize(this.serverAssetsPacket.packet);
					}
				}
			}
			this.SendPacket(player.ClientId, this.serverAssetsPacket);
		}

		private void StartBuildServerAssetsPacket()
		{
			TyronThreadPool.QueueLongDurationTask(new Action(this.BuildServerAssetsPacket), "serverassetspacket");
			ServerMain.Logger.VerboseDebug("Starting to build server assets packet");
		}

		private void WaitOnBuildServerAssetsPacket()
		{
			int countDown = 500;
			while (this.serverAssetsPacket.Length == 0 && this.serverAssetsPacket.packet == null && countDown-- > 0)
			{
				Thread.Sleep(20);
			}
			if (this.serverAssetsPacket.Length == 0 && this.serverAssetsPacket.packet == null)
			{
				ServerMain.Logger.Error("Waiting on buildServerAssetsPacket thread for longer than 10 seconds timeout, trying again ... this may take a while!");
				this.BuildServerAssetsPacket();
			}
		}

		private void BuildServerAssetsPacket()
		{
			try
			{
				using (FastMemoryStream reusableMemoryStream = new FastMemoryStream())
				{
					Packet_ServerAssets packet = new Packet_ServerAssets();
					List<Packet_BlockType> blockPackets = new List<Packet_BlockType>();
					int i = 0;
					foreach (Block block in this.Blocks)
					{
						try
						{
							blockPackets.Add(BlockTypeNet.GetBlockTypePacket(block, this.api.ClassRegistry, reusableMemoryStream));
							block.FreeRAMServer();
						}
						catch (Exception e)
						{
							ServerMain.Logger.Fatal("Failed networking encoding block {0}:", new object[] { block.Code });
							ServerMain.Logger.Fatal(e);
							throw new Exception("SendServerAssets failed. See log files.");
						}
						if (i++ % 1000 == 999)
						{
							Thread.Sleep(5);
						}
					}
					packet.SetBlocks(blockPackets.ToArray());
					Thread.Sleep(5);
					List<Packet_ItemType> itemPackets = new List<Packet_ItemType>();
					for (int j = 0; j < this.Items.Count; j++)
					{
						Item item = this.Items[j];
						if (item != null && !(item.Code == null))
						{
							try
							{
								itemPackets.Add(ItemTypeNet.GetItemTypePacket(item, this.api.ClassRegistry, reusableMemoryStream));
								item.FreeRAMServer();
							}
							catch (Exception e2)
							{
								ServerMain.Logger.Fatal("Failed network encoding block {0}:", new object[] { item.Code });
								ServerMain.Logger.Fatal(e2);
								throw new Exception("SendServerAssets failed. See log files.");
							}
							if (j % 1000 == 999)
							{
								Thread.Sleep(5);
							}
						}
					}
					packet.SetItems(itemPackets.ToArray());
					Thread.Sleep(5);
					Packet_EntityType[] entityPackets = new Packet_EntityType[this.EntityTypesByCode.Count];
					i = 0;
					foreach (EntityProperties entityType in this.EntityTypes)
					{
						try
						{
							entityPackets[i++] = EntityTypeNet.EntityPropertiesToPacket(entityType, reusableMemoryStream);
							EntityClientProperties client = entityType.Client;
							if (client != null)
							{
								client.FreeRAMServer();
							}
						}
						catch (Exception e3)
						{
							ServerMain.Logger.Fatal("Failed network encoding entity type {0}:", new object[] { (entityType != null) ? entityType.Code : null });
							ServerMain.Logger.Fatal(e3);
							throw new Exception("SendServerAssets failed. See log files.");
						}
						if (i % 100 == 99)
						{
							Thread.Sleep(5);
						}
					}
					packet.SetEntities(entityPackets);
					Thread.Sleep(5);
					Packet_Recipes[] recipeRecipes = new Packet_Recipes[this.recipeRegistries.Count];
					i = 0;
					foreach (KeyValuePair<string, RecipeRegistryBase> val in this.recipeRegistries)
					{
						recipeRecipes[i++] = ServerMain.RecipesToPacket(val.Value, val.Key, this, reusableMemoryStream);
					}
					packet.SetRecipes(recipeRecipes);
					Thread.Sleep(5);
					this.TagRegistry.restrictNewTags = true;
					Packet_Tags tagsPacket = new Packet_Tags();
					tagsPacket.SetEntityTags(this.TagRegistry.entityTags.ToArray());
					tagsPacket.SetBlockTags(this.TagRegistry.blockTags.ToArray());
					tagsPacket.SetItemTags(this.TagRegistry.itemTags.ToArray());
					packet.SetTags(tagsPacket);
					Thread.Sleep(5);
					Packet_Server packetToSend = new Packet_Server
					{
						Id = 19,
						Assets = packet
					};
					ServerMain.Logger.VerboseDebug("Finished building server assets packet");
					if (this.IsDedicatedServer)
					{
						this.serverAssetsPacket.Serialize(packetToSend);
					}
					else
					{
						this.serverAssetsPacket.packet = packetToSend;
						if (DummyNetConnection.SendServerPacketDirectly(this.CreatePacketIdentification(true)))
						{
							if (DummyNetConnection.SendServerAssetsPacketDirectly(packetToSend))
							{
								this.serverAssetsSentLocally = true;
								this.worldMetaDataPacketAlreadySentToSinglePlayer = DummyNetConnection.SendServerPacketDirectly(this.WorldMetaDataPacket());
							}
							ServerMain.Logger.VerboseDebug("Single player: sent server assets packet to client");
						}
					}
				}
			}
			catch (ThreadAbortException)
			{
			}
		}

		private static Packet_Recipes RecipesToPacket(RecipeRegistryBase reg, string code, ServerMain world, FastMemoryStream ms)
		{
			RecipeRegistryGeneric<GridRecipe> greg = reg as RecipeRegistryGeneric<GridRecipe>;
			if (greg != null)
			{
				byte[] recdata;
				int quantity;
				greg.ToBytes(world, out recdata, out quantity, ms);
				greg.FreeRAMServer();
				return new Packet_Recipes
				{
					Code = code,
					Data = recdata,
					Quantity = quantity
				};
			}
			byte[] recdata2;
			int quantity2;
			reg.ToBytes(world, out recdata2, out quantity2);
			return new Packet_Recipes
			{
				Code = code,
				Data = recdata2,
				Quantity = quantity2
			};
		}

		private void SendWorldMetaData(IServerPlayer player)
		{
			ConnectedClient client;
			if (this.worldMetaDataPacketAlreadySentToSinglePlayer && this.Clients.TryGetValue(player.ClientId, out client) && client != null && client.IsSinglePlayerClient)
			{
				return;
			}
			this.SendPacket(player, this.WorldMetaDataPacket());
		}

		internal Packet_Server WorldMetaDataPacket()
		{
			float[] blockLightLevels = this.blockLightLevels;
			Packet_WorldMetaData p = new Packet_WorldMetaData();
			int[] serializedBlockLightLevels = new int[blockLightLevels.Length];
			int[] serializedSunLightLevels = new int[this.sunLightLevels.Length];
			for (int i = 0; i < blockLightLevels.Length; i++)
			{
				serializedBlockLightLevels[i] = CollectibleNet.SerializeFloat(blockLightLevels[i]);
				serializedSunLightLevels[i] = CollectibleNet.SerializeFloat(this.sunLightLevels[i]);
			}
			p.SetBlockLightlevels(serializedBlockLightLevels);
			p.SetSunLightlevels(serializedSunLightLevels);
			p.SunBrightness = this.sunBrightness;
			p.SetWorldConfiguration((this.SaveGameData.WorldConfiguration as TreeAttribute).ToBytes());
			p.SeaLevel = this.seaLevel;
			return new Packet_Server
			{
				Id = 21,
				WorldMetaData = p
			};
		}

		private void SendLevelProgress(IServerPlayer player, int percentcomplete, string status)
		{
			Packet_ServerLevelProgress pprogress = new Packet_ServerLevelProgress
			{
				PercentComplete = percentcomplete,
				Status = status
			};
			Packet_Server p = new Packet_Server
			{
				Id = 5,
				LevelDataChunk = pprogress
			};
			this.SendPacket(player, p);
		}

		private void SendServerReady(IServerPlayer player)
		{
			ServerMain.Logger.Audit("{0} joined.", new object[] { player.PlayerName });
			this.SendPacket(player, new Packet_Server
			{
				Id = 73,
				ServerReady = new Packet_ServerReady()
			});
		}

		private void SendServerIdentification(ServerPlayer player)
		{
			if (this.serverAssetsSentLocally && player.client.IsSinglePlayerClient)
			{
				((DummyUdpNetServer)this.UdpSockets[0]).Client.Player = player;
				return;
			}
			this.SendPacket(player, this.CreatePacketIdentification(player.HasPrivilege("controlserver")));
		}

		private Packet_Server CreatePacketIdentification(bool controlServerPrivilege)
		{
			List<Packet_ModId> mods = (from mod in this.api.ModLoader.Mods
				where mod.Info.Side.IsUniversal()
				select new Packet_ModId
				{
					Modid = mod.Info.ModID,
					Name = mod.Info.Name,
					Networkversion = mod.Info.NetworkVersion,
					Version = mod.Info.Version,
					RequiredOnClient = mod.Info.RequiredOnClient
				}).ToList<Packet_ModId>();
			Packet_ServerIdentification packet_ServerIdentification = new Packet_ServerIdentification();
			packet_ServerIdentification.GameVersion = "1.21.5";
			packet_ServerIdentification.NetworkVersion = "1.21.9";
			packet_ServerIdentification.ServerName = this.Config.ServerName;
			packet_ServerIdentification.Seed = this.SaveGameData.Seed;
			packet_ServerIdentification.SavegameIdentifier = this.SaveGameData.SavegameIdentifier;
			packet_ServerIdentification.MapSizeX = this.WorldMap.MapSizeX;
			packet_ServerIdentification.MapSizeY = this.WorldMap.MapSizeY;
			packet_ServerIdentification.MapSizeZ = this.WorldMap.MapSizeZ;
			packet_ServerIdentification.RegionMapSizeX = this.WorldMap.RegionMapSizeX;
			packet_ServerIdentification.RegionMapSizeY = this.WorldMap.RegionMapSizeY;
			packet_ServerIdentification.RegionMapSizeZ = this.WorldMap.RegionMapSizeZ;
			packet_ServerIdentification.PlayStyle = this.SaveGameData.PlayStyle;
			PlayStyle currentPlayStyle = this.api.WorldManager.CurrentPlayStyle;
			packet_ServerIdentification.PlayListCode = ((currentPlayStyle != null) ? currentPlayStyle.PlayListCode : null);
			packet_ServerIdentification.RequireRemapping = ((controlServerPrivilege && this.requiresRemaps) ? 1 : 0);
			Packet_ServerIdentification p = packet_ServerIdentification;
			ServerMain.Logger.Notification("Sending server identification with remap " + this.requiresRemaps.ToString() + ".  Server control privilege is " + controlServerPrivilege.ToString());
			p.SetMods(mods.ToArray());
			p.SetWorldConfiguration((this.SaveGameData.WorldConfiguration as TreeAttribute).ToBytes());
			if (this.Config.ModIdBlackList != null && this.Config.ModIdWhiteList == null)
			{
				p.SetServerModIdBlackList(this.Config.ModIdBlackList);
			}
			if (this.Config.ModIdWhiteList != null)
			{
				p.SetServerModIdWhiteList(this.Config.ModIdWhiteList);
			}
			return new Packet_Server
			{
				Id = 1,
				Identification = p
			};
		}

		public void BroadcastPlayerData(IServerPlayer owningPlayer, bool sendInventory = true, bool sendPrivileges = false)
		{
			Packet_Server ownplayerpacket = ((ServerWorldPlayerData)owningPlayer.WorldData).ToPacket(owningPlayer, sendInventory, sendPrivileges);
			Packet_Server otherplayerspacket = ((ServerWorldPlayerData)owningPlayer.WorldData).ToPacketForOtherPlayers(owningPlayer);
			this.SendPacket(owningPlayer, ownplayerpacket);
			this.BroadcastPacket(otherplayerspacket, new IServerPlayer[] { owningPlayer });
		}

		public void SendOwnPlayerData(IServerPlayer owningPlayer, bool sendInventory = true, bool sendPrivileges = false)
		{
			Packet_Server ownplayerpacket = ((ServerWorldPlayerData)owningPlayer.WorldData).ToPacket(owningPlayer, sendInventory, sendPrivileges);
			this.SendPacket(owningPlayer, ownplayerpacket);
		}

		public void SendInitialPlayerDataForOthers(IServerPlayer owningPlayer, IServerPlayer toPlayer, FastMemoryStream ms)
		{
			Packet_Entities entitiesPacket = new Packet_Entities();
			using (BinaryWriter writer = new BinaryWriter(ms))
			{
				entitiesPacket.SetEntities(new Packet_Entity[] { ServerPackets.GetEntityPacket(owningPlayer.Entity, ms, writer) });
				IServerPlayer[] array = (from pair in this.Clients
					where !pair.Value.ServerAssetsSent || pair.Value.Id == owningPlayer.ClientId
					select pair.Value.Player).ToArray<ServerPlayer>();
				IServerPlayer[] skipPlayers = array;
				this.BroadcastPacket(new Packet_Server
				{
					Id = 40,
					Entities = entitiesPacket
				}, skipPlayers);
				Packet_Server otherplayerspacket = ((ServerWorldPlayerData)owningPlayer.WorldData).ToPacketForOtherPlayers(owningPlayer);
				this.SendPacket(toPlayer, otherplayerspacket);
			}
		}

		public void BroadcastPlayerPings()
		{
			Packet_ServerPlayerPing p = new Packet_ServerPlayerPing();
			ICollection<ConnectedClient> values = this.Clients.Values;
			int count = values.Count;
			int[] clientids = new int[count];
			int[] pings = new int[count];
			int i = 0;
			foreach (ConnectedClient client in values)
			{
				if (client.State != EnumClientState.Connecting && client.State != EnumClientState.Offline && client.State != EnumClientState.Queued)
				{
					clientids[i] = client.Id;
					pings[i] = (int)(1000f * client.Player.Ping);
					i++;
				}
			}
			p.SetPings(pings);
			p.SetClientIds(clientids);
			Packet_Server packet = new Packet_Server
			{
				Id = 3,
				PlayerPing = p
			};
			this.BroadcastArbitraryPacket(packet, Array.Empty<IServerPlayer>());
		}

		public void SendServerRedirect(IServerPlayer player, string host, string name)
		{
			Packet_Server p = new Packet_Server
			{
				Id = 29,
				Redirect = new Packet_ServerRedirect
				{
					Host = host,
					Name = name
				}
			};
			this.SendPacket(player, p);
		}

		public IGameCalendar Calendar
		{
			get
			{
				return this.GameWorldCalendar;
			}
		}

		public void UpdateEntityChunk(Entity entity, long newChunkIndex3d)
		{
			IWorldChunk newChunk = this.worldmap.GetChunk(newChunkIndex3d);
			if (newChunk == null)
			{
				return;
			}
			IWorldChunk chunk = this.worldmap.GetChunk(entity.InChunkIndex3d);
			if (chunk != null)
			{
				chunk.RemoveEntity(entity.EntityId);
			}
			newChunk.AddEntity(entity);
			entity.InChunkIndex3d = newChunkIndex3d;
		}

		public int SetMiniDimension(IMiniDimension dimension, int index)
		{
			this.LoadedMiniDimensions[index] = dimension;
			return index;
		}

		public IMiniDimension GetMiniDimension(int index)
		{
			IMiniDimension dimension;
			this.LoadedMiniDimensions.TryGetValue(index, out dimension);
			return dimension;
		}

		public bool ShuttingDown
		{
			get
			{
				return this.RunPhase >= EnumServerRunPhase.Shutdown;
			}
		}

		public long[] LoadedChunkIndices
		{
			get
			{
				return this.loadedChunks.Keys.ToArray<long>();
			}
		}

		public long[] LoadedMapChunkIndices
		{
			get
			{
				return this.loadedMapChunks.Keys.ToArray<long>();
			}
		}

		public ServerChunk GetLoadedChunk(long index3d)
		{
			ServerChunk chunk = null;
			this.loadedChunksLock.AcquireReadLock();
			try
			{
				this.loadedChunks.TryGetValue(index3d, out chunk);
			}
			finally
			{
				this.loadedChunksLock.ReleaseReadLock();
			}
			return chunk;
		}

		public void SendChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player, bool onlyIfInRange)
		{
			ServerPlayer plr = player as ServerPlayer;
			if (((plr != null) ? plr.Entity : null) == null || ((plr != null) ? plr.client : null) == null)
			{
				return;
			}
			ConnectedClient client = plr.client;
			long index3d = this.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ);
			long index2d = this.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			float viewDist = (float)(client.WorldData.Viewdistance + 16);
			if (!onlyIfInRange || client.Entityplayer.ServerPos.InHorizontalRangeOf(chunkX * 32 + 16, chunkZ * 32 + 16, viewDist * viewDist))
			{
				if (!client.DidSendMapChunk(index2d))
				{
					client.forceSendMapChunks.Add(index2d);
				}
				client.forceSendChunks.Add(index3d);
			}
		}

		public void BroadcastChunk(int chunkX, int chunkY, int chunkZ, bool onlyIfInRange)
		{
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if (client.Entityplayer != null)
				{
					long index3d = this.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ);
					long index2d = this.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
					float viewDist = (float)(client.WorldData.Viewdistance + 16);
					if (!onlyIfInRange || client.Entityplayer.ServerPos.InHorizontalRangeOf(chunkX * 32 + 16, chunkZ * 32 + 16, viewDist * viewDist))
					{
						if (!client.DidSendMapChunk(index2d))
						{
							client.forceSendMapChunks.Add(index2d);
						}
						client.forceSendChunks.Add(index3d);
					}
				}
			}
		}

		public void BroadcastChunkColumn(int chunkX, int chunkZ, bool onlyIfInRange)
		{
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if (client.Entityplayer != null)
				{
					float viewDist = (float)(client.WorldData.Viewdistance + 16);
					if (!onlyIfInRange || client.Entityplayer.ServerPos.InHorizontalRangeOf(chunkX * 32 + 16, chunkZ * 32 + 16, viewDist * viewDist))
					{
						for (int cy = 0; cy < this.WorldMap.ChunkMapSizeY; cy++)
						{
							long index3d = this.WorldMap.ChunkIndex3D(chunkX, cy, chunkZ);
							client.forceSendChunks.Add(index3d);
						}
					}
				}
			}
		}

		public void ResendMapChunk(int chunkX, int chunkZ, bool onlyIfInRange)
		{
			foreach (ConnectedClient client in this.Clients.Values)
			{
				if (client.Entityplayer != null)
				{
					long index2d = this.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
					float viewDist = (float)(client.WorldData.Viewdistance + 16);
					if (!onlyIfInRange || client.Entityplayer.ServerPos.InHorizontalRangeOf(chunkX * 32 + 16, chunkZ * 32 + 16, viewDist * viewDist))
					{
						client.forceSendMapChunks.Add(index2d);
					}
				}
			}
		}

		public void LoadChunkColumnFast(int chunkX, int chunkZ, ChunkLoadOptions options = null)
		{
			if (options == null)
			{
				options = this.defaultOptions;
			}
			long mapindex2d = this.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			if (options.KeepLoaded)
			{
				this.AddChunkColumnToForceLoadedList(mapindex2d);
			}
			if (!this.IsChunkColumnFullyLoaded(chunkX, chunkZ))
			{
				KeyValuePair<HorRectanglei, ChunkLoadOptions> data = new KeyValuePair<HorRectanglei, ChunkLoadOptions>(new HorRectanglei(chunkX, chunkZ, chunkX, chunkZ), options);
				this.fastChunkQueue.Enqueue(data);
				return;
			}
			Action onLoaded = options.OnLoaded;
			if (onLoaded == null)
			{
				return;
			}
			onLoaded();
		}

		public void LoadChunkColumnFast(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null)
		{
			if (options == null)
			{
				options = this.defaultOptions;
			}
			if (options.KeepLoaded)
			{
				for (int chunkX3 = chunkX1; chunkX3 <= chunkX2; chunkX3++)
				{
					for (int chunkZ3 = chunkZ1; chunkZ3 <= chunkZ2; chunkZ3++)
					{
						long mapindex2d = this.WorldMap.MapChunkIndex2D(chunkX3, chunkZ3);
						this.AddChunkColumnToForceLoadedList(mapindex2d);
					}
				}
			}
			KeyValuePair<HorRectanglei, ChunkLoadOptions> data = new KeyValuePair<HorRectanglei, ChunkLoadOptions>(new HorRectanglei(chunkX1, chunkZ1, chunkX2, chunkZ2), options);
			this.fastChunkQueue.Enqueue(data);
		}

		public void PeekChunkColumn(int chunkX, int chunkZ, ChunkPeekOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException("options argument must not be null");
			}
			if (options.OnGenerated == null)
			{
				throw new ArgumentNullException("options.OnGenerated must not be null (there is no point to calling this method otherwise)");
			}
			KeyValuePair<Vec2i, ChunkPeekOptions> data = new KeyValuePair<Vec2i, ChunkPeekOptions>(new Vec2i(chunkX, chunkZ), options);
			this.peekChunkColumnQueue.Enqueue(data);
		}

		public void TestChunkExists(int chunkX, int chunkY, int chunkZ, Action<bool> onTested, EnumChunkType type)
		{
			this.testChunkExistsQueue.Enqueue(new ChunkLookupRequest(chunkX, chunkY, chunkZ, onTested)
			{
				Type = type
			});
		}

		public void LoadChunkColumn(int chunkX, int chunkZ, bool keepLoaded = false)
		{
			long mapindex2d = this.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			if (keepLoaded)
			{
				this.AddChunkColumnToForceLoadedList(mapindex2d);
			}
			if (!this.IsChunkColumnFullyLoaded(chunkX, chunkZ))
			{
				object obj = this.requestedChunkColumnsLock;
				lock (obj)
				{
					this.requestedChunkColumns.Enqueue(mapindex2d);
				}
			}
		}

		public void AddChunkColumnToForceLoadedList(long mapindex2d)
		{
			this.forceLoadedChunkColumns.Add(mapindex2d);
		}

		public void RemoveChunkColumnFromForceLoadedList(long mapindex2d)
		{
			this.forceLoadedChunkColumns.Remove(mapindex2d);
		}

		public bool IsChunkColumnFullyLoaded(int chunkX, int chunkZ)
		{
			long xzMultiplier = 2097152L;
			xzMultiplier *= xzMultiplier;
			long chunkIndex3d = this.WorldMap.ChunkIndex3D(chunkX, 0, chunkZ);
			this.loadedChunksLock.AcquireReadLock();
			try
			{
				for (long cy = 0L; cy < (long)this.WorldMap.ChunkMapSizeY; cy += 1L)
				{
					if (!this.loadedChunks.ContainsKey(chunkIndex3d + cy * xzMultiplier))
					{
						return false;
					}
				}
			}
			finally
			{
				this.loadedChunksLock.ReleaseReadLock();
			}
			return true;
		}

		public void CreateChunkColumnForDimension(int cx, int cz, int dim)
		{
			int quantity = this.WorldMap.ChunkMapSizeY;
			ServerMapChunk mapChunk = (ServerMapChunk)this.WorldMap.GetMapChunk(cx, cz);
			int cy = dim * 32768 / 32;
			this.loadedChunksLock.AcquireWriteLock();
			try
			{
				for (int y = 0; y < quantity; y++)
				{
					long index3d = this.WorldMap.ChunkIndex3D(cx, cy + y, cz);
					ServerChunk chunk = ServerChunk.CreateNew(this.serverChunkDataPool);
					chunk.serverMapChunk = mapChunk;
					this.loadedChunks[index3d] = chunk;
					chunk.MarkToPack();
				}
			}
			finally
			{
				this.loadedChunksLock.ReleaseWriteLock();
			}
		}

		public void LoadChunkColumnForDimension(int cx, int cz, int dim)
		{
			ChunkColumnLoadRequest request = new ChunkColumnLoadRequest(this.WorldMap.MapChunkIndex2D(cx, cz), cx, cz, -1, 6, this);
			request.dimension = dim;
			this.simpleLoadRequests.Enqueue(request);
		}

		public void ForceSendChunkColumn(IServerPlayer player, int cx, int cz, int dimension)
		{
			ConnectedClient client = ((ServerPlayer)player).client;
			int maxY = this.WorldMap.ChunkMapSizeY;
			for (int cy = 0; cy < maxY; cy++)
			{
				long index = this.WorldMap.ChunkIndex3D(cx, cy, cz, dimension);
				client.forceSendChunks.Add(index);
			}
		}

		public bool BlockingTestMapRegionExists(int regionX, int regionZ)
		{
			return this.chunkThread.gameDatabase.MapRegionExists(regionX, regionZ);
		}

		public bool BlockingTestMapChunkExists(int chunkX, int chunkZ)
		{
			return this.chunkThread.gameDatabase.MapChunkExists(chunkX, chunkZ);
		}

		public IServerChunk[] BlockingLoadChunkColumn(int chunkX, int chunkZ)
		{
			ChunkColumnLoadRequest chunkColumnLoadRequest = new ChunkColumnLoadRequest(0L, chunkX, chunkZ, -1, 0, this);
			return this.chunkThread.loadsavechunks.TryLoadChunkColumn(chunkColumnLoadRequest);
		}

		internal ConcurrentDictionary<int, ClientLastLogin> RecentClientLogins = new ConcurrentDictionary<int, ClientLastLogin>();

		public static IXPlatformInterface xPlatInterface;

		public GameExit exit;

		private bool suspended;

		public ThreadLocal<Random> rand = new ThreadLocal<Random>(() => new Random(Environment.TickCount));

		public bool Saving;

		public bool SendChunks = true;

		public bool AutoGenerateChunks = true;

		public bool stopped;

		public PlayerSpawnPos mapMiddleSpawnPos;

		public static Logger Logger;

		[ThreadStatic]
		public static FrameProfilerUtil FrameProfiler;

		public AssetManager AssetManager;

		internal EnumServerRunPhase RunPhase = EnumServerRunPhase.Standby;

		public bool readyToAutoSave = true;

		public List<Thread> Serverthreads = new List<Thread>();

		public readonly CancellationTokenSource ServerThreadsCts;

		internal List<ServerThread> ServerThreadLoops = new List<ServerThread>();

		internal ServerSystem[] Systems;

		public ServerEventManager ModEventManager;

		public CoreServerEventManager EventManager;

		public PlayerDataManager PlayerDataManager;

		public ServerUdpNetwork ServerUdpNetwork;

		private Thread ClientPacketParsingThread;

		public ServerWorldMap WorldMap;

		internal int CurrentPort;

		internal string CurrentIp;

		public static ClassRegistry ClassRegistry;

		public bool Standalone;

		private Stopwatch lastFramePassedTime = new Stopwatch();

		public Stopwatch totalUnpausedTime = new Stopwatch();

		public Stopwatch totalUpTime = new Stopwatch();

		public HashSet<string> AllPrivileges = new HashSet<string>();

		public Dictionary<string, string> PrivilegeDescriptions = new Dictionary<string, string>();

		internal int serverConsoleId = -1;

		private readonly CancellationTokenSource _consoleThreadsCts;

		internal ServerConsoleClient ServerConsoleClient;

		private ServerConsole serverConsole;

		public StatsCollection[] StatsCollector = new StatsCollection[]
		{
			new StatsCollection(),
			new StatsCollection(),
			new StatsCollection(),
			new StatsCollection()
		};

		public int StatsCollectorIndex;

		public CachingConcurrentDictionary<int, ConnectedClient> Clients = new CachingConcurrentDictionary<int, ConnectedClient>();

		public Dictionary<string, ServerPlayer> PlayersByUid = new Dictionary<string, ServerPlayer>();

		public long TotalSentBytes;

		public long TotalSentBytesUdp;

		public long TotalReceivedBytes;

		public long TotalReceivedBytesUdp;

		public ServerConfig Config;

		public bool ConfigNeedsSaving;

		public bool ReducedServerThreads;

		internal long lastDisconnectTotalMs;

		private int lastClientId;

		public ConcurrentQueue<BlockPos> DirtyBlockEntities = new ConcurrentQueue<BlockPos>();

		public ConcurrentQueue<BlockPos> ModifiedBlocks = new ConcurrentQueue<BlockPos>();

		public ConcurrentQueue<Vec4i> DirtyBlocks = new ConcurrentQueue<Vec4i>();

		public ConcurrentQueue<BlockPos> ModifiedDecors = new ConcurrentQueue<BlockPos>();

		public ConcurrentQueue<BlockPos> ModifiedBlocksNoRelight = new ConcurrentQueue<BlockPos>();

		public List<BlockPos> ModifiedBlocksMinimal = new List<BlockPos>();

		public Queue<BlockPos> UpdatedBlocks = new Queue<BlockPos>();

		internal int nextFreeBlockId;

		public OrderedDictionary<AssetLocation, ITreeGenerator> TreeGeneratorsByTreeCode = new OrderedDictionary<AssetLocation, ITreeGenerator>();

		public OrderedDictionary<AssetLocation, EntityProperties> EntityTypesByCode = new OrderedDictionary<AssetLocation, EntityProperties>();

		internal List<EntityProperties> entityTypesCached;

		internal List<string> entityCodesCached;

		public int nextFreeItemId;

		internal Dictionary<EnumClientAwarenessEvent, List<Action<ClientStatistics>>> clientAwarenessEvents;

		internal ServerSystemClientAwareness clientAwarenessSystem;

		public object mainThreadTasksLock = new object();

		private Queue<Action> mainThreadTasks = new Queue<Action>();

		public StartServerArgs serverStartArgs;

		public ServerProgramArgs progArgs;

		public string[] RawCmdLineArgs;

		public int TickPosition;

		internal ChunkServerThread chunkThread;

		internal object suspendLock = new object();

		public int ExitCode;

		private int nextClientID = 1;

		internal DateTime statsupdate;

		public Dictionary<Timer, Timer.Tick> Timers = new Dictionary<Timer, Timer.Tick>();

		private bool ignoreDisconnectCalls;

		internal float[] blockLightLevels = new float[]
		{
			0.062f, 0.102f, 0.142f, 0.182f, 0.222f, 0.262f, 0.302f, 0.342f, 0.382f, 0.422f,
			0.462f, 0.502f, 0.542f, 0.582f, 0.622f, 0.662f, 0.702f, 0.742f, 0.782f, 0.822f,
			0.862f, 0.902f, 0.943f, 0.985f, 1f, 1f, 1f, 1f, 1f, 1f,
			1f, 1f
		};

		internal float[] sunLightLevels = new float[]
		{
			0.062f, 0.102f, 0.142f, 0.182f, 0.222f, 0.262f, 0.302f, 0.342f, 0.382f, 0.422f,
			0.462f, 0.502f, 0.542f, 0.582f, 0.622f, 0.662f, 0.702f, 0.742f, 0.782f, 0.822f,
			0.862f, 0.902f, 0.943f, 0.985f, 1f, 1f, 1f, 1f, 1f, 1f,
			1f, 1f
		};

		internal int sunBrightness = 24;

		internal int seaLevel = 110;

		private CollisionTester collTester = new CollisionTester();

		internal ClientPacketHandler<Packet_Client, ConnectedClient>[] PacketHandlers = new ClientPacketHandler<Packet_Client, ConnectedClient>[255];

		public HandleClientCustomUdpPacket HandleCustomUdpPackets;

		internal bool[] PacketHandlingOnConnectingAllowed = new bool[255];

		public List<QueuedClient> ConnectionQueue = new List<QueuedClient>();

		internal ConcurrentQueue<ReceivedClientPacket> ClientPackets = new ConcurrentQueue<ReceivedClientPacket>();

		internal List<int> DisconnectedClientsThisTick = new List<int>();

		[ThreadStatic]
		private static BoxedPacket reusableBuffer;

		private readonly List<BoxedArray> reusableBuffersDisposalList = new List<BoxedArray>();

		internal bool doNetBenchmark;

		internal SortedDictionary<int, int> packetBenchmark = new SortedDictionary<int, int>();

		internal SortedDictionary<string, int> packetBenchmarkBlockEntitiesBytes = new SortedDictionary<string, int>();

		internal SortedDictionary<int, int> packetBenchmarkBytes = new SortedDictionary<int, int>();

		internal SortedDictionary<int, int> udpPacketBenchmark = new SortedDictionary<int, int>();

		internal SortedDictionary<int, int> udpPacketBenchmarkBytes = new SortedDictionary<int, int>();

		private readonly BoxedPacket_ServerAssets serverAssetsPacket = new BoxedPacket_ServerAssets();

		private bool serverAssetsSentLocally;

		private bool worldMetaDataPacketAlreadySentToSinglePlayer;

		internal GameCalendar GameWorldCalendar;

		internal long lastUpdateSentToClient;

		public bool DebugPrivileges;

		public HashSet<string> ClearPlayerInvs = new HashSet<string>();

		internal bool SpawnDebug;

		internal ServerCoreAPI api;

		internal HandleHandInteractionDelegate OnHandleBlockInteract;

		internal readonly CachingConcurrentDictionary<long, Entity> LoadedEntities = new CachingConcurrentDictionary<long, Entity>();

		public List<Entity> EntitySpawnSendQueue = new List<Entity>(10);

		public List<KeyValuePair<Entity, EntityDespawnData>> EntityDespawnSendQueue = new List<KeyValuePair<Entity, EntityDespawnData>>(10);

		public ConcurrentQueue<Entity> DelayedSpawnQueue = new ConcurrentQueue<Entity>();

		public Dictionary<string, string> EntityCodeRemappings = new Dictionary<string, string>();

		internal ConcurrentDictionary<long, ServerMapChunk> loadedMapChunks = new ConcurrentDictionary<long, ServerMapChunk>();

		internal ConcurrentDictionary<long, ServerMapRegion> loadedMapRegions = new ConcurrentDictionary<long, ServerMapRegion>();

		internal ConcurrentDictionary<int, IMiniDimension> LoadedMiniDimensions = new ConcurrentDictionary<int, IMiniDimension>();

		internal SaveGame SaveGameData;

		internal ChunkDataPool serverChunkDataPool;

		internal FastRWLock loadedChunksLock;

		internal Dictionary<long, ServerChunk> loadedChunks = new Dictionary<long, ServerChunk>(2000);

		internal object requestedChunkColumnsLock = new object();

		internal UniqueQueue<long> requestedChunkColumns = new UniqueQueue<long>();

		internal ConcurrentDictionary<long, int> ChunkColumnRequested = new ConcurrentDictionary<long, int>();

		internal ConcurrentQueue<long> unloadedChunks = new ConcurrentQueue<long>();

		internal HashSet<long> forceLoadedChunkColumns = new HashSet<long>();

		internal ConcurrentQueue<ChunkColumnLoadRequest> simpleLoadRequests = new ConcurrentQueue<ChunkColumnLoadRequest>();

		internal ConcurrentQueue<long> deleteChunkColumns = new ConcurrentQueue<long>();

		internal ConcurrentQueue<long> deleteMapRegions = new ConcurrentQueue<long>();

		internal ConcurrentQueue<KeyValuePair<HorRectanglei, ChunkLoadOptions>> fastChunkQueue = new ConcurrentQueue<KeyValuePair<HorRectanglei, ChunkLoadOptions>>();

		internal ConcurrentQueue<KeyValuePair<Vec2i, ChunkPeekOptions>> peekChunkColumnQueue = new ConcurrentQueue<KeyValuePair<Vec2i, ChunkPeekOptions>>();

		internal ConcurrentQueue<ChunkLookupRequest> testChunkExistsQueue = new ConcurrentQueue<ChunkLookupRequest>();

		private ChunkLoadOptions defaultOptions = new ChunkLoadOptions();

		public bool requiresRemaps;
	}
}
