using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PureIM {
	public class PureIMServer {
		public async Task StartServerAsync (ushort _port = 64250) {
			//var _ssock = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			//_ssock.Bind (new IPEndPoint (IPAddress.Parse (_ip), _port));
			//_ssock.Listen ();
			//var _csock = _ssock.AcceptAsync ();
			var _listener = new TcpListener (IPAddress.Any, _port);
			_listener.Start ();
			_ = Task.Run (async () => { await Task.Delay (TimeSpan.FromSeconds (10)); _listener.Stop (); });
			while (true) {
				try {
					using var _client = await _listener.AcceptTcpClientAsync ();
					using var _stream = _client.GetStream ();
				} catch (SocketException) {
					break;
				}
			}
		}
	}
}
