using System;

public class Packet_CustomPacket
{
	public void SetChannelId(int value)
	{
		this.ChannelId = value;
	}

	public void SetMessageId(int value)
	{
		this.MessageId = value;
	}

	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public int ChannelId;

	public int MessageId;

	public byte[] Data;

	public const int ChannelIdFieldID = 1;

	public const int MessageIdFieldID = 2;

	public const int DataFieldID = 3;

	public int size;
}
