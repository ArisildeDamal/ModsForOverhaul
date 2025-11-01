using System;

public class BoxedArray
{
	public BoxedArray CheckCreated()
	{
		if (this.buffer == null)
		{
			this.buffer = new byte[32];
		}
		return this;
	}

	public virtual void Dispose()
	{
		this.buffer = null;
	}

	public byte[] buffer;
}
