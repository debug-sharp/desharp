using System;

namespace Desharp {
	[Serializable]
	public struct RenderedPanel {
		public string Name;
		public bool AddIfEmpty;
		public string Title;
		public string Content;
		public int[] DefaultWindowSizes;
		public PanelIconType PanelIconType;
		public PanelType PanelType;
		public string IconValue;
	}
}
