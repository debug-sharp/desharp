namespace Desharp.Panels {
	/// <summary>
	/// Web debug bar panel base class.
	/// </summary>
	public abstract class Abstract {
		/// <summary>
		/// Unique bar panel name, required.
		/// </summary>
		public abstract string Name { get; }
		/// <summary>
		/// If true, panel will be displayed also in cases when it's content is empty string, false to not display the panel if empty string, false by default.
		/// </summary>
		public bool AddIfEmpty { get { return false; } }
		/// <summary>
		/// Default window sizes when panel floating window is displayed at first time, 300x200 by default.
		/// </summary>
		public int[] DefaultWindowSizes { get { return new int[] { 300, 200 }; } }
		/// <summary>
		/// Panel type, behaviour. You can specify various types, bar panel with custom resizeable floating window defined with custom content by default.
		/// </summary>
		public PanelType PanelType { get { return PanelType.BarBtnAndWindow; } }
		/// <summary>
		/// Panel icon type, should by "css class" for internal desharp panels or "code" for panels defined icon in IconValue property as HTML code with base64 encoded image in <img /> tag.
		/// </summary>
		public abstract PanelIconType PanelIconType { get; }
		/// <summary>
		/// Icon value, should be css class for internal Desharp panels or HTML code icon value as <img /> tag with base64 encoded icon image.
		/// </summary>
		public abstract string IconValue { get; }
		/// <summary>
		/// Constructor, nothing special, just point for you to start any custom code every request.
		/// </summary>
		public Abstract () { }
		/// <summary>
		/// Called at session begin request event, called before Controller/action execution.
		/// </summary>
		public void SessionBegin () { }
		/// <summary>
		/// Called at session end request event, called after Controller/action execution.
		/// </summary>
		public void SessionEnd () { }
		/// <summary>
		/// Render text content for web bar button.
		/// </summary>
		/// <returns>Text content for web bar button</returns>
		public abstract string RenderBarText ();
		/// <summary>
		/// Render any string content for bar floating window, if content will be empty string, bar panel wil not be rendered by default.
		/// </summary>
		/// <returns>Bar panel floating window content.</returns>
		public abstract string RenderWindowContent ();
	}
}
