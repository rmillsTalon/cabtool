using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// See CommHub for overview

namespace Cab.Communications
{
	public abstract class Comm
	{
		public Comm(string address = null, string name = null, ILogger logger=null)
		{
			Address = address;
			Name = name;
			PendingData = new StringBuilder();
			SendEoln = "";
			RecvEoln = "";
			ReconnectAttempts = 0;
			InReopen = false;
			Logger = logger;
		}

		public string Address { get; set; }
		public string Name { get; set; }
		public string RecvEoln { get; set; }
		public string SendEoln { get; set; }
		public int ReconnectAttempts { get; set; }
		public int ReconnectAttempted { get; protected set; }

		public event EventHandler<ReceivedDataEventArgs> ReceivedData;
		public event EventHandler<string> Closed;
		//public event EventHandler<string> Opened;

		public abstract bool IsOpen();
		public abstract Task Open(string address=null);

		public virtual void Close()
		{
			Closed?.Invoke(this, "");
		}

		public virtual async Task Reopen(Exception origEx = null)
		{
			ReconnectAttempted = 0;
			if (ReconnectAttempts == 0)
				return;
			InReopen = true;

			while (ReconnectAttempts == -1 || ReconnectAttempted < ReconnectAttempts)
			{
				try
				{
					LogDebug(GetLabel() + " - Reopen, attempt: " + (ReconnectAttempted+1) + " of " + ReconnectAttempts);
					await Open();
					InReopen = false;
					return;
				}
				catch (Exception ex)
				{
					//Log error and attempt number
					LogError(GetLabel() + " - Reopen, cannot reconnect  after " + (ReconnectAttempted + 1) + " of "
									+ ReconnectAttempts + " attempts. " + ex.Message
									+ (origEx != null ? " First error: " + origEx.Message : ""));
				}
				++ReconnectAttempted;
				System.Threading.Thread.Sleep(2000);	//???
			}

			InReopen = false;

			// Don't think an exception is good here. Caller can use IsOpen() to determine success for now.
			//throw new ApplicationException("Cannot reconnect " + GetLabel() + " after " + ReconnectAttempted + " attempts. " + origEx?.Message ?? "");
		}

		public virtual string ReadWait(int timeout=-1)
		{
			throw new ApplicationException(GetType().Name + " does not support sync reads.");
		}

		public virtual async Task Write(string data, int timeout = -1)
		{
			LogDebug(GetLabel() + " - Write, data: " + data);

			var tmp = Encoding.UTF8.GetBytes(data);

			while (IsOpen())
			{
				try
				{
					await ActualWrite(data, timeout);
					return;
				}
				catch (Exception ex)
				{
					LogError(GetLabel() + " - Write, error: " + ex.Message);
					if (!IsOpen())
					{
						Close();	// Make sure things are cleaned up
						await Reopen(ex);
					}
				}
			}
		}

		public virtual async Task<string> WriteAndReceive(string data, int readTimeout = -1, int writeTimeout = -1)
		{
			await Write(data, writeTimeout);
			return ReadWait(readTimeout);
		}

		public string GetLabel()
		{
			if (!string.IsNullOrEmpty(Name))
				return Name;
			return GetType().Name + " " + Address??"";
		}

		//
		// Detailed internal operations supporting read and write

		protected virtual Task ActualWrite(string data, int timeout = -1)
		{
			throw new ApplicationException(GetType().Name + " does not support writing data.");
		}

		protected virtual void DataReceived(string data)
		{
			var lines = ProcessDataToLines(data);
			if (lines == null || !lines.Any())
				return;

			foreach (var line in lines)
				ProcessReceivedData(line);
		}

		protected IEnumerable<string> ProcessDataToLines(string data)
		{
			PendingData.Append(data);

			data = PendingData.ToString();

			if (!string.IsNullOrEmpty(RecvEoln))
			{
				// Split on delimiter. If data ends in delimiter, single, complete line: return.
				// Otherwise, keep incomplete line. Returns all but last empty or pending line.

				var lines = data.Split(RecvEoln.ToArray());
				if (!lines.Any() || lines.Length == 1)
					return null;
				PendingData.Clear();
//				if (Util.TrimLeading(lines.Last(), '\0') != "")
//					PendingData.Append(lines.Last());
				return lines.Take(lines.Length - 1);
			}
			PendingData.Clear();

			return new string[] { data };
		}

		protected virtual void ProcessReceivedData(string data)
		{
			ReceivedData?.Invoke(this, new ReceivedDataEventArgs() { Data = data });
		}

		public void LogError(string msg)
		{
			if (Logger != null)
				Logger.LogError(msg);
			else
				LogDebug(msg);
		}

		public void LogDebug(string msg)
		{
			try
			{
				if (Logger != null)
					Logger.LogDebug(msg);
				else
				{
					var t = DateTimeOffset.Now.TimeOfDay.ToString();
					Console.WriteLine(t + " " + msg);
				}
			}
			catch (Exception)
			{
			}
		}

		protected ILogger Logger;
		protected StringBuilder PendingData;
		protected bool InReopen;

	} // Comm
}
