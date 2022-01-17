using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM.ImStructs.Message {
	[Flags]
	public enum MsgType {
		Send	= 0x0,      // 发送信息
		Reply	= 0x1,      // 确认已接收
		Online	= 0x2,      // 在线消息，客户端未在线即销毁
		Store	= 0x4,      // 存档消息，存档进数据库
		Text	= 0x100,    // 文本信息
		Binary	= 0x200,    // 二进制信息（音频文件、图片文件、视频文件、文档文件等）
		Stream	= 0x400,    // 流信息
		Command	= 0x800,    // 命令信息
	}

	public interface IImMessage {
		public async Task<byte[]> SerilizeAsync () {
			var _ms = new MemoryStream ();
			_ms.WriteByte (this switch {
				ReplyMessage => 0,
				PrivateMessage => 1,
				TopicMessage => 2,
				BroadcastMessage => 3,
				_ => 255,
			});
			await MessagePackSerializer.SerializeAsync (_ms, this);
			return _ms.ToArray ();
		}

		public static async Task<IImMessage> FromBytes (Stream _s) {
			try {
				var _buf = new byte [1] { 0 };
				var _source = new CancellationTokenSource (TimeSpan.FromSeconds (1));
				await _s.ReadAsync (_buf, 0, _buf.Length, _source.Token);
				return _buf[0] switch {
					0 => await MessagePackSerializer.DeserializeAsync<ReplyMessage> (_s),
					1 => await MessagePackSerializer.DeserializeAsync<PrivateMessage> (_s),
					2 => await MessagePackSerializer.DeserializeAsync<TopicMessage> (_s),
					3 => await MessagePackSerializer.DeserializeAsync<BroadcastMessage> (_s),
					_ => null,
				};
			} catch (Exception) {
				return null;
			}
		}
	}
}
