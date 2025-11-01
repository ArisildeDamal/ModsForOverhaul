using System;

public class Packet_EntityType
{
	public void SetCode(string value)
	{
		this.Code = value;
	}

	public void SetClass(string value)
	{
		this.Class = value;
	}

	public int[] GetTags()
	{
		return this.Tags;
	}

	public void SetTags(int[] value, int count, int length)
	{
		this.Tags = value;
		this.TagsCount = count;
		this.TagsLength = length;
	}

	public void SetTags(int[] value)
	{
		this.Tags = value;
		this.TagsCount = value.Length;
		this.TagsLength = value.Length;
	}

	public int GetTagsCount()
	{
		return this.TagsCount;
	}

	public void TagsAdd(int value)
	{
		if (this.TagsCount >= this.TagsLength)
		{
			if ((this.TagsLength *= 2) == 0)
			{
				this.TagsLength = 1;
			}
			int[] newArray = new int[this.TagsLength];
			for (int i = 0; i < this.TagsCount; i++)
			{
				newArray[i] = this.Tags[i];
			}
			this.Tags = newArray;
		}
		int[] tags = this.Tags;
		int tagsCount = this.TagsCount;
		this.TagsCount = tagsCount + 1;
		tags[tagsCount] = value;
	}

	public void SetRenderer(string value)
	{
		this.Renderer = value;
	}

	public void SetHabitat(int value)
	{
		this.Habitat = value;
	}

	public void SetDrops(byte[] value)
	{
		this.Drops = value;
	}

	public void SetShape(Packet_CompositeShape value)
	{
		this.Shape = value;
	}

	public Packet_Behavior[] GetBehaviors()
	{
		return this.Behaviors;
	}

	public void SetBehaviors(Packet_Behavior[] value, int count, int length)
	{
		this.Behaviors = value;
		this.BehaviorsCount = count;
		this.BehaviorsLength = length;
	}

	public void SetBehaviors(Packet_Behavior[] value)
	{
		this.Behaviors = value;
		this.BehaviorsCount = value.Length;
		this.BehaviorsLength = value.Length;
	}

	public int GetBehaviorsCount()
	{
		return this.BehaviorsCount;
	}

	public void BehaviorsAdd(Packet_Behavior value)
	{
		if (this.BehaviorsCount >= this.BehaviorsLength)
		{
			if ((this.BehaviorsLength *= 2) == 0)
			{
				this.BehaviorsLength = 1;
			}
			Packet_Behavior[] newArray = new Packet_Behavior[this.BehaviorsLength];
			for (int i = 0; i < this.BehaviorsCount; i++)
			{
				newArray[i] = this.Behaviors[i];
			}
			this.Behaviors = newArray;
		}
		Packet_Behavior[] behaviors = this.Behaviors;
		int behaviorsCount = this.BehaviorsCount;
		this.BehaviorsCount = behaviorsCount + 1;
		behaviors[behaviorsCount] = value;
	}

	public void SetCollisionBoxLength(int value)
	{
		this.CollisionBoxLength = value;
	}

	public void SetCollisionBoxHeight(int value)
	{
		this.CollisionBoxHeight = value;
	}

	public void SetDeadCollisionBoxLength(int value)
	{
		this.DeadCollisionBoxLength = value;
	}

	public void SetDeadCollisionBoxHeight(int value)
	{
		this.DeadCollisionBoxHeight = value;
	}

	public void SetSelectionBoxLength(int value)
	{
		this.SelectionBoxLength = value;
	}

	public void SetSelectionBoxHeight(int value)
	{
		this.SelectionBoxHeight = value;
	}

	public void SetDeadSelectionBoxLength(int value)
	{
		this.DeadSelectionBoxLength = value;
	}

	public void SetDeadSelectionBoxHeight(int value)
	{
		this.DeadSelectionBoxHeight = value;
	}

	public void SetAttributes(string value)
	{
		this.Attributes = value;
	}

	public string[] GetSoundKeys()
	{
		return this.SoundKeys;
	}

	public void SetSoundKeys(string[] value, int count, int length)
	{
		this.SoundKeys = value;
		this.SoundKeysCount = count;
		this.SoundKeysLength = length;
	}

	public void SetSoundKeys(string[] value)
	{
		this.SoundKeys = value;
		this.SoundKeysCount = value.Length;
		this.SoundKeysLength = value.Length;
	}

	public int GetSoundKeysCount()
	{
		return this.SoundKeysCount;
	}

	public void SoundKeysAdd(string value)
	{
		if (this.SoundKeysCount >= this.SoundKeysLength)
		{
			if ((this.SoundKeysLength *= 2) == 0)
			{
				this.SoundKeysLength = 1;
			}
			string[] newArray = new string[this.SoundKeysLength];
			for (int i = 0; i < this.SoundKeysCount; i++)
			{
				newArray[i] = this.SoundKeys[i];
			}
			this.SoundKeys = newArray;
		}
		string[] soundKeys = this.SoundKeys;
		int soundKeysCount = this.SoundKeysCount;
		this.SoundKeysCount = soundKeysCount + 1;
		soundKeys[soundKeysCount] = value;
	}

	public string[] GetSoundNames()
	{
		return this.SoundNames;
	}

	public void SetSoundNames(string[] value, int count, int length)
	{
		this.SoundNames = value;
		this.SoundNamesCount = count;
		this.SoundNamesLength = length;
	}

	public void SetSoundNames(string[] value)
	{
		this.SoundNames = value;
		this.SoundNamesCount = value.Length;
		this.SoundNamesLength = value.Length;
	}

	public int GetSoundNamesCount()
	{
		return this.SoundNamesCount;
	}

	public void SoundNamesAdd(string value)
	{
		if (this.SoundNamesCount >= this.SoundNamesLength)
		{
			if ((this.SoundNamesLength *= 2) == 0)
			{
				this.SoundNamesLength = 1;
			}
			string[] newArray = new string[this.SoundNamesLength];
			for (int i = 0; i < this.SoundNamesCount; i++)
			{
				newArray[i] = this.SoundNames[i];
			}
			this.SoundNames = newArray;
		}
		string[] soundNames = this.SoundNames;
		int soundNamesCount = this.SoundNamesCount;
		this.SoundNamesCount = soundNamesCount + 1;
		soundNames[soundNamesCount] = value;
	}

	public void SetIdleSoundChance(int value)
	{
		this.IdleSoundChance = value;
	}

	public void SetIdleSoundRange(int value)
	{
		this.IdleSoundRange = value;
	}

	public string[] GetTextureCodes()
	{
		return this.TextureCodes;
	}

	public void SetTextureCodes(string[] value, int count, int length)
	{
		this.TextureCodes = value;
		this.TextureCodesCount = count;
		this.TextureCodesLength = length;
	}

	public void SetTextureCodes(string[] value)
	{
		this.TextureCodes = value;
		this.TextureCodesCount = value.Length;
		this.TextureCodesLength = value.Length;
	}

	public int GetTextureCodesCount()
	{
		return this.TextureCodesCount;
	}

	public void TextureCodesAdd(string value)
	{
		if (this.TextureCodesCount >= this.TextureCodesLength)
		{
			if ((this.TextureCodesLength *= 2) == 0)
			{
				this.TextureCodesLength = 1;
			}
			string[] newArray = new string[this.TextureCodesLength];
			for (int i = 0; i < this.TextureCodesCount; i++)
			{
				newArray[i] = this.TextureCodes[i];
			}
			this.TextureCodes = newArray;
		}
		string[] textureCodes = this.TextureCodes;
		int textureCodesCount = this.TextureCodesCount;
		this.TextureCodesCount = textureCodesCount + 1;
		textureCodes[textureCodesCount] = value;
	}

	public Packet_CompositeTexture[] GetCompositeTextures()
	{
		return this.CompositeTextures;
	}

	public void SetCompositeTextures(Packet_CompositeTexture[] value, int count, int length)
	{
		this.CompositeTextures = value;
		this.CompositeTexturesCount = count;
		this.CompositeTexturesLength = length;
	}

	public void SetCompositeTextures(Packet_CompositeTexture[] value)
	{
		this.CompositeTextures = value;
		this.CompositeTexturesCount = value.Length;
		this.CompositeTexturesLength = value.Length;
	}

	public int GetCompositeTexturesCount()
	{
		return this.CompositeTexturesCount;
	}

	public void CompositeTexturesAdd(Packet_CompositeTexture value)
	{
		if (this.CompositeTexturesCount >= this.CompositeTexturesLength)
		{
			if ((this.CompositeTexturesLength *= 2) == 0)
			{
				this.CompositeTexturesLength = 1;
			}
			Packet_CompositeTexture[] newArray = new Packet_CompositeTexture[this.CompositeTexturesLength];
			for (int i = 0; i < this.CompositeTexturesCount; i++)
			{
				newArray[i] = this.CompositeTextures[i];
			}
			this.CompositeTextures = newArray;
		}
		Packet_CompositeTexture[] compositeTextures = this.CompositeTextures;
		int compositeTexturesCount = this.CompositeTexturesCount;
		this.CompositeTexturesCount = compositeTexturesCount + 1;
		compositeTextures[compositeTexturesCount] = value;
	}

	public void SetSize(int value)
	{
		this.Size = value;
	}

	public void SetEyeHeight(int value)
	{
		this.EyeHeight = value;
	}

	public void SetSwimmingEyeHeight(int value)
	{
		this.SwimmingEyeHeight = value;
	}

	public void SetWeight(int value)
	{
		this.Weight = value;
	}

	public void SetCanClimb(int value)
	{
		this.CanClimb = value;
	}

	public void SetAnimationMetaData(byte[] value)
	{
		this.AnimationMetaData = value;
	}

	public void SetKnockbackResistance(int value)
	{
		this.KnockbackResistance = value;
	}

	public void SetGlowLevel(int value)
	{
		this.GlowLevel = value;
	}

	public void SetCanClimbAnywhere(int value)
	{
		this.CanClimbAnywhere = value;
	}

	public void SetClimbTouchDistance(int value)
	{
		this.ClimbTouchDistance = value;
	}

	public void SetRotateModelOnClimb(int value)
	{
		this.RotateModelOnClimb = value;
	}

	public void SetFallDamage(int value)
	{
		this.FallDamage = value;
	}

	public void SetFallDamageMultiplier(int value)
	{
		this.FallDamageMultiplier = value;
	}

	public Packet_VariantPart[] GetVariant()
	{
		return this.Variant;
	}

	public void SetVariant(Packet_VariantPart[] value, int count, int length)
	{
		this.Variant = value;
		this.VariantCount = count;
		this.VariantLength = length;
	}

	public void SetVariant(Packet_VariantPart[] value)
	{
		this.Variant = value;
		this.VariantCount = value.Length;
		this.VariantLength = value.Length;
	}

	public int GetVariantCount()
	{
		return this.VariantCount;
	}

	public void VariantAdd(Packet_VariantPart value)
	{
		if (this.VariantCount >= this.VariantLength)
		{
			if ((this.VariantLength *= 2) == 0)
			{
				this.VariantLength = 1;
			}
			Packet_VariantPart[] newArray = new Packet_VariantPart[this.VariantLength];
			for (int i = 0; i < this.VariantCount; i++)
			{
				newArray[i] = this.Variant[i];
			}
			this.Variant = newArray;
		}
		Packet_VariantPart[] variant = this.Variant;
		int variantCount = this.VariantCount;
		this.VariantCount = variantCount + 1;
		variant[variantCount] = value;
	}

	public void SetSizeGrowthFactor(int value)
	{
		this.SizeGrowthFactor = value;
	}

	public void SetPitchStep(int value)
	{
		this.PitchStep = value;
	}

	public void SetColor(string value)
	{
		this.Color = value;
	}

	internal void InitializeValues()
	{
	}

	public string Code;

	public string Class;

	public int[] Tags;

	public int TagsCount;

	public int TagsLength;

	public string Renderer;

	public int Habitat;

	public byte[] Drops;

	public Packet_CompositeShape Shape;

	public Packet_Behavior[] Behaviors;

	public int BehaviorsCount;

	public int BehaviorsLength;

	public int CollisionBoxLength;

	public int CollisionBoxHeight;

	public int DeadCollisionBoxLength;

	public int DeadCollisionBoxHeight;

	public int SelectionBoxLength;

	public int SelectionBoxHeight;

	public int DeadSelectionBoxLength;

	public int DeadSelectionBoxHeight;

	public string Attributes;

	public string[] SoundKeys;

	public int SoundKeysCount;

	public int SoundKeysLength;

	public string[] SoundNames;

	public int SoundNamesCount;

	public int SoundNamesLength;

	public int IdleSoundChance;

	public int IdleSoundRange;

	public string[] TextureCodes;

	public int TextureCodesCount;

	public int TextureCodesLength;

	public Packet_CompositeTexture[] CompositeTextures;

	public int CompositeTexturesCount;

	public int CompositeTexturesLength;

	public int Size;

	public int EyeHeight;

	public int SwimmingEyeHeight;

	public int Weight;

	public int CanClimb;

	public byte[] AnimationMetaData;

	public int KnockbackResistance;

	public int GlowLevel;

	public int CanClimbAnywhere;

	public int ClimbTouchDistance;

	public int RotateModelOnClimb;

	public int FallDamage;

	public int FallDamageMultiplier;

	public Packet_VariantPart[] Variant;

	public int VariantCount;

	public int VariantLength;

	public int SizeGrowthFactor;

	public int PitchStep;

	public string Color;

	public const int CodeFieldID = 1;

	public const int ClassFieldID = 2;

	public const int TagsFieldID = 40;

	public const int RendererFieldID = 3;

	public const int HabitatFieldID = 4;

	public const int DropsFieldID = 25;

	public const int ShapeFieldID = 11;

	public const int BehaviorsFieldID = 5;

	public const int CollisionBoxLengthFieldID = 6;

	public const int CollisionBoxHeightFieldID = 7;

	public const int DeadCollisionBoxLengthFieldID = 26;

	public const int DeadCollisionBoxHeightFieldID = 27;

	public const int SelectionBoxLengthFieldID = 32;

	public const int SelectionBoxHeightFieldID = 33;

	public const int DeadSelectionBoxLengthFieldID = 34;

	public const int DeadSelectionBoxHeightFieldID = 35;

	public const int AttributesFieldID = 8;

	public const int SoundKeysFieldID = 9;

	public const int SoundNamesFieldID = 10;

	public const int IdleSoundChanceFieldID = 14;

	public const int IdleSoundRangeFieldID = 37;

	public const int TextureCodesFieldID = 12;

	public const int CompositeTexturesFieldID = 13;

	public const int SizeFieldID = 15;

	public const int EyeHeightFieldID = 16;

	public const int SwimmingEyeHeightFieldID = 36;

	public const int WeightFieldID = 29;

	public const int CanClimbFieldID = 17;

	public const int AnimationMetaDataFieldID = 18;

	public const int KnockbackResistanceFieldID = 19;

	public const int GlowLevelFieldID = 20;

	public const int CanClimbAnywhereFieldID = 21;

	public const int ClimbTouchDistanceFieldID = 22;

	public const int RotateModelOnClimbFieldID = 23;

	public const int FallDamageFieldID = 24;

	public const int FallDamageMultiplierFieldID = 39;

	public const int VariantFieldID = 28;

	public const int SizeGrowthFactorFieldID = 30;

	public const int PitchStepFieldID = 31;

	public const int ColorFieldID = 38;

	public int size;
}
