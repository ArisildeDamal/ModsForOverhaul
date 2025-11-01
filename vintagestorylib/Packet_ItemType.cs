using System;

public class Packet_ItemType
{
	public void SetItemId(int value)
	{
		this.ItemId = value;
	}

	public void SetMaxStackSize(int value)
	{
		this.MaxStackSize = value;
	}

	public void SetCode(string value)
	{
		this.Code = value;
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

	public void SetDurability(int value)
	{
		this.Durability = value;
	}

	public int[] GetMiningmaterial()
	{
		return this.Miningmaterial;
	}

	public void SetMiningmaterial(int[] value, int count, int length)
	{
		this.Miningmaterial = value;
		this.MiningmaterialCount = count;
		this.MiningmaterialLength = length;
	}

	public void SetMiningmaterial(int[] value)
	{
		this.Miningmaterial = value;
		this.MiningmaterialCount = value.Length;
		this.MiningmaterialLength = value.Length;
	}

	public int GetMiningmaterialCount()
	{
		return this.MiningmaterialCount;
	}

	public void MiningmaterialAdd(int value)
	{
		if (this.MiningmaterialCount >= this.MiningmaterialLength)
		{
			if ((this.MiningmaterialLength *= 2) == 0)
			{
				this.MiningmaterialLength = 1;
			}
			int[] newArray = new int[this.MiningmaterialLength];
			for (int i = 0; i < this.MiningmaterialCount; i++)
			{
				newArray[i] = this.Miningmaterial[i];
			}
			this.Miningmaterial = newArray;
		}
		int[] miningmaterial = this.Miningmaterial;
		int miningmaterialCount = this.MiningmaterialCount;
		this.MiningmaterialCount = miningmaterialCount + 1;
		miningmaterial[miningmaterialCount] = value;
	}

	public int[] GetMiningmaterialspeed()
	{
		return this.Miningmaterialspeed;
	}

	public void SetMiningmaterialspeed(int[] value, int count, int length)
	{
		this.Miningmaterialspeed = value;
		this.MiningmaterialspeedCount = count;
		this.MiningmaterialspeedLength = length;
	}

	public void SetMiningmaterialspeed(int[] value)
	{
		this.Miningmaterialspeed = value;
		this.MiningmaterialspeedCount = value.Length;
		this.MiningmaterialspeedLength = value.Length;
	}

	public int GetMiningmaterialspeedCount()
	{
		return this.MiningmaterialspeedCount;
	}

	public void MiningmaterialspeedAdd(int value)
	{
		if (this.MiningmaterialspeedCount >= this.MiningmaterialspeedLength)
		{
			if ((this.MiningmaterialspeedLength *= 2) == 0)
			{
				this.MiningmaterialspeedLength = 1;
			}
			int[] newArray = new int[this.MiningmaterialspeedLength];
			for (int i = 0; i < this.MiningmaterialspeedCount; i++)
			{
				newArray[i] = this.Miningmaterialspeed[i];
			}
			this.Miningmaterialspeed = newArray;
		}
		int[] miningmaterialspeed = this.Miningmaterialspeed;
		int miningmaterialspeedCount = this.MiningmaterialspeedCount;
		this.MiningmaterialspeedCount = miningmaterialspeedCount + 1;
		miningmaterialspeed[miningmaterialspeedCount] = value;
	}

	public int[] GetDamagedby()
	{
		return this.Damagedby;
	}

	public void SetDamagedby(int[] value, int count, int length)
	{
		this.Damagedby = value;
		this.DamagedbyCount = count;
		this.DamagedbyLength = length;
	}

	public void SetDamagedby(int[] value)
	{
		this.Damagedby = value;
		this.DamagedbyCount = value.Length;
		this.DamagedbyLength = value.Length;
	}

	public int GetDamagedbyCount()
	{
		return this.DamagedbyCount;
	}

	public void DamagedbyAdd(int value)
	{
		if (this.DamagedbyCount >= this.DamagedbyLength)
		{
			if ((this.DamagedbyLength *= 2) == 0)
			{
				this.DamagedbyLength = 1;
			}
			int[] newArray = new int[this.DamagedbyLength];
			for (int i = 0; i < this.DamagedbyCount; i++)
			{
				newArray[i] = this.Damagedby[i];
			}
			this.Damagedby = newArray;
		}
		int[] damagedby = this.Damagedby;
		int damagedbyCount = this.DamagedbyCount;
		this.DamagedbyCount = damagedbyCount + 1;
		damagedby[damagedbyCount] = value;
	}

	public void SetCreativeInventoryStacks(byte[] value)
	{
		this.CreativeInventoryStacks = value;
	}

	public string[] GetCreativeInventoryTabs()
	{
		return this.CreativeInventoryTabs;
	}

	public void SetCreativeInventoryTabs(string[] value, int count, int length)
	{
		this.CreativeInventoryTabs = value;
		this.CreativeInventoryTabsCount = count;
		this.CreativeInventoryTabsLength = length;
	}

	public void SetCreativeInventoryTabs(string[] value)
	{
		this.CreativeInventoryTabs = value;
		this.CreativeInventoryTabsCount = value.Length;
		this.CreativeInventoryTabsLength = value.Length;
	}

	public int GetCreativeInventoryTabsCount()
	{
		return this.CreativeInventoryTabsCount;
	}

	public void CreativeInventoryTabsAdd(string value)
	{
		if (this.CreativeInventoryTabsCount >= this.CreativeInventoryTabsLength)
		{
			if ((this.CreativeInventoryTabsLength *= 2) == 0)
			{
				this.CreativeInventoryTabsLength = 1;
			}
			string[] newArray = new string[this.CreativeInventoryTabsLength];
			for (int i = 0; i < this.CreativeInventoryTabsCount; i++)
			{
				newArray[i] = this.CreativeInventoryTabs[i];
			}
			this.CreativeInventoryTabs = newArray;
		}
		string[] creativeInventoryTabs = this.CreativeInventoryTabs;
		int creativeInventoryTabsCount = this.CreativeInventoryTabsCount;
		this.CreativeInventoryTabsCount = creativeInventoryTabsCount + 1;
		creativeInventoryTabs[creativeInventoryTabsCount] = value;
	}

	public void SetGuiTransform(Packet_ModelTransform value)
	{
		this.GuiTransform = value;
	}

	public void SetFpHandTransform(Packet_ModelTransform value)
	{
		this.FpHandTransform = value;
	}

	public void SetTpHandTransform(Packet_ModelTransform value)
	{
		this.TpHandTransform = value;
	}

	public void SetTpOffHandTransform(Packet_ModelTransform value)
	{
		this.TpOffHandTransform = value;
	}

	public void SetGroundTransform(Packet_ModelTransform value)
	{
		this.GroundTransform = value;
	}

	public void SetAttributes(string value)
	{
		this.Attributes = value;
	}

	public void SetCombustibleProps(Packet_CombustibleProperties value)
	{
		this.CombustibleProps = value;
	}

	public void SetNutritionProps(Packet_NutritionProperties value)
	{
		this.NutritionProps = value;
	}

	public void SetGrindingProps(Packet_GrindingProperties value)
	{
		this.GrindingProps = value;
	}

	public void SetCrushingProps(Packet_CrushingProperties value)
	{
		this.CrushingProps = value;
	}

	public Packet_TransitionableProperties[] GetTransitionableProps()
	{
		return this.TransitionableProps;
	}

	public void SetTransitionableProps(Packet_TransitionableProperties[] value, int count, int length)
	{
		this.TransitionableProps = value;
		this.TransitionablePropsCount = count;
		this.TransitionablePropsLength = length;
	}

	public void SetTransitionableProps(Packet_TransitionableProperties[] value)
	{
		this.TransitionableProps = value;
		this.TransitionablePropsCount = value.Length;
		this.TransitionablePropsLength = value.Length;
	}

	public int GetTransitionablePropsCount()
	{
		return this.TransitionablePropsCount;
	}

	public void TransitionablePropsAdd(Packet_TransitionableProperties value)
	{
		if (this.TransitionablePropsCount >= this.TransitionablePropsLength)
		{
			if ((this.TransitionablePropsLength *= 2) == 0)
			{
				this.TransitionablePropsLength = 1;
			}
			Packet_TransitionableProperties[] newArray = new Packet_TransitionableProperties[this.TransitionablePropsLength];
			for (int i = 0; i < this.TransitionablePropsCount; i++)
			{
				newArray[i] = this.TransitionableProps[i];
			}
			this.TransitionableProps = newArray;
		}
		Packet_TransitionableProperties[] transitionableProps = this.TransitionableProps;
		int transitionablePropsCount = this.TransitionablePropsCount;
		this.TransitionablePropsCount = transitionablePropsCount + 1;
		transitionableProps[transitionablePropsCount] = value;
	}

	public void SetShape(Packet_CompositeShape value)
	{
		this.Shape = value;
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

	public void SetItemClass(string value)
	{
		this.ItemClass = value;
	}

	public void SetTool(int value)
	{
		this.Tool = value;
	}

	public void SetMaterialDensity(int value)
	{
		this.MaterialDensity = value;
	}

	public void SetAttackPower(int value)
	{
		this.AttackPower = value;
	}

	public void SetAttackRange(int value)
	{
		this.AttackRange = value;
	}

	public void SetLiquidSelectable(int value)
	{
		this.LiquidSelectable = value;
	}

	public void SetMiningTier(int value)
	{
		this.MiningTier = value;
	}

	public void SetStorageFlags(int value)
	{
		this.StorageFlags = value;
	}

	public void SetRenderAlphaTest(int value)
	{
		this.RenderAlphaTest = value;
	}

	public void SetHeldTpHitAnimation(string value)
	{
		this.HeldTpHitAnimation = value;
	}

	public void SetHeldRightTpIdleAnimation(string value)
	{
		this.HeldRightTpIdleAnimation = value;
	}

	public void SetHeldLeftTpIdleAnimation(string value)
	{
		this.HeldLeftTpIdleAnimation = value;
	}

	public void SetHeldTpUseAnimation(string value)
	{
		this.HeldTpUseAnimation = value;
	}

	public void SetMatterState(int value)
	{
		this.MatterState = value;
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

	public void SetHeldSounds(Packet_HeldSoundSet value)
	{
		this.HeldSounds = value;
	}

	public void SetWidth(int value)
	{
		this.Width = value;
	}

	public void SetHeight(int value)
	{
		this.Height = value;
	}

	public void SetLength(int value)
	{
		this.Length = value;
	}

	public int[] GetLightHsv()
	{
		return this.LightHsv;
	}

	public void SetLightHsv(int[] value, int count, int length)
	{
		this.LightHsv = value;
		this.LightHsvCount = count;
		this.LightHsvLength = length;
	}

	public void SetLightHsv(int[] value)
	{
		this.LightHsv = value;
		this.LightHsvCount = value.Length;
		this.LightHsvLength = value.Length;
	}

	public int GetLightHsvCount()
	{
		return this.LightHsvCount;
	}

	public void LightHsvAdd(int value)
	{
		if (this.LightHsvCount >= this.LightHsvLength)
		{
			if ((this.LightHsvLength *= 2) == 0)
			{
				this.LightHsvLength = 1;
			}
			int[] newArray = new int[this.LightHsvLength];
			for (int i = 0; i < this.LightHsvCount; i++)
			{
				newArray[i] = this.LightHsv[i];
			}
			this.LightHsv = newArray;
		}
		int[] lightHsv = this.LightHsv;
		int lightHsvCount = this.LightHsvCount;
		this.LightHsvCount = lightHsvCount + 1;
		lightHsv[lightHsvCount] = value;
	}

	public void SetIsMissing(int value)
	{
		this.IsMissing = value;
	}

	public void SetHeldLeftReadyAnimation(string value)
	{
		this.HeldLeftReadyAnimation = value;
	}

	public void SetHeldRightReadyAnimation(string value)
	{
		this.HeldRightReadyAnimation = value;
	}

	internal void InitializeValues()
	{
		this.MatterState = 0;
	}

	public int ItemId;

	public int MaxStackSize;

	public string Code;

	public int[] Tags;

	public int TagsCount;

	public int TagsLength;

	public Packet_Behavior[] Behaviors;

	public int BehaviorsCount;

	public int BehaviorsLength;

	public Packet_CompositeTexture[] CompositeTextures;

	public int CompositeTexturesCount;

	public int CompositeTexturesLength;

	public int Durability;

	public int[] Miningmaterial;

	public int MiningmaterialCount;

	public int MiningmaterialLength;

	public int[] Miningmaterialspeed;

	public int MiningmaterialspeedCount;

	public int MiningmaterialspeedLength;

	public int[] Damagedby;

	public int DamagedbyCount;

	public int DamagedbyLength;

	public byte[] CreativeInventoryStacks;

	public string[] CreativeInventoryTabs;

	public int CreativeInventoryTabsCount;

	public int CreativeInventoryTabsLength;

	public Packet_ModelTransform GuiTransform;

	public Packet_ModelTransform FpHandTransform;

	public Packet_ModelTransform TpHandTransform;

	public Packet_ModelTransform TpOffHandTransform;

	public Packet_ModelTransform GroundTransform;

	public string Attributes;

	public Packet_CombustibleProperties CombustibleProps;

	public Packet_NutritionProperties NutritionProps;

	public Packet_GrindingProperties GrindingProps;

	public Packet_CrushingProperties CrushingProps;

	public Packet_TransitionableProperties[] TransitionableProps;

	public int TransitionablePropsCount;

	public int TransitionablePropsLength;

	public Packet_CompositeShape Shape;

	public string[] TextureCodes;

	public int TextureCodesCount;

	public int TextureCodesLength;

	public string ItemClass;

	public int Tool;

	public int MaterialDensity;

	public int AttackPower;

	public int AttackRange;

	public int LiquidSelectable;

	public int MiningTier;

	public int StorageFlags;

	public int RenderAlphaTest;

	public string HeldTpHitAnimation;

	public string HeldRightTpIdleAnimation;

	public string HeldLeftTpIdleAnimation;

	public string HeldTpUseAnimation;

	public int MatterState;

	public Packet_VariantPart[] Variant;

	public int VariantCount;

	public int VariantLength;

	public Packet_HeldSoundSet HeldSounds;

	public int Width;

	public int Height;

	public int Length;

	public int[] LightHsv;

	public int LightHsvCount;

	public int LightHsvLength;

	public int IsMissing;

	public string HeldLeftReadyAnimation;

	public string HeldRightReadyAnimation;

	public const int ItemIdFieldID = 1;

	public const int MaxStackSizeFieldID = 2;

	public const int CodeFieldID = 3;

	public const int TagsFieldID = 48;

	public const int BehaviorsFieldID = 39;

	public const int CompositeTexturesFieldID = 4;

	public const int DurabilityFieldID = 5;

	public const int MiningmaterialFieldID = 6;

	public const int MiningmaterialspeedFieldID = 31;

	public const int DamagedbyFieldID = 7;

	public const int CreativeInventoryStacksFieldID = 8;

	public const int CreativeInventoryTabsFieldID = 9;

	public const int GuiTransformFieldID = 10;

	public const int FpHandTransformFieldID = 11;

	public const int TpHandTransformFieldID = 12;

	public const int TpOffHandTransformFieldID = 43;

	public const int GroundTransformFieldID = 22;

	public const int AttributesFieldID = 13;

	public const int CombustiblePropsFieldID = 14;

	public const int NutritionPropsFieldID = 15;

	public const int GrindingPropsFieldID = 32;

	public const int CrushingPropsFieldID = 38;

	public const int TransitionablePropsFieldID = 36;

	public const int ShapeFieldID = 16;

	public const int TextureCodesFieldID = 17;

	public const int ItemClassFieldID = 18;

	public const int ToolFieldID = 19;

	public const int MaterialDensityFieldID = 20;

	public const int AttackPowerFieldID = 21;

	public const int AttackRangeFieldID = 25;

	public const int LiquidSelectableFieldID = 23;

	public const int MiningTierFieldID = 24;

	public const int StorageFlagsFieldID = 26;

	public const int RenderAlphaTestFieldID = 27;

	public const int HeldTpHitAnimationFieldID = 28;

	public const int HeldRightTpIdleAnimationFieldID = 29;

	public const int HeldLeftTpIdleAnimationFieldID = 34;

	public const int HeldTpUseAnimationFieldID = 30;

	public const int MatterStateFieldID = 33;

	public const int VariantFieldID = 35;

	public const int HeldSoundsFieldID = 37;

	public const int WidthFieldID = 40;

	public const int HeightFieldID = 41;

	public const int LengthFieldID = 42;

	public const int LightHsvFieldID = 44;

	public const int IsMissingFieldID = 45;

	public const int HeldLeftReadyAnimationFieldID = 46;

	public const int HeldRightReadyAnimationFieldID = 47;

	public int size;
}
