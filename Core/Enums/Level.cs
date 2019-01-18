using System.Runtime.InteropServices;

namespace Desharp {
	/// <summary>
	/// Application logging levels to define log filename used for Desharp.Debug.Log() calls and to define if log message will be written or not by config settings.
	/// </summary>
	[ComVisible(true)]
	public enum Level {
		/// <summary>
		/// debug.log|debug.html, mostly used as default log file.
		/// </summary>
		DEBUG,
		/// <summary>
		/// info.log|info.html
		/// </summary>
		INFO,
		/// <summary>
		/// notice.log|notice.html
		/// </summary>
		NOTICE,
		/// <summary>
		/// warning.log|warning.html
		/// </summary>
		WARNING,
		/// <summary>
		/// error.log|error.html
		/// </summary>
		ERROR,
		/// <summary>
		/// critical.log|critical.html
		/// </summary>
		CRITICAL,
		/// <summary>
		/// alert.log|alert.html
		/// </summary>
		ALERT,
		/// <summary>
		/// emergency.log|emergency.html
		/// </summary>
		EMERGENCY,
		/// <summary>
		/// javascript.log|javascript.html, usually used to store javascript errors from client's browser by window.onerror() global handler.
		/// </summary>
		JAVASCRIPT,
	}
}
