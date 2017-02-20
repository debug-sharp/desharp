using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Web;
using System.Diagnostics;
using Desharp.Core;

/**
 *  AJAX debug tool equivalent for original FirePHP 
 *	***********************************************
 *	
 *	Original PHP libraries:
 *	http://www.firephp.org/
 *  https://github.com/firephp/firephp-core
 *  https://code.google.com/p/firephp/

 *  Browser plugins:
 *  https://addons.mozilla.org/en/firefox/addon/firephp/
 *  https://chrome.google.com/webstore/detail/firephp4chrome/gpgbmonepdpnacijbbdijfbecmgoojma
 *  
 *	Reqriten into c# by Tom Flidr <tomflidr@gmail.com>
 *	Thanks for "xuzhibin": http://www.cnblogs.com/xuzhibin/archive/2010/02/04/1664032.html
 * 
 * 
 *  All methods is possible to call dynamicly and staticly with fluent interface:
 *  *****************************************************************************
 * 
 *  Calling by shorthand instance reference with fluent interface:
 *  --------------------------------------------------------------
 *	
 *	var fs = FireLogger.Get().LogCallStackInfo();
 *	fs.Debug("debug message").Log("log message").Info("info message").Warn("warn message").Error("error message");
 *	fs.Log(new Dictionary<string, string>(){
 *		{"First Key", "First Value"},
 *		{"Second Key", "Second Value"},
 *	}).Debug(new List<string>(){
 *		"First",
 *		"Second",
 *	});
 *	try {
 *	    throw new Exception("Test exception");
 *	} catch (Exception e) {
 *		fs.Exception(e);
 *	}
 *	string label = String.Format("Http Request Data {0}", Request.Url);
 *	var headers = Request.Headers;
 *	var tableHeaders = new string[headers.Count + 1][];
 *	tableHeaders[0] = new string[2] {"Name", "Value" };
 *	for (int i = 0; i < headers.Count; i++) {
 *	    tableHeaders[i + 1] = new string[2]{
 *			headers.GetKey(i),
 *			headers.Get(i)
 *		};
 *	}
 *	fs.Table(label, tableHeaders);
 *	
 *	
 *  Calling by static class name with fluent interface:
 *  ---------------------------------------------------
 * 
 *	FireLogger.Debug("debug message");
 *	FireLogger.Log("log message");
 *	FireLogger.Info("info message")
 *		.Warn("warn message")
 *		.Error("error message");
 *	FireLogger.Log(new Dictionary<string, string>(){
 *		{"First Key", "First Value"},
 *		{"Second Key", "Second Value"},
 *	}).Debug(new List<string>(){
 *		"First",
 *		"Second",
 *	}).Table(label, tableHeaders);
 *	try {
 *		throw new Exception("Test exception");
 *	} catch (Exception e) {
 *		FireLogger.Exception(e);
 *	}
 *	
*/

namespace Desharp
{
    public class FireLogger
	{
		public const string SELF_FILENAME = "FireLogger.cs";
		public const string LOG = "LOG";
		public const string DUMP = "DUMP";
		public const string TRACE = "TRACE";
		public const string INFO = "INFO";
		public const string WARN = "WARN";
		public const string ERROR = "ERROR";
		public const string TABLE = "TABLE";
		public const string EXCEPTION = "EXCEPTION";
		private static Dictionary<long, FireLoggerLogger> _instances = new Dictionary<long, FireLoggerLogger>();
		public static FireLoggerLogger Get() {
			FireLoggerLogger r;
			long crt = Tools.GetRequestId();
			if (FireLogger._instances.ContainsKey(crt)) {
				r = FireLogger._instances[crt];
			} else {
				r = new FireLoggerLogger();
				FireLogger._instances.Add(crt, r);
			}
			return r;
		}
		public static void CloseHeadersAndCleanUpRequestEnd () {
			long crt = Tools.GetRequestId();
			if (FireLogger._instances.ContainsKey(crt)) {
				FireLogger._instances[crt].CloseHeaders();
				FireLogger._instances.Remove(crt);
			}
		}
		public static FireLoggerLogger CloseHeaders () {
			return FireLogger.Get().CloseHeaders();
		}
		public static FireLoggerLogger Enable(bool enable = true) {
			return FireLogger.Get().Enable(enable);
		}
		public static FireLoggerLogger Disable(bool disable = true) {
			return FireLogger.Get().Disable(disable);
		}
		public static FireLoggerLogger LogCallStackInfo(bool logCallStackInfo = true) {
			return FireLogger.Get().LogCallStackInfo(logCallStackInfo);
		}
		public static FireLoggerLogger Log(object obj) {
			return FireLogger.Get().Log(obj);
		}
		public static FireLoggerLogger Dump(object obj) {
			return FireLogger.Get().Dump(obj);
		}
		public static FireLoggerLogger Trace (object obj) {
			return FireLogger.Get().Trace(obj);
		}
		public static FireLoggerLogger Info(object obj) {
			return FireLogger.Get().Info(obj);
		}
		public static FireLoggerLogger Warn(object obj) {
			return FireLogger.Get().Warn(obj);
		}
		public static FireLoggerLogger Error(object obj) {
			return FireLogger.Get().Error(obj);
		}
		public static FireLoggerLogger Table(string label, string[][] twoDimensionsStringArrayData) {
			return FireLogger.Get().Table(label, twoDimensionsStringArrayData);
		}
		public static FireLoggerLogger Exception(Exception exception) {
			return FireLogger.Get().Exception(exception);
		}
	}
	public class FireLoggerLogger
	{
		private bool _enabled = true;
		private bool _closed = false;
		private bool _logCallStackInfo = false;
		private bool _baseHeadersInitialized = false;
		private int _logCounter = 0;
		private JavaScriptSerializer _serializer;
		private static Dictionary<string, string> _baseHeaders;
        static FireLoggerLogger() {
			FireLoggerLogger._baseHeaders = new Dictionary<string, string>() {
				{"X-Wf-Protocol-1", "http://meta.wildfirehq.org/Protocol/JsonStream/0.2"},
				{"X-Wf-1-Plugin-1", "http://meta.firephp.org/Wildfire/Plugin/FirePHP/Library-FirePHPCore/0.3"},
				{"X-Wf-1-Structure-1", "http://meta.firephp.org/Wildfire/Structure/FirePHP/FirebugConsole/0.1"},
			};
        }
		public FireLoggerLogger() {
			this._serializer = new JavaScriptSerializer();
		}
		public FireLoggerLogger Enable(bool enable = true) {
            this._enabled = enable;
			return this;
        }
		public FireLoggerLogger Disable(bool disable = true) {
			this._enabled = !disable;
			return this;
		}
		public FireLoggerLogger LogCallStackInfo(bool logCallStackInfo = true) {
			this._logCallStackInfo = logCallStackInfo;
			return this;
		}
		public FireLoggerLogger Log(object obj) {
			return this.Log(FireLogger.LOG, obj);
		}
		public FireLoggerLogger Dump (object obj) {
			return this.Log(FireLogger.DUMP, obj);
		}
		public FireLoggerLogger Trace (object obj) {
			return this.Log(FireLogger.TRACE, obj);
		}
		public FireLoggerLogger Info(object obj) {
			return this.Log(FireLogger.INFO, obj);
		}
		public FireLoggerLogger Warn(object obj) {
			return this.Log(FireLogger.WARN, obj);
		}
		public FireLoggerLogger Error(object obj) {
			return this.Log(FireLogger.ERROR, obj);
		}
		public FireLoggerLogger Log(string logType, object obj) {
			dynamic callStackInfo = this._getCallStackInfo();
			dynamic header = new {
				Type = logType,
				File = callStackInfo.File,
				Line = callStackInfo.Line
			};
			return this._completeHeaders(new FireLoggerLog(logType, header, obj));
		}
		public FireLoggerLogger Exception(Exception exception)
		{
			StackTrace stackTrace;
			StackFrame callStack = new StackFrame(1, true);
			dynamic header = new {
				Type = FireLogger.EXCEPTION,
				File = exception.Source,
				Line = 1
			};
			int exceptionCount = 0;
			Exception currentException = exception;
			var traceList = new List<object>();
			while (currentException.InnerException != null) {
				stackTrace = new StackTrace(currentException, true);
				currentException = exception.InnerException;
				exceptionCount++;
				var trace = new {
					file = currentException.Source,
					line = currentException.Source,
					function = currentException.Message,
					args = new string[0]
				};
				traceList.Add(trace);
			}
			if (exceptionCount > 0) {
				var trace = new object[exceptionCount];
			}
			stackTrace = new StackTrace(exception, true);
			dynamic content = new {
				Class = "Exception",
				Message = exception.Message,
				File = stackTrace.GetFrame(0).GetFileName(),
				Line = stackTrace.GetFrame(0).GetFileLineNumber(),
				Type = "throw",
				Trace = traceList.ToArray()
			};
			return this._completeHeaders(new FireLoggerLog(FireLogger.EXCEPTION, header, content));
		}
		public FireLoggerLogger Table(string label, string[][] twoDimensionsStringArrayData)
		{
			dynamic callStackInfo = this._getCallStackInfo();
			dynamic header = new {
				Type = FireLogger.TABLE,
				Label = label,
				File = callStackInfo.File,
				Line = callStackInfo.Line
			};
			return this._completeHeaders(new FireLoggerLog(FireLogger.TABLE, header, twoDimensionsStringArrayData));
		}
		public FireLoggerLogger CloseHeaders () {
			if (!this._enabled || this._closed) return this;
			HttpContext context = HttpContext.Current;
			if (context != null) { // context could be null in unit testing threads
				if (this._logCounter > 0) {
					context.Response.AppendHeader(
						String.Format("X-Wf-1-Index", (this._logCounter + 1)),
						this._logCounter.ToString()
					);
				}
			}
			this._closed = true;
			return this;
		}
		private FireLoggerLogger _completeHeaders(FireLoggerLog log) {
			if (!this._enabled) return this;
			if (this._closed) throw new Exception("FireLogger: it is no longer possible to log anything, htt headers has been allready sent.");
			HttpContext context = HttpContext.Current;
			if (context != null) { // context could be null in unit testing threads
				this._appendBaseHeadersIfNecessary(context.Response);
				string jsonString = String.Format(
					"[{0}, {1}]",
					this._serializer.Serialize(log.header),
					this._serializer.Serialize(log.content)
				);
				context.Response.AppendHeader(
					String.Format("X-Wf-1-1-1-{0}", (this._logCounter + 1)),
					String.Format("{0}|{1}|",		jsonString.Length, jsonString)
				);
				this._logCounter += 1;
				if (this._logCounter > 9999) this._logCounter = 0;
			}
			return this;
		}
		private void _appendBaseHeadersIfNecessary(HttpResponse response)
		{
			if (this._baseHeadersInitialized) return;
			foreach (KeyValuePair<string, string> keypair in FireLoggerLogger._baseHeaders) {
				response.AppendHeader(keypair.Key, keypair.Value);
			}
			this._baseHeadersInitialized = true;
		}
		private dynamic _getCallStackInfo()
		{
			dynamic r = new {
				File = "", 
				Line = ""
			};
			if (this._logCallStackInfo) {
				StackTrace stackTrace = new StackTrace(1, true);
				string fileName;
				int line;
				foreach (var stackItem in stackTrace.GetFrames()) {
					fileName = stackItem.GetFileName();
					line = stackItem.GetFileLineNumber();
					if (fileName is string) {
						if (fileName.LastIndexOf(FireLogger.SELF_FILENAME) == fileName.Length - FireLogger.SELF_FILENAME.Length) {
							continue;
						}
					}
					r = new {
						File = fileName,
						Line = line
					};
					break;
				}
			}
			return r;
		}
	}
    public class FireLoggerLog
    {
		public string logType;
        public object header;
        public object content;
		public FireLoggerLog(string logType, object header, object content)
		{
			this.logType = logType;
			this.header = header;
			this.content = content;
		}
    }
}