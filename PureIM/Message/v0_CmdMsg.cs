using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.Message {
	public enum MsgCmdType {
		Add			= 0,
		Remove		= 1,
		Update		= 2,
		Query		= 3,
		Auth		= 4,
	}

	[MessagePackObject]
	public class v0_CmdMsg: IImMsg {
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long MsgIdShadow { get; set; }
		[Key (2)] public MsgCmdType CmdType { get; set; }
		[Key (3)] public string Option { get; set; }
		[Key (4)] public byte[] Attachment { get; set; }
	}
}
