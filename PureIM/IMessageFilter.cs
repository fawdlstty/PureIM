using PureIM.DataModel;
using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM {
	public interface IMessageFilter {
		public Task<long?> Connect (byte[] _data);

		public Task<(bool, string)> CheckAccept (long _userid, tb_ImPrivateMsg _msg);

		public Task<(bool, string)> CheckAccept (long _userid, tb_ImTopicMsg _msg);
	}
}
