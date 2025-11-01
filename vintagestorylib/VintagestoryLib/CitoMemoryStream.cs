using System;
using System.Text;
using Vintagestory.API.Common;

public class CitoMemoryStream : CitoStream
{
	public CitoMemoryStream()
	{
		this.bufferlength = 16;
		this.buffer_ = new byte[16];
		this.position_ = 0;
	}

	public CitoMemoryStream(BoxedArray reusableBuffer)
	{
		this.ba = reusableBuffer.CheckCreated();
		this.bufferlength = this.ba.buffer.Length;
		this.buffer_ = this.ba.buffer;
		this.position_ = 0;
	}

	public CitoMemoryStream(byte[] buffer, int length)
	{
		this.bufferlength = length;
		this.buffer_ = buffer;
		this.position_ = 0;
	}

	public override int Position()
	{
		return this.position_;
	}

	internal int GetLength()
	{
		return this.bufferlength;
	}

	internal void SetLength(int value)
	{
		this.bufferlength = Math.Min(value, this.buffer_.Length);
	}

	public byte[] ToArray()
	{
		return this.buffer_;
	}

	public byte[] GetBuffer()
	{
		return this.buffer_;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int ourOffset = this.position_;
		int maxCount = this.bufferlength - ourOffset;
		if (count > maxCount)
		{
			count = maxCount;
		}
		byte[] ourBuffer = this.buffer_;
		for (int i = 0; i < count; i++)
		{
			buffer[offset + i] = ourBuffer[ourOffset + i];
		}
		this.position_ = ourOffset + count;
		return count;
	}

	public override bool CanSeek()
	{
		return false;
	}

	public override void Seek(int length, CitoSeekOrigin seekOrigin)
	{
		if (seekOrigin == CitoSeekOrigin.Current)
		{
			this.position_ += length;
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (count <= 0)
		{
			return;
		}
		this.EnsureCapacityFor(count);
		if (count > 200)
		{
			Array.Copy(buffer, offset, this.buffer_, this.position_, count);
			this.position_ += count;
			return;
		}
		int i = offset;
		count += offset;
		int dWordBoundary = (i + 3) / 4 * 4;
		byte[] ourBuffer = this.buffer_;
		int ourPosition = this.position_;
		while (i < dWordBoundary)
		{
			ourBuffer[ourPosition++] = buffer[i++];
		}
		int fastLoopLimit = count / 4 * 4;
		i += 3;
		while (i < buffer.Length && i < fastLoopLimit)
		{
			ourBuffer[ourPosition] = buffer[i - 3];
			ourBuffer[ourPosition + 1] = buffer[i - 2];
			ourBuffer[ourPosition + 2] = buffer[i - 1];
			ourBuffer[ourPosition + 3] = buffer[i];
			ourPosition += 4;
			i += 4;
		}
		i -= 3;
		while (i < count)
		{
			ourBuffer[ourPosition++] = buffer[i++];
		}
		this.position_ = ourPosition;
	}

	private void EnsureCapacityFor(int count)
	{
		if (this.position_ + count > this.bufferlength)
		{
			int newSize = this.bufferlength * 2;
			if (newSize < this.position_ + count)
			{
				newSize = this.position_ + count;
			}
			this.buffer_ = this.FastCopy(this.buffer_, this.position_, newSize);
			this.bufferlength = newSize;
		}
	}

	public override void Seek_(int p, CitoSeekOrigin seekOrigin)
	{
	}

	public override int ReadByte()
	{
		if (this.position_ >= this.bufferlength)
		{
			return -1;
		}
		byte[] array = this.buffer_;
		int num = this.position_;
		this.position_ = num + 1;
		return array[num];
	}

	public override void WriteByte(byte p)
	{
		if (this.position_ >= this.bufferlength)
		{
			this.buffer_ = this.FastCopy(this.buffer_, this.position_, this.bufferlength *= 2);
		}
		byte[] array = this.buffer_;
		int num = this.position_;
		this.position_ = num + 1;
		array[num] = p;
	}

	public override void WriteSmallInt(int v)
	{
		if (v < 128)
		{
			this.WriteByte((byte)v);
			return;
		}
		if (this.position_ >= this.bufferlength - 1)
		{
			this.buffer_ = this.FastCopy(this.buffer_, this.position_, this.bufferlength *= 2);
		}
		byte[] array = this.buffer_;
		int num = this.position_;
		this.position_ = num + 1;
		array[num] = (byte)(v | 128);
		byte[] array2 = this.buffer_;
		num = this.position_;
		this.position_ = num + 1;
		array2[num] = (byte)(v >> 7);
	}

	public override void WriteKey(byte field, byte wiretype)
	{
		this.WriteSmallInt(new Key(field, wiretype));
	}

	private byte[] FastCopy(byte[] buffer, int oldLength, int newSize)
	{
		byte[] buffer2 = new byte[newSize];
		if (oldLength > 256)
		{
			Array.Copy(buffer, 0, buffer2, 0, oldLength);
		}
		else
		{
			int i = 0;
			if (oldLength >= 4)
			{
				int fastLoopLength = oldLength / 4 * 4;
				i = 3;
				while (i < buffer.Length && i < fastLoopLength)
				{
					buffer2[i - 3] = buffer[i - 3];
					buffer2[i - 2] = buffer[i - 2];
					buffer2[i - 1] = buffer[i - 1];
					buffer2[i] = buffer[i];
					i += 4;
				}
				i -= 3;
			}
			while (i < oldLength)
			{
				buffer2[i] = buffer[i];
				i++;
			}
		}
		if (this.ba != null)
		{
			this.ba.buffer = buffer2;
		}
		return buffer2;
	}

	public override string ReadString(int byteCount)
	{
		string @string = Encoding.UTF8.GetString(this.buffer_, this.position_, byteCount);
		this.position_ += byteCount;
		return @string;
	}

	public override void WriteString(string s, int byteCount)
	{
		this.EnsureCapacityFor(byteCount);
		this.position_ += Encoding.UTF8.GetBytes(s, 0, s.Length, this.buffer_, this.position_);
	}

	public static void NetworkTest(ILogger Logger)
	{
		int a = int.MaxValue;
		int b = int.MinValue;
		int ba = 129;
		int bb = 2000000;
		int bc = -2000000;
		int c = -1;
		uint d = uint.MaxValue;
		long e = long.MaxValue;
		long ea = 129L;
		long eb = 2000000L;
		long ec = -2000000L;
		long f = long.MinValue;
		long g = -1L;
		ulong h = ulong.MaxValue;
		CitoMemoryStream ms = new CitoMemoryStream();
		int position = ms.Position();
		bool sizesOK = true;
		ProtocolParser.WriteUInt32(ms, a);
		if (ms.Position() - position != ProtocolParser.GetSize(a))
		{
			sizesOK = false;
			Logger.Notification("wrongsize a");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt32(ms, b);
		if (ms.Position() - position != ProtocolParser.GetSize(b))
		{
			sizesOK = false;
			Logger.Notification("wrongsize b");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt32(ms, ba);
		if (ms.Position() - position != ProtocolParser.GetSize(ba))
		{
			sizesOK = false;
			Logger.Notification("wrongsize ba");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt32(ms, bb);
		if (ms.Position() - position != ProtocolParser.GetSize(bb))
		{
			sizesOK = false;
			Logger.Notification("wrongsize bb");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt32(ms, bc);
		if (ms.Position() - position != ProtocolParser.GetSize(bc))
		{
			sizesOK = false;
			Logger.Notification("wrongsize bc");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt32(ms, c);
		if (ms.Position() - position != ProtocolParser.GetSize(c))
		{
			sizesOK = false;
			Logger.Notification("wrongsize c");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt32(ms, (int)d);
		if (ms.Position() - position != ProtocolParser.GetSize((int)d))
		{
			sizesOK = false;
			Logger.Notification("wrongsize d");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt64(ms, e);
		if (ms.Position() - position != ProtocolParser.GetSize(e))
		{
			sizesOK = false;
			Logger.Notification("wrongsize e");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt64(ms, ea);
		if (ms.Position() - position != ProtocolParser.GetSize(ea))
		{
			sizesOK = false;
			Logger.Notification("wrongsize ea");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt64(ms, eb);
		if (ms.Position() - position != ProtocolParser.GetSize(eb))
		{
			sizesOK = false;
			Logger.Notification("wrongsize eb");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt64(ms, ec);
		if (ms.Position() - position != ProtocolParser.GetSize(ec))
		{
			sizesOK = false;
			Logger.Notification("wrongsize ec");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt64(ms, f);
		if (ms.Position() - position != ProtocolParser.GetSize(f))
		{
			sizesOK = false;
			Logger.Notification("wrongsize f");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt64(ms, g);
		if (ms.Position() - position != ProtocolParser.GetSize(g))
		{
			sizesOK = false;
			Logger.Notification("wrongsize g");
		}
		position = ms.Position();
		ProtocolParser.WriteUInt64(ms, (long)h);
		if (ms.Position() - position != ProtocolParser.GetSize((long)h))
		{
			sizesOK = false;
			Logger.Notification("wrongsize h");
		}
		position = ms.Position();
		ProtocolParser.WriteString(ms, "testString");
		if (ms.Position() - position != ProtocolParser.GetSize("testString"))
		{
			sizesOK = false;
			Logger.Notification("wrongsize string");
		}
		ms.position_ = 0;
		Logger.Notification("Test positive int.   Wrote " + a.ToString() + "  Read " + ProtocolParser.ReadUInt32(ms).ToString());
		Logger.Notification("Test negative int.   Wrote " + b.ToString() + "  Read " + ProtocolParser.ReadUInt32(ms).ToString());
		Logger.Notification("Test positive int.   Wrote " + ba.ToString() + "  Read " + ProtocolParser.ReadUInt32(ms).ToString());
		Logger.Notification("Test positive int.   Wrote " + bb.ToString() + "  Read " + ProtocolParser.ReadUInt32(ms).ToString());
		Logger.Notification("Test negative int.   Wrote " + bc.ToString() + "  Read " + ProtocolParser.ReadUInt32(ms).ToString());
		Logger.Notification("Test negative int.   Wrote " + c.ToString() + "  Read " + ProtocolParser.ReadUInt32(ms).ToString());
		Logger.Notification("Test unsigned uint.  Wrote " + d.ToString() + "  Read " + ((uint)ProtocolParser.ReadUInt32(ms)).ToString());
		Logger.Notification("Test positive long.  Wrote " + e.ToString() + "  Read " + ProtocolParser.ReadUInt64(ms).ToString());
		Logger.Notification("Test positive long.  Wrote " + ea.ToString() + "  Read " + ProtocolParser.ReadUInt64(ms).ToString());
		Logger.Notification("Test positive long.  Wrote " + eb.ToString() + "  Read " + ProtocolParser.ReadUInt64(ms).ToString());
		Logger.Notification("Test negative long.  Wrote " + ec.ToString() + "  Read " + ProtocolParser.ReadUInt64(ms).ToString());
		Logger.Notification("Test negative long.  Wrote " + f.ToString() + "  Read " + ProtocolParser.ReadUInt64(ms).ToString());
		Logger.Notification("Test negative long.  Wrote " + g.ToString() + "  Read " + ProtocolParser.ReadUInt64(ms).ToString());
		Logger.Notification("Test unsigned ulong.  Wrote " + h.ToString() + "  Read " + ((ulong)ProtocolParser.ReadUInt64(ms)).ToString());
		Logger.Notification("Test string.  Wrote 'testString'  Read '" + ProtocolParser.ReadString(ms) + "'");
		Logger.Notification(sizesOK ? "All sizes were OK" : "Size error!");
	}

	private const int byteHighestBit = 128;

	private byte[] buffer_;

	private int bufferlength;

	private int position_;

	private readonly BoxedArray ba;
}
