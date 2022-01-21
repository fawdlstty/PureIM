using PureIM.ImImpl;
using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM {
	class ImClientGuest {
		public IImClientImpl ClientImpl { get; set; }

		public ImClientGuest (IImClientImpl _client_impl) {
			ClientImpl = _client_impl;
			ClientImpl.OnRecvCbAsync = OnRecvCbAsync;
		}

		/// <summary>
		/// 链接接收到的第一个数据包用于
		/// </summary>
		/// <param name="_data"></param>
		/// <returns></returns>
		public async Task OnRecvCbAsync (byte[] _data) {
			ClientImpl.OnRecvCbAsync = null;
			var _msg = IImMsg.FromBytes (_data);
			if (_msg is v0_CmdMsg _cmsg) {

			} else {
				ClientImpl.WriteAsync ();
			}
		}
	}
}
