using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PureIM.Tools {
	public class Log {
		private static Channel<string> s_chan = Channel.CreateUnbounded<string> ();
		private static bool s_run = false;



		public static async Task ProcessAsync () {
			var _path = Path.Combine (Environment.CurrentDirectory, "..", $"{Assembly.GetExecutingAssembly().GetName().Name}_log");
			if (!Config.DebugLog) {
				if (!Directory.Exists (_path))
					Directory.CreateDirectory (_path);
			}
			var _dt = DateTime.Now;
			_dt = new DateTime (_dt.Year, _dt.Month, _dt.Day, _dt.Hour, _dt.Minute, _dt.Second);
			while (true) {
				var _new_dt = _dt.AddSeconds (1);
				if (_new_dt > DateTime.Now)
					await Task.Delay (_new_dt - DateTime.Now);
				var _sb = new StringBuilder ();
				while (s_chan.Reader.TryRead (out string _s))
					_sb.Append (_s);
				if (_sb.Length > 0) {
					if (!Config.DebugLog) {
						var _file = Path.Combine (_path, $"{_dt:yyyyMMdd}.log");
						await File.AppendAllTextAsync (_file, _sb.ToString (), Encoding.UTF8);
					} else {
						Console.Write (_sb);
					}
				}
				_dt = _new_dt;
			}
		}

		public static async Task WriteAsync (string _msg, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0) {
			// , [CallerMemberName] string _func = ""
			_file = _file[(_file.LastIndexOfAny (new char[] { '/', '\\' }) + 1)..];
			await s_chan.Writer.WriteAsync ($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {_msg}{Environment.NewLine}");
			// Console.Write ($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{_file}:{_line}] {_msg}{Environment.NewLine}");
		}

		public static async Task WriteAsync (Exception _ex, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0) {
			await WriteAsync (_ex.ToString (), _file, _line);
		}
	}
}
