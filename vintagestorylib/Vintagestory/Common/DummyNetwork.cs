using System;
using System.Collections.Generic;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common
{
	public class DummyNetwork
	{
		public DummyNetwork()
		{
			this.Clear();
		}

		public void Start()
		{
			this.ServerReceiveBufferLock = new MonitorObject();
			this.ClientReceiveBufferLock = new MonitorObject();
		}

		public void Clear()
		{
			this.ServerReceiveBuffer = new Queue<object>();
			this.ClientReceiveBuffer = new Queue<object>();
		}

		internal Queue<object> ServerReceiveBuffer;

		internal Queue<object> ClientReceiveBuffer;

		internal MonitorObject ServerReceiveBufferLock;

		internal MonitorObject ClientReceiveBufferLock;
	}
}
