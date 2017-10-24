using System;
using System.IO;
using System.Linq;
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

        protected static ReaderWriterLockSlim wrigingBgThreadLock = new ReaderWriterLockSlim();
        protected static volatile bool wrigingBgThreadIsRunning = false;
        protected static volatile Thread wrigingBgThread = null;

        protected volatile static Dictionary<string, StringBuilder> stores;
        protected static volatile Dictionary<string, ReaderWriterLockSlim> storesAppendingLocks;
        protected static volatile Dictionary<string, ReaderWriterLockSlim> hddWritingLocks;
        
        internal static void StaticInit () {
			FileLog.stores = new Dictionary<string, StringBuilder>();
            FileLog.storesAppendingLocks = new Dictionary<string, ReaderWriterLockSlim>();
            FileLog.hddWritingLocks = new Dictionary<string, ReaderWriterLockSlim>();
			foreach (var item in LevelValues.Values) {
				FileLog.stores.Add(item.Value, new StringBuilder());
                FileLog.storesAppendingLocks.Add(item.Value, new ReaderWriterLockSlim());
                FileLog.hddWritingLocks.Add(item.Value, new ReaderWriterLockSlim());
            }
			FileLog.stores.Add("exception", new StringBuilder());
            FileLog.storesAppendingLocks.Add("exception", new ReaderWriterLockSlim());
            FileLog.hddWritingLocks.Add("exception", new ReaderWriterLockSlim());
            FileLog.htmlLogFileBegin = new string[] {
				@"<!DOCTYPE HTML><html lang=""en-US""><head><meta charset=""UTF-8""/><title>",
				"</title><style>" 
				+ Assets.logs_css + Assets.dumps_css + Assets.exception_css 
				+ "</style><script>" 
				+ Assets.logs_js.Replace("__LINK_EDITOR__", Tools.Editor) 
				+ @"</script></head><body id=""desharp"">" 
				+ Environment.NewLine
			};
			FileLog.InitBackgroundWritingIfNecessary();
		}
		internal static void InitBackgroundWritingIfNecessary () {
            FileLog.wrigingBgThreadLock.EnterUpgradeableReadLock();
            if (Dispatcher.LogWriteMilisecond > 0 && FileLog.wrigingBgThreadIsRunning) {
                FileLog.wrigingBgThreadLock.EnterWriteLock();
                FileLog.wrigingBgThreadLock.ExitUpgradeableReadLock();
                bool htmlOut = Dispatcher.GetCurrent().Output == LogFormat.Html;
                FileLog.wrigingBgThread = new Thread(() => {
                    while (true) {
                        Thread.Sleep(Dispatcher.LogWriteMilisecond);
                        FileLog.writeAllStores(htmlOut);
                    }
                });
                FileLog.wrigingBgThread.IsBackground = true;
                FileLog.wrigingBgThread.Start();
                FileLog.wrigingBgThreadIsRunning = true;
                FileLog.wrigingBgThreadLock.ExitWriteLock();
            } else {
                FileLog.wrigingBgThreadLock.ExitUpgradeableReadLock();
            }
		}
		internal static void Disposed () {
            FileLog.wrigingBgThreadLock.EnterUpgradeableReadLock();
            if (FileLog.wrigingBgThreadIsRunning) {
                FileLog.wrigingBgThreadLock.EnterWriteLock();
                FileLog.wrigingBgThreadLock.ExitUpgradeableReadLock();
                FileLog.wrigingBgThread.Abort();
                FileLog.writeAllStores(Dispatcher.GetCurrent().Output == LogFormat.Html);
                FileLog.wrigingBgThreadLock.ExitWriteLock();
            } else {
                FileLog.wrigingBgThreadLock.ExitUpgradeableReadLock();
            }
		}
		internal static void Log (string content, string level) {
			if (Dispatcher.Levels[level] == 0) return;
            ReaderWriterLockSlim lockObj;
            FileLog.wrigingBgThreadLock.EnterReadLock();
            if (FileLog.wrigingBgThreadIsRunning) {
                FileLog.wrigingBgThreadLock.ExitReadLock();
                lockObj = FileLog.storesAppendingLocks[level];
                lockObj.EnterWriteLock();
                FileLog.stores[level].Append(content);
                lockObj.ExitWriteLock();
            } else {
                FileLog.wrigingBgThreadLock.ExitReadLock();
                lockObj = FileLog.hddWritingLocks[level];
                lockObj.EnterWriteLock();
                FileLog.writeStore(level, content, Dispatcher.GetCurrent().Output == LogFormat.Html);
                lockObj.ExitWriteLock();
            }
        }
        /*************************************************************************/
        protected static void writeAllStores (bool htmlOut) {
            // duplicate all stores an clear duplicate records
            string[] storeKeys = LevelValues.Values.Values.ToArray();
            Dictionary<string, string> duplicatedStores = new Dictionary<string, string>();
            List<string> allStoreKeys = FileLog.stores.Keys.ToList<string>();
            allStoreKeys.Add("exception");
            ReaderWriterLockSlim lockObj;
            foreach (string storeKey in allStoreKeys) {
                lockObj = FileLog.storesAppendingLocks[storeKey];
                lockObj.EnterWriteLock();
                duplicatedStores[storeKey] = FileLog.stores[storeKey].ToString();
                FileLog.stores[storeKey].Clear();
                lockObj.ExitWriteLock();
            }
            foreach (var item in duplicatedStores) {
                lockObj = FileLog.hddWritingLocks[item.Key];
                lockObj.EnterWriteLock();
                try {
                    FileLog.writeStore(item.Key, item.Value, htmlOut);
                } catch {}
                lockObj.ExitWriteLock();
            }
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
            FileStream stream = null;
			try {
				stream = new FileStream(fullPath, fromStart ? FileMode.Create : FileMode.Append);
				byte[] bytes = new UTF8Encoding(true).GetBytes(value);
				stream.Write(bytes, 0, bytes.Length);
				stream.Flush();
				r = true;
			} catch (Exception e) {
				r = false;
			} finally {
				if (stream != null) stream.Close();
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