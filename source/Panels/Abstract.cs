namespace Desharp.Panels {
	public abstract class Abstract {
		public abstract string Name { get; }
		public bool AddIfEmpty { get { return false; } }
		public int[] DefaultWindowSizes { get { return new int[] { 300, 200 }; } }
		public PanelType PanelType { get { return PanelType.BarBtnAndWindow; } }
		public abstract PanelIconType PanelIconType { get; }
		public abstract string IconValue { get; }
		public Abstract () { }
		public void SessionBegin () { }
		public void SessionEnd () { }
		public abstract string RenderBarText ();
		public abstract string RenderWindowContent ();
	}
}
