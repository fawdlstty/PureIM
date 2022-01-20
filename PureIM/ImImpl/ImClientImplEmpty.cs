using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PureIM.ImImpl {
	class ImClientImplEmpty: IImClientImpl {
		public bool IsConnecting => false;

		public DateTime LastConnTime => throw new NotImplementedException ();

		public Task<bool> WriteAsync (byte[] _bytes) => throw new NotImplementedException ();
	}
}
