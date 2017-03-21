namespace Desharp {
	internal struct StackTraceItem {
		public string Method;
		public object File; // string | string[]
		public string Line;
		public string Column;
		public StackTraceItem (StackTraceItem item) {
			this.Method = item.Method;
			this.File = item.File;
			this.Line = item.Line;
			this.Column = item.Column;
		}
	}
}
