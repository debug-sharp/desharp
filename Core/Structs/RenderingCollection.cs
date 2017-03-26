using System.Collections.Generic;

namespace Desharp {
	internal struct RenderingCollection {
		public StackTraceItem? ErrorFileStackTrace;
		public List<StackTraceItem> AllStackTraces;
		public List<string[]> Headers;
		public bool Catched;
		public string ExceptionHash;
		public string ExceptionType;
		public string ExceptionMessage;
		public string CausedByHash;
		public string CausedByType;
		public string CausedByMessage;
	}
}
