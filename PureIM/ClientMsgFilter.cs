using PureIM.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM {
	public class ClientMsgFilter {
		public static Func<IImMsg, bool> CheckAccept = (_msg) => true;
	}
}
