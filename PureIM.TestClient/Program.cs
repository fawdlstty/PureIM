﻿using PureIM.Message;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PureIM.TestClient {
	class Program {
		private static async Task _start_one (long _id) {
			try {
				var _client = new TcpClient ("127.0.0.1", 64250);
				var _stream = _client.GetStream ();
				await _stream.WriteAsync (v0_CmdMsg.LoginForce (1, _id));
				var _bytes = new byte [4] { 0, 0, 0, 0 };
				while (_client.Connected) {
					await _stream.WriteAsync (_bytes);
					await Task.Delay (180000);
				}
			} catch (Exception) {
			}
			Console.WriteLine ($"user[{_id}] disconnect");
		}

		static void Main (string[] args) {
			Console.WriteLine ("connect 10000 counts to 127.0.0.1 ...");
			for (int i = 1; i <= 10000; ++i) {
				int _t = i;
				Func<Task> _f = async () => { await _start_one (_t); };
				_ = Task.Run (_f);
			}
			Console.WriteLine ("success. preee any key to exit.");
			Console.ReadKey ();
		}
	}
}
