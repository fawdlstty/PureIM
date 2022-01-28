﻿using PureIM.ServerClientImpl;
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
		public bool ToClear { get; set; } = false;
		public Func<Task> OnClearExit { get; set; } = null;

		// 发送状态缓存
		private List<(IImMsg _msg, DateTime _pstime)> SendCaches = new List<(IImMsg _msg, DateTime _pstime)> ();
		private AsyncLocker SendCachesMutex = new AsyncLocker ();

		// 接收状态缓存 （已接收的信息）
		private List<(long _msgid, DateTime _pstime)> RecvCaches = new List<(long _msgid, DateTime _pstime)> ();
		private static AsyncLocker RecvCachesMutex = new AsyncLocker ();



		public ImServerClient (long _userid) {
			UserId = _userid;
			Task.Run (async () => {
				// 给任意链接提供一个至少 ClearTimeSpan 的生存时间，避免短时间闪断还得重新分配对象
				bool _first = true;

				while (true) {
					// 等待一下
					await Task.Delay (Config.ClearTimeSpan);

					// 清理超时信息
					if (SendCaches.Any ()) {
						using (var _locker = await SendCachesMutex.LockAsync ()) {
							while (SendCaches.Any () && SendCaches[0]._pstime <= DateTime.Now - Config.OnlineMessageCache)
								SendCaches.RemoveAt (0);
						}
					}
					if (RecvCaches.Any ()) {
						using (var _locker = await RecvCachesMutex.LockAsync ()) {
							while (RecvCaches.Any () && RecvCaches[0]._pstime <= DateTime.Now - Config.OnlineMessageCache)
								RecvCaches.RemoveAt (0);
						}
					}

					// 如果链接已断开，并且没有任何缓存信息，那么直接清理链接对象
					if (!(ClientImpl.Status.IsOnline () || SendCaches.Any () || RecvCaches.Any ())) {
						if (_first) {
							_first = false;
						} else {
							break;
						}
					}

					// ping一下
					if (ClientImpl.Status.IsOnline ())
						await ClientImpl.SendPingAsync ();
				}
				ToClear = true;
				ClientImpl = ImClientImplNone.Inst;
				if (OnClearExit != null)
					await OnClearExit ();
			});
		}

		public async Task SetClientImpl (IImClientImpl _client_impl, long _seq) {
			if (ClientImpl != ImClientImplNone.Inst)
				ClientImpl.UserDesp = $"dup user[{UserId}]";
			if (ClientImpl.Status.IsOnline ()) {
				var _ret = v0_CmdMsg.Disconnect (++Seq, "disconnect due to duplicate");
				await Log.WriteAsync ($"server -> {ClientImpl.UserDesp}: {_ret.SerilizeLog ()}");
				await ClientImpl.SendAsync (_ret.Serilize ());
				await ClientImpl.CloseAsync ();
			}
			bool _is_conn = ClientImpl.Status.IsOffline ();
			ClientImpl = _client_impl;
			ClientImpl.OnRecvCbAsync = OnRecvAsync;
			ClientImpl.UserDesp = $"user[{UserId}]";
			await Log.WriteAsync ($"{_client_impl.ClientAddr} {(_is_conn ? "connect" : "reconnect")} to {ClientImpl.UserDesp}.");

			// 新上线或者重新上线，缓存信息重新全发一遍
			var _to_sends = new List<IImMsg> ();
			if (SendCaches.Count > 0) {
				using (var _locker = await SendCachesMutex.LockAsync ()) {
					_to_sends.AddRange (from p in SendCaches select p._msg);
				}
			}
			await SendAndLoggingAsync (v0_ReplyMsg.Success (-1, _seq, "connect", null /*TODO 补充参数，比如userid或者配置*/), false);
			foreach (var _msg in _to_sends)
				_ = ClientImpl.SendAsync (_msg.Serilize ());
			if (_to_sends.Count > 0)
				await Log.WriteAsync ($"resend last {_to_sends.Count} msg to {ClientImpl.UserDesp}.");
		}

		/// <summary>
		/// 发送信息
		/// </summary>
		/// <param name="_msg"></param>
		/// <returns></returns>
		public async Task SendAsync (IImMsg _msg, bool _cache = true) {
			_ = ClientImpl.SendAsync (_msg.Serilize ());
			if (!_cache)
				return;
			using (var _locker = await SendCachesMutex.LockAsync ())
				SendCaches.Add ((_msg, DateTime.Now));
		}

		public async Task SendAndLoggingAsync (IImMsg _msg, bool _cache = true) {
			await Log.WriteAsync ($"server -> {ClientImpl.UserDesp}: {_msg.SerilizeLog ()}");
			await SendAsync (_msg, _cache);
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

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendAndLoggingAsync (v0_ReplyMsg.Success (_msg.MsgId, _msg.Seq, "accept"));

				// TODO 存档
			} else if (_msg is IImContentMsg _cntmsg) {
				// 判定是否重复
				bool _repeat = false;
				if (_repeat)
					return;

				// 检查
				bool _through_check = _msg switch {
					v0_PrivateMsg _pmsg => await ImServer.Filter.CheckAccept (UserId, _pmsg),
					v0_TopicMsg _tmsg => await ImServer.Filter.CheckAccept (UserId, _tmsg),
					_ => false,
				};
				if (!_through_check) {
					await SendFailureReplyAsync (_msg.MsgId, _msg.Seq, "didn't pass filter check");
					return;
				}

				// 回复发送者
				_msg.MsgId = _repeat ? 0 : Config.GetNewId (); // TODO 改为精确msgid
				await SendAndLoggingAsync (v0_ReplyMsg.Success (_msg.MsgId, _msg.Seq, "accept"));

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

		public async Task SendSuccessReplyAsync (long _msgid, long _seq) {
			await SendAndLoggingAsync (v0_ReplyMsg.Success (_msgid, _seq, "accept"));
		}

		public async Task SendFailureReplyAsync (long _msgid, long _seq, string _reason) {
			await SendAndLoggingAsync (v0_ReplyMsg.Failure (_msgid, _seq, _reason));
		}
	}
}
