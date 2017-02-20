using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Desharp.Core;

namespace Desharp.Completers {
	public class StackTrace {
		internal const string SELF_FILENAME = "StackTrace.cs";
		private static Dictionary<long, string> _renderedViews = new Dictionary<long, string>();
		internal static void SetLastRenderedView (string viewAbsolutePath) {
			long crt = Tools.GetRequestId();
			StackTrace._renderedViews[crt] = viewAbsolutePath;
		}
		internal static void CleanLastRenderedView () {
			long crt = Tools.GetRequestId();
			if (StackTrace._renderedViews.ContainsKey(crt)) {
				StackTrace._renderedViews.Remove(crt);
			}
		}
		internal static RenderingCollection CompleteStackTraceForCurrentApplicationPoint (string message = "", string exceptionType = "", bool fileSystemLog = true, bool htmlOut = false) {
			System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
			string fileName;
			string line;
			string column;
			string method;
			StackTraceItem stackTraceItem;
			StackTraceItem? errorFile = null;
			List<StackTraceItem> stackTraceItems = new List<StackTraceItem>();
			bool[] debugFiles = new bool[]{false, false, false};
			int counter = 0;

			foreach (StackFrame stackItem in stackTrace.GetFrames()) {
				fileName = stackItem.GetFileName();
				if (fileName == null) fileName = "";

				if (fileName.Length > 0) {
					debugFiles[0] = fileName.IndexOf(StackTrace.SELF_FILENAME) > -1 && fileName.IndexOf(StackTrace.SELF_FILENAME) == fileName.Length - StackTrace.SELF_FILENAME.Length;
					debugFiles[1] = fileName.IndexOf(Debug.SELF_FILENAME) > -1 && fileName.IndexOf(Debug.SELF_FILENAME) == fileName.Length - Debug.SELF_FILENAME.Length;
					if (debugFiles[0] || debugFiles[1] || debugFiles[2]) continue;
				}

				line = stackItem.GetFileLineNumber().ToString().Trim();
				column = stackItem.GetFileColumnNumber().ToString().Trim();
				method = stackItem.GetMethod().ToString().Trim();

				if (fileName.Length == 0) fileName = Renderers.StackTrace.GetExternalCodeDescription();
				if (line.Length == 0) line = Renderers.StackTrace.GetUnknownLineDescription();

				stackTraceItem = new StackTraceItem {
					File = fileName,
					Line = line,
					Column = column,
					Method = method
				};
				if (
					!errorFile.HasValue && 
					fileName != Renderers.StackTrace.GetExternalCodeDescription() && 
					line != Renderers.StackTrace.GetUnknownLineDescription()
				) {
					errorFile = stackTraceItem;
				}

				stackTraceItems.Add(stackTraceItem);
				counter++;
			}
			return new RenderingCollection {
				ErrorFileStackTrace = errorFile,
				AllStackTraces = stackTraceItems,
				ExceptionMessage = message,
				ExceptionType = exceptionType.Length > 0 ? exceptionType : ""
			};
		}
		internal static RenderingCollection RenderStackTraceForException (Exception e, bool fileSystemLog = true, bool htmlOut = false, int index = 0) {
			StackTraceItem? possibleViewExceptionStackTrace;
			List<StackTraceItem> stackTraceItems = StackTrace._completeStackTraceForSingleException(e);
			StackTraceItem? errorFile = null;
			if (index == 0 && !fileSystemLog) {
				// there is possible view exception
				possibleViewExceptionStackTrace = StackTrace._getPossibleViewExceptionInfo(e);
				if (possibleViewExceptionStackTrace.HasValue) {
					errorFile = possibleViewExceptionStackTrace;
				}
			}
			if (errorFile == null && !fileSystemLog) {
				foreach (StackTraceItem stackTraceItem in stackTraceItems) {
					if (
						stackTraceItem.File != Renderers.StackTrace.GetExternalCodeDescription() &&
						stackTraceItem.Line != Renderers.StackTrace.GetUnknownLineDescription()
					) {
						errorFile = stackTraceItem;
						break;
					}
				}
			}
			string exceptionMessage = e.Message;
			if (htmlOut) {
				exceptionMessage = exceptionMessage
					.Replace("&", "&amp;")
					.Replace("<", "&lt;")
					.Replace(">", "&gt;");
			}
			return new RenderingCollection {
				ErrorFileStackTrace = errorFile,
				AllStackTraces = stackTraceItems,
				ExceptionType = e.GetType().ToString(),
				ExceptionMessage = exceptionMessage
			};
		}

		internal static Dictionary<string, Exception> CompleteInnerExceptions (Exception e) {
			Dictionary<string, Exception> exceptions = new Dictionary<string, Exception>();
			string exceptionFingerPrint = StackTrace._getExceptionFingerPrint(e);
			exceptions.Add(exceptionFingerPrint, e);
			Exception currentException = e;
			while (true) {
				bool cntn = true;
				try {
					Exception baseExc = currentException.GetBaseException();
					if (baseExc is Exception) {
						exceptionFingerPrint = StackTrace._getExceptionFingerPrint(baseExc);
						if (exceptions.ContainsKey(exceptionFingerPrint)) {
							cntn = false;
						} else {
							exceptions.Add(exceptionFingerPrint, baseExc);
							currentException = baseExc;
						}
					} else {
						cntn = false;
					}
				} catch (Exception _e1) {
					cntn = false;
				}
				if (!cntn) break;
			}
			currentException = e;
			while (true) {
				bool cntn = true;
				try {
					Exception innerExc = currentException.InnerException;
					if (innerExc is Exception) {
						exceptionFingerPrint = StackTrace._getExceptionFingerPrint(innerExc);
						if (exceptions.ContainsKey(exceptionFingerPrint)) {
							cntn = false;
						} else {
							exceptions.Add(exceptionFingerPrint, innerExc);
							currentException = innerExc;
						}
					} else {
						cntn = false;
					}
				} catch (Exception _e2) {
					cntn = false;
				}
				if (!cntn) break;
			}
			return exceptions;
		}
		private static StackTraceItem? _getPossibleViewExceptionInfo (Exception e) {
			StackTraceItem? result = null;
			string viewAbsPath = "";
			string viewLine = "";
			string viewColumn = "";
			int docRootPos = 0;
			int viewRelativePathBegin = 0;
			MatchCollection matches;

			string message = e.Message.Replace('\\', '/');

			matches = Regex.Matches(
				message,
				@"\\([a-zA-Z0-9_\-\.\@]*)\.cshtml\(([0-9]*)\)",
				RegexOptions.IgnoreCase | RegexOptions.Singleline
			);
			docRootPos = message.ToLower().IndexOf(Core.Environment.AppRoot.ToLower().Substring(1));
			if (matches.Count > 0) {
				if (matches[0] is Match && docRootPos > -1) {
					viewRelativePathBegin = docRootPos - 1 + Core.Environment.AppRoot.Length;
					viewAbsPath = message.Substring(docRootPos - 1, Core.Environment.AppRoot.Length)
						+ message.Substring(viewRelativePathBegin, matches[0].Index - viewRelativePathBegin)
						+ "\\" + matches[0].Groups[1].ToString() + ".cshtml";
					viewLine = matches[0].Groups[2].ToString();
					result = new StackTraceItem {
						File = viewAbsPath,
						Line = viewLine,
						Column = viewColumn,
						Method = ""
					};
				}
			}

			if (!result.HasValue) {
				// complete view exception info from last stored file before render (mostly control views)
				viewAbsPath = StackTrace._getLastRenderedView();
				if (viewAbsPath.Length > 0) {
					matches = Regex.Matches(
						message,
						@"^\(([0-9]*)\:([0-9]*)\) \- (.*)",
						RegexOptions.IgnoreCase | RegexOptions.Singleline
					);
					if (matches.Count > 0 && matches[0] is Match) {
						GroupCollection grp = matches[0].Groups;
						if (grp.Count > 3 && grp[0] is Match && grp[1] is Group && grp[2] is Group && grp[3] is Group) {
							try {
								viewLine = grp[1].ToString();
								viewColumn = grp[2].ToString();
							} catch (Exception cnvrtException) { }
						}
					}
					if (viewLine.Length > 0 && viewColumn.Length > 0) {
						result = new StackTraceItem {
							File = viewAbsPath,
							Line = viewLine,
							Column = viewColumn,
							Method = ""
						};
					}
				}
			}
			return result;
		}
		private static string _getLastRenderedView () {
			string result = "";
			long crt = Tools.GetRequestId();
			if (StackTrace._renderedViews.ContainsKey(crt) && StackTrace._renderedViews[crt].Length > 0) {
				result = StackTrace._renderedViews[crt];
			}
			return result;
		}
		private static List<StackTraceItem> _completeStackTraceForSingleException (Exception e) {
			List<StackTraceItem> result = new List<StackTraceItem>();
			string rawStackTrace = Regex.Replace(e.StackTrace.ToString(), @"\r", "");
			string[] rawStackTraceLines = rawStackTrace.Split('\n');
			string methodAndPossibleFileNameAndLine;
			string fileNameAndLine;
			string method;
			string fileName;
			string line;
			Regex r1 = new Regex(@"^([^\s]*)\s(.*)$");
			Regex r2 = new Regex(@"^([a-zA-Z0-9_\(\[\]\<\>\.\,\s]*)\)\s+([^\s]+)\s+(.*)$");
			Regex r3 = new Regex(@"(.*)\.(cs|vb):([^\s]+)\s([0-9]*)$");
			for (int i = 0; i < rawStackTraceLines.Length; i++) {
				method = "";
				fileName = Renderers.StackTrace.GetExternalCodeDescription();
				line = Renderers.StackTrace.GetUnknownLineDescription();
				// here stack trace line should contains: conjunction + method with params + conjunction + file + conjunction + : + line
				// 'at App.Controllers.WebsiteController.OnActionExecuting(ActionExecutingContext filterContext) in c:\inetpub\wwwroot\MVC-a-01\MVC-a-04\Controllers\WebsiteController.cs:line 20'
				methodAndPossibleFileNameAndLine = r1.Replace(rawStackTraceLines[i].Trim(), "$2").Trim();
				// here stack trace line should contains: method with params + conjunction + file + conjunction + : + line
				// 'App.Controllers.WebsiteController.OnActionExecuting(ActionExecutingContext filterContext) in c:\inetpub\wwwroot\MVC-a-01\MVC-a-04\Controllers\WebsiteController.cs:line 20'
				if (r2.Match(methodAndPossibleFileNameAndLine).Success) {
					// here stack trace line should contains: method with params + conjunction + file + conjunction + : + line
					method = r2.Replace(methodAndPossibleFileNameAndLine, "$1").Trim() + ")";
					fileNameAndLine = r2.Replace(methodAndPossibleFileNameAndLine, "$3").Trim();
					// here fileNameAndLine should contains: file + conjunction + : + line
					// 'c:\inetpub\wwwroot\MVC-a-01\MVC-a-04\Controllers\WebsiteController.cs:line 20'
					if (r3.Match(fileNameAndLine).Success) {
						fileName = r3.Replace(fileNameAndLine, "$1.$2").Trim();
						line = r3.Replace(fileNameAndLine, "$4").Trim();
					} else {
						fileName = fileNameAndLine;
					}
				} else {
					// here stack trace line should contains: method with params
					method = methodAndPossibleFileNameAndLine;
				}

				if (fileName is string && fileName.LastIndexOf(StackTrace.SELF_FILENAME) == fileName.Length - StackTrace.SELF_FILENAME.Length) continue;
				result.Add(new StackTraceItem {
					File = fileName,
					Line = line,
					Column = "",
					Method = method,
				});
			}
			return result;
		}
		private static string _getExceptionFingerPrint (Exception e) {
			return Tools.Md5((e.Message + e.Source + e.StackTrace).ToString());
		}
	}
}
