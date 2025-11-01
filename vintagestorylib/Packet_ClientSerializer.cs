using System;

public class Packet_ClientSerializer
{
	public static Packet_Client DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Client instance = new Packet_Client();
		Packet_ClientSerializer.DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Client DeserializeBuffer(byte[] buffer, int length, Packet_Client instance)
	{
		Packet_ClientSerializer.Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Client Deserialize(CitoMemoryStream stream, Packet_Client instance)
	{
		instance.InitializeValues();
		int keyInt;
		for (;;)
		{
			keyInt = stream.ReadByte();
			if ((keyInt & 128) != 0)
			{
				keyInt = ProtocolParser.ReadKeyAsInt(keyInt, stream);
				if ((keyInt & 16384) != 0)
				{
					break;
				}
			}
			if (keyInt <= 146)
			{
				if (keyInt <= 50)
				{
					if (keyInt <= 18)
					{
						if (keyInt == 0)
						{
							goto IL_01EC;
						}
						if (keyInt == 8)
						{
							instance.Id = ProtocolParser.ReadUInt32(stream);
							continue;
						}
						if (keyInt == 18)
						{
							if (instance.Identification == null)
							{
								instance.Identification = Packet_ClientIdentificationSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_ClientIdentificationSerializer.DeserializeLengthDelimited(stream, instance.Identification);
							continue;
						}
					}
					else if (keyInt <= 34)
					{
						if (keyInt != 26)
						{
							if (keyInt == 34)
							{
								if (instance.Chatline == null)
								{
									instance.Chatline = Packet_ChatLineSerializer.DeserializeLengthDelimitedNew(stream);
									continue;
								}
								Packet_ChatLineSerializer.DeserializeLengthDelimited(stream, instance.Chatline);
								continue;
							}
						}
						else
						{
							if (instance.BlockPlaceOrBreak == null)
							{
								instance.BlockPlaceOrBreak = Packet_ClientBlockPlaceOrBreakSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_ClientBlockPlaceOrBreakSerializer.DeserializeLengthDelimited(stream, instance.BlockPlaceOrBreak);
							continue;
						}
					}
					else if (keyInt != 42)
					{
						if (keyInt == 50)
						{
							if (instance.PingReply == null)
							{
								instance.PingReply = Packet_ClientPingReplySerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_ClientPingReplySerializer.DeserializeLengthDelimited(stream, instance.PingReply);
							continue;
						}
					}
					else
					{
						if (instance.RequestJoin == null)
						{
							instance.RequestJoin = Packet_ClientRequestJoinSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_ClientRequestJoinSerializer.DeserializeLengthDelimited(stream, instance.RequestJoin);
						continue;
					}
				}
				else if (keyInt <= 82)
				{
					if (keyInt <= 66)
					{
						if (keyInt != 58)
						{
							if (keyInt == 66)
							{
								if (instance.SelectedHotbarSlot == null)
								{
									instance.SelectedHotbarSlot = Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimitedNew(stream);
									continue;
								}
								Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimited(stream, instance.SelectedHotbarSlot);
								continue;
							}
						}
						else
						{
							if (instance.SpecialKey_ == null)
							{
								instance.SpecialKey_ = Packet_ClientSpecialKeySerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_ClientSpecialKeySerializer.DeserializeLengthDelimited(stream, instance.SpecialKey_);
							continue;
						}
					}
					else if (keyInt != 74)
					{
						if (keyInt == 82)
						{
							if (instance.Query == null)
							{
								instance.Query = Packet_ClientServerQuerySerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_ClientServerQuerySerializer.DeserializeLengthDelimited(stream, instance.Query);
							continue;
						}
					}
					else
					{
						if (instance.Leave == null)
						{
							instance.Leave = Packet_ClientLeaveSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_ClientLeaveSerializer.DeserializeLengthDelimited(stream, instance.Leave);
						continue;
					}
				}
				else if (keyInt <= 122)
				{
					if (keyInt != 114)
					{
						if (keyInt == 122)
						{
							if (instance.Flipitemstacks == null)
							{
								instance.Flipitemstacks = Packet_FlipItemstacksSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_FlipItemstacksSerializer.DeserializeLengthDelimited(stream, instance.Flipitemstacks);
							continue;
						}
					}
					else
					{
						if (instance.MoveItemstack == null)
						{
							instance.MoveItemstack = Packet_MoveItemstackSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_MoveItemstackSerializer.DeserializeLengthDelimited(stream, instance.MoveItemstack);
						continue;
					}
				}
				else if (keyInt != 130)
				{
					if (keyInt == 146)
					{
						if (instance.EntityPosition == null)
						{
							instance.EntityPosition = Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_EntityPositionSerializer.DeserializeLengthDelimited(stream, instance.EntityPosition);
						continue;
					}
				}
				else
				{
					if (instance.EntityInteraction == null)
					{
						instance.EntityInteraction = Packet_EntityInteractionSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_EntityInteractionSerializer.DeserializeLengthDelimited(stream, instance.EntityInteraction);
					continue;
				}
			}
			else if (keyInt <= 202)
			{
				if (keyInt <= 170)
				{
					if (keyInt != 154)
					{
						if (keyInt != 162)
						{
							if (keyInt == 170)
							{
								if (instance.RequestModeChange == null)
								{
									instance.RequestModeChange = Packet_PlayerModeSerializer.DeserializeLengthDelimitedNew(stream);
									continue;
								}
								Packet_PlayerModeSerializer.DeserializeLengthDelimited(stream, instance.RequestModeChange);
								continue;
							}
						}
						else
						{
							if (instance.CreateItemstack == null)
							{
								instance.CreateItemstack = Packet_CreateItemstackSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_CreateItemstackSerializer.DeserializeLengthDelimited(stream, instance.CreateItemstack);
							continue;
						}
					}
					else
					{
						if (instance.ActivateInventorySlot == null)
						{
							instance.ActivateInventorySlot = Packet_ActivateInventorySlotSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_ActivateInventorySlotSerializer.DeserializeLengthDelimited(stream, instance.ActivateInventorySlot);
						continue;
					}
				}
				else if (keyInt <= 186)
				{
					if (keyInt != 178)
					{
						if (keyInt == 186)
						{
							if (instance.BlockEntityPacket == null)
							{
								instance.BlockEntityPacket = Packet_BlockEntityPacketSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_BlockEntityPacketSerializer.DeserializeLengthDelimited(stream, instance.BlockEntityPacket);
							continue;
						}
					}
					else
					{
						if (instance.MoveKeyChange == null)
						{
							instance.MoveKeyChange = Packet_MoveKeyChangeSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_MoveKeyChangeSerializer.DeserializeLengthDelimited(stream, instance.MoveKeyChange);
						continue;
					}
				}
				else if (keyInt != 194)
				{
					if (keyInt == 202)
					{
						if (instance.HandInteraction == null)
						{
							instance.HandInteraction = Packet_ClientHandInteractionSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_ClientHandInteractionSerializer.DeserializeLengthDelimited(stream, instance.HandInteraction);
						continue;
					}
				}
				else
				{
					if (instance.CustomPacket == null)
					{
						instance.CustomPacket = Packet_CustomPacketSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_CustomPacketSerializer.DeserializeLengthDelimited(stream, instance.CustomPacket);
					continue;
				}
			}
			else if (keyInt <= 242)
			{
				if (keyInt <= 218)
				{
					if (keyInt != 210)
					{
						if (keyInt == 218)
						{
							if (instance.BlockDamage == null)
							{
								instance.BlockDamage = Packet_BlockDamageSerializer.DeserializeLengthDelimitedNew(stream);
								continue;
							}
							Packet_BlockDamageSerializer.DeserializeLengthDelimited(stream, instance.BlockDamage);
							continue;
						}
					}
					else
					{
						if (instance.ToolMode == null)
						{
							instance.ToolMode = Packet_ToolModeSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_ToolModeSerializer.DeserializeLengthDelimited(stream, instance.ToolMode);
						continue;
					}
				}
				else if (keyInt != 226)
				{
					if (keyInt == 242)
					{
						if (instance.InvOpenedClosed == null)
						{
							instance.InvOpenedClosed = Packet_InvOpenCloseSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_InvOpenCloseSerializer.DeserializeLengthDelimited(stream, instance.InvOpenedClosed);
						continue;
					}
				}
				else
				{
					if (instance.ClientPlaying == null)
					{
						instance.ClientPlaying = Packet_ClientPlayingSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_ClientPlayingSerializer.DeserializeLengthDelimited(stream, instance.ClientPlaying);
					continue;
				}
			}
			else if (keyInt <= 258)
			{
				if (keyInt != 250)
				{
					if (keyInt == 258)
					{
						if (instance.RuntimeSetting == null)
						{
							instance.RuntimeSetting = Packet_RuntimeSettingSerializer.DeserializeLengthDelimitedNew(stream);
							continue;
						}
						Packet_RuntimeSettingSerializer.DeserializeLengthDelimited(stream, instance.RuntimeSetting);
						continue;
					}
				}
				else
				{
					if (instance.EntityPacket == null)
					{
						instance.EntityPacket = Packet_EntityPacketSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_EntityPacketSerializer.DeserializeLengthDelimited(stream, instance.EntityPacket);
					continue;
				}
			}
			else if (keyInt != 266)
			{
				if (keyInt == 274)
				{
					if (instance.UdpPacket == null)
					{
						instance.UdpPacket = Packet_UdpPacketSerializer.DeserializeLengthDelimitedNew(stream);
						continue;
					}
					Packet_UdpPacketSerializer.DeserializeLengthDelimited(stream, instance.UdpPacket);
					continue;
				}
			}
			else
			{
				if (instance.LoginTokenQuery == null)
				{
					instance.LoginTokenQuery = Packet_LoginTokenQuerySerializer.DeserializeLengthDelimitedNew(stream);
					continue;
				}
				Packet_LoginTokenQuerySerializer.DeserializeLengthDelimited(stream, instance.LoginTokenQuery);
				continue;
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
		IL_01EC:
		return null;
	}

	public static Packet_Client DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Client instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Client packet_Client = Packet_ClientSerializer.Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return packet_Client;
	}

	public static void Serialize(CitoStream stream, Packet_Client instance)
	{
		if (instance.LoginTokenQuery != null)
		{
			stream.WriteKey(33, 2);
			Packet_LoginTokenQuery i33 = instance.LoginTokenQuery;
			Packet_LoginTokenQuerySerializer.GetSize(i33);
			Packet_LoginTokenQuerySerializer.SerializeWithSize(stream, i33);
		}
		if (instance.Id != 1)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Id);
		}
		if (instance.Identification != null)
		{
			stream.WriteByte(18);
			Packet_ClientIdentification i34 = instance.Identification;
			Packet_ClientIdentificationSerializer.GetSize(i34);
			Packet_ClientIdentificationSerializer.SerializeWithSize(stream, i34);
		}
		if (instance.BlockPlaceOrBreak != null)
		{
			stream.WriteByte(26);
			Packet_ClientBlockPlaceOrBreak i35 = instance.BlockPlaceOrBreak;
			Packet_ClientBlockPlaceOrBreakSerializer.GetSize(i35);
			Packet_ClientBlockPlaceOrBreakSerializer.SerializeWithSize(stream, i35);
		}
		if (instance.Chatline != null)
		{
			stream.WriteByte(34);
			Packet_ChatLine i36 = instance.Chatline;
			Packet_ChatLineSerializer.GetSize(i36);
			Packet_ChatLineSerializer.SerializeWithSize(stream, i36);
		}
		if (instance.RequestJoin != null)
		{
			stream.WriteByte(42);
			Packet_ClientRequestJoin i37 = instance.RequestJoin;
			Packet_ClientRequestJoinSerializer.GetSize(i37);
			Packet_ClientRequestJoinSerializer.SerializeWithSize(stream, i37);
		}
		if (instance.PingReply != null)
		{
			stream.WriteByte(50);
			Packet_ClientPingReply i38 = instance.PingReply;
			Packet_ClientPingReplySerializer.GetSize(i38);
			Packet_ClientPingReplySerializer.SerializeWithSize(stream, i38);
		}
		if (instance.SpecialKey_ != null)
		{
			stream.WriteByte(58);
			Packet_ClientSpecialKey i39 = instance.SpecialKey_;
			Packet_ClientSpecialKeySerializer.GetSize(i39);
			Packet_ClientSpecialKeySerializer.SerializeWithSize(stream, i39);
		}
		if (instance.SelectedHotbarSlot != null)
		{
			stream.WriteByte(66);
			Packet_SelectedHotbarSlot i40 = instance.SelectedHotbarSlot;
			Packet_SelectedHotbarSlotSerializer.GetSize(i40);
			Packet_SelectedHotbarSlotSerializer.SerializeWithSize(stream, i40);
		}
		if (instance.Leave != null)
		{
			stream.WriteByte(74);
			Packet_ClientLeave i41 = instance.Leave;
			Packet_ClientLeaveSerializer.GetSize(i41);
			Packet_ClientLeaveSerializer.SerializeWithSize(stream, i41);
		}
		if (instance.Query != null)
		{
			stream.WriteByte(82);
			Packet_ClientServerQuery i42 = instance.Query;
			Packet_ClientServerQuerySerializer.GetSize(i42);
			Packet_ClientServerQuerySerializer.SerializeWithSize(stream, i42);
		}
		if (instance.MoveItemstack != null)
		{
			stream.WriteByte(114);
			Packet_MoveItemstack i43 = instance.MoveItemstack;
			Packet_MoveItemstackSerializer.GetSize(i43);
			Packet_MoveItemstackSerializer.SerializeWithSize(stream, i43);
		}
		if (instance.Flipitemstacks != null)
		{
			stream.WriteByte(122);
			Packet_FlipItemstacks i44 = instance.Flipitemstacks;
			Packet_FlipItemstacksSerializer.GetSize(i44);
			Packet_FlipItemstacksSerializer.SerializeWithSize(stream, i44);
		}
		if (instance.EntityInteraction != null)
		{
			stream.WriteKey(16, 2);
			Packet_EntityInteraction i45 = instance.EntityInteraction;
			Packet_EntityInteractionSerializer.GetSize(i45);
			Packet_EntityInteractionSerializer.SerializeWithSize(stream, i45);
		}
		if (instance.EntityPosition != null)
		{
			stream.WriteKey(18, 2);
			Packet_EntityPosition i46 = instance.EntityPosition;
			Packet_EntityPositionSerializer.GetSize(i46);
			Packet_EntityPositionSerializer.SerializeWithSize(stream, i46);
		}
		if (instance.ActivateInventorySlot != null)
		{
			stream.WriteKey(19, 2);
			Packet_ActivateInventorySlot i47 = instance.ActivateInventorySlot;
			Packet_ActivateInventorySlotSerializer.GetSize(i47);
			Packet_ActivateInventorySlotSerializer.SerializeWithSize(stream, i47);
		}
		if (instance.CreateItemstack != null)
		{
			stream.WriteKey(20, 2);
			Packet_CreateItemstack i48 = instance.CreateItemstack;
			Packet_CreateItemstackSerializer.GetSize(i48);
			Packet_CreateItemstackSerializer.SerializeWithSize(stream, i48);
		}
		if (instance.RequestModeChange != null)
		{
			stream.WriteKey(21, 2);
			Packet_PlayerMode i49 = instance.RequestModeChange;
			Packet_PlayerModeSerializer.GetSize(i49);
			Packet_PlayerModeSerializer.SerializeWithSize(stream, i49);
		}
		if (instance.MoveKeyChange != null)
		{
			stream.WriteKey(22, 2);
			Packet_MoveKeyChange i50 = instance.MoveKeyChange;
			Packet_MoveKeyChangeSerializer.GetSize(i50);
			Packet_MoveKeyChangeSerializer.SerializeWithSize(stream, i50);
		}
		if (instance.BlockEntityPacket != null)
		{
			stream.WriteKey(23, 2);
			Packet_BlockEntityPacket i51 = instance.BlockEntityPacket;
			Packet_BlockEntityPacketSerializer.GetSize(i51);
			Packet_BlockEntityPacketSerializer.SerializeWithSize(stream, i51);
		}
		if (instance.EntityPacket != null)
		{
			stream.WriteKey(31, 2);
			Packet_EntityPacket i52 = instance.EntityPacket;
			Packet_EntityPacketSerializer.GetSize(i52);
			Packet_EntityPacketSerializer.SerializeWithSize(stream, i52);
		}
		if (instance.CustomPacket != null)
		{
			stream.WriteKey(24, 2);
			Packet_CustomPacket i53 = instance.CustomPacket;
			Packet_CustomPacketSerializer.GetSize(i53);
			Packet_CustomPacketSerializer.SerializeWithSize(stream, i53);
		}
		if (instance.HandInteraction != null)
		{
			stream.WriteKey(25, 2);
			Packet_ClientHandInteraction i54 = instance.HandInteraction;
			Packet_ClientHandInteractionSerializer.GetSize(i54);
			Packet_ClientHandInteractionSerializer.SerializeWithSize(stream, i54);
		}
		if (instance.ToolMode != null)
		{
			stream.WriteKey(26, 2);
			Packet_ToolMode i55 = instance.ToolMode;
			Packet_ToolModeSerializer.GetSize(i55);
			Packet_ToolModeSerializer.SerializeWithSize(stream, i55);
		}
		if (instance.BlockDamage != null)
		{
			stream.WriteKey(27, 2);
			Packet_BlockDamage i56 = instance.BlockDamage;
			Packet_BlockDamageSerializer.GetSize(i56);
			Packet_BlockDamageSerializer.SerializeWithSize(stream, i56);
		}
		if (instance.ClientPlaying != null)
		{
			stream.WriteKey(28, 2);
			Packet_ClientPlaying i57 = instance.ClientPlaying;
			Packet_ClientPlayingSerializer.GetSize(i57);
			Packet_ClientPlayingSerializer.SerializeWithSize(stream, i57);
		}
		if (instance.InvOpenedClosed != null)
		{
			stream.WriteKey(30, 2);
			Packet_InvOpenClose i58 = instance.InvOpenedClosed;
			Packet_InvOpenCloseSerializer.GetSize(i58);
			Packet_InvOpenCloseSerializer.SerializeWithSize(stream, i58);
		}
		if (instance.RuntimeSetting != null)
		{
			stream.WriteKey(32, 2);
			Packet_RuntimeSetting i59 = instance.RuntimeSetting;
			Packet_RuntimeSettingSerializer.GetSize(i59);
			Packet_RuntimeSettingSerializer.SerializeWithSize(stream, i59);
		}
		if (instance.UdpPacket != null)
		{
			stream.WriteKey(34, 2);
			Packet_UdpPacket i60 = instance.UdpPacket;
			Packet_UdpPacketSerializer.GetSize(i60);
			Packet_UdpPacketSerializer.SerializeWithSize(stream, i60);
		}
	}

	public static void SerializeWithSize(CitoStream stream, Packet_Client instance)
	{
		ProtocolParser.WriteUInt32_(stream, instance.size);
		int positionSaved = stream.Position();
		Packet_ClientSerializer.Serialize(stream, instance);
		int len = stream.Position() - positionSaved;
		if (len != instance.size)
		{
			throw new Exception("Sizing mismatch: " + instance.size.ToString() + " != " + len.ToString());
		}
	}

	public static byte[] SerializeToBytes(Packet_Client instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientSerializer.Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Client instance)
	{
		byte[] data = Packet_ClientSerializer.SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}

	private const int field = 8;
}
