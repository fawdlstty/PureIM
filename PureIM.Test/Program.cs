using System;
using System.Threading.Tasks;

namespace PureIM.Test {
	class Program {
		static async Task Main (string[] args) {
			Console.WriteLine ("Hello World!");
			await ImServer.StartServerAsync ();
			Console.WriteLine ("Server Stop.");
			Console.ReadKey ();
		}
	}
}
