using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Desharp {
	internal struct ExceptionToRender {
		public Exception Exception;
		public Exception CausedBy;
		public bool Catched;
	}
}
