using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.ImImpl {
	public interface IImClientImpl {
		public bool IsConnecting { get; }
		public DateTime LastConnTime { get; }



		public Task<bool> WriteAsync (byte[] _bytes);
	}
}
