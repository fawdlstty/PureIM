using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.ServerClientImpl {
	interface IImClientImpl {
		public OnlineStatus Status { get; }
		public DateTime LastConnTime { get; }
		public Func<byte[], Task> OnRecvCbAsync { get; set; }
		public string UserDesp { get; set; }
		public string ClientAddr { get; }



		public Task<bool> SendAsync (byte[] _bytes);
		public Task<bool> SendPingAsync ();
		public Task RunAsync ();
		public Task CloseAsync ();
	}
}
