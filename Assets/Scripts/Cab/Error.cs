using System;
using System.Collections.Generic;
using System.Text;

namespace Cab.Common
{
	public class Error
	{
		public Error()
		{
			Timestamp = DateTimeOffset.Now;
		}

		public Error(Exception ex)
			: base()
		{
			Format(ex);
		}

		public string Type { get; set; }
		public int HttpStatusCode { get; set; }
		public string Message { get; set; }
		public string InternalDetails { get; set; }
		public DateTimeOffset Timestamp { get; set; }
		public string ReferenceId { get; set; }

		public void Format(Exception ex)
		{
			if (ex == null)
				return;

			Type = ex.GetType().Name;

			StringBuilder s = new StringBuilder();
			StringBuilder msg = new StringBuilder();

			while (ex != null)
			{
				if (s.Length != 0)
					s.Append("\n\n");
				s.Append(ex.ToString());

				if (msg.Length != 0)
					msg.Append(" ");
				msg.Append(ex.Message);

				ex = ex.InnerException;
			}

			Message = msg.ToString();
			InternalDetails = s.ToString();
		}

	}
}
