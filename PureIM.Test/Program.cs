using System;
using System.Threading.Tasks;

namespace PureIM.Test {
	class Program {
		static async Task Main (string[] args) {
			Console.WriteLine ("Hello World!");
			var _server = new PureIMServer ();
			await _server.StartServerAsync ();
			Console.WriteLine ("Hello World!");
			Console.ReadKey ();
		}
	}
}
