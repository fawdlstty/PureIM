using Fawdlstty.PureIM.ImStructs.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM {
	public class ClientMsgFilter {
		public static bool CheckAccept (v0_BroadcastMsg _msg) {
			return true;
		}

		public static bool CheckAccept (v0_PrivateMsg _msg) {
			return true;
		}

		public static bool CheckAccept (v0_StatusUpdateMsg _msg) {
			return true;
		}

		public static bool CheckAccept (v0_TopicMsg _msg) {
			return true;
		}
	}
}
