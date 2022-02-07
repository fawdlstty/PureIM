using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PureIM.Message {
	public enum MsgStructType {
		PrivateMsg,
		TopicMsg,
	}

	public enum StatusMsgType {
		Accept          = 0x1,  // 发送成功
		DestAccept      = 0x2,  // 对方已接收
		DestReaded      = 0x3,  // 对方已读
		//Rejection		= 0x4,	// 拒收
	}



	[MessagePackObject]
	public class v0_StatusUpdateMsg: IImMsg {
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long Seq { get; set; }
		[Key (2)] public MsgStructType MsgStructType { get; set; }
		[Key (3)] public StatusMsgType StatusMsgType { get; set; }



		public string SerilizeLog () => $"v0_StatusUpdateMsg {{ MsgId={MsgId}, StatusMsgType={StatusMsgType} }}";
	}
}
