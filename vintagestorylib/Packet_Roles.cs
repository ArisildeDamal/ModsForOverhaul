using System;

public class Packet_Roles
{
	public Packet_Role[] GetRoles()
	{
		return this.Roles;
	}

	public void SetRoles(Packet_Role[] value, int count, int length)
	{
		this.Roles = value;
		this.RolesCount = count;
		this.RolesLength = length;
	}

	public void SetRoles(Packet_Role[] value)
	{
		this.Roles = value;
		this.RolesCount = value.Length;
		this.RolesLength = value.Length;
	}

	public int GetRolesCount()
	{
		return this.RolesCount;
	}

	public void RolesAdd(Packet_Role value)
	{
		if (this.RolesCount >= this.RolesLength)
		{
			if ((this.RolesLength *= 2) == 0)
			{
				this.RolesLength = 1;
			}
			Packet_Role[] newArray = new Packet_Role[this.RolesLength];
			for (int i = 0; i < this.RolesCount; i++)
			{
				newArray[i] = this.Roles[i];
			}
			this.Roles = newArray;
		}
		Packet_Role[] roles = this.Roles;
		int rolesCount = this.RolesCount;
		this.RolesCount = rolesCount + 1;
		roles[rolesCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_Role[] Roles;

	public int RolesCount;

	public int RolesLength;

	public const int RolesFieldID = 1;

	public int size;
}
