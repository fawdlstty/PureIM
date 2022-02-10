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
		[Key (1)] public long Seq { get; set; }
		[Key (2)] public MsgCmdType CmdType { get; set; }
		[Key (3)] public string Option { get; set; }
		[Key (4)] public byte[] Attachment { get; set; }



		public string SerilizeLog () {
			string _attach_str = Option switch {
				"connect" => Encoding.UTF8.GetString (Attachment),
				"disconnect" => Encoding.UTF8.GetString (Attachment),
				_ when Attachment == null => "(null)",
				_ => "binary data...",
			};
			return $"v0_CmdMsg {{ MsgId={MsgId}, Seq={Seq}, CmdType={CmdType}, Option={Option}, Attachment={_attach_str} }}";
		}

		public static v0_CmdMsg Disconnect (string _reason) {
			return new v0_CmdMsg { MsgId = -1, Seq = -1, CmdType = MsgCmdType.Auth, Option = "disconnect", Attachment = Encoding.UTF8.GetBytes (_reason) };
		}

		// 当前仅客户端测试使用
		public static byte[] LoginForce (long _seq, long _userid) {
			var _cmd_reply_msg = new v0_CmdMsg { MsgId = -1, Seq = _seq, CmdType = MsgCmdType.Auth, Option = "connect", Attachment = Encoding.UTF8.GetBytes ($"[forcelogin]{_userid}") };
			return _cmd_reply_msg.Serilize ();
		}
	}
}
