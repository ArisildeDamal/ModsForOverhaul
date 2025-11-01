using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common
{
	public class InventoryCharacter : InventoryBasePlayer
	{
		public InventoryCharacter(string className, string playerUID, ICoreAPI api)
			: base(className, playerUID, api)
		{
			this.slots = base.GenEmptySlots(15);
			this.baseWeight = 2.5f;
		}

		public InventoryCharacter(string inventoryId, ICoreAPI api)
			: base(inventoryId, api)
		{
			this.slots = base.GenEmptySlots(15);
			this.baseWeight = 2.5f;
		}

		public override void OnItemSlotModified(ItemSlot slot)
		{
			base.OnItemSlotModified(slot);
		}

		public override int Count
		{
			get
			{
				return this.slots.Length;
			}
		}

		public override ItemSlot this[int slotId]
		{
			get
			{
				if (slotId < 0 || slotId >= this.Count)
				{
					return null;
				}
				return this.slots[slotId];
			}
			set
			{
				if (slotId < 0 || slotId >= this.Count)
				{
					throw new ArgumentOutOfRangeException("slotId");
				}
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.slots[slotId] = value;
			}
		}

		public override void FromTreeAttributes(ITreeAttribute tree)
		{
			this.slots = this.SlotsFromTreeAttributes(tree, null, null);
			if (this.slots.Length == 10)
			{
				ItemSlot[] prevSlots = this.slots;
				this.slots = base.GenEmptySlots(12);
				for (int i = 0; i < prevSlots.Length; i++)
				{
					this.slots[i] = prevSlots[i];
				}
			}
			if (this.slots.Length == 12)
			{
				ItemSlot[] prevSlots2 = this.slots;
				this.slots = base.GenEmptySlots(15);
				for (int j = 0; j < prevSlots2.Length; j++)
				{
					this.slots[j] = prevSlots2[j];
				}
			}
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.SlotsToTreeAttributes(this.slots, tree);
			this.ResolveBlocksOrItems();
		}

		protected override ItemSlot NewSlot(int slotId)
		{
			ItemSlotCharacter slot = new ItemSlotCharacter((EnumCharacterDressType)slotId, this);
			this.iconByDressType.TryGetValue((EnumCharacterDressType)slotId, out slot.BackgroundIcon);
			return slot;
		}

		public override void DiscardAll()
		{
		}

		public override void OnOwningEntityDeath(Vec3d pos)
		{
		}

		public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
		{
			return new WeightedSlot();
		}

		private ItemSlot[] slots;

		private Dictionary<EnumCharacterDressType, string> iconByDressType = new Dictionary<EnumCharacterDressType, string>
		{
			{
				EnumCharacterDressType.Foot,
				"boots"
			},
			{
				EnumCharacterDressType.Hand,
				"gloves"
			},
			{
				EnumCharacterDressType.Shoulder,
				"cape"
			},
			{
				EnumCharacterDressType.Head,
				"hat"
			},
			{
				EnumCharacterDressType.LowerBody,
				"trousers"
			},
			{
				EnumCharacterDressType.UpperBody,
				"shirt"
			},
			{
				EnumCharacterDressType.UpperBodyOver,
				"pullover"
			},
			{
				EnumCharacterDressType.Neck,
				"necklace"
			},
			{
				EnumCharacterDressType.Arm,
				"bracers"
			},
			{
				EnumCharacterDressType.Waist,
				"belt"
			},
			{
				EnumCharacterDressType.Emblem,
				"medal"
			},
			{
				EnumCharacterDressType.Face,
				"mask"
			},
			{
				EnumCharacterDressType.ArmorHead,
				"armorhead"
			},
			{
				EnumCharacterDressType.ArmorBody,
				"armorbody"
			},
			{
				EnumCharacterDressType.ArmorLegs,
				"armorlegs"
			}
		};
	}
}
