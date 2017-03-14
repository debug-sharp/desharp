using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using Desharp.Core;
using System.Web.Script.Serialization;
using Desharp.Completers;
using System.Web.UI;

namespace Desharp.Renderers {
    internal class Exceptions {
		internal static List<string> RenderExceptions (Exception e, bool fileSystemLog = true, bool htmlOut = false, bool catched = true) {
			List<string> result = new List<string>();
			Dictionary<string, ExceptionToRender> exceptions = Completers.StackTrace.CompleteInnerExceptions(e, catched);
			List<string[]> headers = new List<string[]>();
			if (Dispatcher.EnvType == EnvType.Web) headers = HttpHeaders.CompletePossibleHttpHeaders();
			int i = 0;
			foreach (var item in exceptions) {
				//if (item.Value.Exception.StackTrace == null) continue; // why ??!!?????!? exception has always a stacktrace hasn't it?
				RenderingCollection preparedResult = Completers.StackTrace.RenderStackTraceForException(
					item.Value, fileSystemLog, htmlOut, i
				);
				preparedResult.Headers = headers;
				result.Add(Exceptions._renderStackRecordResult(
					preparedResult, fileSystemLog, htmlOut
				));
				i++;
			}
			return result;
		}
		internal static string RenderCurrentApplicationPoint (string message = "", string exceptionType = "", bool fileSystemLog = true, bool htmlOut = false) {
			RenderingCollection preparedResult = Completers.StackTrace.CompleteStackTraceForCurrentApplicationPoint(message, exceptionType, fileSystemLog, htmlOut);
			List<string[]> headers = new List<string[]>();
			if (Dispatcher.EnvType == EnvType.Web) {
				headers = HttpHeaders.CompletePossibleHttpHeaders();
			}
			preparedResult.Headers = headers;
			return Renderers.Exceptions._renderStackRecordResult(preparedResult, fileSystemLog, htmlOut);
		}
		internal static string GetExternalCodeDescription () {
			return "External code";
		}

		private static string _renderStackRecordResult(RenderingCollection preparedResult, bool fileSystemLog = true, bool htmlOut = false) {
			bool webEnv = Dispatcher.EnvType == EnvType.Web;
			string headersStr = Exceptions._renderDataTableRows(preparedResult.Headers, fileSystemLog, false);
			
			string dateStr = String.Format("{0:yyyy-MM-dd HH:mm:ss:fff}", DateTime.Now);
			string errorFileStr = "";
			string stackTraceStr;
			if (webEnv && htmlOut && !fileSystemLog) {
				if (preparedResult.ErrorFileStackTrace.HasValue) {
					errorFileStr = ErrorFile.Render(preparedResult.ErrorFileStackTrace.Value, StackTraceFormat.Html);
				}
				stackTraceStr = Exceptions._renderStackTrace(preparedResult.AllStackTraces, StackTraceFormat.Html, fileSystemLog);
				return Exceptions._renderStackRecordResultHtmlResponse(
					preparedResult, errorFileStr, stackTraceStr, headersStr, dateStr
				);
			} else if (htmlOut && fileSystemLog) {
				stackTraceStr = Exceptions._renderStackTrace(preparedResult.AllStackTraces, StackTraceFormat.Html, fileSystemLog);
				return Exceptions._renderStackRecordResultHtmlLog(
					preparedResult, stackTraceStr, headersStr, dateStr
				);
			} else if (!webEnv && !htmlOut && !fileSystemLog) {
				if (preparedResult.ErrorFileStackTrace.HasValue) {
					errorFileStr = ErrorFile.Render(preparedResult.ErrorFileStackTrace.Value, StackTraceFormat.Text);
				}
				stackTraceStr = Exceptions._renderStackTrace(preparedResult.AllStackTraces, StackTraceFormat.Text, fileSystemLog);
				return Exceptions._renderStackRecordResultConsoleText(
					preparedResult, "Thread ID : " + Tools.GetThreadId().ToString(), errorFileStr, stackTraceStr, headersStr, dateStr
				);
			} else {
				stackTraceStr = Exceptions._renderStackTrace(preparedResult.AllStackTraces, StackTraceFormat.Json, fileSystemLog);
				return Exceptions._renderStackRecordResultLoggerText(
					preparedResult, "Thread ID: " + Tools.GetThreadId().ToString(), errorFileStr, stackTraceStr, headersStr, dateStr
				);
			}
        }
		private static string _renderStackRecordResultHtmlResponse (
			RenderingCollection preparedResult,
			string errorFileStr,
			string stackTraceStr,
			string headersStr,
			string dateStr
		) {
			string linkValue = "ht" + "tps://www.google.com/search?sourceid=desharp&gws_rd=us&q="
				+ HttpUtility.UrlEncode(preparedResult.ExceptionMessage);
			string causedByMsg = preparedResult.CausedByMessage;
			if (causedByMsg.Length > 50) causedByMsg = causedByMsg.Substring(0, 50) + "...";
			string result = @"<div class=""exception"">"
				+ @"<div class=""head"">"
					+ @"<div class=""type"">" + preparedResult.ExceptionType + " (Hash code: " + preparedResult.ExceptionHash + ")</div>"
					+ @"<a href=""" + linkValue + @""" target=""_blank"">"
							+ preparedResult.ExceptionMessage
					+ "</a>"
					+ @"<div class=""info"">"
						+ "Catched: " + (preparedResult.Catched ? "yes" : "no")
						+ (preparedResult.CausedByHash.Length > 0 
							? ", Caused by: " + preparedResult.CausedByType + " (Hash code: " + preparedResult.CausedByHash + ", Message: " + causedByMsg + ")" 
							: "")
					+ "</div>"
				+ "</div>"
				+ errorFileStr
				+ stackTraceStr;
			if (preparedResult.Headers.Count > 0) {
				result += Exceptions._renderHtmlDataTable("HTTP Headers:", headersStr);
			}
			result += Exceptions._renderHtmlDataTable(
				"Loaded assemblies:", 
				Exceptions._renderDataTableRows(LoadedAssemblies.CompleteLoadedAssemblies(), false, true)
			);
			result += Exceptions._renderHtmlResponseFooterInfo();
			result += "</div>";
			return result;
		}
		private static string _renderStackRecordResultHtmlLog (
			RenderingCollection preparedResult,
			string stackTraceStr,
			string headersStr,
			string dateStr
		) {
			string linkValue = "ht" + "tps://www.google.com/webhp?hl=en&amp;sourceid=desharp&amp;q="
				+ HttpUtility.UrlEncode(preparedResult.ExceptionMessage);
			string result = @"<div class=""record"">"
				+ @"<a href=""" + linkValue + @""" target=""_blank"" class=""control"">"
					+ @"<span class=""req"">Request ID: " + Tools.GetRequestId().ToString() + "</span>"
					+ @"<span class=""thread"">Thread ID: " + Tools.GetThreadId().ToString() + "</span>"
					+ @"<span class=""date"">Date: " + dateStr + "</span>"
					+ @"<span class=""type"">" + preparedResult.ExceptionType + "</span>"
					+ @"<span class=""msg"">" + preparedResult.ExceptionMessage + "</span>"
				+ "</a>"
				+ stackTraceStr;
			if (preparedResult.Headers.Count > 0) {
				result += Exceptions._renderHtmlDataTable("HTTP Headers:", headersStr);
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
		private static string _renderDataTableRows (List<string[]> headers, bool fileSystemLog = true, bool firstRowAsHead = false) {
			StringBuilder result = new StringBuilder();
			if (Dispatcher.GetCurrent().Output == OutputType.Html) {
				int index = 0;
				foreach (string[] rowData in headers) {
					result.Append("<tr>");
					for (int i = 0, l = rowData.Length; i < l; i += 1) {
						if (index == 0 && firstRowAsHead) {
							result.Append("<th>" + rowData[i] + "</th>");
						} else {
							result.Append("<td>" + rowData[i] + "</td>");
						}
					}
					result.Append("</tr>");
					index++;
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
		private static string _renderHtmlDataTable (string title, string tableRows) {
			return @"<div class=""table""><b class=""title"">" + title + @"</b><div class=""data"" ><table>" + tableRows + "</table></div></div>";
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
					Exceptions._renderStackTraceLine(stackTraceItem, format, textLengths)
				);
				counter++;
			}
			if (format == StackTraceFormat.Html) {
                string resultStr = @"<div class=""table callstack""><b class=""title"">Call stack:</b>"
					+ @"<div class=""calls""><div><table>" + String.Join("", result.ToArray()) + "</table></div></div>"
				+ "</div>";
                return resultStr;
            } else if (format == StackTraceFormat.Text) {
				return System.Environment.NewLine + "      " + String.Join(System.Environment.NewLine + "      ", result.ToArray());
			} else if (format == StackTraceFormat.Json) {
				return "[" + String.Join(",", result.ToArray()) + "]";
			}
			return "";
		}
		private static string _renderStackTraceLine(StackTraceItem stackTraceItem, StackTraceFormat format, int[] textLengths) {
			string result = "";
			bool fileDefined = stackTraceItem.File.Length > 0 && stackTraceItem.File != Exceptions.GetExternalCodeDescription();
			if (format == StackTraceFormat.Html) {
				string fileNameLink = "";
				if (fileDefined) {
					fileNameLink = @"<a href=""editor://open/?file=" + HttpUtility.UrlEncode(stackTraceItem.File)
						+ "&line=" + stackTraceItem.Line
						+ "&editor=" + Tools.Editor
						+ @""">" + Tools.RelativeSourceFullPath(stackTraceItem.File) + "</a>";
				}
				result = (fileNameLink.Length > 0 ? @"<tr class=""known""> " : "<tr>")
					+ @"<td class=""file"">" + fileNameLink + "</td>"
					+ @"<td class=""line"">" + stackTraceItem.Line + "</td>"
					+ @"<td class=""method""><i>" + stackTraceItem.Method.Replace(".", "&#8203;.") + " </i></td>"
				+ "</tr>";
			} else if (format == StackTraceFormat.Text) {
				if (fileDefined) {
					result = stackTraceItem.Method + Tools.SpaceIndent(textLengths[0] - stackTraceItem.Method.Length, false)
						+ " " + stackTraceItem.File + Tools.SpaceIndent(textLengths[1] - stackTraceItem.File.Length, false)
						+ " " + stackTraceItem.Line;
				} else {
					result = stackTraceItem.Method + Tools.SpaceIndent(textLengths[0] - stackTraceItem.Method.Length, false)
						+ " " + stackTraceItem.File + Exceptions.GetExternalCodeDescription();
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
		private static string _renderHtmlResponseFooterInfo () {
			return @"<div class=""version"">"
				+ "<b>.NET Framework Version:</b> " + Environment.Version.ToString() + "; "
				+ "<b>ASP.NET Version:</b> " + typeof(Page).Assembly.GetName().Version.ToString() + "; "
				+ "<b>Server Version:</b> " + HttpContext.Current.Request.ServerVariables["SERVER_SOFTWARE"] + "; "
				+ "<b>Desharp Version:</b> " + Debug.Version.ToString()
			+ "</div>";
		}
	}
}