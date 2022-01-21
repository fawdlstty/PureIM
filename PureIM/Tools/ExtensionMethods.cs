using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.Tools {
	static class ExtensionMethods {
		public static bool IsOnline (this OnlineStatus _status) => _status == OnlineStatus.Online;
		public static bool IsTempOffline (this OnlineStatus _status) => _status == OnlineStatus.TempOffline;
		public static bool IsOffline (this OnlineStatus _status) => _status == OnlineStatus.Offline;
		public static bool IsOnlineOnly (this MsgType _type) => (_type & MsgType.OnlineOnly) != 0;
		public static bool IsStore (this MsgType _type) => (_type & MsgType.Store) != 0;
	}
}
