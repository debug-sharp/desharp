using System;
using System.Web;
using System.Text;
using System.Collections.Generic;
using Desharp.Core;
using Desharp.Producers;
using Desharp.Renderers;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Desharp {
	/// <summary>
	/// Desharp main class:<br /><para /><ul>
	/// <li> Dump any object/exception to application output.</li><para />
	/// <li> Store any dumped object/exception in text/html log files.</li><para />
	/// <li> Enable/disable objects dumping to output.</li><para />
	/// <li> Configure Desharp from running application environment.</li>
	/// </ul></summary>
	[ComVisible(true)]
	public class Debug {
        //public static List<Exception> InitErrors = new List<Exception>();
		/// <summary>
		/// Desharp assembly Version.
		/// </summary>
		public static Version Version;
        /// <summary>
        /// Get or set current request FireDump to log anything into client browser console window through FirePHP extension.
        /// </summary>
        public static FireDump Fire {
            get { return Dispatcher.GetCurrent().GetFireDump(); }
            set { Dispatcher.GetCurrent().FireDump = value; }
        }
		/// <summary>
		/// Session storage key to store Desharp data inside Web app session storage.
		/// </summary>
		public const string SESSION_STORAGE_KEY = "$$$Desharp";
		internal const string SELF_FILENAME = "Debug.cs";
		static Debug () {
			Debug.Version = Assembly.GetExecutingAssembly().GetName().Version;
			//try { 
				Dispatcher.GetCurrent();
			/*} catch (Exception e) {
				Debug.InitErrors.Add(e);
			}*/
		}
		/// <summary>Enable or disable variables dumping from application code environment for all threads.</summary>
		/// <param name="enabled"><c>true</c> to enable, <c>false</c> to disable, if no param defined, no changes will be made.</param>
		/// <returns>bool about enabled/disabled dumping state</returns>
		/// <example>
		/// To determinate if dumps printing to output is enabled or not:
		/// <code>bool dumpsPrintedToOtuput = Desharp.Debug.Enabled();</code>
		/// To enable/disable dumps printing to output:
		/// <code>
		/// Desharp.Debug.Enabled(true);  // to enable dumps print in output
		/// Desharp.Debug.Enabled(false); // to disable dumps print in output
		/// </code>
		/// </example>
		public static bool Enabled (bool? enabled = null) {
			if (enabled.HasValue) Dispatcher.GetCurrent().Enabled = enabled.Value;
			return Dispatcher.GetCurrent().Enabled == true;
		}
		/// <summary>
		/// Configure Desharp assembly from running application environment and override any XML config settings or automatically detected settings.
		/// </summary>
		/// <param name="cfg">Specialized Desharp configuration collection - just create new instance with public fields of that.</param>
		/// <example>
		/// <code>Desharp.Debug.Configure(new Desharp.DebugConfig {
		/// 	Enabled = true,				// enable dumps printing to app output
		/// 	Depth = 3,				// dumped objects max. depth
		/// 	Directory = "~/logs",			// file logs directory rel./abs. path
		/// 	LogFormat = Desharp.LogFormat.Html,	// file logs format - text or html
		/// 	...
		/// });</code>
		/// </example>
		public static void Configure (DebugConfig cfg) {
			Dispatcher.GetCurrent().Configure(cfg);
		}
		/// <summary>
		/// Return last uncaught Exception in request, mostly used in web applications by error page rendering process to know something about Exception before.
		/// </summary>
		/// <returns>Last Exception instance, including original exception call stack and possible inner exceptions.</returns>
		public static Exception GetLastError () {
			return Dispatcher.GetCurrent().LastError;
		}
		/// <summary>
		/// Return spent request processing time for web applications or return application up time for all other platforms.
		/// </summary>
		/// <returns>Returned values is number of seconds with 3 decimal places after comma with milliseconds.</returns>
		public static double GetProcessingTime () {
			long startTicks = (Dispatcher.EnvType == EnvType.Windows)
				? Process.GetCurrentProcess().StartTime.Ticks
				: Tools.GetRequestId();
			double r = new TimeSpan(DateTime.Now.Ticks - startTicks).TotalSeconds;
			return Math.Round(r * 1000) / 1000;
        }
		/// <summary>
		/// Prints to output or into log file number of seconds from last timer call under called name in seconds with 3 floating point decimal spaces.<br /><para />
		/// If no name specified or name is empty string, there is returned:<para /><ul>
		/// <li> <b>Web applications</b> - number of seconds from request beginning.</li><para />
		/// <li> <b>Desktop applications</b> - number of seconds from application start.</li><para />
		/// </ul></summary>
		/// <param name="name">Timer name, used as key to find last <c>Desharp.Debug.Timer(name);</c> call from internal dictionary to print the timespan in app output or log file.</param>
		/// <param name="returnTimerSeconds">If <c>true</c>, do not print or log timer value - only return the time span difference decimal value as result of this function.</param>
		/// <param name="logLevel">Use log level to specify log file used when dump printing into output is disabled, <c>"debug.log"</c> used by default.</param>
		/// <returns>If <c>returnTimerSeconds</c> param is <c>true</c>, return the time span difference decimal from the last Desharp.Debug.Timer(name); call under called <c>name</c> or return zero if <c>returnTimerSeconds</c> param is <c>false</c> (by default).</returns>
		public static double Timer (string name = null, bool returnTimerSeconds = false, Level logLevel = Level.DEBUG) {
			long nowTicks = DateTime.Now.Ticks;
			double result = 0;
			bool nameIsNullOrEmpty = String.IsNullOrEmpty(name);
			if (nameIsNullOrEmpty) {
				result = nowTicks - Debug.GetProcessingTime();
			} else {
				Dictionary<string, double> timers = Dispatcher.GetCurrent().Timers;
				if (timers.ContainsKey(name)) {
					result = (nowTicks - timers[name]);
					timers[name] = nowTicks;
				} else {
					result = 0;
					timers.Add(name, nowTicks);
				}
			}
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			if (!returnTimerSeconds) {
				double seconds = new TimeSpan((long)result).TotalSeconds;
				string dumpResult = String.Format(CultureInfo.InvariantCulture, seconds < 1000 ? "{0:0.000}" : "{0:0,0.000}", seconds);
				if (Dispatcher.EnvType == EnvType.Web || (dispatcher.Enabled != true && dispatcher.Output == LogFormat.Html)) {
					dumpResult = (nameIsNullOrEmpty ? "" : "<i>" + name.Replace(" ", "&nbsp;") + ":</i>")
						+ "<b>" + dumpResult + "&nbsp;s</b>"
						+ @"<span class=""type"">[Desharp.Timer]</span>";
					if (dispatcher.Enabled == true) {
						dumpResult = Dumper.HtmlDumpWrapper[0].Replace("-dump", "-dump timer") + dumpResult + Dumper.HtmlDumpWrapper[1];
					}
				} else {
					dumpResult = (nameIsNullOrEmpty ? "" : name + ": ") + dumpResult + " s [Desharp.Timer]" + Environment.NewLine;
				}
				if (dispatcher.Enabled == true) {
					dispatcher.WriteDumpToOutput(dumpResult);
				} else {
					dumpResult = Exceptions.RenderCurrentApplicationPoint(
						dumpResult, "Value", true, dispatcher.Output == LogFormat.Html
					) + Environment.NewLine;
					FileLog.Log(dumpResult, LevelValues.Values[logLevel]);
				}
				return 0;
			} else {
				return result;
			}
		}
        /// <summary>
        /// Print to application output or log into file (if enabled) first param to be <c>true</c> or not and describe what was equal or not in first param by second param <c>message</c>.
        /// </summary>
        /// <param name="assertion">Comparison boolean to dump or log, way to compare things is up to you, you need to process it in method param space to send here boolean only.</param>
        /// <param name="description">Any text to describe previous comparison.</param>
        /// <param name="logLevel">Log level, used only when printing to output is disabled, <c>"default.log"</c> used by default.</param>
        public static void Assert (bool assertion, string description = "", Level logLevel = Level.DEBUG) {
			string result = String.Format(
				(description.Length > 0 ? "{0}: {1}" : "{0}{1}"), description, (assertion ? "true" : "false")
			);
			if (Dispatcher.GetCurrent().Enabled == true) {
				Debug.Dump(result);
			} else {
				Debug.Log(result, logLevel);
			}
        }
        /// <summary>
        /// Print current thread stack trace into application output and exit running application.
        /// - web applications - stop current request
        /// - any other applications - stop application with all it's threads
        /// </summary>
        public static void Stop () {
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			if (dispatcher.Enabled != true) return;
			bool htmlOut = dispatcher.Output == LogFormat.Html && Dispatcher.EnvType == EnvType.Web;
		    string renderedException = Exceptions.RenderCurrentApplicationPoint(
                "Script has been stopped.", "Exception", false, htmlOut
            );
            if (Dispatcher.EnvType == EnvType.Web){
				HtmlResponse.SendRenderedExceptions(renderedException, "Exception");
			} else {
				dispatcher.WriteExceptionToOutput(new List<string>() { renderedException });
                Console.Read();
			}
            dispatcher.Stop();
		}
        /// <summary>
        /// Dump exception instance to application output if output dumping is enabled. It renders:<para /><ul>
        /// <li>exception <b>type</b><para /></li>
        /// <li>exception <b>message</b><para /></li>
        /// <li>if exception has been <b>caught</b> or <b>not caught</b><para /></li>
        /// <li>exception <b>hash id</b><para /></li>
        /// <li><b>error file</b> where exception has been thrown<para /></li>
        /// <li>thread call stack<para /></li>
        /// <li>all inner exceptions after this exception in the same way<para /></li>
        /// </ul></summary>
        /// <param name="exception">Exception instance to dump.</param>
        /// <param name="options">Dump options collection (optional) - just create new instance with public fields of that:<para /><br />
        /// For this dump call you can change options:<para /><ul>
        /// <li><b>Return</b> (bool, optional) - if exception will be dumped into application output (as default) or returned as dumped string value.<para /></li>
        /// </ul></param>
        /// <returns>Returns empty string if debug printing is disabled and also returns empty string if second param <c>DumpOptions.Return</c> is <c>false</c> (by default), but if true, return dumped exception as string.</returns>
        public static string Dump (Exception exception = null, DumpOptions? options = null) {
            Dispatcher dispatcher = Dispatcher.GetCurrent();
			dispatcher.LastError = exception;
			if (dispatcher.Enabled != true) return "";
			if (!options.HasValue) options = new DumpOptions { Return = false, Depth = 0, MaxLength = 0 };
			DumpOptions optionsValue = options.Value;
			string dumpResult = "";
			List<string> exceptionResult = new List<string>();
			bool htmlOut = Dispatcher.EnvType == EnvType.Web;
			if (exception == null) {
				dumpResult = Dumper.Dump(null);
			} else if (exception is Exception) {
				if (!optionsValue.CatchedException.HasValue) {
					optionsValue.CatchedException = true;
				}
				exceptionResult = Exceptions.RenderExceptions(exception, false, htmlOut, optionsValue.CatchedException.Value);
			} else {
				if (!optionsValue.Depth.HasValue) optionsValue.Depth = 0;
				if (!optionsValue.MaxLength.HasValue) optionsValue.MaxLength = 0;
				if (!optionsValue.Return.HasValue) optionsValue.Return = false;
				dumpResult = Dumper.Dump(exception, htmlOut, optionsValue.Depth.Value, optionsValue.MaxLength.Value);
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
		/// <summary>
		/// Dump any values to application output (in web applications into debug bar, in desktop applications into console).
		/// </summary>
		/// <param name="args">Infinite number of values to dump into application output.</param>
		/// <returns>All dumped values are in this function version always returned as dumped string.</returns>
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
					resultItems.Append(Dumper.Dump(e, htmlOut));
                }
			}
			result = resultItems.ToString();
			dispatcher.WriteDumpToOutput(result);
			return result;
		}
		/// <summary>
		/// Dump any type value to application output (in web applications into debug bar, in desktop applications into console).
		/// </summary>
		/// <param name="obj">Any type value to dump into application output.</param>
		/// <param name="options">Dump options collection (optional) - just create new instance with public fields of that:<para /><br />
		/// For this dump call you can change options:<para /><ul>
		/// <li><b>Depth</b> (int, optional) - how many levels in complex type variables will be iterated throw to dump all it's properties, fields and other values.<para /></li>
		/// <li><b>MaxLength</b> (int, optional) - if any dumped string length is larger than this value, it will be cut into this max. length.<para /></li>
		/// <li><b>Return</b> (bool, optional)- if value will be dumped into application output (as default) or returned as dumped string value.<para /></li>
		/// </ul></param>
		/// <returns>Returns empty string by default or dumped variable string if you specify in second argument <c>DumpOptions.Return</c> to be <c>true</c>.</returns>
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
		/// <summary>
		/// Dump any type value to direct application output (not into web request debug bar in web applications!) and stop request/thread (in web applications dump into direct response body, in desktop applications into console).
		/// </summary>
		/// <param name="obj">Any type value to dump into application output.</param>
		/// <param name="options">Dump options collection (optional) - just create new instance with public fields of that:<para /><br />
		/// For this dump call you can change options:<para /><ul>
		/// <li><b>Depth</b> (int, optional) - how many levels in complex type variables will be iterated throw to dump all it's properties, fields and other values.<para /></li>
		/// <li><b>MaxLength</b> (int, optional) - if any dumped string length is larger than this value, it will be cut into this max. length.<para /></li>
		/// <li><b>Return</b> (bool, optional)- if value will be dumped into application output (as default) or returned as dumped string value.<para /></li>
		/// </ul></param>
		/// <returns>Returns empty string by default or dumped variable string if you specify in second argument <c>DumpOptions.Return</c> to be <c>true</c>.</returns>
		public static string DumpAndDie (object obj = null, DumpOptions? options = null) {
			if (!options.HasValue) options = new DumpOptions { Return = true, Depth = 0, MaxLength = 0 };
			DumpOptions optionsValue = options.Value;
			DumpOptions optionsValueClone = options.Value;
			if (!optionsValue.Return.HasValue) optionsValue.Return = true;
			if (!optionsValueClone.Return.HasValue) optionsValueClone.Return = false;
			string result = Debug.Dump(obj, optionsValue);
			if (Dispatcher.EnvType == EnvType.Web) {
				HttpContext.Current.Response.Write(result);
			} else {
				Console.Write(result);
			}
			Dispatcher.GetCurrent().Stop();
			return optionsValueClone.Return == true ? result : "";
		}
		/// <summary>
		/// Log exception instance as dumped string into <c>exceptions.log|exceptions.html</c> file. It stores:<para /><ul>
		/// <li>exception <b>type</b><para /></li>
		/// <li>exception <b>message</b><para /></li>
		/// <li>if exception has been <b>caught</b> or <b>not caught</b><para /></li>
		/// <li>exception <b>hash id</b><para /></li>
		/// <li><b>error file</b> where exception has been thrown<para /></li>
		/// <li>thread call stack<para /></li>
		/// <li>all inner exceptions after this exception in the same way<para /></li>
		/// </ul></summary>
		/// <param name="exception">Exception instance to print.</param>
		public static void Log (Exception exception = null) {
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			dispatcher.LastError = exception;
			bool htmlOut = dispatcher.Output == LogFormat.Html;
			List<string> renderedExceptions = Exceptions.RenderExceptions(exception, true, htmlOut, true);
			if (Dispatcher.Levels["exception"] == 2) Mailer.Notify(String.Join(Environment.NewLine, renderedExceptions), "exception", htmlOut);
			foreach (string renderedException in renderedExceptions) FileLog.Log(renderedException + Environment.NewLine, "exception");
		}
		/// <summary>
		/// Log any type value to application <c>*.log|*.html</c> file, specified by level param.
		/// </summary>
		/// <param name="obj">Any type value to dump into application output.</param>
		/// <param name="level">Log level to specify log file name and also to allow/prevent write dumped variable into proper log file by config settings.</param>
		/// <param name="maxDepth">How many levels in complex type variables will be iterated throw to dump all it's properties, fields and other values.</param>
		/// <param name="maxLength">If any dumped string length is larger than this value, it will be cut into this max. length.</param>
		/// <returns>Returns empty string by default or dumped variable string if you specify in second argument <c>DumpOptions.Return</c> to <c>true</c>.</returns>
		public static void Log (object obj = null, Level level = Level.INFO, int maxDepth = 0, int maxLength = 0) {
			Dispatcher dispatcher = Dispatcher.GetCurrent();
			bool htmlOut = dispatcher.Output == LogFormat.Html;
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
					// remove begin: <div class="desharp-dump"> and end: </div>
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