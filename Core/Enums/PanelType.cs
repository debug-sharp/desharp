using System;

namespace Desharp {
	/// <summary>
	/// Desharp web panel type.
	/// </summary>
	[Serializable]
	public enum PanelType {
		/// <summary>
		/// Web debug panel with text context, in bar, no window, no actions on mouse over or click.
		/// </summary>
		BarText,
		/// <summary>
		/// Web debug panel with js handler, you can specify any js function call in panel content.
		/// </summary>
		BarBtnWithJsHandler,
		/// <summary>
		/// Web debug panel with floating window with custom HTML content, used for most cases.
		/// </summary>
		BarBtnAndWindow,
		/// <summary>
		/// Web debug panel with rendered content showed on whole browser screen, used for exceptions panel.
		/// </summary>
		BarBtnAndScreen
	}
}
