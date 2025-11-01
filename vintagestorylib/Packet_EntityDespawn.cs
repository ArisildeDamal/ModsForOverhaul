using System;

public class Packet_EntityDespawn
{
	public long[] GetEntityId()
	{
		return this.EntityId;
	}

	public void SetEntityId(long[] value, int count, int length)
	{
		this.EntityId = value;
		this.EntityIdCount = count;
		this.EntityIdLength = length;
	}

	public void SetEntityId(long[] value)
	{
		this.EntityId = value;
		this.EntityIdCount = value.Length;
		this.EntityIdLength = value.Length;
	}

	public int GetEntityIdCount()
	{
		return this.EntityIdCount;
	}

	public void EntityIdAdd(long value)
	{
		if (this.EntityIdCount >= this.EntityIdLength)
		{
			if ((this.EntityIdLength *= 2) == 0)
			{
				this.EntityIdLength = 1;
			}
			long[] newArray = new long[this.EntityIdLength];
			for (int i = 0; i < this.EntityIdCount; i++)
			{
				newArray[i] = this.EntityId[i];
			}
			this.EntityId = newArray;
		}
		long[] entityId = this.EntityId;
		int entityIdCount = this.EntityIdCount;
		this.EntityIdCount = entityIdCount + 1;
		entityId[entityIdCount] = value;
	}

	public int[] GetDespawnReason()
	{
		return this.DespawnReason;
	}

	public void SetDespawnReason(int[] value, int count, int length)
	{
		this.DespawnReason = value;
		this.DespawnReasonCount = count;
		this.DespawnReasonLength = length;
	}

	public void SetDespawnReason(int[] value)
	{
		this.DespawnReason = value;
		this.DespawnReasonCount = value.Length;
		this.DespawnReasonLength = value.Length;
	}

	public int GetDespawnReasonCount()
	{
		return this.DespawnReasonCount;
	}

	public void DespawnReasonAdd(int value)
	{
		if (this.DespawnReasonCount >= this.DespawnReasonLength)
		{
			if ((this.DespawnReasonLength *= 2) == 0)
			{
				this.DespawnReasonLength = 1;
			}
			int[] newArray = new int[this.DespawnReasonLength];
			for (int i = 0; i < this.DespawnReasonCount; i++)
			{
				newArray[i] = this.DespawnReason[i];
			}
			this.DespawnReason = newArray;
		}
		int[] despawnReason = this.DespawnReason;
		int despawnReasonCount = this.DespawnReasonCount;
		this.DespawnReasonCount = despawnReasonCount + 1;
		despawnReason[despawnReasonCount] = value;
	}

	public int[] GetDeathDamageSource()
	{
		return this.DeathDamageSource;
	}

	public void SetDeathDamageSource(int[] value, int count, int length)
	{
		this.DeathDamageSource = value;
		this.DeathDamageSourceCount = count;
		this.DeathDamageSourceLength = length;
	}

	public void SetDeathDamageSource(int[] value)
	{
		this.DeathDamageSource = value;
		this.DeathDamageSourceCount = value.Length;
		this.DeathDamageSourceLength = value.Length;
	}

	public int GetDeathDamageSourceCount()
	{
		return this.DeathDamageSourceCount;
	}

	public void DeathDamageSourceAdd(int value)
	{
		if (this.DeathDamageSourceCount >= this.DeathDamageSourceLength)
		{
			if ((this.DeathDamageSourceLength *= 2) == 0)
			{
				this.DeathDamageSourceLength = 1;
			}
			int[] newArray = new int[this.DeathDamageSourceLength];
			for (int i = 0; i < this.DeathDamageSourceCount; i++)
			{
				newArray[i] = this.DeathDamageSource[i];
			}
			this.DeathDamageSource = newArray;
		}
		int[] deathDamageSource = this.DeathDamageSource;
		int deathDamageSourceCount = this.DeathDamageSourceCount;
		this.DeathDamageSourceCount = deathDamageSourceCount + 1;
		deathDamageSource[deathDamageSourceCount] = value;
	}

	public long[] GetByEntityId()
	{
		return this.ByEntityId;
	}

	public void SetByEntityId(long[] value, int count, int length)
	{
		this.ByEntityId = value;
		this.ByEntityIdCount = count;
		this.ByEntityIdLength = length;
	}

	public void SetByEntityId(long[] value)
	{
		this.ByEntityId = value;
		this.ByEntityIdCount = value.Length;
		this.ByEntityIdLength = value.Length;
	}

	public int GetByEntityIdCount()
	{
		return this.ByEntityIdCount;
	}

	public void ByEntityIdAdd(long value)
	{
		if (this.ByEntityIdCount >= this.ByEntityIdLength)
		{
			if ((this.ByEntityIdLength *= 2) == 0)
			{
				this.ByEntityIdLength = 1;
			}
			long[] newArray = new long[this.ByEntityIdLength];
			for (int i = 0; i < this.ByEntityIdCount; i++)
			{
				newArray[i] = this.ByEntityId[i];
			}
			this.ByEntityId = newArray;
		}
		long[] byEntityId = this.ByEntityId;
		int byEntityIdCount = this.ByEntityIdCount;
		this.ByEntityIdCount = byEntityIdCount + 1;
		byEntityId[byEntityIdCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public long[] EntityId;

	public int EntityIdCount;

	public int EntityIdLength;

	public int[] DespawnReason;

	public int DespawnReasonCount;

	public int DespawnReasonLength;

	public int[] DeathDamageSource;

	public int DeathDamageSourceCount;

	public int DeathDamageSourceLength;

	public long[] ByEntityId;

	public int ByEntityIdCount;

	public int ByEntityIdLength;

	public const int EntityIdFieldID = 1;

	public const int DespawnReasonFieldID = 2;

	public const int DeathDamageSourceFieldID = 3;

	public const int ByEntityIdFieldID = 4;

	public int size;
}
