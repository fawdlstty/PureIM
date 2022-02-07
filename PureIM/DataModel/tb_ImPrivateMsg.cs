using FreeSql.DataAnnotations;
using MessagePack;
using PureIM.Message;
using PureIM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.DataModel {
	[MessagePackObject]
	[Index ("uk_{tablename}", "Seq,SenderUserId", IsUnique = true)]
	public class tb_ImPrivateMsg: IImContentMsg {
		[Column (IsIdentity = true, IsPrimary = true)]
		[Key (0)] public long MsgId { get; set; }
		[Key (1)] public long Seq { get; set; }
		[Key (2)] public long SenderUserId { get; set; }
		[Key (3)] public long RecverUserId { get; set; }
		[Key (4)] public MsgType Type { get; set; }
		[Key (5)] public DateTime SendTime { get; set; }
		//[Column (DbType = "longblob")]
		[Key (6)] public byte[] Data { get; set; }



		public string SerilizeLog () {
			var _ntype = Type & (MsgType) 0xf;
			var _data_str = _ntype switch {
				MsgType.Text => Encoding.UTF8.GetString (Data),
				MsgType.Command => Encoding.UTF8.GetString (Data),
				_ when Data == null => "(null)",
				_ => "binary data...",
			};
			return $"v0_PrivateMsg {{ MsgId={MsgId}, Seq={Seq}, SenderUserId={SenderUserId}, RecverUserId={RecverUserId}, Type={Type.GetDesp ()}, SendTime={SendTime}, Data={_data_str} }}";
		}
	}
}
