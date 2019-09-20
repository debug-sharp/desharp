using System;

namespace Desharp {
	internal struct ExceptionToRender {
		internal Exception Exception;
		internal Exception CausedBy;
		internal bool Catched;
	}
}
