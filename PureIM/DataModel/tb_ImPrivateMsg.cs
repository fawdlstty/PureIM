using FreeSql.DataAnnotations;
using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.DataModel {
	[Index ("uk_{tablename}", "Seq,SenderUserId", IsUnique = true)]
	public class tb_ImPrivateMsg {
		[Column (IsIdentity = true, IsPrimary = true)]
		public long MsgId { get; set; }
		public long Seq { get; set; }
		public long SenderUserId { get; set; }
		public long RecverUserId { get; set; }
		public MsgType Type { get; set; }
		public DateTime SendTime { get; set; }
		//[Column (DbType = "longblob")]
		public byte[] Data { get; set; }
	}
}
