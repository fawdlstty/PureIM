using PureIM.ServerClientImpl;
using PureIM.Message;
using PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PureIM.DataModel;
using Newtonsoft.Json;

namespace PureIM {
	public enum OnlineStatus { Offline, Online, TempOffline }

	class ImServerClient {
		public long UserId { get; private set; } = -1;
		private IImClientImpl ClientImpl { get; set; } = ImClientImplNone.Inst;
		private long Seq { get; set; } = 0;
		public bool ToClear { get; set; } = false;
		public Func<Task> OnClearExit { get; set; } = null;

		// 发送状态缓存
		private List<(IImMsg _msg, DateTime _pstime)> SendCaches = new List<(IImMsg _msg, DateTime _pstime)> ();
		private AsyncLocker SendCachesMutex = new AsyncLocker ();

		// 接收状态缓存 （已接收的信息）
		private List<(IImMsg _msg, IImMsg _reply, DateTime _pstime)> RecvCaches = new List<(IImMsg _msg, IImMsg _reply, DateTime _pstime)> ();
		private static AsyncLocker RecvCachesMutex = new AsyncLocker ();



		/// <summary>
		/// 检查接收到的数据包在近一分钟是否重复，如重复则自动回复
		/// </summary>
		/// <param name="_msg"></param>
		/// <returns></returns>
		public async Task<bool> CheckRecvRepeatAsync (IImMsg _msg) {
			using (var _locker = await RecvCachesMutex.LockAsync ()) {
				foreach (var (_msg1, _reply, _pstime1) in RecvCaches) {
					if (_msg.Seq == _msg1.Seq) {
						if (_reply != null)
							await SendAndLoggingAsync (_reply, false);
						return true;
					}
				}
			}
			return false;
		}

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
				var _ret = v0_CmdMsg.Disconnect ("disconnect due to duplicate");
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
			// 对应处理
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
			} else if (_msg is v0_StatusUpdate _stupd_msg) {
				// 判定是否重复
				if (await CheckRecvRepeatAsync (_msg))
					return;

				// 错误校验
				if (_stupd_msg.StatusMsgType == StatusMsgType.Accept)
					return;
				bool _is_recver = (_stupd_msg.StatusMsgType == StatusMsgType.RecverAccept || _stupd_msg.StatusMsgType == StatusMsgType.RecverReaded);
				if (_stupd_msg.PrivateMsgs?.Count > 0) {
					foreach (var _pv_items in _stupd_msg.PrivateMsgs) {
						if ((_is_recver && _pv_items.RecverUserId != UserId) || ((!_is_recver) && _pv_items.SenderUserId != UserId)) {
							// TODO 回复错误 用户ID不匹配
							await SendFailureReplyAsync (-1, );
							return;
						}
					}
				}
				if (_stupd_msg.TopicMsgs?.Count > 0) {
					if (_stupd_msg.TopicMsgs.Count > 100) {
						// TODO 回复错误 主题设置状态数量不允许超过1000
						return;
					}
					if (!_is_recver) {
						// TODO 回复错误 主题消息无法设置发送者已知悉
						return;
					}
					using (var _locker = await ImServer.SubscriptionsMutex.LockAsync ()) {
						foreach (var _topic_items in _stupd_msg.TopicMsgs) {
							if (ImServer.Subscriptions.TryGetValue (_topic_items.TopicId, out var _uids)) {
								if (!_uids.Contains (UserId)) {
									// TODO 回复错误 用户ID不匹配
									return;
								}
							} else {
								// TODO 回复错误 主题消息无法设置发送者已知悉
								return;
							}
						}
					}
				}

				// 存档
				if (_stupd_msg.PrivateMsgs?.Count > 0) {
					await DataStorer.UpdateStatusAsync (_stupd_msg.PrivateMsgs, _stupd_msg.StatusMsgType);
					//if (_stupd_msg.StatusMsgType == StatusMsgType.RecverAccept || _stupd_msg.StatusMsgType == StatusMsgType.RecverReaded) {
					//	bool _recv = _stupd_msg.StatusMsgType == StatusMsgType.RecverAccept;
					//	foreach (var (_msgid, _sender_userid, _recver_userid) in _stupd_msg.PrivateMsgs)
					//		await ImServer.SendAsync (_sender_userid, new v0_StatusUpdate { MsgId = -1, Seq = -1 });
					//}
				}
				if (_stupd_msg.TopicMsgs?.Count > 0)
					await DataStorer.UpdateStatusAsync (_stupd_msg.TopicMsgs, _stupd_msg.StatusMsgType);

				// 写入接收缓存
				using (var _locker = await RecvCachesMutex.LockAsync ())
					RecvCaches.Add ((_msg, null, DateTime.Now));
			} else if (_msg is IImContentMsg _cntmsg) {
				// 判定是否重复
				if (await CheckRecvRepeatAsync (_msg))
					return;

				_cntmsg.MsgId = 0;
				_cntmsg.SendTime = DateTime.Now;

				if (_msg is tb_ImPrivateMsg _pmsg) {
					var (_check, _reason) = await ImServer.Filter.CheckAccept (UserId, _pmsg);
					if (!_check) {
						await SendFailureReplyAsync (_cntmsg.MsgId, _msg.Seq, _reason);
						return;
					}

					if (_pmsg.Type.IsStore ())
						_pmsg.MsgId = await DataStorer.StoreMsgAsync (_pmsg);
				} else if (_msg is tb_ImTopicMsg _tmsg) {
					var (_check, _reason) = await ImServer.Filter.CheckAccept (UserId, _tmsg);
					if (!_check) {
						await SendFailureReplyAsync (_cntmsg.MsgId, _msg.Seq, _reason);
						return;
					}

					if (_tmsg.Type.IsStore ())
						_cntmsg.MsgId = await DataStorer.StoreMsgAsync (_tmsg);
				}

				// 回复发送者
				await SendSuccessReplyAsync (_cntmsg.MsgId, _msg.Seq);

				// 发送给接收者
				var _receivers = _cntmsg switch {
					tb_ImPrivateMsg _priv_msg => new List<long> { _priv_msg.RecverUserId },
					tb_ImTopicMsg _topic_msg => await ImServer.GetTopicUserIds (_topic_msg.TopicId, _topic_msg.Type.IsOnlineOnly ()),
					_ => new List<long> (),
				};
				foreach (var _receiver in _receivers)
					await ImServer.SendAsync (_receiver, _msg);
			}
		}

		public async Task SendSuccessReplyAsync (long _msgid, long _seq) {
			await SendAndLoggingAsync (v0_ReplyMsg.Success ($"accept seq[{_seq}] new_msgid[{_msgid}]"));
		}

		public async Task SendFailureReplyAsync (string _reason) {
			await SendAndLoggingAsync (v0_ReplyMsg.Failure (_reason));
		}
	}
}
