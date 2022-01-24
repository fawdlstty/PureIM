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

		/// <summary>
		/// WS断开连接后赋值，用于确定超时后发送状态（此对象）的存在时间
		/// </summary>
		private DateTime ElapsedTime { get; set; } = DateTime.Now.Add (Config.OnlineMessageCache);

		// 发送状态缓存
		private List<(IImMsg _msg, DateTime _pstime)> SendCaches = new List<(IImMsg _msg, DateTime _pstime)> ();
		private AsyncLocker SendCachesMutex = new AsyncLocker ();
		private async Task<IImMsg> GetCacheItemAsync (DateTime _earlier_than) {
			using var _locker = await SendCachesMutex.LockAsync ();
			if (SendCaches.Count > 0 && SendCaches[0]._pstime <= _earlier_than) {
				var _ret = SendCaches[0]._msg;
				SendCaches.RemoveAt (0);
				return _ret;
			}
			return null;
		}

		private async Task<TimeSpan?> GetNextCacheTimeSpanAsync () {
			using (var _locker = await SendCachesMutex.LockAsync ()) {
				var _now = DateTime.Now;
				if (SendCaches.Count > 0) {
					// 避免上面几行运行太过缓慢导致等待负数时间，此处再判断一次
					if (SendCaches[0]._pstime > _now)
						return SendCaches[0]._pstime - _now;
				} else {
					return Config.MessageResend;
				}
			}
			return null;
		}

		// 接收状态缓存 （已接收的信息）
		private List<(long _msgid, DateTime _pstime)> RecvCaches = new List<(long _msgid, DateTime _pstime)> ();
		private static AsyncLocker RecvCachesMutex = new AsyncLocker ();



		public ImServerClient (long _userid) {
			UserId = _userid;
			Task.Run (async () => {
				await ImServer.Add (this);
				while (ElapsedTime >= DateTime.Now) {
					if (ClientImpl.Status.IsOnline ()) {
						// 如果在线

						// 延续超时时长
						ElapsedTime = DateTime.Now.Add (Config.OnlineMessageCache + Config.MessageResend);

						// 重发缓存信息
						IImMsg _msg;
						while ((_msg = await GetCacheItemAsync (DateTime.Now - Config.MessageResend)) != null)
							_ = SendAsync (_msg);
					} else {
						// 如果离线

						// 清理超时信息
						while ((await GetCacheItemAsync (DateTime.Now - Config.OnlineMessageCache)) != null);
					}

					// 等待下一个数据包处理时间
					var _wait = await GetNextCacheTimeSpanAsync ();
					if (_wait != null)
						await Task.Delay (_wait.Value);
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
			await SendAndLoggingAsync (v0_CmdReplyMsg.LoginSuccess (_seq));
		}

		/// <summary>
		/// 发送信息
		/// </summary>
		/// <param name="_msg"></param>
		/// <returns></returns>
		public async Task SendAsync (IImMsg _msg) {
			ElapsedTime = DateTime.Now.Add (Config.OnlineMessageCache);
			_ = ClientImpl.SendAsync (_msg.Serilize ());

			// 回复类信息不加入缓存，没送达直接抛弃
			if (_msg is v0_AcceptMsg || _msg is v0_CmdReplyMsg)
				return;
			using (var _locker = await SendCachesMutex.LockAsync ())
				SendCaches.Add ((_msg, DateTime.Now));
		}

		public async Task SendAndLoggingAsync (IImMsg _msg) {
			await Log.WriteAsync ($"server -> {ClientImpl.UserDesp}: {_msg.SerilizeLog ()}");
			await SendAsync (_msg);
		}

		public async Task OnRecvAsync (byte[] _data) {
			ElapsedTime = DateTime.Now.Add (Config.OnlineMessageCache);
			var _msg = IImMsg.FromBytes (_data);
			await Log.WriteAsync ($"{ClientImpl.UserDesp} -> server: {_msg.SerilizeLog ()}");
			if (_msg is v0_AcceptMsg || _msg is v0_CmdReplyMsg) {
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
			await SendAndLoggingAsync (new v0_AcceptMsg { MsgId = _msgid, Seq = _seq, Type = _type });
		}
	}
}
