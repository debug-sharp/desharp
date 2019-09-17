using System.Runtime.InteropServices;

namespace Desharp {
	/// <summary>
	/// Dump options for Desharp.Debug.Dump() calls to optimize how values or exceptions should be rendered or how to work with rendered results.
	/// </summary>
	[ComVisible(true)]
	public struct DumpOptions {
		/// <summary>
		/// How many levels in complex type variables will be iterated throw to dump all it's properties, fields and other values.
		/// </summary>
		public int? Depth;
		/// <summary>
		/// If any dumped string length is larger than this value, it will be cutted into this max length.
		/// </summary>
		public int? MaxLength;
		/// <summary>
		/// Set true if you want to return dumped string as result of Desharp.Debug.Dump() function call.
		/// </summary>
		public bool? Return;
        /// <summary>
        /// Set true if you want to render red exception over whole browser screen.
        /// </summary>
        public bool? CatchedException;
	}
}
