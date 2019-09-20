namespace Desharp {
	internal struct StackTraceItem {
		internal string Method;
		internal object File; // string | string[]
		internal string Line;
		internal string Column;
		internal StackTraceItem (StackTraceItem item) {
			this.Method = item.Method;
			this.File = item.File;
			this.Line = item.Line;
			this.Column = item.Column;
		}
	}
}
