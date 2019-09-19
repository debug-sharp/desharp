using Desharp.Renderers;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Desharp.Core {
	/// <summary>
	/// Desharp class to create debugging and logging instance in VBA console:<br /><para /><ul>
	/// <li> Dump any object/exception to application output.</li><para />
	/// <li> Store any dumped object/exception in text/html log files.</li><para />
	/// <li> Enable/disable objects dumping to output.</li><para />
	/// <li> Configure Desharp from running application environment.</li>
	/// </ul></summary>
	[ComVisible(true)]
	public class Vba {
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
		[ComVisible(true)]
		public bool Enabled (bool enabled) {
			if (enabled) Dispatcher.GetCurrent().Enabled = enabled;
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
		public void Configure (DebugConfig cfg) {
			Dispatcher.GetCurrent().Configure(cfg);
		}
		/// <summary>
		/// Return last uncaught Exception in request, mostly used in web applications by error page rendering process to know something about Exception before.
		/// </summary>
		/// <returns>Last Exception instance, including original exception call stack and possible inner exceptions.</returns>
		[ComVisible(true)]
		public Exception GetLastError () {
			return Dispatcher.GetCurrent().LastError;
		}
		/// <summary>
		/// Return spent request processing time for web applications or return application up time for all other platforms.
		/// </summary>
		/// <returns>Returned values is number of seconds with 3 decimal places after comma with milliseconds.</returns>
		[ComVisible(true)]
		public double GetProcessingTime () {
			return Debug.GetProcessingTime();
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
		[ComVisible(true)]
		public double Timer(string name = null, bool returnTimerSeconds = false, Level logLevel = Level.DEBUG) {
			return Debug.Timer(name, returnTimerSeconds, logLevel);
		}
		/// <summary>
        /// Print to application output or log into file (if enabled) first param to be <c>true</c> or not and describe what was equal or not in first param by second param <c>message</c>.
        /// </summary>
        /// <param name="assertion">Comparison boolean to dump or log, way to compare things is up to you, you need to process it in method param space to send here boolean only.</param>
        /// <param name="description">Any text to describe previous comparison.</param>
        /// <param name="logLevel">Log level, used only when printing to output is disabled, <c>"default.log"</c> used by default.</param>
		[ComVisible(true)]
		public void Assert(bool assertion, string description = "", Level logLevel = Level.DEBUG) {
			Debug.Assert(assertion, description, logLevel);
		}
		/// <summary>
        /// Print current thread stack trace into application output and exit running application.
        /// - web applications - stop current request
        /// - any other applications - stop application with all it's threads
        /// </summary>
		[ComVisible(true)]
		public void Stop() {
			Debug.Stop();
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
		[ComVisible(true)]
		public string Dump(Exception exception, DumpOptions options) {
			return Debug.Dump(exception, options);
		}
		/// <summary>
		/// Dump any values to application output (in web applications into debug bar, in desktop applications into console).
		/// </summary>
		/// <param name="args">Infinite number of values to dump into application output.</param>
		/// <returns>All dumped values are in this function version always returned as dumped string.</returns>
		[ComVisible(true)]
		public string DumpMultiple(params object[] args) {
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
		[ComVisible(true)]
		public string DumpWithOptions(object obj, DumpOptions options) {
			return Debug.Dump(obj, options);
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
		[ComVisible(true)]
		public string DumpAndDie(object obj, DumpOptions options) {
			return Debug.DumpAndDie(obj, options);
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
		[ComVisible(true)]
		public void LogException(Exception exception = null) {
			Debug.Log(exception);
		}
		/// <summary>
		/// Log any type value to application <c>*.log|*.html</c> file, specified by level param.
		/// </summary>
		/// <param name="obj">Any type value to dump into application output.</param>
		/// <param name="level">Log level to specify log file name and also to allow/prevent write dumped variable into proper log file by config settings.</param>
		/// <param name="maxDepth">How many levels in complex type variables will be iterated throw to dump all it's properties, fields and other values.</param>
		/// <param name="maxLength">If any dumped string length is larger than this value, it will be cut into this max. length.</param>
		/// <returns>Returns empty string by default or dumped variable string if you specify in second argument <c>DumpOptions.Return</c> to <c>true</c>.</returns>
		[ComVisible(true)]
		public void Log(object obj = null, Level level = Level.INFO, int maxDepth = 0, int maxLength = 0) {
			Debug.Log(obj, level, maxDepth, maxLength);
		}
	}
}
