using PureIM.ImImpl;
using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM {
	class ImServerClientGuest {
		public IImClientImpl ClientImpl { get; set; }

		public ImServerClientGuest (IImClientImpl _client_impl) {
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
			Func<string, Task> _failure_cb = async _err => await ClientImpl.WriteAsync (v0_CmdReplyMsg.Failure (_err));

			try {
				var _msg = IImMsg.FromBytes (_data);
				if (_msg is v0_CmdMsg _cmsg) {
					if (_cmsg.CmdType == MsgCmdType.Auth && _cmsg.Option == "login" && _cmsg.Attachment != null) {
						var auth_str = Encoding.UTF8.GetString (_cmsg.Attachment);
						if (auth_str.StartsWith ("[forcelogin]")) {
							if (long.TryParse (auth_str[12..], out var _userid)) {
								var _client = ImServer.GetClientAsync (_userid);
							}
						}
					} else {
						await _failure_cb ("cmdtype is not `Auth` or option is not `login` or attachment is null");
					}
				} else {
					await _failure_cb ("msg is not `v0_CmdMsg`");
				}
			} catch (Exception _e) {
				await _failure_cb ($"exception: {_e.Message}");
			}
			await ClientImpl.CloseAsync ();
		}
	}
}
