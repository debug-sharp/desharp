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
		internal const string SELF_FILENAME = "Exceptions.cs";
		internal static List<string> RenderExceptions (Exception e, bool fileSystemLog = true, bool htmlOut = false, bool catched = true) {
			List<string> result = new List<string>();
			Dictionary<string, ExceptionToRender> exceptions = StackTrace.CompleteInnerExceptions(e, catched);
			List<string[]> headers = new List<string[]>();
			if (Dispatcher.EnvType == EnvType.Web) headers = HttpHeaders.CompletePossibleHttpHeaders();
			int i = 0;
			foreach (var item in exceptions) {
				//if (item.Value.Exception.StackTrace == null) continue; // why ??!!?????!? exception has always a stacktrace hasn't it?
				RenderingCollection preparedResult = StackTrace.RenderStackTraceForException(
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
			RenderingCollection preparedResult = StackTrace.CompleteStackTraceForCurrentApplicationPoint(message, exceptionType, fileSystemLog, htmlOut);
			List<string[]> headers = new List<string[]>();
			if (Dispatcher.EnvType == EnvType.Web) {
				headers = HttpHeaders.CompletePossibleHttpHeaders();
			}
			preparedResult.Headers = headers;
			return Exceptions._renderStackRecordResult(preparedResult, fileSystemLog, htmlOut);
		}
		private static string _renderStackRecordResult(RenderingCollection preparedResult, bool fileSystemLog = true, bool htmlOut = false) {
			bool webEnv = Dispatcher.EnvType == EnvType.Web;
			string dateStr = String.Format("{0:yyyy-MM-dd HH:mm:ss:fff}", DateTime.Now);
			string errorFileStr = "";
			string headersStr;
			string stackTraceStr;
			if (htmlOut) { 
				if (webEnv && !fileSystemLog) {
					headersStr = Exceptions._renderDataTableRows(preparedResult.Headers, htmlOut, false);
					if (preparedResult.ErrorFileStackTrace.HasValue) {
						errorFileStr = ErrorFile.Render(preparedResult.ErrorFileStackTrace.Value, StackTraceFormat.Html);
					}
					stackTraceStr = Exceptions._renderStackTrace(preparedResult.AllStackTraces, htmlOut, StackTraceFormat.Html, fileSystemLog);
					return Exceptions._renderStackRecordResultHtmlResponse(
						preparedResult, errorFileStr, stackTraceStr, headersStr, dateStr
					);
				} else {
					headersStr = Exceptions._renderDataTableRows(preparedResult.Headers, false, false);
					stackTraceStr = Exceptions._renderStackTrace(preparedResult.AllStackTraces, htmlOut, StackTraceFormat.Json, fileSystemLog);
					return Exceptions._renderStackRecordResultHtmlLog(
						preparedResult, stackTraceStr, headersStr, dateStr
					);
				}
			} else {
				headersStr = Exceptions._renderDataTableRows(preparedResult.Headers, htmlOut, false);
				List<string> processAndThreadId = new List<string>() { "Process ID: " + Tools.GetProcessId().ToString() };
				if (!webEnv && !fileSystemLog) {
					if (preparedResult.ErrorFileStackTrace.HasValue) {
						errorFileStr = ErrorFile.Render(preparedResult.ErrorFileStackTrace.Value, StackTraceFormat.Text);
					}
					stackTraceStr = Exceptions._renderStackTrace(preparedResult.AllStackTraces, htmlOut, StackTraceFormat.Text, fileSystemLog);
					processAndThreadId.Add("Thread ID : " + Tools.GetThreadId().ToString());
					return Exceptions._renderStackRecordResultConsoleText(
						preparedResult, String.Join(Environment.NewLine + "   ", processAndThreadId.ToArray()), errorFileStr, stackTraceStr, headersStr, dateStr
					);
				} else {
					stackTraceStr = Exceptions._renderStackTrace(preparedResult.AllStackTraces, htmlOut, StackTraceFormat.Json, fileSystemLog);
					if (webEnv) processAndThreadId.Add("Request ID: " + Tools.GetRequestId().ToString());
					processAndThreadId.Add("Thread ID: " + Tools.GetThreadId().ToString());
					return Exceptions._renderStackRecordResultLoggerText(
						preparedResult, String.Join(" | ", processAndThreadId.ToArray()), errorFileStr, stackTraceStr, headersStr, dateStr
					);
				}
			}
        }
		private static string _renderStackRecordResultHtmlResponse (
			RenderingCollection preparedResult,
			string errorFileStr,
			string stackTraceStr,
			string headersStr,
			string dateStr
		) {
			string linkValue = "https://www.google.com/search?sourceid=desharp&gws_rd=us&q="
				+ HttpUtility.UrlEncode(preparedResult.ExceptionMessage);
			string causedByMsg = preparedResult.CausedByMessage;
			if (causedByMsg.Length > 50) causedByMsg = causedByMsg.Substring(0, 50) + "...";
			string result = @"<div class=""exception"">"
				+ @"<div class=""head"">"
					+ @"<div class=""type"">" + preparedResult.ExceptionType + " (Hash Code: " + preparedResult.ExceptionHash + ")</div>"
					+ @"<a href=""" + linkValue + @""" target=""_blank"">"
							+ preparedResult.ExceptionMessage
					+ "</a>"
					+ @"<div class=""info"">"
						+ "Catched: " + (preparedResult.Catched ? "yes" : "no")
						+ (preparedResult.CausedByHash.Length > 0 
							? ", Caused By: " + preparedResult.CausedByType + " (Hash Code: " + preparedResult.CausedByHash + ", Message: " + causedByMsg + ")" 
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
				Exceptions._renderDataTableRows(LoadedAssemblies.CompleteLoadedAssemblies(), true, true)
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
			JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
			bool valueRecording = preparedResult.ExceptionType == "Value";
			string result = @"<div class=""record" + (valueRecording ? " value" : " exception") + @""">"
				+ @"<div class=""control"">"
					+ @"<div class=""date"">" + dateStr + "</div>";
			if (valueRecording) {
				result += @"<div class=""process"">" + Tools.GetProcessId().ToString() + "</div>"
					+ ((Dispatcher.EnvType == EnvType.Web) ? @"<div class=""request"">" + Tools.GetRequestId().ToString() + "</div>" : "")
					+ @"<div class=""thread"">" + Tools.GetThreadId().ToString() + "</div>"
					+ @"<div class=""desharp-dump"">" + preparedResult.ExceptionMessage + "</div>";
			} else {
				result += @"<div class=""catched"">" + (preparedResult.Catched ? "yes" : "no") + "</div>"
					+ @"<div class=""type"">" + preparedResult.ExceptionType + " (" + preparedResult.ExceptionHash + ")</div>"
					+ @"<div class=""msg"">" + preparedResult.ExceptionMessage + "</div>";
			}
			result += @"</div><div class=""json-data"">";
			string dataStr = "{";
			if (!valueRecording) {
				dataStr += "processId: " + Tools.GetProcessId().ToString();
				if (Dispatcher.EnvType == EnvType.Web) dataStr += ",requestId:" + Tools.GetRequestId().ToString();
				dataStr += ",threadId:" + Tools.GetThreadId().ToString();
			}
			if (!valueRecording && preparedResult.CausedByType != null && preparedResult.CausedByType.Length > 0) {
				if (dataStr != "{") dataStr += ",";
				dataStr += "causedByType:" + jsonSerializer.Serialize(preparedResult.CausedByType)
					+ ",causedByHash:" + jsonSerializer.Serialize(preparedResult.CausedByHash);
			}
			if (dataStr != "{") dataStr += ",";
			dataStr += "callstack:" + stackTraceStr;
			if (preparedResult.Headers.Count > 0) dataStr += ",headers:" + headersStr;
			dataStr += "}";
			result += dataStr + "</div></div>";
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
			string result = preparedResult.ExceptionType + " (Hash Code: " + preparedResult.ExceptionHash + "):";
			result += System.Environment.NewLine + "   Message   : " + preparedResult.ExceptionMessage;
			result += System.Environment.NewLine + "   Time      : " + dateStr;
			result += System.Environment.NewLine + "   " + threadOrRequestIdStr;
			if (preparedResult.CausedByType != null && preparedResult.CausedByType.Length > 0) {
				result += System.Environment.NewLine + "   Cuased By : " + preparedResult.CausedByType + " (Hash Code: " + preparedResult.CausedByHash + ")";
			}
			if (errorFileStr.Length > 0) {
				string file = Tools.RelativeSourceFullPath(preparedResult.ErrorFileStackTrace.Value.File.ToString());
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
			message = r2.Replace(message.Trim(), '\\' + "n");
			string result = "Time: " + dateStr + " | " + threadOrRequestIdStr;
			if (preparedResult.ExceptionType == "Value") {
				result += " | Value: " + message;
			} else {
				result += " | Type: " + preparedResult.ExceptionType + " (Hash Code: " + preparedResult.ExceptionHash + ")"
					+ " | Catched: " + (preparedResult.Catched ? "yes" : "no")
					+ " | Message: " + message;
				if (preparedResult.CausedByType != null && preparedResult.CausedByType.Length > 0)
					result += " | Caused By: " + preparedResult.CausedByType + " (Hash Code: " + preparedResult.CausedByHash + ")";
			}
			if (stackTraceStr.Length > 0) result += " | Callstack: " + stackTraceStr;
			if (headersStr.Length > 0) result += " | Request Headers: " + headersStr;
			return result;
		}
		private static string _renderDataTableRows (List<string[]> data, bool htmlOut = false, bool firstRowAsHead = false) {
			StringBuilder result = new StringBuilder();
			if (data.Count == 0) return "";
			if (htmlOut) {
				int index = 0;
				foreach (string[] rowData in data) {
					result.Append("<tr>");
					for (int i = 0, l = rowData.Length; i < l; i += 1) {
						if (index == 0 && firstRowAsHead) {
							result.Append("<th>" + rowData[i] + "</th>");
						} else if (data[0].Length == 2 && i == 0) {
							result.Append("<th>" + rowData[i] + "</th>");
						} else {
							result.Append("<td>" + rowData[i] + "</td>");
						}
					}
					result.Append("</tr>");
					index++;
				}
			} else {
				if (data.Count > 0 && data[0].Length == 2) {
					Dictionary<string, string> dataDct = new Dictionary<string, string>();
					foreach (string[] dataItem in data) dataDct.Add(dataItem[0], dataItem[1]);
					result.Append(new JavaScriptSerializer().Serialize(dataDct));
				} else {
					result.Append(new JavaScriptSerializer().Serialize(data));
				}
			}
			return result.ToString();
		}
		private static string _renderHtmlDataTable (string title, string tableRows) {
			return @"<div class=""table""><b class=""title"">" + title + @"</b><div class=""data"" ><table>" + tableRows + "</table></div></div>";
		}
		private static string _renderStackTrace (List<StackTraceItem> stackTrace, bool htmlOut, StackTraceFormat format, bool fileSystemLog = true) {
			List<string> result = new List<string>();
			int counter = 0;
			int[] textLengths = new int[] { 0, 0};
			if (format != StackTraceFormat.Html) {
				List<StackTraceItem> newStackTrace = new List<StackTraceItem>();
				StackTraceItem newStackTraceItem;
				foreach (StackTraceItem stackTraceItem in stackTrace) {
					newStackTraceItem = new StackTraceItem(stackTraceItem);
					if (newStackTraceItem.Method.Length > textLengths[0]) textLengths[0] = newStackTraceItem.Method.Length;
					if (htmlOut && format == StackTraceFormat.Json && newStackTraceItem.File.ToString().Length > 0) {
						newStackTraceItem.File = new string[] {
							newStackTraceItem.File.ToString(),
							Tools.RelativeSourceFullPath(newStackTraceItem.File.ToString())
						};
					} else {
						newStackTraceItem.File = Tools.RelativeSourceFullPath(newStackTraceItem.File.ToString());
					}
					if (newStackTraceItem.File.ToString().Length > textLengths[1]) textLengths[1] = newStackTraceItem.File.ToString().Length;
					newStackTrace.Add(newStackTraceItem);
				}
				stackTrace = newStackTrace;
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
			string fileNameLink = "";
			bool fileDefined = stackTraceItem.File.ToString().Length > 0 && stackTraceItem.File.ToString().Length > 0;
			if (format == StackTraceFormat.Html) {
				if (fileDefined) {
					fileNameLink = @"<a href=""editor://open/?file=" + HttpUtility.UrlEncode(stackTraceItem.File.ToString())
						+ "&line=" + stackTraceItem.Line
						+ "&editor=" + Tools.Editor
						+ @""">" + Tools.RelativeSourceFullPath(stackTraceItem.File.ToString()) + "</a>";
				}
				result = (fileNameLink.Length > 0 ? @"<tr class=""known""> " : "<tr>")
					+ @"<td class=""file"">" + fileNameLink + "</td>"
					+ @"<td class=""line"">" + stackTraceItem.Line + "</td>"
					+ @"<td class=""method""><i>" + stackTraceItem.Method.Replace(".", "&#8203;.") + " </i></td>"
				+ "</tr>";
			} else if (format == StackTraceFormat.Text) {
				if (fileDefined) {
					result = stackTraceItem.Method + Tools.SpaceIndent(textLengths[0] - stackTraceItem.Method.Length, false)
						+ " " + stackTraceItem.File + Tools.SpaceIndent(textLengths[1] - stackTraceItem.File.ToString().Length, false)
						+ " " + stackTraceItem.Line;
				} else {
					result = stackTraceItem.Method + Tools.SpaceIndent(textLengths[0] - stackTraceItem.Method.Length, false)
						+ " " + stackTraceItem.File;
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