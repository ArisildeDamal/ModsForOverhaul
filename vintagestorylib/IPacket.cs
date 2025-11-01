using System;

public interface IPacket
{
	void SerializeTo(CitoStream stream);
}
