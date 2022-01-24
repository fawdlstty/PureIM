using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM {
	public class Config {
		public static long GetNewId () => DateTime.Now.Ticks;

		/// <summary>在线消息缓存时间，默认1min</summary>
		public static TimeSpan OnlineMessageCache = TimeSpan.FromMinutes (1);

		/// <summary>消息发送超时，默认10s</summary>
		public static TimeSpan MessageTimeout = TimeSpan.FromSeconds (10);

		/// <summary>消息重复发送间隔，默认10s</summary>
		public static TimeSpan MessageResend = TimeSpan.FromSeconds (10);

		/// <summary>控制台输出+源码文件名:行数</summary>
		public static bool DebugLog = true;

		/// <summary>接收者是否需要在用户已读后回复已读</summary>
		public static bool EnableReceiverReaded = true;

		/// <summary>接收者收到信息后是否通知发布者</summary>
		public static bool EnableReceiverReceivedNotify = true;

		/// <summary>接收者已读信息后是否通知发布者</summary>
		public static bool EnableReceiverReadedNotify = true;
	}
}
