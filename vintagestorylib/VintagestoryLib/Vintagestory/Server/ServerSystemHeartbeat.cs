using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class ServerSystemHeartbeat : ServerSystem
	{
		public override int GetUpdateInterval()
		{
			return 120000;
		}

		public ServerSystemHeartbeat(ServerMain server)
			: base(server)
		{
			server.EventManager.OnUpnpComplete += this.EventManager_OnUpnpComplete;
		}

		public override void OnBeginModsAndConfigReady()
		{
			this.server.Config.onAdvertiseChanged += this.Config_onAdvertiseChanged;
			this.server.Config.onUpnpChanged += this.Config_onUpnpChanged;
		}

		private void Config_onUpnpChanged()
		{
			if (!this.server.Config.Upnp)
			{
				this.upnpComplete = false;
			}
		}

		private void Config_onAdvertiseChanged()
		{
			if ((this.server.Config.Upnp || this.server.Config.RuntimeUpnp) && !this.upnpComplete)
			{
				return;
			}
			if (this.server.Config.AdvertiseServer)
			{
				this.SendRegister();
				return;
			}
			this.SendUnregister();
		}

		private void EventManager_OnUpnpComplete(bool success)
		{
			if (!this.server.Config.AdvertiseServer)
			{
				return;
			}
			if (this.token != null && this.token.Length > 0)
			{
				return;
			}
			if (!success)
			{
				ServerMain.Logger.Error("Upnp failed, will not attempt to register to the master server");
				return;
			}
			ServerMain.Logger.Notification("Server Advertising enabled. Attempt to register at the master server.");
			this.upnpComplete = true;
			try
			{
				this.SendRegister();
			}
			catch (Exception)
			{
				ServerMain.Logger.Error("Failed to register on the master server");
			}
		}

		public override void OnBeginRunGame()
		{
			if (!this.server.Config.AdvertiseServer || !this.server.IsDedicatedServer)
			{
				return;
			}
			if (this.server.Config.Upnp)
			{
				return;
			}
			ServerMain.Logger.Notification("Server Advertising enabled. Attempt to register at the master server.");
			try
			{
				this.SendRegister();
			}
			catch (Exception)
			{
				ServerMain.Logger.Error("Failed to register on the master server");
			}
		}

		public override void OnBeginShutdown()
		{
			this.SendUnregister();
		}

		public override void OnServerTick(float dt)
		{
			if (this.token == null || this.token.Length == 0)
			{
				return;
			}
			this.SendHeartbeat();
		}

		public void SendHeartbeat()
		{
			if (!this.server.Config.VerifyPlayerAuth)
			{
				return;
			}
			HeartbeatPacket packet = new HeartbeatPacket
			{
				token = this.token,
				players = this.server.GetPlayingClients()
			};
			this.SendRequestAsync<HeartbeatPacket>(this.server.Config.MasterserverUrl + "heartbeat", packet, delegate(ResponsePacket response)
			{
				if (response.status == "invalid" || response.status == "timeout")
				{
					ServerMain.Logger.Notification("Master server sent response {0}. Will re-register now.", new object[] { response.status });
					this.server.EnqueueMainThreadTask(delegate
					{
						this.SendRegister();
					});
				}
			});
		}

		public void SendUnregister()
		{
			if (this.token == null || this.token.Length == 0)
			{
				return;
			}
			ServerMain.Logger.Notification("Unregistering from master server...");
			string text = this.server.Config.MasterserverUrl + "unregister";
			UnregisterPacket unregisterPacket = new UnregisterPacket();
			unregisterPacket.token = this.token;
			this.SendRequestAsync<UnregisterPacket>(text, unregisterPacket, delegate(ResponsePacket _)
			{
			});
		}

		private void SendRegister()
		{
			if (!this.server.Config.VerifyPlayerAuth)
			{
				ServerMain.Logger.Notification("VerifyPlayerAuth is off. Will not register to master server");
			}
			ServerMain.Logger.Notification("Registering to master server...");
			ModPacket[] mods = (from mod in this.server.api.ModLoader.Mods
				where mod.Info.Side.IsUniversal() && mod.Info.RequiredOnClient
				select new ModPacket
				{
					id = mod.Info.ModID,
					version = mod.Info.Version
				}).ToArray<ModPacket>();
			bool whitelistonly = this.server.Config.WhitelistMode == EnumWhitelistMode.On || (this.server.Config.WhitelistMode == EnumWhitelistMode.Default && this.server.IsDedicatedServer);
			RegisterRequestPacket packet = new RegisterRequestPacket
			{
				gameVersion = "1.21.5",
				maxPlayers = (ushort)this.server.Config.MaxClients,
				name = this.server.Config.ServerName,
				serverUrl = this.server.Config.ServerUrl,
				vhIdentifier = this.server.Config.VhIdentifier,
				gameDescription = this.server.Config.ServerDescription,
				hasPassword = this.server.Config.IsPasswordProtected(),
				playstyle = new PlaystylePacket
				{
					id = this.server.SaveGameData.PlayStyle,
					langCode = this.server.SaveGameData.PlayStyleLangCode
				},
				port = (ushort)this.server.CurrentPort,
				Mods = mods,
				whitelisted = whitelistonly
			};
			this.SendRequestAsync<RegisterRequestPacket>(this.server.Config.MasterserverUrl + "register", packet, delegate(ResponsePacket response)
			{
				ServerMain.Logger.Notification("Master server response status: {0}", new object[] { response.status });
				if (response.status == "blacklisted")
				{
					ServerMain.Logger.Warning("Your server has been blacklisted from the public server list. Likely due to inappropriate naming. Other players can still connect however. You may request to be removed from the blacklist through a support ticket on the official site.");
				}
				if (response.status == "ok")
				{
					this.token = response.data;
					this.server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, "Successfully registered to master server", EnumChatType.Notification, null, "masterserverstatus:ok");
					return;
				}
				string msg = "Could not register to master server, master server says: " + response.data;
				ServerMain.Logger.Notification(msg);
				this.server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, msg, EnumChatType.Notification, null, "masterserverstatus:fail");
			});
		}

		private async void SendRequestAsync<T>(string url, T packet, Action<ResponsePacket> onComplete)
		{
			string json = string.Empty;
			try
			{
				json = JsonConvert.SerializeObject(packet);
				StringContent jsonContent = new StringContent(json, null, "application/json");
				HttpResponseMessage httpResponseMessage2 = await VSWebClient.Inst.PostAsync(url, jsonContent);
				HttpResponseMessage httpResponseMessage = httpResponseMessage2;
				ResponsePacket responsePacket = JsonConvert.DeserializeObject<ResponsePacket>(await httpResponseMessage.Content.ReadAsStringAsync());
				if (responsePacket == null)
				{
					ResponsePacket responsePacket2 = new ResponsePacket();
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 1);
					defaultInterpolatedStringHandler.AppendLiteral("StatusCode: ");
					defaultInterpolatedStringHandler.AppendFormatted<int>((int)httpResponseMessage.StatusCode);
					responsePacket2.data = defaultInterpolatedStringHandler.ToStringAndClear();
					responsePacket2.status = "timeout";
					onComplete(responsePacket2);
				}
				else
				{
					onComplete(responsePacket);
				}
				httpResponseMessage = null;
			}
			catch (TaskCanceledException es)
			{
				ServerMain.Logger.Warning("Socket exception on master server async request: {0}", new object[] { es.Message });
				onComplete(new ResponsePacket
				{
					data = null,
					status = "timeout"
				});
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Failed request to master server url {0}.\nSent Json data: {1}", new object[] { url, json });
				ServerMain.Logger.Error(e);
			}
		}

		private string token;

		private bool upnpComplete;
	}
}
