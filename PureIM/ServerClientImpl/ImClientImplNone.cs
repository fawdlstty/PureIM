using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.ServerClientImpl {
	class ImClientImplNone: IImClientImpl {
		public OnlineStatus Status { get; private set; } = OnlineStatus.Offline;
		public DateTime LastConnTime => DateTime.Now - Config.OnlineMessageCache;
		public static IImClientImpl Inst { get; } = new ImClientImplNone ();
		public Func<byte[], Task> OnRecvCbAsync { get; set; } = null;
		public Func<Task> OnCloseAsync { get; set; } = null;
		public string UserDesp { get; set; }
		public string ClientAddr { get => "unknown"; }



		public Task<bool> SendAsync (byte[] _bytes) => Task.FromResult (false);
		public Task CloseAsync () => Task.CompletedTask;
	}
}
