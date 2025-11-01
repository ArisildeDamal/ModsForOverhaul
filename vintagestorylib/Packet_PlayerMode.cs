using System;

public class Packet_PlayerMode
{
	public void SetPlayerUID(string value)
	{
		this.PlayerUID = value;
	}

	public void SetGameMode(int value)
	{
		this.GameMode = value;
	}

	public void SetMoveSpeed(int value)
	{
		this.MoveSpeed = value;
	}

	public void SetFreeMove(int value)
	{
		this.FreeMove = value;
	}

	public void SetNoClip(int value)
	{
		this.NoClip = value;
	}

	public void SetViewDistance(int value)
	{
		this.ViewDistance = value;
	}

	public void SetPickingRange(int value)
	{
		this.PickingRange = value;
	}

	public void SetFreeMovePlaneLock(int value)
	{
		this.FreeMovePlaneLock = value;
	}

	public void SetImmersiveFpMode(int value)
	{
		this.ImmersiveFpMode = value;
	}

	public void SetRenderMetaBlocks(int value)
	{
		this.RenderMetaBlocks = value;
	}

	internal void InitializeValues()
	{
	}

	public string PlayerUID;

	public int GameMode;

	public int MoveSpeed;

	public int FreeMove;

	public int NoClip;

	public int ViewDistance;

	public int PickingRange;

	public int FreeMovePlaneLock;

	public int ImmersiveFpMode;

	public int RenderMetaBlocks;

	public const int PlayerUIDFieldID = 1;

	public const int GameModeFieldID = 2;

	public const int MoveSpeedFieldID = 3;

	public const int FreeMoveFieldID = 4;

	public const int NoClipFieldID = 5;

	public const int ViewDistanceFieldID = 6;

	public const int PickingRangeFieldID = 7;

	public const int FreeMovePlaneLockFieldID = 8;

	public const int ImmersiveFpModeFieldID = 9;

	public const int RenderMetaBlocksFieldID = 10;

	public int size;
}
