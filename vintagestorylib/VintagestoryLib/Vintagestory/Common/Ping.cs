using System;

namespace Vintagestory.Common
{
	public class Ping
	{
		public Ping()
		{
			this.RoundtripTimeMilliseconds = 0;
			this.didReplyOnLast = true;
			this.timeSendMilliseconds = 0L;
			this.TimeReceivedUdp = 0L;
			this.timeoutThreshold = 15;
		}

		public bool DidReplyOnLastPing
		{
			get
			{
				return this.didReplyOnLast;
			}
		}

		public long TimeSendMilliSeconds
		{
			get
			{
				return this.timeSendMilliseconds;
			}
		}

		public int GetTimeoutThreshold()
		{
			return this.timeoutThreshold;
		}

		public void SetTimeoutThreshold(int value)
		{
			this.timeoutThreshold = value;
		}

		public void OnSend(long ElapsedMilliseconds)
		{
			this.timeSendMilliseconds = ElapsedMilliseconds;
			this.didReplyOnLast = false;
		}

		public void OnReceive(long ElapsedMilliseconds)
		{
			this.RoundtripTimeMilliseconds = (int)(ElapsedMilliseconds - this.timeSendMilliseconds);
			this.didReplyOnLast = true;
		}

		public void OnReceiveUdp(long elapsedMilliseconds)
		{
			this.TimeReceivedUdp = elapsedMilliseconds;
		}

		public bool DidUdpTimeout(long elapsedMilliseconds)
		{
			return elapsedMilliseconds - this.TimeReceivedUdp > 30000L;
		}

		public bool DidTimeout(long ElapsedMilliseconds)
		{
			if ((ElapsedMilliseconds - this.timeSendMilliseconds) / 1000L > (long)this.timeoutThreshold)
			{
				this.didReplyOnLast = true;
				return true;
			}
			return false;
		}

		internal int RoundtripTimeTotalMilliseconds()
		{
			return this.RoundtripTimeMilliseconds;
		}

		private int RoundtripTimeMilliseconds;

		private bool didReplyOnLast;

		private long timeSendMilliseconds;

		private int timeoutThreshold;

		public long TimeReceivedUdp;
	}
}
