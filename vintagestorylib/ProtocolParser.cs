using System;
using System.IO;
using System.Text;

public class ProtocolParser
{
	public static string ReadString(CitoStream stream)
	{
		int length = ProtocolParser.ReadUInt32(stream);
		return stream.ReadString(length);
	}

	public static byte[] ReadBytes(CitoStream stream)
	{
		int length = ProtocolParser.ReadUInt32(stream);
		byte[] buffer = new byte[length];
		int r;
		for (int read = 0; read < length; read += r)
		{
			r = stream.Read(buffer, read, length - read);
			if (r == 0)
			{
				throw new InvalidDataException("Expected " + (length - read).ToString() + " got " + read.ToString());
			}
		}
		return buffer;
	}

	public static void SkipBytes(CitoStream stream)
	{
		int length = ProtocolParser.ReadUInt32(stream);
		if (stream.CanSeek())
		{
			stream.Seek(length, CitoSeekOrigin.Current);
			return;
		}
		ProtocolParser.ReadBytes(stream);
	}

	public static void WriteString(CitoStream stream, string s)
	{
		int byteCount = Encoding.UTF8.GetByteCount(s);
		ProtocolParser.WriteUInt32_(stream, byteCount);
		stream.WriteString(s, byteCount);
	}

	public static void WriteBytes(CitoStream stream, byte[] val)
	{
		ProtocolParser.WriteUInt32_(stream, val.Length);
		stream.Write(val, 0, val.Length);
	}

	public static Key ReadKey_(byte firstByte, CitoStream stream)
	{
		if (firstByte < 128)
		{
			return Key.Create((int)firstByte);
		}
		return Key.Create((int)firstByte, ProtocolParser.ReadUInt32(stream));
	}

	public static int ReadKeyAsInt(int firstByte, CitoStream stream)
	{
		int secondByte = stream.ReadByte();
		return (firstByte & 127) | (secondByte << 7);
	}

	public static void WriteKey(CitoStream stream, Key key)
	{
		ProtocolParser.WriteUInt32_(stream, key);
	}

	public static void SkipKey(CitoStream stream, Key key)
	{
		switch (key.WireType)
		{
		case 0:
			ProtocolParser.ReadSkipVarInt(stream);
			return;
		case 1:
			stream.Seek(8, CitoSeekOrigin.Current);
			return;
		case 2:
			stream.Seek(ProtocolParser.ReadUInt32(stream), CitoSeekOrigin.Current);
			return;
		case 5:
			stream.Seek(4, CitoSeekOrigin.Current);
			return;
		}
		throw new InvalidDataException("Unknown wire type: " + key.WireType.ToString() + " at stream position " + stream.Position().ToString());
	}

	public static byte[] ReadValueBytes(CitoStream stream, Key key)
	{
		int offset = 0;
		switch (key.WireType)
		{
		case 0:
			return ProtocolParser.ReadVarIntBytes(stream);
		case 1:
		{
			byte[] b = new byte[8];
			while (offset < 8)
			{
				offset += stream.Read(b, offset, 8 - offset);
			}
			return b;
		}
		case 2:
		{
			int length = ProtocolParser.ReadUInt32(stream);
			CitoMemoryStream ms = new CitoMemoryStream();
			ProtocolParser.WriteUInt32(ms, length);
			offset = ms.Position();
			int bLength = length + offset;
			byte[] b = new byte[bLength];
			for (int i = 0; i < offset; i++)
			{
				b[i] = ms.ToArray()[i];
			}
			while (offset < bLength)
			{
				offset += stream.Read(b, offset, bLength - offset);
			}
			return b;
		}
		case 5:
		{
			byte[] b = new byte[4];
			while (offset < 4)
			{
				offset += stream.Read(b, offset, 4 - offset);
			}
			return b;
		}
		}
		throw new InvalidDataException("Unknown wire type: " + key.WireType.ToString() + " at stream position " + stream.Position().ToString());
	}

	public static void ReadSkipVarInt(CitoStream stream)
	{
		for (;;)
		{
			int num = stream.ReadByte();
			if (num < 0)
			{
				break;
			}
			if ((num & 128) == 0)
			{
				return;
			}
		}
		throw new IOException("Stream ended too early");
	}

	public static byte[] ReadVarIntBytes(CitoStream stream)
	{
		byte[] buffer = new byte[10];
		int offset = 0;
		for (;;)
		{
			int b = stream.ReadByte();
			if (b < 0)
			{
				break;
			}
			buffer[offset] = (byte)b;
			offset++;
			if ((b & 128) == 0)
			{
				goto IL_0043;
			}
			if (offset >= buffer.Length)
			{
				goto Block_3;
			}
		}
		throw new IOException("Stream ended too early");
		Block_3:
		throw new InvalidDataException("VarInt too long, more than 10 bytes");
		IL_0043:
		byte[] ret = new byte[offset];
		for (int i = 0; i < offset; i++)
		{
			ret[i] = buffer[i];
		}
		return ret;
	}

	public static int ReadInt32(CitoStream stream)
	{
		return ProtocolParser.ReadUInt32(stream);
	}

	public static void WriteInt32(CitoStream stream, int val)
	{
		ProtocolParser.WriteUInt32(stream, val);
	}

	public static int ReadZInt32(CitoStream stream)
	{
		int val = ProtocolParser.ReadUInt32(stream);
		return (val >> 1) ^ (val << 31 >> 31);
	}

	public static void WriteZInt32(CitoStream stream, int val)
	{
		ProtocolParser.WriteUInt32_(stream, (val << 1) ^ (val >> 31));
	}

	public static long ReadInt64(CitoStream stream)
	{
		return ProtocolParser.ReadUInt64(stream);
	}

	public static void WriteInt64(CitoStream stream, long val)
	{
		ProtocolParser.WriteUInt64(stream, val);
	}

	public static long ReadZInt64(CitoStream stream)
	{
		long val = ProtocolParser.ReadUInt64(stream);
		return (val >> 1) ^ (val << 63 >> 63);
	}

	public static void WriteZInt64(CitoStream stream, long val)
	{
		ProtocolParser.WriteUInt64(stream, (val << 1) ^ (val >> 63));
	}

	public static int ReadUInt32(CitoStream stream)
	{
		int val = 0;
		for (int i = 0; i < 5; i++)
		{
			int b = stream.ReadByte();
			if (b < 0)
			{
				throw new IOException("Stream ended too early");
			}
			if (i == 4 && b > 15)
			{
				throw new InvalidDataException("Got larger VarInt than 32 bit unsigned");
			}
			if ((b & 128) == 0)
			{
				return val | (b << 7 * i);
			}
			val |= (b & 127) << 7 * i;
		}
		throw new InvalidDataException("Got larger VarInt than 32 bit unsigned");
	}

	public static void WriteUInt32(CitoStream stream, int val)
	{
		if ((val & -16384) == 0)
		{
			stream.WriteSmallInt(val);
			return;
		}
		byte b;
		for (;;)
		{
			b = (byte)(val & 127);
			val = (val >> 7) & 33554431;
			if (val == 0)
			{
				break;
			}
			stream.WriteByte(b + 128);
		}
		stream.WriteByte(b);
	}

	public static void WriteUInt32_(CitoStream stream, int val)
	{
		if (val <= 16383)
		{
			stream.WriteSmallInt(val);
			return;
		}
		byte b;
		for (;;)
		{
			b = (byte)(val & 127);
			val >>= 7;
			if (val == 0)
			{
				break;
			}
			stream.WriteByte(b + 128);
		}
		stream.WriteByte(b);
	}

	public static long ReadUInt64(CitoStream stream)
	{
		long val = 0L;
		for (int i = 0; i < 10; i++)
		{
			int b = stream.ReadByte();
			if (b < 0)
			{
				throw new IOException("Stream ended too early");
			}
			if (i == 9 && b > 1)
			{
				throw new InvalidDataException("Got larger VarInt than 64 bit unsigned");
			}
			if ((b & 128) == 0)
			{
				return val | ((long)b << 7 * i);
			}
			val |= ((long)b & 127L) << 7 * i;
		}
		throw new InvalidDataException("Got larger VarInt than 64 bit unsigned");
	}

	public static void WriteUInt64(CitoStream stream, long val)
	{
		if ((val & -16384L) == 0L)
		{
			stream.WriteSmallInt((int)val);
			return;
		}
		byte b;
		for (;;)
		{
			b = (byte)(val & 127L);
			val = (val >> 7) & 144115188075855871L;
			if (val == 0L)
			{
				break;
			}
			stream.WriteByte(b + 128);
		}
		stream.WriteByte(b);
	}

	public static bool ReadBool(CitoStream stream)
	{
		int b = stream.ReadByte();
		if (b == 1)
		{
			return true;
		}
		if (b == 0)
		{
			return false;
		}
		if (b < 0)
		{
			throw new IOException("Stream ended too early");
		}
		throw new InvalidDataException("Invalid boolean value");
	}

	public static void WriteBool(CitoStream stream, bool val)
	{
		byte ret = 0;
		if (val)
		{
			ret = 1;
		}
		stream.WriteByte(ret);
	}

	public static int PeekPacketId(byte[] data)
	{
		if (data.Length == 0)
		{
			return -1;
		}
		int keyInt = (int)data[0];
		if (keyInt >= 128)
		{
			if (data.Length == 1)
			{
				return -1;
			}
			int secondByte = (int)data[1];
			if (secondByte >= 128)
			{
				return -1;
			}
			keyInt = (keyInt & 127) | (secondByte << 7);
		}
		if (!Wire.IsValid(keyInt % 8))
		{
			return -1;
		}
		return keyInt;
	}

	public static int GetSize(int v)
	{
		if (v < 128)
		{
			if (v >= 0)
			{
				return 1;
			}
			return 5;
		}
		else
		{
			if (v < 16384)
			{
				return 2;
			}
			if (v < 2097152)
			{
				return 3;
			}
			if (v < 268435456)
			{
				return 4;
			}
			return 5;
		}
	}

	public static int GetSize(long v)
	{
		if (v < 128L)
		{
			if (v >= 0L)
			{
				return 1;
			}
			return 10;
		}
		else
		{
			if (v < 16384L)
			{
				return 2;
			}
			if (v < 2097152L)
			{
				return 3;
			}
			if (v < 268435456L)
			{
				return 4;
			}
			if (v < 34359738368L)
			{
				return 5;
			}
			if (v < 4398046511104L)
			{
				return 6;
			}
			if (v < 562949953421312L)
			{
				return 7;
			}
			if (v < 72057594037927936L)
			{
				return 8;
			}
			return 9;
		}
	}

	public static int GetSize(byte[] data)
	{
		return data.Length + ProtocolParser.GetSize(data.Length);
	}

	public static int GetSize(string s)
	{
		int byteCount = Encoding.UTF8.GetByteCount(s);
		return ProtocolParser.GetSize(byteCount) + byteCount;
	}

	private const int byteHighestBit = 128;

	private const int BitMaskLogicalRightShiftBy7 = 33554431;

	private const long BitMaskLogicalRightShiftBy7L = 144115188075855871L;

	private const int BitMask14bits = -16384;
}
