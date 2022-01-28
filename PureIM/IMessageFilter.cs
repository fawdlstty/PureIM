using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM {
	public interface IMessageFilter {
		public Task<long?> Connect (byte[] _data);

		public Task<bool> CheckAccept (long _userid, v0_PrivateMsg _msg);

		public Task<bool> CheckAccept (long _userid, v0_TopicMsg _msg);
	}
}
