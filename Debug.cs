using System;
using System.Web;
using System.Text;
using System.Collections.Generic;
using Desharp.Core;
using Desharp.Producers;
using Desharp.Renderers;
using System.Reflection;

namespace Desharp {
    public class Debug {
		public static Version Version;
		public const string SESSION_STORAGE_KEY = "$$$Desharp";
		internal const string SELF_FILENAME = "Debug.cs";
		static Debug () {
			Debug.Version = Assembly.GetExecutingAssembly().GetName().Version;
			Dispatcher.GetCurrent();
		}
		/// <summary>Enable or disable variables dumping from application code environment for all threads.</summary>
		/// <param name="enabled">true to enable, false to disable, if not defined, no changes will be made.</param>
		/// <returns>bool about enabled/disabled dumping state</returns>
		public static bool Enabled (bool? enabled = null) {
			if (enabled.HasValue) Dispatcher.GetCurrent().Enabled = enabled.Value;
			return Dispatcher.GetCurrent().Enabled == true;
		}
		public static void Configure (DebugConfig cfg) {
			Dispatcher.GetCurrent().Configure(cfg);
		}
		public static double GetProcessingTime() {
			double r = new TimeSpan(DateTime.Now.Ticks - Tools.GetRequestId()).TotalSeconds;
            return Math.Round(r * 1000) / 1000;
        }
		public static Exception GetLastError () {
			return Dispatcher.GetCurrent().LastError;
		}
		public static void Time(string msg = "") {
			if (Dispatcher.GetCurrent().Enabled != true) return;
			Debug.Dump((msg.Length > 0 ? msg + ": " : "") + Debug.GetProcessingTime());
        }
		public static void Assert(bool assertion, string message = "") {
			if (Dispatcher.GetCurrent().Enabled != true) return;
			Debug.Dump(String.Format(
				(message.Length > 0 ? "{0}: ({1})" : "{0}{1}"), message, (assertion ? "true" : "false")
			));
        }
		public static void Stop () {
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			if (dispatcher.Enabled != true) return;
			bool htmlOut = dispatcher.Output == OutputType.Html && Dispatcher.EnvType == EnvType.Web;
		    string renderedException = Exceptions.RenderCurrentApplicationPoint(
                "Script has been stopped.", "Exception", false, htmlOut
            );
            if (Dispatcher.EnvType == EnvType.Web){
				HtmlResponse.SendRenderedExceptions(renderedException, "Exception");
			} else {
				dispatcher.WriteExceptionToOutput(new List<string>() { renderedException });
			}
			dispatcher.Stop();
		}
		public static string Dump (Exception e, DumpOptions? options = null) {
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			dispatcher.LastError = e;
			if (dispatcher.Enabled != true) return "";
			if (!options.HasValue) options = new DumpOptions { Return = false, Depth = 0, MaxLength = 0 };
			DumpOptions optionsValue = options.Value;
			string dumpResult = "";
			List<string> exceptionResult = new List<string>();
			bool htmlOut = Dispatcher.EnvType == EnvType.Web;
			if (e == null) {
				dumpResult = Dumper.Dump(null);
			} else if (e is Exception) {
				if (!optionsValue.CatchedException.HasValue) {
					optionsValue.CatchedException = true;
				}
				exceptionResult = Exceptions.RenderExceptions(e, false, htmlOut, optionsValue.CatchedException.Value);
			} else {
				if (!optionsValue.Depth.HasValue) optionsValue.Depth = 0;
				if (!optionsValue.MaxLength.HasValue) optionsValue.MaxLength = 0;
				if (!optionsValue.Return.HasValue) optionsValue.Return = false;
				dumpResult = Dumper.Dump(e, htmlOut, optionsValue.Depth.Value, optionsValue.MaxLength.Value);
			}
			if (!optionsValue.Return.HasValue || (optionsValue.Return.HasValue && !optionsValue.Return.Value)) {
				if (dumpResult.Length == 0 && exceptionResult.Count > 0) {
					dispatcher.WriteExceptionToOutput(exceptionResult);
				} else {
					dispatcher.WriteDumpToOutput(dumpResult);
				}
				return "";
			}
			if (dumpResult.Length == 0 && exceptionResult.Count > 0) {
				return String.Join(Environment.NewLine, exceptionResult.ToArray());
			} else {
				return dumpResult;
			}
		}
		public static string Dump (params object[] args) {
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			if (dispatcher.Enabled != true) return "";
			string result;
			StringBuilder resultItems = new StringBuilder();
			bool htmlOut = Dispatcher.EnvType == EnvType.Web;
			if (args == null) args = new object[] { null };
			if (args.GetType().FullName != "System.Object[]") args = new object[] { args };
			for (int i = 0; i < args.Length; i++) {
                try {
					resultItems.Append(Dumper.Dump(args[i], htmlOut));
                } catch (Exception e) {
					resultItems.Append(Debug.Dump(e, true));
                }
			}
			result = resultItems.ToString();
			dispatcher.WriteDumpToOutput(result);
			return result;
		}
		public static string Dump (object obj, DumpOptions? options = null) {
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			if (dispatcher.Enabled != true) return "";
			if (!options.HasValue) options = new DumpOptions { Return = false, Depth = 0, MaxLength = 0 };
			DumpOptions optionsValue = options.Value;
			if (!optionsValue.Depth.HasValue) optionsValue.Depth = 0;
			if (!optionsValue.MaxLength.HasValue) optionsValue.MaxLength = 0;
			if (!optionsValue.Return.HasValue) optionsValue.Return = false;
			string result = "";
			bool htmlOut = Dispatcher.EnvType == EnvType.Web;
			try {
				result = Dumper.Dump(obj, htmlOut, optionsValue.Depth.Value, optionsValue.MaxLength.Value);
			} catch (Exception e) {
				result = Debug.Dump(e, optionsValue);
			}
			if (!optionsValue.Return.Value || (optionsValue.Return.HasValue && !optionsValue.Return.Value)) {
				dispatcher.WriteDumpToOutput(result);
				return "";
			}
			return result;
		}
		public static string DumpAndDie (object obj, DumpOptions? options = null) {
			if (!options.HasValue) options = new DumpOptions { Return = true, Depth = 0, MaxLength = 0 };
			DumpOptions optionsValue = options.Value;
			if (!optionsValue.Return.HasValue) optionsValue.Return = true;
			string result = Debug.Dump(obj, optionsValue);
			if (Dispatcher.EnvType == EnvType.Web) {
				HttpContext.Current.Response.Write(result);
			} else {
				Console.Write(result);
			}
			Dispatcher.GetCurrent().Stop();
			return "";
		}
		public static void Log (Exception e) {
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			dispatcher.LastError = e;
			bool htmlOut = dispatcher.Output == OutputType.Html;
			List<string> renderedExceptions = Exceptions.RenderExceptions(e, true, htmlOut, true);
			if (Dispatcher.Levels["exception"] == 2) Mailer.Notify(String.Join(Environment.NewLine, renderedExceptions), "exception", htmlOut);
			foreach (string renderedException in renderedExceptions) FileLog.Log(renderedException + Environment.NewLine, "exception");
		}
		public static void Log (object obj, Level level = Level.INFO, int maxDepth = 0, int maxLength = 0) {
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			bool htmlOut = dispatcher.Output == OutputType.Html;
			string renderedObj;
			string logLevelValue = LevelValues.Values[level];
			if (level == Level.JAVASCRIPT) {
				if (!(obj is Dictionary<string, string>)) {
					Debug.Log(new Exception("To log javascript exceptions, call: Desharp.Debug.Log(data as Dictionary<string, string>, Level.JAVASCRIPT);"));
				}
				renderedObj = JavascriptExceptionData.RenderLogedExceptionData(obj as Dictionary<string, string>, htmlOut) 
					+ Environment.NewLine;
			} else {
                try {
                    renderedObj = Dumper.Dump(obj, htmlOut, maxDepth, maxLength);
                } catch (Exception e) {
                    renderedObj = e.Message;
                }
				if (htmlOut && renderedObj.Length > Dumper.HtmlDumpWrapper[0].Length && renderedObj.IndexOf(Dumper.HtmlDumpWrapper[0]) == 0) {
					// remove begin <div class="item"> end </div>
					renderedObj = renderedObj.Substring(Dumper.HtmlDumpWrapper[0].Length, renderedObj.Length - (Dumper.HtmlDumpWrapper[0].Length + Dumper.HtmlDumpWrapper[1].Length));
				}
				renderedObj = Exceptions.RenderCurrentApplicationPoint(
					renderedObj, "Value", true, htmlOut
				) + Environment.NewLine;
				if (Dispatcher.Levels[logLevelValue] == 2) Mailer.Notify(renderedObj, logLevelValue, htmlOut);
			}
			FileLog.Log(renderedObj, logLevelValue);
		}
	}
}