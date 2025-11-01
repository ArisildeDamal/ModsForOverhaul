using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;

namespace Vintagestory.Common
{
	public class GameDatabase : IDisposable
	{
		public string DatabaseFilename
		{
			get
			{
				return this.databaseFilename;
			}
		}

		public GameDatabase(ILogger logger)
		{
			this.logger = logger;
		}

		public bool OpenConnection(string databaseFilename, int databaseVersion, bool corruptionProtection, bool doIntegrityCheck)
		{
			string errorMessage;
			return this.OpenConnection(databaseFilename, databaseVersion, out errorMessage, true, corruptionProtection, doIntegrityCheck);
		}

		public bool OpenConnection(string databaseFilename, out string errorMessage, bool corruptionProtection, bool doIntegrityCheck)
		{
			return this.OpenConnection(databaseFilename, GameVersion.DatabaseVersion, out errorMessage, true, corruptionProtection, doIntegrityCheck);
		}

		public bool OpenConnection(string databaseFilename, bool corruptionProtection, bool doIntegrityCheck)
		{
			string errorMessage;
			return this.OpenConnection(databaseFilename, GameVersion.DatabaseVersion, out errorMessage, true, corruptionProtection, doIntegrityCheck);
		}

		public bool OpenConnection(string databaseFilename, int databaseVersion, out string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck)
		{
			this.databaseFilename = databaseFilename;
			errorMessage = null;
			if (this.conn != null)
			{
				this.conn.Dispose();
			}
			if (databaseVersion != 1)
			{
				if (databaseVersion == 2)
				{
					this.conn = new SQLiteDbConnectionv2(this.logger);
				}
			}
			else
			{
				this.conn = new SQLiteDbConnectionv1(this.logger);
			}
			return this.conn.OpenOrCreate(databaseFilename, ref errorMessage, requireWriteAccess, corruptionProtection, doIntegrityCheck);
		}

		public void UpgradeToWriteAccess()
		{
			this.conn.UpgradeToWriteAccess();
		}

		public bool IntegrityCheck()
		{
			return this.conn.IntegrityCheck();
		}

		public SaveGame ProbeOpenConnection(string databaseFilename, bool corruptionProtection, out int foundVersion, out bool isReadonly, bool requireWrite = true)
		{
			string text;
			return this.ProbeOpenConnection(databaseFilename, corruptionProtection, out foundVersion, out text, out isReadonly, requireWrite);
		}

		public SaveGame ProbeOpenConnection(string databaseFilename, bool corruptionProtection, out int foundVersion, out string errorMessage, out bool isReadonly, bool requireWrite = true)
		{
			int version = GameVersion.DatabaseVersion;
			errorMessage = null;
			if (!File.Exists(databaseFilename))
			{
				this.OpenConnection(databaseFilename, version, out errorMessage, requireWrite, corruptionProtection, false);
				isReadonly = this.conn.IsReadOnly;
				foundVersion = version;
				return null;
			}
			foundVersion = 0;
			while (version > 0)
			{
				foundVersion = version;
				if (!this.OpenConnection(databaseFilename, version, out errorMessage, requireWrite, corruptionProtection, false))
				{
					isReadonly = this.conn.IsReadOnly;
					return null;
				}
				if (!this.conn.QuickCorrectSaveGameVersionTest())
				{
					version--;
				}
				else
				{
					SaveGame savegame = this.GetSaveGame();
					if (savegame != null)
					{
						isReadonly = this.conn.IsReadOnly;
						return savegame;
					}
					version--;
				}
			}
			isReadonly = false;
			return null;
		}

		public IEnumerable<DbChunk> GetAllChunks()
		{
			return this.conn.GetAllChunks();
		}

		public IEnumerable<DbChunk> GetAllMapChunks()
		{
			return this.conn.GetAllMapChunks();
		}

		public IEnumerable<DbChunk> GetAllMapRegions()
		{
			return this.conn.GetAllMapRegions();
		}

		public void ForAllChunks(Action<DbChunk> action)
		{
			this.conn.ForAllChunks(action);
		}

		public void Vacuum()
		{
			this.conn.Vacuum();
		}

		public bool ChunkExists(int x, int y, int z)
		{
			return this.conn.ChunkExists(ChunkPos.ToChunkIndex(x, y, z, 0));
		}

		public bool MapChunkExists(int x, int z)
		{
			return this.conn.MapChunkExists(ChunkPos.ToChunkIndex(x, 0, z));
		}

		public bool MapRegionExists(int x, int z)
		{
			return this.conn.MapRegionExists(ChunkPos.ToChunkIndex(x, 0, z));
		}

		public byte[] GetChunk(int x, int y, int z)
		{
			return this.conn.GetChunk(ChunkPos.ToChunkIndex(x, y, z, 0));
		}

		public byte[] GetChunk(int x, int y, int z, int dimension)
		{
			return this.conn.GetChunk(ChunkPos.ToChunkIndex(x, y, z, dimension));
		}

		public byte[] GetMapChunk(int x, int z)
		{
			return this.conn.GetMapChunk(ChunkPos.ToChunkIndex(x, 0, z));
		}

		public byte[] GetMapRegion(int x, int z)
		{
			return this.conn.GetMapRegion(ChunkPos.ToChunkIndex(x, 0, z));
		}

		public void SetChunks(IEnumerable<DbChunk> chunks)
		{
			this.conn.SetChunks(chunks);
		}

		public void SetMapChunks(IEnumerable<DbChunk> mapchunks)
		{
			this.conn.SetMapChunks(mapchunks);
		}

		public void SetMapRegions(IEnumerable<DbChunk> mapregions)
		{
			this.conn.SetMapRegions(mapregions);
		}

		public void DeleteChunks(IEnumerable<ChunkPos> coords)
		{
			this.conn.DeleteChunks(coords);
		}

		public void DeleteMapChunks(IEnumerable<ChunkPos> coords)
		{
			this.conn.DeleteMapChunks(coords);
		}

		public void DeleteMapRegions(IEnumerable<ChunkPos> coords)
		{
			this.conn.DeleteMapRegions(coords);
		}

		public byte[] GetPlayerData(string playeruid)
		{
			return this.conn.GetPlayerData(playeruid);
		}

		public void SetPlayerData(string playeruid, byte[] data)
		{
			this.conn.SetPlayerData(playeruid, data);
		}

		public void Dispose()
		{
			if (this.conn != null)
			{
				this.conn.Dispose();
			}
			this.conn = null;
		}

		public void CreateBackup(string backupFilename)
		{
			this.conn.CreateBackup(backupFilename);
		}

		public SaveGame GetSaveGame()
		{
			byte[] gamedata = this.conn.GetGameData();
			SaveGame savegame = null;
			if (gamedata != null)
			{
				try
				{
					savegame = Serializer.Deserialize<SaveGame>(new MemoryStream(gamedata));
				}
				catch (Exception e)
				{
					this.logger.Warning("Exception thrown on GetSaveGame: " + e.Message);
					return null;
				}
				return savegame;
			}
			return savegame;
		}

		public void StoreSaveGame(SaveGame savegame)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				Serializer.Serialize<SaveGame>(ms, savegame);
				this.conn.StoreGameData(ms.ToArray());
			}
		}

		public void StoreSaveGame(SaveGame savegame, FastMemoryStream ms)
		{
			this.conn.StoreGameData(SerializerUtil.Serialize<SaveGame>(savegame, ms));
		}

		internal void CloseConnection()
		{
			this.Dispose();
		}

		public static bool HaveWriteAccessFolder(string folderPath)
		{
			bool flag;
			try
			{
				string text = Path.Combine(folderPath, "temp.txt");
				File.Create(text).Close();
				File.Delete(text);
				flag = true;
			}
			catch (UnauthorizedAccessException)
			{
				flag = false;
			}
			return flag;
		}

		public static bool HaveWriteAccessFile(FileInfo file)
		{
			FileStream stream = null;
			try
			{
				stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				if (stream != null)
				{
					stream.Close();
				}
			}
			return true;
		}

		private IGameDbConnection conn;

		private ILogger logger;

		private string databaseFilename;
	}
}
