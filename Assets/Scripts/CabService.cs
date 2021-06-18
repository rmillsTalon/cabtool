using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Cab.Communications;
using dto = CabTool.Dtos;

namespace CabTool
{
	public class CabService :  MonoBehaviour //CabServiceClient : CabService
	{
		private CabinetServiceGrpc GrpcSvc;

		public CabService()
		{
			Svc = new HttpApiService();
			EventClient = new JsonProtocol<dto.EventArgs>(new WebSocket());
			EventClient.Events += OnReceive;
			SetHost("http://localhost:5009/", "http://localhost:5011/");
			Counter = ++PrevCounter;
		}
		public int Counter;
		private static int PrevCounter = 0;

		public event EventHandler<dto.EventArgs> Events;

		void Start()
		{
//			var go = GameObject.Find("IoItemsControl");
//			GrpcSvc = go.GetComponent<CabinetService>();
//			GrpcSvc.OnChangeValue += ChangeValue;
//			Debug.Log("CabService Start, grpcsvc instance: " + GrpcSvc.InstanceNum);
		}

		public void SetHost(string host, string eventHost)
		{
			if (host == Svc.Host)
				return;

			Debug.Log("CabService SetHost, counter: " + Counter + ", host: " + host + ", eventHost: " + eventHost??"(null)");

			if (!host.Trim().EndsWith("/"))
				host += "/";
			Svc.Host = host;

			if ( !string.IsNullOrEmpty(eventHost))
			{
				EventClient.Com.Address = eventHost;
				EventClient.Open();
			}

/*
			MsgSvc = new HubConnectionBuilder()
							.WithUrl(host + "cabhub")
							.WithAutomaticReconnect(new UnlimitedRetryPolicy())
							.Build();
			MsgSvc.On<IEnumerable<Dtos.IoItem>>("UpdateIoItems", OnReceive);
			MsgSvc.Reconnected += connectionId =>
			{
				var item = new Dtos.IoItem();
				item.Type = "connection";
				item.Name = "CabSvr Connection";
				item.Value = "reconnected";
				OnReceive(new Dtos.IoItem[] { item });
				return Task.CompletedTask;
			};
			MsgSvc.Closed += async ex =>
			{
				var item = new Dtos.IoItem();
				item.Type = "connection";
				item.Name = "CabSvr Connection";
				item.Value = "disconnected - " + (ex?.Message ?? "(unknown reason)");
				OnReceive(new Dtos.IoItem[] { item });
				await Task.FromResult(Task.CompletedTask);
				//				return Task.CompletedTask;
			};
*/
		}

		public async Task<IEnumerable<dto.Device>> GetDevices()
		{
			return await Svc.GetList<dto.Device>("devicess");
		}

		public async Task<IEnumerable<dto.Container>> GetContainers()
		{
			return await Svc.GetList<dto.Container>("containers-hierarchy");
		}

		public async Task<string> GetIoValue(string id)
		{
			return await Svc.Get<string>("ios/value", id);
		}

		public async Task<Dtos.IoItem> GetIoItem(string id)
		{
			return await Svc.Get<Dtos.IoItem>("ios", id);
		}

		public async Task<IEnumerable<Dtos.IoItem>> GetIoItemsByType(string type, string deviceType = null)
		{
			return await Svc.GetList<Dtos.IoItem>("ios/type", new Dictionary<string, string>() { { "type", type }, { "deviceType", deviceType } });
		}

		public async Task<string> PutIoValue(string id, string value)
		{
			var item = new BriefItem() { Id = id, Value = value };
			return await Svc.Put("ios", new BriefItem[] { item });
			//return await Svc.Patch("ios", new BriefItem[] { item });
		}

		private void OnReceive(object sender, Dtos.EventArgs arg)
		{
			Debug.Log("CabService OnReceive, action: " + arg.Action + ", item: " + arg.Item.Label + ", events: " + (Events==null?"(null)":"(not null)"));
			Events?.Invoke(this, arg);
		}
/*
		private void OnReceive(object sender, IEnumerable<Dtos.IoItem> items)
		{
			if (items == null || !items.Any())
				return;

			foreach (var item in items)
			{
				// ??? set label until server supports this
				if (!string.IsNullOrEmpty(item.Name))
					item.Label = item.Name;
				else if (!string.IsNullOrEmpty(item.Type))
					item.Label = item.Type;
				else
					item.Label = item.Id;
//???				Events?.Invoke(this, new Dtos.EventArgs() { Item = item, Action = "Updated" });
			}
		}
*/
		private HttpApiService Svc;
		private JsonProtocol<Dtos.EventArgs> EventClient;
		//private HubConnection MsgSvc;

		private class BriefItem
		{
			public string Id { get; set; }
			public string Value { get; set; }
		}
//		private class UnlimitedRetryPolicy : IRetryPolicy
//		{
//			public TimeSpan? NextRetryDelay(RetryContext retryContext)
//			{
//				return TimeSpan.FromSeconds(5);
//			}
//		}

		public void ChangeValue(IoItem item)
		{
Debug.Log("CabService ChangeValue 1");

			var dest = new Dtos.IoItem();
			dest.Id = item.Id;
			dest.Value = item.Value;
			dest.Direction = item.Direction;
			dest.ValueType = item.ValueType;
			dest.ContainerId = item.ContainerId;
			dest.Type = item.Type;
			dest.DeviceId = item.DeviceId;
			dest.DeviceChannel = item.DeviceChannel;
			dest.ScheduleId = item.ScheduleId;
			dest.Name = item.Name;
			dest.Range = item.Range;
			dest.Description = item.Description;

			Events?.Invoke(this, new Dtos.EventArgs() { Item = dest, Action = "Updated" });
Debug.Log("CabService ChangeValue 2");
		}

	} // CabService
}
