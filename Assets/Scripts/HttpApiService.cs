using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Net.Http.Headers;
//using Microsoft.AspNetCore.SignalR.Client;
using CabTool.Dtos;
using System.Linq;

namespace CabTool
{
	public enum DeleteStatus
	{
		DidNotExist = 0,
		Deleted = 1,
		MarkedDeleted = 2,
		MarkedDeletedWithReferences = 3,
		CouldNotDelete = 4,
		NotSpecified = 5,   // No object/id requested
		AlreadyHandled = 6  // Trying to mark deleted if already marked deleted
	}

	public class Error
	{
		public string Type { get; set; }
		public int HttpStatusCode { get; set; }
		public string Message { get; set; }
		public string InternalDetails { get; set; }
		public DateTimeOffset Timestamp { get; set; }
		public string ReferenceId { get; set; }
	}

	public class HttpApiService
	{
		public HttpApiService(string host = null, string token = null)
		{
			Host = host;
			Token = token;
			CheckErrors = true;
//			Uri uri = new Uri("http://besthttpsignalr.azurewebsites.net/raw-connection/");

		}

/*
		public void AddMonitor(string host)
		{
			host = "http://localhost:5009/";

			MsgSvc = new HubConnectionBuilder()
								.WithUrl(host + "cabhub")
								//                            .WithAutomaticReconnect(new UnlimitedRetryPolicy())
								.Build();
			MsgSvc.On<IEnumerable<Dtos.IoItem>>("UpdateIoItems", OnReceive);
		}

		private void OnReceive(IEnumerable<IoItem> items)
		{
			//			Console.WriteLine("Received event, items: " + (items == null ? "(null)" : items.Count().ToString()));
			Debug.Log("Received event, items: " + (items == null ? "(null)" : items.Count().ToString()));
		}

		private HubConnection MsgSvc;
*/
		public string Token { get; set; }

		public string Host { get; set; }

		public bool CheckErrors { get; set; }

		public async Task<IEnumerable<T>> GetList<T>(string requestName, IDictionary<string, string> paramss = null, string host = null)
		{
			return await Request<IEnumerable<T>>(host ?? Host, HttpMethod.Get, requestName, paramss);
		}

		public async Task<T> Get<T>(string requestName, string id, string host = null)
		{
			return await Request<T>(host ?? Host, HttpMethod.Get, requestName, null, null, id);
		}

		public async Task<string> Put<T>(string requestName, T item, IDictionary<string, string> paramss = null)
		{
			return await Put<T>(Host, requestName, item, paramss);
		}

		public async Task<string> Put<T>(string host, string requestName, T item, IDictionary<string, string> paramss = null)
		{
			var content = JsonContent(item);
			return await Request<string>(host ?? Host, HttpMethod.Put, requestName, paramss, content);
		}

		//public async Task<bool> Patch<T>(string requestName, T item, IDictionary<string, string> paramss = null)
		//{
		//	return await Patch<T>(Host, requestName, item, paramss);
		//}

		//public async Task<bool> Patch<T>(string host, string requestName, T item, IDictionary<string, string> paramss = null)
		//{
		//	var content = JsonContent(item);
		//	var response = await Request<object>(host ?? Host, HttpMethod.Patch, requestName, paramss, content);
		//	return response != null;
		//}

		public async Task<T> Post<T>(string requestName, object data = null, IDictionary<string, string> paramss = null)
		{
			HttpContent content = data != null ? JsonContent(data) : null;
			return await Request<T>(Host, HttpMethod.Post, requestName, paramss, content);
		}

		// Exceptions thrown when there is not an await, crash the emulators (maybe on devices too).
		// So, this function is no longer available; must await the non void call with a try/catch.
		//public async void Post(string requestName, object data=null, IDictionary<string, string> paramss=null)
		//{
		//	HttpContent content = data != null ? await Util.JsonContent(data) : null;
		//	await Request<string>(Host, HttpMethod.Post, requestName, paramss, content);
		//}

		public async Task<T> Upload<T>(string requestName, string fileName, IDictionary<string, string> paramss = null)
		{
			var strm = System.IO.File.OpenRead(fileName);

			byte[] data;
			using (var br = new BinaryReader(strm))
			{
				data = br.ReadBytes((int)strm.Length);
			}
			ByteArrayContent bytes = new ByteArrayContent(data);

			var multiContent = new MultipartFormDataContent();
			multiContent.Add(bytes, "file", fileName);

			return await Request<T>(HttpMethod.Post, requestName, paramss, multiContent);
		}

		public async Task<System.IO.Stream> GetContent(string id, string host = null)
		{
			var paramss = new Dictionary<string, string>() { { "id", id } };
			return await GetContent(paramss, null, host);
		}

		public async Task<System.IO.Stream> GetContent(IDictionary<string, string> parameters, string requestName = null, string host = null)
		{
			if (string.IsNullOrEmpty(requestName))
				requestName = "content";
			var requestUri = BuildUri(requestName, parameters);

			var response = await Execute(
								GetClient(host)
								, new HttpRequestMessage(HttpMethod.Get, requestUri)
								, requestName);
			return await response.Content.ReadAsStreamAsync();
		}

		public async Task<T> Request<T>(
				HttpMethod method,
				string requestName,
				IDictionary<string, string> parameters = null,
				HttpContent body = null)
		{
			return await Request<T>(Host, method, requestName, parameters, body);
		}

		public async Task<T> Request<T>(
				HttpMethod method,
				string requestName,
				string id,
				IDictionary<string, string> parameters = null,
				HttpContent body = null)
		{
			return await Request<T>(Host, method, requestName, parameters, body, id);
		}

		public async Task<T> Request<T>(
				string host,
				HttpMethod method,
				string requestName,
				IDictionary<string, string> parameters = null,
				HttpContent body = null,
				string id = null)
		{
			var client = GetClient(host);

			var requestUri = BuildUri(requestName, parameters, host, id);
			var request = new HttpRequestMessage(method, requestUri);
			request.Content = body;

			var response = await Execute(client, request, requestName);

			var text = await response.Content.ReadAsStringAsync();
			return await Task.Run(() => JsonConvert.DeserializeObject<T>(text));
		}

		public static async Task<string> ToJson(object obj)
		{
			return await Task.Run(() => JsonConvert.SerializeObject(obj));
		}

		public static HttpContent JsonContent(object obj)
		{
			//			var json = await Task.Run(() => JsonConvert.SerializeObject(obj));
			var json = JsonConvert.SerializeObject(obj);
			return new StringContent(json, Encoding.UTF8, "application/json");
		}

		protected async Task<HttpResponseMessage> Execute(HttpClient client, HttpRequestMessage request, string requestName)
		{
			HttpResponseMessage response;
			try
			{
//				client.DefaultRequestHeaders.Accept.Clear();
//				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//				client.DefaultRequestHeaders.Add("Content-Type", "multipart/form-data");
/*
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				request.Headers.Add("Connection", "Keep-alive");
//				request.Headers.Add("Content-Type", "multipart/form-data");
*/
//				request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
//				request.Headers.Add("Connection", "keep-alive");
//				request.Headers.Add("Cache-Control", "no-cache");
//				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//				request.Content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
//				request.Content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data");
				response = await client.SendAsync(request);

			}
			catch (Exception ex)
			{
				// Some exceptions cause the app to crash; ??? might need to retry instead of returning
				throw new SystemException("Cannot contact server, " + requestName + ", host: " + client.BaseAddress + ". " + ex.Message);
				//App.Error(new RequestException("Cannot contact server, " + requestName + ", host: " + host + ". " + ex.Message));
				//return await Task.FromResult<T>(default(T));
			}

			if (response == null)
			{
				throw new SystemException("No response was received for request, " + requestName + ", host: " + client.BaseAddress + ".");
				//App.Error(new RequestException("No response was received for request, " + requestName + ", host: " + host + "."));
				//return await Task.FromResult<T>(default(T));
			}

			// CabSvr returns NotModified in some cases and is not an error

			if (!response.IsSuccessStatusCode
				 && response.StatusCode != System.Net.HttpStatusCode.NotModified
				 && CheckErrors)
				throw await GetException(response);

			return response;
		}

		public async Task<DeleteStatus> Delete(string requestName, string id, string host = null)
		{
			return await Request<DeleteStatus>(host ?? Host, HttpMethod.Delete, requestName,
													new Dictionary<string, string>() { { "id", id } });
		}

		private string GetUrl(string host, string requestName = null)
		{
			return host + (!string.IsNullOrEmpty(requestName) ? "/" + requestName : "");
		}

		private async Task<SystemException> GetException(HttpResponseMessage response)
		{
			var s = await response.Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(s))
			{
				return new SystemException(response == null ? ""
									: (string.IsNullOrEmpty(response.ReasonPhrase) ? response.StatusCode.ToString()
										: response.ReasonPhrase + " - " + (response.RequestMessage.RequestUri)));
			}
			else if (!string.IsNullOrEmpty(s) && s[0] == '{')
			{
				if (s.Contains("ReferenceId"))
				{
					var err = await Task.Run(() => JsonConvert.DeserializeObject<Error>(s));
					return new SystemException(err.Message);
				}
				//				else if ( s.Contains("exceptionMessage") )
				else
				{
					var err = await Task.Run(() => JsonConvert.DeserializeObject(s));
					return new SystemException(err.ToString());
				}
			}
			else if (s.Contains("[Fiddler]"))
				return new SystemException(s);
			else if (!string.IsNullOrEmpty(s))
				return new SystemException(s);
			return new SystemException("Error occured on server but the expected Error model was not returned. See output for content.");
		}

		public HttpClient GetClient(string host = null)
		{
			var client = new System.Net.Http.HttpClient();
			client.BaseAddress = new Uri(GetUrl(host ?? Host));
			client.DefaultRequestHeaders.Accept.Clear();
			var t = Token;
			if (!string.IsNullOrEmpty(t))
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
			return client;
		}

		public string BuildUri(string request, IDictionary<string, string> parameters = null, string host = null)
		{
			return BuildUri(request, parameters, host, null);
		}

		public string BuildUri(string request, IDictionary<string, string> parameters, string host, string id = null)
		{
			var baseAddress = new Uri(GetUrl(host ?? Host));
			var uri = BuildUri2(request, parameters, id);
			return baseAddress + uri;
		}

		protected string BuildUri2(string request, IDictionary<string, string> parameters, string id)
		{
			var s = new StringBuilder();
			if (parameters != null)
			{
				foreach (var p in parameters)
				{
					if (!string.IsNullOrEmpty(p.Key))
					{
						if (s.Length != 0)
							s.Append("&");
						s.Append(System.Net.WebUtility.UrlEncode(p.Key));
						if (!string.IsNullOrEmpty(p.Value))
						{
							s.Append("=");
							s.Append(System.Net.WebUtility.UrlEncode(p.Value));
						}
					}
				}
			}

			var idPart = string.IsNullOrEmpty(id) ? "" : "/" + id;

			if (s.Length == 0)
				return request + idPart;

			int pos;
			if ((pos = request.IndexOf("?")) >= 0)
			{
				if (idPart != "")
					request = request.Insert(pos, idPart);
				return request + "&" + s.ToString();
			}
			else
				return request + id + "?" + s.ToString();
		}

	} // HttpApiService
}
