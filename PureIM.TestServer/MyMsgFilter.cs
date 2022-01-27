using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.TestServer {
	class MyMsgFilter: IMessageFilter {
		public async Task<long?> Connect (byte[] _data) {
			await Task.Yield ();
			var auth_str = Encoding.UTF8.GetString (_data);
			if (auth_str.StartsWith ("[forcelogin]")) {
				if (long.TryParse (auth_str[12..], out var _userid)) {
					return _userid;
				}
			}
			return null;
		}

		public async Task<bool> CheckAccept (IImMsg _msg) {
			await Task.Yield ();
			return true;
		}
	}
}
