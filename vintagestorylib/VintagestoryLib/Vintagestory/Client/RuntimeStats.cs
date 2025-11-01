using System;

namespace Vintagestory.Client
{
	public class RuntimeStats
	{
		public static void Reset()
		{
			RuntimeStats.chunksTesselatedTotal = 0;
			RuntimeStats.chunksReceived = 0;
			RuntimeStats.chunksTesselatedPerSecond = 0;
			RuntimeStats.chunksTesselatedEdgeOnly = 0;
			RuntimeStats.chunksUnloaded = 0;
			RuntimeStats.renderedTriangles = 0;
			RuntimeStats.availableTriangles = 0;
			RuntimeStats.drawCallsCount = 0;
			RuntimeStats.renderedEntities = 0;
			RuntimeStats.TCTpacked = 0;
			RuntimeStats.TCTunpacked = 1;
			RuntimeStats.OCpacked = 0;
			RuntimeStats.OCunpacked = 1;
		}

		public static int chunksReceived = 0;

		public static int chunksTesselatedPerSecond = 0;

		public static int chunksTesselatedEdgeOnly = 0;

		public static int chunksAwaitingTesselation = 0;

		public static int chunksAwaitingPooling = 0;

		public static int chunksTesselatedTotal = 0;

		public static int chunksUnloaded = 0;

		public static int renderedTriangles = 0;

		public static int availableTriangles = 0;

		public static int renderedEntities = 0;

		public static int drawCallsCount = 0;

		internal static long tesselationStart;

		internal static int TCTpacked;

		internal static int TCTunpacked = 1;

		internal static int OCpacked;

		internal static int OCunpacked;
	}
}
