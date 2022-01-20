using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.ImImpl {
	class ImClientImplTcp: IImClientImpl {
		public TcpClient Client { get; private set; }
		public NetworkStream CStream { get; private set; }
		private Func<byte[], Task> OnRecvCbAsync { get; set; }

		public bool IsConnecting { get; private set; } = true;
		public DateTime LastConnTime { get => IsConnecting ? DateTime.Now : _last_conn_time; }
		private DateTime _last_conn_time = DateTime.Now;



		public ImClientImplTcp (TcpClient _client, Func<byte[], Task> _on_recv_cb_async) {
			Client = _client;
			CStream = _client.GetStream ();
			OnRecvCbAsync = _on_recv_cb_async;
			_ = Task.Run (async () => {
				try {
					byte[] _len_buf = new byte [4], _buf = new byte [4];
					while (true) {
						var _readed = 0;
						while (_readed < _len_buf.Length)
							_readed += await CStream.ReadAsync (_len_buf, _readed, _len_buf.Length - _readed);
						//
						var _pkg_len = BitConverter.ToInt32 (_len_buf);
						if (_pkg_len == 0)
							continue;
						_buf = _pkg_len != _buf.Length ? new byte[_pkg_len] : _buf;
						_readed = 0;
						while (_readed < _pkg_len)
							_readed += await CStream.ReadAsync (_buf, _readed, _pkg_len - _readed);
						//
						await OnRecvCbAsync (_buf);
					}
				} catch (Exception) {
				}
				_last_conn_time = DateTime.Now;
				IsConnecting = false;
			});
		}

		public async Task<bool> WriteAsync (byte[] _bytes) {
			try {
				if (IsConnecting) {
					await CStream.WriteAsync (_bytes);
					return true;
				}
			} catch (Exception) {
			}
			return false;
		}
	}
}
