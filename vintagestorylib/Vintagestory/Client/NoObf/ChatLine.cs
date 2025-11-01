using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ChatLine
	{
		private ChatLine()
		{
		}

		public ChatLine Create(string text, EnumChatType chatType, long msEllapsed)
		{
			return new ChatLine
			{
				Text = text,
				PostedMs = msEllapsed,
				TextColor = this.TextColorFromChatSource(chatType),
				BackgroundColor = this.BackColorFromChatSource(chatType)
			};
		}

		private int BackColorFromChatSource(EnumChatType chatType)
		{
			if (chatType == EnumChatType.Notification)
			{
				return -1;
			}
			return 0;
		}

		private int TextColorFromChatSource(EnumChatType chatType)
		{
			switch (chatType)
			{
			case EnumChatType.CommandSuccess:
				return ColorUtil.ToRgba(255, 192, 255, 192);
			case EnumChatType.CommandError:
				return ColorUtil.ToRgba(255, 255, 192, 192);
			case EnumChatType.OwnMessage:
				return ColorUtil.ToRgba(255, 192, 192, 192);
			case EnumChatType.Notification:
				return ColorUtil.BlackArgb;
			}
			return -1;
		}

		public string Text;

		public long PostedMs;

		public int TextColor;

		public int BackgroundColor;
	}
}
