using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemRenderDebugWireframes : ClientSystem
	{
		private WireframeModes wfmodes
		{
			get
			{
				return this.game.api.renderapi.WireframeDebugRender;
			}
		}

		public override string Name
		{
			get
			{
				return "debwf";
			}
		}

		public SystemRenderDebugWireframes(ClientMain game)
			: base(game)
		{
			this.chunkWf = WireframeCube.CreateCenterOriginCube(game.api, int.MinValue);
			this.entityWf = WireframeCube.CreateCenterOriginCube(game.api, -1);
			this.beWf = WireframeCube.CreateCenterOriginCube(game.api, -939523896);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3D), EnumRenderStage.Opaque, this.Name, 0.5);
		}

		public override void Dispose(ClientMain game)
		{
			this.chunkWf.Dispose();
			this.entityWf.Dispose();
			this.beWf.Dispose();
		}

		public void OnRenderFrame3D(float deltaTime)
		{
			int plrDim = this.game.EntityPlayer.Pos.Dimension;
			if (this.wfmodes.Entity)
			{
				foreach (Entity entity in this.game.LoadedEntities.Values)
				{
					if (entity.Pos.Dimension == plrDim && entity.SelectionBox != null)
					{
						float scaleX = entity.SelectionBox.XSize / 2f;
						float scaleY = entity.SelectionBox.YSize / 2f;
						float scaleZ = entity.SelectionBox.ZSize / 2f;
						double X;
						double Y;
						double Z;
						if (entity == this.game.EntityPlayer)
						{
							X = this.game.EntityPlayer.CameraPos.X;
							Y = this.game.EntityPlayer.CameraPos.Y + (double)(plrDim * 32768);
							Z = this.game.EntityPlayer.CameraPos.Z;
						}
						else
						{
							X = entity.Pos.X;
							Y = entity.Pos.InternalY;
							Z = entity.Pos.Z;
						}
						double x = X + (double)entity.SelectionBox.X1 + (double)scaleX;
						double y = Y + (double)entity.SelectionBox.Y1 + (double)scaleY;
						double z = Z + (double)entity.SelectionBox.Z1 + (double)scaleZ;
						float lineWidth = ((this.game.EntitySelection != null && entity.EntityId == this.game.EntitySelection.Entity.EntityId) ? 3f : 1f);
						this.entityWf.Render(this.game.api, x, y, z, scaleX, scaleY, scaleZ, lineWidth, new Vec4f(0f, 0f, 1f, 1f));
						float selScaleX = entity.SelectionBox.XSize / 2f;
						float selScaleY = entity.SelectionBox.YSize / 2f;
						float selScaleZ = entity.SelectionBox.ZSize / 2f;
						if (selScaleX != scaleX || selScaleY != scaleY || selScaleZ != scaleZ)
						{
							x = X + (double)entity.SelectionBox.X1 + (double)selScaleX;
							y = Y + (double)entity.SelectionBox.Y1 + (double)selScaleY;
							z = Z + (double)entity.SelectionBox.Z1 + (double)selScaleZ;
							this.entityWf.Render(this.game.api, x, y, z, selScaleX, selScaleY, selScaleZ, lineWidth, new Vec4f(0f, 0f, 1f, 1f));
						}
						float colScaleX = entity.CollisionBox.XSize / 2f;
						float colScaleY = entity.CollisionBox.YSize / 2f;
						float colScaleZ = entity.CollisionBox.ZSize / 2f;
						x = X + (double)entity.CollisionBox.X1 + (double)colScaleX;
						y = Y + (double)entity.CollisionBox.Y1 + (double)colScaleY;
						z = Z + (double)entity.CollisionBox.Z1 + (double)colScaleZ;
						this.entityWf.Render(this.game.api, x, y, z, colScaleX, colScaleY, colScaleZ, lineWidth, new Vec4f(1f, 0f, 0f, 1f));
					}
				}
			}
			if (this.wfmodes.Chunk)
			{
				int chunksize = this.game.WorldMap.ClientChunkSize;
				BlockPos pos = this.game.EntityPlayer.Pos.AsBlockPos / chunksize * chunksize + chunksize / 2;
				this.chunkWf.Render(this.game.api, (double)((float)pos.X + 0.01f), (double)((float)pos.InternalY + 0.01f), (double)((float)pos.Z + 0.01f), (float)(chunksize / 2), (float)(chunksize / 2), (float)(chunksize / 2), 8f, null);
			}
			if (this.wfmodes.ServerChunk)
			{
				int chunksize2 = this.game.WorldMap.ServerChunkSize;
				BlockPos pos2 = this.game.EntityPlayer.Pos.AsBlockPos / chunksize2 * chunksize2 + chunksize2 / 2;
				this.chunkWf.Render(this.game.api, (double)((float)pos2.X + 0.01f), (double)((float)pos2.InternalY + 0.01f), (double)((float)pos2.Z + 0.01f), (float)(chunksize2 / 2), (float)(chunksize2 / 2), (float)(chunksize2 / 2), 8f, null);
			}
			if (this.wfmodes.Region)
			{
				int regionSize = this.game.WorldMap.RegionSize;
				BlockPos pos3 = this.game.EntityPlayer.Pos.AsBlockPos / regionSize * regionSize + regionSize / 2;
				this.chunkWf.Render(this.game.api, (double)((float)pos3.X + 0.01f), (double)((float)pos3.InternalY + 0.01f), (double)((float)pos3.Z + 0.01f), (float)(regionSize / 2), (float)(regionSize / 2), (float)(regionSize / 2), 16f, null);
			}
			if (this.wfmodes.LandClaim && this.game.WorldMap.LandClaims != null)
			{
				foreach (LandClaim landClaim in this.game.WorldMap.LandClaims)
				{
					Vec4f colorLandClaim = new Vec4f(1f, 1f, 0.5f, 1f);
					foreach (Cuboidi claim in landClaim.Areas)
					{
						int x2 = Math.Min(claim.X1, claim.X2);
						int y2 = Math.Min(claim.Y1, claim.Y2);
						int z2 = Math.Min(claim.Z1, claim.Z2);
						this.entityWf.Render(this.game.api, (double)((float)x2 + (float)claim.SizeX / 2f), (double)((float)y2 + (float)claim.SizeY / 2f), (double)((float)z2 + (float)claim.SizeZ / 2f), (float)claim.SizeX / 2f, (float)claim.SizeY / 2f, (float)claim.SizeZ / 2f, 4f, colorLandClaim);
					}
				}
			}
			if (this.wfmodes.Structures)
			{
				int regionSize2 = this.game.WorldMap.RegionSize;
				int regionX = (int)(this.game.EntityPlayer.Pos.X / (double)regionSize2);
				int regionZ = (int)(this.game.EntityPlayer.Pos.Z / (double)regionSize2);
				IMapRegion region = this.game.WorldMap.GetMapRegion(regionX, regionZ);
				if (region != null)
				{
					Vec4f colorStruc = new Vec4f(1f, 0f, 1f, 1f);
					foreach (GeneratedStructure structure in region.GeneratedStructures)
					{
						this.entityWf.Render(this.game.api, (double)((float)structure.Location.X1 + (float)structure.Location.SizeX / 2f), (double)((float)structure.Location.Y1 + (float)structure.Location.SizeY / 2f), (double)((float)structure.Location.Z1 + (float)structure.Location.SizeZ / 2f), (float)structure.Location.SizeX / 2f, (float)structure.Location.SizeY / 2f, (float)structure.Location.SizeZ / 2f, 2f, colorStruc);
					}
				}
			}
			if (this.wfmodes.BlockEntity)
			{
				BlockPos chunkpos = this.game.EntityPlayer.Pos.AsBlockPos / 32;
				int dimensionAdjust = chunkpos.dimension * 1024;
				for (int cx = -1; cx <= 1; cx++)
				{
					for (int cy = -1; cy <= 1; cy++)
					{
						for (int cz = -1; cz <= 1; cz++)
						{
							ClientChunk chunk = this.game.WorldMap.GetClientChunk(chunkpos.X + cx, chunkpos.Y + dimensionAdjust + cy, chunkpos.Z + cz);
							if (chunk != null)
							{
								foreach (KeyValuePair<BlockPos, BlockEntity> val in chunk.BlockEntities)
								{
									BlockPos bePos = val.Key;
									this.beWf.Render(this.game.api, (double)((float)bePos.X + 0.5f), (double)((float)bePos.InternalY + 0.5f), (double)((float)bePos.Z + 0.5f), 0.5f, 0.5f, 0.5f, 1f, null);
								}
							}
						}
					}
				}
			}
			if (this.wfmodes.Inside)
			{
				EntityPos pos4 = this.game.player.Entity.SidedPos;
				BlockPos tmpPos = new BlockPos(pos4.Dimension);
				Block block = this.game.player.Entity.GetInsideTorsoBlockSoundSource(tmpPos);
				this.renderBoxes(block, tmpPos, new Vec4f(1f, 0.7f, 0.2f, 1f));
				block = this.game.player.Entity.GetInsideLegsBlockSoundSource(tmpPos);
				this.renderBoxes(block, tmpPos, new Vec4f(1f, 1f, 0f, 1f));
				block = this.game.player.Entity.GetNearestBlockSoundSource(tmpPos, -0.03, 4, true);
				this.renderBoxes(block, tmpPos, new Vec4f(1f, 0f, 1f, 1f));
				tmpPos.Set((int)pos4.X, (int)(pos4.Y + 0.10000000149011612), (int)pos4.Z);
				block = this.game.blockAccessor.GetBlock(tmpPos, 2);
				if (block.Id != 0)
				{
					this.renderBoxes(block, tmpPos, new Vec4f(0f, 1f, 1f, 1f));
				}
			}
			if (this.wfmodes.Smoothstep)
			{
				EntityAgent entity2 = this.game.player.Entity;
				EntityControls controls = entity2.Controls;
				float StepHeight = 0.6f;
				Cuboidd entityCollisionBox = entity2.CollisionBox.ToDouble();
				double searchBoxLength = 0.5 + (controls.Sprint ? 0.25 : (controls.Sneak ? 0.05 : 0.2));
				double centerX = (entityCollisionBox.X1 + entityCollisionBox.X2) / 2.0;
				double centerZ = (entityCollisionBox.Z1 + entityCollisionBox.Z2) / 2.0;
				double searchHeight = Math.Max(entityCollisionBox.Y1 + (double)StepHeight, entityCollisionBox.Y2);
				Vec3d vec3d = controls.WalkVector.Clone().Clone().Normalize();
				double outerX = vec3d.X * searchBoxLength;
				double outerZ = vec3d.Z * searchBoxLength;
				double entityHalfWidth = entityCollisionBox.Width / 2.0;
				double entityHalfLength = entityCollisionBox.Length / 2.0;
				outerX += (double)Math.Sign(outerX) * entityHalfWidth;
				outerZ += (double)Math.Sign(outerZ) * entityHalfLength;
				Cuboidd cuboidd = new Cuboidd();
				cuboidd.X1 = Math.Min(-entityHalfWidth, outerX);
				cuboidd.X2 = Math.Max(entityHalfWidth, outerX);
				cuboidd.Z1 = Math.Min(-entityHalfLength, outerZ);
				cuboidd.Z2 = Math.Max(entityHalfLength, outerZ);
				cuboidd.Y1 = (double)entity2.CollisionBox.Y1 + 0.01 - ((!entity2.CollidedVertically && !controls.Jump) ? 0.05 : 0.0);
				cuboidd.Y2 = searchHeight;
				cuboidd.Translate(centerX, 0.0, centerZ);
				Cuboidd box = cuboidd;
				float scaleX2 = (float)box.Width / 2f;
				float scaleY2 = (float)box.Height / 2f;
				float scaleZ2 = (float)box.Length / 2f;
				double X2;
				double Y2;
				double Z2;
				if (entity2 == this.game.EntityPlayer)
				{
					X2 = this.game.EntityPlayer.CameraPos.X;
					Y2 = this.game.EntityPlayer.CameraPos.Y + (double)(plrDim * 32768);
					Z2 = this.game.EntityPlayer.CameraPos.Z;
				}
				else
				{
					X2 = entity2.Pos.X;
					Y2 = entity2.Pos.InternalY;
					Z2 = entity2.Pos.Z;
				}
				double x3 = X2 + box.X1 + (double)scaleX2;
				double y3 = Y2 + box.Y1 + (double)scaleY2;
				double z3 = Z2 + box.Z1 + (double)scaleZ2;
				float lineWidth2 = 1.5f;
				this.entityWf.Render(this.game.api, x3, y3, z3, scaleX2, scaleY2, scaleZ2, lineWidth2, new Vec4f(0.8f, 0.6f, 0.1f, 1f));
			}
		}

		private void renderBoxes(Block block, BlockPos tmpPos, Vec4f color)
		{
			if (block == null)
			{
				return;
			}
			foreach (Cuboidf box in block.GetSelectionBoxes(this.game.blockAccessor, tmpPos))
			{
				this.entityWf.Render(this.game.api, (double)((float)tmpPos.X + box.MidX), (double)((float)tmpPos.Y + box.MidY), (double)((float)tmpPos.Z + box.MidZ), box.XSize / 2f, box.YSize / 2f, box.ZSize / 2f, 1f, color);
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private WireframeCube chunkWf;

		private WireframeCube entityWf;

		private WireframeCube beWf;
	}
}
