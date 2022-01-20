using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM {
	public class Config {
		public static long GetNewId () => DateTime.Now.Ticks;

		/// <summary>在线消息缓存时间</summary>
		public static TimeSpan OnlineMessageCache = TimeSpan.FromMinutes (1);

		/// <summary>消息发送超时</summary>
		public static TimeSpan MessageTimeout = TimeSpan.FromSeconds (10);

		/// <summary>消息重复发送间隔</summary>
		public static TimeSpan MessageResend = TimeSpan.FromSeconds (10);
	}
}
