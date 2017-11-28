using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Desharp.Core;
using System.Linq;
using Desharp.Renderers;
using System.Web;
using System.Reflection;
using System.CodeDom.Compiler;

namespace Desharp.Completers {
	internal class StackTrace {
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
			int counter = 0;

			foreach (StackFrame stackItem in stackTrace.GetFrames()) {
				fileName = stackItem.GetFileName() ?? "";
				fileName = fileName.Replace('\\', '/');

				if (fileName.Length > 0) {
					if (fileName.IndexOf(StackTrace.SELF_FILENAME) > -1 & fileName.IndexOf(StackTrace.SELF_FILENAME) == fileName.Length - StackTrace.SELF_FILENAME.Length) continue;
					if (fileName.IndexOf(Exceptions.SELF_FILENAME) > -1 & fileName.IndexOf(Exceptions.SELF_FILENAME) == fileName.Length - Exceptions.SELF_FILENAME.Length) continue;
					if (fileName.IndexOf(Debug.SELF_FILENAME) > -1 & fileName.IndexOf(Debug.SELF_FILENAME) == fileName.Length - Debug.SELF_FILENAME.Length) continue;
				}

				line = stackItem.GetFileLineNumber().ToString().Trim();
				column = stackItem.GetFileColumnNumber().ToString().Trim();
				method = stackItem.GetMethod().ToString().Trim();
				
				if (line.Length == 0) line = "?";

				stackTraceItem = new StackTraceItem {
					File = fileName,
					Line = line,
					Column = column,
					Method = method
				};
				if (
					!errorFile.HasValue & 
					fileName.Length > 0 &
					line != "?"
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
        internal static StackTraceItem CompleteCallerPoint () {
            StackTraceItem result = new StackTraceItem();
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(1, true);
            string fileName;
            string line;
            foreach (var stackItem in stackTrace.GetFrames()) {
                fileName = stackItem.GetFileName() ?? "";
                fileName = fileName.Replace('\\', '/');
                if (fileName.Length > 0) {
                    if (fileName.IndexOf(FireDump.SELF_FILENAME) > -1 & fileName.IndexOf(FireDump.SELF_FILENAME) == fileName.Length - FireDump.SELF_FILENAME.Length) continue;
                    if (fileName.IndexOf(StackTrace.SELF_FILENAME) > -1 & fileName.IndexOf(StackTrace.SELF_FILENAME) == fileName.Length - StackTrace.SELF_FILENAME.Length) continue;
                    if (fileName.IndexOf(Exceptions.SELF_FILENAME) > -1 & fileName.IndexOf(Exceptions.SELF_FILENAME) == fileName.Length - Exceptions.SELF_FILENAME.Length) continue;
                    if (fileName.IndexOf(Debug.SELF_FILENAME) > -1 & fileName.IndexOf(Debug.SELF_FILENAME) == fileName.Length - Debug.SELF_FILENAME.Length) continue;
                }
                line = stackItem.GetFileLineNumber().ToString().Trim();
                if (line.Length == 0) line = "";
                result = new StackTraceItem {
                    File = fileName,
                    Line = line
                };
                break;
            }
            return result;
        }
        internal static RenderingCollection RenderStackTraceForException (ExceptionToRender exceptionToRender, bool fileSystemLog = true, bool htmlOut = false, int index = 0) {
			StackTraceItem? possibleViewExceptionStackTrace;
			List<StackTraceItem> stackTraceItems = StackTrace._completeStackTraceForSingleException(exceptionToRender.Exception);
			StackTraceItem? errorFile = null;
			string causedByHash = "";
			string causedByType = "";
			string causedByMessage = "";
			string exceptionMessage = exceptionToRender.Exception.Message;
			if (index == 0 && !fileSystemLog) {
				// there is possible view exception
				/*possibleViewExceptionStackTrace = StackTrace._getPossibleViewExceptionInfo(exceptionToRender.Exception);
				if (possibleViewExceptionStackTrace.HasValue) {
					errorFile = possibleViewExceptionStackTrace;
				}*/
				if (exceptionToRender.Exception is HttpCompileException) {
					PropertyInfo[] props = exceptionToRender.Exception.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
					PropertyInfo prop;
					for (int i = 0, l = props.Length; i < l; i += 1) {
						prop = props[i];
						if (prop.Name == "FirstCompileError") {
							CompilerError compileError = prop.GetValue(exceptionToRender.Exception, null) as CompilerError;
							if (compileError is CompilerError) {
								exceptionMessage = compileError.ErrorText;
								errorFile = new StackTraceItem {
									File = compileError.FileName,
									Line = compileError.Line.ToString(),
									Column = compileError.Column.ToString()
								};
							}
							break;
						}
					}
				}

			}
			if (errorFile == null && !fileSystemLog) {
				foreach (StackTraceItem stackTraceItem in stackTraceItems) {
					if (
						stackTraceItem.File.ToString().Length > 0 &&
						stackTraceItem.Line != "?"
					) {
						errorFile = stackTraceItem;
						break;
					}
				}
			}
			if (htmlOut) {
				exceptionMessage = exceptionMessage
					.Replace("&", "&amp;")
					.Replace("<", "&lt;")
					.Replace(">", "&gt;");
			}
			if (exceptionToRender.CausedBy is Exception) {
				causedByHash = exceptionToRender.CausedBy.GetHashCode().ToString();
				causedByType = exceptionToRender.CausedBy.GetType().FullName;
				causedByMessage = exceptionToRender.CausedBy.Message;
			}
			return new RenderingCollection {
				ErrorFileStackTrace = errorFile,
				AllStackTraces = stackTraceItems,
				Catched = exceptionToRender.Catched,
				ExceptionHash = exceptionToRender.Exception.GetHashCode().ToString(),
				ExceptionType = exceptionToRender.Exception.GetType().FullName,
				ExceptionMessage = exceptionMessage,
				CausedByHash = causedByHash,
				CausedByType = causedByType,
				CausedByMessage = causedByMessage,
			};
		}
		internal static Dictionary<string, ExceptionToRender> CompleteInnerExceptions (Exception e, bool catched = true) {
			// key - current exception imprint (child exception)
			// value[0] - current exception instance (child exception)
			// value[1] - optional, inner/base exception imprint (parent exception, caused by exception)
			// value[2] - optional, inner/base exception instance (parent exception, caused by exception)
			Dictionary<string, object[]> exceptions = new Dictionary<string, object[]>();
			exceptions.Add(StackTrace._getExceptionFingerPrint(e), new object[] { e });
			StackTrace._completeExceptionsGetBaseAndInnerExceptions(e, ref exceptions, true);
			StackTrace._completeExceptionsGetBaseAndInnerExceptions(e, ref exceptions, false);
			return StackTrace._completeExceptionsOrderByRelationships(exceptions, catched);
		}
        private static void _completeExceptionsGetBaseAndInnerExceptions (Exception e, ref Dictionary<string, object[]> exceptions, bool useBaseExceptionsGetter) {
			string currentExceptionImprint = StackTrace._getExceptionFingerPrint(e);
			Exception currentException = e;
			Exception baseException;
			string baseExceptionImprint;
			bool breakWhile;
			int safeCounter = 0;
			while (true && safeCounter < 10) {
				breakWhile = true;
				try {
					if (useBaseExceptionsGetter) { 
						baseException = currentException.GetBaseException();
					} else {
						baseException = currentException.InnerException;
					}
					if (baseException is Exception) {
						baseExceptionImprint = StackTrace._getExceptionFingerPrint(baseException);
						if (!exceptions.ContainsKey(baseExceptionImprint)) {
							breakWhile = false;
							exceptions.Add(baseExceptionImprint, new object[] {
								baseException, currentExceptionImprint, currentException
							});
							currentException = baseException;
							currentExceptionImprint = baseExceptionImprint;
						}
					}
				} catch (Exception _e1) {
				}
				safeCounter++;
				if (breakWhile) break;
			}
		}
		private static Dictionary<string, ExceptionToRender> _completeExceptionsOrderByRelationships (Dictionary<string, object[]> exceptions, bool catched = true) {
			Dictionary<string, ExceptionToRender> result = new Dictionary<string, ExceptionToRender>();
			Dictionary<string, ExceptionToRender> reversedResult = new Dictionary<string, ExceptionToRender>();
			Exception causedByException;
			Exception triggeredException;
			string triggeredExceptionKey;
			foreach (var item in exceptions) {
				causedByException = item.Value[0] as Exception;
				triggeredException = null;
				if (item.Value.Length > 1) {
					triggeredExceptionKey = item.Value[1].ToString();
					if (exceptions.ContainsKey(triggeredExceptionKey)) {
						triggeredException = item.Value[2] as Exception;
					}
					if (reversedResult.ContainsKey(triggeredExceptionKey)) continue;
					reversedResult.Add(triggeredExceptionKey, new ExceptionToRender {
						Exception = triggeredException,
						CausedBy = causedByException,
						Catched = catched
					});
				}
			}
			foreach (var item in exceptions) {
				if (!reversedResult.ContainsKey(item.Key)) {
					reversedResult.Add(item.Key, new ExceptionToRender {
						Exception = item.Value[0] as Exception,
						CausedBy = null,
						Catched = catched
					});
					// break;?
				}
			}
			List<string> resultKeysOrdered = reversedResult.Keys.ToList<string>();
			resultKeysOrdered.Reverse();
			foreach (string resultKey in resultKeysOrdered) {
				result.Add(resultKey, reversedResult[resultKey]);
			}
			return result;
		}
		/*private static StackTraceItem? _getPossibleViewExceptionInfo (Exception e) {
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
			docRootPos = message.ToLower().IndexOf(Dispatcher.AppRoot.ToLower().Substring(1));
			if (matches.Count > 0) {
				if (matches[0] is Match && docRootPos > -1) {
					viewRelativePathBegin = docRootPos - 1 + Dispatcher.AppRoot.Length;
					viewAbsPath = message.Substring(docRootPos - 1, Dispatcher.AppRoot.Length)
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
		}*/
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
			if (String.IsNullOrEmpty(e.StackTrace)) return result;
			string rawStackTrace = Regex.Replace(e.StackTrace.ToString(), @"\r", "");
			string[] rawStackTraceLines = rawStackTrace.Split('\n');
			string methodAndPossibleFileNameAndLine;
			string fileNameAndLine;
			string method;
			string fileName;
			string line;
			Regex r1 = new Regex(@"^([^\s]*)\s(.*)$");
			Regex r2 = new Regex(@"^([^\)]*)\)\s+([^\s]+)\s+(.*)$");
			Regex r3 = new Regex(@"(.*)\.(cs|vb):([^\s]+)\s([0-9]*)$");
			for (int i = 0; i < rawStackTraceLines.Length; i++) {
				method = "";
				fileName = "";
				line = "?";
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

				//if (fileName.LastIndexOf(Debug.SELF_FILENAME) == fileName.Length - Debug.SELF_FILENAME.Length) continue;
				//if (fileName.LastIndexOf(Exceptions.SELF_FILENAME) == fileName.Length - Exceptions.SELF_FILENAME.Length) continue;

				fileName = fileName.Replace('\\', '/');
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
