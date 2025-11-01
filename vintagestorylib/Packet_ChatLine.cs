using System;

public class Packet_ChatLine
{
	public void SetMessage(string value)
	{
		this.Message = value;
	}

	public void SetGroupid(int value)
	{
		this.Groupid = value;
	}

	public void SetChatType(int value)
	{
		this.ChatType = value;
	}

	public void SetData(string value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public string Message;

	public int Groupid;

	public int ChatType;

	public string Data;

	public const int MessageFieldID = 1;

	public const int GroupidFieldID = 2;

	public const int ChatTypeFieldID = 3;

	public const int DataFieldID = 4;

	public int size;
}
