using System;

public class Packet_ClientIdentification
{
	public void SetMdProtocolVersion(string value)
	{
		this.MdProtocolVersion = value;
	}

	public void SetPlayername(string value)
	{
		this.Playername = value;
	}

	public void SetMpToken(string value)
	{
		this.MpToken = value;
	}

	public void SetServerPassword(string value)
	{
		this.ServerPassword = value;
	}

	public void SetPlayerUID(string value)
	{
		this.PlayerUID = value;
	}

	public void SetViewDistance(int value)
	{
		this.ViewDistance = value;
	}

	public void SetRenderMetaBlocks(int value)
	{
		this.RenderMetaBlocks = value;
	}

	public void SetNetworkVersion(string value)
	{
		this.NetworkVersion = value;
	}

	public void SetShortGameVersion(string value)
	{
		this.ShortGameVersion = value;
	}

	internal void InitializeValues()
	{
	}

	public string MdProtocolVersion;

	public string Playername;

	public string MpToken;

	public string ServerPassword;

	public string PlayerUID;

	public int ViewDistance;

	public int RenderMetaBlocks;

	public string NetworkVersion;

	public string ShortGameVersion;

	public const int MdProtocolVersionFieldID = 1;

	public const int PlayernameFieldID = 2;

	public const int MpTokenFieldID = 3;

	public const int ServerPasswordFieldID = 4;

	public const int PlayerUIDFieldID = 6;

	public const int ViewDistanceFieldID = 7;

	public const int RenderMetaBlocksFieldID = 8;

	public const int NetworkVersionFieldID = 9;

	public const int ShortGameVersionFieldID = 10;

	public int size;
}
