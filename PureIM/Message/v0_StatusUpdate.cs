using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PureIM.Message {
	public enum StatusMsgType {
		Accept				= 0x1,  // 发送成功
		RecverAccept		= 0x2,  // 对方已接收
		RecverReaded		= 0x3,  // 对方已读
		SenderKnowAccept	= 0x4,
		SenderKnowReaded	= 0x5,
		//Rejection		= 0x4,	// 拒收
	}



	[MessagePackObject]
	public class v0_StatusUpdate: IImMsg {
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long Seq { get; set; }
		[Key (2)] public List<(long LastMsgId, long SenderUserId, long RecverUserId)> PrivateMsgs { get; set; }
		[Key (3)] public List<(long LastMsgId, long TopicId)> TopicMsgs { get; set; }
		[Key (4)] public StatusMsgType StatusMsgType { get; set; }



		public string SerilizeLog () => $"v0_StatusUpdateMsg {{ MsgId={MsgId}, StatusMsgType={StatusMsgType} }}";
	}
}
