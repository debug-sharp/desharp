using Desharp.Core;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;

namespace Desharp.Panels {
	public class SystemInfo: IPanel {
		public static string PanelName = "systeminfo";
		public int[] DefaultWindowSizes => new int[] { 430, 330 };
		public bool AddIfEmpty => true;
		public string IconValue => SystemInfo.PanelName;
		public string Name => SystemInfo.PanelName;
		public PanelIconType PanelIconType => PanelIconType.Class;
		public PanelType PanelType => PanelType.BarBtnAndWindow;

		protected string title = "";
		protected StringBuilder content = new StringBuilder();

		public void SessionBegin() { }
		public void SessionEnd() { }
		public string[] RenderBarTitle () {
			this.completeTitleAndContent();
			return new string[] { this.title, "System Info" };
		}
		public string RenderWindowContent () {
			return this.content.ToString();
		}
		protected virtual void completeTitleAndContent () {
			string format = "0.###";
			CultureInfo formatInfo = new CultureInfo("en-US");
			Process currentProcess = Process.GetCurrentProcess();
			HttpContext context = HttpContext.Current;
			string ramPeakWorking = (currentProcess.PeakWorkingSet64 / 1048576)
				.ToString(format, formatInfo) + " MB";
			string ramPeakPaged = (currentProcess.PeakPagedMemorySize64 / 1048576)
				.ToString(format, formatInfo) + " MB";
			string requestTime = (DateTime.Now - context.Timestamp).Milliseconds
				.ToString(format, formatInfo) + " ms";
			string gcTotalMemory = (GC.GetTotalMemory(true) / 1048576)
				.ToString(format, formatInfo) + " MB";
			this.title = requestTime;
			this.content.Append(@"<table class=""session-configuration""><tbody>");
			this.addContentTableRow("Execution time", requestTime)
				.addContentTableRow("GC RAM", gcTotalMemory)
				.addContentTableRow("Server working RAM peak", ramPeakWorking)
				.addContentTableRow("Server pager RAM peak", ramPeakPaged)
				.addContentTableRow("Your IP", context.Request.ServerVariables["REMOTE_ADDR"])
				.addContentTableRow("HTTP method / response code", context.Request.HttpMethod.ToString() + " / " + context.Response.StatusCode.ToString())
				.addContentTableRow("Runtime Version", Environment.Version)
				.addContentTableRow("ASP.NET Version", typeof(Page).Assembly.GetName().Version)
				.addContentTableRow("Desharp", Assembly.GetExecutingAssembly().GetName().Version)
				.addContentTableRow("Server", context.Request.ServerVariables["SERVER_SOFTWARE"]);
			this.content.Append("</tbody></table>");
		}
		protected virtual SystemInfo addContentTableRow (string labelText, object contenText) {
			this.content.Append("<tr><th>" + labelText + "</th><td>" + contenText.ToString() + "</td></tr>");
			return this;
		}
	}
}
