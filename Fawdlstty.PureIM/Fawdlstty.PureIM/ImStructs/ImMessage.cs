using Fawdlstty.PureIM.Tools;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.ImStructs {
	[Flags]
	public enum MsgType {
		Send    = 0x0,      // 发送信息
		Reply   = 0x1,      // 确认已接收
		Online  = 0x2,      // 在线消息，客户端未在线即销毁
		Store   = 0x4,      // 存档消息，存档进数据库
		Text    = 0x100,    // 文本信息
		Binary  = 0x200,    // 二进制信息（音频文件、图片文件、视频文件、文档文件等）
		Stream  = 0x400,    // 流信息
		Command = 0x800,    // 命令信息
	}

	[MessagePackObject]
	public class ImMessage {
		// 消息ID
		[Key (0)] public long		MsgId { init; get; }
		// 主题名称，不为空代表订阅消息，为空且FromUserId不为0代表私聊消息，反之则为广播消息
		[Key (1)] public string		TopicName { init; get; }
		// 发送者ID
		[Key (2)] public long		FromUserId { init; get; }
		// 接收者ID
		[Key (3)] public long		ToUserId { init; get; }
		// 消息类型
		[Key (4)] public MsgType	Type { init; get; }
		// 发送时间
		[Key (5)] public DateTime	SendTime { init; get; }
		// 消息内容
		[Key (6)] public byte[]		Data { init; get; }



		public byte[] ToBytes () => MessagePackSerializer.Serialize (this);
		public static ImMessage FromBytes (byte[] _data) => MessagePackSerializer.Deserialize<ImMessage> (_data);
	}
}
