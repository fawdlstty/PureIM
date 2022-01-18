using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.ImStructs.Message {
	[MessagePackObject]
	public class v0_BroadcastMsg: IImMsg {
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long MsgIdShadow { get; set; }
		[Key (2)] public long FromUserId { get; set; }
		[Key (3)] public MsgType Type { get; set; }
		[Key (4)] public DateTime SendTime { get; set; }
		[Key (5)] public byte[] Data { get; set; }
	}
}
