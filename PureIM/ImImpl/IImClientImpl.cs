using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.ImImpl {
	public interface IImClientImpl {
		public OnlineStatus Status { get; }
		public DateTime LastConnTime { get; }
		public Func<byte[], Task> OnRecvCbAsync { get; set; }



		public Task<bool> WriteAsync (byte[] _bytes);
		public Task CloseAsync ();
	}
}
