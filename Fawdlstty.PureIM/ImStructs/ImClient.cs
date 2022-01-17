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
		public WebSocket WS { init; private get; }
		public long UserId { init; get; }

		/// <summary>
		/// WS断开连接后赋值，用于确定超时后发送状态（此对象）的存在时间
		/// </summary>
		private DateTime ElapsedTime { get; set; } = DateTime.Now.Add (Config.OnlineMessageCache);

		// 发送状态缓存
		private List<(IImMessage _msg, DateTime _pstime)> SendCaches = new List<(IImMessage _msg, DateTime _pstime)> ();
		private static AsyncLocker SendCachesMutex = new AsyncLocker ();

		// 接收状态缓存 （已接收的信息）
		private List<(long _msgid, DateTime _pstime)> RecvCaches = new List<(long _msgid, DateTime _pstime)> ();
		private static AsyncLocker RecvCachesMutex = new AsyncLocker ();



		public ImClient () {
			ImManager.Add (this);
			// 处理未送达的情况
			Task.Run (async () => {
				while (WS != null || ElapsedTime <= DateTime.Now) {
					DateTime _next_process_time;
					using (var _locker = await SendCachesMutex.LockAsync ()) {
						if (WS != null && SendCaches.Any () && SendCaches[0]._pstime <= DateTime.Now) {
							_ = ImplSendAsync (await SendCaches[0]._msg.SerilizeAsync ());
							SendCaches.Add ((SendCaches[0]._msg, DateTime.Now.Add (Config.MessageResend)));
							SendCaches.RemoveAt (0);
						}
						_next_process_time = SendCaches.Any () ? SendCaches[0]._pstime : DateTime.Now.Add (Config.MessageResend);
					}
					if (_next_process_time > DateTime.Now)
						await Task.Delay (_next_process_time - DateTime.Now);
				}
				ImManager.Remove (UserId);
			});
		}

		public async Task Process () {
			var _buf = new byte [1024 * 4];
			var _source = new CancellationTokenSource (TimeSpan.FromSeconds (10));
			while (!WS.CloseStatus.HasValue) {
				try {
					var _result = await WS.ReceiveAsync (_buf, CancellationToken.None);
					if (_result.MessageType != WebSocketMessageType.Text) { // _result.MessageType == WebSocketMessageType.Close
						await Log.WriteAsync ($"websocket receive type[{_result.MessageType}] msg. disconnect.");
						break;
					}
					_recv_data.AddRange (new ReadOnlySpan<byte> (_buf, 0, _result.Count).ToArray ());
					if (_result.EndOfMessage) {
						var _msg = Encoding.UTF8.GetString (_recv_data.ToArray ());
						_recv_data.Clear ();
						try {
							await _on_client_msg (UserId, WS, _msg, _ip);
						} catch (Exception _e) {
							await WS.MySendFailureAsync (UserId, MsgType.reply, -1, _e.Message);
						}
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
		public async Task SendAsync (IImMessage _msg) {
			_ = ImplSendAsync (await _msg.SerilizeAsync ());
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

		public async Task OnReadAsync (byte[] _data) {
			var _msg = IImMessage.FromBytes (_data);
			if (_msg == null)
				return;
			if ((_msg.Type & MsgType.Send) > 0) {
				using (var _locker = await RecvCachesMutex.LockAsync ()) {
					for (int i = 0; i < RecvCaches.Count; ++i) {
						if (RecvCaches[i]._msgid == _msg.MsgId)
							return;
					}
					RecvCaches.Add ((_msg.MsgId, DateTime.Now.Add (Config.OnlineMessageCache)));
				}
				// TODO 处理信息
			} else if ((_msg.Type & MsgType.Reply) > 0) {
				using (var _locker = await SendCachesMutex.LockAsync ()) {
					for (int i = 0; i < SendCaches.Count; ++i) {
						if (SendCaches[i]._msg.MsgId == _msg.MsgId) {
							SendCaches.RemoveAt (i);
							return;
						}
					}
				}
			} else {
				await Log.WriteAsync ("Unknown Message Status. Ignore.");
			}
		}
	}
}
