using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemSelectedBlockOutline : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "sbo";
			}
		}

		public SystemSelectedBlockOutline(ClientMain game)
			: base(game)
		{
			this.cubeWireFrame = WireframeCube.CreateUnitCube(game.api, -1);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3DPost), EnumRenderStage.AfterFinalComposition, this.Name, 0.9);
		}

		public override void Dispose(ClientMain game)
		{
			this.cubeWireFrame.Dispose();
		}

		public void OnRenderFrame3DPost(float deltaTime)
		{
			if (!ClientSettings.SelectedBlockOutline)
			{
				return;
			}
			float linewidthMul = ClientSettings.Wireframethickness;
			if (this.game.ShouldRender2DOverlays && this.game.BlockSelection != null)
			{
				BlockPos pos = this.game.BlockSelection.Position;
				if (this.game.BlockSelection.DidOffset)
				{
					pos = pos.AddCopy(this.game.BlockSelection.Face.Opposite);
				}
				Block block = this.game.WorldMap.RelaxedBlockAccess.GetBlock(pos, 2);
				Cuboidf[] boxes;
				if (block.SideSolid.Any)
				{
					boxes = block.GetSelectionBoxes(this.game.WorldMap.RelaxedBlockAccess, pos);
				}
				else
				{
					block = this.game.WorldMap.RelaxedBlockAccess.GetBlock(pos);
					boxes = this.game.GetBlockIntersectionBoxes(pos);
				}
				if (boxes == null || boxes.Length == 0)
				{
					return;
				}
				bool partialSelection = block.DoParticalSelection(this.game, pos);
				Vec4f color = block.GetSelectionColor(this.game.api, pos);
				double x = (double)pos.X + this.game.Player.Entity.CameraPosOffset.X;
				double y = (double)pos.InternalY + this.game.Player.Entity.CameraPosOffset.Y;
				double z = (double)pos.Z + this.game.Player.Entity.CameraPosOffset.Z;
				for (int i = 0; i < boxes.Length; i++)
				{
					if (partialSelection)
					{
						i = this.game.BlockSelection.SelectionBoxIndex;
					}
					if (boxes.Length <= i)
					{
						break;
					}
					Cuboidf box = boxes[i];
					if (box is DecorSelectionBox)
					{
						if (partialSelection)
						{
							return;
						}
					}
					else
					{
						double posx = x + (double)box.X1;
						double posy = y + (double)box.Y1;
						double posz = z + (double)box.Z1;
						this.cubeWireFrame.Render(this.game.api, posx, posy, posz, box.XSize, box.YSize, box.ZSize, 1.6f * linewidthMul, color);
						if (partialSelection)
						{
							break;
						}
					}
				}
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Render;
		}

		private WireframeCube cubeWireFrame;
	}
}
