using System;

public class Packet_ServerQueryAnswer
{
	public void SetName(string value)
	{
		this.Name = value;
	}

	public void SetMOTD(string value)
	{
		this.MOTD = value;
	}

	public void SetPlayerCount(int value)
	{
		this.PlayerCount = value;
	}

	public void SetMaxPlayers(int value)
	{
		this.MaxPlayers = value;
	}

	public void SetGameMode(string value)
	{
		this.GameMode = value;
	}

	public void SetPassword(bool value)
	{
		this.Password = value;
	}

	public void SetServerVersion(string value)
	{
		this.ServerVersion = value;
	}

	internal void InitializeValues()
	{
	}

	public string Name;

	public string MOTD;

	public int PlayerCount;

	public int MaxPlayers;

	public string GameMode;

	public bool Password;

	public string ServerVersion;

	public const int NameFieldID = 1;

	public const int MOTDFieldID = 2;

	public const int PlayerCountFieldID = 3;

	public const int MaxPlayersFieldID = 4;

	public const int GameModeFieldID = 5;

	public const int PasswordFieldID = 6;

	public const int ServerVersionFieldID = 7;

	public int size;
}
