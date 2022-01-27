using PureIM.ServerClientImpl;
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

	class ImServerClient {
		public long UserId { get; private set; } = -1;
		private IImClientImpl ClientImpl { get; set; } = ImClientImplNone.Inst;
		private long Seq = 0;

		// 发送状态缓存
		private List<(IImMsg _msg, DateTime _pstime)> SendCaches = new List<(IImMsg _msg, DateTime _pstime)> ();
		private AsyncLocker SendCachesMutex = new AsyncLocker ();

		// 接收状态缓存 （已接收的信息）
		private List<(long _msgid, DateTime _pstime)> RecvCaches = new List<(long _msgid, DateTime _pstime)> ();
		private static AsyncLocker RecvCachesMutex = new AsyncLocker ();



		/// <summary>
		/// 清理超时信息
		/// </summary>
		/// <returns></returns>
		private async Task ClearTimeoutMsgAsync () {
			if (SendCaches.Any ()) {
				using (var _locker = await SendCachesMutex.LockAsync ()) {
					while (SendCaches.Any () && SendCaches [0]._pstime <= DateTime.Now - Config.OnlineMessageCache)
						SendCaches.RemoveAt (0);
				}
			}
			if (RecvCaches.Any ()) {
				using (var _locker = await RecvCachesMutex.LockAsync ()) {
					while (RecvCaches.Any () && RecvCaches [0]._pstime <= DateTime.Now - Config.OnlineMessageCache)
						RecvCaches.RemoveAt (0);
				}
			}
		}

		public ImServerClient (long _userid) {
			UserId = _userid;
			Task.Run (async () => {
				await ImServer.Add (this);
				while (true) {
					// 清理超时信息
					await ClearTimeoutMsgAsync ();

					// 如果链接已断开，并且没有任何缓存信息，那么直接清理链接对象
					if (!(ClientImpl.Status.IsOnline () || SendCaches.Any () || RecvCaches.Any ()))
						break;

					// TODO 每2次清理时间发一个ping

					// 等待下一个清理时间
					await Task.Delay (Config.MsgQueueClearSpan);
				}
				await Log.WriteAsync ($"{ClientImpl.UserDesp} connect clear.");
				ClientImpl = ImClientImplNone.Inst;
				await ImServer.Remove (UserId);
			});
		}

		public async Task SetClientImpl (IImClientImpl _client_impl, long _seq) {
			if (ClientImpl != ImClientImplNone.Inst)
				ClientImpl.UserDesp = $"dup user[{UserId}]";
			if (ClientImpl.Status.IsOnline ()) {
				var _ret = v0_CmdMsg.Offline (++Seq, "offline due to duplicate");
				await Log.WriteAsync ($"server -> {ClientImpl.UserDesp}: {_ret.SerilizeLog ()}");
				await ClientImpl.SendAsync (_ret.Serilize ());
				await ClientImpl.CloseAsync ();
			}
			ClientImpl = _client_impl;
			ClientImpl.OnRecvCbAsync = OnRecvAsync;
			ClientImpl.UserDesp = $"user[{UserId}]";
			await SendAndLoggingAsync (v0_ReplyMsg.LoginSuccess (_seq));

			// 新上线或者重新上线，缓存信息重新全发一遍
		}

		/// <summary>
		/// 发送信息
		/// </summary>
		/// <param name="_msg"></param>
		/// <returns></returns>
		public async Task SendAsync (IImMsg _msg) {
			_ = ClientImpl.SendAsync (_msg.Serilize ());
			using (var _locker = await SendCachesMutex.LockAsync ())
				SendCaches.Add ((_msg, DateTime.Now));
		}

		public async Task SendAndLoggingAsync (IImMsg _msg) {
			await Log.WriteAsync ($"server -> {ClientImpl.UserDesp}: {_msg.SerilizeLog ()}");
			await SendAsync (_msg);
		}

		public async Task OnRecvAsync (byte[] _data) {
			var _msg = IImMsg.FromBytes (_data);
			await Log.WriteAsync ($"{ClientImpl.UserDesp} -> server: {_msg.SerilizeLog ()}");
			if (_msg is v0_ReplyMsg) {
				using var _locker = await SendCachesMutex.LockAsync ();
				for (int i = 0; i < SendCaches.Count; ++i) {
					if (SendCaches[i]._msg.MsgId == _msg.MsgId && SendCaches[i]._msg.Seq == _msg.Seq) {
						SendCaches.RemoveAt (i);
						break;
					}
				}
			} else if (_msg is v0_CmdMsg _cmsg) {
				throw new Exception ();
			} else if (_msg is v0_StatusUpdateMsg _stupd_msg) {
				// 判定是否重复
				bool _repeat = false;
				if (_repeat)
					return;

				// 检查
				if (!await ImServer.Filter.CheckAccept (_stupd_msg)) {
					await SendReplyAsync (_msg.MsgId, _msg.Seq, AcceptMsgType.Reject);
					return;
				}

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendReplyAsync (_msg.MsgId, _msg.Seq, AcceptMsgType.Accept);

				// TODO 存档
			} else if (_msg is IImContentMsg _cntmsg) {
				// 判定是否重复
				bool _repeat = false;
				if (_repeat)
					return;

				// 检查
				if (!await ImServer.Filter.CheckAccept (_msg)) {
					await SendReplyAsync (_msg.MsgId, _msg.Seq, AcceptMsgType.Reject);
					return;
				}

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendReplyAsync (_msg.MsgId, _msg.Seq, AcceptMsgType.Accept);

				// 存档
				if (_cntmsg.Type.IsStore ()) {
					// TODO 存数据库
				}

				// 发送给接收者
				var _receivers = _cntmsg switch {
					v0_PrivateMsg _priv_msg => new List<long> { _priv_msg.RecverUserId },
					v0_TopicMsg _topic_msg => await ImServer.GetTopicUserIds (_topic_msg.TopicId, _topic_msg.Type.IsOnlineOnly ()),
					_ => new List<long> (),
				};
				foreach (var _receiver in _receivers)
					await ImServer.SendAsync (_receiver, _msg);
			}
		}

		public async Task SendReplyAsync (long _msgid, long _seq, AcceptMsgType _type) {
			await SendAndLoggingAsync (new v0_ReplyMsg { MsgId = _msgid, Seq = _seq, Type = _type });
		}
	}
}
