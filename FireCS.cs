using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Web;
using System.Diagnostics;
using Desharp.Core;

namespace Desharp {
	/// <summary>
	/// AJAX debug tool equivalent for original FirePHP
	/// </summary>
	/// <remarks>
	///	<b>Original PHP libraries:</b><br />
	///	<a href="http://www.firephp.org/">www.firephp.org</a><br />
	///	<a href="https://github.com/firephp/firephp-core">github.com/firephp/firephp-core</a><br />
	///	<a href="https://code.google.com/p/firephp/">code.google.com/p/firephp</a><br />
	/// <br />
	///	<b>Browser plugins:</b><br />
	///	<a href="https://addons.mozilla.org/en/firefox/addon/firephp/">Firefox - FirePHP plugin</a><br />
	///	<a href="https://chrome.google.com/webstore/detail/firephp4chrome/gpgbmonepdpnacijbbdijfbecmgoojma">Chrome - FirePHP4Chrome plugin</a><br />
	///	<br />
	///	Special thanks to <a href="http://www.cnblogs.com/xuzhibin/archive/2010/02/04/1664032.html">xuzhibin</a> to inspire me to rewite FirePHP again into C#.<br />
	///	Current API designed by Tom Flidr &lt;tomflidr@gmail.com&gt;
	/// </remarks>
	/// <example>
	/// All methods is possible to call dynamicly and staticly with fluent interface:
	/// <code>
	/// // let's prepare some web table data to print it later:
	/// List&lt;string[]&gt; headersTable = new List&lt;string[]&gt; { new string[] { "Name", "Value" } };
	/// for (int i = 0; i &lt; Request.Headers.Count; i++) {
	/// 	headersTable.Add(new string[2] {
	/// 		Request.Headers.GetKey(i), 
	/// 		Request.Headers.Get(i)
	/// 	});
	/// }
	/// 
	/// // to display everytime where Desharp.FireCS.Any(...) was called:
	/// Desharp.FireCS.LogCallStackInfo();
	/// 
	/// // to display Exception full name, message, place where it happend and with all inner exceptions:
	/// try {
	/// 	throw new Exception (Custom msg:-)");
	/// } catch (Exception e) {
	/// 	FireCS.Exception(e);
	/// }
	/// 
	/// // you can put inside FireCS call just anything, what is possible 
	/// // to run throught: (new JavascriptSerializer()).Serialize(anything);
	/// FireCS
	/// 	.Debug("debug message")
	/// 	.Log("log message")
	/// 	.Info("info message")
	/// 	.Warn("warn message")
	/// 	.Error("error message")
	/// 	.Log(new Dictionary&lt;string, string&gt;(){
	/// 		{ "First Key", "First Value" },
	/// 		{ "Second Key", "Second Value" },
	/// 	})
	/// 	.Debug (new List&lt;string&gt;(){
	/// 		"First", "Second",
	/// 	})
	/// 	.Table (
	/// 		String.Format("Http Request Data {0}", Request.Url),
	/// 		headersTable
	/// 	);
	/// </code>
	/// </example>
	public class FireCS {
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
		/// <summary>
		/// Get current request FireCS instance property, if doesn't exists under request id, it's created automaticly.
		/// </summary>
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
		/// <summary>
		/// Configure for current request all FireCS calls to display 
		/// source place where FireCS call has been used in browser console.
		/// </summary>
		/// <param name="logCallStackInfo">Set <c>true</c> to display sources places, where FireCS calls has been used, <c>false</c> otherwise.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public static FireCSLogger LogCallStackInfo(bool logCallStackInfo = true) {
			return FireCS.Current.LogCallStackInfo(logCallStackInfo);
		}
		/// <summary>
		/// Display value in browser console by classic <c>console.log(obj);</c> call.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public static FireCSLogger Log(object obj) {
			return FireCS.Current.Log(obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.debug(obj);</c> call.
		/// This console item is rendered as blue text only if value is only a text message, if value is structuralized value, it's displayed in classic way.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public static FireCSLogger Debug(object obj) {
			return FireCS.Current.Debug(obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.trace(obj);</c> call.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public static FireCSLogger Trace (object obj) {
			return FireCS.Current.Trace(obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.info(obj);</c> call.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public static FireCSLogger Info(object obj) {
			return FireCS.Current.Info(obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.warn(obj);</c> call.
		/// This console item is rendered as black text with light orange background and with orange icon with exclamation mark inside.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public static FireCSLogger Warn(object obj) {
			return FireCS.Current.Warn(obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.error(obj);</c> call.
		/// This console item is rendered as red text with light red background and with red icon with cross inside.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public static FireCSLogger Error(object obj) {
			return FireCS.Current.Error(obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.table(obj);</c> call.
		/// This console item is rendered as table with variable count of columns and rows - it depends by value.
		/// </summary>
		/// <param name="label">Table heading text.</param>
		/// <param name="obj">
		/// Any structuralized value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.
		/// Basicly it shoud by .NET Array with Arrays, but it should by any .NET IList, ICollection, IEnumerable, IDictionary, anything for foreach or for cycle in two levels.
		/// </param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public static FireCSLogger Table(string label, object obj) {
			return FireCS.Current.Table(label, obj);
		}
		/// <summary>
		/// Display .NET Exception in browser console by <c>console.error(obj);</c> call.
		/// This console item is rendered as red text with light red background and with red icon with cross inside.
		/// </summary>
		/// <param name="exception">.NET Exception to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public static FireCSLogger Exception(Exception exception) {
			return FireCS.Current.Exception(exception);
		}
	}
	/// <summary>
	/// Instancing class for static FireCS class, always returned as result of any FireCS static call,
	/// to have a possibility to call any other FireCS call in call chain, one method by one separated by dot.
	/// </summary>
	public class FireCSLogger {
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
		/// <summary>
		/// Every FireCSLogger instance has created in constructor it's own JavaScriptSerializer instance.
		/// </summary>
		public FireCSLogger () {
			this._serializer = new JavaScriptSerializer();
		}
		/// <summary>
		/// Configure for current request all FireCS calls to display 
		/// source place where FireCS call has been used in browser console.
		/// </summary>
		/// <param name="logCallStackInfo">Set <c>true</c> to display sources places, where FireCS calls has been used, <c>false</c> otherwise.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public FireCSLogger LogCallStackInfo(bool logCallStackInfo = true) {
			this._logCallStackInfo = logCallStackInfo;
			return this;
		}
		/// <summary>
		/// Display value in browser console by classic <c>console.log(obj);</c> call.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public FireCSLogger Log(object obj) {
			return this.log(FireCS.LOG, obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.debug(obj);</c> call.
		/// This console item is rendered as blue text only if value is only a text message, if value is structuralized value, it's displayed in classic way.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public FireCSLogger Debug (object obj) {
			return this.log(FireCS.DEBUG, obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.trace(obj);</c> call.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public FireCSLogger Trace (object obj) {
			return this.log(FireCS.TRACE, obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.info(obj);</c> call.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public FireCSLogger Info(object obj) {
			return this.log(FireCS.INFO, obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.warn(obj);</c> call.
		/// This console item is rendered as black text with light orange background and with orange icon with exclamation mark inside.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public FireCSLogger Warn(object obj) {
			return this.log(FireCS.WARN, obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.error(obj);</c> call.
		/// This console item is rendered as red text with light red background and with red icon with cross inside.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public FireCSLogger Error(object obj) {
			return this.log(FireCS.ERROR, obj);
		}
		/// <summary>
		/// Display value in browser console by <c>console.[log|info|debug|trace|warn|error|table](obj);</c> call.
		/// </summary>
		/// <param name="logType">Javascript browser console object function name to call on client, but in upper case.</param>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		protected FireCSLogger log (string logType, object obj) {
			if (Dispatcher.GetCurrent().Enabled != true) return this;
			dynamic callStackInfo = this._getCallStackInfo();
			dynamic header = new {
				Type = logType.ToUpper(),
				File = callStackInfo.File.Replace("\\", "/"),
				Line = callStackInfo.Line
			};
			return this._renderHeaders(new FireCSLog(logType, header, obj));
		}
		/// <summary>
		/// Display .NET Exception in browser console by <c>console.error(obj);</c> call.
		/// This console item is rendered as red text with light red background and with red icon with cross inside.
		/// </summary>
		/// <param name="exception">.NET Exception to display in client browser console.</param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public FireCSLogger Exception(Exception exception) {
			if (Dispatcher.GetCurrent().Enabled != true) return this;
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
		/// <summary>
		/// Display value in browser console by <c>console.table(obj);</c> call.
		/// This console item is rendered as table with variable count of columns and rows - it depends by value.
		/// </summary>
		/// <param name="label">Table heading text.</param>
		/// <param name="obj">
		/// Any structuralized value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.
		/// Basicly it shoud by .NET Array with Arrays, but it should by any .NET IList, ICollection, IEnumerable, IDictionary, anything for foreach or for cycle in two levels.
		/// </param>
		/// <returns>FireCSLogger instance is returned to call any other FireCS method in call chain.</returns>
		public FireCSLogger Table(string label, object obj) {
			if (Dispatcher.GetCurrent().Enabled != true) return this;
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
			Dispatcher dispatcher = Dispatcher.GetCurrent(false);
			if (dispatcher == null || dispatcher.Enabled != true) return this;
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