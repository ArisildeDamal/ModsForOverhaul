using System;

public class Packet_EntityAttributeUpdate
{
	public void SetEntityId(long value)
	{
		this.EntityId = value;
	}

	public Packet_PartialAttribute[] GetAttributes()
	{
		return this.Attributes;
	}

	public void SetAttributes(Packet_PartialAttribute[] value, int count, int length)
	{
		this.Attributes = value;
		this.AttributesCount = count;
		this.AttributesLength = length;
	}

	public void SetAttributes(Packet_PartialAttribute[] value)
	{
		this.Attributes = value;
		this.AttributesCount = value.Length;
		this.AttributesLength = value.Length;
	}

	public int GetAttributesCount()
	{
		return this.AttributesCount;
	}

	public void AttributesAdd(Packet_PartialAttribute value)
	{
		if (this.AttributesCount >= this.AttributesLength)
		{
			if ((this.AttributesLength *= 2) == 0)
			{
				this.AttributesLength = 1;
			}
			Packet_PartialAttribute[] newArray = new Packet_PartialAttribute[this.AttributesLength];
			for (int i = 0; i < this.AttributesCount; i++)
			{
				newArray[i] = this.Attributes[i];
			}
			this.Attributes = newArray;
		}
		Packet_PartialAttribute[] attributes = this.Attributes;
		int attributesCount = this.AttributesCount;
		this.AttributesCount = attributesCount + 1;
		attributes[attributesCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public long EntityId;

	public Packet_PartialAttribute[] Attributes;

	public int AttributesCount;

	public int AttributesLength;

	public const int EntityIdFieldID = 1;

	public const int AttributesFieldID = 2;

	public int size;
}
