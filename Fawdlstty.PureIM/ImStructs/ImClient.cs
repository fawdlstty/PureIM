using Fawdlstty.PureIM.ImStructs.Message;
using Fawdlstty.PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.ImStructs {
	public class ImClient {
		private WebSocket WS { get; set; }
		public long UserId { init; get; }

		/// <summary>
		/// WS断开连接后赋值，用于确定超时后发送状态（此对象）的存在时间
		/// </summary>
		private DateTime ElapsedTime { get; set; } = DateTime.Now.Add (Config.OnlineMessageCache);

		// 发送状态缓存
		private List<(IImMsg _msg, DateTime _pstime)> SendCaches = new List<(IImMsg _msg, DateTime _pstime)> ();
		private static AsyncLocker SendCachesMutex = new AsyncLocker ();

		// 接收状态缓存 （已接收的信息）
		private List<(long _msgid, DateTime _pstime)> RecvCaches = new List<(long _msgid, DateTime _pstime)> ();
		private static AsyncLocker RecvCachesMutex = new AsyncLocker ();



		public ImClient (WebSocket _ws, long _userid) {
			WS = _ws;
			UserId = _userid;
		}

		public async Task Process () {
			await ImManager.Add (this);
			// 处理未送达的情况
			_ = Task.Run (async () => {
				while (WS != null || ElapsedTime <= DateTime.Now) {
					DateTime _next_process_time;
					using (var _locker = await SendCachesMutex.LockAsync ()) {
						if (WS != null && SendCaches.Any () && SendCaches[0]._pstime <= DateTime.Now) {
							_ = ImplSendAsync (SendCaches[0]._msg.Serilize ());
							SendCaches.Add ((SendCaches[0]._msg, DateTime.Now.Add (Config.MessageResend)));
							SendCaches.RemoveAt (0);
						}
						_next_process_time = SendCaches.Any () ? SendCaches[0]._pstime : DateTime.Now.Add (Config.MessageResend);
					}
					if (_next_process_time > DateTime.Now)
						await Task.Delay (_next_process_time - DateTime.Now);
				}
				await ImManager.Remove (UserId);
			});

			// TODO 没有WS对象则不执行
			var _buf = new byte [1024 * 4];
			var _recv_data = new List<byte> ();
			var _source = new CancellationTokenSource (TimeSpan.FromSeconds (10));
			int _msg_size = 0;
			while (!WS.CloseStatus.HasValue) {
				try {
					var _result = await WS.ReceiveAsync (_buf, CancellationToken.None);
					if (_result.MessageType != WebSocketMessageType.Text) { // _result.MessageType == WebSocketMessageType.Close
						await Log.WriteAsync ($"websocket receive type[{_result.MessageType}] msg. disconnect.");
						break;
					}
					_recv_data.AddRange (new ReadOnlySpan<byte> (_buf, 0, _result.Count).ToArray ());

					// 读取长度
					if (_msg_size == 0 && _recv_data.Count >= 4) {
						_msg_size = BitConverter.ToInt32 (_recv_data.Take (4).ToArray (), 0);
						_recv_data.RemoveRange (0, 4);
					}

					// 读取内容
					if (_msg_size > 0 && _recv_data.Count >= _msg_size) {
						var _msg = IImMsg.FromBytes (_recv_data.Take (_msg_size).ToArray ());
						_recv_data.RemoveRange (0, _msg_size);
						if (_msg != null)
							await OnRecvAsync (_msg);
						_msg_size = 0;
					}
				} catch (Exception _ex) {
					await Log.WriteAsync (_ex);
					break;
				}
			}
		}

		/// <summary>
		/// 发送信息
		/// </summary>
		/// <param name="_msg"></param>
		/// <returns></returns>
		public async Task SendAsync (IImMsg _msg) {
			_ = ImplSendAsync (_msg.Serilize ());
			if (_msg is v0_ReplyMsg)
				return;
			using (var _locker = await SendCachesMutex.LockAsync ())
				SendCaches.Add ((_msg, DateTime.Now.Add (Config.MessageResend)));
		}

		/// <summary>
		/// 发送信息实现
		/// </summary>
		/// <param name="_data"></param>
		/// <returns></returns>
		private async Task<bool> ImplSendAsync (byte[] _data) {
			if (WS != null) {
				try {
					var _source = new CancellationTokenSource (Config.MessageTimeout);
					await WS.SendAsync (_data, WebSocketMessageType.Binary, true, _source.Token);
					return true;
				} catch (Exception) {
				}
			}
			return false;
		}

		public async Task OnRecvAsync (IImMsg _msg) {
			if (_msg is v0_ReplyMsg _reply_msg) {
				using (var _locker = await SendCachesMutex.LockAsync ()) {
					for (int i = 0; i < SendCaches.Count; ++i) {
						if (SendCaches[i]._msg.MsgId == _reply_msg.MsgId) {
							SendCaches.RemoveAt (i);
							break;
						}
					}
				}
			} else if (_msg is IImContentMsg _cmsg) {
				// 判定是否重复
				bool _repeat = false;
				if (_repeat)
					return;

				var (_accept, _receivers) = _cmsg switch {
					v0_PrivateMsg _priv_msg => (ClientMsgFilter.CheckAccept (_priv_msg), new List<long> { _priv_msg.ToUserId }),
					v0_TopicMsg _topic_msg => (ClientMsgFilter.CheckAccept (_topic_msg), new List<long> { /*TODO*/ }),
					v0_BroadcastMsg _bdcast_msg => (ClientMsgFilter.CheckAccept (_bdcast_msg), new List<long> { /*TODO*/ }),
				};

				// 检查
				if (!_accept) {
					await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, ReplyMsgType.Reject);
					return;
				}

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, ReplyMsgType.Accept);

				// TODO 判定是否存档
				bool _store = true;
				if (_store) {
					// TODO 存数据库
				}

				// TODO 发送给接收者
				foreach (var _receiver in _receivers)
					await ImManager.SendAsync (_receiver, _msg);
			} else if (_msg is v0_TopicMsg _topic_msg) {
				// 判定是否重复
				bool _repeat = false;
				if (_repeat)
					return;

				// 检查
				if (!ClientMsgFilter.CheckAccept (_topic_msg)) {
					await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, ReplyMsgType.Reject);
					return;
				}

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, ReplyMsgType.Accept);

				// TODO 查询主题下所有用户
				var _userids = new List<long> ();

				// 判定是否存档
				bool _store = true;
				if (_store) {
					// TODO 存数据库
				}

				// TODO 发送给接收者
			} else if (_msg is v0_BroadcastMsg _bdcast_msg) {
				// 判定是否重复
				bool _repeat = false;
				if (_repeat)
					return;

				// 检查
				if (!ClientMsgFilter.CheckAccept (_bdcast_msg)) {
					await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, ReplyMsgType.Reject);
					return;
				}

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, ReplyMsgType.Accept);

				// 判定是否存档
				bool _store = true;
				if (_store) {
					// TODO 存数据库
				}

				// TODO 发送给接收者
			} else if (_msg is v0_StatusUpdateMsg _stupd_msg) {
				// 判定是否重复
				bool _repeat = false;
				if (_repeat)
					return;

				// 检查
				if (!ClientMsgFilter.CheckAccept (_stupd_msg)) {
					await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, ReplyMsgType.Reject);
					return;
				}

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, ReplyMsgType.Accept);

				// 存档
			}
		}

		public async Task SendReplyAsync (long _msgid, long _msgid_shadow, ReplyMsgType _type) {
			await SendAsync (new v0_ReplyMsg { MsgId = _msgid, MsgIdShadow = _msgid_shadow, Type = _type });
		}
	}
}
