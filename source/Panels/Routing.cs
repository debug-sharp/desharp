namespace Desharp.Panels {
	public class Routing: Abstract {
		public static string PanelName = "routing";
		public new int[] DefaultWindowSizes {
			get { return new int[] { 350, 250 }; }
		}
		public override string IconValue {
			get { return Routing.PanelName; }
		}
		public override string Name {
			get { return Routing.PanelName; }
		}
		public override PanelIconType PanelIconType {
			get { return PanelIconType.Class; }
		}
		public override string RenderBarText () {
			return "Routing";
		}
		public override string RenderWindowContent () {
			return "TODO:-)";
		}
	}
}
