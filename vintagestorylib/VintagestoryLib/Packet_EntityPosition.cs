using System;

public class Packet_EntityPosition
{
	public void SetEntityId(long value)
	{
		this.EntityId = value;
	}

	public void SetX(long value)
	{
		this.X = value;
	}

	public void SetY(long value)
	{
		this.Y = value;
	}

	public void SetZ(long value)
	{
		this.Z = value;
	}

	public void SetYaw(int value)
	{
		this.Yaw = value;
	}

	public void SetPitch(int value)
	{
		this.Pitch = value;
	}

	public void SetRoll(int value)
	{
		this.Roll = value;
	}

	public void SetHeadYaw(int value)
	{
		this.HeadYaw = value;
	}

	public void SetHeadPitch(int value)
	{
		this.HeadPitch = value;
	}

	public void SetBodyYaw(int value)
	{
		this.BodyYaw = value;
	}

	public void SetControls(int value)
	{
		this.Controls = value;
	}

	public void SetTick(int value)
	{
		this.Tick = value;
	}

	public void SetPositionVersion(int value)
	{
		this.PositionVersion = value;
	}

	public void SetMotionX(long value)
	{
		this.MotionX = value;
	}

	public void SetMotionY(long value)
	{
		this.MotionY = value;
	}

	public void SetMotionZ(long value)
	{
		this.MotionZ = value;
	}

	public void SetTeleport(bool value)
	{
		this.Teleport = value;
	}

	public void SetTagsBitmask1(long value)
	{
		this.TagsBitmask1 = value;
	}

	public void SetTagsBitmask2(long value)
	{
		this.TagsBitmask2 = value;
	}

	public void SetMountControls(int value)
	{
		this.MountControls = value;
	}

	internal void InitializeValues()
	{
	}

	public long EntityId;

	public long X;

	public long Y;

	public long Z;

	public int Yaw;

	public int Pitch;

	public int Roll;

	public int HeadYaw;

	public int HeadPitch;

	public int BodyYaw;

	public int Controls;

	public int Tick;

	public int PositionVersion;

	public long MotionX;

	public long MotionY;

	public long MotionZ;

	public bool Teleport;

	public long TagsBitmask1;

	public long TagsBitmask2;

	public int MountControls;

	public const int EntityIdFieldID = 1;

	public const int XFieldID = 2;

	public const int YFieldID = 3;

	public const int ZFieldID = 4;

	public const int YawFieldID = 5;

	public const int PitchFieldID = 6;

	public const int RollFieldID = 7;

	public const int HeadYawFieldID = 8;

	public const int HeadPitchFieldID = 9;

	public const int BodyYawFieldID = 10;

	public const int ControlsFieldID = 11;

	public const int TickFieldID = 12;

	public const int PositionVersionFieldID = 13;

	public const int MotionXFieldID = 14;

	public const int MotionYFieldID = 15;

	public const int MotionZFieldID = 16;

	public const int TeleportFieldID = 17;

	public const int TagsBitmask1FieldID = 18;

	public const int TagsBitmask2FieldID = 19;

	public const int MountControlsFieldID = 20;

	public int size;
}
