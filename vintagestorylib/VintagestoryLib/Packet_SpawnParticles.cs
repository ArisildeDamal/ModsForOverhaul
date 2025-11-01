using System;

public class Packet_SpawnParticles
{
	public void SetParticlePropertyProviderClassName(string value)
	{
		this.ParticlePropertyProviderClassName = value;
	}

	public void SetData(byte[] value)
	{
		this.Data = value;
	}

	internal void InitializeValues()
	{
	}

	public string ParticlePropertyProviderClassName;

	public byte[] Data;

	public const int ParticlePropertyProviderClassNameFieldID = 1;

	public const int DataFieldID = 2;

	public int size;
}
