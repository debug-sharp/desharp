using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Desharp.Panels {
	[ComVisible(true)]
	public class Dumps: IPanel {
		public static string PanelName = "dumps";
		public string Name => Dumps.PanelName;
		public int[] DefaultWindowSizes => new int[] { 600, 600 };
		public PanelIconType PanelIconType => PanelIconType.Class;
		public string IconValue => Dumps.PanelName;
		public bool AddIfEmpty => false;
		public PanelType PanelType => PanelType.BarBtnAndWindow;

		protected List<string> dumps = new List<string>();

		public string[] RenderBarTitle () {
			return new string[] { "Dumps (" + this.dumps.Count.ToString() + ")" };
		}
		public string RenderWindowContent () {
			return String.Join("", this.dumps);
		}

		public void AddRenderedDump(string dumpedCode) {
			this.dumps.Add(dumpedCode);
		}
	}
}
