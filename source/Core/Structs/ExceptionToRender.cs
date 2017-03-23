using System;

namespace Desharp {
	internal struct ExceptionToRender {
		public Exception Exception;
		public Exception CausedBy;
		public bool Catched;
	}
}
