using Fawdlstty.PureIM.DataModel;
using Fawdlstty.PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.ImStructs {
	public class ImManager {
		public static async void Add (ImClient _client) {
			lock (Clients)
				Clients.Add (_client.UserId, _client);
		}
		public static async void Remove (long _userid) {
			lock (Clients) {
				if (Clients.ContainsKey (_userid))
					Clients.Remove (_userid);
			}
		}

		public static async Task SendAsync (IImMessage _msg) {
			//// 获取目标用户列表
			//var _userids = new HashSet<long> ();
			//if (_msg.ToUserId > 0 && _msg.TopicName == "") {
			//	// 点对点信息
			//	_userids.Add (_msg.ToUserId);
			//} else if (_msg.ToUserId == 0 && _msg.TopicName != "") {
			//	// 主题信息
			//	using (var _locker = await SubscriptionsMutex.LockAsync ()) {
			//		if (Subscriptions.ContainsKey (_msg.TopicName)) {
			//			foreach (var _sub_userid in Subscriptions[_msg.TopicName])
			//				_userids.Add (_sub_userid);
			//		}
			//	}
			//} else if (_msg.ToUserId == 0 && _msg.TopicName == "") {
			//	// 广播信息
			//	using (var _locker = await ClientsMutex.LockAsync ()) {
			//		_userids = (from p in Clients select p.Key).ToHashSet ();
			//	}
			//} else {
			//	await Log.WriteAsync ("func[SendAsync] argument error. Ignore.");
			//	return;
			//}
			if (_msg is P2pMessage _p2p) {

			}

			// TODO 存数据库

			// 发送
			foreach (var _userid in _userids) {
				var _client = await GetClient (_userid);
				await _client?.SendAsync (_msg);
			}
		}

		public static async Task<ImClient> GetClient (long _userid) {
			using (var _locker = await ClientsMutex.LockAsync ()) {
				if (Clients.ContainsKey (_userid))
					return Clients[_userid];
			}
			return null;
		}



		// 客户端列表
		private static Dictionary<long, ImClient> Clients = new Dictionary<long, ImClient> ();
		private static AsyncLocker ClientsMutex = new AsyncLocker ();

		// 订阅列表
		private static Dictionary<string, HashSet<long>> Subscriptions = new Dictionary<string, HashSet<long>> ();
		private static AsyncLocker SubscriptionsMutex = new AsyncLocker ();
	}
}
