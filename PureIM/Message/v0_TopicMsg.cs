using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PureIM.Message {
	[MessagePackObject]
	public class v0_TopicMsg: IImContentMsg {
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long MsgIdShadow { get; set; }
		[Key (2)] public string TopicName { get; set; }
		[Key (3)] public long SenderUserId { get; set; }
		[Key (4)] public MsgType Type { get; set; }
		[Key (5)] public DateTime SendTime { get; set; }
		[Key (6)] public byte[] Data { get; set; }
	}
}
