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
		public bool IsRunning { get; private set; } = false;

		public async Task StartServerAsync (ushort _port = 64250) {
			////var _ssock = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			////_ssock.Bind (new IPEndPoint (IPAddress.Parse (_ip), _port));
			////_ssock.Listen ();
			////var _csock = _ssock.AcceptAsync ();
			var _listener = new TcpListener (IPAddress.Any, _port);
			_listener.Start ();
			//_ = Task.Run (async () => { await Task.Delay (TimeSpan.FromSeconds (10)); _listener.Stop (); });
			IsRunning = true;
			while (true) {
				try {
					using var _client = await _listener.AcceptTcpClientAsync ();
					using var _stream = _client.GetStream ();
				} catch (SocketException) {
					break;
				}
			}
			IsRunning = false;
		}

		public static async Task Add (ImClient _client) {
			using (var _locker = await ClientsMutex.LockAsync ())
				Clients.Add (_client.UserId, _client);
		}
		public static async Task Remove (long _userid) {
			using (var _locker = await ClientsMutex.LockAsync ()) {
				if (Clients.ContainsKey (_userid))
					Clients.Remove (_userid);
			}
		}

		public static async Task SendAsync (long _userid, IImMsg _msg) => await (await GetClientAsync (_userid)).SendAsync (_msg);

		public static async Task<ImClient> GetClientAsync (long _userid) {
			using (var _locker = await ClientsMutex.LockAsync ()) {
				if (Clients.ContainsKey (_userid))
					return Clients[_userid];
			}
			var _client = new ImClient (_userid);
			await _client.ProcessObject ();
			return _client;
		}

		public static async Task<List<long>> GetTopicUserIds (string _topic, bool _only_online) {
			List<long> _userids;
			using (var _locker = await SubscriptionsMutex.LockAsync ()) {
				if (Subscriptions.ContainsKey (_topic)) {
					_userids = Subscriptions[_topic].ToList ();
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

		public static async Task<List<long>> GetAllUserIds (bool _only_online) {
			if (_only_online) {
				using (var _locker = await ClientsMutex.LockAsync ())
					return Clients.Keys.ToList ();
			} else {

			}
		}

		// 客户端列表
		private static Dictionary<long, ImClient> Clients = new Dictionary<long, ImClient> ();
		private static AsyncLocker ClientsMutex = new AsyncLocker ();

		// 订阅列表
		private static Dictionary<string, HashSet<long>> Subscriptions = new Dictionary<string, HashSet<long>> ();
		private static AsyncLocker SubscriptionsMutex = new AsyncLocker ();
	}
}
