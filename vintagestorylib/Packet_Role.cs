using System;

public class Packet_Role
{
	public void SetCode(string value)
	{
		this.Code = value;
	}

	public void SetPrivilegeLevel(int value)
	{
		this.PrivilegeLevel = value;
	}

	internal void InitializeValues()
	{
	}

	public string Code;

	public int PrivilegeLevel;

	public const int CodeFieldID = 1;

	public const int PrivilegeLevelFieldID = 2;

	public int size;
}
