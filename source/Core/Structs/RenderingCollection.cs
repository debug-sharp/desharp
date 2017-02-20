using System.Collections.Generic;

namespace Desharp {
	internal struct RenderingCollection {
		public StackTraceItem? ErrorFileStackTrace;
		public List<StackTraceItem> AllStackTraces;
		public Dictionary<string, string> Headers;
		public string ExceptionType;
		public string ExceptionMessage;
	}
}
