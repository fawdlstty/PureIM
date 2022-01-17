using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM {
	public static class ExtensionMethods {
		//private static async Task MySendTextAsync (this WebSocket _ws, long _uid, string _msg) {
		//	await Log.WriteAsync ($"websocket send to uid[{_uid}] data[{_msg}]");
		//	var _bin = Encoding.UTF8.GetBytes (_msg).ToList ();
		//	var _source = new CancellationTokenSource (TimeSpan.FromSeconds (2));
		//	await _ws.SendAsync (_bin, WebSocketMessageType.Text, true, _source.Token);
		//}

		//public static async Task MySendSuccessAsync (this WebSocket _ws, long _uid, MsgType _type, long _seq, object _content = null) {
		//	JObject _o = JObject.FromObject (new { result = true, type = _type.ToString (), seq = _seq });
		//	if (_content != null)
		//		_o["content"] = _content.ToJson ();
		//	await _ws.MySendTextAsync (_uid, _o.ToString ());
		//}

		//public static async Task MySendSuccessAsync (this WebSocket _ws, long _uid, MsgType _type, long _seq, long _msg_id, object _content) {
		//	JObject _o = JObject.FromObject (new { result = true, type = _type.ToString (), seq = _seq, msg_id = _msg_id, content = _content.ToJson () });
		//	await _ws.MySendTextAsync (_uid, _o.ToString ());
		//}

		//public static async Task MySendFailureAsync (this WebSocket _ws, long _uid, MsgType _type, long _seq, string _reason) {
		//	JObject _o = JObject.FromObject (new { result = false, type = _type.ToString (), seq = _seq, reason = _reason });
		//	await _ws.MySendTextAsync (_uid, _o.ToString ());
		//}

		public static async Task MyCloseAsync (this WebSocket _ws) {
			var _source = new CancellationTokenSource (TimeSpan.FromSeconds (1));
			await _ws.CloseAsync (WebSocketCloseStatus.NormalClosure, null, _source.Token);
		}

		public static JToken ToJson (this object _o) {
			JToken _content = _o switch {
				null => null,
				string _s => _s,
				bool _b => _b,
				sbyte _b => _b,
				byte _b => _b,
				short _s => _s,
				ushort _us => _us,
				int _i => _i,
				uint _ui => _ui,
				long _l => _l,
				ulong _ul => _ul,
				JObject _jo => _jo,
				JArray _ja => _ja,
				JToken _jt => _jt,
				_ when _o.GetType ().IsAssignableTo (typeof (IEnumerable)) => JArray.FromObject (_o),
				_ => JObject.FromObject (_o),
			};
			return _content;
		}
	}
}
