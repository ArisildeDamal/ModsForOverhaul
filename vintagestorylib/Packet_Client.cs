using System;

public class Packet_Client : IPacket
{
	public void SerializeTo(CitoStream stream)
	{
		Packet_ClientSerializer.Serialize(stream, this);
	}

	public void SetLoginTokenQuery(Packet_LoginTokenQuery value)
	{
		this.LoginTokenQuery = value;
	}

	public void SetId(int value)
	{
		this.Id = value;
	}

	public void SetIdentification(Packet_ClientIdentification value)
	{
		this.Identification = value;
	}

	public void SetBlockPlaceOrBreak(Packet_ClientBlockPlaceOrBreak value)
	{
		this.BlockPlaceOrBreak = value;
	}

	public void SetChatline(Packet_ChatLine value)
	{
		this.Chatline = value;
	}

	public void SetRequestJoin(Packet_ClientRequestJoin value)
	{
		this.RequestJoin = value;
	}

	public void SetPingReply(Packet_ClientPingReply value)
	{
		this.PingReply = value;
	}

	public void SetSpecialKey_(Packet_ClientSpecialKey value)
	{
		this.SpecialKey_ = value;
	}

	public void SetSelectedHotbarSlot(Packet_SelectedHotbarSlot value)
	{
		this.SelectedHotbarSlot = value;
	}

	public void SetLeave(Packet_ClientLeave value)
	{
		this.Leave = value;
	}

	public void SetQuery(Packet_ClientServerQuery value)
	{
		this.Query = value;
	}

	public void SetMoveItemstack(Packet_MoveItemstack value)
	{
		this.MoveItemstack = value;
	}

	public void SetFlipitemstacks(Packet_FlipItemstacks value)
	{
		this.Flipitemstacks = value;
	}

	public void SetEntityInteraction(Packet_EntityInteraction value)
	{
		this.EntityInteraction = value;
	}

	public void SetEntityPosition(Packet_EntityPosition value)
	{
		this.EntityPosition = value;
	}

	public void SetActivateInventorySlot(Packet_ActivateInventorySlot value)
	{
		this.ActivateInventorySlot = value;
	}

	public void SetCreateItemstack(Packet_CreateItemstack value)
	{
		this.CreateItemstack = value;
	}

	public void SetRequestModeChange(Packet_PlayerMode value)
	{
		this.RequestModeChange = value;
	}

	public void SetMoveKeyChange(Packet_MoveKeyChange value)
	{
		this.MoveKeyChange = value;
	}

	public void SetBlockEntityPacket(Packet_BlockEntityPacket value)
	{
		this.BlockEntityPacket = value;
	}

	public void SetEntityPacket(Packet_EntityPacket value)
	{
		this.EntityPacket = value;
	}

	public void SetCustomPacket(Packet_CustomPacket value)
	{
		this.CustomPacket = value;
	}

	public void SetHandInteraction(Packet_ClientHandInteraction value)
	{
		this.HandInteraction = value;
	}

	public void SetToolMode(Packet_ToolMode value)
	{
		this.ToolMode = value;
	}

	public void SetBlockDamage(Packet_BlockDamage value)
	{
		this.BlockDamage = value;
	}

	public void SetClientPlaying(Packet_ClientPlaying value)
	{
		this.ClientPlaying = value;
	}

	public void SetInvOpenedClosed(Packet_InvOpenClose value)
	{
		this.InvOpenedClosed = value;
	}

	public void SetRuntimeSetting(Packet_RuntimeSetting value)
	{
		this.RuntimeSetting = value;
	}

	public void SetUdpPacket(Packet_UdpPacket value)
	{
		this.UdpPacket = value;
	}

	internal void InitializeValues()
	{
		this.Id = 1;
	}

	public Packet_LoginTokenQuery LoginTokenQuery;

	public int Id;

	public Packet_ClientIdentification Identification;

	public Packet_ClientBlockPlaceOrBreak BlockPlaceOrBreak;

	public Packet_ChatLine Chatline;

	public Packet_ClientRequestJoin RequestJoin;

	public Packet_ClientPingReply PingReply;

	public Packet_ClientSpecialKey SpecialKey_;

	public Packet_SelectedHotbarSlot SelectedHotbarSlot;

	public Packet_ClientLeave Leave;

	public Packet_ClientServerQuery Query;

	public Packet_MoveItemstack MoveItemstack;

	public Packet_FlipItemstacks Flipitemstacks;

	public Packet_EntityInteraction EntityInteraction;

	public Packet_EntityPosition EntityPosition;

	public Packet_ActivateInventorySlot ActivateInventorySlot;

	public Packet_CreateItemstack CreateItemstack;

	public Packet_PlayerMode RequestModeChange;

	public Packet_MoveKeyChange MoveKeyChange;

	public Packet_BlockEntityPacket BlockEntityPacket;

	public Packet_EntityPacket EntityPacket;

	public Packet_CustomPacket CustomPacket;

	public Packet_ClientHandInteraction HandInteraction;

	public Packet_ToolMode ToolMode;

	public Packet_BlockDamage BlockDamage;

	public Packet_ClientPlaying ClientPlaying;

	public Packet_InvOpenClose InvOpenedClosed;

	public Packet_RuntimeSetting RuntimeSetting;

	public Packet_UdpPacket UdpPacket;

	public const int LoginTokenQueryFieldID = 33;

	public const int IdFieldID = 1;

	public const int IdentificationFieldID = 2;

	public const int BlockPlaceOrBreakFieldID = 3;

	public const int ChatlineFieldID = 4;

	public const int RequestJoinFieldID = 5;

	public const int PingReplyFieldID = 6;

	public const int SpecialKey_FieldID = 7;

	public const int SelectedHotbarSlotFieldID = 8;

	public const int LeaveFieldID = 9;

	public const int QueryFieldID = 10;

	public const int MoveItemstackFieldID = 14;

	public const int FlipitemstacksFieldID = 15;

	public const int EntityInteractionFieldID = 16;

	public const int EntityPositionFieldID = 18;

	public const int ActivateInventorySlotFieldID = 19;

	public const int CreateItemstackFieldID = 20;

	public const int RequestModeChangeFieldID = 21;

	public const int MoveKeyChangeFieldID = 22;

	public const int BlockEntityPacketFieldID = 23;

	public const int EntityPacketFieldID = 31;

	public const int CustomPacketFieldID = 24;

	public const int HandInteractionFieldID = 25;

	public const int ToolModeFieldID = 26;

	public const int BlockDamageFieldID = 27;

	public const int ClientPlayingFieldID = 28;

	public const int InvOpenedClosedFieldID = 30;

	public const int RuntimeSettingFieldID = 32;

	public const int UdpPacketFieldID = 34;

	public int size;
}
