using PureIM.ServerClientImpl;
using PureIM.Message;
using PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PureIM {
	public class ImServer {
		public static IMessageFilter Filter { get; set; } = null;
		public static bool TcpServerIsRunning { get; private set; } = false;

		public static async Task StartTcpServerAsync (ushort _port = 64250) {
			if (Filter == null) {
				await Log.WriteAsync ($"start im server(tcp) failure because not set message filter.");
				return;
			}
			_ = Task.Run (Log.ProcessAsync);
			await Log.WriteAsync ($"start im server(tcp) in port[{_port}]");
			var _listener = new TcpListener (IPAddress.Any, _port);
			_listener.Start ();
			TcpServerIsRunning = true;
			while (true) {
				var _rclient = await _listener.AcceptTcpClientAsync ();

				_ = Task.Run (async () => {
					var _client_impl = new ImClientImplTcp (_rclient);
					await Log.WriteAsync ($"accept tcp connect from {_client_impl.ClientAddr}");
					var _guest_client = new ImServerClientGuest (_client_impl);
					await _client_impl.RunAsync ();
					await Log.WriteAsync ($"{_client_impl.UserDesp} disconnect.");
				});
			}
			TcpServerIsRunning = false;
		}

		internal static async Task Add (ImServerClient _client) {
			using (var _locker = await ClientsMutex.LockAsync ())
				Clients.Add (_client.UserId, _client);
		}
		internal static async Task Remove (long _userid) {
			using (var _locker = await ClientsMutex.LockAsync ()) {
				if (Clients.ContainsKey (_userid))
					Clients.Remove (_userid);
			}
		}

		public static async Task SendAsync (long _userid, IImMsg _msg) => await (await GetClientAsync (_userid)).SendAndLoggingAsync (_msg);

		internal static async Task<ImServerClient> GetClientAsync (long _userid) {
			using (var _locker = await ClientsMutex.LockAsync ()) {
				if (Clients.ContainsKey (_userid))
					return Clients[_userid];
			}
			var _client = new ImServerClient (_userid);
			return _client;
		}

		public static async Task<List<long>> GetTopicUserIds (long _topicid, bool _only_online) {
			if (_topicid == 0) {
				// 广播消息
				if (_only_online) {
					using (var _locker = await ClientsMutex.LockAsync ())
						return Clients.Keys.ToList ();
				} else {
					return await DataStorer.GetAllUserIds ();
				}
			} else {
				// 主题消息
				List<long> _userids;
				using (var _locker = await SubscriptionsMutex.LockAsync ()) {
					if (Subscriptions.ContainsKey (_topicid)) {
						_userids = Subscriptions[_topicid].ToList ();
					} else {
						return new List<long> ();
					}
				}
				if (_only_online) {
					using (var _locker = await ClientsMutex.LockAsync ()) {
						_userids = (from p in _userids where Clients.ContainsKey (p) select p).ToList ();
					}
				}
				return _userids;
			}
		}

		// 客户端列表
		private static Dictionary<long, ImServerClient> Clients { get; set; } = new Dictionary<long, ImServerClient> ();
		private static AsyncLocker ClientsMutex = new AsyncLocker ();

		// 订阅列表
		private static Dictionary<long, HashSet<long>> Subscriptions { get; set; } = new Dictionary<long, HashSet<long>> ();

		private static AsyncLocker SubscriptionsMutex = new AsyncLocker ();
	}
}
