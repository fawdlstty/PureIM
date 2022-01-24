using System;
using System.Threading.Tasks;

namespace PureIM.TestServer {
	class Program {
		static async Task Main (string[] args) {
			ImServer.Filter = new MyMsgFilter ();
			await ImServer.StartTcpServerAsync ();
		}
	}
}
