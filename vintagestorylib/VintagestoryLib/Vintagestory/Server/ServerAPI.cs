using System;
using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	internal class ServerAPI : ServerAPIComponentBase, IServerAPI
	{
		public ServerAPI(ServerMain server)
			: base(server)
		{
		}

		public void LogChat(string s)
		{
			ServerMain.Logger.Chat(s);
		}

		public void LogBuild(string message, params object[] args)
		{
			ServerMain.Logger.Build(message, args);
		}

		public void LogChat(string message, params object[] args)
		{
			ServerMain.Logger.Chat(message, args);
		}

		public void LogVerboseDebug(string message, params object[] args)
		{
			ServerMain.Logger.VerboseDebug(message, args);
		}

		public void LogDebug(string message, params object[] args)
		{
			ServerMain.Logger.Debug(message, args);
		}

		public void LogNotification(string message, params object[] args)
		{
			ServerMain.Logger.Notification(message, args);
		}

		public void LogWarning(string message, params object[] args)
		{
			ServerMain.Logger.Warning(message, args);
		}

		public void LogError(string message, params object[] args)
		{
			ServerMain.Logger.Error(message, args);
		}

		public void LogFatal(string message, params object[] args)
		{
			ServerMain.Logger.Fatal(message, args);
		}

		public void LogEvent(string message, params object[] args)
		{
			ServerMain.Logger.Event(message, args);
		}

		public ILogger Logger
		{
			get
			{
				return ServerMain.Logger;
			}
		}

		public bool IsDedicated
		{
			get
			{
				return this.server.IsDedicatedServer;
			}
		}

		public int TotalWorldPlayTime
		{
			get
			{
				return this.server.SaveGameData.TotalSecondsPlayed;
			}
		}

		public string ServerIp
		{
			get
			{
				if (!this.server.IsDedicatedServer)
				{
					return this.server.MainSockets[0].LocalEndpoint;
				}
				return this.server.MainSockets[1].LocalEndpoint;
			}
		}

		public long TotalReceivedBytes
		{
			get
			{
				return this.server.TotalReceivedBytes;
			}
		}

		public long TotalSentBytes
		{
			get
			{
				return this.server.TotalSentBytes;
			}
		}

		public long TotalReceivedBytesUdp
		{
			get
			{
				return this.server.TotalReceivedBytesUdp;
			}
		}

		public long TotalSentBytesUdp
		{
			get
			{
				return this.server.TotalSentBytesUdp;
			}
		}

		public int ServerUptimeSeconds
		{
			get
			{
				return (int)this.server.totalUnpausedTime.Elapsed.TotalSeconds;
			}
		}

		public long ServerUptimeMilliseconds
		{
			get
			{
				return (long)((int)this.server.totalUnpausedTime.ElapsedMilliseconds);
			}
		}

		public bool IsShuttingDown
		{
			get
			{
				return this.server.exit.GetExit();
			}
		}

		public void ShutDown()
		{
			this.server.AttemptShutdown("Shutdown through Server API", 7500);
		}

		public void MarkConfigDirty()
		{
			this.server.ConfigNeedsSaving = true;
		}

		public void AddServerThread(string threadname, IAsyncServerSystem system)
		{
			this.server.AddServerThread(threadname, system);
		}

		public bool PauseThread(string threadname, int waitTimeoutMs = 5000)
		{
			ServerThread t = this.server.ServerThreadLoops.FirstOrDefault((ServerThread val) => val.threadName == threadname);
			t.ShouldPause = true;
			while (waitTimeoutMs > 0 && !t.paused)
			{
				Thread.Sleep(50);
				waitTimeoutMs -= 50;
			}
			return t.paused;
		}

		public void ResumeThread(string threadname)
		{
			this.server.ServerThreadLoops.FirstOrDefault((ServerThread val) => val.threadName == threadname).ShouldPause = false;
		}

		public EnumServerRunPhase CurrentRunPhase
		{
			get
			{
				return this.server.RunPhase;
			}
		}

		public IServerConfig Config
		{
			get
			{
				return this.server.Config;
			}
		}

		public IServerPlayer[] Players
		{
			get
			{
				return this.server.PlayersByUid.Values.ToArray<ServerPlayer>();
			}
		}

		public bool ReducedServerThreads
		{
			get
			{
				return this.server.ReducedServerThreads;
			}
		}

		public int LoadMiniDimension(IMiniDimension blocks)
		{
			if (this.server.SaveGameData.MiniDimensionsCreated >= 16777216)
			{
				return -1;
			}
			SaveGame saveGameData = this.server.SaveGameData;
			int num = saveGameData.MiniDimensionsCreated + 1;
			saveGameData.MiniDimensionsCreated = num;
			int index = num;
			this.server.SetMiniDimension(blocks, index);
			return index;
		}

		public int SetMiniDimension(IMiniDimension blocks, int subId)
		{
			return this.server.SetMiniDimension(blocks, subId);
		}

		public IMiniDimension GetMiniDimension(int subId)
		{
			return this.server.GetMiniDimension(subId);
		}

		public void AddPhysicsTickable(IPhysicsTickable entityBehavior)
		{
			this.server.ServerUdpNetwork.physicsManager.toAdd.Enqueue(entityBehavior);
		}

		public void RemovePhysicsTickable(IPhysicsTickable entityBehavior)
		{
			this.server.ServerUdpNetwork.physicsManager.toRemove.Enqueue(entityBehavior);
		}
	}
}
