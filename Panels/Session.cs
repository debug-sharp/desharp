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
			this.content.Append(@"<div class=""content"">");
			this.content.Append(@"<b class=""heading"">Configuration:</b>");
			this.content.Append(@"<table class=""session-configuration""><tbody>");
			this.content.Append("<tr><th>Timeout</th><td>" + session.Timeout + "</td></tr>");
			this.content.Append("<tr><th>Mode</th><td>" + session.Mode.ToString() + "</td></tr>");
			this.content.Append("<tr><th>CookieMode</th><td>" + session.CookieMode.ToString() + "</td></tr>");
			this.content.Append("</tbody></table>");
			if (session.Count == 0) this.content.Append(@"<b class=""heading"">No items</b>");
			this.content.Append("</p>");
			this.count = session.Count;
			if (session.Count > 0) { 
				this.content.Append(@"<b class=""heading"">Items:</b>");
				this.content.Append(@"<div class=""inset"">");
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
				this.content.Append("</div>");
			}
			this.content.Append("</div>");
		}
		public override string RenderBarText () {
			return "Session (" + this.count + ")";
		}
		public override string RenderWindowContent () {
			return this.content.ToString();
		}
	}
}
