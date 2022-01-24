﻿using MessagePack;
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
		[Key (1)] public long Seq { get; set; }
		[Key (2)] public MsgCmdReplyType CmdReplyType { get; set; }
		[Key (3)] public string Info { get; set; }
		[Key (4)] public byte[] Attachment { get; set; }



		public string SerilizeLog () {
			string _attach_str = Info switch {
				_ when Attachment == null => "(null)",
				_ => "binary data...",
			};
			return $"v0_CmdMsg {{ Seq={Seq}, CmdReplyType={CmdReplyType}, Info={Info}, Attachment={_attach_str} }}";
		}

		public static v0_CmdReplyMsg LoginSuccess (long _seq) {
			return new v0_CmdReplyMsg { MsgId = 0, Seq = _seq, CmdReplyType = MsgCmdReplyType.Success, Info = "login success", Attachment = null };
		}

		public static v0_CmdReplyMsg Failure (long _seq, string _reason) {
			return new v0_CmdReplyMsg { MsgId = 0, Seq = _seq, CmdReplyType = MsgCmdReplyType.Failure, Info = _reason, Attachment = null };
		}
	}
}
