using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desharp.Core {
	internal class AppExitWatcher {
		#region unmanaged
		[DllImport("Kernel32")]
		public static extern bool SetConsoleCtrlHandler (HandlerRoutine Handler, bool Add);
		public delegate bool HandlerRoutine (AppExitType CtrlType);
		#endregion
	}
}
