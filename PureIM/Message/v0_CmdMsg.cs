using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.Message {
	[MessagePackObject]
	public class v0_CmdMsg: IImMsg {
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long MsgIdShadow { get; set; }
		[Key (2)] public MsgCmdType CmdType { get; set; }
		[Key (3)] public string OptionName { get; set; }
		[Key (4)] public byte[] Argument { get; set; }
	}
}
