using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.Message {
	public enum MsgCmdReplyType {
		Failure		= 0,
		Success		= 1,
	}

	[MessagePackObject]
	public class v0_CmdReplyMsg: IImMsg {
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long MsgIdShadow { get; set; }
		[Key (2)] public MsgCmdReplyType CmdReplyType { get; set; }
		[Key (3)] public string Info { get; set; }
		[Key (4)] public byte[] Argument { get; set; }
	}
}
