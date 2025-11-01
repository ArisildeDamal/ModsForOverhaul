using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace Vintagestory.Client
{
	public class UriHandler
	{
		public static UriHandler Instance
		{
			get
			{
				UriHandler uriHandler;
				if ((uriHandler = UriHandler._instance) == null)
				{
					uriHandler = (UriHandler._instance = new UriHandler());
				}
				return uriHandler;
			}
		}

		private UriHandler()
		{
		}

		public void StartPipeServer()
		{
			try
			{
				if (this._namedPipeServerStream == null)
				{
					this._namedPipeServerStream = new NamedPipeServerStream("SingleInstanceVintageStoryWithUriScheme", PipeDirection.In);
					this._pipeServerCts = new CancellationTokenSource();
					this._serverStringReader = new StreamReader(this._namedPipeServerStream);
				}
				Task.Run(async delegate
				{
					while (!this._pipeServerCts.IsCancellationRequested)
					{
						try
						{
							await this._namedPipeServerStream.WaitForConnectionAsync(this._pipeServerCts.Token);
						}
						catch (OperationCanceledException)
						{
							break;
						}
						string readLineAsync = await this._serverStringReader.ReadLineAsync();
						if (readLineAsync != null)
						{
							ClientProgramArgs clientProgramArgs = Parser.Default.ParseArguments<ClientProgramArgs>(readLineAsync.Split(" ", StringSplitOptions.None)).Value;
							if (((clientProgramArgs != null) ? clientProgramArgs.InstallModId : null) != null)
							{
								this.HandleModInstall(clientProgramArgs.InstallModId);
							}
							else if (((clientProgramArgs != null) ? clientProgramArgs.ConnectServerAddress : null) != null)
							{
								this.HandleConnect(clientProgramArgs.ConnectServerAddress);
							}
						}
						this._namedPipeServerStream.Disconnect();
					}
				}, this._pipeServerCts.Token);
			}
			catch
			{
				Console.WriteLine("Couldn't start NamedPipeServer.");
			}
		}

		public bool TryConnectClientPipe()
		{
			NamedPipeClientStream namedPipeClientStream = this._namedPipeClientStream;
			if (namedPipeClientStream != null && namedPipeClientStream.IsConnected)
			{
				Console.WriteLine("Client pipe already connected");
				return true;
			}
			bool flag;
			try
			{
				this._namedPipeClientStream = new NamedPipeClientStream(".", "SingleInstanceVintageStoryWithUriScheme", PipeDirection.Out);
				this._namedPipeClientStream.Connect(1000);
				this._clientStreamWriter = new StreamWriter(this._namedPipeClientStream);
				flag = true;
			}
			catch (Exception)
			{
				flag = false;
			}
			return flag;
		}

		public void Dispose()
		{
			CancellationTokenSource pipeServerCts = this._pipeServerCts;
			if (pipeServerCts != null)
			{
				pipeServerCts.Cancel();
			}
			StreamReader serverStringReader = this._serverStringReader;
			if (serverStringReader != null)
			{
				serverStringReader.Dispose();
			}
			NamedPipeServerStream namedPipeServerStream = this._namedPipeServerStream;
			if (namedPipeServerStream != null)
			{
				namedPipeServerStream.Close();
			}
			NamedPipeServerStream namedPipeServerStream2 = this._namedPipeServerStream;
			if (namedPipeServerStream2 != null)
			{
				namedPipeServerStream2.Dispose();
			}
			StreamWriter clientStreamWriter = this._clientStreamWriter;
			if (clientStreamWriter != null)
			{
				clientStreamWriter.Dispose();
			}
			NamedPipeClientStream namedPipeClientStream = this._namedPipeClientStream;
			if (namedPipeClientStream != null)
			{
				namedPipeClientStream.Close();
			}
			NamedPipeClientStream namedPipeClientStream2 = this._namedPipeClientStream;
			if (namedPipeClientStream2 == null)
			{
				return;
			}
			namedPipeClientStream2.Dispose();
		}

		private void HandleConnect(string uri)
		{
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				ClientProgram.screenManager.GamePlatform.XPlatInterface.FocusWindow();
				ClientProgram.screenManager.ConnectToMultiplayer(uri, null);
			});
		}

		private void HandleModInstall(string modId)
		{
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				ClientProgram.screenManager.GamePlatform.XPlatInterface.FocusWindow();
				ClientProgram.screenManager.InstallMod(modId);
			});
		}

		public void SendModInstall(string argsInstallModId)
		{
			if (this._clientStreamWriter == null || this._namedPipeClientStream == null)
			{
				Console.WriteLine("ClientPipeStream seems not initialized did you forget to call ConnectClientPipe first?");
				return;
			}
			this._clientStreamWriter.WriteLine("-i " + argsInstallModId);
			this._clientStreamWriter.Flush();
		}

		public void SendConnect(string argsConnectServerAddress)
		{
			if (this._clientStreamWriter == null || this._namedPipeClientStream == null)
			{
				Console.WriteLine("ClientPipeStream seems not initialized did you forget to call ConnectClientPipe first?");
				return;
			}
			this._clientStreamWriter.WriteLine("-c " + argsConnectServerAddress);
			this._clientStreamWriter.Flush();
		}

		private const string IPC_CHANNEL_NAME = "SingleInstanceVintageStoryWithUriScheme";

		private NamedPipeServerStream _namedPipeServerStream;

		private StreamReader _serverStringReader;

		private CancellationTokenSource _pipeServerCts;

		private NamedPipeClientStream _namedPipeClientStream;

		private StreamWriter _clientStreamWriter;

		private static UriHandler _instance;
	}
}
