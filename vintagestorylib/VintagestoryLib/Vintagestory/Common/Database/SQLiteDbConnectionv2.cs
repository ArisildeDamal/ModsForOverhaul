using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Common.Database
{
	public class SQLiteDbConnectionv2 : SQLiteDBConnection, IGameDbConnection, IDisposable
	{
		public override string DBTypeCode
		{
			get
			{
				return "savegame database";
			}
		}

		public SQLiteDbConnectionv2(ILogger logger)
			: base(logger)
		{
			this.logger = logger;
		}

		public override void OnOpened()
		{
			this.setChunksCmd = this.sqliteConn.CreateCommand();
			this.setChunksCmd.CommandText = "INSERT OR REPLACE INTO chunk (position, data) VALUES (@position,@data)";
			this.setChunksCmd.Parameters.Add(base.CreateParameter("position", DbType.UInt64, 0, this.setChunksCmd));
			this.setChunksCmd.Parameters.Add(base.CreateParameter("data", DbType.Object, null, this.setChunksCmd));
			this.setChunksCmd.Prepare();
			this.setMapChunksCmd = this.sqliteConn.CreateCommand();
			this.setMapChunksCmd.CommandText = "INSERT OR REPLACE INTO mapchunk (position, data) VALUES (@position,@data)";
			this.setMapChunksCmd.Parameters.Add(base.CreateParameter("position", DbType.UInt64, 0, this.setMapChunksCmd));
			this.setMapChunksCmd.Parameters.Add(base.CreateParameter("data", DbType.Object, null, this.setMapChunksCmd));
			this.setMapChunksCmd.Prepare();
		}

		public void UpgradeToWriteAccess()
		{
			this.CreateTablesIfNotExists(this.sqliteConn);
		}

		public bool IntegrityCheck()
		{
			if (!base.DoIntegrityCheck(this.sqliteConn, true))
			{
				string msg = "Database integrity check failed. Attempt basic repair procedure (via VACUUM), this might take minutes to hours depending on the size of the save game...";
				this.logger.Notification(msg);
				this.logger.StoryEvent(msg);
				try
				{
					using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
					{
						cmd.CommandText = "PRAGMA writable_schema=ON;";
						cmd.ExecuteNonQuery();
					}
					using (SqliteCommand cmd2 = this.sqliteConn.CreateCommand())
					{
						cmd2.CommandText = "VACUUM;";
						cmd2.ExecuteNonQuery();
					}
				}
				catch
				{
					this.logger.StoryEvent("Unable to repair :(");
					this.logger.Notification("Unable to repair :(\nRecommend any of the solutions posted here: https://wiki.vintagestory.at/index.php/Repairing_a_corrupt_savegame_or_worldmap\nWill exit now");
					throw new Exception("Database integrity bad");
				}
				if (!base.DoIntegrityCheck(this.sqliteConn, false))
				{
					this.logger.StoryEvent("Unable to repair :(");
					this.logger.Notification("Database integrity still bad :(\nRecommend any of the solutions posted here: https://wiki.vintagestory.at/index.php/Repairing_a_corrupt_savegame_or_worldmap\nWill exit now");
					throw new Exception("Database integrity bad");
				}
				this.logger.Notification("Database integrity check now okay, yay!");
			}
			return true;
		}

		public int QuantityChunks()
		{
			int num;
			using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "SELECT count(*) FROM chunk";
				num = Convert.ToInt32(cmd.ExecuteScalar());
			}
			return num;
		}

		public IEnumerable<DbChunk> GetAllChunks(string tablename)
		{
			SQLiteDbConnectionv2.<GetAllChunks>d__9 <GetAllChunks>d__ = new SQLiteDbConnectionv2.<GetAllChunks>d__9(-2);
			<GetAllChunks>d__.<>4__this = this;
			<GetAllChunks>d__.<>3__tablename = tablename;
			return <GetAllChunks>d__;
		}

		public IEnumerable<DbChunk> GetAllChunks()
		{
			return this.GetAllChunks("chunk");
		}

		public IEnumerable<DbChunk> GetAllMapChunks()
		{
			return this.GetAllChunks("mapchunk");
		}

		public IEnumerable<DbChunk> GetAllMapRegions()
		{
			return this.GetAllChunks("mapregion");
		}

		public void ForAllChunks(Action<DbChunk> action)
		{
			using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "SELECT position, data FROM chunk";
				using (SqliteDataReader sqlite_datareader = cmd.ExecuteReader())
				{
					while (sqlite_datareader.Read())
					{
						object data = sqlite_datareader["data"];
						ChunkPos pos = ChunkPos.FromChunkIndex_saveGamev2((ulong)((long)sqlite_datareader["position"]));
						action(new DbChunk
						{
							Position = pos,
							Data = (data as byte[])
						});
					}
				}
			}
		}

		public byte[] GetPlayerData(string playeruid)
		{
			byte[] array;
			using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "SELECT data FROM playerdata WHERE playeruid=@playeruid";
				cmd.Parameters.Add(base.CreateParameter("playeruid", DbType.String, playeruid, cmd));
				using (SqliteDataReader dataReader = cmd.ExecuteReader())
				{
					if (dataReader.Read())
					{
						array = dataReader["data"] as byte[];
					}
					else
					{
						array = null;
					}
				}
			}
			return array;
		}

		public void SetPlayerData(string playeruid, byte[] data)
		{
			if (data == null)
			{
				using (DbCommand deleteCmd = this.sqliteConn.CreateCommand())
				{
					deleteCmd.CommandText = "DELETE FROM playerdata WHERE playeruid=@playeruid";
					deleteCmd.Parameters.Add(base.CreateParameter("playeruid", DbType.String, playeruid, deleteCmd));
					deleteCmd.ExecuteNonQuery();
					return;
				}
			}
			if (this.GetPlayerData(playeruid) == null)
			{
				using (DbCommand insertCmd = this.sqliteConn.CreateCommand())
				{
					insertCmd.CommandText = "INSERT INTO playerdata (playeruid, data) VALUES (@playeruid,@data)";
					insertCmd.Parameters.Add(base.CreateParameter("playeruid", DbType.String, playeruid, insertCmd));
					insertCmd.Parameters.Add(base.CreateParameter("data", DbType.Object, data, insertCmd));
					insertCmd.ExecuteNonQuery();
					return;
				}
			}
			using (DbCommand updateCmd = this.sqliteConn.CreateCommand())
			{
				updateCmd.CommandText = "UPDATE playerdata set data=@data where playeruid=@playeruid";
				updateCmd.Parameters.Add(base.CreateParameter("data", DbType.Object, data, updateCmd));
				updateCmd.Parameters.Add(base.CreateParameter("playeruid", DbType.String, playeruid, updateCmd));
				updateCmd.ExecuteNonQuery();
			}
		}

		public IEnumerable<byte[]> GetChunks(IEnumerable<ChunkPos> chunkpositions)
		{
			SQLiteDbConnectionv2.<GetChunks>d__16 <GetChunks>d__ = new SQLiteDbConnectionv2.<GetChunks>d__16(-2);
			<GetChunks>d__.<>4__this = this;
			<GetChunks>d__.<>3__chunkpositions = chunkpositions;
			return <GetChunks>d__;
		}

		public byte[] GetChunk(ulong position)
		{
			return this.GetChunk(position, "chunk");
		}

		public byte[] GetMapChunk(ulong position)
		{
			return this.GetChunk(position, "mapchunk");
		}

		public byte[] GetMapRegion(ulong position)
		{
			return this.GetChunk(position, "mapregion");
		}

		public bool ChunkExists(ulong position)
		{
			return this.ChunkExists(position, "chunk");
		}

		public bool MapChunkExists(ulong position)
		{
			return this.ChunkExists(position, "mapchunk");
		}

		public bool MapRegionExists(ulong position)
		{
			return this.ChunkExists(position, "mapregion");
		}

		public bool ChunkExists(ulong position, string tablename)
		{
			bool hasRows;
			using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "SELECT position FROM " + tablename + " WHERE position=@position";
				cmd.Parameters.Add(base.CreateParameter("position", DbType.UInt64, position, cmd));
				using (SqliteDataReader dataReader = cmd.ExecuteReader())
				{
					hasRows = dataReader.HasRows;
				}
			}
			return hasRows;
		}

		public byte[] GetChunk(ulong position, string tablename)
		{
			byte[] array;
			using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "SELECT data FROM " + tablename + " WHERE position=@position";
				cmd.Parameters.Add(base.CreateParameter("position", DbType.UInt64, position, cmd));
				using (SqliteDataReader dataReader = cmd.ExecuteReader())
				{
					if (dataReader.Read())
					{
						array = dataReader["data"] as byte[];
					}
					else
					{
						array = null;
					}
				}
			}
			return array;
		}

		public void DeleteChunks(IEnumerable<ChunkPos> chunkpositions)
		{
			this.DeleteChunks(chunkpositions, "chunk");
		}

		public void DeleteMapChunks(IEnumerable<ChunkPos> mapchunkpositions)
		{
			this.DeleteChunks(mapchunkpositions, "mapchunk");
		}

		public void DeleteMapRegions(IEnumerable<ChunkPos> mapchunkregions)
		{
			this.DeleteChunks(mapchunkregions, "mapregion");
		}

		public void DeleteChunks(IEnumerable<ChunkPos> chunkpositions, string tablename)
		{
			object transactionLock = this.transactionLock;
			lock (transactionLock)
			{
				using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
				{
					foreach (ChunkPos vec in chunkpositions)
					{
						this.DeleteChunk(vec.ToChunkIndex(), tablename);
					}
					transaction.Commit();
				}
			}
		}

		public void DeleteChunk(ulong position, string tablename)
		{
			using (DbCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "DELETE FROM " + tablename + " WHERE position=@position";
				cmd.Parameters.Add(base.CreateParameter("position", DbType.UInt64, position, cmd));
				cmd.ExecuteNonQuery();
			}
		}

		public void SetChunks(IEnumerable<DbChunk> chunks)
		{
			object transactionLock = this.transactionLock;
			lock (transactionLock)
			{
				using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
				{
					this.setChunksCmd.Transaction = transaction;
					foreach (DbChunk c in chunks)
					{
						this.setChunksCmd.Parameters["position"].Value = c.Position.ToChunkIndex();
						this.setChunksCmd.Parameters["data"].Value = c.Data;
						this.setChunksCmd.ExecuteNonQuery();
					}
					transaction.Commit();
				}
			}
		}

		public void SetMapChunks(IEnumerable<DbChunk> mapchunks)
		{
			object transactionLock = this.transactionLock;
			lock (transactionLock)
			{
				using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
				{
					this.setMapChunksCmd.Transaction = transaction;
					foreach (DbChunk c in mapchunks)
					{
						c.Position.Y = 0;
						this.setMapChunksCmd.Parameters["position"].Value = c.Position.ToChunkIndex();
						this.setMapChunksCmd.Parameters["data"].Value = c.Data;
						this.setMapChunksCmd.ExecuteNonQuery();
					}
					transaction.Commit();
				}
			}
		}

		public void SetMapRegions(IEnumerable<DbChunk> mapregions)
		{
			object transactionLock = this.transactionLock;
			lock (transactionLock)
			{
				using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
				{
					foreach (DbChunk c in mapregions)
					{
						c.Position.Y = 0;
						this.InsertChunk(c.Position.ToChunkIndex(), c.Data, "mapregion");
					}
					transaction.Commit();
				}
			}
		}

		private void InsertChunk(ulong position, byte[] data, string tablename)
		{
			using (DbCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "INSERT OR REPLACE INTO " + tablename + " (position, data) VALUES (@position,@data)";
				cmd.Parameters.Add(base.CreateParameter("position", DbType.UInt64, position, cmd));
				cmd.Parameters.Add(base.CreateParameter("data", DbType.Object, data, cmd));
				cmd.ExecuteNonQuery();
			}
		}

		public byte[] GetGameData()
		{
			byte[] array;
			try
			{
				using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
				{
					cmd.CommandText = "SELECT data FROM gamedata LIMIT 1";
					using (SqliteDataReader sqlite_datareader = cmd.ExecuteReader())
					{
						if (!sqlite_datareader.Read())
						{
							array = null;
						}
						else
						{
							array = sqlite_datareader["data"] as byte[];
						}
					}
				}
			}
			catch (Exception e)
			{
				this.logger.Warning("Exception thrown on GetGlobalData: " + e.Message);
				array = null;
			}
			return array;
		}

		public void StoreGameData(byte[] data)
		{
			object transactionLock = this.transactionLock;
			lock (transactionLock)
			{
				using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
				{
					using (DbCommand cmd = this.sqliteConn.CreateCommand())
					{
						cmd.CommandText = "INSERT OR REPLACE INTO gamedata (savegameid, data) VALUES (@savegameid,@data)";
						cmd.Parameters.Add(base.CreateParameter("savegameid", DbType.UInt64, 1, cmd));
						cmd.Parameters.Add(base.CreateParameter("data", DbType.Object, data, cmd));
						cmd.ExecuteNonQuery();
					}
					transaction.Commit();
				}
			}
		}

		public bool QuickCorrectSaveGameVersionTest()
		{
			bool flag;
			using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'gamedata';";
				flag = cmd.ExecuteScalar() != null;
			}
			return flag;
		}

		protected override void CreateTablesIfNotExists(SqliteConnection sqliteConn)
		{
			using (SqliteCommand sqlite_cmd = sqliteConn.CreateCommand())
			{
				sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS chunk (position integer PRIMARY KEY, data BLOB);";
				sqlite_cmd.ExecuteNonQuery();
			}
			using (SqliteCommand sqlite_cmd2 = sqliteConn.CreateCommand())
			{
				sqlite_cmd2.CommandText = "CREATE TABLE IF NOT EXISTS mapchunk (position integer PRIMARY KEY, data BLOB);";
				sqlite_cmd2.ExecuteNonQuery();
			}
			using (SqliteCommand sqlite_cmd3 = sqliteConn.CreateCommand())
			{
				sqlite_cmd3.CommandText = "CREATE TABLE IF NOT EXISTS mapregion (position integer PRIMARY KEY, data BLOB);";
				sqlite_cmd3.ExecuteNonQuery();
			}
			using (SqliteCommand sqlite_cmd4 = sqliteConn.CreateCommand())
			{
				sqlite_cmd4.CommandText = "CREATE TABLE IF NOT EXISTS gamedata (savegameid integer PRIMARY KEY, data BLOB);";
				sqlite_cmd4.ExecuteNonQuery();
			}
			using (SqliteCommand sqlite_cmd5 = sqliteConn.CreateCommand())
			{
				sqlite_cmd5.CommandText = "CREATE TABLE IF NOT EXISTS playerdata (playerid integer PRIMARY KEY AUTOINCREMENT, playeruid TEXT, data BLOB);";
				sqlite_cmd5.ExecuteNonQuery();
			}
			using (SqliteCommand sqlite_cmd6 = sqliteConn.CreateCommand())
			{
				sqlite_cmd6.CommandText = "CREATE index IF NOT EXISTS index_playeruid on playerdata(playeruid);";
				sqlite_cmd6.ExecuteNonQuery();
			}
		}

		public void CreateBackup(string backupFilename)
		{
			if (this.databaseFileName == backupFilename)
			{
				this.logger.Error("Cannot overwrite current running database. Chose another destination.");
				return;
			}
			if (File.Exists(backupFilename))
			{
				this.logger.Error("File " + backupFilename + " exists. Overwriting file.");
			}
			SqliteConnection sqliteBckConn = new SqliteConnection(new DbConnectionStringBuilder
			{
				{
					"Data Source",
					Path.Combine(GamePaths.Backups, backupFilename)
				},
				{ "Pooling", "false" }
			}.ToString());
			sqliteBckConn.Open();
			using (SqliteCommand configCmd = sqliteBckConn.CreateCommand())
			{
				configCmd.CommandText = "PRAGMA journal_mode=Off;";
				configCmd.ExecuteNonQuery();
			}
			this.sqliteConn.BackupDatabase(sqliteBckConn, sqliteBckConn.Database, this.sqliteConn.Database);
			sqliteBckConn.Close();
			sqliteBckConn.Dispose();
		}

		public override void Close()
		{
			SqliteCommand sqliteCommand = this.setChunksCmd;
			if (sqliteCommand != null)
			{
				sqliteCommand.Dispose();
			}
			SqliteCommand sqliteCommand2 = this.setMapChunksCmd;
			if (sqliteCommand2 != null)
			{
				sqliteCommand2.Dispose();
			}
			base.Close();
		}

		public override void Dispose()
		{
			SqliteCommand sqliteCommand = this.setChunksCmd;
			if (sqliteCommand != null)
			{
				sqliteCommand.Dispose();
			}
			SqliteCommand sqliteCommand2 = this.setMapChunksCmd;
			if (sqliteCommand2 != null)
			{
				sqliteCommand2.Dispose();
			}
			base.Dispose();
		}

		bool IGameDbConnection.get_IsReadOnly()
		{
			return base.IsReadOnly;
		}

		bool IGameDbConnection.OpenOrCreate(string filename, ref string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck)
		{
			return base.OpenOrCreate(filename, ref errorMessage, requireWriteAccess, corruptionProtection, doIntegrityCheck);
		}

		void IGameDbConnection.Vacuum()
		{
			base.Vacuum();
		}

		private SqliteCommand setChunksCmd;

		private SqliteCommand setMapChunksCmd;
	}
}
