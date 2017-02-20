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
                Debug.RequestBegin();
            };
            application.EndRequest +=delegate (object o, EventArgs e) {
                Debug.RequestEnd();
            };
        }
    }
}
