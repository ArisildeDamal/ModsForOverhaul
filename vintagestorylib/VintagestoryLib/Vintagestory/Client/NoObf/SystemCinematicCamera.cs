using System;
using System.IO;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class SystemCinematicCamera : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "cica";
			}
		}

		public SystemCinematicCamera(ClientMain game)
			: base(game)
		{
			this.InitModel();
			this.platform = game.Platform;
			this.cameraPoints = new CameraPoint[256];
			this.cameraPointsCount = 0;
			CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
			game.api.ChatCommands.Create("cam").WithPreCondition(new CommandPreconditionDelegate(this.checkPrecond)).BeginSubCommand("tp")
				.WithDescription("Whether to teleport the player back to where he was previously (default on)")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("teleportBack", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdTp))
				.EndSubCommand()
				.BeginSubCommand("gui")
				.WithDescription(">If one, will disable the guis during the duration of the recording (default on)")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalBool("shouldDisableGui", "on") })
				.HandleWith(new OnCommandDelegate(this.OnCmdGui))
				.EndSubCommand()
				.BeginSubCommand("loop")
				.WithDescription("If the path should be looped")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("quantityLoops", 9999999) })
				.HandleWith(new OnCommandDelegate(this.OnCmdLoop))
				.EndSubCommand()
				.BeginSubCommand("p")
				.WithDescription("Add a point to path")
				.HandleWith(new OnCommandDelegate(this.OnCmdAddPoint))
				.EndSubCommand()
				.BeginSubCommand("up")
				.WithDescription("Update nth point")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("point_index", 0) })
				.HandleWith(new OnCommandDelegate(this.OnCmdUp))
				.EndSubCommand()
				.BeginSubCommand("goto")
				.WithDescription("Teleport to nth point")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalInt("point_index", 0) })
				.HandleWith(new OnCommandDelegate(this.OnCmdGoto))
				.EndSubCommand()
				.BeginSubCommand("cp")
				.WithDescription("Close path")
				.HandleWith(new OnCommandDelegate(this.OnCmdCp))
				.EndSubCommand()
				.BeginSubCommand("rp")
				.WithDescription("Remove the last point from the path")
				.HandleWith(new OnCommandDelegate(this.OnCmdRemovePoint))
				.EndSubCommand()
				.BeginSubCommand("play")
				.WithAlias(new string[] { "start" })
				.WithAlias(new string[] { "rec" })
				.WithDescription("Play/start path or play and record to .avi file using rec command")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalDouble("totalTime", 10.0) })
				.HandleWith(new OnCommandDelegate(this.OnCmdPlay))
				.EndSubCommand()
				.BeginSubCommand("stop")
				.WithDescription("Stop playing and recording")
				.HandleWith(new OnCommandDelegate(this.OnCmdStop))
				.EndSubCommand()
				.BeginSubCommand("clear")
				.WithDescription("Remove all points from path")
				.HandleWith(new OnCommandDelegate(this.OnCmdClear))
				.EndSubCommand()
				.BeginSubCommand("save")
				.WithDescription("Copy path points to clipboard")
				.HandleWith(new OnCommandDelegate(this.OnCmdSave))
				.EndSubCommand()
				.BeginSubCommand("load")
				.WithDescription("Load point to the path, seperated by ,")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("points") })
				.HandleWith(new OnCommandDelegate(this.OnCmdLoad))
				.EndSubCommand()
				.BeginSubCommand("loadold")
				.WithDescription("Load point to the path from a pre 1.20 point list, seperated by ,")
				.WithArgs(new ICommandArgumentParser[] { parsers.Word("points") })
				.HandleWith(new OnCommandDelegate(this.OnCmdLoadOld))
				.EndSubCommand()
				.BeginSubCommand("alpha")
				.WithDescription("Set/Show alpha")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalFloat("alpha", 0f) })
				.HandleWith(new OnCommandDelegate(this.OnCmdAlpha))
				.EndSubCommand()
				.BeginSubCommand("angles")
				.WithDescription("Set camera angle mode [point, direction]")
				.WithArgs(new ICommandArgumentParser[] { parsers.OptionalWord("mode") })
				.HandleWith(new OnCommandDelegate(this.OnCmdAngles))
				.EndSubCommand();
			game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderFrame3D), EnumRenderStage.Opaque, "cinecam", 0.699999988079071);
			game.eventManager.RegisterRenderer(new Action<float>(this.OnFinalizeFrame), EnumRenderStage.Done, "cinecam-done", 0.9800000190734863);
		}

		private TextCommandResult checkPrecond(TextCommandCallingArgs args)
		{
			IPlayer plr = args.Caller.Player;
			if (plr == null || (plr.WorldData.CurrentGameMode != EnumGameMode.Creative && plr.WorldData.CurrentGameMode != EnumGameMode.Spectator))
			{
				return TextCommandResult.Error("Only available in creative or spectator mode", "");
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdAngles(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing || args[0] as string == "point")
			{
				this.camAngleMode = EnumAutoCamAngleMode.Point;
			}
			else
			{
				this.camAngleMode = EnumAutoCamAngleMode.Direction;
			}
			return TextCommandResult.Success("Camera angle mode is now " + this.camAngleMode.ToString(), null);
		}

		private TextCommandResult OnCmdAlpha(TextCommandCallingArgs args)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success("Current alpha is " + this.alpha.ToString(), null);
			}
			this.alpha = (double)((float)args[0]);
			this.GenerateCameraPathModel();
			return TextCommandResult.Success("Alpha set to " + this.alpha.ToString(), null);
		}

		private TextCommandResult OnCmdStop(TextCommandCallingArgs args)
		{
			this.StopAutoCamera();
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdPlay(TextCommandCallingArgs args)
		{
			if (this.cameraPointsCount < 2)
			{
				this.game.ShowChatMessage("Need at least 2 points!");
			}
			else
			{
				this.StartAutoCamera(args);
			}
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdLoop(TextCommandCallingArgs args)
		{
			this.quantityLoops = (int)args[0];
			return TextCommandResult.Success("Will loop " + this.quantityLoops.ToString() + " times.", null);
		}

		private TextCommandResult OnCmdGui(TextCommandCallingArgs args)
		{
			this.shouldDisableGui = (bool)args[0];
			return TextCommandResult.Success("Disable guis now " + (this.shouldDisableGui ? "on" : "off"), null);
		}

		private TextCommandResult OnCmdTp(TextCommandCallingArgs args)
		{
			this.teleportBack = (bool)args[0];
			return TextCommandResult.Success("Teleport back to previous position now " + (this.teleportBack ? "on" : "off"), null);
		}

		private void InitModel()
		{
			this.cameraPathModel = new MeshData(4, 4, false, false, true, true);
			this.cameraPathModel.SetMode(EnumDrawMode.LineStrip);
			this.cameraPathModelRef = null;
		}

		public void OnRenderFrame3D(float deltaTime)
		{
			if (!this.game.ShouldRender2DOverlays || this.cameraPathModelRef == null)
			{
				return;
			}
			ShaderProgramAutocamera autocamera = ShaderPrograms.Autocamera;
			autocamera.Use();
			this.game.Platform.GLLineWidth(2f);
			this.game.Platform.BindTexture2d(0);
			this.game.GlPushMatrix();
			this.game.GlLoadMatrix(this.game.MainCamera.CameraMatrixOrigin);
			Vec3d cameraPos = this.game.EntityPlayer.CameraPos;
			this.game.GlTranslate((double)((float)((double)this.origin.X - cameraPos.X)), (double)((float)((double)this.origin.Y - cameraPos.Y)), (double)((float)((double)this.origin.Z - cameraPos.Z)));
			autocamera.ProjectionMatrix = this.game.CurrentProjectionMatrix;
			autocamera.ModelViewMatrix = this.game.CurrentModelViewMatrix;
			this.game.Platform.RenderMesh(this.cameraPathModelRef);
			this.game.GlPopMatrix();
			autocamera.Stop();
		}

		private TextCommandResult OnCmdLoad(TextCommandCallingArgs args)
		{
			return TextCommandResult.Success(string.Format("Camera points loaded: {0}", this.load(args, 0f).ToString() ?? ""), null);
		}

		private TextCommandResult OnCmdLoadOld(TextCommandCallingArgs args)
		{
			return TextCommandResult.Success(string.Format("Camera points loaded: {0}", this.load(args, 1.5707964f).ToString() ?? ""), null);
		}

		private int load(TextCommandCallingArgs args, float yawOffset = 0f)
		{
			string[] points = (args[0] as string).Split(',', StringSplitOptions.None);
			int i = (points.Length - 1) / 6;
			this.cameraPointsCount = 0;
			this.origin = this.game.EntityPlayer.Pos.AsBlockPos;
			for (int j = 0; j < i; j++)
			{
				CameraPoint point = new CameraPoint();
				point.x = (double)((float)int.Parse(points[1 + j * 6]) / 100f);
				point.y = (double)((float)int.Parse(points[1 + j * 6 + 1]) / 100f);
				point.z = (double)((float)int.Parse(points[1 + j * 6 + 2]) / 100f);
				point.pitch = (float)int.Parse(points[1 + j * 6 + 3]) / 1000f;
				point.yaw = (float)int.Parse(points[1 + j * 6 + 4]) / 1000f + yawOffset;
				point.roll = (float)int.Parse(points[1 + j * 6 + 5]) / 1000f;
				if (this.cameraPointsCount - 1 >= 0 && this.cameraPoints[this.cameraPointsCount - 1].PositionEquals(point))
				{
					point.x += 0.0010000000474974513;
				}
				CameraPoint[] array = this.cameraPoints;
				int num = this.cameraPointsCount;
				this.cameraPointsCount = num + 1;
				array[num] = point;
			}
			this.GenerateCameraPathModel();
			return i;
		}

		private TextCommandResult OnCmdSave(TextCommandCallingArgs args)
		{
			string s = this.PointsToString();
			this.platform.XPlatInterface.SetClipboardText(s);
			return TextCommandResult.Success("Camera points copied to clipboard.", null);
		}

		protected string PointsToString()
		{
			string s = "1,";
			for (int i = 0; i < this.cameraPointsCount; i++)
			{
				CameraPoint point = this.cameraPoints[i];
				s = string.Format("{0}{1},", s, ((int)(point.x * 100.0)).ToString() ?? "");
				s = string.Format("{0}{1},", s, ((int)(point.y * 100.0)).ToString() ?? "");
				s = string.Format("{0}{1},", s, ((int)(point.z * 100.0)).ToString() ?? "");
				s = string.Format("{0}{1},", s, ((int)(point.pitch * 1000f)).ToString() ?? "");
				s = string.Format("{0}{1},", s, ((int)(point.yaw * 1000f)).ToString() ?? "");
				s = string.Format("{0}{1}", s, ((int)(point.roll * 1000f)).ToString() ?? "");
				if (i != this.cameraPointsCount - 1)
				{
					s = string.Format("{0},", s);
				}
			}
			return s;
		}

		private TextCommandResult OnCmdAddPoint(TextCommandCallingArgs args)
		{
			if (this.cameraPointsCount == 0)
			{
				this.origin = this.game.EntityPlayer.Pos.AsBlockPos;
			}
			CameraPoint point = CameraPoint.FromEntityPos(this.game.EntityPlayer.Pos);
			if (this.cameraPointsCount - 1 >= 0 && this.cameraPoints[this.cameraPointsCount - 1].PositionEquals(point))
			{
				point.x += 0.0010000000474974513;
			}
			CameraPoint[] array = this.cameraPoints;
			int num = this.cameraPointsCount;
			this.cameraPointsCount = num + 1;
			array[num] = point;
			if (this.cameraPointsCount > 1)
			{
				this.FixYaw(this.cameraPointsCount);
			}
			this.closedPath = false;
			this.GenerateCameraPathModel();
			return TextCommandResult.Success("Point added", null);
		}

		private TextCommandResult OnCmdUp(TextCommandCallingArgs args)
		{
			int num = (int)args[0];
			if (num < 0 || num >= this.cameraPointsCount)
			{
				return TextCommandResult.Success("Your supplied number is above the point count or negative", null);
			}
			CameraPoint point = CameraPoint.FromEntityPos(this.game.EntityPlayer.Pos);
			this.cameraPoints[num] = point;
			if (this.cameraPointsCount > 1)
			{
				this.FixYaw(this.cameraPointsCount);
			}
			this.GenerateCameraPathModel();
			return TextCommandResult.Success("Point updated", null);
		}

		private TextCommandResult OnCmdGoto(TextCommandCallingArgs args)
		{
			int num = (int)args[0];
			if (num < 0 || num >= this.cameraPointsCount)
			{
				return TextCommandResult.Success("Your supplied number is above the point count or negative", null);
			}
			CameraPoint p = this.cameraPoints[num];
			this.game.EntityPlayer.Pos.X = p.x;
			this.game.EntityPlayer.Pos.Y = p.y;
			this.game.EntityPlayer.Pos.Z = p.z;
			this.game.mouseYaw = (this.game.EntityPlayer.Pos.Yaw = p.yaw);
			this.game.mousePitch = (this.game.EntityPlayer.Pos.Pitch = p.pitch);
			this.game.EntityPlayer.Pos.Roll = p.roll;
			return TextCommandResult.Success("", null);
		}

		private TextCommandResult OnCmdCp(TextCommandCallingArgs args)
		{
			if (this.cameraPointsCount <= 1)
			{
				return TextCommandResult.Success("Requires at least 2 points", null);
			}
			CameraPoint[] array = this.cameraPoints;
			int num = this.cameraPointsCount;
			this.cameraPointsCount = num + 1;
			array[num] = this.cameraPoints[0].Clone();
			this.FixYaw(this.cameraPointsCount);
			this.game.ShowChatMessage("Path Closed");
			this.closedPath = true;
			this.GenerateCameraPathModel();
			return TextCommandResult.Success("", null);
		}

		private void FixYaw(int pos)
		{
			double prevYaw = (double)this.cameraPoints[pos - 2].yaw;
			double num = (double)this.cameraPoints[pos - 1].yaw - prevYaw;
			if (num > 3.141592653589793)
			{
				this.cameraPoints[pos - 1].yaw -= 6.2831855f;
			}
			if (num < -3.141592653589793)
			{
				this.cameraPoints[pos - 1].yaw += 6.2831855f;
			}
		}

		private TextCommandResult OnCmdRemovePoint(TextCommandCallingArgs args)
		{
			this.cameraPointsCount = Math.Max(0, this.cameraPointsCount - 1);
			this.closedPath = false;
			this.GenerateCameraPathModel();
			return TextCommandResult.Success("Point removed", null);
		}

		private TextCommandResult OnCmdClear(TextCommandCallingArgs args)
		{
			this.closedPath = false;
			this.cameraPointsCount = 0;
			this.StopAutoCamera();
			this.InitModel();
			return TextCommandResult.Success("Camera points cleared.", null);
		}

		private void StartAutoCamera(TextCommandCallingArgs args)
		{
			if (!this.game.AllowFreemove)
			{
				this.game.ShowChatMessage("Free move not allowed.");
				return;
			}
			if (this.cameraPointsCount == 0)
			{
				this.game.ShowChatMessage("No points defined. Enter points with \".cam p\" Command.");
				return;
			}
			ClientWorldPlayerData wdata = this.game.player.worlddata;
			this.prevWData = wdata.Clone();
			this.game.player.worlddata.RequestMode(this.game, wdata.MoveSpeedMultiplier, wdata.PickingRange, EnumGameMode.Spectator, true, true, EnumFreeMovAxisLock.None, ClientSettings.RenderMetaBlocks);
			this.currentPoint = 0;
			this.totalDistance = this.CalculateApproximateDistances();
			if (args.SubCmdCode == "rec")
			{
				string path = ((ClientSettings.VideoFileTarget == null) ? GamePaths.Videos : ClientSettings.VideoFileTarget);
				try
				{
					if (new DirectoryInfo(path).Parent != null && !Directory.Exists(path))
					{
						Directory.CreateDirectory(path);
					}
					this.avi = this.platform.CreateAviWriter(ClientSettings.RecordingFrameRate, ClientSettings.RecordingCodec);
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
					defaultInterpolatedStringHandler.AppendFormatted<DateTime>(DateTime.Now, "yyyy-MM-dd_HH-mm-ss");
					string time = defaultInterpolatedStringHandler.ToStringAndClear();
					this.avi.Open(Path.Combine(path, this.videoFileName = time + ".avi"), this.game.Width, this.game.Height);
				}
				catch (Exception e)
				{
					this.game.ShowChatMessage("Could not start recording: " + e.Message);
					return;
				}
				if (ClientSettings.GameTickFrameRate > 0f)
				{
					this.game.DeltaTimeLimiter = 1f / ClientSettings.GameTickFrameRate;
				}
			}
			this.totalTime = (double)args[0];
			this.firstFrameDone = false;
			this.currentPoint = 0;
			this.currentLoop = 0;
			this.currentLengthTraveled = 0.0;
			EntityPos pos = this.game.EntityPlayer.Pos;
			this.previousPositionX = pos.X;
			this.previousPositionY = pos.Y;
			this.previousPositionZ = pos.Z;
			this.previousOrientationX = (double)pos.Pitch;
			this.previousOrientationY = (double)pos.Yaw;
			this.previousOrientationZ = (double)pos.Roll;
			if (this.shouldDisableGui)
			{
				this.game.ShouldRender2DOverlays = false;
			}
			this.game.AllowCameraControl = false;
			this.game.MainCamera.UpdateCameraPos = false;
			this.PrecalcCurrentPoint();
			this.cameraLive = true;
		}

		private void StopAutoCamera()
		{
			if (this.shouldDisableGui)
			{
				this.game.ShouldRender2DOverlays = true;
			}
			this.game.AllowCameraControl = true;
			this.game.MainCamera.UpdateCameraPos = true;
			if (this.cameraLive)
			{
				this.game.player.worlddata.RequestMode(this.game, this.prevWData.MoveSpeedMultiplier, this.prevWData.PickingRange, this.prevWData.CurrentGameMode, this.prevWData.FreeMove, this.prevWData.NoClip, this.prevWData.FreeMovePlaneLock, ClientSettings.RenderMetaBlocks);
				if (this.teleportBack)
				{
					EntityPos pos = this.game.EntityPlayer.Pos;
					pos.X = this.previousPositionX;
					pos.Y = this.previousPositionY;
					pos.Z = this.previousPositionZ;
					pos.Pitch = (float)this.previousOrientationX;
					pos.Yaw = (float)this.previousOrientationY;
					pos.Roll = (float)this.previousOrientationZ;
				}
			}
			if (this.avi != null)
			{
				this.avi.Close();
				this.avi = null;
				string path = ((ClientSettings.VideoFileTarget == null) ? GamePaths.Videos : ClientSettings.VideoFileTarget);
				this.game.ShowChatMessage(this.videoFileName + " saved to " + path);
			}
			this.cameraLive = false;
			this.game.DeltaTimeLimiter = -1f;
		}

		public void OnFinalizeFrame(float dt)
		{
			if (!this.cameraLive)
			{
				return;
			}
			if (this.game.IsPaused)
			{
				return;
			}
			this.UpdateAvi(dt);
			if (this.currentPoint == this.cameraPointsCount - 1)
			{
				this.StopAutoCamera();
				File.AppendAllText(this.prevPathsFile, this.PointsToString() + "\r\n");
				this.game.ShowChatMessage("Camera path saved to Cache folder, in case you loose it");
				return;
			}
			double travelSpeed = this.totalDistance / this.totalTime;
			this.currentLengthTraveled += travelSpeed * (double)dt;
			double percentTraveled = this.currentLengthTraveled / this.points[1].distance;
			this.AdvanceTo(Math.Min(1.0, percentTraveled));
			if (percentTraveled > 1.0)
			{
				this.currentPoint++;
				if (this.currentPoint != this.cameraPointsCount - 1 || this.currentLoop < this.quantityLoops)
				{
					if (this.currentLoop < this.quantityLoops && this.currentPoint == this.cameraPointsCount - 1)
					{
						this.currentPoint = 0;
						this.currentLoop++;
					}
					this.PrecalcCurrentPoint();
					this.currentLengthTraveled = 0.0;
				}
			}
		}

		public void AdvanceTo(double percent)
		{
			EntityPos pos = this.game.EntityPlayer.Pos;
			Vec3d cameraPos = this.game.EntityPlayer.CameraPos;
			double prevX = pos.X;
			double prevY = pos.Y;
			double prevZ = pos.Z;
			cameraPos.X = (pos.X = GameMath.CPCatmullRomSplineLerp(this.tstart + percent * (this.tend - this.tstart), this.pointsX, this.time));
			cameraPos.Y = (pos.Y = GameMath.CPCatmullRomSplineLerp(this.tstart + percent * (this.tend - this.tstart), this.pointsY, this.time));
			cameraPos.Z = (pos.Z = GameMath.CPCatmullRomSplineLerp(this.tstart + percent * (this.tend - this.tstart), this.pointsZ, this.time));
			cameraPos.Add((double)this.game.MainCamera.CameraShakeStrength * GameMath.Cos(this.game.MainCamera.deltaSum * 100.0) / 10.0, (double)(-(double)this.game.MainCamera.CameraShakeStrength) * GameMath.Cos(this.game.MainCamera.deltaSum * 100.0) / 10.0, (double)this.game.MainCamera.CameraShakeStrength * GameMath.Sin(this.game.MainCamera.deltaSum * 100.0) / 10.0);
			if (this.camAngleMode == EnumAutoCamAngleMode.Point)
			{
				this.game.mousePitch = (pos.Pitch = (float)GameMath.CPCatmullRomSplineLerp(this.tstart + percent * (this.tend - this.tstart), this.pointsPitch, this.time));
				this.game.mouseYaw = (pos.Yaw = (float)GameMath.CPCatmullRomSplineLerp(this.tstart + percent * (this.tend - this.tstart), this.pointsYaw, this.time));
				pos.Roll = (float)GameMath.CPCatmullRomSplineLerp(this.tstart + percent * (this.tend - this.tstart), this.pointsRoll, this.time);
				return;
			}
			double dx = pos.X - prevX;
			double dy = pos.Y - prevY;
			double dz = pos.Z - prevZ;
			double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
			if (length > 0.0)
			{
				double np = GameMath.Asin(dy / length);
				double ny = Math.Atan2(dx, dz) + 1.5707963705062866;
				pos.Pitch += (float)Math.Min(0.03, np - (double)(pos.Pitch % 6.2831855f));
				pos.Yaw += (float)Math.Min(0.03, ny - (double)(pos.Yaw % 6.2831855f));
			}
		}

		private void PrecalcCurrentPoint()
		{
			this.points[1] = this.cameraPoints[this.currentPoint];
			this.points[2] = this.cameraPoints[this.currentPoint + 1];
			if (this.currentPoint > 0)
			{
				this.points[0] = this.cameraPoints[this.currentPoint - 1];
			}
			else
			{
				this.points[0] = (this.closedPath ? this.cameraPoints[this.cameraPointsCount - 2] : this.points[1].ExtrapolateFrom(this.points[2], 1));
			}
			if (this.currentPoint < this.cameraPointsCount - 2)
			{
				this.points[3] = this.cameraPoints[this.currentPoint + 2];
			}
			else
			{
				this.points[3] = (this.closedPath ? this.cameraPoints[1] : this.points[2].ExtrapolateFrom(this.points[1], 1));
			}
			this.time[0] = 0.0;
			this.time[1] = 1.0;
			this.time[2] = 2.0;
			this.time[3] = 3.0;
			double total = 0.0;
			for (int i = 1; i < 4; i++)
			{
				double num = this.points[i].x - this.points[i - 1].x;
				double dy = this.points[i].y - this.points[i - 1].y;
				double dz = this.points[i].z - this.points[i - 1].z;
				double dt = Math.Pow(num * num + dy * dy + dz * dz, this.alpha);
				total += dt;
				this.time[i] = total;
			}
			for (int j = 0; j < 4; j++)
			{
				this.pointsX[j] = this.points[j].x;
				this.pointsY[j] = this.points[j].y;
				this.pointsZ[j] = this.points[j].z;
				this.pointsPitch[j] = (double)this.points[j].pitch;
				this.pointsYaw[j] = (double)this.points[j].yaw;
				this.pointsRoll[j] = (double)this.points[j].roll;
			}
			this.tstart = this.time[1];
			this.tend = this.time[2];
		}

		private void UpdateAvi(float dt)
		{
			if (this.avi == null)
			{
				return;
			}
			if (!this.firstFrameDone)
			{
				this.firstFrameDone = true;
				return;
			}
			this.writeAccum += (double)dt;
			float frameRate = ClientSettings.RecordingFrameRate;
			if (this.writeAccum >= (double)(1f / frameRate))
			{
				this.writeAccum -= (double)(1f / frameRate);
				this.avi.AddFrame();
			}
		}

		private double CalculateApproximateDistances()
		{
			double totalDistance = 0.0;
			Vec3d cur = new Vec3d();
			Vec3d prev = new Vec3d();
			int currentPointBefore = this.currentPoint;
			this.currentPoint = 0;
			while (this.currentPoint < this.cameraPointsCount - 1)
			{
				this.PrecalcCurrentPoint();
				CameraPoint point = this.cameraPoints[this.currentPoint];
				prev.X = point.x;
				prev.Y = point.y;
				prev.Z = point.z;
				double distance = 0.0;
				for (int i = 0; i < 60; i++)
				{
					double dt = (double)((float)i / 60f);
					cur.X = GameMath.CPCatmullRomSplineLerp(this.tstart + dt * (this.tend - this.tstart), this.pointsX, this.time);
					cur.Y = GameMath.CPCatmullRomSplineLerp(this.tstart + dt * (this.tend - this.tstart), this.pointsY, this.time);
					cur.Z = GameMath.CPCatmullRomSplineLerp(this.tstart + dt * (this.tend - this.tstart), this.pointsZ, this.time);
					distance += Math.Sqrt((double)cur.SquareDistanceTo(prev));
					prev.X = cur.X;
					prev.Y = cur.Y;
					prev.Z = cur.Z;
				}
				point.distance = distance;
				totalDistance += point.distance;
				this.currentPoint++;
			}
			this.currentPoint = currentPointBefore;
			return totalDistance;
		}

		private void GenerateCameraPathModel()
		{
			this.InitModel();
			int vertexIndex = 0;
			this.currentPoint = 0;
			while (this.currentPoint < this.cameraPointsCount - 1)
			{
				this.PrecalcCurrentPoint();
				for (int i = 0; i <= 30; i++)
				{
					double dt = (double)((float)i / 30f);
					double x = GameMath.CPCatmullRomSplineLerp(this.tstart + dt * (this.tend - this.tstart), this.pointsX, this.time);
					double y = GameMath.CPCatmullRomSplineLerp(this.tstart + dt * (this.tend - this.tstart), this.pointsY, this.time);
					double z = GameMath.CPCatmullRomSplineLerp(this.tstart + dt * (this.tend - this.tstart), this.pointsZ, this.time);
					this.cameraPathModel.AddVertexSkipTex((float)(x - (double)this.origin.X), (float)(y - (double)this.origin.Y), (float)(z - (double)this.origin.Z), (this.currentPoint % 2 == 0) ? (-1) : ColorUtil.ToRgba(255, 255, 50, 50));
					this.cameraPathModel.AddIndex(vertexIndex++);
				}
				this.currentPoint++;
			}
			this.currentPoint = 0;
			if (this.cameraPathModelRef != null)
			{
				this.platform.DeleteMesh(this.cameraPathModelRef);
			}
			this.cameraPathModelRef = this.platform.UploadMesh(this.cameraPathModel);
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.Misc;
		}

		private ClientPlatformAbstract platform;

		private CameraPoint[] cameraPoints;

		private int cameraPointsCount;

		private ClientWorldPlayerData prevWData;

		private double previousPositionX;

		private double previousPositionY;

		private double previousPositionZ;

		private double previousOrientationX;

		private double previousOrientationY;

		private double previousOrientationZ;

		private double totalDistance;

		private double totalTime;

		private double currentLengthTraveled;

		private int currentPoint;

		private int currentLoop;

		private bool cameraLive;

		private bool teleportBack;

		private int quantityLoops;

		private bool closedPath;

		private double alpha = 0.10000000149011612;

		private IAviWriter avi;

		private double writeAccum;

		private bool firstFrameDone;

		private bool shouldDisableGui = true;

		private MeshData cameraPathModel;

		private MeshRef cameraPathModelRef;

		private BlockPos origin;

		private EnumAutoCamAngleMode camAngleMode = EnumAutoCamAngleMode.Point;

		private string videoFileName = "";

		private string prevPathsFile = Path.Combine(GamePaths.Cache, "campaths.txt");

		private double[] time = new double[4];

		private CameraPoint[] points = new CameraPoint[4];

		private double[] pointsX = new double[4];

		private double[] pointsY = new double[4];

		private double[] pointsZ = new double[4];

		private double[] pointsPitch = new double[4];

		private double[] pointsYaw = new double[4];

		private double[] pointsRoll = new double[4];

		private double tstart;

		private double tend;
	}
}
