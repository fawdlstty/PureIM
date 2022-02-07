using FreeSql;
using PureIM.DataModel;
using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
		public static async Task<long> StoreMsgAsync (tb_ImPrivateMsg _pvmsg) {
			long _id = await _sql.Insert (_pvmsg).ExecuteIdentityAsync ();
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
		public static async Task<long> StoreMsgAsync (tb_ImTopicMsg _tmsg) {
			var _tb_tmsg = new tb_ImTopicMsg { MsgId = _tmsg.MsgId, Seq = _tmsg.Seq, SenderUserId = _tmsg.SenderUserId, TopicId = _tmsg.TopicId, Type = _tmsg.Type, SendTime = _tmsg.SendTime, Data = _tmsg.Data };
			long _id = await _sql.Insert (_tb_tmsg).ExecuteIdentityAsync ();
			await _sql.Update<tb_ImTopicMsgStatus> ().Set (t => t.MonthLastMsgId, _id).Where (t => t.YearMonth == YearMonth && t.TopicId == _tmsg.TopicId).ExecuteAffrowsAsync ();
			return _id;
		}



		/// <summary>
		/// 修改私聊消息状态
		/// </summary>
		/// <param name="_private_msg_ids"></param>
		/// <param name="_status_msg_type"></param>
		/// <returns></returns>
		public static async Task UpdateStatusAsync (List<(long LastMsgId, long SenderUserId, long RecverUserId)> _private_msg_ids, StatusMsgType _status_msg_type) {

		}



		/// <summary>
		/// 修改主题消息状态
		/// </summary>
		/// <param name="_private_msg_ids"></param>
		/// <returns></returns>
		public static async Task UpdateStatusAsync (List<(long LastMsgId, long TopicId)> _private_msg_ids) {

		}



		#region TODO 以后优化操作合并
		//private static AsyncLocker StorePrivMutex = new AsyncLocker ();
		//private static List<v0_PrivateMsg> StorePrivMsg { get; set; } = new List<v0_PrivateMsg> ();
		//private static List<Action<long>> StorePrivCb { get; set; } = new List<Action<long>> ();

		///// <summary>
		///// 存档私聊消息
		///// </summary>
		///// <param name="_msg"></param>
		///// <returns></returns>
		//public static async Task<long> StoreMsg (v0_PrivateMsg _msg) {
		//	var _sema = new SemaphoreSlim (0);
		//	long _msgid = -1;
		//	using (var _locker = await StorePrivMutex.LockAsync ()) {
		//		StorePrivMsg.Add (_msg);
		//		StorePrivCb.Add (_msgid1 => { _msgid = _msgid1; _sema.Release (); });
		//	}
		//	await _sema.WaitAsync ();
		//	return _msgid;
		//}
		//#endregion

		//#region 主题信息
		//private static AsyncLocker StoreTopicMutex = new AsyncLocker ();
		//private static List<tb_ImTopicMsg> StoreTopicMsg { get; set; } = new List<tb_ImTopicMsg> ();
		//private static List<Action<long>> StoreTopicCb { get; set; } = new List<Action<long>> ();

		///// <summary>
		///// 存档主题信息
		///// </summary>
		///// <param name="_tmsg"></param>
		///// <returns></returns>
		//public static async Task<long> StoreMsgAsync (v0_TopicMsg _tmsg) {
		//	var _tb_tmsg = new tb_ImTopicMsg { MsgId = _tmsg.MsgId, Seq = _tmsg.Seq, SenderUserId = _tmsg.SenderUserId, TopicId = _tmsg.TopicId, Type = _tmsg.Type, SendTime = _tmsg.SendTime, Data = _tmsg.Data };
		//	long _id = await _sql.Insert (_tb_tmsg).ExecuteIdentityAsync ();
		//	await _sql.Update<tb_ImTopicMsgStatus> ().Set (t => t.MonthLastMsgId, _id).Where (t => t.YearMonth == YearMonth && t.TopicId == _tmsg.TopicId).ExecuteAffrowsAsync ();
		//	return _id;
		//}



		///// <summary>
		///// 修改私聊消息状态
		///// </summary>
		///// <param name="_sender_id"></param>
		///// <param name="_recver_id"></param>
		///// <param name="_msgid"></param>
		///// <param name="_status_type"></param>
		///// <returns></returns>
		//public static async Task UpdatePrivateStatusAsync (long _sender_id, long _recver_id, long _msgid, StatusMsgType _status_type) {

		//}



		///// <summary>
		///// 修改主题消息状态
		///// </summary>
		///// <param name="_topic_id"></param>
		///// <param name="_msgid"></param>
		///// <param name="_status_type"></param>
		///// <returns></returns>
		//public static async Task UpdatePrivateStatusAsync (long _topic_id, long _msgid, StatusMsgType _status_type) {

		//}
		///// <param name="_msg"></param>
		///// <returns></returns>
		//public static async Task<long> StoreMsg (v0_TopicMsg _msg) {
		//	var _sema = new SemaphoreSlim (0);
		//	long _msgid = -1;
		//	using (var _locker = await StoreTopicMutex.LockAsync ()) {
		//		StoreTopicMsg.Add (_msg);
		//		StoreTopicCb.Add (_msgid1 => { _msgid = _msgid1; _sema.Release (); });
		//	}
		//	await _sema.WaitAsync ();
		//	return _msgid;
		//}
		//#endregion

		//private static Task s_StoreThread = Task.Run (async () => {
		//	var _store_priv_msg = new List<v0_PrivateMsg> ();
		//	var _store_priv_cb = new List<Action<long>> ();
		//	var _store_topic_msg = new List<v0_TopicMsg> ();
		//	var _store_topic_cb = new List<Action<long>> ();

		//	var _dt = DateTime.Now;
		//	_dt = new DateTime (_dt.Year, _dt.Month, _dt.Day, _dt.Hour, _dt.Minute, _dt.Second);
		//	while (true) {
		//		var _dt1 = _dt.AddSeconds (1);
		//		var _span = _dt1 - DateTime.Now;
		//		if (_span.TotalMilliseconds > 0)
		//			await Task.Delay (_span);
		//		_dt = _dt1;

		//		// 存档私聊信息
		//		using (var _locker = await StorePrivMutex.LockAsync ()) {
		//			(_store_priv_msg, StorePrivMsg) = (StorePrivMsg, _store_priv_msg);
		//			(_store_priv_cb, StorePrivCb) = (StorePrivCb, _store_priv_cb);
		//		}
		//		if (_store_priv_msg.Any ()) {
		//			// TODO
		//			_store_priv_msg.Clear ();
		//			_store_priv_cb.Clear ();
		//		}

		//		// 存档主题信息
		//		using (var _locker = await StoreTopicMutex.LockAsync ()) {
		//			(_store_topic_msg, StoreTopicMsg) = (StoreTopicMsg, _store_topic_msg);
		//			(_store_topic_cb, StoreTopicCb) = (StoreTopicCb, _store_topic_cb);
		//		}
		//		if (_store_topic_msg.Any ()) {
		//			// TODO
		//			_store_topic_msg.Clear ();
		//			_store_topic_cb.Clear ();
		//		}
		//	}
		//});
		#endregion
	}
}
