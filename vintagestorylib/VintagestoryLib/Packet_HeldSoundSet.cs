using System;

public class Packet_HeldSoundSet
{
	public void SetIdle(string value)
	{
		this.Idle = value;
	}

	public void SetEquip(string value)
	{
		this.Equip = value;
	}

	public void SetUnequip(string value)
	{
		this.Unequip = value;
	}

	public void SetAttack(string value)
	{
		this.Attack = value;
	}

	public void SetInvPickup(string value)
	{
		this.InvPickup = value;
	}

	public void SetInvPlace(string value)
	{
		this.InvPlace = value;
	}

	internal void InitializeValues()
	{
	}

	public string Idle;

	public string Equip;

	public string Unequip;

	public string Attack;

	public string InvPickup;

	public string InvPlace;

	public const int IdleFieldID = 1;

	public const int EquipFieldID = 2;

	public const int UnequipFieldID = 3;

	public const int AttackFieldID = 4;

	public const int InvPickupFieldID = 5;

	public const int InvPlaceFieldID = 6;

	public int size;
}
