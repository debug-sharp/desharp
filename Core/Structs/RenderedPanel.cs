using System;
using System.Runtime.InteropServices;

namespace Desharp {
	[Serializable]
	[ComVisible(true)]
	public struct RenderedPanel {
		public string Name;
		public bool AddIfEmpty;
		public string[] Title;
		public string Content;
		public int[] DefaultWindowSizes;
		public PanelIconType PanelIconType;
		public PanelType PanelType;
		public string IconValue;
	}
}
