using System;
using System.Threading.Tasks;

namespace PureIM.TestServer {
	class Program {
		static async Task Main (string[] args) {
			_ = Task.Run (async () => {
				while (true) {
					GC.Collect ();
					await Task.Delay (TimeSpan.FromSeconds (10));
				}
			});
			ImServer.Filter = new MyMsgFilter ();
			await ImServer.StartTcpServerAsync ();
		}
	}
}
