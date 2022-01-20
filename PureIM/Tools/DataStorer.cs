using FreeSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.Tools {
	class DataStorer {
		public static IFreeSql _sql = new FreeSqlBuilder ()
				//.UseConnectionString (DataType.MySql, @"Server=47.114.2.248;Port=3306;Database=mosquitto;User=mosquitto;Password=mosquitto")
				.UseConnectionString (DataType.SqlServer, @"Server=(localdb)\\mssqllocaldb;Database=PureIM_db;Trusted_Connection=True")
				.UseAutoSyncStructure (true) //自动同步实体结构到数据库，FreeSql不会扫描程序集，只有CRUD时才会生成表。
				.Build ();

		public static async Task<List<long>> GetAllUserIds () {

		}
	}
}
