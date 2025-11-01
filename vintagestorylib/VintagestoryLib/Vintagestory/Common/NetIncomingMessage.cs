using System;

namespace Vintagestory.Common
{
	public class NetIncomingMessage
	{
		public static int ReadInt(byte[] readBuf)
		{
			return ((int)readBuf[0] << 24) + ((int)readBuf[1] << 16) + ((int)readBuf[2] << 8) + (int)readBuf[3];
		}

		public static void WriteInt(byte[] writeBuf, int n)
		{
			int a = (n >> 24) & 255;
			int b = (n >> 16) & 255;
			int c = (n >> 8) & 255;
			int d = n & 255;
			writeBuf[0] = (byte)a;
			writeBuf[1] = (byte)b;
			writeBuf[2] = (byte)c;
			writeBuf[3] = (byte)d;
		}

		public NetConnection SenderConnection;

		public NetworkMessageType Type;

		public byte[] message;

		public int messageLength;

		public int originalMessageLength;
	}
}
