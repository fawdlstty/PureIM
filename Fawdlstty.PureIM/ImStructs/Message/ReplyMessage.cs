using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.ImStructs.Message {
	[MessagePackObject]
	public class ReplyMessage: IImMessage {
		[Key (0)] public List<long> ReceiveMsgIds { get; set; }
	}
}
