using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common.Database
{
	public class SQLiteDbConnectionv1 : IGameDbConnection, IDisposable
	{
		public SQLiteDbConnectionv1(ILogger logger)
		{
			this.logger = logger;
		}

		public bool OpenOrCreate(string filename, ref string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck)
		{
			try
			{
				this.databaseFileName = filename;
				bool newdatabase = !File.Exists(this.databaseFileName);
				DbConnectionStringBuilder conf = new DbConnectionStringBuilder
				{
					{ "Data Source", this.databaseFileName },
					{ "Pooling", "false" }
				};
				this.sqliteConn = new SqliteConnection(conf.ToString());
				this.sqliteConn.Open();
				using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
				{
					cmd.CommandText = "PRAGMA journal_mode=Off;";
					cmd.ExecuteNonQuery();
				}
				if (newdatabase)
				{
					this.CreateTables(this.sqliteConn);
				}
				if (doIntegrityCheck && !this.integrityCheck(this.sqliteConn))
				{
					this.logger.Error("Database is possibly corrupted.");
				}
			}
			catch (Exception e)
			{
				ILogger logger = this.logger;
				string text;
				errorMessage = (text = "Failed opening savegame.");
				logger.Error(text);
				this.logger.Error(e);
				return false;
			}
			return true;
		}

		public void Close()
		{
			this.sqliteConn.Close();
			this.sqliteConn.Dispose();
		}

		public void Dispose()
		{
			this.Close();
		}

		private void CreateTables(SqliteConnection sqliteConn)
		{
			using (SqliteCommand sqlite_cmd = sqliteConn.CreateCommand())
			{
				sqlite_cmd.CommandText = "CREATE TABLE chunks (position integer PRIMARY KEY, data BLOB);";
				sqlite_cmd.ExecuteNonQuery();
			}
		}

		public void CreateBackup(string backupFilename)
		{
			if (this.databaseFileName == backupFilename)
			{
				this.logger.Warning("Cannot overwrite current running database. Chose another destination.");
				return;
			}
			if (File.Exists(backupFilename))
			{
				this.logger.Notification("File " + backupFilename + " exists. Overwriting file.");
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

		private bool integrityCheck(SqliteConnection sqliteConn)
		{
			bool okay = false;
			bool flag;
			using (SqliteCommand command = sqliteConn.CreateCommand())
			{
				command.CommandText = "PRAGMA integrity_check";
				using (SqliteDataReader sqlite_datareader = command.ExecuteReader())
				{
					this.logger.Notification(string.Format("Database: {0}. Running SQLite integrity check...", sqliteConn.DataSource));
					while (sqlite_datareader.Read())
					{
						this.logger.Notification("Integrity check " + sqlite_datareader[0].ToString());
						if (sqlite_datareader[0].ToString() == "ok")
						{
							okay = true;
							break;
						}
					}
					flag = okay;
				}
			}
			return flag;
		}

		public int QuantityChunks()
		{
			int num;
			using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "SELECT count(*) FROM chunks";
				num = Convert.ToInt32(cmd.ExecuteScalar());
			}
			return num;
		}

		public IEnumerable<byte[]> GetChunks(IEnumerable<Vec3i> chunkpositions)
		{
			SQLiteDbConnectionv1.<GetChunks>d__13 <GetChunks>d__ = new SQLiteDbConnectionv1.<GetChunks>d__13(-2);
			<GetChunks>d__.<>4__this = this;
			<GetChunks>d__.<>3__chunkpositions = chunkpositions;
			return <GetChunks>d__;
		}

		public IEnumerable<DbChunk> GetAllChunks()
		{
			SQLiteDbConnectionv1.<GetAllChunks>d__14 <GetAllChunks>d__ = new SQLiteDbConnectionv1.<GetAllChunks>d__14(-2);
			<GetAllChunks>d__.<>4__this = this;
			return <GetAllChunks>d__;
		}

		public IEnumerable<DbChunk> GetAllMapChunks()
		{
			SQLiteDbConnectionv1.<GetAllMapChunks>d__15 <GetAllMapChunks>d__ = new SQLiteDbConnectionv1.<GetAllMapChunks>d__15(-2);
			<GetAllMapChunks>d__.<>4__this = this;
			return <GetAllMapChunks>d__;
		}

		public IEnumerable<DbChunk> GetAllMapRegions()
		{
			SQLiteDbConnectionv1.<GetAllMapRegions>d__16 <GetAllMapRegions>d__ = new SQLiteDbConnectionv1.<GetAllMapRegions>d__16(-2);
			<GetAllMapRegions>d__.<>4__this = this;
			return <GetAllMapRegions>d__;
		}

		public void ForAllChunks(Action<DbChunk> action)
		{
			throw new NotImplementedException();
		}

		public byte[] GetMapChunk(ulong position)
		{
			Vec3i vec = SQLiteDbConnectionv1.FromMapPos(position);
			position = SQLiteDbConnectionv1.ToMapPos(vec.X, SQLiteDbConnectionv1.MapChunkYCoord, vec.Z);
			return this.GetChunk(position);
		}

		public byte[] GetMapRegion(ulong position)
		{
			Vec3i vec = SQLiteDbConnectionv1.FromMapPos(position);
			position = SQLiteDbConnectionv1.ToMapPos(vec.X, SQLiteDbConnectionv1.MapRegionYCoord, vec.Z);
			return this.GetChunk(position);
		}

		public byte[] GetChunk(ulong position)
		{
			byte[] array;
			using (SqliteCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "SELECT data FROM chunks WHERE position=@position";
				cmd.Parameters.Add(this.CreateParameter("position", DbType.UInt64, position, cmd));
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

		public void DeleteMapChunks(IEnumerable<ChunkPos> coords)
		{
			using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
			{
				foreach (ChunkPos vec in coords)
				{
					this.DeleteChunk(SQLiteDbConnectionv1.ToMapPos(vec.X, SQLiteDbConnectionv1.MapChunkYCoord, vec.Z));
				}
				transaction.Commit();
			}
		}

		public void DeleteMapRegions(IEnumerable<ChunkPos> coords)
		{
			using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
			{
				foreach (ChunkPos vec in coords)
				{
					this.DeleteChunk(SQLiteDbConnectionv1.ToMapPos(vec.X, SQLiteDbConnectionv1.MapRegionYCoord, vec.Z));
				}
				transaction.Commit();
			}
		}

		public void DeleteChunks(IEnumerable<ChunkPos> coords)
		{
			using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
			{
				foreach (ChunkPos vec in coords)
				{
					this.DeleteChunk(SQLiteDbConnectionv1.ToMapPos(vec.X, vec.Y, vec.Z));
				}
				transaction.Commit();
			}
		}

		public void DeleteChunk(ulong position)
		{
			using (DbCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "DELETE FROM chunks WHERE position=@position";
				cmd.Parameters.Add(this.CreateParameter("position", DbType.UInt64, position, cmd));
				cmd.ExecuteNonQuery();
			}
		}

		public void SetChunks(IEnumerable<DbChunk> chunks)
		{
			using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
			{
				foreach (DbChunk c in chunks)
				{
					ulong pos = SQLiteDbConnectionv1.ToMapPos(c.Position.X, c.Position.Y, c.Position.Z);
					this.InsertChunk(pos, c.Data);
				}
				transaction.Commit();
			}
		}

		public void SetMapChunks(IEnumerable<DbChunk> mapchunks)
		{
			foreach (DbChunk dbChunk in mapchunks)
			{
				dbChunk.Position.Y = SQLiteDbConnectionv1.MapChunkYCoord;
			}
			this.SetChunks(mapchunks);
		}

		public void SetMapRegions(IEnumerable<DbChunk> mapregions)
		{
			foreach (DbChunk dbChunk in mapregions)
			{
				dbChunk.Position.Y = SQLiteDbConnectionv1.MapRegionYCoord;
			}
			this.SetChunks(mapregions);
		}

		private void InsertChunk(ulong position, byte[] data)
		{
			using (DbCommand cmd = this.sqliteConn.CreateCommand())
			{
				cmd.CommandText = "INSERT OR REPLACE INTO chunks (position, data) VALUES (@position,@data)";
				cmd.Parameters.Add(this.CreateParameter("position", DbType.UInt64, position, cmd));
				cmd.Parameters.Add(this.CreateParameter("data", DbType.Object, data, cmd));
				cmd.ExecuteNonQuery();
			}
		}

		private DbParameter CreateParameter(string parameterName, DbType dbType, object value, DbCommand command)
		{
			DbParameter dbParameter = command.CreateParameter();
			dbParameter.ParameterName = parameterName;
			dbParameter.DbType = dbType;
			dbParameter.Value = value;
			return dbParameter;
		}

		public byte[] GetGameData()
		{
			byte[] array;
			try
			{
				array = this.GetChunk(9223372036854775807UL);
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
			using (SqliteTransaction transaction = this.sqliteConn.BeginTransaction())
			{
				this.InsertChunk(9223372036854775807UL, data);
				transaction.Commit();
			}
		}

		public bool QuickCorrectSaveGameVersionTest()
		{
			return true;
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public static Vec3i FromMapPos(ulong v)
		{
			uint z = (uint)(v & SQLiteDbConnectionv1.pow20minus1);
			v >>= 20;
			uint y = (uint)(v & SQLiteDbConnectionv1.pow20minus1);
			v >>= 20;
			return new Vec3i((int)((uint)(v & SQLiteDbConnectionv1.pow20minus1)), (int)y, (int)z);
		}

		public static ulong ToMapPos(int x, int y, int z)
		{
			return (ulong)(((long)x << 40) | ((long)y << 20) | (long)((ulong)z));
		}

		public byte[] GetPlayerData(string playeruid)
		{
			throw new NotImplementedException();
		}

		public void SetPlayerData(string playeruid, byte[] data)
		{
			throw new NotImplementedException();
		}

		public void UpgradeToWriteAccess()
		{
		}

		public bool IntegrityCheck()
		{
			throw new NotImplementedException();
		}

		public bool ChunkExists(ulong position)
		{
			throw new NotImplementedException();
		}

		public bool MapChunkExists(ulong position)
		{
			throw new NotImplementedException();
		}

		public bool MapRegionExists(ulong position)
		{
			throw new NotImplementedException();
		}

		public void Vacuum()
		{
			using (SqliteCommand command = this.sqliteConn.CreateCommand())
			{
				command.CommandText = "VACUUM;";
				command.ExecuteNonQuery();
			}
		}

		private SqliteConnection sqliteConn;

		private string databaseFileName;

		public ILogger logger = new NullLogger();

		private static int MapChunkYCoord = 99998;

		private static int MapRegionYCoord = 99999;

		private static ulong pow20minus1 = 1048575UL;
	}
}
