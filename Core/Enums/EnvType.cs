using System.Runtime.InteropServices;

namespace Desharp {
	/// <summary>
	/// Application general environment - should be only defined as web or desktop.
	/// </summary>
	[ComVisible(true)]
	public enum EnvType {
		/// <summary>
		/// Environment will be defined by Desharp assembly at first Desharp call.
		/// </summary>
		Auto,
		/// <summary>
		/// Environment for desktop applications - WPF, Winforms, console apps and other - generaly not web.
		/// </summary>
		Windows,
		/// <summary>
		/// Environment for web applications hosted in IIS server.
		/// </summary>
		Web	
	}
}
