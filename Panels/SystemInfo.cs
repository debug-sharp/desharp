using Desharp.Core;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;

namespace Desharp.Panels {
	[ComVisible(true)]
	public class SystemInfo: IPanel {
		public static string PanelName = "systeminfo";
		public int[] DefaultWindowSizes => new int[] { 600, 600 };
		public bool AddIfEmpty => true;
		public string IconValue => SystemInfo.PanelName;
		public string Name => SystemInfo.PanelName;
		public PanelIconType PanelIconType => PanelIconType.Class;
		public PanelType PanelType => PanelType.BarBtnAndWindow;

		protected string title = "";
		protected StringBuilder content = new StringBuilder();

		public string[] RenderBarTitle () {
			this.completeTitleAndContent();
			return new string[] { this.title, "System Info" };
		}
		public string RenderWindowContent () {
			return this.content.ToString();
		}
		protected virtual void completeTitleAndContent () {
			string msFormat = "0.###";
			string mbFormat = "0.00";
			string bFormat = "0,000.###";
			CultureInfo formatInfo = new CultureInfo("en-US");
			Process currentProcess = Process.GetCurrentProcess();
			HttpContext context = HttpContext.Current;
			string ramPeakWorking = (currentProcess.PeakWorkingSet64 / 1048576.0)
				.ToString(mbFormat, formatInfo) + " MB (" + currentProcess.PeakWorkingSet64.ToString(bFormat, formatInfo) + " bytes)";
			string ramPeakPaged = (currentProcess.PeakPagedMemorySize64 / 1048576.0)
				.ToString(mbFormat, formatInfo) + " MB (" + currentProcess.PeakPagedMemorySize64.ToString(bFormat, formatInfo) + " bytes)";
			string requestTime = (DateTime.Now - context.Timestamp).Milliseconds
				.ToString(msFormat, formatInfo) + " ms";
			long gcTotalMemoryLong = GC.GetTotalMemory(true);
			string gcTotalMemory = (gcTotalMemoryLong / 1048576.0)
				.ToString(mbFormat, formatInfo) + " MB (" + gcTotalMemoryLong.ToString(bFormat, formatInfo) + " bytes)";
            string appPoolId = System.Environment.GetEnvironmentVariable(
                "APP_POOL_ID", EnvironmentVariableTarget.Process
            );
            StringBuilder modulesStr = new StringBuilder();
            HttpModuleCollection iisModulesNames = HttpContext.Current.ApplicationInstance.Modules;
            string iisModulesName;
            string separator = "";
            for (int i = 0, l = iisModulesNames.Keys.Count; i < l; i += 1) {
                iisModulesName = iisModulesNames.Keys[i];
                int commaSpacePos = iisModulesName.IndexOf(", ");
                if (commaSpacePos != -1) 
                    iisModulesName = iisModulesName.Substring(0, commaSpacePos).Replace(".", "\n\t.");
                modulesStr.Append(separator + iisModulesName);
                separator = "<br />";
            }
			this.title = requestTime;
			this.content.Append(@"<table class=""system-info""><tbody>");
			string fullUrl = HttpUtility.HtmlEncode(context.Request.Url.AbsoluteUri.ToString());
			fullUrl = fullUrl.Replace("?", "\n\t?").Replace("&amp;", "\n\t&amp;");
			this
				.addContentTableRow("URL", fullUrl, "url")
				.addContentTableRow("Execution time", requestTime)
				.addContentTableRow("GC RAM", gcTotalMemory)
				.addContentTableRow("Server working RAM peak", ramPeakWorking)
				.addContentTableRow("Server pager RAM peak", ramPeakPaged)
				.addContentTableRow("Your IP", context.Request.ServerVariables["REMOTE_ADDR"])
				.addContentTableRow("HTTP method / response code", context.Request.HttpMethod.ToString() + " / " + context.Response.StatusCode.ToString())
				.addContentTableRow("Runtime Version", Environment.Version.ToString())
				.addContentTableRow("ASP.NET Version", typeof(Page).Assembly.GetName().Version.ToString())
				.addContentTableRow("Desharp", Assembly.GetExecutingAssembly().GetName().Version.ToString())
				.addContentTableRow("Server", context.Request.ServerVariables["SERVER_SOFTWARE"])
				.addContentTableRow("Application Pool", appPoolId)
                .addContentTableRow("Loaded Modules", modulesStr.ToString(), "modules");
			this.content.Append("</tbody></table>");
		}
		protected virtual SystemInfo addContentTableRow (string labelText, string contenText, string cssClass = null) {
			this.content.Append(
                "<tr" + (
                    String.IsNullOrEmpty(cssClass) ? "" : @" class=""" + cssClass + @""""
                ) + "><th>" 
                    + labelText 
                + "</th><td>" 
                    + contenText.ToString() 
                + "</td></tr>"
            );
			return this;
		}
	}
}
