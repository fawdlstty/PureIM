using Fawdlstty.PureIM.DataModel;
using Fawdlstty.PureIM.ImStructs.Message;
using Fawdlstty.PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.ImStructs {
	public class ImManager {
		public static async Task Add (ImClient _client) {
			using (var _locker = await ClientsMutex.LockAsync ())
				Clients.Add (_client.UserId, _client);
		}
		public static async Task Remove (long _userid) {
			using (var _locker = await ClientsMutex.LockAsync ()) {
				if (Clients.ContainsKey (_userid))
					Clients.Remove (_userid);
			}
		}

		public static async Task SendAsync (long _userid, IImMsg _msg) {
			var _client = await GetClientForceAsync (_userid);
		}

		//public static async Task SendAsync (IImMsg _msg) {
		//	// 获取目标用户列表
		//	var _userids = new HashSet<long> ();
		//	if (_msg is v0_PrivateMsg _priv_msg) {
		//		// 点对点信息
		//		_userids.Add (_priv_msg.ToUserId);
		//	} else if (_msg is v0_TopicMsg _topic_msg) {
		//		// 主题信息
		//		using (var _locker = await SubscriptionsMutex.LockAsync ()) {
		//			if (Subscriptions.ContainsKey (_topic_msg.TopicName)) {
		//				foreach (var _sub_userid in Subscriptions[_topic_msg.TopicName])
		//					_userids.Add (_sub_userid);
		//			}
		//		}
		//	} else if (_msg is v0_BroadcastMsg) {
		//		// 广播信息
		//		using (var _locker = await ClientsMutex.LockAsync ()) {
		//			_userids = (from p in Clients select p.Key).ToHashSet ();
		//		}
		//	} else {
		//		await Log.WriteAsync ("func[SendAsync] argument error. Ignore.");
		//		return;
		//	}

		//	// 检查用户是否存在，不存在则创建用户消息缓存

		//	// 发送
		//	foreach (var _userid in _userids) {
		//		var _client = await GetClient (_userid);
		//		await _client?.SendAsync (_msg);
		//	}
		//}

		public static async Task<ImClient> GetClientForceAsync (long _userid) {
			using (var _locker = await ClientsMutex.LockAsync ()) {
				if (Clients.ContainsKey (_userid))
					return Clients[_userid];
			}
			var _client = new ImClient { WS = null, UserId = _userid };
			await _client.Process ();
		}



		// 客户端列表
		private static Dictionary<long, ImClient> Clients = new Dictionary<long, ImClient> ();
		private static AsyncLocker ClientsMutex = new AsyncLocker ();

		// 订阅列表
		private static Dictionary<string, HashSet<long>> Subscriptions = new Dictionary<string, HashSet<long>> ();
		private static AsyncLocker SubscriptionsMutex = new AsyncLocker ();
	}
}
