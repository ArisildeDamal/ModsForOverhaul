using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client.Util
{
	public class ServerCtrlBackendInterface
	{
		public void Start(OnSrvActionComplete<ServerCtrlResponse> onComplete)
		{
			this.runAction<ServerCtrlResponse>(onComplete, "start", null);
		}

		public void Stop(OnSrvActionComplete<ServerCtrlResponse> onComplete)
		{
			this.runAction<ServerCtrlResponse>(onComplete, "stop", null);
		}

		public void ForceStop(OnSrvActionComplete<ServerCtrlResponse> onComplete)
		{
			this.runAction<ServerCtrlResponse>(onComplete, "forcestop", null);
		}

		public void DeleteSaves(OnSrvActionComplete<GameServerStatus> onComplete)
		{
			this.runAction<GameServerStatus>(onComplete, "clearsaves", null);
		}

		public void DeleteAll(OnSrvActionComplete<GameServerStatus> onComplete)
		{
			this.runAction<GameServerStatus>(onComplete, "deleteall", null);
		}

		public void GetLog(OnSrvActionComplete<GameServerLogResponse> onComplete)
		{
			this.runAction<GameServerLogResponse>(onComplete, "getlog", null);
		}

		public void GetGameVersions(OnActionComplete<string[]> onComplete)
		{
			try
			{
				JObject jobject = JObject.Parse(this.webClient.GetStringAsync("http://api.vintagestory.at/stable-unstable.json").Result);
				List<string> versions = new List<string>();
				foreach (KeyValuePair<string, JToken> val in jobject)
				{
					if (GameVersion.IsAtLeastVersion(val.Key, "1.14.9"))
					{
						versions.Add(val.Key);
					}
				}
				Action <>9__1;
				this.GetVSHostingUnsupportedGameVersions(delegate(EnumAuthServerResponse status, string[] unsupversions)
				{
					if (status != EnumAuthServerResponse.Good)
					{
						throw new Exception("Unable to load unsupported versions");
					}
					foreach (string ver in unsupversions)
					{
						versions.Remove(ver);
					}
					Action action;
					if ((action = <>9__1) == null)
					{
						action = (<>9__1 = delegate
						{
							onComplete(EnumAuthServerResponse.Good, versions.ToArray());
						});
					}
					ScreenManager.EnqueueMainThreadTask(action);
				});
			}
			catch (Exception e)
			{
				ScreenManager.Platform.Logger.Error(e);
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					onComplete(EnumAuthServerResponse.Bad, null);
				});
			}
		}

		public void GetVSHostingUnsupportedGameVersions(OnActionComplete<string[]> onComplete)
		{
			try
			{
				JObject jobject = JObject.Parse(this.webClient.GetStringAsync("http://api.vintagestory.at/vshostingunsupported.json").Result);
				List<string> versions = new List<string>();
				foreach (JToken val in ((IEnumerable<JToken>)jobject["versions"]))
				{
					versions.Add(val.ToString());
				}
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					onComplete(EnumAuthServerResponse.Good, versions.ToArray());
				});
			}
			catch (Exception)
			{
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					onComplete(EnumAuthServerResponse.Bad, null);
				});
			}
		}

		public void RequestDownload(OnSrvActionComplete<GameServerStatus> onComplete)
		{
			this.runAction<GameServerStatus>(delegate(EnumAuthServerResponse status, GameServerStatus response)
			{
				onComplete(status, response);
			}, "downloadsaves", null);
		}

		public void GetStatus(OnSrvActionComplete<GameServerStatus> onComplete)
		{
			this.runAction<GameServerStatus>(delegate(EnumAuthServerResponse status, GameServerStatus response)
			{
				onComplete(status, response);
			}, "status", null);
		}

		public void GetConfig(OnSrvActionComplete<GameServerConfigResponse> onComplete)
		{
			this.runAction<GameServerConfigResponse>(onComplete, "getconfig", null);
		}

		public void SetConfig(OnSrvActionComplete<GameServerStatus> onComplete, string serverconfig, string worldconfig)
		{
			this.runAction<GameServerStatus>(onComplete, "setconfig", new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("serverconfig", serverconfig),
				new KeyValuePair<string, string>("worldconfig", worldconfig)
			});
		}

		public void SelectRegion(string region, OnSrvActionComplete<ServerCtrlResponse> onComplete)
		{
			this.runAction<ServerCtrlResponse>(onComplete, "selectregion", new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("region", region)
			});
		}

		public void SelectVersion(string version, OnSrvActionComplete<ServerCtrlResponse> onComplete)
		{
			this.runAction<ServerCtrlResponse>(onComplete, "install", new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("version", version)
			});
		}

		public void DeleteMod(string mod, OnSrvActionComplete<ServerCtrlResponse> onComplete)
		{
			this.runAction<ServerCtrlResponse>(onComplete, "deletemod", new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("mod", mod)
			});
		}

		public void DeleteAllMods(OnSrvActionComplete<ServerCtrlResponse> onComplete)
		{
			this.runAction<ServerCtrlResponse>(onComplete, "deleteallmods", null);
		}

		private void runAction<T>(OnSrvActionComplete<T> onComplete, string action, List<KeyValuePair<string, string>> postData = null) where T : ServerCtrlResponse
		{
			this.IsLoading = true;
			if (postData == null)
			{
				postData = new List<KeyValuePair<string, string>>();
			}
			postData.Add(new KeyValuePair<string, string>("action", action));
			postData.Add(new KeyValuePair<string, string>("uid", ClientSettings.PlayerUID));
			postData.Add(new KeyValuePair<string, string>("sessionkey", ClientSettings.Sessionkey));
			FormUrlEncodedContent formContent = new FormUrlEncodedContent(postData);
			Uri uri = new Uri("https://auth3.vintagestory.at/v2/gameserverctrl");
			ScreenManager.Platform.Logger.Notification("Send request: {0}", new object[] { action });
			this.webClient.PostAsync(uri, formContent, delegate(CompletedArgs args)
			{
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					if (args.State != CompletionState.Good)
					{
						onComplete(EnumAuthServerResponse.Offline, default(T));
						return;
					}
					ScreenManager.Platform.Logger.Notification("Response {0}: {1}", new object[] { args.StatusCode, args.Response });
					if (args.Response == null)
					{
						onComplete(EnumAuthServerResponse.Bad, default(T));
						return;
					}
					this.IsLoading = false;
					T response;
					try
					{
						response = JsonConvert.DeserializeObject<T>(args.Response);
					}
					catch (Exception e)
					{
						ScreenManager.Platform.Logger.Notification(LoggerBase.CleanStackTrace(e.ToString()));
						onComplete(EnumAuthServerResponse.Bad, default(T));
						return;
					}
					if (response != null && response.Valid == 1)
					{
						onComplete(EnumAuthServerResponse.Good, response);
						return;
					}
					ScreenManager.Platform.Logger.Notification("Response is bad. Valid flag not set.");
					onComplete(EnumAuthServerResponse.Bad, response);
				});
			});
		}

		public bool IsLoading;

		public VSWebClient webClient = new VSWebClient
		{
			Timeout = TimeSpan.FromSeconds(60.0)
		};
	}
}
