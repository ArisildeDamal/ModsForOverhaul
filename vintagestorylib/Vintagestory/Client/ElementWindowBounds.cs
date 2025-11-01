using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client
{
	internal class ElementWindowBounds : ElementBounds
	{
		public override double relX
		{
			get
			{
				return 0.0;
			}
		}

		public override double relY
		{
			get
			{
				return 0.0;
			}
		}

		public override double absX
		{
			get
			{
				return 0.0;
			}
		}

		public override double absY
		{
			get
			{
				return 0.0;
			}
		}

		public override double renderX
		{
			get
			{
				return 0.0;
			}
		}

		public override double renderY
		{
			get
			{
				return 0.0;
			}
		}

		public override double drawX
		{
			get
			{
				return 0.0;
			}
		}

		public override double drawY
		{
			get
			{
				return 0.0;
			}
		}

		public override double OuterWidth
		{
			get
			{
				return (double)this.width;
			}
		}

		public override double OuterHeight
		{
			get
			{
				return (double)this.height;
			}
		}

		public override double InnerWidth
		{
			get
			{
				return (double)this.width;
			}
		}

		public override double InnerHeight
		{
			get
			{
				return (double)this.height;
			}
		}

		public override int OuterWidthInt
		{
			get
			{
				return this.width;
			}
		}

		public override int OuterHeightInt
		{
			get
			{
				return this.height;
			}
		}

		public override bool RequiresRecalculation
		{
			get
			{
				return this.width != ScreenManager.Platform.WindowSize.Width || this.height != ScreenManager.Platform.WindowSize.Height;
			}
		}

		public ElementWindowBounds()
		{
			this.IsWindowBounds = true;
			this.width = ScreenManager.Platform.WindowSize.Width;
			this.height = ScreenManager.Platform.WindowSize.Height;
			this.Initialized = true;
		}

		public override void CalcWorldBounds()
		{
			this.IsWindowBounds = true;
			this.width = ScreenManager.Platform.WindowSize.Width;
			this.height = ScreenManager.Platform.WindowSize.Height;
		}

		private int width;

		private int height;
	}
}
