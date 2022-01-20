using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.DataModel {
	[Index ("uk_{tablename}", "YearMonth,SenderUserId,RecverUserId", IsUnique = true)]
	public class tb_ImPrivateMsgStatus {
		[Column (IsIdentity = true, IsPrimary = true)]
		public long Id { get; set; }
		public int YearMonth { get; set; }
		public long SenderUserId { get; set; }
		public long RecverUserId { get; set; }

		// 当月最后一条消息的ID
		public long MonthLastMsgId { get; set; }

		// 接收者实际已接收的最后一条消息ID
		public long RecverRecvMsgId { get; set; }

		// 发送者实际已了解到用户已接收的最后一条消息ID
		public long SenderRecvMsgId { get; set; }

		// 接收者实际已读的最后一条消息ID
		public long RecverReadMsgId { get; set; }

		// 发送者实际已了解到用户已读的最后一条消息ID
		public long SenderReadMsgId { get; set; }
	}
}
