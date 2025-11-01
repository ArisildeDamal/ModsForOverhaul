using System;

public class Packet_BlockType
{
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

	public string[] GetInventoryTextureCodes()
	{
		return this.InventoryTextureCodes;
	}

	public void SetInventoryTextureCodes(string[] value, int count, int length)
	{
		this.InventoryTextureCodes = value;
		this.InventoryTextureCodesCount = count;
		this.InventoryTextureCodesLength = length;
	}

	public void SetInventoryTextureCodes(string[] value)
	{
		this.InventoryTextureCodes = value;
		this.InventoryTextureCodesCount = value.Length;
		this.InventoryTextureCodesLength = value.Length;
	}

	public int GetInventoryTextureCodesCount()
	{
		return this.InventoryTextureCodesCount;
	}

	public void InventoryTextureCodesAdd(string value)
	{
		if (this.InventoryTextureCodesCount >= this.InventoryTextureCodesLength)
		{
			if ((this.InventoryTextureCodesLength *= 2) == 0)
			{
				this.InventoryTextureCodesLength = 1;
			}
			string[] newArray = new string[this.InventoryTextureCodesLength];
			for (int i = 0; i < this.InventoryTextureCodesCount; i++)
			{
				newArray[i] = this.InventoryTextureCodes[i];
			}
			this.InventoryTextureCodes = newArray;
		}
		string[] inventoryTextureCodes = this.InventoryTextureCodes;
		int inventoryTextureCodesCount = this.InventoryTextureCodesCount;
		this.InventoryTextureCodesCount = inventoryTextureCodesCount + 1;
		inventoryTextureCodes[inventoryTextureCodesCount] = value;
	}

	public Packet_CompositeTexture[] GetInventoryCompositeTextures()
	{
		return this.InventoryCompositeTextures;
	}

	public void SetInventoryCompositeTextures(Packet_CompositeTexture[] value, int count, int length)
	{
		this.InventoryCompositeTextures = value;
		this.InventoryCompositeTexturesCount = count;
		this.InventoryCompositeTexturesLength = length;
	}

	public void SetInventoryCompositeTextures(Packet_CompositeTexture[] value)
	{
		this.InventoryCompositeTextures = value;
		this.InventoryCompositeTexturesCount = value.Length;
		this.InventoryCompositeTexturesLength = value.Length;
	}

	public int GetInventoryCompositeTexturesCount()
	{
		return this.InventoryCompositeTexturesCount;
	}

	public void InventoryCompositeTexturesAdd(Packet_CompositeTexture value)
	{
		if (this.InventoryCompositeTexturesCount >= this.InventoryCompositeTexturesLength)
		{
			if ((this.InventoryCompositeTexturesLength *= 2) == 0)
			{
				this.InventoryCompositeTexturesLength = 1;
			}
			Packet_CompositeTexture[] newArray = new Packet_CompositeTexture[this.InventoryCompositeTexturesLength];
			for (int i = 0; i < this.InventoryCompositeTexturesCount; i++)
			{
				newArray[i] = this.InventoryCompositeTextures[i];
			}
			this.InventoryCompositeTextures = newArray;
		}
		Packet_CompositeTexture[] inventoryCompositeTextures = this.InventoryCompositeTextures;
		int inventoryCompositeTexturesCount = this.InventoryCompositeTexturesCount;
		this.InventoryCompositeTexturesCount = inventoryCompositeTexturesCount + 1;
		inventoryCompositeTextures[inventoryCompositeTexturesCount] = value;
	}

	public void SetBlockId(int value)
	{
		this.BlockId = value;
	}

	public void SetCode(string value)
	{
		this.Code = value;
	}

	public void SetEntityClass(string value)
	{
		this.EntityClass = value;
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

	public void SetEntityBehaviors(string value)
	{
		this.EntityBehaviors = value;
	}

	public void SetRenderPass(int value)
	{
		this.RenderPass = value;
	}

	public void SetDrawType(int value)
	{
		this.DrawType = value;
	}

	public void SetMatterState(int value)
	{
		this.MatterState = value;
	}

	public void SetWalkSpeedFloat(int value)
	{
		this.WalkSpeedFloat = value;
	}

	public void SetIsSlipperyWalk(bool value)
	{
		this.IsSlipperyWalk = value;
	}

	public void SetSounds(Packet_BlockSoundSet value)
	{
		this.Sounds = value;
	}

	public void SetHeldSounds(Packet_HeldSoundSet value)
	{
		this.HeldSounds = value;
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

	public void SetVertexFlags(int value)
	{
		this.VertexFlags = value;
	}

	public void SetClimbable(int value)
	{
		this.Climbable = value;
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

	public void SetCreativeInventoryStacks(byte[] value)
	{
		this.CreativeInventoryStacks = value;
	}

	public int[] GetSideOpaqueFlags()
	{
		return this.SideOpaqueFlags;
	}

	public void SetSideOpaqueFlags(int[] value, int count, int length)
	{
		this.SideOpaqueFlags = value;
		this.SideOpaqueFlagsCount = count;
		this.SideOpaqueFlagsLength = length;
	}

	public void SetSideOpaqueFlags(int[] value)
	{
		this.SideOpaqueFlags = value;
		this.SideOpaqueFlagsCount = value.Length;
		this.SideOpaqueFlagsLength = value.Length;
	}

	public int GetSideOpaqueFlagsCount()
	{
		return this.SideOpaqueFlagsCount;
	}

	public void SideOpaqueFlagsAdd(int value)
	{
		if (this.SideOpaqueFlagsCount >= this.SideOpaqueFlagsLength)
		{
			if ((this.SideOpaqueFlagsLength *= 2) == 0)
			{
				this.SideOpaqueFlagsLength = 1;
			}
			int[] newArray = new int[this.SideOpaqueFlagsLength];
			for (int i = 0; i < this.SideOpaqueFlagsCount; i++)
			{
				newArray[i] = this.SideOpaqueFlags[i];
			}
			this.SideOpaqueFlags = newArray;
		}
		int[] sideOpaqueFlags = this.SideOpaqueFlags;
		int sideOpaqueFlagsCount = this.SideOpaqueFlagsCount;
		this.SideOpaqueFlagsCount = sideOpaqueFlagsCount + 1;
		sideOpaqueFlags[sideOpaqueFlagsCount] = value;
	}

	public void SetFaceCullMode(int value)
	{
		this.FaceCullMode = value;
	}

	public int[] GetSideSolidFlags()
	{
		return this.SideSolidFlags;
	}

	public void SetSideSolidFlags(int[] value, int count, int length)
	{
		this.SideSolidFlags = value;
		this.SideSolidFlagsCount = count;
		this.SideSolidFlagsLength = length;
	}

	public void SetSideSolidFlags(int[] value)
	{
		this.SideSolidFlags = value;
		this.SideSolidFlagsCount = value.Length;
		this.SideSolidFlagsLength = value.Length;
	}

	public int GetSideSolidFlagsCount()
	{
		return this.SideSolidFlagsCount;
	}

	public void SideSolidFlagsAdd(int value)
	{
		if (this.SideSolidFlagsCount >= this.SideSolidFlagsLength)
		{
			if ((this.SideSolidFlagsLength *= 2) == 0)
			{
				this.SideSolidFlagsLength = 1;
			}
			int[] newArray = new int[this.SideSolidFlagsLength];
			for (int i = 0; i < this.SideSolidFlagsCount; i++)
			{
				newArray[i] = this.SideSolidFlags[i];
			}
			this.SideSolidFlags = newArray;
		}
		int[] sideSolidFlags = this.SideSolidFlags;
		int sideSolidFlagsCount = this.SideSolidFlagsCount;
		this.SideSolidFlagsCount = sideSolidFlagsCount + 1;
		sideSolidFlags[sideSolidFlagsCount] = value;
	}

	public void SetSeasonColorMap(string value)
	{
		this.SeasonColorMap = value;
	}

	public void SetClimateColorMap(string value)
	{
		this.ClimateColorMap = value;
	}

	public void SetCullFaces(int value)
	{
		this.CullFaces = value;
	}

	public void SetReplacable(int value)
	{
		this.Replacable = value;
	}

	public void SetLightAbsorption(int value)
	{
		this.LightAbsorption = value;
	}

	public void SetHardnessLevel(int value)
	{
		this.HardnessLevel = value;
	}

	public void SetResistance(int value)
	{
		this.Resistance = value;
	}

	public void SetBlockMaterial(int value)
	{
		this.BlockMaterial = value;
	}

	public void SetModdata(byte[] value)
	{
		this.Moddata = value;
	}

	public void SetShape(Packet_CompositeShape value)
	{
		this.Shape = value;
	}

	public void SetShapeInventory(Packet_CompositeShape value)
	{
		this.ShapeInventory = value;
	}

	public void SetAmbientocclusion(int value)
	{
		this.Ambientocclusion = value;
	}

	public Packet_Cube[] GetCollisionBoxes()
	{
		return this.CollisionBoxes;
	}

	public void SetCollisionBoxes(Packet_Cube[] value, int count, int length)
	{
		this.CollisionBoxes = value;
		this.CollisionBoxesCount = count;
		this.CollisionBoxesLength = length;
	}

	public void SetCollisionBoxes(Packet_Cube[] value)
	{
		this.CollisionBoxes = value;
		this.CollisionBoxesCount = value.Length;
		this.CollisionBoxesLength = value.Length;
	}

	public int GetCollisionBoxesCount()
	{
		return this.CollisionBoxesCount;
	}

	public void CollisionBoxesAdd(Packet_Cube value)
	{
		if (this.CollisionBoxesCount >= this.CollisionBoxesLength)
		{
			if ((this.CollisionBoxesLength *= 2) == 0)
			{
				this.CollisionBoxesLength = 1;
			}
			Packet_Cube[] newArray = new Packet_Cube[this.CollisionBoxesLength];
			for (int i = 0; i < this.CollisionBoxesCount; i++)
			{
				newArray[i] = this.CollisionBoxes[i];
			}
			this.CollisionBoxes = newArray;
		}
		Packet_Cube[] collisionBoxes = this.CollisionBoxes;
		int collisionBoxesCount = this.CollisionBoxesCount;
		this.CollisionBoxesCount = collisionBoxesCount + 1;
		collisionBoxes[collisionBoxesCount] = value;
	}

	public Packet_Cube[] GetSelectionBoxes()
	{
		return this.SelectionBoxes;
	}

	public void SetSelectionBoxes(Packet_Cube[] value, int count, int length)
	{
		this.SelectionBoxes = value;
		this.SelectionBoxesCount = count;
		this.SelectionBoxesLength = length;
	}

	public void SetSelectionBoxes(Packet_Cube[] value)
	{
		this.SelectionBoxes = value;
		this.SelectionBoxesCount = value.Length;
		this.SelectionBoxesLength = value.Length;
	}

	public int GetSelectionBoxesCount()
	{
		return this.SelectionBoxesCount;
	}

	public void SelectionBoxesAdd(Packet_Cube value)
	{
		if (this.SelectionBoxesCount >= this.SelectionBoxesLength)
		{
			if ((this.SelectionBoxesLength *= 2) == 0)
			{
				this.SelectionBoxesLength = 1;
			}
			Packet_Cube[] newArray = new Packet_Cube[this.SelectionBoxesLength];
			for (int i = 0; i < this.SelectionBoxesCount; i++)
			{
				newArray[i] = this.SelectionBoxes[i];
			}
			this.SelectionBoxes = newArray;
		}
		Packet_Cube[] selectionBoxes = this.SelectionBoxes;
		int selectionBoxesCount = this.SelectionBoxesCount;
		this.SelectionBoxesCount = selectionBoxesCount + 1;
		selectionBoxes[selectionBoxesCount] = value;
	}

	public Packet_Cube[] GetParticleCollisionBoxes()
	{
		return this.ParticleCollisionBoxes;
	}

	public void SetParticleCollisionBoxes(Packet_Cube[] value, int count, int length)
	{
		this.ParticleCollisionBoxes = value;
		this.ParticleCollisionBoxesCount = count;
		this.ParticleCollisionBoxesLength = length;
	}

	public void SetParticleCollisionBoxes(Packet_Cube[] value)
	{
		this.ParticleCollisionBoxes = value;
		this.ParticleCollisionBoxesCount = value.Length;
		this.ParticleCollisionBoxesLength = value.Length;
	}

	public int GetParticleCollisionBoxesCount()
	{
		return this.ParticleCollisionBoxesCount;
	}

	public void ParticleCollisionBoxesAdd(Packet_Cube value)
	{
		if (this.ParticleCollisionBoxesCount >= this.ParticleCollisionBoxesLength)
		{
			if ((this.ParticleCollisionBoxesLength *= 2) == 0)
			{
				this.ParticleCollisionBoxesLength = 1;
			}
			Packet_Cube[] newArray = new Packet_Cube[this.ParticleCollisionBoxesLength];
			for (int i = 0; i < this.ParticleCollisionBoxesCount; i++)
			{
				newArray[i] = this.ParticleCollisionBoxes[i];
			}
			this.ParticleCollisionBoxes = newArray;
		}
		Packet_Cube[] particleCollisionBoxes = this.ParticleCollisionBoxes;
		int particleCollisionBoxesCount = this.ParticleCollisionBoxesCount;
		this.ParticleCollisionBoxesCount = particleCollisionBoxesCount + 1;
		particleCollisionBoxes[particleCollisionBoxesCount] = value;
	}

	public void SetBlockclass(string value)
	{
		this.Blockclass = value;
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

	public void SetFertility(int value)
	{
		this.Fertility = value;
	}

	public void SetParticleProperties(byte[] value)
	{
		this.ParticleProperties = value;
	}

	public void SetParticlePropertiesQuantity(int value)
	{
		this.ParticlePropertiesQuantity = value;
	}

	public void SetRandomDrawOffset(int value)
	{
		this.RandomDrawOffset = value;
	}

	public void SetRandomizeAxes(int value)
	{
		this.RandomizeAxes = value;
	}

	public void SetRandomizeRotations(int value)
	{
		this.RandomizeRotations = value;
	}

	public Packet_BlockDrop[] GetDrops()
	{
		return this.Drops;
	}

	public void SetDrops(Packet_BlockDrop[] value, int count, int length)
	{
		this.Drops = value;
		this.DropsCount = count;
		this.DropsLength = length;
	}

	public void SetDrops(Packet_BlockDrop[] value)
	{
		this.Drops = value;
		this.DropsCount = value.Length;
		this.DropsLength = value.Length;
	}

	public int GetDropsCount()
	{
		return this.DropsCount;
	}

	public void DropsAdd(Packet_BlockDrop value)
	{
		if (this.DropsCount >= this.DropsLength)
		{
			if ((this.DropsLength *= 2) == 0)
			{
				this.DropsLength = 1;
			}
			Packet_BlockDrop[] newArray = new Packet_BlockDrop[this.DropsLength];
			for (int i = 0; i < this.DropsCount; i++)
			{
				newArray[i] = this.Drops[i];
			}
			this.Drops = newArray;
		}
		Packet_BlockDrop[] drops = this.Drops;
		int dropsCount = this.DropsCount;
		this.DropsCount = dropsCount + 1;
		drops[dropsCount] = value;
	}

	public void SetLiquidLevel(int value)
	{
		this.LiquidLevel = value;
	}

	public void SetAttributes(string value)
	{
		this.Attributes = value;
	}

	public void SetCombustibleProps(Packet_CombustibleProperties value)
	{
		this.CombustibleProps = value;
	}

	public int[] GetSideAo()
	{
		return this.SideAo;
	}

	public void SetSideAo(int[] value, int count, int length)
	{
		this.SideAo = value;
		this.SideAoCount = count;
		this.SideAoLength = length;
	}

	public void SetSideAo(int[] value)
	{
		this.SideAo = value;
		this.SideAoCount = value.Length;
		this.SideAoLength = value.Length;
	}

	public int GetSideAoCount()
	{
		return this.SideAoCount;
	}

	public void SideAoAdd(int value)
	{
		if (this.SideAoCount >= this.SideAoLength)
		{
			if ((this.SideAoLength *= 2) == 0)
			{
				this.SideAoLength = 1;
			}
			int[] newArray = new int[this.SideAoLength];
			for (int i = 0; i < this.SideAoCount; i++)
			{
				newArray[i] = this.SideAo[i];
			}
			this.SideAo = newArray;
		}
		int[] sideAo = this.SideAo;
		int sideAoCount = this.SideAoCount;
		this.SideAoCount = sideAoCount + 1;
		sideAo[sideAoCount] = value;
	}

	public void SetNeighbourSideAo(int value)
	{
		this.NeighbourSideAo = value;
	}

	public void SetGrindingProps(Packet_GrindingProperties value)
	{
		this.GrindingProps = value;
	}

	public void SetNutritionProps(Packet_NutritionProperties value)
	{
		this.NutritionProps = value;
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

	public void SetMaxStackSize(int value)
	{
		this.MaxStackSize = value;
	}

	public void SetCropProps(byte[] value)
	{
		this.CropProps = value;
	}

	public string[] GetCropPropBehaviors()
	{
		return this.CropPropBehaviors;
	}

	public void SetCropPropBehaviors(string[] value, int count, int length)
	{
		this.CropPropBehaviors = value;
		this.CropPropBehaviorsCount = count;
		this.CropPropBehaviorsLength = length;
	}

	public void SetCropPropBehaviors(string[] value)
	{
		this.CropPropBehaviors = value;
		this.CropPropBehaviorsCount = value.Length;
		this.CropPropBehaviorsLength = value.Length;
	}

	public int GetCropPropBehaviorsCount()
	{
		return this.CropPropBehaviorsCount;
	}

	public void CropPropBehaviorsAdd(string value)
	{
		if (this.CropPropBehaviorsCount >= this.CropPropBehaviorsLength)
		{
			if ((this.CropPropBehaviorsLength *= 2) == 0)
			{
				this.CropPropBehaviorsLength = 1;
			}
			string[] newArray = new string[this.CropPropBehaviorsLength];
			for (int i = 0; i < this.CropPropBehaviorsCount; i++)
			{
				newArray[i] = this.CropPropBehaviors[i];
			}
			this.CropPropBehaviors = newArray;
		}
		string[] cropPropBehaviors = this.CropPropBehaviors;
		int cropPropBehaviorsCount = this.CropPropBehaviorsCount;
		this.CropPropBehaviorsCount = cropPropBehaviorsCount + 1;
		cropPropBehaviors[cropPropBehaviorsCount] = value;
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

	public void SetRequiredMiningTier(int value)
	{
		this.RequiredMiningTier = value;
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

	public void SetDragMultiplierFloat(int value)
	{
		this.DragMultiplierFloat = value;
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

	public void SetRainPermeable(int value)
	{
		this.RainPermeable = value;
	}

	public void SetLiquidCode(string value)
	{
		this.LiquidCode = value;
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

	public void SetLod0shape(Packet_CompositeShape value)
	{
		this.Lod0shape = value;
	}

	public void SetFrostable(int value)
	{
		this.Frostable = value;
	}

	public void SetCrushingProps(Packet_CrushingProperties value)
	{
		this.CrushingProps = value;
	}

	public void SetRandomSizeAdjust(int value)
	{
		this.RandomSizeAdjust = value;
	}

	public void SetLod2shape(Packet_CompositeShape value)
	{
		this.Lod2shape = value;
	}

	public void SetDoNotRenderAtLod2(int value)
	{
		this.DoNotRenderAtLod2 = value;
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

	public void SetIsMissing(int value)
	{
		this.IsMissing = value;
	}

	public void SetDurability(int value)
	{
		this.Durability = value;
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

	public string[] TextureCodes;

	public int TextureCodesCount;

	public int TextureCodesLength;

	public Packet_CompositeTexture[] CompositeTextures;

	public int CompositeTexturesCount;

	public int CompositeTexturesLength;

	public string[] InventoryTextureCodes;

	public int InventoryTextureCodesCount;

	public int InventoryTextureCodesLength;

	public Packet_CompositeTexture[] InventoryCompositeTextures;

	public int InventoryCompositeTexturesCount;

	public int InventoryCompositeTexturesLength;

	public int BlockId;

	public string Code;

	public string EntityClass;

	public int[] Tags;

	public int TagsCount;

	public int TagsLength;

	public Packet_Behavior[] Behaviors;

	public int BehaviorsCount;

	public int BehaviorsLength;

	public string EntityBehaviors;

	public int RenderPass;

	public int DrawType;

	public int MatterState;

	public int WalkSpeedFloat;

	public bool IsSlipperyWalk;

	public Packet_BlockSoundSet Sounds;

	public Packet_HeldSoundSet HeldSounds;

	public int[] LightHsv;

	public int LightHsvCount;

	public int LightHsvLength;

	public int VertexFlags;

	public int Climbable;

	public string[] CreativeInventoryTabs;

	public int CreativeInventoryTabsCount;

	public int CreativeInventoryTabsLength;

	public byte[] CreativeInventoryStacks;

	public int[] SideOpaqueFlags;

	public int SideOpaqueFlagsCount;

	public int SideOpaqueFlagsLength;

	public int FaceCullMode;

	public int[] SideSolidFlags;

	public int SideSolidFlagsCount;

	public int SideSolidFlagsLength;

	public string SeasonColorMap;

	public string ClimateColorMap;

	public int CullFaces;

	public int Replacable;

	public int LightAbsorption;

	public int HardnessLevel;

	public int Resistance;

	public int BlockMaterial;

	public byte[] Moddata;

	public Packet_CompositeShape Shape;

	public Packet_CompositeShape ShapeInventory;

	public int Ambientocclusion;

	public Packet_Cube[] CollisionBoxes;

	public int CollisionBoxesCount;

	public int CollisionBoxesLength;

	public Packet_Cube[] SelectionBoxes;

	public int SelectionBoxesCount;

	public int SelectionBoxesLength;

	public Packet_Cube[] ParticleCollisionBoxes;

	public int ParticleCollisionBoxesCount;

	public int ParticleCollisionBoxesLength;

	public string Blockclass;

	public Packet_ModelTransform GuiTransform;

	public Packet_ModelTransform FpHandTransform;

	public Packet_ModelTransform TpHandTransform;

	public Packet_ModelTransform TpOffHandTransform;

	public Packet_ModelTransform GroundTransform;

	public int Fertility;

	public byte[] ParticleProperties;

	public int ParticlePropertiesQuantity;

	public int RandomDrawOffset;

	public int RandomizeAxes;

	public int RandomizeRotations;

	public Packet_BlockDrop[] Drops;

	public int DropsCount;

	public int DropsLength;

	public int LiquidLevel;

	public string Attributes;

	public Packet_CombustibleProperties CombustibleProps;

	public int[] SideAo;

	public int SideAoCount;

	public int SideAoLength;

	public int NeighbourSideAo;

	public Packet_GrindingProperties GrindingProps;

	public Packet_NutritionProperties NutritionProps;

	public Packet_TransitionableProperties[] TransitionableProps;

	public int TransitionablePropsCount;

	public int TransitionablePropsLength;

	public int MaxStackSize;

	public byte[] CropProps;

	public string[] CropPropBehaviors;

	public int CropPropBehaviorsCount;

	public int CropPropBehaviorsLength;

	public int MaterialDensity;

	public int AttackPower;

	public int AttackRange;

	public int LiquidSelectable;

	public int MiningTier;

	public int RequiredMiningTier;

	public int[] Miningmaterial;

	public int MiningmaterialCount;

	public int MiningmaterialLength;

	public int[] Miningmaterialspeed;

	public int MiningmaterialspeedCount;

	public int MiningmaterialspeedLength;

	public int DragMultiplierFloat;

	public int StorageFlags;

	public int RenderAlphaTest;

	public string HeldTpHitAnimation;

	public string HeldRightTpIdleAnimation;

	public string HeldLeftTpIdleAnimation;

	public string HeldTpUseAnimation;

	public int RainPermeable;

	public string LiquidCode;

	public Packet_VariantPart[] Variant;

	public int VariantCount;

	public int VariantLength;

	public Packet_CompositeShape Lod0shape;

	public int Frostable;

	public Packet_CrushingProperties CrushingProps;

	public int RandomSizeAdjust;

	public Packet_CompositeShape Lod2shape;

	public int DoNotRenderAtLod2;

	public int Width;

	public int Height;

	public int Length;

	public int IsMissing;

	public int Durability;

	public string HeldLeftReadyAnimation;

	public string HeldRightReadyAnimation;

	public const int TextureCodesFieldID = 1;

	public const int CompositeTexturesFieldID = 2;

	public const int InventoryTextureCodesFieldID = 3;

	public const int InventoryCompositeTexturesFieldID = 4;

	public const int BlockIdFieldID = 5;

	public const int CodeFieldID = 6;

	public const int EntityClassFieldID = 58;

	public const int TagsFieldID = 104;

	public const int BehaviorsFieldID = 7;

	public const int EntityBehaviorsFieldID = 84;

	public const int RenderPassFieldID = 8;

	public const int DrawTypeFieldID = 9;

	public const int MatterStateFieldID = 10;

	public const int WalkSpeedFloatFieldID = 11;

	public const int IsSlipperyWalkFieldID = 12;

	public const int SoundsFieldID = 13;

	public const int HeldSoundsFieldID = 83;

	public const int LightHsvFieldID = 14;

	public const int VertexFlagsFieldID = 51;

	public const int ClimbableFieldID = 15;

	public const int CreativeInventoryTabsFieldID = 16;

	public const int CreativeInventoryStacksFieldID = 17;

	public const int SideOpaqueFlagsFieldID = 24;

	public const int FaceCullModeFieldID = 23;

	public const int SideSolidFlagsFieldID = 46;

	public const int SeasonColorMapFieldID = 25;

	public const int ClimateColorMapFieldID = 88;

	public const int CullFacesFieldID = 26;

	public const int ReplacableFieldID = 27;

	public const int LightAbsorptionFieldID = 29;

	public const int HardnessLevelFieldID = 30;

	public const int ResistanceFieldID = 31;

	public const int BlockMaterialFieldID = 32;

	public const int ModdataFieldID = 33;

	public const int ShapeFieldID = 34;

	public const int ShapeInventoryFieldID = 35;

	public const int AmbientocclusionFieldID = 38;

	public const int CollisionBoxesFieldID = 39;

	public const int SelectionBoxesFieldID = 40;

	public const int ParticleCollisionBoxesFieldID = 91;

	public const int BlockclassFieldID = 41;

	public const int GuiTransformFieldID = 42;

	public const int FpHandTransformFieldID = 43;

	public const int TpHandTransformFieldID = 44;

	public const int TpOffHandTransformFieldID = 99;

	public const int GroundTransformFieldID = 45;

	public const int FertilityFieldID = 47;

	public const int ParticlePropertiesFieldID = 48;

	public const int ParticlePropertiesQuantityFieldID = 49;

	public const int RandomDrawOffsetFieldID = 50;

	public const int RandomizeAxesFieldID = 69;

	public const int RandomizeRotationsFieldID = 87;

	public const int DropsFieldID = 52;

	public const int LiquidLevelFieldID = 53;

	public const int AttributesFieldID = 54;

	public const int CombustiblePropsFieldID = 55;

	public const int SideAoFieldID = 57;

	public const int NeighbourSideAoFieldID = 79;

	public const int GrindingPropsFieldID = 77;

	public const int NutritionPropsFieldID = 59;

	public const int TransitionablePropsFieldID = 85;

	public const int MaxStackSizeFieldID = 60;

	public const int CropPropsFieldID = 61;

	public const int CropPropBehaviorsFieldID = 90;

	public const int MaterialDensityFieldID = 62;

	public const int AttackPowerFieldID = 63;

	public const int AttackRangeFieldID = 70;

	public const int LiquidSelectableFieldID = 64;

	public const int MiningTierFieldID = 65;

	public const int RequiredMiningTierFieldID = 66;

	public const int MiningmaterialFieldID = 67;

	public const int MiningmaterialspeedFieldID = 76;

	public const int DragMultiplierFloatFieldID = 68;

	public const int StorageFlagsFieldID = 71;

	public const int RenderAlphaTestFieldID = 72;

	public const int HeldTpHitAnimationFieldID = 73;

	public const int HeldRightTpIdleAnimationFieldID = 74;

	public const int HeldLeftTpIdleAnimationFieldID = 80;

	public const int HeldTpUseAnimationFieldID = 75;

	public const int RainPermeableFieldID = 78;

	public const int LiquidCodeFieldID = 81;

	public const int VariantFieldID = 82;

	public const int Lod0shapeFieldID = 86;

	public const int FrostableFieldID = 89;

	public const int CrushingPropsFieldID = 92;

	public const int RandomSizeAdjustFieldID = 93;

	public const int Lod2shapeFieldID = 94;

	public const int DoNotRenderAtLod2FieldID = 95;

	public const int WidthFieldID = 96;

	public const int HeightFieldID = 97;

	public const int LengthFieldID = 98;

	public const int IsMissingFieldID = 100;

	public const int DurabilityFieldID = 101;

	public const int HeldLeftReadyAnimationFieldID = 102;

	public const int HeldRightReadyAnimationFieldID = 103;

	public int size;
}
