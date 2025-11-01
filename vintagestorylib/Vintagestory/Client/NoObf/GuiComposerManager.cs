using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf
{
	public class GuiComposerManager : IGuiComposerManager
	{
		public void ClearCache()
		{
			foreach (KeyValuePair<string, GuiComposer> val in this.dialogComposers)
			{
				val.Value.Dispose();
			}
			this.dialogComposers.Clear();
		}

		public void ClearCached(string dialogName)
		{
			GuiComposer composer;
			if (this.dialogComposers.TryGetValue(dialogName, out composer))
			{
				composer.Dispose();
				this.dialogComposers.Remove(dialogName);
			}
		}

		public void Dispose(string dialogName)
		{
			GuiComposer composer;
			if (this.dialogComposers.TryGetValue(dialogName, out composer))
			{
				if (composer != null)
				{
					composer.Dispose();
				}
				this.dialogComposers.Remove(dialogName);
			}
		}

		public Dictionary<string, GuiComposer> Composers
		{
			get
			{
				return this.dialogComposers;
			}
		}

		public GuiComposerManager(ICoreClientAPI api)
		{
			this.api = api;
		}

		public GuiComposer Create(string dialogName, ElementBounds bounds)
		{
			GuiComposer composer;
			if (this.dialogComposers.ContainsKey(dialogName))
			{
				composer = this.dialogComposers[dialogName];
				composer.Dispose();
			}
			if (bounds.ParentBounds == null)
			{
				bounds.ParentBounds = new ElementWindowBounds();
			}
			composer = new GuiComposer(this.api, bounds, dialogName);
			composer.composerManager = this;
			this.dialogComposers[dialogName] = composer;
			return composer;
		}

		public void RecomposeAllDialogs()
		{
			Stopwatch watch = Stopwatch.StartNew();
			foreach (GuiComposer composer in this.dialogComposers.Values)
			{
				watch.Restart();
				composer.Composed = false;
				composer.Compose(true);
				ScreenManager.Platform.CheckGlError("recomp - " + composer.DialogName);
				ScreenManager.Platform.Logger.Notification("Recomposed dialog {0} in {1}s", new object[]
				{
					composer.DialogName,
					Math.Round((double)((float)watch.ElapsedMilliseconds / 1000f), 3)
				});
			}
		}

		public void MarkAllDialogsForRecompose()
		{
			foreach (GuiComposer guiComposer in this.dialogComposers.Values)
			{
				guiComposer.recomposeOnRender = true;
			}
		}

		public void UnfocusElements()
		{
			this.UnfocusElementsExcept(null, null);
		}

		public void UnfocusElementsExcept(GuiComposer newFocusedComposer, GuiElement newFocusedElement)
		{
			foreach (GuiComposer composer in this.dialogComposers.Values)
			{
				if (newFocusedComposer != composer)
				{
					composer.UnfocusOwnElementsExcept(newFocusedElement);
				}
			}
		}

		internal Dictionary<string, GuiComposer> dialogComposers = new Dictionary<string, GuiComposer>();

		private ICoreClientAPI api;
	}
}
