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

		public static async Task StartTcpServerAsync (ushort _port = 64250) {
			if (Filter == null) {
				await Log.WriteAsync ($"start im server(tcp) failure because not set message filter.");
				return;
			}
			_ = Task.Run (Log.ProcessAsync);
			await Log.WriteAsync ($"start im server(tcp) in port[{_port}]");
			var _listener = new TcpListener (IPAddress.Any, _port);
			_listener.Start ();
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
							if (_cmsg.CmdType == MsgCmdType.Auth && _cmsg.Option == "connect") {
								long? _ouserid = await Filter.Connect (_cmsg.Attachment);
								if (_ouserid != null) {
									_userid = _ouserid.Value;
									return;
								} else {
									await _client_impl.SendReplyAndLoggingAsync (-1, _seq, "auth failure");
								}
							} else {
								await _client_impl.SendReplyAndLoggingAsync (-1, _seq, "cmdtype is not `Auth` or option is not `connect`");
							}
						} else {
							await _client_impl.SendReplyAndLoggingAsync (-1, _seq, "msg is not `v0_CmdMsg`");
						}
						await _client_impl.CloseAsync ();
					};
					ImServerClient _client = null;
					try {
						await _client_impl.RunOnceAsync ();
						_client = await GetClientAsync (_userid);
						_client.OnClearExit = async () => {
							await Remove (_userid);
							await Log.WriteAsync ($"{_client_impl.UserDesp} connect clear.");
						};
						await _client.SetClientImpl (_client_impl, _seq);

						await _client_impl.RunAsync ();
						await Log.WriteAsync ($"{_client_impl.UserDesp} temp disconnect.");
					} catch (Exception _e) {
						await _client_impl.SendReplyAndLoggingAsync (-1, -1, $"exception: {_e.Message}");
					}
					if (_client == null)
						return;
				});
			}
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
			ImServerClient _client = null;
			using (var _locker = await ClientsMutex.LockAsync ()) {
				if (Clients.ContainsKey (_userid)) {
					_client = Clients[_userid];
					if (_client.ToClear) {
						await Task.Delay (TimeSpan.FromMilliseconds (1));
						return await GetClientAsync (_userid);
					}
					return _client;
				}
			}
			_client = new ImServerClient (_userid);
			await Add (_client);
			return _client;
		}

		public static async Task<List<long>> GetTopicUserIds (long _topicid, bool _only_online) {
			if (_topicid == 0) {
				// 广播消息
				if (_only_online) {
					using (var _locker = await ClientsMutex.LockAsync ())
						return Clients.Keys.ToList ();
				} else {
					return await DataStorer.GetAllUserIdsAsync ();
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
