using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server
{
	public class AuthServerComm
	{
		public static void ValidatePlayerWithServer(string mptokenv2, string playerName, string playerUID, string serverLoginToken, ValidationCompleteDelegate OnValidationComplete)
		{
			FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
			{
				new KeyValuePair<string, string>("mptokenv2", mptokenv2),
				new KeyValuePair<string, string>("uid", playerUID),
				new KeyValuePair<string, string>("serverlogintoken", serverLoginToken),
				new KeyValuePair<string, string>("serverversion", "1.21.5")
			});
			Uri uri = new Uri("https://auth3.vintagestory.at/v2/servervalidate");
			VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
			{
				ServerMain.Logger.Debug("Response from auth server: {0}", new object[] { args.Response });
				if (args.State != CompletionState.Good)
				{
					ServerMain.Logger.Warning("Unable to connect to auth server: State {0}, Error msg '{1}'", new object[] { args.State, args.ErrorMessage });
					OnValidationComplete(EnumServerResponse.Offline, null, null);
					return;
				}
				ValidateResponse response = JsonConvert.DeserializeObject<ValidateResponse>(args.Response);
				if (response.valid == 1 && response.playername == playerName)
				{
					OnValidationComplete(EnumServerResponse.Good, response.entitlements, null);
					return;
				}
				OnValidationComplete(EnumServerResponse.Bad, response.entitlements, response.reason);
			});
		}

		public static void ResolvePlayerName(string playername, Action<EnumServerResponse, string> OnResolveComplete)
		{
			FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
			{
				new KeyValuePair<string, string>("playername", playername)
			});
			Uri uri = new Uri("https://auth3.vintagestory.at/resolveplayername");
			VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
			{
				if (args.State != CompletionState.Good)
				{
					OnResolveComplete(EnumServerResponse.Offline, null);
					return;
				}
				ServerMain.Logger.Debug("Response from auth server: {0}", new object[] { args.Response });
				ResolveResponse response = JsonConvert.DeserializeObject<ResolveResponse>(args.Response);
				if (response.playeruid == null)
				{
					OnResolveComplete(EnumServerResponse.Bad, null);
					return;
				}
				OnResolveComplete(EnumServerResponse.Good, response.playeruid);
			});
		}

		public static void ResolvePlayerUid(string playeruid, Action<EnumServerResponse, string> OnResolveComplete)
		{
			FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
			{
				new KeyValuePair<string, string>("uid", playeruid)
			});
			Uri uri = new Uri("https://auth3.vintagestory.at/resolveplayeruid");
			VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
			{
				if (args.State != CompletionState.Good)
				{
					OnResolveComplete(EnumServerResponse.Offline, null);
					return;
				}
				ServerMain.Logger.Debug("Response from auth server: {0}", new object[] { args.Response });
				ResolveResponseUid response = JsonConvert.DeserializeObject<ResolveResponseUid>(args.Response);
				if (response.playername == null)
				{
					OnResolveComplete(EnumServerResponse.Bad, null);
					return;
				}
				OnResolveComplete(EnumServerResponse.Good, response.playername);
			});
		}
	}
}
