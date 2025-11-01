using System;

public class Packet_NetworkChannels
{
	public int[] GetChannelIds()
	{
		return this.ChannelIds;
	}

	public void SetChannelIds(int[] value, int count, int length)
	{
		this.ChannelIds = value;
		this.ChannelIdsCount = count;
		this.ChannelIdsLength = length;
	}

	public void SetChannelIds(int[] value)
	{
		this.ChannelIds = value;
		this.ChannelIdsCount = value.Length;
		this.ChannelIdsLength = value.Length;
	}

	public int GetChannelIdsCount()
	{
		return this.ChannelIdsCount;
	}

	public void ChannelIdsAdd(int value)
	{
		if (this.ChannelIdsCount >= this.ChannelIdsLength)
		{
			if ((this.ChannelIdsLength *= 2) == 0)
			{
				this.ChannelIdsLength = 1;
			}
			int[] newArray = new int[this.ChannelIdsLength];
			for (int i = 0; i < this.ChannelIdsCount; i++)
			{
				newArray[i] = this.ChannelIds[i];
			}
			this.ChannelIds = newArray;
		}
		int[] channelIds = this.ChannelIds;
		int channelIdsCount = this.ChannelIdsCount;
		this.ChannelIdsCount = channelIdsCount + 1;
		channelIds[channelIdsCount] = value;
	}

	public string[] GetChannelNames()
	{
		return this.ChannelNames;
	}

	public void SetChannelNames(string[] value, int count, int length)
	{
		this.ChannelNames = value;
		this.ChannelNamesCount = count;
		this.ChannelNamesLength = length;
	}

	public void SetChannelNames(string[] value)
	{
		this.ChannelNames = value;
		this.ChannelNamesCount = value.Length;
		this.ChannelNamesLength = value.Length;
	}

	public int GetChannelNamesCount()
	{
		return this.ChannelNamesCount;
	}

	public void ChannelNamesAdd(string value)
	{
		if (this.ChannelNamesCount >= this.ChannelNamesLength)
		{
			if ((this.ChannelNamesLength *= 2) == 0)
			{
				this.ChannelNamesLength = 1;
			}
			string[] newArray = new string[this.ChannelNamesLength];
			for (int i = 0; i < this.ChannelNamesCount; i++)
			{
				newArray[i] = this.ChannelNames[i];
			}
			this.ChannelNames = newArray;
		}
		string[] channelNames = this.ChannelNames;
		int channelNamesCount = this.ChannelNamesCount;
		this.ChannelNamesCount = channelNamesCount + 1;
		channelNames[channelNamesCount] = value;
	}

	public int[] GetChannelUdpIds()
	{
		return this.ChannelUdpIds;
	}

	public void SetChannelUdpIds(int[] value, int count, int length)
	{
		this.ChannelUdpIds = value;
		this.ChannelUdpIdsCount = count;
		this.ChannelUdpIdsLength = length;
	}

	public void SetChannelUdpIds(int[] value)
	{
		this.ChannelUdpIds = value;
		this.ChannelUdpIdsCount = value.Length;
		this.ChannelUdpIdsLength = value.Length;
	}

	public int GetChannelUdpIdsCount()
	{
		return this.ChannelUdpIdsCount;
	}

	public void ChannelUdpIdsAdd(int value)
	{
		if (this.ChannelUdpIdsCount >= this.ChannelUdpIdsLength)
		{
			if ((this.ChannelUdpIdsLength *= 2) == 0)
			{
				this.ChannelUdpIdsLength = 1;
			}
			int[] newArray = new int[this.ChannelUdpIdsLength];
			for (int i = 0; i < this.ChannelUdpIdsCount; i++)
			{
				newArray[i] = this.ChannelUdpIds[i];
			}
			this.ChannelUdpIds = newArray;
		}
		int[] channelUdpIds = this.ChannelUdpIds;
		int channelUdpIdsCount = this.ChannelUdpIdsCount;
		this.ChannelUdpIdsCount = channelUdpIdsCount + 1;
		channelUdpIds[channelUdpIdsCount] = value;
	}

	public string[] GetChannelUdpNames()
	{
		return this.ChannelUdpNames;
	}

	public void SetChannelUdpNames(string[] value, int count, int length)
	{
		this.ChannelUdpNames = value;
		this.ChannelUdpNamesCount = count;
		this.ChannelUdpNamesLength = length;
	}

	public void SetChannelUdpNames(string[] value)
	{
		this.ChannelUdpNames = value;
		this.ChannelUdpNamesCount = value.Length;
		this.ChannelUdpNamesLength = value.Length;
	}

	public int GetChannelUdpNamesCount()
	{
		return this.ChannelUdpNamesCount;
	}

	public void ChannelUdpNamesAdd(string value)
	{
		if (this.ChannelUdpNamesCount >= this.ChannelUdpNamesLength)
		{
			if ((this.ChannelUdpNamesLength *= 2) == 0)
			{
				this.ChannelUdpNamesLength = 1;
			}
			string[] newArray = new string[this.ChannelUdpNamesLength];
			for (int i = 0; i < this.ChannelUdpNamesCount; i++)
			{
				newArray[i] = this.ChannelUdpNames[i];
			}
			this.ChannelUdpNames = newArray;
		}
		string[] channelUdpNames = this.ChannelUdpNames;
		int channelUdpNamesCount = this.ChannelUdpNamesCount;
		this.ChannelUdpNamesCount = channelUdpNamesCount + 1;
		channelUdpNames[channelUdpNamesCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public int[] ChannelIds;

	public int ChannelIdsCount;

	public int ChannelIdsLength;

	public string[] ChannelNames;

	public int ChannelNamesCount;

	public int ChannelNamesLength;

	public int[] ChannelUdpIds;

	public int ChannelUdpIdsCount;

	public int ChannelUdpIdsLength;

	public string[] ChannelUdpNames;

	public int ChannelUdpNamesCount;

	public int ChannelUdpNamesLength;

	public const int ChannelIdsFieldID = 1;

	public const int ChannelNamesFieldID = 2;

	public const int ChannelUdpIdsFieldID = 3;

	public const int ChannelUdpNamesFieldID = 4;

	public int size;
}
