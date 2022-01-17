using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.DataModel {
	public enum SubType { Subscribe, Unsubscribe, }

	public class UserSubscription {
		public long Id { get; set; }
		public long UserId { get; set; }
		public string TopicName { get; set; }
		public DateTime JoinTime { get; set; }
		public DateTime LeaveTime { get; set; }
	}
}
