using System;

public class Packet_LandClaims
{
	public Packet_LandClaim[] GetAllclaims()
	{
		return this.Allclaims;
	}

	public void SetAllclaims(Packet_LandClaim[] value, int count, int length)
	{
		this.Allclaims = value;
		this.AllclaimsCount = count;
		this.AllclaimsLength = length;
	}

	public void SetAllclaims(Packet_LandClaim[] value)
	{
		this.Allclaims = value;
		this.AllclaimsCount = value.Length;
		this.AllclaimsLength = value.Length;
	}

	public int GetAllclaimsCount()
	{
		return this.AllclaimsCount;
	}

	public void AllclaimsAdd(Packet_LandClaim value)
	{
		if (this.AllclaimsCount >= this.AllclaimsLength)
		{
			if ((this.AllclaimsLength *= 2) == 0)
			{
				this.AllclaimsLength = 1;
			}
			Packet_LandClaim[] newArray = new Packet_LandClaim[this.AllclaimsLength];
			for (int i = 0; i < this.AllclaimsCount; i++)
			{
				newArray[i] = this.Allclaims[i];
			}
			this.Allclaims = newArray;
		}
		Packet_LandClaim[] allclaims = this.Allclaims;
		int allclaimsCount = this.AllclaimsCount;
		this.AllclaimsCount = allclaimsCount + 1;
		allclaims[allclaimsCount] = value;
	}

	public Packet_LandClaim[] GetAddclaims()
	{
		return this.Addclaims;
	}

	public void SetAddclaims(Packet_LandClaim[] value, int count, int length)
	{
		this.Addclaims = value;
		this.AddclaimsCount = count;
		this.AddclaimsLength = length;
	}

	public void SetAddclaims(Packet_LandClaim[] value)
	{
		this.Addclaims = value;
		this.AddclaimsCount = value.Length;
		this.AddclaimsLength = value.Length;
	}

	public int GetAddclaimsCount()
	{
		return this.AddclaimsCount;
	}

	public void AddclaimsAdd(Packet_LandClaim value)
	{
		if (this.AddclaimsCount >= this.AddclaimsLength)
		{
			if ((this.AddclaimsLength *= 2) == 0)
			{
				this.AddclaimsLength = 1;
			}
			Packet_LandClaim[] newArray = new Packet_LandClaim[this.AddclaimsLength];
			for (int i = 0; i < this.AddclaimsCount; i++)
			{
				newArray[i] = this.Addclaims[i];
			}
			this.Addclaims = newArray;
		}
		Packet_LandClaim[] addclaims = this.Addclaims;
		int addclaimsCount = this.AddclaimsCount;
		this.AddclaimsCount = addclaimsCount + 1;
		addclaims[addclaimsCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public Packet_LandClaim[] Allclaims;

	public int AllclaimsCount;

	public int AllclaimsLength;

	public Packet_LandClaim[] Addclaims;

	public int AddclaimsCount;

	public int AddclaimsLength;

	public const int AllclaimsFieldID = 1;

	public const int AddclaimsFieldID = 2;

	public int size;
}
