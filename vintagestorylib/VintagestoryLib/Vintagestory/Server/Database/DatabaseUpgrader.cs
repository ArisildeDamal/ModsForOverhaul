using System;

namespace Vintagestory.Server.Database
{
	public class DatabaseUpgrader
	{
		public DatabaseUpgrader(ServerMain server, string worldFilename, int curVersion, int destVersion)
		{
			this.server = server;
			this.worldFilename = worldFilename;
			this.curVersion = curVersion;
			this.destVersion = destVersion;
		}

		public void PerformUpgrade()
		{
			while (this.curVersion < this.destVersion)
			{
				this.ApplyUpgrader(this.curVersion + 1);
				this.curVersion++;
			}
		}

		private void ApplyUpgrader(int curVersion)
		{
			IDatabaseUpgrader upgrader = null;
			if (curVersion == 2)
			{
				upgrader = new DatabaseUpgraderToVersion2();
			}
			if (upgrader == null)
			{
				ServerMain.Logger.Event("No upgrader to " + curVersion.ToString() + " found.");
				throw new Exception("No upgrader to " + curVersion.ToString() + " found.");
			}
			upgrader.Upgrade(this.server, this.worldFilename);
		}

		private string worldFilename;

		private int curVersion;

		private int destVersion;

		private ServerMain server;
	}
}
