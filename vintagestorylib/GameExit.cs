using System;

public class GameExit
{
	public void SetExit(bool p)
	{
		this.exit = p;
	}

	public bool GetExit()
	{
		return this.exit;
	}

	internal bool exit;
}
