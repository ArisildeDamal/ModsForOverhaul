using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common
{
	public class VSWebClient : HttpClient
	{
		public void PostAsync(Uri uri, FormUrlEncodedContent postData, VSWebClient.PostCompleteHandler onFinished)
		{
			VSWebClient.<>c__DisplayClass2_0 CS$<>8__locals1 = new VSWebClient.<>c__DisplayClass2_0();
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.uri = uri;
			CS$<>8__locals1.postData = postData;
			CS$<>8__locals1.onFinished = onFinished;
			Task.Run(delegate
			{
				VSWebClient.<>c__DisplayClass2_0.<<PostAsync>b__0>d <<PostAsync>b__0>d;
				<<PostAsync>b__0>d.<>t__builder = AsyncTaskMethodBuilder.Create();
				<<PostAsync>b__0>d.<>4__this = CS$<>8__locals1;
				<<PostAsync>b__0>d.<>1__state = -1;
				<<PostAsync>b__0>d.<>t__builder.Start<VSWebClient.<>c__DisplayClass2_0.<<PostAsync>b__0>d>(ref <<PostAsync>b__0>d);
				return <<PostAsync>b__0>d.<>t__builder.Task;
			});
		}

		public string Post(Uri uri, FormUrlEncodedContent postData)
		{
			string text;
			try
			{
				text = base.PostAsync(uri, postData).Result.Content.ReadAsStringAsync().Result;
			}
			catch (Exception)
			{
				text = string.Empty;
			}
			return text;
		}

		public async Task DownloadAsync(string requestUri, Stream destination, IProgress<Tuple<int, long>> progress = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			VSWebClient.<>c__DisplayClass4_0 CS$<>8__locals1 = new VSWebClient.<>c__DisplayClass4_0();
			CS$<>8__locals1.progress = progress;
			HttpResponseMessage httpResponseMessage = await base.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			Stream download;
			using (HttpResponseMessage response = httpResponseMessage)
			{
				CS$<>8__locals1.contentLength = response.Content.Headers.ContentLength;
				download = await response.Content.ReadAsStreamAsync(cancellationToken);
				object obj = null;
				int num = 0;
				try
				{
					if (CS$<>8__locals1.progress == null || CS$<>8__locals1.contentLength == null)
					{
						await download.CopyToAsync(destination, cancellationToken);
					}
					else
					{
						Progress<int> relativeProgress = new Progress<int>(delegate(int totalBytes)
						{
							CS$<>8__locals1.progress.Report(new Tuple<int, long>(totalBytes, CS$<>8__locals1.contentLength.Value));
						});
						await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
						download.Close();
					}
					num = 1;
				}
				catch (object obj)
				{
				}
				if (download != null)
				{
					await download.DisposeAsync();
				}
				object obj2 = obj;
				if (obj2 != null)
				{
					Exception ex = obj2 as Exception;
					if (ex == null)
					{
						throw obj2;
					}
					ExceptionDispatchInfo.Capture(ex).Throw();
				}
				if (num == 1)
				{
					return;
				}
				obj = null;
			}
			CS$<>8__locals1 = null;
			HttpResponseMessage response = null;
			download = null;
		}

		public static readonly VSWebClient Inst = new VSWebClient
		{
			Timeout = TimeSpan.FromSeconds((double)ClientSettings.WebRequestTimeout)
		};

		public delegate void PostCompleteHandler(CompletedArgs args);
	}
}
