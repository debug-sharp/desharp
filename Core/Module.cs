using Desharp.Core;
using System;
using System.Web;

namespace Desharp {
	/// <summary>
	/// ASP.NET Http module to dispatch Desharp separately from original application.
	/// </summary>
	public class Module: IHttpModule {
		/// <summary>
		/// Unique module name
		/// </summary>
		public string ModuleName {
			get { return "Desharp"; }
		}
		/// <summary>
		/// Empty constructor - required by IHttpModule interface
		/// </summary>
		public Module() { }
		/// <summary>
		/// Empty method Dispose - required by IHttpModule interface
		/// </summary>
		public void Dispose() { }
		/// <summary>
		/// Application request events initialization - BeginRequest, AcquireRequestState, PostRequestHandlerExecute, PreSendRequestHeaders, EndRequest, Error and Disposed
		/// </summary>
		/// <param name="application">ASP.NET aplication instance.</param>
		public void Init(HttpApplication application) {
			application.BeginRequest += delegate (object o, EventArgs e) {
				try {
					Dispatcher.GetCurrent().WebRequestBegin();
				} catch { }
			};
			application.AcquireRequestState += delegate (object o, EventArgs e) {
				try {
					Dispatcher.GetCurrent().WebRequestSessionBegin();
				} catch { }
			};
			application.PostRequestHandlerExecute += delegate (object o, EventArgs e) {
				try {
					Dispatcher.GetCurrent().WebRequestSessionEnd();
				} catch { }
			};
			application.EndRequest += delegate (object o, EventArgs e) {
				try {
					// be carefull, EndRequest event is sometimes called twice (...if there is exception in your application)
					Dispatcher dispatcher = Dispatcher.GetCurrent(false);
					if (dispatcher is Dispatcher) {
                        dispatcher.GetFireDump().CloseHeaders();
                        dispatcher.WebRequestEnd();
						Dispatcher.Remove();
					}
				} catch { }
			};
			application.Error += delegate (object o, EventArgs e) {
				try {
					Dispatcher.GetCurrent().WebRequestError();
				} catch { }
			};
			application.Disposed += delegate (object o, EventArgs e) {
				try {
					Dispatcher.Disposed();
				} catch { }
			};
		}
	}
}