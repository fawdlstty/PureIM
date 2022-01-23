using PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.ImImpl {
	class ImClientImplTcp: IImClientImpl {
		public OnlineStatus Status { get; private set; } = OnlineStatus.Online;
		private TcpClient Client { get; set; }
		private NetworkStream CStream { get; set; }

		public DateTime LastConnTime { get => Status.IsOnline () ? DateTime.Now : _last_conn_time; }
		private DateTime _last_conn_time = DateTime.Now;
		public Func<byte[], Task> OnRecvCbAsync { get; set; } = null;



		public ImClientImplTcp (TcpClient _client) {
			Client = _client;
			CStream = _client.GetStream ();
		}

		public async Task RunAsync () {
			try {
				byte[] _len_buf = new byte [4], _buf = _len_buf;
				while (Client.Connected) {
					int _readed = 0;
					while (_readed < _len_buf.Length)
						_readed += await CStream.ReadAsync (_len_buf, _readed, _len_buf.Length - _readed);
					//
					int _pkg_len = BitConverter.ToInt32 (_len_buf);
					if (_pkg_len == 0)
						continue;
					await Log.WriteAsync ($"read {_pkg_len} bytes from client[{Client.Client.RemoteEndPoint}]");
					_buf = _pkg_len != _buf.Length ? new byte[_pkg_len] : _buf;
					_readed = 0;
					while (_readed < _pkg_len)
						_readed += await CStream.ReadAsync (_buf, _readed, _pkg_len - _readed);
					//
					while (OnRecvCbAsync == null)
						await Task.Delay (TimeSpan.FromMilliseconds (1));
					await Log.WriteAsync ($"process packet from client[{Client.Client.RemoteEndPoint}]");
					await OnRecvCbAsync (_buf);
				}
			} catch (Exception) {
			}
			_last_conn_time = DateTime.Now;
			Status = OnlineStatus.TempOffline;
			await Log.WriteAsync ($"connect temp offline");
		}

		public async Task<bool> WriteAsync (byte[] _bytes) {
			try {
				if (Status.IsOnline ()) {
					await CStream.WriteAsync (_bytes);
					return true;
				}
			} catch (Exception) {
			}
			return false;
		}

		public Task CloseAsync () {
			CStream.Close ();
			Client.Close ();
			return Task.CompletedTask;
		}
	}
}
