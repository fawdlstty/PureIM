using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PureIM.Message {
	public interface IImContentMsg: IImMsg {
		public long SenderUserId { get; set; }
		public MsgType Type { get; set; }
		public DateTime SendTime { get; set; }
		public byte[] Data { get; set; }
	}
}
