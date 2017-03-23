using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Desharp.Core;
using System.Threading;

namespace Desharp.Producers {
    internal class FileLog	{
        protected const string LOGS_EXT_HTML = ".html";
		protected const string LOGS_EXT_TEXT = ".log";
		protected const string LOGS_NUMBERING_SEPARATOR = "-";
		protected const int MAX_LOG_FILE_SIZE = 50000000; // 50 MB
		protected static string[] htmlLogFileBegin;
		protected volatile static Dictionary<string, StringBuilder> stores;
		protected volatile static List<string> loadedStores;
		protected volatile static bool writing;
		protected volatile static object storesLock = new object { };
		protected static object writingLock = new object { };
		protected static Thread writeThread = null;
		internal static void Init () {
			FileLog.stores = new Dictionary<string, StringBuilder>();
			FileLog.writing = false;
			FileLog.loadedStores = new List<string>();
			foreach (var item in LevelValues.Values) {
				FileLog.stores.Add(item.Value, new StringBuilder());
			}
			FileLog.stores.Add("exception", new StringBuilder());
			FileLog.htmlLogFileBegin = new string[] {
				@"<!DOCTYPE HTML><html lang=""en-US""><head><meta charset=""UTF-8""/><title>",
				"</title><style>" 
				+ Assets.logs_css + Assets.dumps_css + Assets.exception_css 
				+ "</style><script>" 
				+ Assets.logs_js.Replace("__LINK_EDITOR__", Tools.Editor) 
				+ @"</script></head><body id=""desharp"">" 
				+ Environment.NewLine
			};
			FileLog.InitCyclicWritingIfNecessary();
		}
		internal static void InitCyclicWritingIfNecessary () {
			if (Dispatcher.LogWriteMilisecond > 0 && FileLog.writeThread == null) {
				bool htmlOut = Dispatcher.GetCurrent().Output == OutputType.Html;
				FileLog.writeThread = new Thread(() => {
					while (true) { 
						Thread.Sleep(Dispatcher.LogWriteMilisecond);
						FileLog.writeAllStores(htmlOut);
					}
				});
				FileLog.writeThread.IsBackground = true;
				FileLog.writeThread.Start();
			}
		}
		internal static void Disposed () {
			if (FileLog.writeThread != null) FileLog.writeThread.Abort();
			FileLog.writeAllStores(Dispatcher.GetCurrent().Output == OutputType.Html);
		}
		internal static void Log (string content, string level) {
			if (Dispatcher.Levels[level] == 0) return;
			lock (FileLog.storesLock) {
				FileLog.stores[level].Append(content);
			}
			if (FileLog.writeThread == null) FileLog.writeImmediately(level);
		}
		/*************************************************************************/
		protected static void writeImmediately (string level) {
			string[] levelsToWrite = new string[] { };
			lock (FileLog.writingLock) {
				if (!FileLog.loadedStores.Contains(level)) FileLog.loadedStores.Add(level);
				if (!FileLog.writing) {
					FileLog.writing = true;
					levelsToWrite = FileLog.loadedStores.ToArray();
					FileLog.loadedStores = new List<string>();
				}
			}
			if (levelsToWrite.Length > 0) FileLog.writeStores(Dispatcher.GetCurrent().Output == OutputType.Html, levelsToWrite);
		}
		protected static void writeStores (bool htmlOut, params string[] levels) {
			Dictionary<string, string> stores = FileLog.duplicateStores(levels);
			foreach (var item in stores) {
				if (item.Value.Length == 0) continue;
				try {
					FileLog.writeStore(item.Key, item.Value, htmlOut);
				} catch (Exception e) {
				}
			}
			lock (FileLog.writingLock) {
				if (FileLog.loadedStores.Count > 0) {
					string[] levelsToWrite = FileLog.loadedStores.ToArray();
					FileLog.loadedStores = new List<string>();
					FileLog.writeStores(htmlOut, levelsToWrite);
				} else {
					FileLog.writing = false;
				}
			}
		}
		protected static void writeAllStores (bool htmlOut) {
			Dictionary<string, string> stores = FileLog.duplicateStores();
			foreach (var item in stores) {
				if (item.Value.Length == 0) continue;
				try {
					FileLog.writeStore(item.Key, item.Value, htmlOut);
				} catch (Exception e) {
				}
			}
		}
		protected static Dictionary<string, string> duplicateStores (params string[] levels) {
			Dictionary<string, string> result = new Dictionary<string, string>();
			string level;
			StringBuilder levelStore;
			if (levels.Length > 0) {
				lock (FileLog.storesLock) {
					for (int i = 0, l = levels.Length; i < l; i += 1) {
						level = levels[i];
						levelStore = FileLog.stores[level];
						result.Add(level, levelStore.ToString());
						levelStore.Clear();
					}
				}
			} else {
				lock (FileLog.storesLock) {
					foreach (var item in FileLog.stores) {
						levelStore = item.Value;
						result.Add(item.Key, levelStore.ToString());
						levelStore.Clear();
					}
				}
			}
			return result;
		}
		protected static bool writeStore (string filename, string writeContent, bool htmlOut) {
            string fullPath = FileLog.getFullPathFromFilename(filename, htmlOut);
			bool logBegin = !File.Exists(fullPath) || (File.Exists(fullPath) && new FileInfo(fullPath).Length < 4 /* utf8bom has length 3 */);
            if (logBegin) {
				if (htmlOut) writeContent = FileLog.getHtmlLogFileBegin(filename) + writeContent;
				return FileLog.writeFileStream(fullPath, writeContent,  true);
            } else {
				if ((new FileInfo(fullPath)).Length < FileLog.MAX_LOG_FILE_SIZE) {
                    // append into file
                    return FileLog.writeFileStream(fullPath, writeContent, false);
                } else {
                    // create new brotherhood log file (recursion)
                    filename = FileLog.getNewNumberedLogFilename(filename);
					return FileLog.writeStore(filename, writeContent, htmlOut);
                }
            }
        }
		protected static string getHtmlLogFileBegin (string filename) {
			return FileLog.htmlLogFileBegin[0] + filename + FileLog.htmlLogFileBegin[1];
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
		protected static string getFullPathFromFilename(string filename, bool htmlOut) {
			string fullPath = Dispatcher.Directory + "/" + filename;
			fullPath += htmlOut ? FileLog.LOGS_EXT_HTML : FileLog.LOGS_EXT_TEXT;
            return fullPath;
        }
	}
}