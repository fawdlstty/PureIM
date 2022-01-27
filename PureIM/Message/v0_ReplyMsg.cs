using MessagePack;
using PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.Message {
	public enum ReplyType {
		Failure		= 0,
		Success		= 1,
	}

	[MessagePackObject]
	public class v0_ReplyMsg: IImMsg {
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long Seq { get; set; }
		[Key (2)] public ReplyType ReplyType { get; set; }
		[Key (3)] public string Info { get; set; }
		[Key (4)] public byte[] Attachment { get; set; }



		public string SerilizeLog () {
			string _attach_str = Info switch {
				_ when Attachment == null => "(null)",
				_ => "binary data...",
			};
			return $"v0_ReplyMsg {{ MsgId={MsgId}, Seq={Seq}, ReplyType={ReplyType}, Info={Info}, Attachment={_attach_str} }}";
		}

		public static v0_ReplyMsg Success (long _msgid, long _seq, string _info, byte[] _attachment = null) {
			// TODO 附带配置参数
			return new v0_ReplyMsg { MsgId = _msgid, Seq = _seq, ReplyType = ReplyType.Success, Info = _info, Attachment = _attachment };
		}

		public static v0_ReplyMsg Failure (long _msgid, long _seq, string _info) {
			return new v0_ReplyMsg { MsgId = _msgid, Seq = _seq, ReplyType = ReplyType.Failure, Info = _info, Attachment = null };
		}
	}
}
