using System.Collections.Generic;

namespace Desharp {
	internal struct RenderingCollection {
		internal StackTraceItem? ErrorFileStackTrace;
		internal List<StackTraceItem> AllStackTraces;
		internal List<string[]> Headers;
		internal bool Catched;
		internal string ExceptionHash;
		internal string ExceptionType;
		internal string ExceptionMessage;
		internal string CausedByHash;
		internal string CausedByType;
		internal string CausedByMessage;
	}
}
