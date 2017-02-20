using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using Desharp.Core;
using System.Web.Script.Serialization;

namespace Desharp.Renderers {
	enum StackTraceFormat {
		Html,
		Text,
		Json
	}
    internal class StackTrace {
        
		internal static string GetExternalCodeDescription () {
			return Core.Environment.GetOutput() == OutputType.Html ? "<i>[External code]</i>" : "External code";
		}
		internal static string GetUnknownLineDescription () {
			return Core.Environment.GetOutput() == OutputType.Html ? "<i>[?]</i>" : "?";
		}
		internal static string RenderStackRecordResult(RenderingCollection preparedResult, bool fileSystemLog = true, bool htmlOut = false) {
			string headersStr = StackTrace._renderHttpHeaders(preparedResult.Headers);
			string[] threadOrRequestIdStrs;
			if (Core.Environment.Type == EnvironmentType.Web) {
				threadOrRequestIdStrs = new string[] { "Request ID", Tools.GetRequestId().ToString() };
			} else {
				threadOrRequestIdStrs = new string[] { "Thread ID", Tools.GetThreadId().ToString() };
			}
			string dateStr = String.Format("{0:yyyy-MM-dd HH:mm:ss:fff}", DateTime.Now);
			string errorFileStr = "";
			string stackTraceStr;
			bool windowsEnv = Core.Environment.Type == EnvironmentType.Windows;
			if (htmlOut) {
				if (preparedResult.ErrorFileStackTrace.HasValue) {
					errorFileStr = ErrorFile.Render(preparedResult.ErrorFileStackTrace.Value, StackTraceFormat.Html);
				}
				stackTraceStr = StackTrace._renderStackTrace(preparedResult.AllStackTraces, StackTraceFormat.Html, fileSystemLog);
				return StackTrace._renderStackRecordResultHtml(
					preparedResult, String.Join(": ", threadOrRequestIdStrs), errorFileStr, stackTraceStr, headersStr, dateStr
				);
			} else if (windowsEnv && !fileSystemLog) {
				if (preparedResult.ErrorFileStackTrace.HasValue) {
					errorFileStr = ErrorFile.Render(preparedResult.ErrorFileStackTrace.Value, StackTraceFormat.Text);
				}
				stackTraceStr = StackTrace._renderStackTrace(preparedResult.AllStackTraces, StackTraceFormat.Text, fileSystemLog);
				return StackTrace._renderStackRecordResultConsoleText(
					preparedResult, String.Join(" : ", threadOrRequestIdStrs), errorFileStr, stackTraceStr, headersStr, dateStr
				);
			} else {
				stackTraceStr = StackTrace._renderStackTrace(preparedResult.AllStackTraces, StackTraceFormat.Json, fileSystemLog);
				return StackTrace._renderStackRecordResultLoggerText(
					preparedResult, String.Join(": ", threadOrRequestIdStrs), errorFileStr, stackTraceStr, headersStr, dateStr
				);
			}
        }
		private static string _renderStackRecordResultHtml (
			RenderingCollection preparedResult,
			string threadOrRequestIdStr,
			string errorFileStr,
			string stackTraceStr,
			string headersStr,
			string dateStr
		) {
			string linkValue = " href=\"ht" + "tps://www.google.com/webhp?hl=en&amp;sourceid=desharp&amp;q="
				+ HttpUtility.UrlEncode(preparedResult.ExceptionMessage) + "\"";
			string result = "<div class=\"desharp-dump\">"
				+ "<a" + linkValue + " target=\"_blank\" class=\"desharp-dump-control\">"
					+ "<span class=\"desharp-dump-id\">[" + threadOrRequestIdStr + "; Date: " + dateStr + "]</span>&nbsp;"
					+ "<span class=\"desharp-dump-msg\">"
						+ "<span>" + preparedResult.ExceptionType + "</span>&nbsp;"
						+ "<b>" + preparedResult.ExceptionMessage + "</b>"
					+ "</span>"
				+ "</a>"
				+ errorFileStr
				+ stackTraceStr;
			if (preparedResult.Headers.Count > 0) {
				result += "<table class=\"desharp-dump-hdrs\">"
					+ "<thead><tr><th colspan=\"2\">HTTP Headers:</th></tr></thead>"
					+ "<tbody>" + headersStr + "</tbody>"
				+ "</table>";
			}
			result += "</div>";
			return result;
		}
		private static string _renderStackRecordResultConsoleText (
			RenderingCollection preparedResult,
			string threadOrRequestIdStr,
			string errorFileStr,
			string stackTraceStr,
			string headersStr,
			string dateStr
		) {
			string result = preparedResult.ExceptionType + ":";
			result += System.Environment.NewLine + "   Message   : " + preparedResult.ExceptionMessage;
			result += System.Environment.NewLine + "   Time      : " + dateStr;
			result += System.Environment.NewLine + "   " + threadOrRequestIdStr;
			if (errorFileStr.Length > 0) {
				string file = preparedResult.ErrorFileStackTrace.Value.File;
				string line = preparedResult.ErrorFileStackTrace.Value.Line;
				result += System.Environment.NewLine + "   File      : " + file + ":" + line
					+ System.Environment.NewLine + errorFileStr;
			}
			result += System.Environment.NewLine + "   Callstack: " + stackTraceStr;
			return result;
		}
		private static string _renderStackRecordResultLoggerText (
			RenderingCollection preparedResult,
			string threadOrRequestIdStr,
			string errorFileStr,
			string stackTraceStr,
			string headersStr,
			string dateStr
		) {
			Regex r1 = new Regex(@"\r");
			Regex r2 = new Regex(@"\n");
			string message = r1.Replace(preparedResult.ExceptionMessage, "");
			message = r2.Replace(message, '\\' + "n");
			string result = "Time: " + dateStr + " | " + threadOrRequestIdStr;
			if (stackTraceStr.Length > 0) result += " | Callstack: " + stackTraceStr;
			if (headersStr.Length > 0) result += " | Request Headers: " + stackTraceStr;
			result += " | Type: " + preparedResult.ExceptionType + " | Message: " + message;
			return result;
		}
		private static string _renderHttpHeaders (Dictionary<string, string> headers, bool fileSystemLog = true) {
			StringBuilder result = new StringBuilder();
			if (Core.Environment.GetOutput() == OutputType.Html) {
				foreach (var item in headers) {
					result.Append(
						"<tr>"
							+ "<th>" + item.Key + "</th>"
							+ "<td>" + item.Value + "</td>"
						+ "</tr>"
					);
				}
			} else {
				try {
					result.Append(
						new JavaScriptSerializer().Serialize(headers)
					);
				} catch (Exception e) { }
			}
			return result.ToString();
		}
		private static string _renderStackTrace (List<StackTraceItem> stackTrace, StackTraceFormat format, bool fileSystemLog = true) {
			List<string> result = new List<string>();
			int counter = 0;
			int[] textLengths = new int[] { 0, 0};
			if (format == StackTraceFormat.Text) {
				foreach (StackTraceItem stackTraceItem in stackTrace) {
					if (stackTraceItem.Method.Length > textLengths[0]) textLengths[0] = stackTraceItem.Method.Length;
					if (stackTraceItem.File.Length > textLengths[1]) textLengths[1] = stackTraceItem.File.Length;
				}
			}
			foreach (StackTraceItem stackTraceItem in stackTrace) {
				result.Add(
					StackTrace._renderStackTraceLine(
						stackTraceItem, counter, format, textLengths
					)
				);
				counter++;
			}
			if (format == StackTraceFormat.Html) {
                string resultStr = "<table class=\"desharp-dump-dtls\">"
                    + "<thead><tr><th colspan=\"3\">Call stack:</th></tr></thead>"
                    + "<tbody>" + String.Join("", result.ToArray()) + "</tbody>"
                + "</table>";
                if (!fileSystemLog) {
                    resultStr += "<script>"
                        + "(function(){var a=document.getElementsByTagName(\"table\"),b={},c=[],d={},e={};for(var i=0,l=a.length;i<l;i+=1){b=a[i];if(b.className.indexOf(\"desharp-dump-dtls\")>-1){c=b.getElementsByTagName(\"td\");for(var j=0,k=c.length;j<k;j+=1){d=c[j];if(d.className.indexOf('mthd')>-1){e=d.getElementsByTagName(\"i\")[0];e.style.width=e.offsetWidth+'px'}}b.className=b.className+\" nowrap\";break}}})();"
                    + "</script>";
                }
                return resultStr;
            } else if (format == StackTraceFormat.Text) {
				return System.Environment.NewLine + "      " + String.Join(System.Environment.NewLine + "      ", result.ToArray());
			} else if (format == StackTraceFormat.Json) {
				return "[" + String.Join(",", result.ToArray()) + "]";
			}
			return "";
		}
		private static string _renderStackTraceLine(StackTraceItem stackTraceItem, int counter, StackTraceFormat format, int[] textLengths) {
			string result = "";
			bool fileDefined = stackTraceItem.File.Length > 0 && stackTraceItem.File != StackTrace.GetExternalCodeDescription();
			if (format == StackTraceFormat.Html) {
				string fileNameLink = "";
				if (fileDefined) {
					fileNameLink = "<a href=\"editor://open/?file=" + HttpUtility.UrlEncode(stackTraceItem.File)
						+ "&line=" + stackTraceItem.Line
						+ "&editor=" + Tools.Editor
						+ "\">" + stackTraceItem.File + "</a>";
				}
				result = "<tr>"
					+ "<td class=\"flnm\">" + fileNameLink + "</td>"
					+ "<td class=\"ln\">" + stackTraceItem.Line + "</td>"
					+ "<td class=\"mthd" + (counter > 0 ? "" : " first") + "\"><i>" + stackTraceItem.Method + "</i></td>"
				+ "</tr>";
			} else if (format == StackTraceFormat.Text) {
				if (fileDefined) {
					result = stackTraceItem.Method + Tools.SpaceIndent(textLengths[0] - stackTraceItem.Method.Length, false)
						+ " " + stackTraceItem.File + Tools.SpaceIndent(textLengths[1] - stackTraceItem.File.Length, false)
						+ " " + stackTraceItem.Line;
				} else {
					result = stackTraceItem.Method + Tools.SpaceIndent(textLengths[0] - stackTraceItem.Method.Length, false)
						+ " " + stackTraceItem.File + StackTrace.GetExternalCodeDescription();
				}
			} else if (format == StackTraceFormat.Json) {
				JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
				result = "{method:" + jsonSerializer.Serialize(stackTraceItem.Method);
				if (fileDefined) {
					result += ",file:" + jsonSerializer.Serialize(stackTraceItem.File);
					result += ",line:" + stackTraceItem.Line;
				}
				result += "}";
			}
			return result;
		}
	}
}