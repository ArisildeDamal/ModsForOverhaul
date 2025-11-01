using System;

public class Packet_PlayerGroups
{
	public Packet_PlayerGroup[] GetGroups()
	{
		return this.Groups;
	}

	public void SetGroups(Packet_PlayerGroup[] value, int count, int length)
	{
		this.Groups = value;
		this.GroupsCount = count;
		this.GroupsLength = length;
	}

	public void SetGroups(Packet_PlayerGroup[] value)
	{
		this.Groups = value;
		this.GroupsCount = value.Length;
		this.GroupsLength = value.Length;
	}

	public int GetGroupsCount()
	{
		return this.GroupsCount;
	}

	public void GroupsAdd(Packet_PlayerGroup value)
	{
		if (this.GroupsCount >= this.GroupsLength)
		{
			if ((this.GroupsLength *= 2) == 0)
			{
				this.GroupsLength = 1;
			}
			Packet_PlayerGroup[] newArray = new Packet_PlayerGroup[this.GroupsLength];
			for (int i = 0; i < this.GroupsCount; i++)
			{
				newArray[i] = this.Groups[i];
			}
			this.Groups = newArray;
		}
		Packet_PlayerGroup[] groups = this.Groups;
		int groupsCount = this.GroupsCount;
		this.GroupsCount = groupsCount + 1;
		groups[groupsCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_PlayerGroup[] Groups;

	public int GroupsCount;

	public int GroupsLength;

	public const int GroupsFieldID = 1;

	public int size;
}
