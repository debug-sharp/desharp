using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
