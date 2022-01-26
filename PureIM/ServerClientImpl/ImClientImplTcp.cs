using PureIM.Message;
using PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.ServerClientImpl {
	class ImClientImplTcp: IImClientImpl {
		public OnlineStatus Status { get; private set; } = OnlineStatus.Online;
		private TcpClient Client { get; set; }
		private NetworkStream CStream { get; set; }
		private static byte[] PingBuf { get; } = new byte[4] { 0, 0, 0, 0 };

		public DateTime LastConnTime { get => Status.IsOnline () ? DateTime.Now : _last_conn_time; }
		private DateTime _last_conn_time = DateTime.Now;
		public Func<byte[], Task> OnRecvCbAsync { get; set; } = null;
		public Func<Task> OnCloseAsync { get; set; } = null;
		public string UserDesp {
			get => _user_desp == "" ? ClientAddr : _user_desp;
			set => _user_desp = value;
		}
		private string _user_desp = "";
		public string ClientAddr { get => _client_addr; }
		private string _client_addr = "";



		public ImClientImplTcp (TcpClient _client) {
			Client = _client;
			CStream = _client.GetStream ();
			_client_addr = $"client[{Client.Client.RemoteEndPoint}]";
		}

		public async Task RunOnceAsync () {
			byte[] _buf = new byte [4];
			if (!Client.Connected)
				return;
			int _readed = 0;
			while (_readed < _buf.Length)
				_readed += await CStream.ReadAsync (_buf, _readed, _buf.Length - _readed);
			//
			int _pkg_len = BitConverter.ToInt32 (_buf);
			if (_pkg_len == 0) {
				await SendAsync (PingBuf);
				return;
			}
			//await Log.WriteAsync ($"read {_pkg_len} bytes from {UserDesp}");
			_buf = _pkg_len != _buf.Length ? new byte[_pkg_len] : _buf;
			_readed = 0;
			while (_readed < _pkg_len)
				_readed += await CStream.ReadAsync (_buf, _readed, _pkg_len - _readed);
			//
			while (OnRecvCbAsync == null)
				await Task.Delay (TimeSpan.FromMilliseconds (1));
			//await Log.WriteAsync ($"process packet from {UserDesp}");
			await OnRecvCbAsync (_buf);
		}

		public async Task RunAsync () {
			try {
				while (Client.Connected)
					await RunOnceAsync ();
			} catch (Exception) {
			}
			_last_conn_time = DateTime.Now;
			Status = OnlineStatus.TempOffline;
		}

		public async Task<bool> SendAsync (byte[] _bytes) {
			try {
				if (Status.IsOnline ()) {
					await CStream.WriteAsync (_bytes);
					return true;
				}
			} catch (Exception) {
			}
			return false;
		}

		public async Task<bool> SendReplyAndLoggingAsync (long _seq, string _data) {
			var _reply = v0_ReplyMsg.Failure (_seq, _data);
			await Log.WriteAsync ($"server -> {ClientAddr}: {_reply.SerilizeLog ()}");
			return await SendAsync (_reply.Serilize ());
		}

		public async Task CloseAsync () {
			CStream.Close ();
			await CStream.DisposeAsync ();
			Client.Close ();
			await CStream.DisposeAsync ();
		}
	}
}
