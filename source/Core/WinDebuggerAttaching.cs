using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Desharp.Core {
	internal class WinDebuggerAttachingEventArgs: EventArgs {
		internal bool Attached = false;
	}
	internal class WinDebuggerAttaching {
		internal event EventHandler<WinDebuggerAttachingEventArgs> Changed = null;
		internal static int CheckIntervalMiliseconds = 1000;
		protected Thread bgCheckThread = null;
		internal static WinDebuggerAttaching GetInstance () {
			return new WinDebuggerAttaching();
		}
		internal WinDebuggerAttaching () {
			if (this.bgCheckThread is Thread) return;
			this.bgCheckThread = new Thread(this.checkHandler);
			this.bgCheckThread.IsBackground = true;
			this.bgCheckThread.Start();
		}
		protected void checkHandler () {
			bool lastValue;
			while (true) {
				lastValue = System.Diagnostics.Debugger.IsAttached;
				while (lastValue == System.Diagnostics.Debugger.IsAttached) {
					Thread.Sleep(WinDebuggerAttaching.CheckIntervalMiliseconds);
				}
				this.callChangeEventHandler();
			}
		}
		protected void callChangeEventHandler () {
			if (this.Changed != null) {
				this.Changed.Invoke(
					this,
					new WinDebuggerAttachingEventArgs {
						Attached = System.Diagnostics.Debugger.IsAttached
					}
				);
			}
		}
	}
}
