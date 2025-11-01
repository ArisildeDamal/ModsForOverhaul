using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ClientSystemIntroMenu : ClientSystem
	{
		public override string Name
		{
			get
			{
				return "intromenu";
			}
		}

		public override EnumClientSystemType GetSystemType()
		{
			return EnumClientSystemType.HudElement;
		}

		public ClientSystemIntroMenu(ClientMain game)
			: base(game)
		{
		}

		public override void OnBlockTexturesLoaded()
		{
			this.capi = this.game.api;
		}

		private void onEventBus(string eventName, ref EnumHandling handling, IAttribute data)
		{
			if (eventName == "skipcharacterselection" || eventName == "finishcharacterselection")
			{
				bool saveExtendedDebugInfo = this.game.extendedDebugInfo;
				this.game.extendedDebugInfo = false;
				this.capi.Event.RegisterCallback(new Action<float>(this.after15Secs), 15000);
				this.game.extendedDebugInfo = saveExtendedDebugInfo;
			}
		}

		private void after15Secs(float obj)
		{
			this.slideInTextBoxTexture = this.capi.Gui.TextTexture.GenTextTexture(Lang.Get("gameintrotip", Array.Empty<object>()).Replace("\\n", "\n"), CairoFont.WhiteSmallText(), new TextBackground
			{
				FillColor = GuiStyle.DialogDefaultBgColor,
				BorderColor = GuiStyle.DialogBorderColor,
				BorderWidth = 1.0,
				Padding = 10
			});
		}

		public void OnRenderGUI(float deltaTime)
		{
			if (this.slideInTextBoxTexture != null)
			{
				if (this.openkeydown)
				{
					this.pressAccum += deltaTime;
				}
				else
				{
					this.pressAccum = Math.Max(0f, this.pressAccum - deltaTime);
				}
				if (this.pressAccum > 1f)
				{
					this.pressAccum = 1f;
					if (this.openkeydown)
					{
						this.game.LoadedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogFirstlaunchInfo).TryOpen();
					}
					this.slideInTextBoxTexture.Dispose();
					this.slideInTextBoxTexture = null;
					return;
				}
				this.accum += deltaTime;
				float px = GameMath.Clamp(1.5f * this.accum / 1f, 0f, 1f) - 1f;
				float dx = (float)(this.capi.World.Rand.NextDouble() - 0.5) * 6f * RuntimeEnv.GUIScale * this.pressAccum;
				float dy = (float)(this.capi.World.Rand.NextDouble() - 0.5) * 6f * RuntimeEnv.GUIScale * this.pressAccum;
				this.capi.Render.Render2DLoadedTexture(this.slideInTextBoxTexture, px * (float)this.slideInTextBoxTexture.Width + dx, 80f * RuntimeEnv.GUIScale + dy, 50f);
			}
		}

		public override void OnKeyDown(KeyEvent args)
		{
			base.OnKeyDown(args);
			if (this.slideInTextBoxTexture != null && args.KeyCode == 93)
			{
				args.Handled = true;
				this.openkeydown = true;
			}
		}

		public override void OnKeyUp(KeyEvent args)
		{
			base.OnKeyUp(args);
			if (this.slideInTextBoxTexture != null && args.KeyCode == 93)
			{
				this.openkeydown = false;
			}
		}

		internal override void OnLevelFinalize()
		{
			ServerInformation serverInfo = (this.capi.World as ClientMain).ServerInfo;
			if ((((serverInfo != null) ? serverInfo.Playstyle : null) == "creativebuilding" && ClientSettings.ShowCreativeHelpDialog) || ClientSettings.ShowSurvivalHelpDialog)
			{
				this.game.eventManager.RegisterRenderer(new Action<float>(this.OnRenderGUI), EnumRenderStage.Ortho, this.Name, 1.0);
				this.capi.Event.RegisterEventBusListener(new EventBusListenerDelegate(this.onEventBus), 0.5, null);
			}
		}

		private ICoreClientAPI capi;

		private LoadedTexture slideInTextBoxTexture;

		private float accum;

		private float pressAccum;

		private bool openkeydown;
	}
}
