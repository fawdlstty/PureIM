using PureIM.Message;
using System;
using System.Net.Sockets;
using System.Threading;

namespace PureIM.TestClient {
	class Program {
		static void Main (string[] args) {
			Console.WriteLine ("Connect to 127.0.0.1 ...");
			var _client = new TcpClient ("127.0.0.1", 64250);
			var _stream = _client.GetStream ();
			_stream.WriteAsync (v0_CmdMsg.LoginForce ());
			while (_client.Connected) {
				Console.WriteLine ("Connecting.");
				Thread.Sleep (1000);
			}
			Console.WriteLine ("Disonnect.");
			Console.ReadKey ();
		}
	}
}
