using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mono.Nat;
using Open.Nat;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	public class ServerSystemUpnp : ServerSystem
	{
		public ServerSystemUpnp(ServerMain server)
			: base(server)
		{
			server.api.ChatCommands.Create("upnp").WithDescription("Runtime only setting. When turned on, the server will attempt to set up port forwarding through PMP or UPnP. When turned off, the port forward will be deleted again.").WithArgs(new ICommandArgumentParser[] { server.api.ChatCommands.Parsers.OptionalBool("on_off", "on") })
				.RequiresPrivilege(Privilege.controlserver)
				.HandleWith(new OnCommandDelegate(this.OnCmdToggleUpnp));
		}

		private TextCommandResult OnCmdToggleUpnp(TextCommandCallingArgs args)
		{
			bool nowon = (bool)args[0];
			if (nowon)
			{
				this.Initiate();
			}
			else
			{
				this.Dispose();
			}
			this.wasOn = nowon;
			this.server.Config.RuntimeUpnp = this.wasOn;
			return TextCommandResult.Success("Upnp mode now " + (nowon ? "on" : "off"), null);
		}

		public override void OnBeginRunGame()
		{
			this.mapping = new Open.Nat.Mapping(Open.Nat.Protocol.Tcp, this.server.CurrentPort, this.server.CurrentPort, 600, "Vintage Story TCP");
			this.mappingUdp = new Open.Nat.Mapping(Open.Nat.Protocol.Udp, this.server.CurrentPort, this.server.CurrentPort, 600, "Vintage Story UDP");
			this.monoNatMapping = new Mono.Nat.Mapping(Mono.Nat.Protocol.Tcp, this.server.CurrentPort, this.server.CurrentPort, 600, "Vintage Story TCP");
			this.monoNatMappingUdp = new Mono.Nat.Mapping(Mono.Nat.Protocol.Udp, this.server.CurrentPort, this.server.CurrentPort, 600, "Vintage Story UDP");
			this.wasOn = this.server.Config.Upnp;
			if (this.wasOn && this.server.IsDedicatedServer)
			{
				this.Initiate();
			}
			this.server.Config.onUpnpChanged += this.onUpnpChanged;
		}

		private void onUpnpChanged()
		{
			if (this.wasOn && !this.server.Config.Upnp)
			{
				this.Dispose();
			}
			if (!this.wasOn && this.server.Config.Upnp)
			{
				this.Initiate();
			}
			this.wasOn = this.server.Config.Upnp;
		}

		public void Initiate()
		{
			string str;
			ServerMain.Logger.Event(str = "Begin searching for PMP and UPnP devices...");
			this.server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, str, EnumChatType.Notification, null, null);
			this.findPmpDeviceAsync();
		}

		private async void findUpnpDeviceAsync()
		{
			CancellationTokenSource cts = new CancellationTokenSource(5000);
			try
			{
				NatDevice upnpdevice = await new NatDiscoverer().DiscoverDeviceAsync(PortMapper.Upnp, cts);
				this.onFoundNatDevice(upnpdevice, "UPnP");
			}
			catch (Exception)
			{
				this.findUpnpDeviceWithMonoNat();
			}
			cts.Dispose();
		}

		private async void findPmpDeviceAsync()
		{
			CancellationTokenSource cts = new CancellationTokenSource(5000);
			try
			{
				NatDevice pmpdevice = await new NatDiscoverer().DiscoverDeviceAsync(PortMapper.Pmp, cts);
				this.onFoundNatDevice(pmpdevice, "PMP");
			}
			catch (Exception)
			{
				this.findUpnpDeviceAsync();
			}
			cts.Dispose();
		}

		private void findUpnpDeviceWithMonoNat()
		{
			if (this.server.RunPhase == EnumServerRunPhase.Shutdown)
			{
				return;
			}
			string str = string.Format("No upnp or pmp device found after 5 seconds. Trying another method...", Array.Empty<object>());
			ServerMain.Logger.Event(str);
			this.server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, str, EnumChatType.Notification, null, null);
			NatUtility.DeviceFound += this.MonoNatDeviceFound;
			NatUtility.StartDiscovery(Array.Empty<NatProtocol>());
			this.server.RegisterCallback(new Action<float>(this.After5s), 5000);
		}

		private void After5s(float dt)
		{
			if (this.monoNatDevice == null)
			{
				NatUtility.StopDiscovery();
				string str = "No upnp or pmp device found using either method. Giving up, sorry.";
				ServerMain.Logger.Event(str);
				this.server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, str, EnumChatType.Notification, null, "nonatdevice");
				this.server.EventManager.TriggerUpnpComplete(false);
			}
			NatUtility.DeviceFound -= this.MonoNatDeviceFound;
		}

		private void CreateRenewCallback()
		{
			if (this.renewListenerId == 0L)
			{
				this.renewListenerId = this.server.RegisterCallback(new Action<float>(this.RenewMapping), 540000);
			}
		}

		private void RenewMapping(float delta)
		{
			Task.Run(async delegate
			{
				try
				{
					if (this.monoNatDevice != null)
					{
						this.monoNatDevice.CreatePortMap(this.monoNatMapping);
						this.monoNatDevice.CreatePortMap(this.monoNatMappingUdp);
						this.ipaddr = this.monoNatDevice.GetExternalIP();
					}
					if (this.natDevice != null)
					{
						await this.natDevice.CreatePortMapAsync(this.mapping);
						await this.natDevice.CreatePortMapAsync(this.mappingUdp);
						this.ipaddr = await this.natDevice.GetExternalIPAsync();
					}
				}
				catch (Exception e)
				{
					ServerMain.Logger.Warning("Failed to renew UnPn Port mapping, removing UPnP");
					ServerMain.Logger.Warning(e);
					this.Dispose();
				}
			});
		}

		private void MonoNatDeviceFound(object sender, DeviceEventArgs e)
		{
			try
			{
				this.monoNatDevice = e.Device;
				this.monoNatDevice.CreatePortMap(this.monoNatMapping);
				this.monoNatDevice.CreatePortMap(this.monoNatMappingUdp);
				this.CreateRenewCallback();
				this.ipaddr = this.monoNatDevice.GetExternalIP();
				this.SendNatMessage();
			}
			catch (Exception ex)
			{
				ServerMain.Logger.Error("mono port map threw an exception:");
				ServerMain.Logger.Error(ex);
				this.monoNatDevice = null;
				this.ipaddr = null;
			}
		}

		private async void onFoundNatDevice(NatDevice device, string type)
		{
			if (this.natDevice == null)
			{
				try
				{
					this.natDevice = device;
					IPAddress ipaddress = await this.natDevice.GetExternalIPAsync();
					this.ipaddr = ipaddress;
					await this.natDevice.CreatePortMapAsync(this.mapping);
					await this.natDevice.CreatePortMapAsync(this.mappingUdp);
					this.CreateRenewCallback();
					this.SendNatMessage();
				}
				catch (Exception)
				{
					this.natDevice = null;
					if (type == "PMP")
					{
						this.findUpnpDeviceAsync();
					}
					if (type == "UPnP")
					{
						this.findUpnpDeviceWithMonoNat();
					}
				}
			}
		}

		private void SendNatMessage()
		{
			if (NetUtil.IsPrivateIp(this.ipaddr.ToString()))
			{
				string str = string.Format("Device with external ip {0} found, but this is a private ip! Might not be accessible. Created mapping for port {1} anyway.", this.ipaddr.ToString(), this.mapping.PublicPort);
				ServerMain.Logger.Event(str);
				ServerMain server = this.server;
				int serverInfoChatGroup = GlobalConstants.ServerInfoChatGroup;
				string text = str;
				EnumChatType enumChatType = EnumChatType.Notification;
				IServerPlayer serverPlayer = null;
				string text2 = "foundnatdeviceprivip:";
				IPAddress ipaddress = this.ipaddr;
				server.SendMessageToGroup(serverInfoChatGroup, text, enumChatType, serverPlayer, text2 + ((ipaddress != null) ? ipaddress.ToString() : null));
			}
			else
			{
				string str2 = string.Format("Device with external ip {0} found. Created mapping for port {1}!", this.ipaddr.ToString(), this.mapping.PublicPort);
				ServerMain.Logger.Event(str2);
				ServerMain server2 = this.server;
				int serverInfoChatGroup2 = GlobalConstants.ServerInfoChatGroup;
				string text3 = str2;
				EnumChatType enumChatType2 = EnumChatType.Notification;
				IServerPlayer serverPlayer2 = null;
				string text4 = "foundnatdevice:";
				IPAddress ipaddress2 = this.ipaddr;
				server2.SendMessageToGroup(serverInfoChatGroup2, text3, enumChatType2, serverPlayer2, text4 + ((ipaddress2 != null) ? ipaddress2.ToString() : null));
			}
			this.server.EventManager.TriggerUpnpComplete(true);
		}

		public override void Dispose()
		{
			if (this.natDevice != null)
			{
				ServerMain.Logger.Event("Deleting port map on device with external ip {0}", new object[] { this.ipaddr.ToString() });
				Task.Run(async delegate
				{
					await this.natDevice.DeletePortMapAsync(this.mapping);
					await this.natDevice.DeletePortMapAsync(this.mappingUdp);
				});
			}
			if (this.monoNatDevice != null)
			{
				ServerMain.Logger.Event("Deleting port map on device with external ip {0}", new object[] { this.ipaddr.ToString() });
				this.monoNatDevice.DeletePortMap(this.monoNatMapping);
				this.monoNatDevice.DeletePortMap(this.monoNatMappingUdp);
			}
			this.server.UnregisterCallback(this.renewListenerId);
			this.renewListenerId = 0L;
			this.natDevice = null;
			this.monoNatDevice = null;
		}

		private NatDevice natDevice;

		private IPAddress ipaddr;

		private INatDevice monoNatDevice;

		private Open.Nat.Mapping mapping;

		private Open.Nat.Mapping mappingUdp;

		private Mono.Nat.Mapping monoNatMapping;

		private Mono.Nat.Mapping monoNatMappingUdp;

		private bool wasOn;

		private long renewListenerId;
	}
}
