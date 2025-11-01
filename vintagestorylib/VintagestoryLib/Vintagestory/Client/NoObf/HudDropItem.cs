using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf
{
	internal class HudDropItem : HudElement
	{
		public override double DrawOrder
		{
			get
			{
				return 1.0;
			}
		}

		public HudDropItem(ICoreClientAPI capi)
			: base(capi)
		{
			this.TryOpen();
		}

		public override bool TryClose()
		{
			return false;
		}

		public override void OnMouseDown(MouseEvent args)
		{
			if (args.Handled)
			{
				return;
			}
			foreach (GuiDialog guiDialog in this.capi.Gui.OpenedGuis)
			{
				if (guiDialog.IsOpened() && !(guiDialog is HudMouseTools))
				{
					using (IEnumerator<GuiComposer> enumerator2 = guiDialog.Composers.Values.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							if (enumerator2.Current.Bounds.PointInside(args.X, args.Y))
							{
								return;
							}
						}
					}
				}
			}
			if (this.capi.World.Player.InventoryManager.DropMouseSlotItems(args.Button == EnumMouseButton.Left))
			{
				args.Handled = true;
			}
		}

		public override bool ShouldReceiveKeyboardEvents()
		{
			return true;
		}

		public override bool Focusable
		{
			get
			{
				return false;
			}
		}
	}
}
