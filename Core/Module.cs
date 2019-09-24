using Desharp.Core;
using System;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.SessionState;

namespace Desharp {
	/// <summary>
	/// ASP.NET Http module to dispatch Desharp separately from original application.
	/// </summary>
	[ComVisible(true)]
	public class Module: IHttpModule/*, IRequiresSessionState*/ {
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
					Dispatcher.GetCurrent(true).WebRequestBegin();
				} catch (Exception ex) {
					Desharp.Debug.Log(ex.Message + "\r\n" + ex.StackTrace);
				}
			};
            application.PostAuthorizeRequest += delegate (object o, EventArgs e) {
                HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
            };
			application.AcquireRequestState += delegate (object o, EventArgs e) {
				try {
                    Dispatcher dispatcher = Dispatcher.GetCurrent(false);
                    if (dispatcher != null) 
					    dispatcher.WebRequestSessionBegin();
				} catch (Exception ex) {
					Desharp.Debug.Log(ex.Message + "\r\n" + ex.StackTrace);
				}
			};
			application.PostAcquireRequestState += delegate (object o, EventArgs e) {
				try {
                    Dispatcher dispatcher = Dispatcher.GetCurrent(false);
					if (dispatcher != null) 
					    dispatcher.WebRequestSessionEnd();
				} catch (Exception ex) {
					Desharp.Debug.Log(ex.Message + "\r\n" + ex.StackTrace);
				}
			};
			/*application.EndRequest += delegate (object o, EventArgs e) {
				try {
					// be carefull, EndRequest event is sometimes called twice (...if there is exception in your application)
					Dispatcher dispatcher = Dispatcher.GetCurrent(false);
					if (dispatcher is Dispatcher) {
						dispatcher.WebRequestPreSendHeaders();
						dispatcher.WebRequestPreSendBody();
						Dispatcher.Remove();
					}
				} catch (Exception ex) {
					Desharp.Debug.Log(ex.Message + "\r\n" + ex.StackTrace);
				}
			};*/
			application.PreSendRequestHeaders += delegate (object o, EventArgs e) {
				try {
                    Dispatcher dispatcher = Dispatcher.GetCurrent(false);
                    if (dispatcher != null) { 
					    dispatcher.WebRequestSessionEnd();
					    dispatcher.WebRequestPreSendHeaders();
                    }
				} catch (Exception ex) {
					Desharp.Debug.Log(ex.Message + "\r\n" + ex.StackTrace);
				}
			};
			application.PreSendRequestContent += delegate (object o, EventArgs e) {
				try {
					Dispatcher dispatcher = Dispatcher.GetCurrent(false);
                    if (dispatcher != null) {
                        dispatcher.WebRequestPreSendBody();
                        //Dispatcher.Remove();
                    }
				} catch (Exception ex) {
					Desharp.Debug.Log(ex.Message + "\r\n" + ex.StackTrace);
				}
			};
			application.Error += delegate (object o, EventArgs e) {
				try {
					Dispatcher dispatcher = Dispatcher.GetCurrent(true);
                    if (dispatcher != null)
                        dispatcher.WebRequestError();
				} catch (Exception ex) {
					Desharp.Debug.Log(ex.Message + "\r\n" + ex.StackTrace);
				}
			};
			application.Disposed += delegate (object o, EventArgs e) {
				try {
					Dispatcher.Disposed();
				} catch (Exception ex) {
					Desharp.Debug.Log(ex.Message + "\r\n" + ex.StackTrace);
				}
			};
		}
	}
}