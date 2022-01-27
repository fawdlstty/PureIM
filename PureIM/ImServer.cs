using PureIM.ServerClientImpl;
using PureIM.Message;
using PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;

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
					// 接受新链接
					var _client_impl = new ImClientImplTcp (_rclient);
					await Log.WriteAsync ($"accept tcp connect from {_client_impl.ClientAddr}");
					long _userid = -1, _seq = -1;
					_client_impl.OnRecvCbAsync = async (_data) => {
						var _msg = IImMsg.FromBytes (_data);
						_seq = _msg.Seq;
						await Log.WriteAsync ($"{_client_impl.UserDesp} -> server: {_msg.SerilizeLog ()}");
						if (_msg is v0_CmdMsg _cmsg) {
							if (_cmsg.CmdType == MsgCmdType.Auth && _cmsg.Option == "login" && _cmsg.Attachment != null) {
								var auth_str = Encoding.UTF8.GetString (_cmsg.Attachment);
								if (auth_str.StartsWith ("[forcelogin]")) {
									if (long.TryParse (auth_str[12..], out _userid)) {
										return;
									}
								}
							} else {
								await _client_impl.SendReplyAndLoggingAsync (_seq, "cmdtype is not `Auth` or option is not `login` or attachment is null");
							}
						} else {
							await _client_impl.SendReplyAndLoggingAsync (_seq, "msg is not `v0_CmdMsg`");
						}
						await _client_impl.CloseAsync ();
					};
					ImServerClient _client = null;
					try {
						await _client_impl.RunOnceAsync ();
						await Log.WriteAsync ($"{_client_impl.ClientAddr} login to {_client_impl.UserDesp}");
						_client = await GetClientAsync (_userid);
						await _client.SetClientImpl (_client_impl, _seq);
					} catch (Exception _e) {
						await _client_impl.SendReplyAndLoggingAsync (-1, $"exception: {_e.Message}");
					}
					if (_client == null)
						return;
					// TODO ???
					await _client.CloseAsync ();
					using (var _locker = await ClientsMutex.LockAsync ()) {
						ClientsOnline.Remove (_userid);
						ClientsTempClose[_userid] = _client;
					}
					await Log.WriteAsync ($"{_client_impl.UserDesp} disconnect.");
					// TODO 专用线程清理发送缓存
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
