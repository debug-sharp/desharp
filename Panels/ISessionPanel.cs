using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desharp.Panels {
	/// <summary>
	/// PanelDesharp panel with methods using session start/end events.
	/// </summary>
	[ComVisible(true)]
	public interface ISessionPanel {
		/// <summary>
		/// Executed after session is readed.
		/// </summary>
		void SessionBegin();
		/// <summary>
		/// Executed when session is before write/close state.
		/// </summary>
		void SessionEnd();
	}
}
