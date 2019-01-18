using System;
using System.Runtime.InteropServices;

namespace Desharp {
	/// <summary>
	/// Desharp configuration collection for Desharp.Debug.Configure(); calls.
	/// </summary>
	[ComVisible(true)]
	public struct DebugConfig {
		/// <summary>How many levels in complex type variables will be iterated throw to dump all it's properties, fields and other values.</summary>
		public int? Depth;
		/// <summary>Dumped objects printing to output enabled/disabled.</summary>
        public bool? Enabled;
		/// <summary>Relative or absolute path into directory where all log files and mail notify boolean files will be stored.</summary>
		public string Directory;
		/// <summary>Custom static error page file to send as web application response if any not catched exception will be thrown.</summary>
		public string ErrorPage;
		/// <summary>Miliseconds for interval to write Desharp.Debug.Log(); results from RAM into HDD to optimize application performance.</summary>
		public int? LogWriteMilisecond;
		/// <summary>Application logs content format, should be HTML or TEXT.</summary>
		public LogFormat LogFormat;
		/// <summary>Application environment - usually automaticly determinated by Desharp asembly - change this value only when you REALLY know what you are doing!</summary>
		public EnvType EnvType;
		/// <summary>Custom web debug panel types, implementing Desharp.Panels.Abstract to create their instances for each web request where debuging enabled.</summary>
		public Type[] Panels;
	}
}