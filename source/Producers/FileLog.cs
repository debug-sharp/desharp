using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Desharp.Core;
using System.Resources;
using System.Reflection;
using System.Threading;

namespace Desharp.Producers {
    internal class FileLog	{
        protected const string LOGS_EXT_HTML = ".html";
		protected const string LOGS_EXT_TEXT = ".log";
		protected const string LOGS_NUMBERING_SEPARATOR = "-";
		protected static Dictionary<string, bool> levels;
        protected volatile static Dictionary<string, StringBuilder> stores;
		protected static object storesLock = new object { };
		protected static Thread writeThread;
		internal static void Init () {
			FileLog.levels = Config.GetLevels();
            FileLog.stores = new Dictionary<string, StringBuilder>();
			foreach (var item in LevelValues.Values) {
				FileLog.stores.Add(item.Value, new StringBuilder());
			}
			FileLog.stores.Add("exception", new StringBuilder());
			FileLog.writeThread = new Thread(() => {
				Thread.Sleep(Dispatcher.LogWriteMilisecond);
				FileLog.writeAllStores();
			});
			FileLog.writeThread.IsBackground = true;
			FileLog.writeThread.Start();
		}
		internal static void Log (string content, string level) {
			if (FileLog.levels.Count > 0) {
				if (!FileLog.levels.ContainsKey(level) || (FileLog.levels.ContainsKey(level) && !FileLog.levels[level])) return;
			}
			if (FileLog.stores.ContainsKey(level)) {
				lock (FileLog.storesLock) {
					FileLog.stores[level].Append(content);
				}
			}
		}
		/*************************************************************************/
		protected static void writeAllStores () {
			Dictionary<string, string> stores = FileLog.duplicateStores();
			bool writeSuccess;
			foreach (var item in stores) {
				if (item.Value.Length == 0) continue;
				try {
					writeSuccess = FileLog.writeStore(item.Key, item.Value);
				} catch (Exception e) {
				}
			}
		}
		protected static Dictionary<string, string> duplicateStores () {
			Dictionary<string, string> result = new Dictionary<string, string>();
			lock (FileLog.storesLock) {
				foreach (var item in FileLog.stores) {
					result.Add(item.Key, item.Value.ToString());
					item.Value.Clear();
				}
			}
			return result;
		}
		protected static bool writeStore (string filename, string writeContent) {
            string fullPath = FileLog.getFullPathFromFilename(filename);
			bool logBegin = !File.Exists(fullPath) || (File.Exists(fullPath) && new FileInfo(fullPath).Length < 4 /* utf8bom has length 3 */);
            if (logBegin) {
				if (Dispatcher.GetCurrent().Output == OutputType.Html) {
					writeContent = FileLog.getHtmlLogFileBegin(filename) + writeContent;
				}
				return FileLog.writeFileStream(fullPath, writeContent,  true);
            } else {
                if (File.ReadAllBytes(fullPath).Length < 100 * 1024 * 1024) {
                    // append into file
                    return FileLog.writeFileStream(fullPath, writeContent, false);
                } else {
                    // create new brotherhood log file (recursion)
                    filename = FileLog.getNewNumberedLogFilename(filename);
					return FileLog.writeStore(filename, writeContent);
                }
            }
        }
		protected static string getHtmlLogFileBegin (string filename) {
			ResourceManager rm = new ResourceManager("Desharp.Assets", Assembly.GetExecutingAssembly());
            string jsContent = rm.GetString("logs_js");
			string cssContent = rm.GetString("logs_css");
			return "<!DOCTYPE HTML>"
				+ @"<html lang=""en-US"">"
					+ "<head>"
						+ @"<meta charset=""UTF-8""/>"
						+ "<title>" + filename + "</title>"
						+ "<style>" + cssContent + "</style>"
						+ "<script>" + jsContent + "</script>"
					+ "</head>"
				+ "<body>";
		}
        protected static bool writeFileStream(string fullPath, string value, bool fromStart) {
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
		protected static string getNewNumberedLogFilename(string filename) {
            if (filename.Contains(FileLog.LOGS_NUMBERING_SEPARATOR)) {
				int logNumber = Int32.Parse(filename.Substring(filename.LastIndexOf(FileLog.LOGS_NUMBERING_SEPARATOR) + 1));
                logNumber++;
                filename = filename.Substring(0, filename.LastIndexOf(FileLog.LOGS_NUMBERING_SEPARATOR)) + FileLog.LOGS_NUMBERING_SEPARATOR + logNumber;
            } else {
                filename = filename + FileLog.LOGS_NUMBERING_SEPARATOR + 1;
            }
            return filename;
        }
		protected static string getFullPathFromFilename(string filename) {
			string fullPath = Dispatcher.Directory + "/" + filename;
			fullPath += Dispatcher.GetCurrent().Output == OutputType.Html ? FileLog.LOGS_EXT_HTML : FileLog.LOGS_EXT_TEXT;
            return fullPath;
        }
	}
}