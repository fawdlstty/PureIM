using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.ImStructs.Message {
	public enum MsgType {
		Text		= 0x0,	// 文本信息
		Binary		= 0x1,	// 二进制信息（音频文件、图片文件、视频文件、文档文件等）
		Stream		= 0x2,	// 流信息
		Command		= 0x3,	// 命令信息
	}



	[MessagePack.Union (0, typeof (v0_ReplyMsg))]
	[MessagePack.Union (1, typeof (v0_PrivateMsg))]
	[MessagePack.Union (2, typeof (v0_TopicMsg))]
	[MessagePack.Union (3, typeof (v0_BroadcastMsg))]
	[MessagePack.Union (3, typeof (v0_StatusUpdateMsg))]
	public interface IImMsg {
		public long MsgId { get; set; }



		private static MessagePackSerializerOptions Lz4Compress = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

		public byte[] Serilize () {
			var _ms = new MemoryStream ();
			_ms.WriteByte (0);
			_ms.WriteByte (0);
			_ms.WriteByte (0);
			_ms.WriteByte (0);
			MessagePackSerializer.Serialize (_ms, this, Lz4Compress);
			var _bytes = _ms.ToArray ();
			BitConverter.GetBytes (_bytes.Length - 4).CopyTo (_bytes, 0);
			return _bytes;
		}
		public static IImMsg FromBytes (byte[] _raw_data) => MessagePackSerializer.Deserialize<IImMsg> (_raw_data, Lz4Compress);
	}
}
