using Desharp.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace Desharp.Renderers {
	internal class ErrorFile {
		private const string ERROR_FILE_LINE_COLUMN_CURSOR_PRESET = "__STACK_TRACE_RENDERER_ERROR_FILE_LINE_COLUMN_CURSOR__";
		private const string ERROR_FILE_LINE_COLUMN_CURSOR_VALUE = "<span class=\"logger-file-line-current-column\">|</span>";
		private static int[] _errorFileLineDisplayArea = new int[2]{4, 4};
		internal static string Render (StackTraceItem errorFileStackTrace, StackTraceFormat format) {
			string result = "";
			string rawFileContent = ErrorFile._readErrorFile(errorFileStackTrace.File);
			rawFileContent = rawFileContent.Replace("\r", "");
			string[] allFileLines = Regex.Split(rawFileContent, "\n");
			int[] linesCount = ErrorFile._errorFileLineDisplayArea;
			int lineInt = -1;
			int columnInt = -1;
			try {
				if (errorFileStackTrace.Line.Length > 0) lineInt = Convert.ToInt32(errorFileStackTrace.Line);
				if (errorFileStackTrace.Column.Length > 0) columnInt = Convert.ToInt32(errorFileStackTrace.Column);
			} catch (Exception e) { }
			if (lineInt > -1) {
				int beginLine;
				int endLine;
				if (lineInt - 1 - linesCount[0] < 0) {
					beginLine = 1;
					linesCount[1] += linesCount[0] - (lineInt - 1);
				} else {
					beginLine = lineInt - linesCount[0];
				}
				if (lineInt + linesCount[1] > allFileLines.Length) {
					endLine = allFileLines.Length;
					beginLine -= (lineInt + linesCount[1]) - allFileLines.Length;
					if (beginLine < 1) beginLine = 1;
				} else {
					endLine = lineInt + linesCount[1];
				}
				List<dynamic> linesToRender = new List<dynamic>();
				bool current;
				for (int i = 1, l = allFileLines.Length + 1; i < l; i++) {
					if (i < beginLine) continue;
					if (i > endLine) break;
					current = (i == lineInt) ? true : false;
					linesToRender.Add(new {
						text = allFileLines[i - 1],
						current = current,
						num = i,
					});
				}
				result = ErrorFile.RenderErrorLines(errorFileStackTrace.File, lineInt, columnInt, linesToRender, format);
			}
			return result;
		}
		internal static string RenderErrorLines (string errorFile, int errorLine, int columnInt, List<dynamic> linesToRender, StackTraceFormat format) {
			string result = "";
			int lineNumDigitsCount = linesToRender[linesToRender.Count - 1].num.ToString().Length;
			List<string> linesContent = new List<string>();
			string lineContent = "";
			string currentLineCls = "";
			string lineText = "";
			string baseIndent = "   ";
			string tabAsHtmlSpaces = "&nbsp;&nbsp;&nbsp;&nbsp;";
			foreach (dynamic line in linesToRender) {
				lineText = line.text;
				if (line.current) {
					currentLineCls = " logger-file-line-current";
					if (columnInt > -1) {
						lineText = lineText.Substring(0, columnInt - 1)
							+ ErrorFile.ERROR_FILE_LINE_COLUMN_CURSOR_PRESET
							+ lineText.Substring(columnInt - 1);
					}
				} else {
					currentLineCls = "";
				}
				if (format == StackTraceFormat.Html) {
					lineText = lineText
						.Replace("<", "&lt;")
						.Replace(">", "&gt;")
						.Replace("    ", tabAsHtmlSpaces)
						.Replace("\t", tabAsHtmlSpaces)
						.Replace(
							ErrorFile.ERROR_FILE_LINE_COLUMN_CURSOR_PRESET,
							ErrorFile.ERROR_FILE_LINE_COLUMN_CURSOR_VALUE
						);
					lineContent = "<div class=\"logger-file-line" + currentLineCls.ToString() + "\">"
						+ "<span class=\"logger-file-line-number logger-file-line-digits-" + lineNumDigitsCount.ToString() + "\">"
							+ line.num.ToString() + ":"
						+ "</span>"
						+ "<span class=\"logger-file-line-content\">"
							+ lineText
						+ "</span>"
					+ "</div>";
				} else {
					lineText = lineText.Replace(
						ErrorFile.ERROR_FILE_LINE_COLUMN_CURSOR_PRESET, "|"
					);
					lineContent = baseIndent + (line.current == true ? "-> " : "   ")
						+ line.num.ToString()
						+ Tools.SpaceIndent(lineNumDigitsCount.ToString().Length - line.num.ToString().Length, false)
						+ " | " + lineText;
				}
				linesContent.Add(lineContent);
			}
			if (format == StackTraceFormat.Html) {
				result = "<div class=\"logger-file\">"
					+ "<div class=\"logger-file-title\">"
						+ "<b>Fi" + "le:</b>"
						+ "<a href=\"editor://open/?file=" + HttpUtility.UrlEncode(errorFile)
							+ "&line=" + errorLine.ToString()
							+ "&editor=" + Tools.Editor
						+ "\">" + errorFile + ":" + errorLine.ToString() + "</a>"
					+ "</div>"
					+ "<div class=\"logger-file-content\">"
						+ "<div class=\"logger-file-lines\">" + String.Join("", linesContent) + "</div>"
					+ "</div>"
				+ "</div>";
			} else {
				string lineBorder = "-----";
				for (var i = 0; i < lineNumDigitsCount; i++) lineBorder += "-";
				result = baseIndent + lineBorder + System.Environment.NewLine
					+ String.Join(System.Environment.NewLine, linesContent) + System.Environment.NewLine
					+ baseIndent + lineBorder;
			}
			return result;
		}
		private static string _readErrorFile (string fileFullPath) {
			string result = "";
			StreamReader streamReader = null;
			try {
				streamReader = new StreamReader(fileFullPath);
				result = streamReader.ReadToEnd();
				streamReader.Close();
			} catch (Exception e) {
				streamReader.Close();
			}
			return result;
		}
	}
}
