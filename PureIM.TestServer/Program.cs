using System;
using System.Threading.Tasks;

namespace PureIM.TestServer {
	class Program {
		static async Task Main (string[] args) {
			Console.WriteLine ("Hello World!");
			await ImServer.StartServerAsync ();
		}
	}
}
