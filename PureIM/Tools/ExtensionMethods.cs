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
		public static byte[] Serilize (this IImMsg _msg) => IImMsg.Serilize (_msg);
		public static string GetDesp (this MsgType _type) {
			var _ntype = _type & (MsgType) 0xf;
			string _online = (_type & MsgType.OnlineOnly) != 0 ? "&online" : "";
			string _store = (_type & MsgType.Store) != 0 ? "&store" : "";
			return $"{_ntype}{_online}{_store}";
		}
	}
}
