using PureIM.ImImpl;
using PureIM.Message;
using PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PureIM {
	public enum OnlineStatus { Offline, Online, TempOffline }

	class ImClient {
		public long UserId { get; private set; } = -1;
		private IImClientImpl ClientImpl { get; set; } = ImClientImplEmpty.Inst;

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



		public ImClient (long _userid) {
			UserId = _userid;
			Task.Run (async () => {
				await ImServer.Add (this);
				while (ClientImpl.Status.IsOnline () || ElapsedTime <= DateTime.Now) {
					DateTime _next_process_time;
					using (var _locker = await SendCachesMutex.LockAsync ()) {
						if (ClientImpl.Status.IsOnline () && SendCaches.Any () && SendCaches[0]._pstime <= DateTime.Now) {
							_ = ClientImpl.WriteAsync (SendCaches[0]._msg.Serilize ());
							SendCaches.Add ((SendCaches[0]._msg, DateTime.Now.Add (Config.MessageResend)));
							SendCaches.RemoveAt (0);
						}
						_next_process_time = SendCaches.Any () ? SendCaches[0]._pstime : DateTime.Now.Add (Config.MessageResend);
					}
					if (_next_process_time > DateTime.Now) {
						await Task.Delay (_next_process_time - DateTime.Now);
					}
					if (ClientImpl.Status.IsTempOffline () && DateTime.Now - ClientImpl.LastConnTime >= Config.OnlineMessageCache)
						ClientImpl = ImClientImplEmpty.Inst;
				}
				await ImServer.Remove (UserId);
			});
		}

		/// <summary>
		/// 发送信息
		/// </summary>
		/// <param name="_msg"></param>
		/// <returns></returns>
		public async Task SendAsync (IImMsg _msg) {
			_ = ClientImpl.WriteAsync (_msg.Serilize ());
			if (_msg is v0_AcceptMsg)
				return;
			using (var _locker = await SendCachesMutex.LockAsync ())
				SendCaches.Add ((_msg, DateTime.Now.Add (Config.MessageResend)));
		}

		public async Task OnRecvAsync (byte[] _data) {
			var _msg = IImMsg.FromBytes (_data);
			if (_msg is v0_AcceptMsg _reply_msg) {
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

				// 检查
				if (!ClientMsgFilter.CheckAccept (_msg)) {
					await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, AcceptMsgType.Reject);
					return;
				}

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, AcceptMsgType.Accept);

				// 存档
				if (_cmsg.Type.IsStore ()) {
					// TODO 存数据库
				}

				// 发送给接收者
				var _receivers = _cmsg switch {
					v0_PrivateMsg _priv_msg => new List<long> { _priv_msg.RecverUserId },
					v0_TopicMsg _topic_msg => await ImServer.GetTopicUserIds (_topic_msg.TopicId, _topic_msg.Type.IsOnlineOnly ()),
					_ => new List<long> (),
				};
				foreach (var _receiver in _receivers)
					await ImServer.SendAsync (_receiver, _msg);
			} else if (_msg is v0_StatusUpdateMsg _stupd_msg) {
				// 判定是否重复
				bool _repeat = false;
				if (_repeat)
					return;

				// 检查
				if (!ClientMsgFilter.CheckAccept (_stupd_msg)) {
					await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, AcceptMsgType.Reject);
					return;
				}

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendReplyAsync (_msg.MsgId, _msg.MsgIdShadow, AcceptMsgType.Accept);

				// TODO 存档
			}
		}

		public async Task SendReplyAsync (long _msgid, long _msgid_shadow, AcceptMsgType _type) {
			await SendAsync (new v0_AcceptMsg { MsgId = _msgid, MsgIdShadow = _msgid_shadow, Type = _type });
		}
	}
}
