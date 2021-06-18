using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cab.Communications
{
	public class ProtocolBase
	{
		public ProtocolBase(Comm com, bool openCloseCom=true)
		{
			if (com == null)
				throw new ApplicationException("Creating a communications protocol requires a communication port.");
			OpenCloseCom = openCloseCom;
			Com = com;
		}

		public Comm Com { get; private set; }

		public bool IsOpen() { return Com != null && Com.IsOpen(); }
		public void Open()
		{
			if (!OpenCloseCom || IsOpen() )
				return;
			Com.Open();
		}

		public void Close()
		{
			if ( OpenCloseCom)
				Com.Close();
		}

		public virtual string GetLabel()
		{
			return GetType().Name;
		}

		private bool OpenCloseCom;
	}

	public class JsonProtocol<T> : ProtocolBase
	{
		public JsonProtocol(Comm com, bool closeCom=true)
			: base(com, closeCom)
		{
			com.ReceivedData += ReceivedData;
		}

		public async Task Write<T2>(T2 obj)
		{
			var json = JsonConvert.SerializeObject(obj);
			await Com.Write(json);
		}

		public event EventHandler<T> Events;

		public void ReceivedData(object sender, ReceivedDataEventArgs arg)
		{
			DataReceived(arg);
		}

		public virtual void DataReceived(ReceivedDataEventArgs arg)
		{
			Events.Invoke(this, JsonConvert.DeserializeObject<T>(arg.Data));
		}

	} // JsonProtocol
}
