using Desharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace Desharp {
    public class Module: IHttpModule {
        public string ModuleName {
            get { return "Desharp"; }
        }
        public Module () { }
        public void Dispose () { }
        public void Init (HttpApplication application) {
            application.BeginRequest += delegate (object o, EventArgs e) {
				Dispatcher.GetCurrent().WebRequestBegin();
            };
			application.AcquireRequestState += delegate (object o, EventArgs e) {
				Dispatcher.GetCurrent().WebRequestSessionBegin();
			};
			application.PostRequestHandlerExecute += delegate (object o, EventArgs e) {
				Dispatcher.GetCurrent().WebRequestSessionEnd();
			};
			application.EndRequest += delegate (object o, EventArgs e) {
				// be carefull, EndRequest event is sometimes called twice (...if there is exception in your application)
				Dispatcher dispatcher = Dispatcher.GetCurrent(false);
				if (dispatcher is Dispatcher) {
					dispatcher.WebRequestEnd();
					Dispatcher.Remove();
				}
			};
			application.Error += delegate (object o, EventArgs e) {
				Dispatcher.GetCurrent().WebRequestError();
			};
		}
    }
}
