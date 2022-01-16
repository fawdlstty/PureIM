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

		public static async Task<bool> SendAsync (long _userid, byte[] _data) {

		}

		public static async Task PublishTopicAsync (long _msgid, string _topic, byte[] _data) {
			var _userids = new HashSet<long> ();
			using (var _locker = await SubscriptionsMutex.LockAsync ()) {
				if (Subscriptions.ContainsKey (_topic))
					foreach (var _sub_userid in Subscriptions[_topic])
						_userids.Add (_sub_userid);
			}
			foreach (var _userid in _userids) {
				var _client = await GetClient (_userid);
				_client?.SendAsync (_msgid, _data)
			}
		}

		public static async Task<int> BroadcastAsync (long _msgid, byte[] _data) {

		}

		public static async Task<ImClient> GetClient (long _userid) {
			using var _locker = await ClientsMutex.LockAsync ();
			if (Clients.ContainsKey (_userid))
				return Clients[_userid];
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
