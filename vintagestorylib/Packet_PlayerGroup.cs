using System;

public class Packet_PlayerGroup
{
	public void SetUid(int value)
	{
		this.Uid = value;
	}

	public void SetOwneruid(string value)
	{
		this.Owneruid = value;
	}

	public void SetName(string value)
	{
		this.Name = value;
	}

	public Packet_ChatLine[] GetChathistory()
	{
		return this.Chathistory;
	}

	public void SetChathistory(Packet_ChatLine[] value, int count, int length)
	{
		this.Chathistory = value;
		this.ChathistoryCount = count;
		this.ChathistoryLength = length;
	}

	public void SetChathistory(Packet_ChatLine[] value)
	{
		this.Chathistory = value;
		this.ChathistoryCount = value.Length;
		this.ChathistoryLength = value.Length;
	}

	public int GetChathistoryCount()
	{
		return this.ChathistoryCount;
	}

	public void ChathistoryAdd(Packet_ChatLine value)
	{
		if (this.ChathistoryCount >= this.ChathistoryLength)
		{
			if ((this.ChathistoryLength *= 2) == 0)
			{
				this.ChathistoryLength = 1;
			}
			Packet_ChatLine[] newArray = new Packet_ChatLine[this.ChathistoryLength];
			for (int i = 0; i < this.ChathistoryCount; i++)
			{
				newArray[i] = this.Chathistory[i];
			}
			this.Chathistory = newArray;
		}
		Packet_ChatLine[] chathistory = this.Chathistory;
		int chathistoryCount = this.ChathistoryCount;
		this.ChathistoryCount = chathistoryCount + 1;
		chathistory[chathistoryCount] = value;
	}

	public void SetCreatedbyprivatemessage(int value)
	{
		this.Createdbyprivatemessage = value;
	}

	public void SetMembership(int value)
	{
		this.Membership = value;
	}

	internal void InitializeValues()
	{
	}

	public int Uid;

	public string Owneruid;

	public string Name;

	public Packet_ChatLine[] Chathistory;

	public int ChathistoryCount;

	public int ChathistoryLength;

	public int Createdbyprivatemessage;

	public int Membership;

	public const int UidFieldID = 1;

	public const int OwneruidFieldID = 2;

	public const int NameFieldID = 3;

	public const int ChathistoryFieldID = 4;

	public const int CreatedbyprivatemessageFieldID = 5;

	public const int MembershipFieldID = 6;

	public int size;
}
