﻿using PureIM.ImImpl;
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
		public static bool IsRunning { get; private set; } = false;

		public static async Task StartServerAsync (ushort _port = 64250) {
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
					var _rclient = await _listener.AcceptTcpClientAsync ();
					await Log.WriteAsync ($"Accept new client[{_rclient.Client.RemoteEndPoint}]");

					_ = Task.Run (async () => {
						var _client_impl = new ImClientImplTcp (_rclient);
						var guest_client = new ImServerClientGuest (_client_impl);
						await _client_impl.RunAsync ();
					});
				} catch (SocketException) {
					break;
				}
			}
			IsRunning = false;
		}

		public static async Task Add (ImServerClient _client) {
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

		public static async Task<ImServerClient> GetClientAsync (long _userid) {
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