using System;
using System.Runtime.InteropServices;

namespace Desharp {
	/// <summary>
	/// Panel icon type for custom web debug panel.
	/// </summary>
	[Serializable]
	[ComVisible(true)]
	public enum PanelIconType {
		/// <summary>
		/// Debug panels without icon.
		/// </summary>
		None,
		/// <summary>
		/// For panel icons defined in internal Desharp response css code.
		/// </summary>
		Class,
		/// <summary>
		/// For panel icons defined in custom class as HTML code.
		/// </summary>
		Code
	}
}
