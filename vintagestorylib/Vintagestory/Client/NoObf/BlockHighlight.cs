using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class BlockHighlight
	{
		public void TesselateModel(ClientMain game, BlockPos[] positions, int[] colors)
		{
			if (this.modelRef != null)
			{
				game.Platform.DeleteMesh(this.modelRef);
				this.modelRef = null;
			}
			if (positions.Length == 0)
			{
				return;
			}
			switch (this.shape)
			{
			case EnumHighlightShape.Arbitrary:
			case EnumHighlightShape.Cylinder:
				this.TesselateArbitraryModel(game, positions, colors);
				return;
			case EnumHighlightShape.Cube:
				if (positions.Length == 0)
				{
					this.modelRef = null;
					return;
				}
				if (positions.Length == 2)
				{
					int color = this.defaultColor;
					if (colors != null && colors.Length != 0)
					{
						color = colors[0];
					}
					this.TesselateCubeModel(game, positions[0], positions[1], color);
					return;
				}
				this.TesselateArbitraryModel(game, positions, colors);
				return;
			case EnumHighlightShape.Ball:
				break;
			case EnumHighlightShape.Cubes:
			{
				if (positions.Length < 2 || positions.Length % 2 != 0)
				{
					this.modelRef = null;
					return;
				}
				MeshData modeldata = new MeshData(24, 36, false, false, true, false);
				int color2 = this.defaultColor;
				if (colors != null && colors.Length != 0)
				{
					color2 = colors[0];
				}
				bool manyColors = colors != null && colors.Length >= positions.Length / 2;
				BlockPos start = positions[0];
				BlockPos end = positions[1];
				this.origin = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Min(start.Z, end.Z));
				for (int i = 0; i < positions.Length; i += 2)
				{
					this.GenCubeModel(game, modeldata, positions[i], positions[i + 1], manyColors ? colors[i / 2] : color2);
				}
				this.modelRef = game.Platform.UploadMesh(modeldata);
				break;
			}
			default:
				return;
			}
		}

		private void TesselateCubeModel(ClientMain game, BlockPos start, BlockPos end, int color)
		{
			this.origin = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.InternalY, end.InternalY), Math.Min(start.Z, end.Z));
			MeshData modeldata = new MeshData(24, 36, false, false, true, false);
			this.GenCubeModel(game, modeldata, start, end, color);
			this.modelRef = game.Platform.UploadMesh(modeldata);
		}

		private void GenCubeModel(ClientMain game, MeshData intoMesh, BlockPos start, BlockPos end, int color)
		{
			BlockPos minPos = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.InternalY, end.InternalY), Math.Min(start.Z, end.Z));
			int widthX = Math.Max(start.X, end.X) - minPos.X;
			int widthY = Math.Max(start.InternalY, end.InternalY) - minPos.InternalY;
			int widthZ = Math.Max(start.Z, end.Z) - minPos.Z;
			if (widthX == 0 || widthY == 0 || widthZ == 0)
			{
				game.Logger.Error("Cannot generate block highlight. Highlight width, height and length must be above 0");
				return;
			}
			if (this.mode == EnumHighlightBlocksMode.CenteredToSelectedBlock || this.mode == EnumHighlightBlocksMode.AttachedToSelectedBlock)
			{
				this.origin.X = 0;
				this.origin.Y = 0;
				this.origin.Z = 0;
				this.attachmentPoints = new BlockPos[6];
				for (int i = 0; i < 6; i++)
				{
					Vec3i j = BlockFacing.ALLNORMALI[i];
					this.attachmentPoints[i] = new BlockPos(widthX / 2 * j.X, widthY / 2 * j.Y, widthZ / 2 * j.Z);
				}
			}
			Vec3f centerPos = new Vec3f((float)widthX / 2f + (float)minPos.X - (float)this.origin.X, (float)widthY / 2f + (float)minPos.InternalY - (float)this.origin.Y, (float)widthZ / 2f + (float)minPos.Z - (float)this.origin.Z);
			Vec3f cubeSize = new Vec3f((float)widthX, (float)widthY, (float)widthZ);
			float[] shadings = CubeMeshUtil.DefaultBlockSideShadingsByFacing;
			for (int k = 0; k < 6; k++)
			{
				BlockFacing face = BlockFacing.ALLFACES[k];
				ModelCubeUtilExt.AddFaceSkipTex(intoMesh, face, centerPos, cubeSize, color, shadings[face.Index]);
			}
		}

		private void TesselateArbitraryModel(ClientMain game, BlockPos[] positions, int[] colors)
		{
			Dictionary<BlockPos, int> faceDrawFlags = new Dictionary<BlockPos, int>();
			BlockPos min = positions[0].Copy();
			BlockPos max = positions[0].Copy();
			foreach (BlockPos cur in positions)
			{
				min.X = Math.Min(min.X, cur.X);
				min.Y = Math.Min(min.Y, cur.Y);
				min.Z = Math.Min(min.Z, cur.Z);
				max.X = Math.Max(max.X, cur.X);
				max.Y = Math.Max(max.Y, cur.Y);
				max.Z = Math.Max(max.Z, cur.Z);
				faceDrawFlags[cur] = 0;
			}
			foreach (BlockPos cur in positions)
			{
				int flags = 0;
				if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.NORTH)))
				{
					flags |= (int)BlockFacing.NORTH.Flag;
				}
				if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.EAST)))
				{
					flags |= (int)BlockFacing.EAST.Flag;
				}
				if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.SOUTH)))
				{
					flags |= (int)BlockFacing.SOUTH.Flag;
				}
				if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.WEST)))
				{
					flags |= (int)BlockFacing.WEST.Flag;
				}
				if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.UP)))
				{
					flags |= (int)BlockFacing.UP.Flag;
				}
				if (!faceDrawFlags.ContainsKey(cur.AddCopy(BlockFacing.DOWN)))
				{
					flags |= (int)BlockFacing.DOWN.Flag;
				}
				faceDrawFlags[cur] = flags;
			}
			this.origin = min.Copy();
			if (this.mode == EnumHighlightBlocksMode.CenteredToSelectedBlock || this.mode == EnumHighlightBlocksMode.AttachedToSelectedBlock || this.mode == EnumHighlightBlocksMode.CenteredToBlockSelectionIndex || this.mode == EnumHighlightBlocksMode.AttachedToBlockSelectionIndex)
			{
				this.origin.X = 0;
				this.origin.Y = 0;
				this.origin.Z = 0;
				if (this.shape == EnumHighlightShape.Cube)
				{
					this.Size = new Vec3i(max.X - min.X + 1, max.Y - min.Y + 1, max.Z - min.Z + 1);
				}
				else
				{
					this.Size = new Vec3i(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
				}
				this.attachmentPoints = new BlockPos[6];
				for (int k = 0; k < 6; k++)
				{
					Vec3i l = BlockFacing.ALLNORMALI[k];
					if (this.shape == EnumHighlightShape.Cylinder)
					{
						this.attachmentPoints[k] = new BlockPos((int)((float)this.Size.X / 2f * (float)l.X), (int)Math.Ceiling((double)((float)this.Size.Y / 2f * (float)l.Y)), (int)((float)this.Size.Z / 2f * (float)l.Z));
						if (k == BlockFacing.DOWN.Index)
						{
							this.attachmentPoints[k].Y--;
						}
						if (k == BlockFacing.WEST.Index)
						{
							this.attachmentPoints[k].X--;
						}
						if (k == BlockFacing.NORTH.Index)
						{
							this.attachmentPoints[k].Z--;
						}
					}
					else if (this.shape == EnumHighlightShape.Cube)
					{
						this.attachmentPoints[k] = new BlockPos((int)((float)this.Size.X / 2f * (float)l.X), (int)((float)this.Size.Y / 2f * (float)l.Y), (int)((float)this.Size.Z / 2f * (float)l.Z));
						if (this.Size.Y == 1 && k == BlockFacing.DOWN.Index)
						{
							this.attachmentPoints[k].Y--;
						}
						if (this.Size.X == 1 && k == BlockFacing.WEST.Index)
						{
							this.attachmentPoints[k].X--;
						}
						if (this.Size.Z == 1 && k == BlockFacing.NORTH.Index)
						{
							this.attachmentPoints[k].Z--;
						}
					}
					else
					{
						this.attachmentPoints[k] = new BlockPos((int)((float)this.Size.X / 2f * (float)l.X), (int)((float)this.Size.Y / 2f * (float)l.Y), (int)((float)this.Size.Z / 2f * (float)l.Z));
						if (k == BlockFacing.DOWN.Index)
						{
							this.attachmentPoints[k].Y--;
						}
						if (k == BlockFacing.WEST.Index)
						{
							this.attachmentPoints[k].X--;
						}
						if (k == BlockFacing.NORTH.Index)
						{
							this.attachmentPoints[k].Z--;
						}
					}
				}
			}
			MeshData modeldata = new MeshData(positions.Length * 4 * 6, positions.Length * 6 * 6, false, false, true, false);
			Vec3f centerPos = new Vec3f();
			Vec3f cubeSize = new Vec3f(1f, 1f, 1f);
			int color = this.defaultColor;
			if (colors != null && colors.Length != 0)
			{
				color = colors[0];
			}
			bool manyColors = colors != null && colors.Length >= positions.Length && colors.Length > 1;
			float[] shadings = CubeMeshUtil.DefaultBlockSideShadingsByFacing;
			int posIndex = 0;
			foreach (KeyValuePair<BlockPos, int> val in faceDrawFlags)
			{
				int flags2 = val.Value;
				centerPos.X = (float)(val.Key.X - this.origin.X) + 0.5f;
				centerPos.Y = (float)(val.Key.InternalY - this.origin.Y) + 0.5f;
				centerPos.Z = (float)(val.Key.Z - this.origin.Z) + 0.5f;
				for (int m = 0; m < 6; m++)
				{
					BlockFacing face = BlockFacing.ALLFACES[m];
					if ((flags2 & (int)face.Flag) != 0)
					{
						ModelCubeUtilExt.AddFaceSkipTex(modeldata, face, centerPos, cubeSize, manyColors ? colors[posIndex] : color, shadings[face.Index]);
					}
				}
				posIndex++;
			}
			this.modelRef = game.Platform.UploadMesh(modeldata);
		}

		internal void Dispose(ClientMain game)
		{
			if (this.modelRef != null)
			{
				game.Platform.DeleteMesh(this.modelRef);
			}
		}

		public MeshRef modelRef;

		public BlockPos origin;

		public BlockPos[] attachmentPoints;

		public Vec3i Size;

		public EnumHighlightBlocksMode mode;

		public EnumHighlightShape shape;

		public float Scale = 1f;

		private int defaultColor = ColorUtil.ToRgba(96, (int)(GuiStyle.DialogDefaultBgColor[2] * 255.0), (int)(GuiStyle.DialogDefaultBgColor[1] * 255.0), (int)(GuiStyle.DialogDefaultBgColor[0] * 255.0));
	}
}
