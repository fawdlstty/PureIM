using FreeSql;
using PureIM.DataModel;
using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.Tools {
	class DataStorer {
		public static IFreeSql _sql = new FreeSqlBuilder ()
				//.UseConnectionString (DataType.MySql, @"Server=47.114.2.248;Port=3306;Database=mosquitto;User=mosquitto;Password=mosquitto")
				//.UseConnectionString (DataType.SqlServer, @"Server=(localdb)\\mssqllocaldb;Database=PureIM_db;Trusted_Connection=True")
				.UseConnectionString (DataType.Sqlite, @"Data Source=PureIM;Mode=Memory;Cache=Shared")
				.UseAutoSyncStructure (true) //自动同步实体结构到数据库，FreeSql不会扫描程序集，只有CRUD时才会生成表。
				.Build ();

		public static int YearMonth { get => DateTime.Now.Year * 100 + DateTime.Now.Month; }



		/// <summary>
		/// 获取所有用户id
		/// </summary>
		/// <returns></returns>
		public static async Task<List<long>> GetAllUserIdsAsync () => await _sql.Select<tb_ImClient> ().ToListAsync (a => a.Id);



		/// <summary>
		/// 存档私聊信息
		/// </summary>
		/// <param name="_pvmsg"></param>
		/// <returns></returns>
		public static async Task<long> StoreMsgAsync (v0_PrivateMsg _pvmsg) {
			var _tb_pvmsg = new tb_ImPrivateMsg { MsgId = _pvmsg.MsgId, Seq = _pvmsg.Seq, SenderUserId = _pvmsg.SenderUserId, RecverUserId = _pvmsg.RecverUserId, Type = _pvmsg.Type, SendTime = _pvmsg.SendTime, Data = _pvmsg.Data };
			long _id = await _sql.Insert (_tb_pvmsg).ExecuteIdentityAsync ();
			var _tb_pvmsg_status = await _sql.Select<tb_ImPrivateMsgStatus> ().Where (t => t.YearMonth == YearMonth && t.SenderUserId == _pvmsg.SenderUserId && t.RecverUserId == _pvmsg.RecverUserId).FirstAsync ();
			if (_tb_pvmsg_status != null) {
				_tb_pvmsg_status.MonthLastMsgId = _id;
				await _sql.Update<tb_ImPrivateMsgStatus> (_tb_pvmsg_status.Id).Set (t => t.MonthLastMsgId, _id).ExecuteAffrowsAsync ();
			} else {
				_tb_pvmsg_status = new tb_ImPrivateMsgStatus { Id = 0, YearMonth = YearMonth, SenderUserId = _pvmsg.SenderUserId, RecverUserId = _pvmsg.RecverUserId, MonthLastMsgId = _id, RecverRecvMsgId = -1, SenderRecvMsgId = -1, RecverReadMsgId = -1, SenderReadMsgId = -1 };
				await _sql.Insert (_tb_pvmsg_status).ExecuteAffrowsAsync ();
			}
			return _id;
		}



		/// <summary>
		/// 存档主题信息
		/// </summary>
		/// <param name="_tmsg"></param>
		/// <returns></returns>
		public static async Task<long> StoreMsgAsync (v0_TopicMsg _tmsg) {
			var _tb_tmsg = new tb_ImTopicMsg { MsgId = _tmsg.MsgId, Seq = _tmsg.Seq, SenderUserId = _tmsg.SenderUserId, TopicId = _tmsg.TopicId, Type = _tmsg.Type, SendTime = _tmsg.SendTime, Data = _tmsg.Data };
			long _id = await _sql.Insert (_tb_tmsg).ExecuteIdentityAsync ();
			await _sql.Update<tb_ImTopicMsgStatus> ().Set (t => t.MonthLastMsgId, _id).Where (t => t.YearMonth == YearMonth && t.TopicId == _tmsg.TopicId).ExecuteAffrowsAsync ();
			return _id;
		}



		/// <summary>
		/// 修改私聊消息状态
		/// </summary>
		/// <param name="_sender_id"></param>
		/// <param name="_recver_id"></param>
		/// <param name="_msgid"></param>
		/// <param name="_status_type"></param>
		/// <returns></returns>
		public static async Task UpdatePrivateStatusAsync (long _sender_id, long _recver_id, long _msgid, StatusMsgType _status_type) {

		}



		/// <summary>
		/// 修改主题消息状态
		/// </summary>
		/// <param name="_topic_id"></param>
		/// <param name="_msgid"></param>
		/// <param name="_status_type"></param>
		/// <returns></returns>
		public static async Task UpdatePrivateStatusAsync (long _topic_id, long _msgid, StatusMsgType _status_type) {

		}
	}
}
