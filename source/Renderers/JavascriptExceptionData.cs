using Desharp.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

namespace Desharp.Renderers {
	internal class JavascriptExceptionData {
		internal static string RenderLogedExceptionData (Dictionary<string, string> jsExceptionProps, bool htmlOut = false) {
			StringBuilder dataItems = new StringBuilder();
			long crt = Tools.GetRequestId();
			string requestDate = String.Format("{0:yyyy-MM-dd HH:mm:ss:fff}", new DateTime(crt));
			if (htmlOut) {
				string jsExceptionPropValue;
				foreach (var jsExceptionProp in jsExceptionProps) {
					if (jsExceptionProp.Key == "message") continue;
					jsExceptionPropValue = jsExceptionProp.Value;
					if (jsExceptionProp.Key == "stack") {
						jsExceptionPropValue = jsExceptionPropValue
							.Replace("\r", "")
							.Replace("\n", "<br />");
					}
					dataItems.Append(
						"<tr>"
							+ "<td>" + jsExceptionProp.Key + "</td>"
							+ "<td>" + jsExceptionPropValue + "</td>"
						+ "</tr>"
					);
				}
				return "<div class=\"logger-record\">"
					+ "<a class=\"logger-record-control\">"
						+ "<span class=\"logger-record-id\">[Date: " + requestDate + "]</span>&nbsp;"
						+ "<span class=\"logger-record-msg\"><b>"
							+ jsExceptionProps["message"]
								.Replace("&", "&amp;")
								.Replace("<", "&lt;")
								.Replace(">", "&gt;")
						+ "</b></span>"
					+ "</a>"
					+ "<table class=\"logger-record-hdrs\">"
						+ dataItems.ToString()
							.Replace("&", "&amp;")
							.Replace("<", "&lt;")
							.Replace(">", "&gt;")
					+ "</table>"
				+ "</div>";
			} else {
				jsExceptionProps.Add("date", requestDate);
				string result = "";
				try {
					result = new JavaScriptSerializer().Serialize(jsExceptionProps);
				} catch (Exception e) { }
				return result;
			}
		}
	}
}
