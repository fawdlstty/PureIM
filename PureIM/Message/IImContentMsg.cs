using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PureIM.Message {
	public interface IImContentMsg {
		public long MsgId { get; set; }
		public long MsgIdShadow { get; set; }
		public long FromUserId { get; set; }
		public MsgType Type { get; set; }
		public DateTime SendTime { get; set; }
		public byte[] Data { get; set; }
	}
}
