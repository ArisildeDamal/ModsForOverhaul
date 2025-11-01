using System;
using Vintagestory.API.Server;

namespace Vintagestory.Server
{
	public class ServerSystemAsync : ServerSystem
	{
		public ServerSystemAsync(ServerMain server, string name, IAsyncServerSystem system)
			: base(server)
		{
			this.server = server;
			this.system = system;
			this.FrameprofilerName = "ss-tick-" + name;
		}

		public override int GetUpdateInterval()
		{
			return this.system.OffThreadInterval();
		}

		public override void OnSeparateThreadTick()
		{
			this.system.OnSeparateThreadTick();
		}

		public override void Dispose()
		{
			this.system.ThreadDispose();
		}

		private IAsyncServerSystem system;
	}
}
