using System;
using System.Collections.Generic;

namespace Desharp.Panels {
	public class Exceptions: Abstract {
		public static string PanelName = "exceptions";
		public override string Name {
			get { return Exceptions.PanelName; }
		}
		public new PanelType PanelType {
			get { return PanelType.BarBtnAndScreen; }
		}
		public override PanelIconType PanelIconType {
			get { return PanelIconType.Class; }
		}
		public override string IconValue {
			get { return Exceptions.PanelName; }
		}
		private List<string> _exceptions = new List<string>();
		public void AddRenderedException (string dumpedCode) {
			this._exceptions.Add(dumpedCode);
		}
		public override string RenderBarText () {
			return "Exceptions (" + this._exceptions.Count.ToString() + ")";
		}
		public override string RenderWindowContent () {
			return String.Join("", this._exceptions);
		}
	}
}
