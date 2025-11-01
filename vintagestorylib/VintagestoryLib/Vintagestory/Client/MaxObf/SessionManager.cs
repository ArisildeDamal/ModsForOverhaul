using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client.MaxObf
{
	public class SessionManager
	{
		public bool IsCachedSessionKeyValid()
		{
			bool valid = false;
			try
			{
				RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
				rsa.FromXmlString("<RSAKeyValue><Modulus>mRaP5hO0mWf6gIdPMFD0sg4KLhwsA08Tk2246fdwNk6G7cRk+BJYtTOwKO+plurICQMKF2ktDJWOkjz+Hq2BCjBDB/al7XNdnoOJ1w0BsgInEPOGz9nn8OM4GjQyNcuv0iY0XqwElgy5xCNjBRKJJuqQje/E5SIiHs2O78nJUsZWCv6xjaH+4N/3Kno+sQoBFpNqKmXsq1+2KGMu8t4x58LrojbXzxJUm3O3agK8MvDg/xTAmumd2PTjVJBnrlSBIPdsaQwzX1G9s29B7CzQC6T7TzQehA8hPmUSQLEnwBV6EaUXbcjOBh01i5k5MP6i22wrDCfQMnnkch+i+UsgyQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
				if (ClientSettings.Sessionkey == null)
				{
					return false;
				}
				byte[] computedHash = SHA256.HashData(Encoding.UTF8.GetBytes(ClientSettings.Sessionkey));
				byte[] signature = Convert.FromBase64String(ClientSettings.SessionSignature);
				valid = rsa.VerifyHash(computedHash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
				valid &= !string.IsNullOrEmpty(ClientSettings.PlayerUID);
				rsa.Dispose();
			}
			catch (Exception)
			{
			}
			return valid;
		}

		public void ValidateSessionKeyWithServer(Action<EnumAuthServerResponse> OnValidationComplete)
		{
			FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
			{
				new KeyValuePair<string, string>("uid", ClientSettings.PlayerUID),
				new KeyValuePair<string, string>("sessionkey", ClientSettings.Sessionkey)
			});
			Uri uri = new Uri("https://auth3.vintagestory.at/clientvalidate");
			VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
			{
				if (args.State != CompletionState.Good)
				{
					OnValidationComplete(EnumAuthServerResponse.Offline);
					return;
				}
				ValidateResponse response = JsonConvert.DeserializeObject<ValidateResponse>(args.Response);
				if (response.valid == 1)
				{
					ClientSettings.MpToken = null;
					ClientSettings.Entitlements = response.entitlements;
					ClientSettings.HasGameServer = response.hasgameserver;
					GlobalConstants.SinglePlayerEntitlements = response.entitlements;
					OnValidationComplete(EnumAuthServerResponse.Good);
					return;
				}
				ClientSettings.Sessionkey = null;
				ClientSettings.SessionSignature = null;
				ScreenManager.Platform.Logger.Debug("Unable to validate session. Server says: {0}", new object[] { response.reason });
				OnValidationComplete(EnumAuthServerResponse.Bad);
			});
		}

		public void RequestMpToken(Action<EnumAuthServerResponse, string> OnValidationComplete, string serverlogintoken)
		{
			VSWebClient.PostCompleteHandler <>9__1;
			TyronThreadPool.QueueTask(delegate
			{
				FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
				{
					new KeyValuePair<string, string>("uid", ClientSettings.PlayerUID),
					new KeyValuePair<string, string>("serverlogintoken", serverlogintoken),
					new KeyValuePair<string, string>("sessionkey", ClientSettings.Sessionkey)
				});
				Uri uri = new Uri("https://auth3.vintagestory.at/v2.1/clientrequestmptoken");
				VSWebClient inst = VSWebClient.Inst;
				Uri uri2 = uri;
				FormUrlEncodedContent formUrlEncodedContent = postData;
				VSWebClient.PostCompleteHandler postCompleteHandler;
				if ((postCompleteHandler = <>9__1) == null)
				{
					postCompleteHandler = (<>9__1 = delegate(CompletedArgs args)
					{
						if (args.State != CompletionState.Good)
						{
							OnValidationComplete(EnumAuthServerResponse.Offline, "offline");
							return;
						}
						MpTokenResponse response = JsonConvert.DeserializeObject<MpTokenResponse>(args.Response);
						if (response.valid == 1)
						{
							ClientSettings.MpToken = response.mptokenv2;
							OnValidationComplete(EnumAuthServerResponse.Good, null);
							return;
						}
						ScreenManager.Platform.Logger.Debug("Unable to request mp token. Server says: {0}", new object[] { response.reason });
						OnValidationComplete(EnumAuthServerResponse.Bad, response.reason);
					});
				}
				inst.PostAsync(uri2, formUrlEncodedContent, postCompleteHandler);
			}, "requestmptoken");
		}

		public void GetNewestVersion(Action<string> OnGetComplete)
		{
			SessionManager.<>c__DisplayClass5_0 CS$<>8__locals1 = new SessionManager.<>c__DisplayClass5_0();
			CS$<>8__locals1.OnGetComplete = OnGetComplete;
			Task.Run(delegate
			{
				SessionManager.<>c__DisplayClass5_0.<<GetNewestVersion>b__0>d <<GetNewestVersion>b__0>d;
				<<GetNewestVersion>b__0>d.<>t__builder = AsyncTaskMethodBuilder.Create();
				<<GetNewestVersion>b__0>d.<>4__this = CS$<>8__locals1;
				<<GetNewestVersion>b__0>d.<>1__state = -1;
				<<GetNewestVersion>b__0>d.<>t__builder.Start<SessionManager.<>c__DisplayClass5_0.<<GetNewestVersion>b__0>d>(ref <<GetNewestVersion>b__0>d);
				return <<GetNewestVersion>b__0>d.<>t__builder.Task;
			});
		}

		public void GetPlayerSkin(string playerUid, Action<byte[]> OnGetComplete)
		{
			TyronThreadPool.QueueTask(delegate
			{
				try
				{
					byte[] response = VSWebClient.Inst.GetByteArrayAsync("https://skins.vintagestory.at/" + playerUid).Result;
					OnGetComplete(response);
				}
				catch (Exception)
				{
					OnGetComplete(null);
				}
			}, "getplayerskin");
		}

		public void DoLogin(string email, string password, string totpCode, string prelogintoken, Action<EnumAuthServerResponse, string, string, string> OnLoginComplete)
		{
			FormUrlEncodedContent postData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("email", email),
				new KeyValuePair<string, string>("password", password),
				new KeyValuePair<string, string>("totpcode", totpCode),
				new KeyValuePair<string, string>("prelogintoken", prelogintoken),
				new KeyValuePair<string, string>("gameloginversion", "1.21.5")
			});
			Uri uri = new Uri("https://auth3.vintagestory.at/v2/gamelogin");
			VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
			{
				if (args.State != CompletionState.Good)
				{
					OnLoginComplete(EnumAuthServerResponse.Offline, "cantconnect", string.Empty, string.Empty);
					ScreenManager.Platform.Logger.Debug("Login attempt failed: {0}", new object[] { args.ErrorMessage });
					return;
				}
				LoginResponse response = JsonConvert.DeserializeObject<LoginResponse>(args.Response);
				ScreenManager.Platform.Logger.Debug("Server login response: {0}, reason: {1}", new object[]
				{
					(response.valid == 1) ? "valid" : "invalid",
					response.reason
				});
				ClientSettings.MpToken = null;
				if (response.valid != 1)
				{
					OnLoginComplete(EnumAuthServerResponse.Bad, response.reason, response.reasondata, response.prelogintoken);
					return;
				}
				ClientSettings.UserEmail = email;
				ClientSettings.Sessionkey = response.sessionkey;
				ClientSettings.SessionSignature = response.sessionsignature;
				ClientSettings.HasGameServer = response.hasgameserver;
				ClientSettings.PlayerUID = response.uid;
				ClientSettings.PlayerName = response.playername;
				ClientSettings.Entitlements = response.entitlements;
				if (this.IsCachedSessionKeyValid())
				{
					OnLoginComplete(EnumAuthServerResponse.Good, response.reason, string.Empty, string.Empty);
					return;
				}
				OnLoginComplete(EnumAuthServerResponse.Bad, "invalidcachedsessionkey", string.Empty, string.Empty);
			});
		}

		public void DoLogout()
		{
			FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
			{
				new KeyValuePair<string, string>("email", ClientSettings.UserEmail),
				new KeyValuePair<string, string>("sessionkey", ClientSettings.Sessionkey)
			});
			Uri uri = new Uri("https://auth3.vintagestory.at/gamelogout");
			VSWebClient.Inst.PostAsync(uri, postData, delegate
			{
			});
			ClientSettings.UserEmail = string.Empty;
			ClientSettings.MpToken = string.Empty;
			ClientSettings.Sessionkey = string.Empty;
			ClientSettings.SessionSignature = string.Empty;
			ClientSettings.PlayerUID = string.Empty;
			ClientSettings.PlayerName = string.Empty;
		}

		private const string PubKey = "<RSAKeyValue><Modulus>mRaP5hO0mWf6gIdPMFD0sg4KLhwsA08Tk2246fdwNk6G7cRk+BJYtTOwKO+plurICQMKF2ktDJWOkjz+Hq2BCjBDB/al7XNdnoOJ1w0BsgInEPOGz9nn8OM4GjQyNcuv0iY0XqwElgy5xCNjBRKJJuqQje/E5SIiHs2O78nJUsZWCv6xjaH+4N/3Kno+sQoBFpNqKmXsq1+2KGMu8t4x58LrojbXzxJUm3O3agK8MvDg/xTAmumd2PTjVJBnrlSBIPdsaQwzX1G9s29B7CzQC6T7TzQehA8hPmUSQLEnwBV6EaUXbcjOBh01i5k5MP6i22wrDCfQMnnkch+i+UsgyQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
	}
}
