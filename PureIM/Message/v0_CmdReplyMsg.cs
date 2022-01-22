using MessagePack;
using PureIM.Tools;
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
		[Key (4)] public byte[] Attachment { get; set; }



		public static byte[] Failure (string _reason) {
			var _cmd_reply_msg = new v0_CmdReplyMsg { MsgId = 0, MsgIdShadow = 0, CmdReplyType = MsgCmdReplyType.Failure, Info = _reason, Attachment = null };
			return _cmd_reply_msg.Serilize ();
		}
	}
}
