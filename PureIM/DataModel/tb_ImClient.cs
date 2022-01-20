using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.DataModel {
	public enum UserStatus { Normal, Forbidden, }

	public class tb_ImClient {
		[Column (IsIdentity = true, IsPrimary = true)]
		public long Id { get; set; }
		public string Name { get; set; }
		public DateTime JoinTime { get; set; }
		public DateTime LastUsingTime { get; set; }
		public UserStatus Status { get; set; }
	}
}
