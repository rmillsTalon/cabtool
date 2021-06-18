using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using Newtonsoft.Json;

using Grpc.Core;
using GrpcCabinet;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public class CabinetServiceGrpc : MonoBehaviour
    {
        private static int NextInstance = 1;
        public readonly int InstanceNum;
        public CabinetServiceGrpc()
        {
            InstanceNum = NextInstance;
            ++NextInstance;
        }

        const string GRPC_CABINETSERVICE_URL = "localhost:5010";
        bool iosLoaded;
        bool containersLoaded;
        bool devicesLoaded;

        string targetUrl = GRPC_CABINETSERVICE_URL;
        Channel channel;
        Cabinet.CabinetClient client;

        // Keep track of what we got back

        public List<IoItem> ios;
        public List<ContainerItem> containers;

        public List<DeviceItem> devices;
        //public GameObject canvas;
        //Sliders sliders;
        //Radials radials;

        private Subscription _subscription;

        public event Action<IoItem> OnChangeValue;
        public string getUrl()
        {
            return targetUrl;
        }
        public void setUrl(string url)
        {
            if (targetUrl != null)
            {
                targetUrl = url;
                InitCabinetClient();
            }
            
        }
        void  InitCabinetClient()
        {
             channel = new Channel(targetUrl, ChannelCredentials.Insecure);
             client = new GrpcCabinet.Cabinet.CabinetClient(channel);
        }
        //Cabinet.CabinetClient GetCabinetClient()
        //{
        //    var channel = new Channel(targetUrl, ChannelCredentials.Insecure);
        //    var client = new GrpcCabinet.Cabinet.CabinetClient(channel);
        //    return client;
        //}
        void Awake()
        {
            //sliders = canvas.GetComponent<Sliders>();
            //radials = canvas.GetComponent<Radials>();

            containersLoaded = false;
            iosLoaded = false;
            devicesLoaded = false;
            targetUrl = GRPC_CABINETSERVICE_URL;
            InitCabinetClient();

            this.DoCheck();
        }

        void OnDisable()
        {
            Debug.Log("PrintOnDisable: script was disabled");
            Unsubscribe();
        }
        async void getContainersFromServer()
        {
            //var client = GetCabinetClient();
            var reply = await client.GetContainersAsync(
                              new CabinetRequest());
            Debug.Log("GetContainers: " + reply.Containers.Count);
            this.containers = new List<ContainerItem>();
            var mapper = new Mapper();
            foreach (var c in reply.Containers)
            {
                this.containers.Add(mapper.ToDto(c));
            }            
            containersLoaded = true;

        }

        async void getIosFromServer()
        {
            //var client = GetCabinetClient();
            var reply = await client.GetIosAsync(
                              new CabinetRequest());
            Debug.Log("GetIos: " + reply.Ios.Count);
            this.ios = new List<IoItem>();
            var mapper = new Mapper();
            foreach (var c in reply.Ios)
            {
                this.ios.Add(mapper.ToDto(c));
            }
            iosLoaded = true;
        }

        async void getDevicesFromServer()
        {
            //var client = GetCabinetClient();
            var reply = await client.GetDevicesAsync(
                              new CabinetRequest());
            Debug.Log("GetDevices: " + reply.Devices.Count);
            this.devices = new List<DeviceItem>();
            var mapper = new Mapper();
            foreach (var c in reply.Devices)
            {
                if (!string.IsNullOrWhiteSpace(c.ClassName))
                    this.devices.Add(mapper.ToDto(c));
            }
            //devices = devices.Where(x => !string.IsNullOrWhiteSpace(x.ClassName)).ToList();
            devicesLoaded = true;
        }
        // Where to send our request

        public System.Threading.CancellationTokenSource CanSrc = new System.Threading.CancellationTokenSource();
        public System.Threading.CancellationToken SubscriptionCancelToken;
        public async void Subscribe()
        {
            //Task.Run(async () =>
            //{
                await Subscribe(Guid.NewGuid().ToString("N"));
            //}).ConfigureAwait(false).GetAwaiter();
        }
        public async Task Subscribe(string subscriptionId)
        {
            SubscriptionCancelToken = CanSrc.Token;

            //var client = GetCabinetClient();
            _subscription = new Subscription() { Id = subscriptionId };
            Debug.Log($">> SubscriptionId : {subscriptionId}");
            using (var call = client.Subscribe(_subscription))
            {

                //Receive
                //var responseReaderTask = Task.Run(async () =>
                //{
                    while (await call.ResponseStream.MoveNext(SubscriptionCancelToken))
                    {
                        Debug.Log("Event received: " + call.ResponseStream.Current);
                        var msg = call.ResponseStream.Current;
                        try
                        {
                            ProcessMsg(msg);
                        }
                        catch(Exception ex)
                        {
                            Debug.Log(ex);
                        }
                    }
                //});

                //await responseReaderTask;
            }
        }

        public void Unsubscribe()
        {
            if (_subscription != null)
            {
                CanSrc.Cancel();
                client.Unsubscribe(_subscription);
                channel.ShutdownAsync();
                Debug.Log("Unsubscribed");

            }
        }

        void ProcessMsg(IoMsg msg)
        {          
            var mapper = new Mapper();
            var item = mapper.ToDto(msg);
            var f = this.ios.FirstOrDefault(x => x.Id == item.Id);
            if (f != null)
            {
                if (f.Value != item.Value)
                {
                    Debug.Log("Change received, instance: " + NextInstance);
                     
                    if (OnChangeValue !=null)
                         OnChangeValue( item);                      
                  
                }
            }
        }

        bool Shutdown = false;
        void OnApplicationQuit()
        {
            Shutdown = true;
        }

        async void DoCheck()
        {
            for (; ; )
            {
                try
                {
                    if (Shutdown)
                    {
//                        Unsubscribe();
                        return;
                    }

                    //var client = GetCabinetClient();
                    var reply = client.GetHealth(new CabinetRequest());
                    if (reply.IsHealthy)
                    {
                        break;
                    }
                }
                catch (RpcException e)
                {
                    Debug.Log($"GetHealth: {e.Status}");
                }
                if (Shutdown)
                {
//                    Unsubscribe();
                    return;
                }
                await Task.Delay(1000);
            }
            try
            {
                getContainersFromServer();
                getIosFromServer();
                getDevicesFromServer();

                Subscribe();
            }
            catch (RpcException e)
            {
                Debug.Log($"DoCheck: {e.Status}");
            }
        }
        public bool IsReady()
        {
            return containersLoaded && iosLoaded && devicesLoaded;
        }



        public void ChangeByContainerId(string containerId, string value)
        {
            var io = ios.FirstOrDefault(n => n.ContainerId == containerId);
            if (io == null) return;
            Change(io.Id, value);

        }

        public void ChangeByDeviceId(string deviceId, string value)
        {
            var io = ios.FirstOrDefault(n => n.DeviceId == deviceId);
            if (io == null) return;
            Change(io.Id, value);

        }
        public async void Change(string id, string value)
        {
            var io = ios.FirstOrDefault(n => n.Id == id);
            if (io == null) return;

            io.Value = value;

            var item = new BriefMsg() { Id = id, Value = value };
            var items = new List<BriefMsg>();
            items.Add(item);
            var request = new IoDirectRequest();
            request.UpdateItems.AddRange(items);
            try
            {
                //var client = GetCabinetClient();
                var reply = await client.UpdateIoDirectAsync(request);
                Debug.Log("UpdateIo: " + reply.Success);
            }
            catch (RpcException e)
            {
                Debug.Log($"UpdateIo: {e.Status}");
            }
        }

        public  void ChangeLocal(string id, string value)
        {
            var io = ios.FirstOrDefault(n => n.Id == id);
            if (io == null) return;

            io.Value = value;
        }

            public string GetDeviceType(int value)
        {
            var deviceItem = devices[value];
            var type = !string.IsNullOrEmpty(deviceItem.ClassName) ? deviceItem.ClassName : deviceItem.Type;
            return type;
        }

        public List<ContainerItem> GetDrawers()
        {
            var id = this.containers[0].Id;
            var selected = this.containers.Where(x => x.ParentId == id&&(string.IsNullOrEmpty(x.Position)||x.Position.Equals("front", StringComparison.InvariantCultureIgnoreCase))).ToList();           
            return selected;
        }

        public List<IoItem> GetIosSliders()
        {
            var selected = this.ios.Where(x => x.ValueType != "string" && (x.Id == "1" || x.Id.Length > 2) && x.Type != "batteryVoltage" && x.Type != "led" && x.Type != "servoPosition").ToList();
            return selected;
        }
        public List<IoItem> GetIosRadials()
        {
            var selected = this.ios.Where(x => x.Type == "batteryVoltage" || x.Type == "led" || x.Type == "servoPosition" || x.Type == "bist").ToList();
            return selected;
        }
    }
}

