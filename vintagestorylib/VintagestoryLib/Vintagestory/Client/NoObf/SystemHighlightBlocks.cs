using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	public class SystemHighlightBlocks : ClientSystem
	{
		public SystemHighlightBlocks(ClientMain game)
			: base(game)
		{
			game.PacketHandlers[52] = new ServerPacketHandler<Packet_Server>(this.HandlePacket);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3DTransparent), EnumRenderStage.OIT, this.Name, 0.89);
			game.eventManager.OnHighlightBlocks += this.EventManager_OnHighlightBlocks;
		}

		private void EventManager_OnHighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
		{
			BlockHighlight orCreateHighlight = this.getOrCreateHighlight(slotId);
			orCreateHighlight.mode = mode;
			orCreateHighlight.shape = shape;
			orCreateHighlight.Scale = scale;
			orCreateHighlight.TesselateModel(this.game, blocks.ToArray(), (colors != null) ? colors.ToArray() : null);
		}

		private void HandlePacket(Packet_Server packet)
		{
			BlockHighlight highlight = this.getOrCreateHighlight(packet.HighlightBlocks.Slotid);
			if (packet.HighlightBlocks.Blocks.Length == 0)
			{
				highlight.Dispose(this.game);
				this.highlightsByslotId.Remove(packet.HighlightBlocks.Slotid);
				return;
			}
			highlight.mode = (EnumHighlightBlocksMode)packet.HighlightBlocks.Mode;
			highlight.shape = (EnumHighlightShape)packet.HighlightBlocks.Shape;
			highlight.Scale = CollectibleNet.DeserializeFloatVeryPrecise(packet.HighlightBlocks.Scale);
			BlockPos[] positions = BlockTypeNet.UnpackBlockPositions(packet.HighlightBlocks.Blocks);
			int count = packet.HighlightBlocks.ColorsCount;
			int[] colors = new int[count];
			if (count > 0)
			{
				Array.Copy(packet.HighlightBlocks.Colors, colors, count);
			}
			highlight.TesselateModel(this.game, positions, colors);
		}

		public void OnRenderFrame3DTransparent(float deltaTime)
		{
			if (this.highlightsByslotId.Count == 0)
			{
				return;
			}
			ShaderProgramBlockhighlights prog = ShaderPrograms.Blockhighlights;
			prog.Use();
			Vec3d playerPos = this.game.EntityPlayer.CameraPos;
			foreach (KeyValuePair<int, BlockHighlight> keyValuePair in this.highlightsByslotId)
			{
				int num;
				BlockHighlight blockHighlight;
				keyValuePair.Deconstruct(out num, out blockHighlight);
				BlockHighlight highlight = blockHighlight;
				if (highlight.modelRef != null)
				{
					if (highlight.mode == EnumHighlightBlocksMode.CenteredToSelectedBlock || highlight.mode == EnumHighlightBlocksMode.AttachedToSelectedBlock)
					{
						if (this.game.BlockSelection == null || this.game.BlockSelection.Position == null)
						{
							continue;
						}
						highlight.origin.X = this.game.BlockSelection.Position.X + this.game.BlockSelection.Face.Normali.X;
						highlight.origin.Y = this.game.BlockSelection.Position.Y + this.game.BlockSelection.Face.Normali.Y;
						highlight.origin.Z = this.game.BlockSelection.Position.Z + this.game.BlockSelection.Face.Normali.Z;
					}
					if (highlight.mode == EnumHighlightBlocksMode.AttachedToSelectedBlock)
					{
						highlight.origin.X += highlight.attachmentPoints[this.game.BlockSelection.Face.Index].X;
						highlight.origin.Y += highlight.attachmentPoints[this.game.BlockSelection.Face.Index].Y;
						highlight.origin.Z += highlight.attachmentPoints[this.game.BlockSelection.Face.Index].Z;
					}
					this.game.GlPushMatrix();
					this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
					if (highlight.mode == EnumHighlightBlocksMode.CenteredToBlockSelectionIndex || highlight.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex)
					{
						if (this.game.BlockSelection == null || this.game.BlockSelection.Position == null)
						{
							this.game.GlPopMatrix();
							continue;
						}
						Cuboidf[] boxes = this.game.GetBlockIntersectionBoxes(this.game.BlockSelection.Position);
						if (boxes == null || boxes.Length == 0 || this.game.BlockSelection.SelectionBoxIndex >= boxes.Length)
						{
							this.game.GlPopMatrix();
							continue;
						}
						BlockPos pos = this.game.BlockSelection.Position;
						float scale = highlight.Scale;
						Vec3d hitPos = this.game.BlockSelection.HitPosition;
						int faceIndex = this.game.BlockSelection.Face.Index;
						double posx;
						double posy;
						double posz;
						if (highlight.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex && highlight.shape != EnumHighlightShape.Cube)
						{
							posx = (double)((float)pos.X + (float)((int)(hitPos.X * 16.0)) / 16f + (float)highlight.attachmentPoints[faceIndex].X * scale);
							posy = (double)((float)pos.Y + (float)((int)(hitPos.Y * 16.0)) / 16f + (float)highlight.attachmentPoints[faceIndex].Y * scale);
							posz = (double)((float)pos.Z + (float)((int)(hitPos.Z * 16.0)) / 16f + (float)highlight.attachmentPoints[faceIndex].Z * scale);
						}
						else
						{
							posx = (double)((float)pos.X + (float)((int)(hitPos.X * 16.0)) / 16f);
							posy = (double)((float)pos.Y + (float)((int)(hitPos.Y * 16.0)) / 16f);
							posz = (double)((float)pos.Z + (float)((int)(hitPos.Z * 16.0)) / 16f);
						}
						if (highlight.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex && highlight.shape == EnumHighlightShape.Cube)
						{
							if (highlight.attachmentPoints[faceIndex].X < 0)
							{
								posx -= Math.Ceiling((double)((float)highlight.Size.X / 2f)) * (double)scale;
							}
							else if (highlight.attachmentPoints[faceIndex].X > 0)
							{
								posx += (double)((float)highlight.attachmentPoints[faceIndex].X * scale);
							}
							if (highlight.attachmentPoints[faceIndex].Y < 0)
							{
								posy -= Math.Ceiling((double)((float)highlight.Size.Y / 2f)) * (double)scale;
							}
							else if (highlight.attachmentPoints[faceIndex].Y > 0)
							{
								posy += (double)((float)highlight.attachmentPoints[faceIndex].Y * scale);
							}
							if (highlight.attachmentPoints[faceIndex].Z < 0)
							{
								posz -= Math.Ceiling((double)((float)highlight.Size.Z / 2f)) * (double)scale;
							}
							else if (highlight.attachmentPoints[faceIndex].Z > 0)
							{
								posz += (double)((float)highlight.attachmentPoints[faceIndex].Z * scale);
							}
						}
						this.game.GlTranslate((double)((float)(posx - playerPos.X)), (double)((float)(posy - playerPos.Y)), (double)((float)(posz - playerPos.Z)));
						this.game.GlScale((double)scale, (double)scale, (double)scale);
					}
					else
					{
						this.game.GlTranslate((double)((float)((double)highlight.origin.X - playerPos.X)), (double)((float)((double)highlight.origin.Y - playerPos.Y)), (double)((float)((double)highlight.origin.Z - playerPos.Z)));
					}
					prog.ProjectionMatrix = this.game.CurrentProjectionMatrix;
					prog.ModelViewMatrix = this.game.CurrentModelViewMatrix;
					this.game.Platform.RenderMesh(highlight.modelRef);
					this.game.GlPopMatrix();
				}
			}
			prog.Stop();
		}

		public override void Dispose(ClientMain game)
		{
			foreach (KeyValuePair<int, BlockHighlight> val in this.highlightsByslotId)
			{
				val.Value.Dispose(game);
			}
			this.highlightsByslotId.Clear();
		}

		private BlockHighlight getOrCreateHighlight(int slotId)
		{
			BlockHighlight highlight;
			if (!this.highlightsByslotId.TryGetValue(slotId, out highlight))
			{
				return this.highlightsByslotId[slotId] = new BlockHighlight();
			}
			return highlight;
		}

		public override string Name
		{
			get
			{
				return "hibl";
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		private Dictionary<int, BlockHighlight> highlightsByslotId = new Dictionary<int, BlockHighlight>();
	}
}
