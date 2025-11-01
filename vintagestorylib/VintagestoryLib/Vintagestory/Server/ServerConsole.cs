using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server
{
	public class ServerConsole
	{
		public ServerConsole(ServerMain server, CancellationToken token)
		{
			this.token = token;
			this.server = server;
			this.inputStream = Console.OpenStandardInput();
			Console.CancelKeyPress += this.Console_CancelKeyPress;
			Task.Run(new Func<Task>(this.readAsync), this.token);
		}

		private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			e.Cancel = true;
			this.server.EnqueueMainThreadTask(delegate
			{
				if (this.server.RunPhase == EnumServerRunPhase.Standby)
				{
					Environment.Exit(0);
					return;
				}
				this.server.Stop("CTRL+c pressed", null, EnumLogType.Notification);
			});
		}

		private async Task readAsync()
		{
			while (!this.token.IsCancellationRequested)
			{
				ServerConsole.<>c__DisplayClass8_0 CS$<>8__locals1 = new ServerConsole.<>c__DisplayClass8_0();
				CS$<>8__locals1.<>4__this = this;
				int num = await this.inputStream.ReadAsync(this._memoryBuffer, this.token);
				this.readCount = num;
				if (this.readCount != 0)
				{
					this.input += Encoding.UTF8.GetString(this._memoryBuffer.Slice(0, this.readCount).Span);
					if (this.input.EndsWithOrdinal(Environment.NewLine))
					{
						CS$<>8__locals1.inputCopy = this.input.Trim();
						this.server.EnqueueMainThreadTask(delegate
						{
							if (CS$<>8__locals1.<>4__this.server.RunPhase == EnumServerRunPhase.Standby)
							{
								if (CS$<>8__locals1.inputCopy == "/stop")
								{
									Environment.Exit(0);
								}
								if (CS$<>8__locals1.inputCopy == "/stats")
								{
									ServerMain.Logger.Notification(CmdStats.genStats(CS$<>8__locals1.<>4__this.server, "\n"));
								}
								return;
							}
							CS$<>8__locals1.<>4__this.server.ReceiveServerConsole(CS$<>8__locals1.inputCopy);
						});
						this.input = string.Empty;
						CS$<>8__locals1 = null;
						continue;
					}
				}
				return;
			}
		}

		public void Dispose()
		{
			Console.CancelKeyPress -= this.Console_CancelKeyPress;
			this.server = null;
		}

		private string input;

		private Memory<byte> _memoryBuffer = new Memory<byte>(new byte[1024]);

		private readonly CancellationToken token;

		private int readCount;

		private readonly Stream inputStream;

		private ServerMain server;
	}
}
