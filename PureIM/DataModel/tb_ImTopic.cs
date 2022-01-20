using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.DataModel {
	public class tb_ImTopic {
		[Column (IsIdentity = true, IsPrimary = true)]
		public long Id { get; set; }
		public string Name { get; set; }
	}
}
