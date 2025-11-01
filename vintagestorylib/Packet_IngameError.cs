using System;

public class Packet_IngameError
{
	public void SetCode(string value)
	{
		this.Code = value;
	}

	public void SetMessage(string value)
	{
		this.Message = value;
	}

	public string[] GetLangParams()
	{
		return this.LangParams;
	}

	public void SetLangParams(string[] value, int count, int length)
	{
		this.LangParams = value;
		this.LangParamsCount = count;
		this.LangParamsLength = length;
	}

	public void SetLangParams(string[] value)
	{
		this.LangParams = value;
		this.LangParamsCount = value.Length;
		this.LangParamsLength = value.Length;
	}

	public int GetLangParamsCount()
	{
		return this.LangParamsCount;
	}

	public void LangParamsAdd(string value)
	{
		if (this.LangParamsCount >= this.LangParamsLength)
		{
			if ((this.LangParamsLength *= 2) == 0)
			{
				this.LangParamsLength = 1;
			}
			string[] newArray = new string[this.LangParamsLength];
			for (int i = 0; i < this.LangParamsCount; i++)
			{
				newArray[i] = this.LangParams[i];
			}
			this.LangParams = newArray;
		}
		string[] langParams = this.LangParams;
		int langParamsCount = this.LangParamsCount;
		this.LangParamsCount = langParamsCount + 1;
		langParams[langParamsCount] = value;
	}

	internal void InitializeValues()
	{
	}

	public string Code;

	public string Message;

	public string[] LangParams;

	public int LangParamsCount;

	public int LangParamsLength;

	public const int CodeFieldID = 1;

	public const int MessageFieldID = 2;

	public const int LangParamsFieldID = 3;

	public int size;
}
