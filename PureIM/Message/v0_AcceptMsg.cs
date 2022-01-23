﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PureIM.Message {
	public enum AcceptMsgType {
		Accept		= 0x1,	// 发送成功
		DestAccept	= 0x2,	// 对方已接收
		DestReaded	= 0x3,	// 对方已读
		Reject		= 0x4,	// 服务端拒收
	}



	[MessagePackObject]
	public class v0_AcceptMsg: IImMsg {
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long MsgIdShadow { get; set; }
		[Key (2)] public AcceptMsgType Type { get; set; }
	}
}