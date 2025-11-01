using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common
{
	public abstract class PlayerInventoryManager : IPlayerInventoryManager
	{
		public IEnumerable<InventoryBase> InventoriesOrdered
		{
			get
			{
				return this.Inventories.ValuesOrdered;
			}
		}

		public EnumTool? ActiveTool
		{
			get
			{
				ItemStack itemstack = this.ActiveHotbarSlot.Itemstack;
				if (itemstack == null)
				{
					return null;
				}
				return itemstack.Collectible.Tool;
			}
		}

		public EnumTool? OffhandTool
		{
			get
			{
				ItemStack itemstack = this.OffhandHotbarSlot.Itemstack;
				if (itemstack == null)
				{
					return null;
				}
				return itemstack.Collectible.Tool;
			}
		}

		public virtual int ActiveHotbarSlotNumber { get; set; }

		public ItemSlot ActiveHotbarSlot
		{
			get
			{
				string invId = "hotbar-" + this.player.PlayerUID;
				InventoryBase hotbarInv;
				this.GetInventory(invId, out hotbarInv);
				int skoffset = ((hotbarInv == null || hotbarInv[10].Empty) ? 0 : 1);
				if (this.ActiveHotbarSlotNumber >= 10 + skoffset)
				{
					invId = "backpack-" + this.player.PlayerUID;
					InventoryBase backpackInv;
					if (this.GetInventory(invId, out backpackInv))
					{
						return backpackInv[this.ActiveHotbarSlotNumber - 10 - skoffset];
					}
					return null;
				}
				else
				{
					if (hotbarInv == null)
					{
						return null;
					}
					return hotbarInv[this.ActiveHotbarSlotNumber];
				}
			}
		}

		public ItemSlot OffhandHotbarSlot
		{
			get
			{
				string invId = "hotbar-" + this.player.PlayerUID;
				if (this.Inventories.ContainsKey(invId))
				{
					return this.Inventories[invId][11];
				}
				return null;
			}
		}

		public ItemSlot MouseItemSlot
		{
			get
			{
				string invId = "mouse-" + this.player.PlayerUID;
				if (this.Inventories.ContainsKey(invId))
				{
					return this.Inventories[invId][0];
				}
				return null;
			}
		}

		Dictionary<string, IInventory> IPlayerInventoryManager.Inventories
		{
			get
			{
				Dictionary<string, IInventory> inv = new Dictionary<string, IInventory>();
				foreach (KeyValuePair<string, InventoryBase> val in this.Inventories)
				{
					inv[val.Key] = val.Value;
				}
				return inv;
			}
		}

		public List<IInventory> OpenedInventories
		{
			get
			{
				return this.InventoriesOrdered.Where((InventoryBase inv) => inv.HasOpened(this.player)).ToList<IInventory>();
			}
		}

		public abstract ItemSlot CurrentHoveredSlot { get; set; }

		public abstract void BroadcastHotbarSlot();

		public PlayerInventoryManager(OrderedDictionary<string, InventoryBase> AllInventories, IPlayer player)
		{
			this.Inventories = AllInventories;
			this.player = player;
		}

		public bool IsVisibleHandSlot(string invid, int slotNumber)
		{
			return this.Inventories.ContainsKey(invid) && this.Inventories[invid] is InventoryPlayerHotbar && (this.ActiveHotbarSlotNumber == slotNumber || slotNumber == 10);
		}

		public string GetInventoryName(string inventoryClassName)
		{
			return inventoryClassName + "-" + this.player.PlayerUID;
		}

		public IInventory GetOwnInventory(string inventoryClassName)
		{
			if (this.Inventories.ContainsKey(this.GetInventoryName(inventoryClassName)))
			{
				return this.Inventories[this.GetInventoryName(inventoryClassName)];
			}
			return null;
		}

		public IInventory GetInventory(string inventoryClassName)
		{
			if (this.Inventories.ContainsKey(inventoryClassName))
			{
				return this.Inventories[inventoryClassName];
			}
			return null;
		}

		public ItemStack GetHotbarItemstack(int slotId)
		{
			string invId = "hotbar-" + this.player.PlayerUID;
			if (this.Inventories.ContainsKey(invId))
			{
				return this.Inventories[invId][slotId].Itemstack;
			}
			return null;
		}

		public IInventory GetHotbarInventory()
		{
			string invId = "hotbar-" + this.player.PlayerUID;
			if (this.Inventories.ContainsKey(invId))
			{
				return this.Inventories[invId];
			}
			return null;
		}

		public bool GetInventory(string invID, out InventoryBase invFound)
		{
			return this.Inventories.TryGetValue(invID, out invFound);
		}

		[Obsolete("Use GetBestSuitedSlot(ItemSlot sourceSlot, bool onlyPlayerInventory, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null) instead")]
		public ItemSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
		{
			return this.GetBestSuitedSlot(sourceSlot, true, op, skipSlots);
		}

		public ItemSlot GetBestSuitedSlot(ItemSlot sourceSlot, bool onlyPlayerInventory, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null)
		{
			WeightedSlot bestFreeslot = new WeightedSlot();
			foreach (InventoryBase inv in this.InventoriesOrdered.Reverse<InventoryBase>())
			{
				if ((!onlyPlayerInventory || inv is InventoryBasePlayer) && inv.HasOpened(this.player) && inv.CanPlayerAccess(this.player, new EntityPos()))
				{
					WeightedSlot freeSlot = inv.GetBestSuitedSlot(sourceSlot, op, skipSlots);
					if (freeSlot.weight > bestFreeslot.weight)
					{
						bestFreeslot = freeSlot;
					}
				}
			}
			return bestFreeslot.slot;
		}

		public bool TryGiveItemstack(ItemStack itemstack, bool slotNotifyEffect = false)
		{
			if (itemstack == null || itemstack.StackSize == 0)
			{
				return false;
			}
			ItemSlot dummySlot = new DummySlot(null);
			dummySlot.Itemstack = itemstack;
			ItemStackMoveOperation op = new ItemStackMoveOperation(this.player.Entity.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, itemstack.StackSize);
			object obj = this.TryTransferAway(dummySlot, ref op, true, null, slotNotifyEffect);
			if (dummySlot.Itemstack == null)
			{
				itemstack.StackSize = 0;
			}
			return obj != null;
		}

		public object[] TryTransferAway(ItemSlot sourceSlot, ref ItemStackMoveOperation op, bool onlyPlayerInventory, bool slotNotifyEffect = false)
		{
			return this.TryTransferAway(sourceSlot, ref op, onlyPlayerInventory, null, slotNotifyEffect);
		}

		public object[] TryTransferAway(ItemSlot sourceSlot, ref ItemStackMoveOperation op, bool onlyPlayerInventory, StringBuilder shiftClickDebugText, bool slotNotifyEffect = false)
		{
			if (sourceSlot.Itemstack == null || !sourceSlot.CanTake())
			{
				return null;
			}
			List<object> packets = new List<object>();
			List<ItemSlot> skipSlots = new List<ItemSlot>();
			op.RequestedQuantity = sourceSlot.StackSize;
			int i = 0;
			while (i++ < 5000 && sourceSlot.StackSize > 0)
			{
				ItemSlot sinkSlot = this.GetBestSuitedSlot(sourceSlot, onlyPlayerInventory, op, skipSlots);
				if (sinkSlot == null)
				{
					break;
				}
				skipSlots.Add(sinkSlot);
				int beforeQuantity = sinkSlot.StackSize;
				sourceSlot.TryPutInto(sinkSlot, ref op);
				if (shiftClickDebugText != null)
				{
					if (beforeQuantity != sinkSlot.StackSize)
					{
						if (shiftClickDebugText.Length > 0)
						{
							shiftClickDebugText.Append(", ");
						}
						string text = "{0}x into {1}";
						object obj = sinkSlot.StackSize - beforeQuantity;
						InventoryBase inventory = sinkSlot.Inventory;
						shiftClickDebugText.Append(string.Format(text, obj, (inventory != null) ? inventory.InventoryID : null));
					}
					else if (sinkSlot is ItemSlotBlackHole)
					{
						if (shiftClickDebugText.Length > 0)
						{
							shiftClickDebugText.Append(", ");
						}
						shiftClickDebugText.Append(string.Format("{0}x into black hole slot", op.RequestedQuantity));
					}
				}
				int quantityUnMerged = op.NotMovedQuantity;
				if (beforeQuantity != sinkSlot.StackSize && !sinkSlot.Empty && sinkSlot.Inventory is InventoryBasePlayer)
				{
					TreeAttribute tree = new TreeAttribute();
					tree["itemstack"] = new ItemstackAttribute(sinkSlot.Itemstack.Clone());
					TreeAttribute treeAttribute = tree;
					string text2 = "byentityid";
					IPlayer player = this.player;
					long? num;
					if (player == null)
					{
						num = null;
					}
					else
					{
						EntityPlayer entity = player.Entity;
						num = ((entity != null) ? new long?(entity.EntityId) : null);
					}
					long? num2 = num;
					treeAttribute[text2] = new LongAttribute(num2.GetValueOrDefault());
					this.player.Entity.Api.Event.PushEvent("onitemgrabbed", tree);
				}
				if (beforeQuantity != sinkSlot.StackSize && slotNotifyEffect)
				{
					sinkSlot.MarkDirty();
					sourceSlot.MarkDirty();
					this.NotifySlot(this.player, sinkSlot);
					if (sinkSlot == this.ActiveHotbarSlot)
					{
						this.BroadcastHotbarSlot();
					}
				}
				if (sourceSlot.Inventory == null || sourceSlot is ItemSlotCreative)
				{
					if (sinkSlot.Itemstack != null && sinkSlot.Itemstack.StackSize != beforeQuantity)
					{
						packets.Add(new Packet_Client
						{
							CreateItemstack = new Packet_CreateItemstack
							{
								Itemstack = StackConverter.ToPacket(sinkSlot.Itemstack),
								TargetInventoryId = sinkSlot.Inventory.InventoryID,
								TargetLastChanged = sinkSlot.Inventory.LastChanged,
								TargetSlot = sinkSlot.Inventory.GetSlotId(sinkSlot)
							},
							Id = 10
						});
					}
					if (quantityUnMerged == 0)
					{
						break;
					}
				}
				else
				{
					packets.Add(new Packet_Client
					{
						MoveItemstack = new Packet_MoveItemstack
						{
							SourceInventoryId = sourceSlot.Inventory.InventoryID,
							TargetInventoryId = sinkSlot.Inventory.InventoryID,
							SourceSlot = sourceSlot.Inventory.GetSlotId(sourceSlot),
							TargetSlot = sinkSlot.Inventory.GetSlotId(sinkSlot),
							SourceLastChanged = sourceSlot.Inventory.LastChanged,
							TargetLastChanged = sinkSlot.Inventory.LastChanged,
							Quantity = op.RequestedQuantity,
							Modifiers = (int)op.Modifiers,
							MouseButton = (int)op.MouseButton,
							Priority = (int)op.CurrentPriority
						},
						Id = 8
					});
					if (quantityUnMerged == 0 || sourceSlot.Empty)
					{
						break;
					}
				}
			}
			if (packets.Count <= 0)
			{
				return null;
			}
			return packets.ToArray();
		}

		public void DiscardAll()
		{
			foreach (InventoryBase inv in this.Inventories.Values)
			{
				if (inv is InventoryBasePlayer)
				{
					inv.DiscardAll();
				}
			}
		}

		public void OnDeath()
		{
			foreach (InventoryBase inv in this.Inventories.Values)
			{
				if (inv is InventoryBasePlayer)
				{
					inv.OnOwningEntityDeath(this.player.Entity.SidedPos.XYZ);
				}
			}
		}

		public object OpenInventory(IInventory inventory)
		{
			this.Inventories[inventory.InventoryID] = (InventoryBase)inventory;
			return inventory.Open(this.player);
		}

		public object CloseInventory(IInventory inventory)
		{
			if (inventory.RemoveOnClose)
			{
				this.Inventories.Remove(inventory.InventoryID);
			}
			return inventory.Close(this.player);
		}

		public void CloseInventoryAndSync(IInventory inventory)
		{
			object pkt = this.CloseInventory(inventory);
			ICoreClientAPI capi = this.player.Entity.Api as ICoreClientAPI;
			if (capi != null)
			{
				capi.Network.SendPacketClient(pkt);
			}
		}

		public bool HasInventory(IInventory inventory)
		{
			return this.Inventories.ContainsValue((InventoryBase)inventory);
		}

		public abstract void NotifySlot(IPlayer player, ItemSlot slot);

		public bool DropMouseSlotItems(bool fullStack)
		{
			return this.DropItem(this.MouseItemSlot, fullStack);
		}

		public bool DropHotbarSlotItems(bool fullStack)
		{
			return this.DropItem(this.ActiveHotbarSlot, fullStack);
		}

		public void DropAllInventoryItems(IInventory inventory)
		{
			foreach (ItemSlot slot in inventory)
			{
				this.DropItem(slot, true);
			}
		}

		public abstract bool DropItem(ItemSlot mouseItemSlot, bool fullStack);

		public object TryTransferTo(ItemSlot sourceSlot, ItemSlot targetSlot, ref ItemStackMoveOperation op)
		{
			if (sourceSlot.Itemstack == null || !sourceSlot.CanTake() || targetSlot == null)
			{
				return null;
			}
			int beforeQuantity = targetSlot.StackSize;
			sourceSlot.TryPutInto(targetSlot, ref op);
			if ((sourceSlot.Inventory == null || sourceSlot is ItemSlotCreative) && targetSlot.Itemstack != null && targetSlot.Itemstack.StackSize != beforeQuantity)
			{
				return new Packet_Client
				{
					CreateItemstack = new Packet_CreateItemstack
					{
						Itemstack = StackConverter.ToPacket(targetSlot.Itemstack),
						TargetInventoryId = targetSlot.Inventory.InventoryID,
						TargetLastChanged = targetSlot.Inventory.LastChanged,
						TargetSlot = targetSlot.Inventory.GetSlotId(targetSlot)
					},
					Id = 10
				};
			}
			return new Packet_Client
			{
				MoveItemstack = new Packet_MoveItemstack
				{
					SourceInventoryId = sourceSlot.Inventory.InventoryID,
					TargetInventoryId = targetSlot.Inventory.InventoryID,
					SourceSlot = sourceSlot.Inventory.GetSlotId(sourceSlot),
					TargetSlot = targetSlot.Inventory.GetSlotId(targetSlot),
					SourceLastChanged = sourceSlot.Inventory.LastChanged,
					TargetLastChanged = targetSlot.Inventory.LastChanged,
					Quantity = Math.Max(0, targetSlot.StackSize - beforeQuantity),
					Modifiers = (int)op.Modifiers,
					MouseButton = (int)op.MouseButton,
					Priority = (int)op.CurrentPriority
				},
				Id = 8
			};
		}

		public bool Find(Func<ItemSlot, bool> matcher)
		{
			foreach (IInventory inventory in this.OpenedInventories)
			{
				foreach (ItemSlot slot in inventory)
				{
					if (matcher(slot))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static string[] defaultInventories = new string[] { "hotbar", "creative", "backpack", "ground", "mouse", "craftinggrid", "character" };

		public IPlayer player;

		public OrderedDictionary<string, InventoryBase> Inventories;
	}
}
