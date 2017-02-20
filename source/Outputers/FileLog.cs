using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Desharp.Core;
using System.Resources;
using System.Reflection;

namespace Desharp.Outputers {
    public class FileLog	{
        private const string LOGS_EXT_HTML = ".html";
		private const string LOGS_EXT_TEXT = ".log";
		private const string LOGS_NUMBERING_SEPARATOR = "-";
        private static Dictionary<string, bool> _levels;
        private static Dictionary<long, Dictionary<string, StringBuilder>> _stores;
        static FileLog() {
			FileLog._levels = Config.GetLevels();
            FileLog._stores = new Dictionary<long, Dictionary<string, StringBuilder>>();
        }
		public static void RequestEnd(long crt)
        {
			Dictionary<string, StringBuilder> cs = FileLog._getCurrentStore(crt);
			bool writeSuccess;
            foreach (var item in cs) {
				writeSuccess = FileLog._write(item.Key, item.Value);
            }
			FileLog._destroyCurrentStore(crt);
        }
		internal static void Log (string renderedContentToWrite, string level) {
			if (FileLog._levels.Count > 0) {
				if (!FileLog._levels.ContainsKey(level) || (FileLog._levels.ContainsKey(level) && !FileLog._levels[level])) return;
			}
			if (Core.Environment.Type == EnvironmentType.Web) {
				StringBuilder sb = FileLog._getCurrentStoreAndLog(level);
				FileLog._stores[Tools.GetRequestId()][level] = sb.Append(renderedContentToWrite);
			} else {
				FileLog._write(level, new StringBuilder(renderedContentToWrite));
			}
		}
		/*************************************************************************/
		private static bool _write(string filename, StringBuilder sb)
        {
            string fullPath = FileLog._getFullPathFromFilename(filename);
			bool logBegin = !File.Exists(fullPath) || (File.Exists(fullPath) && new FileInfo(fullPath).Length < 4 /* utf8bom has length 3 */);
            if (logBegin) {
				string writeContent = sb.ToString();
				if (Core.Environment.GetOutput() == OutputType.Html) {
					writeContent = FileLog._getHtmlLogFileBegin(filename) + writeContent;
				}
				return FileLog._writeFileStream(fullPath, writeContent,  true);
            } else {
                if (File.ReadAllBytes(fullPath).Length < 100 * 1024 * 1024) {
                    // append into file
                    return FileLog._writeFileStream(fullPath, sb.ToString(), false);
                } else {
                    // create new brotherhood log file (recursion)
                    filename = FileLog._getNewNumberedLogFilename(filename);
					return FileLog._write(filename, sb);
                }
            }
        }
		private static string _getHtmlLogFileBegin (string filename) {
			ResourceManager rm = new ResourceManager("Desharp.Assets", Assembly.GetExecutingAssembly());
			string jsContent = rm.GetString("LogsJs").Replace("\r", "").Replace("\n", "").Replace("\t", "");
			string cssContent = rm.GetString("LogsCss").Replace("\r", "").Replace("\n", "").Replace("\t", "");
			return "<!DOCTYPE HTML>"
				+ "<html lang=\"en-US\">"
					+ "<head>"
						+ "<meta charset=\"UTF-8\">"
						+ "<title>" + filename + "</title>"
						+ "<style>" + cssContent + "</style>"
						+ "<script>" + jsContent + "</script>"
					+ "</head>"
				+ "<body onload=\"init();\">";
		}
        private static bool _writeFileStream(string fullPath, string value, bool fromStart)
        {
			bool r;
            FileStream lStream = null;
			try {
				lStream = new FileStream(fullPath, fromStart ? FileMode.Create : FileMode.Append);
				byte[] bytes = new UTF8Encoding(true).GetBytes(value);
				lStream.Write(bytes, 0, bytes.Length);
				lStream.Flush();
				r = true;
			} catch (Exception e) {
				r = false;
			} finally {
				if (lStream != null) lStream.Close();
            }
			return r;
        }
        private static string _getNewNumberedLogFilename(string filename) {
            if (filename.Contains(FileLog.LOGS_NUMBERING_SEPARATOR)) {
				int logNumber = Int32.Parse(filename.Substring(filename.LastIndexOf(FileLog.LOGS_NUMBERING_SEPARATOR) + 1));
                logNumber++;
                filename = filename.Substring(0, filename.LastIndexOf(FileLog.LOGS_NUMBERING_SEPARATOR)) + FileLog.LOGS_NUMBERING_SEPARATOR + logNumber;
            } else {
                filename = filename + FileLog.LOGS_NUMBERING_SEPARATOR + 1;
            }
            return filename;
        }
        private static string _getFullPathFromFilename(string filename) {
			string fullPath = Core.Environment.Directory + "/" + filename;
			fullPath += Core.Environment.GetOutput() == OutputType.Html ? FileLog.LOGS_EXT_HTML : FileLog.LOGS_EXT_TEXT;
            return fullPath;
        }
		
		private static Dictionary<string, StringBuilder> _getCurrentStore(long crt = -1)
        {
			if (crt == -1) crt = Tools.GetRequestId();
            Dictionary<string, StringBuilder> cr;
            if (FileLog._stores.ContainsKey(crt) && FileLog._stores[crt] is Dictionary<string, StringBuilder>) {
                cr = FileLog._stores[crt];
            } else {
                cr = new Dictionary<string, StringBuilder>();
                FileLog._stores[crt] = cr;
            }
            return cr;
        }
        private static StringBuilder _getCurrentStoreAndLog(string l) {
            long crt = Tools.GetRequestId();
            StringBuilder sb;
            Dictionary<string, StringBuilder> cs = FileLog._getCurrentStore();
            if (cs.ContainsKey(l) && cs[l] is StringBuilder) {
                sb = cs[l];
            } else {
                sb = new StringBuilder();
                FileLog._stores[crt][l] = sb;
            }
            return sb;
        }
		private static void _destroyCurrentStore(long crt = -1) {
			if (crt == -1) crt = Tools.GetRequestId();
            FileLog._stores.Remove(crt);
        }
    }
}