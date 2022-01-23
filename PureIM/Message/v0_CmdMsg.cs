using MessagePack;
using PureIM.Tools;
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



		public static byte[] LoginForce () {
			var _cmd_reply_msg = new v0_CmdMsg { MsgId = 0, MsgIdShadow = 0, CmdType = MsgCmdType.Auth, Option = "login", Attachment = Encoding.UTF8.GetBytes ("[forcelogin]1") };
			return _cmd_reply_msg.Serilize ();
		}
	}
}
