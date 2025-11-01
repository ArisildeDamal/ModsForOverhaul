using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client
{
	internal class GuiScreenLogin : GuiScreen
	{
		public GuiScreenLogin(ScreenManager screenManager, GuiScreen parentScreen)
			: base(screenManager, parentScreen)
		{
			this.compose();
		}

		private void compose()
		{
			float dy = 0f;
			this.ElementComposer = ScreenManager.GuiComposers.Create("mainmenu-login", ElementBounds.Fill).AddShadedDialogBG(ElementBounds.Fill, false, 5.0, 0.75f).BeginChildElements(ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 160.0, 400.0, 500.0))
				.AddStaticText(Lang.Get("Please enter your game account credentials", Array.Empty<object>()), CairoFont.WhiteSmallishText(), EnumTextOrientation.Center, ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.None).WithFixedWidth(400.0), null)
				.AddIf(!this.requireTOTPCode)
				.AddStaticText(Lang.Get("E-Mail", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1.2f, 0.0, EnumDialogArea.None).WithFixedWidth(400.0), null)
				.AddTextInput(ElementStdBounds.Rowed(1.6f, 0.0, EnumDialogArea.None).WithFixedSize(400.0, 30.0), null, null, "email")
				.AddStaticText(Lang.Get("Password", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(2.3f, 0.0, EnumDialogArea.None).WithFixedWidth(400.0), null)
				.AddTextInput(ElementStdBounds.Rowed(2.7f, 0.0, EnumDialogArea.None).WithFixedSize(400.0, 30.0), null, null, "password")
				.EndIf()
				.AddIf(this.requireTOTPCode)
				.Execute(delegate
				{
					dy += 0.9f;
				})
				.AddStaticText(Lang.Get("TOTP Code", Array.Empty<object>()), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(3.2f, 0.0, EnumDialogArea.None).WithFixedWidth(400.0), null)
				.AddTextInput(ElementStdBounds.Rowed(3.6f, 0.0, EnumDialogArea.None).WithFixedSize(100.0, 30.0), null, null, "totpcode")
				.EndIf()
				.AddIf(!this.requireTOTPCode)
				.AddSmallButton(Lang.Get("Forgot Password?", Array.Empty<object>()), new ActionConsumable(this.OnForgotPwd), ElementStdBounds.Rowed(3.2f + dy, 0.0, EnumDialogArea.None).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.EndIf()
				.AddButton(Lang.Get("Login", Array.Empty<object>()), new ActionConsumable(this.OnLogin), ElementStdBounds.Rowed(4f + dy, 0.0, EnumDialogArea.None).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, "login")
				.AddButton(Lang.Get("Quit", Array.Empty<object>()), new ActionConsumable(this.OnQuit), ElementStdBounds.Rowed(4f + dy, 0.0, EnumDialogArea.None).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, null)
				.AddRichtext(string.Empty, CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(5f + dy, 0.0, EnumDialogArea.None).WithFixedSize(400.0, 100.0), null, "status")
				.EndChildElements()
				.Compose(true);
			GuiElementTextInput textInput = this.ElementComposer.GetTextInput("password");
			if (textInput != null)
			{
				textInput.HideCharacters();
			}
			GuiElementTextInput textInput2 = this.ElementComposer.GetTextInput("email");
			if (textInput2 == null)
			{
				return;
			}
			textInput2.SetValue(ClientSettings.UserEmail, true);
		}

		private bool OnForgotPwd()
		{
			NetUtil.OpenUrlInBrowser("https://account.vintagestory.at/requestresetpwd");
			return true;
		}

		private bool OnQuit()
		{
			ScreenManager.Platform.WindowExit("Login quit button was pressed");
			return true;
		}

		private bool OnLogin()
		{
			this.connecting = true;
			GuiElementTextInput textInput = this.ElementComposer.GetTextInput("email");
			string email = ((textInput != null) ? textInput.GetText() : null) ?? string.Empty;
			GuiElementTextInput textInput2 = this.ElementComposer.GetTextInput("password");
			string password = ((textInput2 != null) ? textInput2.GetText() : null) ?? string.Empty;
			GuiElementTextInput textInput3 = this.ElementComposer.GetTextInput("totpcode");
			string totpCode = ((textInput3 != null) ? textInput3.GetText() : null) ?? string.Empty;
			this.ElementComposer.GetButton("login").Enabled = false;
			this.ElementComposer.GetRichtext("status").SetNewText(Lang.Get("Connecting...", Array.Empty<object>()), CairoFont.WhiteSmallishText(), null);
			this.lastStatusUpdateMS = ScreenManager.Platform.EllapsedMs;
			this.ScreenManager.sessionManager.DoLogin(email, password, totpCode, this.prelogintoken, new Action<EnumAuthServerResponse, string, string, string>(this.OnLoginComplete));
			return true;
		}

		private void OnLoginComplete(EnumAuthServerResponse response, string failreason, string failreasondata, string prelogintoken)
		{
			this.logincomplete = true;
			this.response = response;
			this.failreason = failreason;
			this.failreasondata = failreasondata;
			if (this.response == EnumAuthServerResponse.Good)
			{
				this.prelogintoken = null;
			}
			else if (this.prelogintoken == null)
			{
				this.prelogintoken = prelogintoken;
			}
			this.connecting = false;
		}

		public override void OnKeyDown(KeyEvent e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == 49 || e.KeyCode == 82)
			{
				this.OnLogin();
				e.Handled = true;
			}
		}

		public override void RenderToDefaultFramebuffer(float dt)
		{
			this.ElementComposer.Render(dt);
			this.ElementComposer.PostRender(dt);
			if (this.connecting && ScreenManager.Platform.EllapsedMs - this.lastStatusUpdateMS > 1000L)
			{
				this.lastStatusUpdateMS = ScreenManager.Platform.EllapsedMs;
				GuiElementRichtext richtext = this.ElementComposer.GetRichtext("status");
				string text = (((int)(this.lastStatusUpdateMS / 1000L % 2L) == 0) ? Lang.Get("Connecting...", Array.Empty<object>()) : Lang.Get("Connecting..", Array.Empty<object>()));
				richtext.SetNewText(text, CairoFont.WhiteSmallishText(), null);
			}
			if (this.logincomplete)
			{
				this.ElementComposer.GetButton("login").Enabled = true;
				this.ScreenManager.ClientIsOffline = true;
				if (this.response == EnumAuthServerResponse.Good)
				{
					this.ScreenManager.ClientIsOffline = false;
					this.ScreenManager.DoGameInitStage4();
					this.requireTOTPCode = false;
				}
				else
				{
					if (this.failreason == "requiretotpcode" || this.failreason == "wrongtotpcode" || this.failreason == "ipchanged")
					{
						this.requireTOTPCode = this.failreason != "ipchanged";
						this.compose();
					}
					this.ElementComposer.GetRichtext("status").SetNewText(Lang.Get("game:loginfailure-" + this.failreason, new object[] { this.failreasondata }), CairoFont.WhiteSmallishText(), null);
				}
				this.logincomplete = false;
			}
			this.ScreenManager.GamePlatform.UseMouseCursor((this.FocusedMouseCursor == null) ? "normal" : this.FocusedMouseCursor, false);
		}

		public override void OnScreenLoaded()
		{
		}

		private long lastStatusUpdateMS;

		private bool logincomplete;

		private bool connecting;

		private bool requireTOTPCode;

		private EnumAuthServerResponse response;

		private string failreason;

		private string failreasondata;

		private string prelogintoken;
	}
}
