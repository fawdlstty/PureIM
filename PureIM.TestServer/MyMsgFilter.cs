using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.TestServer {
	class MyMsgFilter: IMessageFilter {
		public async Task<long> Login (byte[] _data) {
			return (long) 1;
		}

		public async Task<bool> CheckAccept (IImMsg _msg) {
			return true;
		}
	}
}
