using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Web;
using System.Diagnostics;
using Desharp.Core;
using System.Runtime.InteropServices;

namespace Desharp {
    /// <summary>
    /// AJAX debug utility for original FirePHP browser extension
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
    ///	Current API designed by Tom Flidr &#60;tomflidr@gmail.com&#62;
    /// </remarks>
    /// <example>
    /// All methods is possible to call dynamicly and staticly with fluent interface:
    /// <code>
    /// // let's prepare some web table data to print it later:
    /// List&#60;string[]&#62; headersTable = new List&#60;string[]&#62; () { new string[] { "Name", "Value" } };
    /// for (int i = 0; i &#60; Request.Headers.Count; i++) {
    /// 	headersTable.Add(new string[2] {
    /// 		Request.Headers.GetKey(i), 
    /// 		Request.Headers.Get(i)
    /// 	});
    /// }
    /// 
    /// // to display everytime where Desharp.FireDump.Any(...) was called:
    /// Desharp.Debug.Fire.DumpCallStackInfo();
    /// 
    /// // to display Exception full name, message, place where it happend and with all inner exceptions:
    /// try {
    /// 	throw new Exception ("Custom msg:-)");
    /// } catch (Exception e) {
    /// 	Desharp.Debug.Fire.Exception(e);
    /// }
    /// 
    /// // you can put inside FireDump call just anything, what is possible 
    /// // to run throught: (new JavascriptSerializer()).Serialize(anything);
    /// Desharp.Debug.Fire
    /// 	.Debug("debug message")
    /// 	.Log("log message")
    /// 	.Info("info message")
    /// 	.Warn("warn message")
    /// 	.Error("error message")
    /// 	.Log(new Dictionary&#60;string, string&#62;(){
    /// 		{ "First Key", "First Value" },
    /// 		{ "Second Key", "Second Value" },
    /// 	})
    /// 	.Debug (new List&#60;string&#62;(){
    /// 		"First", "Second",
    /// 	})
    /// 	.Table (
    /// 		string.Format("Http Request Data {0}", Request.Url),
    /// 		headersTable
    /// 	);
    /// </code>
    /// </example>
	[ComVisible(true)]
    public class FireDump {
        internal const string SELF_FILENAME = "FireDump.cs";
        private bool _dumpCallStackInfo = false;
		private bool _baseHeadersInitialized = false;
		private int _logCounter = 0;
        private bool _enabled;
        private JavaScriptSerializer _serializer;
		private static Dictionary<string, string> _baseHeaders = new Dictionary<string, string>() {
            {"X-Wf-Protocol-1", "http://meta.wildfirehq.org/Protocol/JsonStream/0.2"},
            {"X-Wf-1-Plugin-1", "http://meta.firephp.org/Wildfire/Plugin/FirePHP/Library-FirePHPCore/0.3"},
            {"X-Wf-1-Structure-1", "http://meta.firephp.org/Wildfire/Structure/FirePHP/FirebugConsole/0.1"},
        };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="enabled"></param>
        public FireDump (bool enabled) {
            this._enabled = enabled;
            this._serializer = new JavaScriptSerializer();
        }
        /// <summary>
        /// Configure  all FireDump calls for current request 
        /// to display source code place in browser console,
        /// where these FireDump calls have been called.
        /// </summary>
        /// <param name="dumpCallStackInfo">Set <c>true</c> to display sources places, where FireDump calls has been used, <c>false</c> otherwise.</param>
        /// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
        public FireDump DumpCallStackInfo(bool dumpCallStackInfo = true) {
			this._dumpCallStackInfo = dumpCallStackInfo;
			return this;
		}
		/// <summary>
		/// Display value in browser console by classic <c>console.log(obj);</c> call.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
		public FireDump Log(object obj) {
			return this.Dump(obj, FireDumpType.Log);
		}
		/// <summary>
		/// Display value in browser console by <c>console.debug(obj);</c> call.
		/// This console item is rendered as blue text only if value is only a text message, if value is structuralized value, it's displayed in classic way.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
		public FireDump Debug (object obj) {
			return this.Dump(obj, FireDumpType.Debug);
		}
		/// <summary>
		/// Display value in browser console by <c>console.trace(obj);</c> call.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
		public FireDump Trace (object obj) {
			return this.Dump(obj, FireDumpType.Trace);
		}
		/// <summary>
		/// Display value in browser console by <c>console.info(obj);</c> call.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
		public FireDump Info(object obj) {
			return this.Dump(obj, FireDumpType.Info);
		}
		/// <summary>
		/// Display value in browser console by <c>console.warn(obj);</c> call.
		/// This console item is rendered as black text with light orange background and with orange icon with exclamation mark inside.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
		public FireDump Warn(object obj) {
			return this.Dump(obj, FireDumpType.Warn);
		}
		/// <summary>
		/// Display value in browser console by <c>console.error(obj);</c> call.
		/// This console item is rendered as red text with light red background and with red icon with cross inside.
		/// </summary>
		/// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
		/// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
		public FireDump Error(object obj) {
			return this.Dump(obj, FireDumpType.Error);
		}
        /// <summary>
        /// Display value in browser console by <c>console.[log|info|debug|trace|warn|error|table](obj);</c> call.
        /// </summary>
        /// <param name="obj">Any value to serialize with <c>(new JavascriptSerializer()).Serialize(obj);</c> and to display in client browser console.</param>
        /// <param name="dumpType">Javascript browser console object function name to call on client, but in upper case.</param>
        /// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
        public FireDump Dump (object obj, FireDumpType dumpType = FireDumpType.Debug) {
			if (!this._enabled) return this;
            StackTraceItem callPoint = Completers.StackTrace.CompleteCallerPoint();
            return this._renderHeaders(new FireDumpItem {
                Type = dumpType,
                File = callPoint.File.ToString(),
                Line = callPoint.Line,
                Content = obj
            });
		}
		/// <summary>
		/// Display .NET Exception in browser console by <c>console.error(obj);</c> call.
		/// This console item is rendered as red text with light red background and with red icon with cross inside.
		/// </summary>
		/// <param name="exception">.NET Exception to display in client browser console.</param>
		/// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
		public FireDump Exception(Exception exception) {
            if (!this._enabled) return this;
            StackTraceItem callPoint = this._getCallStackInfo();
            StackTraceItem? exceptionThrowPoint;
            Dictionary<string, ExceptionToRender> exceptions = Completers.StackTrace.CompleteInnerExceptions(exception, true);
            int i = 0;
            Exception e;
            string content;
            foreach (var item in exceptions) {
				e = item.Value.Exception;
				content = $"{e.GetType().FullName}: {e.Message}";
				RenderingCollection preparedResult = Completers.StackTrace.RenderStackTraceForException(
					item.Value, false, false, i++
				);
                exceptionThrowPoint = null;
                foreach (StackTraceItem stackTracesItem in preparedResult.AllStackTraces) {
					if (stackTracesItem.File.ToString().Length > 0) {
                        exceptionThrowPoint = new StackTraceItem {
							File = stackTracesItem.File.ToString(),
							Line = stackTracesItem.Line
						};
						break;
					}
				}
				if (exceptionThrowPoint == null) exceptionThrowPoint = callPoint;
				this._renderHeaders(new FireDumpItem {
                    Type = FireDumpType.Error,
                    Content = content,
                    File = exceptionThrowPoint.Value.File.ToString(),
                    Line = exceptionThrowPoint.Value.Line
                });
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
        /// <returns>FireDump instance is returned to call any other FireDump method in call chain.</returns>
        public FireDump Table(string label, object obj) {
            if (!this._enabled) return this;
            StackTraceItem callPoint = this._getCallStackInfo();
			return this._renderHeaders(new FireDumpItem {
                Type = FireDumpType.Table,
                Content = obj,
                File = callPoint.File.ToString(),
                Line = callPoint.Line,
                Label = label
            });
		}
		internal FireDump CloseHeaders () {
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
		private FireDump _renderHeaders(FireDumpItem item) {
			HttpContext context = HttpContext.Current;
			if (context != null) { // context could be null in unit testing threads
				this._appendBaseHeadersIfNecessary(context.Response);
                int line = 0;
                int.TryParse(item.Line, out line);
                string jsonString = String.Format(
                    // {message header}, {message content}
					"[{0}, {1}]",
                    // message header: {"Type":"DEBUG","File":"Path/To/CallerFile.cs","Line":10}
					this._serializer.Serialize(new {
                        Type = item.Type.ToString().ToUpper(),
                        File = item.File,
                        Line = line
                    }),
                    // message content: any serialized .NET value into JSON to put into console.debug() or any other console method.
					this._serializer.Serialize(item.Content)
				);
				context.Response.AppendHeader(
					String.Format("X-Wf-1-1-1-{0}", (this._logCounter + 1)),
                    // {json string total length}|{json string value}|
                    String.Format("{0}|{1}|",       jsonString.Length, jsonString)
				);
				this._logCounter += 1;
				if (this._logCounter > 9999) this._logCounter = 0;
			}
			return this;
		}
		private void _appendBaseHeadersIfNecessary(HttpResponse response) {
			if (this._baseHeadersInitialized) return;
			foreach (KeyValuePair<string, string> keypair in FireDump._baseHeaders) {
				response.AppendHeader(keypair.Key, keypair.Value);
			}
			this._baseHeadersInitialized = true;
		}
		private StackTraceItem _getCallStackInfo () {
            StackTraceItem result = new StackTraceItem();
			if (this._dumpCallStackInfo) {
                result = Completers.StackTrace.CompleteCallerPoint();
            }
			return result;
		}
	}
}