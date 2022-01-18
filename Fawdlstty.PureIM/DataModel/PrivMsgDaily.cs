using Fawdlstty.PureIM.ImStructs.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.DataModel {
	public class PrivMsgDaily {
		public long MsgId { get; set; }
		public long MsgIdShadow { get; set; }
		public long FromUserId { get; set; }
		public long ToUserId { get; set; }
		public MsgType Type { get; set; }
		public DateTime SendTime { get; set; }
		public byte[] Data { get; set; }
		public string StrData { get; set; }
	}
}
