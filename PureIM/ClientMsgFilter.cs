using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM {
	public class ClientMsgFilter {
		public static bool CheckAccept (v0_CmdMsg _msg) {
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
