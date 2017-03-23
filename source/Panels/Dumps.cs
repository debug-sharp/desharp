using System;
using System.Collections.Generic;

namespace Desharp.Panels {
	public class Dumps: Abstract {
		public static string PanelName = "dumps";
		public override string Name {
			get { return Dumps.PanelName; }
		}
		public new int[] DefaultWindowSizes {
			get { return new int[] { 300, 200 }; }
		}
		public override PanelIconType PanelIconType {
			get { return PanelIconType.Class; }
		}
		public override string IconValue {
			get { return Dumps.PanelName; }
		}
		private List<string> _dumps = new List<string>();
		public void AddRenderedDump (string dumpedCode) {
			this._dumps.Add(dumpedCode);
		}
		public override string RenderBarText () {
			return "Dumps (" + this._dumps.Count.ToString() + ")";
		}
		public override string RenderWindowContent () {
			return String.Join("", this._dumps);
		}
	}
}
