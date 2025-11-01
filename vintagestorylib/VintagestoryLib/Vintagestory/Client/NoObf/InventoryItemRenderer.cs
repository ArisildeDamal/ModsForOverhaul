using System;
using System.Collections.Generic;
using Cairo;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf
{
	public class InventoryItemRenderer : IRenderer, IDisposable
	{
		public double RenderOrder
		{
			get
			{
				return 9.0;
			}
		}

		public int RenderRange
		{
			get
			{
				return 24;
			}
		}

		public InventoryItemRenderer(ClientMain game)
		{
			this.game = game;
			MeshData modeldata = QuadMeshUtil.GetQuad();
			modeldata.Uv = new float[] { 1f, 1f, 0f, 1f, 0f, 0f, 1f, 0f };
			modeldata.Rgba = new byte[16];
			modeldata.Rgba.Fill(byte.MaxValue);
			this.quadModelRef = game.Platform.UploadMesh(modeldata);
			this.stackSizeFont = CairoFont.WhiteSmallText().WithFontSize((float)GuiStyle.SmallishFontSize);
			this.stackSizeFont.FontWeight = FontWeight.Bold;
			this.stackSizeFont.Color = new double[] { 1.0, 1.0, 1.0, 1.0 };
			this.stackSizeFont.StrokeColor = new double[] { 0.0, 0.0, 0.0, 1.0 };
			this.stackSizeFont.StrokeWidth = (double)ClientSettings.GUIScale + 0.25;
			ClientSettings.Inst.AddWatcher<float>("guiScale", delegate(float newvalue)
			{
				this.stackSizeFont.StrokeWidth = (double)ClientSettings.GUIScale + 0.25;
				foreach (KeyValuePair<string, LoadedTexture> val in this.StackSizeTextures)
				{
					LoadedTexture value = val.Value;
					if (value != null)
					{
						value.Dispose();
					}
				}
				this.StackSizeTextures.Clear();
			});
			game.eventManager.RegisterRenderer(this, EnumRenderStage.Ortho, "renderstacktoatlas");
		}

		public bool RenderItemStackToAtlas(ItemStack stack, ITextureAtlasAPI atlas, int size, Action<int> onComplete, int color = -1, float sepiaLevel = 0f, float scale = 1f)
		{
			AtlasRenderTask task = new AtlasRenderTask
			{
				Stack = stack,
				Atlas = atlas,
				Size = size,
				Color = color,
				SepiaLevel = sepiaLevel,
				OnComplete = onComplete,
				Scale = scale
			};
			if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
			{
				this.game.EnqueueMainThreadTask(delegate
				{
					this.queue.Enqueue(task);
				}, "enqueueRenderTask");
				return false;
			}
			if (this.game.currentRenderStage != EnumRenderStage.Ortho)
			{
				this.queue.Enqueue(task);
				return false;
			}
			int atlasTextureId = this.CreatePositionInAtlas(atlas, task);
			FrameBufferRef fb = this.CreateFrameBuffer(atlas.Size, atlasTextureId);
			this.RenderItemStackToFrameBuffer(atlas.Size, task, new DummySlot(stack));
			this.DisposeFrameBuffer(fb);
			onComplete(task.SubId);
			return true;
		}

		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			ITextureAtlasAPI atlas = null;
			int maxCount = 512;
			while (this.queue.Count > 0 && maxCount-- > 0)
			{
				AtlasRenderTask task = this.queue.Dequeue();
				atlas = task.Atlas;
				int atlasTextureId = this.CreatePositionInAtlas(atlas, task);
				Queue<AtlasRenderTask> atlasQueue;
				if (!this.perAtlasQueues.TryGetValue(atlasTextureId, out atlasQueue))
				{
					atlasQueue = new Queue<AtlasRenderTask>();
					this.perAtlasQueues[atlasTextureId] = atlasQueue;
				}
				atlasQueue.Enqueue(task);
				this.taskCompletedQueue.Enqueue(task);
			}
			if (atlas == null)
			{
				return;
			}
			DummySlot dummySlot = new DummySlot();
			using (Dictionary<int, Queue<AtlasRenderTask>>.Enumerator enumerator = this.perAtlasQueues.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<int, Queue<AtlasRenderTask>> atlasIdAndQueue = enumerator.Current;
					Queue<AtlasRenderTask> itemQueue = atlasIdAndQueue.Value;
					int atlasTextureId2 = atlasIdAndQueue.Key;
					FrameBufferRef fb = this.CreateFrameBuffer(atlas.Size, atlasTextureId2);
					while (itemQueue.Count > 0)
					{
						AtlasRenderTask itemToRender = itemQueue.Dequeue();
						dummySlot.Set(itemToRender.Stack);
						this.game.Platform.BindTexture2d(atlasTextureId2);
						this.RenderItemStackToFrameBuffer(atlas.Size, itemToRender, dummySlot);
					}
					this.DisposeFrameBuffer(fb);
				}
				goto IL_0140;
			}
			IL_0120:
			AtlasRenderTask task2 = this.taskCompletedQueue.Dequeue();
			task2.OnComplete(task2.SubId);
			IL_0140:
			if (this.taskCompletedQueue.Count <= 0)
			{
				return;
			}
			goto IL_0120;
		}

		private FrameBufferRef CreateFrameBuffer(Size2i atlasSize, int atlasTextureId)
		{
			FramebufferAttrsAttachment[] attachments = new FramebufferAttrsAttachment[]
			{
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
					Texture = new RawTexture
					{
						Width = atlasSize.Width,
						Height = atlasSize.Height,
						TextureId = atlasTextureId,
						PixelFormat = EnumTexturePixelFormat.Rgba,
						PixelInternalFormat = EnumTextureInternalFormat.Rgba8
					}
				},
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.DepthAttachment,
					Texture = new RawTexture
					{
						Width = atlasSize.Width,
						Height = atlasSize.Height,
						PixelFormat = EnumTexturePixelFormat.DepthComponent,
						PixelInternalFormat = EnumTextureInternalFormat.DepthComponent32
					}
				}
			};
			FrameBufferRef fb = this.game.Platform.CreateFramebuffer(new FramebufferAttrs("atlasRenderer", atlasSize.Width, atlasSize.Height)
			{
				Attachments = attachments
			});
			this.game.Platform.LoadFrameBuffer(fb);
			this.game.Platform.GlEnableDepthTest();
			this.game.Platform.GlDisableCullFace();
			this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
			this.game.Platform.ClearFrameBuffer(fb, null, true, false);
			this.game.OrthoMode(atlasSize.Width, atlasSize.Height, true);
			this.game.Platform.BindTexture2d(atlasTextureId);
			return fb;
		}

		public void RenderItemStackToFrameBuffer(Size2i atlasSize, AtlasRenderTask task, DummySlot dummySlot)
		{
			float x = task.TexPos.x1 * (float)atlasSize.Width;
			float y = task.TexPos.y1 * (float)atlasSize.Height;
			int size = task.Size;
			if (this.clearPixels == null || this.clearPixels.Length < size * size)
			{
				this.clearPixels = new int[size * size];
			}
			this.game.guiShaderProg.SepiaLevel = task.SepiaLevel;
			GL.TexSubImage2D<int>(TextureTarget.Texture2D, 0, (int)x, (int)y, size, size, PixelFormat.Bgra, PixelType.UnsignedByte, this.clearPixels);
			this.game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(dummySlot, (double)(x + (float)size / 2f), (double)(y + (float)size / 2f), 500.0, (float)(size / 2) * task.Scale, task.Color, true, false, false);
		}

		public void DisposeFrameBuffer(FrameBufferRef fb)
		{
			this.game.PerspectiveMode();
			this.game.guiShaderProg.SepiaLevel = 0f;
			this.game.Platform.LoadFrameBuffer(EnumFrameBuffer.Default);
			fb.ColorTextureIds = Array.Empty<int>();
			this.game.Platform.DisposeFrameBuffer(fb, true);
		}

		private int CreatePositionInAtlas(ITextureAtlasAPI atlas, AtlasRenderTask task)
		{
			int subid;
			TextureAtlasPosition texPos;
			if (!atlas.AllocateTextureSpace(task.Size, task.Size, out subid, out texPos, null))
			{
				throw new Exception(string.Format("Was not able to allocate texture space of size {0}x{0} for item stack '{1}', maybe atlas is full?", task.Size, task.Stack.GetName()));
			}
			task.SubId = subid;
			task.TexPos = texPos;
			return texPos.atlasTextureId;
		}

		public void RenderEntityToGui(float dt, Entity entity, double posX, double posY, double posZ, float yawDelta, float size, int color)
		{
			this.game.guiShaderProg.RgbaIn = new Vec4f(1f, 1f, 1f, 1f);
			this.game.guiShaderProg.ExtraGlow = 0;
			this.game.guiShaderProg.ApplyColor = 1;
			this.game.guiShaderProg.Tex2d2D = this.game.EntityAtlasManager.AtlasTextures[0].TextureId;
			this.game.guiShaderProg.AlphaTest = 0.1f;
			this.game.guiShaderProg.NoTexture = 0f;
			this.game.guiShaderProg.OverlayOpacity = 0f;
			this.game.guiShaderProg.NormalShaded = 1;
			entity.Properties.Client.Renderer.RenderToGui(dt, posX, posY, posZ, yawDelta, size);
			this.game.guiShaderProg.NormalShaded = 0;
		}

		public void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, bool shading = true, bool origRotate = false, bool showStackSize = true)
		{
			this.RenderItemstackToGui(inSlot, posX, posY, posZ, size, color, 0f, shading, origRotate, showStackSize);
		}

		public void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, bool shading = true, bool origRotate = false, bool showStackSize = true)
		{
			try
			{
				ItemStack itemstack = inSlot.Itemstack;
				ItemRenderInfo renderInfo = InventoryItemRenderer.GetItemStackRenderInfo(this.game, inSlot, EnumItemRenderTarget.Gui, dt);
				if (renderInfo.ModelRef != null)
				{
					itemstack.Collectible.InGuiIdle(this.game, itemstack);
					ModelTransform transform = renderInfo.Transform;
					if (transform != null)
					{
						bool upsidedown = itemstack.Class == EnumItemClass.Block;
						bool rotate = origRotate && renderInfo.Transform.Rotate;
						Matrixf modelMat = this.modelMat;
						modelMat.Identity();
						modelMat.Translate((float)((int)posX - ((itemstack.Class == EnumItemClass.Item) ? 3 : 0)), (float)((int)posY - ((itemstack.Class == EnumItemClass.Item) ? 1 : 0)), (float)posZ);
						modelMat.Translate((double)transform.Origin.X + GuiElement.scaled((double)transform.Translation.X), (double)transform.Origin.Y + GuiElement.scaled((double)transform.Translation.Y), (double)(transform.Origin.Z * size) + GuiElement.scaled((double)transform.Translation.Z));
						modelMat.Scale(size * transform.ScaleXYZ.X, size * transform.ScaleXYZ.Y, size * transform.ScaleXYZ.Z);
						modelMat.RotateXDeg(transform.Rotation.X + (upsidedown ? 180f : 0f));
						modelMat.RotateYDeg(transform.Rotation.Y - (float)(upsidedown ? (-1) : 1) * (rotate ? ((float)this.game.Platform.EllapsedMs / 50f) : 0f));
						modelMat.RotateZDeg(transform.Rotation.Z);
						modelMat.Translate(-transform.Origin.X, -transform.Origin.Y, -transform.Origin.Z);
						int num = (int)itemstack.Collectible.GetTemperature(this.game, itemstack);
						float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(num);
						float[] drawcolor = ColorUtil.ToRGBAFloats(color);
						int extraGlow = GameMath.Clamp((num - 550) / 2, 0, 255);
						bool tempGlowMode = itemstack.Attributes.HasAttribute("temperature");
						ShaderProgramGui guiShaderProg = this.game.guiShaderProg;
						guiShaderProg.NormalShaded = ((renderInfo.NormalShaded && shading) ? 1 : 0);
						guiShaderProg.RgbaIn = new Vec4f(drawcolor[0], drawcolor[1], drawcolor[2], drawcolor[3]);
						guiShaderProg.ExtraGlow = extraGlow;
						guiShaderProg.TempGlowMode = ((tempGlowMode > false) ? 1 : 0);
						guiShaderProg.RgbaGlowIn = (tempGlowMode ? new Vec4f(glowColor[0], glowColor[1], glowColor[2], (float)extraGlow / 255f) : new Vec4f(1f, 1f, 1f, (float)extraGlow / 255f));
						guiShaderProg.ApplyColor = ((renderInfo.ApplyColor > false) ? 1 : 0);
						guiShaderProg.AlphaTest = renderInfo.AlphaTest;
						guiShaderProg.OverlayOpacity = renderInfo.OverlayOpacity;
						if (renderInfo.OverlayTexture != null && renderInfo.OverlayOpacity > 0f)
						{
							guiShaderProg.Tex2dOverlay2D = renderInfo.OverlayTexture.TextureId;
							guiShaderProg.OverlayTextureSize = new Vec2f((float)renderInfo.OverlayTexture.Width, (float)renderInfo.OverlayTexture.Height);
							guiShaderProg.BaseTextureSize = new Vec2f((float)renderInfo.TextureSize.Width, (float)renderInfo.TextureSize.Height);
							TextureAtlasPosition texPos = InventoryItemRenderer.GetTextureAtlasPosition(this.game, itemstack);
							guiShaderProg.BaseUvOrigin = new Vec2f(texPos.x1, texPos.y1);
						}
						guiShaderProg.ModelMatrix = modelMat.Values;
						guiShaderProg.ProjectionMatrix = this.game.CurrentProjectionMatrix;
						guiShaderProg.ModelViewMatrix = modelMat.ReverseMul(this.game.CurrentModelViewMatrix).Values;
						guiShaderProg.ApplyModelMat = 1;
						ItemRenderDelegate renderer;
						if (this.game.api.eventapi.itemStackRenderersByTarget[(int)itemstack.Collectible.ItemClass][0].TryGetValue(itemstack.Collectible.Id, out renderer))
						{
							renderer(inSlot, renderInfo, modelMat, posX, posY, posZ, size, color, origRotate, showStackSize);
							guiShaderProg.ApplyModelMat = 0;
							guiShaderProg.NormalShaded = 0;
							guiShaderProg.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
							guiShaderProg.AlphaTest = 0f;
						}
						else
						{
							guiShaderProg.DamageEffect = renderInfo.DamageEffect;
							this.game.api.renderapi.RenderMultiTextureMesh(renderInfo.ModelRef, "tex2d", 0);
							guiShaderProg.ApplyModelMat = 0;
							guiShaderProg.NormalShaded = 0;
							guiShaderProg.TempGlowMode = 0;
							guiShaderProg.DamageEffect = 0f;
							LoadedTexture stackSizeTexture = null;
							if (itemstack.StackSize != 1 && showStackSize)
							{
								float mul = size / (float)GuiElement.scaled(25.600000381469727);
								string key = itemstack.StackSize.ToString() + "-" + ((int)(mul * 100f)).ToString();
								if (!this.StackSizeTextures.TryGetValue(key, out stackSizeTexture))
								{
									stackSizeTexture = (this.StackSizeTextures[key] = this.GenStackSizeTexture(itemstack.StackSize, mul));
								}
							}
							if (stackSizeTexture != null)
							{
								float mul2 = size / (float)GuiElement.scaled(25.600000381469727);
								this.game.Platform.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
								this.game.Render2DLoadedTexture(stackSizeTexture, (float)((int)(posX + (double)size + 1.0 - (double)stackSizeTexture.Width)), (float)((int)(posY + (double)mul2 * GuiElement.scaled(3.0) - GuiElement.scaled(4.0))), (float)((int)posZ + 100), null);
								this.game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
							}
							guiShaderProg.AlphaTest = 0f;
							guiShaderProg.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
						}
					}
				}
			}
			catch (Exception e)
			{
				throw new Exception("Error while rendering item in slot " + ((inSlot != null) ? inSlot.ToString() : null), e);
			}
		}

		private LoadedTexture GenStackSizeTexture(int stackSize, float fontSizeMultiplier = 1f)
		{
			CairoFont font = this.stackSizeFont.Clone();
			font.UnscaledFontsize *= (double)fontSizeMultiplier;
			return this.game.api.guiapi.TextTexture.GenTextTexture(stackSize.ToString() ?? "", font, null);
		}

		public static ItemRenderInfo GetItemStackRenderInfo(ClientMain game, ItemSlot inSlot, EnumItemRenderTarget target, float dt)
		{
			ItemStack itemstack = inSlot.Itemstack;
			if (itemstack == null || itemstack.Collectible.Code == null)
			{
				return new ItemRenderInfo();
			}
			ItemRenderInfo renderInfo = new ItemRenderInfo();
			renderInfo.dt = dt;
			switch (target)
			{
			case EnumItemRenderTarget.Gui:
				renderInfo.Transform = itemstack.Collectible.GuiTransform;
				break;
			case EnumItemRenderTarget.HandTp:
				renderInfo.Transform = itemstack.Collectible.TpHandTransform;
				break;
			case EnumItemRenderTarget.HandTpOff:
				renderInfo.Transform = itemstack.Collectible.TpOffHandTransform ?? itemstack.Collectible.TpHandTransform;
				break;
			case EnumItemRenderTarget.Ground:
				renderInfo.Transform = itemstack.Collectible.GroundTransform;
				break;
			}
			CollectibleObject collectible = itemstack.Collectible;
			if (((collectible != null) ? collectible.Code : null) == null)
			{
				renderInfo.ModelRef = ((itemstack.Block == null) ? game.TesselatorManager.unknownItemModelRef : game.TesselatorManager.unknownBlockModelRef);
			}
			else if (itemstack.Class == EnumItemClass.Block)
			{
				renderInfo.ModelRef = game.TesselatorManager.blockModelRefsInventory[itemstack.Id];
			}
			else
			{
				int variant = (itemstack.TempAttributes.HasAttribute("renderVariant") ? itemstack.TempAttributes.GetInt("renderVariant", 0) : itemstack.Attributes.GetInt("renderVariant", 0));
				if (variant != 0 && (variant < 0 || game.TesselatorManager.altItemModelRefsInventory[itemstack.Id] == null || game.TesselatorManager.altItemModelRefsInventory[itemstack.Id].Length < variant - 1))
				{
					game.Logger.Warning("Itemstack {0} has an invalid renderVariant {1}. No such model variant exists. Will reset to 0", new object[]
					{
						itemstack.GetName(),
						variant
					});
					itemstack.TempAttributes.SetInt("renderVariant", 0);
					variant = 0;
				}
				if (variant == 0)
				{
					renderInfo.ModelRef = game.TesselatorManager.itemModelRefsInventory[itemstack.Id];
				}
				else
				{
					renderInfo.ModelRef = game.TesselatorManager.altItemModelRefsInventory[itemstack.Id][variant - 1];
				}
			}
			ItemRenderInfo itemRenderInfo = renderInfo;
			bool flag;
			if (itemstack.Class != EnumItemClass.Block)
			{
				CompositeShape shape = itemstack.Item.Shape;
				flag = shape != null && !shape.VoxelizeTexture;
			}
			else
			{
				CompositeShape shape2 = itemstack.Block.Shape;
				flag = shape2 != null && !shape2.VoxelizeTexture;
			}
			itemRenderInfo.NormalShaded = flag;
			renderInfo.TextureSize.Width = ((itemstack.Class == EnumItemClass.Block) ? game.BlockAtlasManager.Size.Width : game.ItemAtlasManager.Size.Width);
			renderInfo.TextureSize.Height = ((itemstack.Class == EnumItemClass.Block) ? game.BlockAtlasManager.Size.Height : game.ItemAtlasManager.Size.Height);
			renderInfo.HalfTransparent = itemstack.Block != null && (itemstack.Block.RenderPass == EnumChunkRenderPass.Meta || itemstack.Block.RenderPass == EnumChunkRenderPass.Transparent);
			renderInfo.AlphaTest = itemstack.Collectible.RenderAlphaTest;
			renderInfo.CullFaces = itemstack.Block != null && (itemstack.Block.RenderPass == EnumChunkRenderPass.Opaque || itemstack.Block.RenderPass == EnumChunkRenderPass.TopSoil);
			renderInfo.ApplyColor = renderInfo.NormalShaded;
			TransitionState state = itemstack.Collectible.UpdateAndGetTransitionState(game, inSlot, EnumTransitionType.Perish);
			if (state != null && state.TransitionLevel > 0f)
			{
				renderInfo.SetRotOverlay(game.api, state.TransitionLevel);
			}
			renderInfo.InSlot = inSlot;
			inSlot.OnBeforeRender(renderInfo);
			itemstack.Collectible.OnBeforeRender(game.api, itemstack, target, ref renderInfo);
			return renderInfo;
		}

		public static TextureAtlasPosition GetTextureAtlasPosition(ClientMain game, IItemStack itemstack)
		{
			int tileSide = BlockFacing.UP.Index;
			if (itemstack.Collectible.Code == null)
			{
				return game.BlockAtlasManager.UnknownTexturePos;
			}
			if (itemstack.Class == EnumItemClass.Block)
			{
				int textureSubId = game.FastBlockTextureSubidsByBlockAndFace[itemstack.Id][tileSide];
				return game.BlockAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId];
			}
			if (itemstack.Item.FirstTexture == null)
			{
				return game.BlockAtlasManager.UnknownTexturePos;
			}
			int textureSubId2 = itemstack.Item.FirstTexture.Baked.TextureSubId;
			return game.ItemAtlasManager.TextureAtlasPositionsByTextureSubId[textureSubId2];
		}

		public int GetCurrentBlockOrItemTextureId(int side)
		{
			ItemSlot slot = this.game.player.inventoryMgr.ActiveHotbarSlot;
			if (slot != null && slot.Itemstack.Class == EnumItemClass.Block)
			{
				return this.game.FastBlockTextureSubidsByBlockAndFace[slot.Itemstack.Id][side];
			}
			return 0;
		}

		public int GetBlockOrItemTextureId(BlockFacing facing, IItemStack itemstack)
		{
			if (itemstack.Class != EnumItemClass.Block)
			{
				return 0;
			}
			return this.game.FastBlockTextureSubidsByBlockAndFace[itemstack.Id][facing.Index];
		}

		public void Dispose()
		{
			MeshRef meshRef = this.quadModelRef;
			if (meshRef != null)
			{
				meshRef.Dispose();
			}
			foreach (KeyValuePair<string, LoadedTexture> val in this.StackSizeTextures)
			{
				LoadedTexture value = val.Value;
				if (value != null)
				{
					value.Dispose();
				}
			}
		}

		private ClientMain game;

		private Dictionary<string, LoadedTexture> StackSizeTextures = new Dictionary<string, LoadedTexture>();

		private MeshRef quadModelRef;

		private CairoFont stackSizeFont;

		private Matrixf modelMat = new Matrixf();

		private Queue<AtlasRenderTask> queue = new Queue<AtlasRenderTask>();

		private Dictionary<int, Queue<AtlasRenderTask>> perAtlasQueues = new Dictionary<int, Queue<AtlasRenderTask>>();

		private Queue<AtlasRenderTask> taskCompletedQueue = new Queue<AtlasRenderTask>();

		private int[] clearPixels;
	}
}
