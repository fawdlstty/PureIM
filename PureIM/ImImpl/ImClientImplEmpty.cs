using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.ImImpl {
	class ImClientImplEmpty: IImClientImpl {
		public OnlineStatus Status { get; private set; } = OnlineStatus.Offline;
		public DateTime LastConnTime => DateTime.Now - Config.OnlineMessageCache;

		public Task<bool> WriteAsync (byte[] _bytes) => Task.FromResult (false);
		public static IImClientImpl Inst { get; } = new ImClientImplEmpty ();
		public Func<byte[], Task> OnRecvCbAsync { get; set; } = null;
		public Task CloseAsync () => Task.CompletedTask;
	}
}
