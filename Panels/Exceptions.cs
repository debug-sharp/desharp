using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Desharp.Panels {
	[ComVisible(true)]
	public class Exceptions: IPanel {
		public static string PanelName = "exceptions";
		public string Name => Exceptions.PanelName;
		public PanelType PanelType => PanelType.BarBtnAndScreen;
		public PanelIconType PanelIconType => PanelIconType.Class;
		public string IconValue => Exceptions.PanelName;
		public bool AddIfEmpty => false;
		public int[] DefaultWindowSizes => new int[] { 0, 0 };

		private List<string> _exceptions = new List<string>();

		public void SessionBegin() { }
		public void SessionEnd() { }
		public string[] RenderBarTitle () {
			return new string[] { "Exceptions (" + this._exceptions.Count.ToString() + ")" };
		}
		public string RenderWindowContent () {
			return String.Join("", this._exceptions);
		}
		public void AddRenderedException(string dumpedCode) {
			this._exceptions.Add(dumpedCode);
		}
	}
}
