using System;

public class KeyValue
{
	public static KeyValue Create(Key key, byte[] value)
	{
		return new KeyValue
		{
			Key_ = key,
			Value = value
		};
	}

	private Key Key_;

	private byte[] Value;
}
