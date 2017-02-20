using System;
using System.Web;
using System.Text;
using System.Collections.Generic;
using Desharp.Core;
using Desharp.Completers;
using Desharp.Outputers;
using Desharp.Renderers;
using System.Threading;

namespace Desharp {
    public class Debug {
		internal const string SELF_FILENAME = "Debug.cs";
		public static bool Enabled() {
            return Core.Environment.GetEnabled();
		}
		public static void Configure (DebugConfig cfg) {
			Core.Environment.Configure(cfg);
		}
		public static double GetProcessingTime() {
			double r = new TimeSpan(DateTime.Now.Ticks - Tools.GetRequestId()).TotalSeconds;
            return Math.Round(r * 1000) / 1000;
        }
        public static void Time(string msg = "") {
			if (!Core.Environment.GetEnabled()) return;
			Debug.Dump((msg.Length > 0 ? msg + ": " : "") + Debug.GetProcessingTime());
        }
		public static void Assert(bool assertion, string message) {
			if (!Core.Environment.GetEnabled()) return;
			Debug.Dump(
				String.Format("{0}: ({1})", message, assertion ? "true" : "false")
			);
        }
		public static void Stop () {
			if (!Core.Environment.GetEnabled()) return;
			Debug.Dump(new Exception("Script has been stopped."));
			if (Core.Environment.Type == EnvironmentType.Web) {
				HttpContext.Current.Response.End();
			} else {
				Thread.CurrentThread.Abort();
			}
		}
		public static void Dump (Exception e) {
			if (!Core.Environment.GetEnabled()) return;
			bool htmlOut = Core.Environment.GetOutput() == OutputType.Html && Core.Environment.Type == EnvironmentType.Web;
			string renderedExceptions = Debug._renderStackTraceForExceptions(e, false, htmlOut);
			Core.Environment.WriteOutput(renderedExceptions);
		}
		public static void Log (Exception e) {
			if (!Core.Environment.GetEnabled()) return;
			bool htmlOut = Core.Environment.GetOutput() == OutputType.Html;
			string renderedExceptions = Debug._renderStackTraceForExceptions(e, true, htmlOut);
			FileLog.Log(renderedExceptions, "exception");
		}
		public static void Dump(params object[] args){
			if (!Core.Environment.GetEnabled()) return;
            bool htmlOut = Core.Environment.GetOutput() == OutputType.Html && Core.Environment.Type == EnvironmentType.Web;
            string logResult;
			if (args == null) args = new object[] { null };
			object arg;
			for (int i = 0; i < args.Length; i++) {
				arg = args[i];
				if (arg != null && arg is Exception) {
					Debug.Log(arg as Exception);
				} else {
					logResult = Dumper.Dump(args[i], 0, htmlOut);
					Core.Environment.WriteOutput(logResult);
				}
            }
        }
		public static void Log (object obj, Level level = Level.DEBUG) {
			if (!Core.Environment.GetEnabled()) return;
			bool htmlOut = Core.Environment.GetOutput() == OutputType.Html;
			string renderedObj;
			if (level == Level.JAVASCRIPT) {
				if (!(obj is Dictionary<string, string>)) {
					Debug.Log(new Exception("To log javascript exceptions - pass into Debug.Write() type: Dictionary<string, string>."));
				}
				renderedObj = JavascriptExceptionData.RenderLogedExceptionData(obj as Dictionary<string, string>, htmlOut) + "\n";
			} else {
				renderedObj = Dumper.Dump(obj, 0, htmlOut);
				if (htmlOut) {
					// remove begin and end div
					renderedObj = renderedObj.Substring(5, renderedObj.Length - (5 + 6));
				}
				renderedObj = Debug._renderStackTraceForCurrentApplicationPoint(
					renderedObj, htmlOut ? "" : "Value", true, htmlOut
				) + "\n";
			}
			FileLog.Log(renderedObj, LevelValues.Values[level]);
		}
		internal static void RequestBegin (long crt = -1) {
			if (crt == -1) crt = Tools.GetRequestId();
			Core.Environment.RequestBegin(crt);
		}
		internal static void RequestEnd (long crt = -1) {
            if (Core.Environment.Type == EnvironmentType.Web) {
                if (crt == -1) crt = Tools.GetRequestId();
                HtmlResponse.RequestEnd(crt);
				FileLog.RequestEnd(crt);
				Core.Environment.RequestEnd(crt);
            }
        }
		private static string _renderStackTraceForExceptions (Exception e, bool fileSystemLog = true, bool htmlOut = false) {
			StringBuilder result = new StringBuilder();
			Dictionary<string, Exception> exceptions = Completers.StackTrace.CompleteInnerExceptions(e);
			Dictionary<string, string> headers = new Dictionary<string, string>();
			if (Core.Environment.Type == EnvironmentType.Web) {
				headers = HttpHeaders.CompletePossibleHttpHeaders();
			}
			int i = 0;
			foreach (var item in exceptions) {
				if (item.Value.StackTrace == null) continue;
				RenderingCollection preparedResult = Completers.StackTrace.RenderStackTraceForException(
					item.Value, fileSystemLog, htmlOut, i
				);
				preparedResult.Headers = headers;
				result.Append(
					Renderers.StackTrace.RenderStackRecordResult(
						preparedResult, fileSystemLog, htmlOut
					)
				);
				i++;
			}
			return result.ToString();
		}
		private static string _renderStackTraceForCurrentApplicationPoint (string message = "", string exceptionType = "", bool fileSystemLog = true, bool htmlOut = false) {
			RenderingCollection preparedResult = Completers.StackTrace.CompleteStackTraceForCurrentApplicationPoint(message, exceptionType, fileSystemLog, htmlOut);
			Dictionary<string, string> headers = new Dictionary<string, string>();
			if (Core.Environment.Type == EnvironmentType.Web) {
				headers = HttpHeaders.CompletePossibleHttpHeaders();
			}
			preparedResult.Headers = headers;
			return Renderers.StackTrace.RenderStackRecordResult(preparedResult, fileSystemLog, htmlOut);
		}
	}
}