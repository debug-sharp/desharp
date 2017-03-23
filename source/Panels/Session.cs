using Desharp.Core;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace Desharp.Panels {
	public class Session: Abstract {
		public static string PanelName = "session";
		public static int DumpDepth = 0;
		public new int[] DefaultWindowSizes {
			get { return new int[] { 400, 300 }; }
		}
		public new bool AddIfEmpty {
			get { return true; }
		}
		public override string IconValue {
			get { return Session.PanelName; }
		}
		public override string Name {
			get { return Session.PanelName; }
		}
		public override PanelIconType PanelIconType {
			get { return PanelIconType.Class; }
		}
		protected int count = 0;
		protected StringBuilder content = new StringBuilder();
		public void SessionEnd () {
			HttpSessionState session = HttpContext.Current.Session;
			this.content.Append(@"<h4 style=""margin:0 0 2px 0;"">Configuration:</h4>");
			this.content.Append(@"<p style=""margin: 2px 0 5px 0;font-size:14px;"">");
			this.content.Append("&nbsp;&nbsp;Timeout: " + session.Timeout + "<br />");
			this.content.Append("&nbsp;&nbsp;Mode: " + session.Mode.ToString() + "<br />");
			this.content.Append("&nbsp;&nbsp;CookieMode: " + session.CookieMode.ToString());
			if (session.Count == 0) this.content.Append("<br />&nbsp;&nbsp;no items");
			this.content.Append("</p>");
			this.count = session.Count;
			if (session.Count > 0) { 
				this.content.Append(@"<h4 style=""margin:5px 0 2px 0;"">Items:</h4>");
				string sessionKey;
				int depth = Session.DumpDepth > 0 ? Session.DumpDepth : Dispatcher.DumpDepth;
				string dumpBeginCode = @"<div class=""desharp-dump"">";
				int beginCodePos = 0;
				string dumpCode;
				for (int i = 0, l = session.Count; i < l; i += 1) {
					sessionKey = session.Keys[i];
					dumpCode = Debug.Dump(session[sessionKey], new DumpOptions {
						Depth = depth,
						Return = true
					});
					beginCodePos = dumpCode.IndexOf(dumpBeginCode);
					if (beginCodePos == -1) beginCodePos = 0;
					dumpCode = dumpCode.Substring(0, beginCodePos) + 
						dumpCode.Substring(beginCodePos, dumpBeginCode.Length) +
						@"<span class=""string"">""" + sessionKey + @"""</span><s>:&nbsp;</s>" +
						dumpCode.Substring(beginCodePos + dumpBeginCode.Length);
					this.content.Append(dumpCode);
				}
			}
		}
		public override string RenderBarText () {
			return "Session (" + this.count + ")";
		}
		public override string RenderWindowContent () {
			return this.content.ToString();
		}
	}
}
