using System.Runtime.InteropServices;

namespace Desharp {
	/// <summary>
	/// Specify log files format - should be only text or html.
	/// </summary>
	[ComVisible(true)]
	public enum LogFormat {
		/// <summary>
		/// Log format will be determinated automaticly at first Desharp assembly use.
		/// </summary>
		Auto,
		/// <summary>
		/// All log messages will be written in HTML into *.html file
		/// </summary>
		Html,
		/// <summary>
		/// All log messages will be written in TEXT into *.log file
		/// </summary>
		Text
	}
}
