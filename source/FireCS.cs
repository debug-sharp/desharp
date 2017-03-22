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
 *	Rewriten to current API by Tom Flidr <tomflidr@gmail.com>
 *	Thanks for "xuzhibin": http://www.cnblogs.com/xuzhibin/archive/2010/02/04/1664032.html
 * 
 * 
 *  All methods is possible to call dynamicly and staticly with fluent interface:
 *  *****************************************************************************
	
	// let's prepare some web table data to print it later:
	List<string[]> headersTable = new List<string[]> { new string[] { "Name", "Value" } };
	for (int i = 0; i < Request.Headers.Count; i++) {
		headersTable.Add(new string[2]{
			Request.Headers.GetKey(i), Request.Headers.Get(i)
		});
	}

	// to display everytime where FileCS.Xxx(...) was called:
	FireCS.LogCallStackInfo();

	// to display Exception full name, message, place where it happend and with all inner exceptions:
	try {
		throw new Exception(Custom msg:-)");
	} catch (Exception e) {
		FireCS.Exception(e);
	}

	// you can put inside just anything what is possible to use for: (new JavascriptSerializer()).Serialize(...);
	FireCS
		.Debug("debug message")
		.Log("log message")
		.Info("info message")
		.Warn("warn message")
		.Error("error message")
		.Log(new Dictionary<string, string>(){
			{"First Key", "First Value"},
			{"Second Key", "Second Value"},
		})
		.Debug(new List<string>(){
			"First",
			"Second",
		})
		.Table(
			String.Format("Http Request Data {0}", Request.Url),
			headersTable
		);

*/

namespace Desharp
{
    public class FireCS
	{
		internal const string SELF_FILENAME = "FireCS.cs";
		internal const string LOG = "LOG";
		internal const string DEBUG = "DEBUG";
		internal const string TRACE = "TRACE";
		internal const string INFO = "INFO";
		internal const string WARN = "WARN";
		internal const string ERROR = "ERROR";
		internal const string TABLE = "TABLE";
		private static Dictionary<long, FireCSLogger> _instances = new Dictionary<long, FireCSLogger>();
		internal static void CloseHeaders () {
			long crt = Tools.GetRequestId();
			if (FireCS._instances.ContainsKey(crt)) {
				FireCS._instances[crt].CloseHeaders();
				FireCS._instances.Remove(crt);
			}
		}
		public static FireCSLogger Current {
			get { 
				FireCSLogger r;
				long crt = Tools.GetRequestId();
				if (FireCS._instances.ContainsKey(crt)) {
					r = FireCS._instances[crt];
				} else {
					r = new FireCSLogger();
					FireCS._instances.Add(crt, r);
				}
				return r;
			}
		}
		public static FireCSLogger Enable(bool enable = true) {
			return FireCS.Current.Enable(enable);
		}
		public static FireCSLogger Disable(bool disable = true) {
			return FireCS.Current.Disable(disable);
		}
		public static FireCSLogger LogCallStackInfo(bool logCallStackInfo = true) {
			return FireCS.Current.LogCallStackInfo(logCallStackInfo);
		}
		public static FireCSLogger Log(object obj) {
			return FireCS.Current.Log(obj);
		}
		public static FireCSLogger Debug(object obj) {
			return FireCS.Current.Debug(obj);
		}
		public static FireCSLogger Trace (object obj) {
			return FireCS.Current.Trace(obj);
		}
		public static FireCSLogger Info(object obj) {
			return FireCS.Current.Info(obj);
		}
		public static FireCSLogger Warn(object obj) {
			return FireCS.Current.Warn(obj);
		}
		public static FireCSLogger Error(object obj) {
			return FireCS.Current.Error(obj);
		}
		public static FireCSLogger Table(string label, object obj) {
			return FireCS.Current.Table(label, obj);
		}
		public static FireCSLogger Exception(Exception exception) {
			return FireCS.Current.Exception(exception);
		}
	}
	public class FireCSLogger
	{
		private bool _enabled = true;
		private bool _logCallStackInfo = false;
		private bool _baseHeadersInitialized = false;
		private int _logCounter = 0;
		private JavaScriptSerializer _serializer;
		private static Dictionary<string, string> _baseHeaders;
        static FireCSLogger() {
			FireCSLogger._baseHeaders = new Dictionary<string, string>() {
				{"X-Wf-Protocol-1", "http://meta.wildfirehq.org/Protocol/JsonStream/0.2"},
				{"X-Wf-1-Plugin-1", "http://meta.firephp.org/Wildfire/Plugin/FirePHP/Library-FirePHPCore/0.3"},
				{"X-Wf-1-Structure-1", "http://meta.firephp.org/Wildfire/Structure/FirePHP/FirebugConsole/0.1"},
			};
        }
		public FireCSLogger() {
			this._serializer = new JavaScriptSerializer();
		}
		public FireCSLogger Enable(bool enable = true) {
            this._enabled = enable;
			return this;
        }
		public FireCSLogger Disable(bool disable = true) {
			this._enabled = !disable;
			return this;
		}
		public FireCSLogger LogCallStackInfo(bool logCallStackInfo = true) {
			this._logCallStackInfo = logCallStackInfo;
			return this;
		}
		public FireCSLogger Log(object obj) {
			return this.Log(FireCS.LOG, obj);
		}
		public FireCSLogger Debug (object obj) {
			return this.Log(FireCS.DEBUG, obj);
		}
		public FireCSLogger Trace (object obj) {
			return this.Log(FireCS.TRACE, obj);
		}
		public FireCSLogger Info(object obj) {
			return this.Log(FireCS.INFO, obj);
		}
		public FireCSLogger Warn(object obj) {
			return this.Log(FireCS.WARN, obj);
		}
		public FireCSLogger Error(object obj) {
			return this.Log(FireCS.ERROR, obj);
		}
		public FireCSLogger Log(string logType, object obj) {
			dynamic callStackInfo = this._getCallStackInfo();
			dynamic header = new {
				Type = logType,
				File = callStackInfo.File.Replace("\\", "/"),
				Line = callStackInfo.Line
			};
			return this._renderHeaders(new FireCSLog(logType, header, obj));
		}
		public FireCSLogger Exception(Exception exception) {
			dynamic callStackInfo = this._getCallStackInfo();
			dynamic header = null;
			dynamic defaultHeader = new {
				Type = FireCS.ERROR,
				File = callStackInfo.File.Replace("\\", "/"),
				Line = callStackInfo.Line
			};
			Dictionary<string, ExceptionToRender> exceptions = Completers.StackTrace.CompleteInnerExceptions(exception, true);
			Exception e;
			int i = 0;
			foreach (var item in exceptions) {
				e = item.Value.Exception;
				string content = e.GetType().FullName + ": " + e.Message;
				header = null;
				RenderingCollection preparedResult = Completers.StackTrace.RenderStackTraceForException(
					item.Value, false, false, i++
				);
				foreach (StackTraceItem stackTracesItem in preparedResult.AllStackTraces) {
					if (stackTracesItem.File.ToString().Length > 0) {
						header = new {
							Type = FireCS.ERROR,
							File = stackTracesItem.File.ToString(),
							Line = stackTracesItem.Line
						};
						break;
					}
				}
				if (header == null) header = defaultHeader;
				this._renderHeaders(new FireCSLog(FireCS.ERROR, header, content));
			}
			return this;
		}
		public FireCSLogger Table(string label, object obj) {
			dynamic callStackInfo = this._getCallStackInfo();
			dynamic header = new {
				Type = FireCS.TABLE,
				Label = label,
				File = callStackInfo.File.Replace("\\", "/"),
				Line = callStackInfo.Line
			};
			return this._renderHeaders(new FireCSLog(FireCS.TABLE, header, obj));
		}
		internal FireCSLogger CloseHeaders () {
			if (!this._enabled) return this;
			HttpContext context = HttpContext.Current;
			if (context != null) { // context could be null in unit testing threads
				if (this._logCounter > 0) {
					//this._logCounter++;
					context.Response.AppendHeader(
						"X-Wf-1-Index", this._logCounter.ToString()
					);
				}
			}
			return this;
		}
		private FireCSLogger _renderHeaders(FireCSLog log) {
			if (!this._enabled) return this;
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
		private void _appendBaseHeadersIfNecessary(HttpResponse response) {
			if (this._baseHeadersInitialized) return;
			foreach (KeyValuePair<string, string> keypair in FireCSLogger._baseHeaders) {
				response.AppendHeader(keypair.Key, keypair.Value);
			}
			this._baseHeadersInitialized = true;
		}
		private dynamic _getCallStackInfo() {
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
						if (fileName.LastIndexOf(FireCS.SELF_FILENAME) == fileName.Length - FireCS.SELF_FILENAME.Length) {
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
    internal class FireCSLog {
		internal string logType;
		internal object header;
		internal object content;
		internal FireCSLog (string logType, object header, object content) {
			this.logType = logType;
			this.header = header;
			this.content = content;
		}
    }
}