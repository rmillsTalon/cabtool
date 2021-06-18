using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Cab.Communications
{
	public class WebSocket : Comm
	{
		public WebSocket(string serverAddr=null, string name=null, ILogger logger=null)
			: base(serverAddr, name, logger)
		{
			IsShutdown = false;
		}
		internal WebSocket(System.Net.WebSockets.WebSocket socket, ILogger logger=null)
			: base(null, null, logger)
		{
			IsShutdown = false;
			CancelCulture = new System.Threading.CancellationTokenSource();
			CancelToken = CancelCulture.Token;
			Client = socket;
			if ( Client != null )
				Hander();
		}

		public override bool IsOpen()
		{
			return Client != null && Client.State == WebSocketState.Open && !IsShutdown;
		}

		public override async Task Open(string address = null)
		{
			if (IsOpen())
				return;

			if (!string.IsNullOrEmpty(address))
				Address = address;
			else if (string.IsNullOrEmpty(Address))
				throw new ApplicationException("Web socket cannot connect to server; the server address has not been provided.");

			var addr = Address.Trim();
			addr = addr.Replace("https:", "wss:")
						.Replace("http:", "ws:");

			LogDebug(GetLabel() + " - Open, addr: " + addr);

			var client = new ClientWebSocket();
			CancelCulture = new System.Threading.CancellationTokenSource();
			CancelToken = CancelCulture.Token;

			await client.ConnectAsync(new Uri(addr), CancelToken); // CancellationToken.None);
			LogDebug(GetLabel() + " - Open, connected");

			IsShutdown = false;
			Client = client;

			if ( !InReopen )
				Hander();
		}

		public override async void Close()
		{
			await CloseIfOpen();
		}

		private async Task<bool> CloseIfOpen()
		{
			// See HandleReadError for more info on this function's purpose

			// The proper way to close websockets is to include a closing handshake. I am not doing
			// that for 1: time, but we have to handle unexpected/hard closes anyway. Currently I
			// don't see a need for handshaking. This is primarily that I have just one purpose for
			// this and that's for a lowly utility. This guy researched and has a pretty good post.
			// https://mcguirev10.com/2019/08/17/how-to-close-websocket-correctly.html

			// Make sure that the Handler and the app do not close at the same time.

			System.Net.WebSockets.WebSocket client;
			lock (this)
			{
				client = Client;
				Client = null;
			}
			if (client == null)
				return false;

			// Note: the state could already be Aborted, this is likely that the client to this connection
			// disconnected. This function still needs to be called to clean up for a reconnection if configured
			// If the caller needs to know this, they can just check state. True is still returned from this call.

			LogDebug(GetLabel() + " - Close 2323, state: " + client.State.ToString());

			IsShutdown = true;

			try
			{
				// This cancel causes the handler's receive to wake up and throw an abort exception. See that
				// catch statement. Not calling this causes the handle to stay waiting the object is deleted
				// by .net or the program ends. No need to put a sleep after this call.
				// This switches the state to Aborted so the CloseAsync does not need to be called (the check
				// and the call to close could probably be remove).

				CancelCulture.Cancel();
				if (client.State == WebSocketState.Open)
					await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
			}
			catch (Exception ex)
			{
				LogError(GetLabel() + " - Close, error: " + ex.Message);
			}
			CancelToken = CancellationToken.None;
			LogDebug(GetLabel() + " - Close, closed");
			base.Close();

			return true;
		}
		protected override async Task ActualWrite(string data, int timeout = -1)
		{
			var tmp = Encoding.UTF8.GetBytes(data);
			var bytes = new ArraySegment<byte>(tmp, 0, tmp.Length);
			await Client.SendAsync(bytes, WebSocketMessageType.Text, true, CancelToken);
		}

		protected override void DataReceived(string data)
		{
			LogDebug(GetLabel() + " - DataReceived, data: " + data);
			ProcessReceivedData(data);
		}

		private async void Hander()
		{
			LogDebug(GetLabel() + " - Hander");

			var buffer = new byte[1024 * 4];

			while (IsOpen() && !IsShutdown)
			{
				try
				{
					var result = await Client.ReceiveAsync(new ArraySegment<byte>(buffer), CancelToken);

					if (result.CloseStatus.HasValue && !IsShutdown)
						throw new ApplicationException("Client result marked close.");

					ProcessResult(result, buffer);
				}
				catch ( Exception ex )
				{
					// If this determines the socket to remain close IsOpen() will return false to exit the handler
					await HandleReadError(ex);
				}
			}
			LogDebug(GetLabel() + " - Handler, leaving");
		}

		protected virtual async Task HandleReadError(Exception ex)
		{
			// WebSocket throws error when other end closes. We want it to become an event instead of an exception so it can
			// be handled better.

			var error = new Common.Error(ex);
			LogError(GetLabel() + " - HandleReadError, is open: " + IsOpen() + ", error: " + error.Message);

			// Errors that are just read error do not need any processing. Let Handler continue.
			if (ex == null || (!error.Message.Contains("remote party closed")
								&& !error.Message.Contains("application request")
								&& !error.Message.Contains("CloseReceived")))
			{
				return;
			}

			// Handle connection problems

			// If this call does not actually close, then this has been already closed through another route,
			// and this error should not be handled any futher.
			if (!await CloseIfOpen())
				return;

			await Reopen(ex);
		}

		private void ProcessResult(WebSocketReceiveResult result, byte[] data)
		{
			//??? result.EndOfMessage
			//result.MessageType
			var str = Encoding.UTF8.GetString(data, 0, result.Count);
			LogDebug(GetLabel() + " - ProcessResult, data: " + str);
			DataReceived(str);
		}

		private CancellationTokenSource CancelCulture;
		private CancellationToken CancelToken;
		private System.Net.WebSockets.WebSocket Client;
		private bool IsShutdown;

	} // WebSocket

}
