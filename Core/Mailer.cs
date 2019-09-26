using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;

namespace Desharp.Core {
	internal class Mailer {
		internal const string MAIL_SENDED_FILE = "notified";
		internal const string MAIL_NOT_SENDED_FILE = "notify-fail";
		protected static bool? failureFileExists = null;
		protected static List<string> successNotifycationLevels = null;
		protected static Thread bgNotifyThread = null;
        protected static object queueLock = new object { };
        protected static volatile List<object[]> queue = new List<object[]>();
		protected static JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
		protected static Dictionary<string, object> notifySettings = null;
		internal static void Notify (string msg, string logLevel, bool htmlOut) {
			lock (Mailer.queueLock) {
				if (Mailer.notifySettings == null) 
					Mailer.notifySettings = Config.GetNotifySettings();
				if (Mailer.notifySettings.ContainsKey("background") && (bool)Mailer.notifySettings["background"] == false) {
					Mailer.bgNotify(msg, logLevel, htmlOut);
				} else {
					Mailer.queue.Add(new object[] { msg, logLevel, htmlOut });
					bool notifyThreadNotAlive = Mailer.bgNotifyThread is Thread && !Mailer.bgNotifyThread.IsAlive;
					if (Mailer.bgNotifyThread == null || notifyThreadNotAlive) {
						if (notifyThreadNotAlive) Mailer.bgNotifyThread.Abort();
						Mailer.bgNotifyThread = new Thread(new ThreadStart(delegate () {
							object[] args = Mailer.unshiftQueue();
							if (args.Length == 0) return;
							Mailer.bgNotify(args[0].ToString(), args[1].ToString(), (bool)args[2]);
						}));
						Mailer.bgNotifyThread.IsBackground = true;
						Mailer.bgNotifyThread.Start();
					}
				}
			}
		}
		protected static object[] unshiftQueue (bool useLock = true) {
			object[] result = new object[] { };
			lock(Mailer.queueLock) { 
				if (Mailer.queue.Count > 0) {
					result = Mailer.queue[0];
					Mailer.queue.RemoveAt(0);
				}
			}
			return result;
		}
		protected static void bgNotify (string msg, string logLevel, bool htmlOut) {
			if (Mailer.failFileExist() || Mailer.notificationSended(logLevel)) return;
			Dictionary<string, object> notifySettings = Mailer.notifySettings;
			try {
				bool errorMsgPresented = (notifySettings.Count == 1 && notifySettings.ContainsKey("error"));
				if (notifySettings.Count == 0 || errorMsgPresented)
					throw new Exception(
						"Configuration doesn't contain key 'Desharp:NotifySettings' or it has wrong JSON format. Try:"
						+ Environment.NewLine
						+ @"<add key=""Desharp:NotifySettings"" value=""{host:'smtp.host.com',port:25,ssl:false,user:'username',password:'secret',from:'desharp@yourappdomain.com',to:'username@mailbox.com',priority:'high',timeout:30000}"" />"
						+ (errorMsgPresented ? Environment.NewLine + Environment.NewLine + notifySettings["error"] : "") 
					);
				MailMessage message = Mailer.getMessage(msg, logLevel, htmlOut);
				SmtpClient smtp = Mailer.getSmtpClient();
				smtp.SendCompleted += new SendCompletedEventHandler(Mailer.bgNotifySended);
				smtp.Send(message);
				Mailer.storeSuccess(logLevel);
			} catch (Exception sendException) {
				string failureStr =
					sendException.GetType().FullName + ": " + sendException.Message +
					Environment.NewLine +
					sendException.StackTrace;
				if (sendException.InnerException is Exception) {
					Exception innerEx1 = sendException.InnerException;
					failureStr += 
						Environment.NewLine +
						innerEx1.GetType().FullName + ": " + innerEx1.Message +
						Environment.NewLine +
						innerEx1.StackTrace;
					if (innerEx1.InnerException is Exception) {
						Exception innerEx2 = innerEx1.InnerException;
						failureStr += 
							Environment.NewLine +
							innerEx2.GetType().FullName + ": " + innerEx2.Message +
							Environment.NewLine +
							innerEx2.StackTrace;
					}
				}
				Mailer.storeFailure(failureStr);
			}
			object[] args = Mailer.unshiftQueue();
			if (args.Length > 0) {
				Mailer.bgNotify(args[0].ToString(), args[1].ToString(), (bool)args[2]);
			} else {
				Mailer.bgNotifyThread = null;
				Thread.CurrentThread.Abort();
			}
		}
		private static void bgNotifySended (object sender, AsyncCompletedEventArgs e) {
			string failureStr = "";
			if (e.Error is Exception) {
				failureStr = e.Error.GetType().FullName + ": " + e.Error.Message 
					+ Environment.NewLine 
					+ e.Error.StackTrace;
				if (e.Error.InnerException is Exception) {
					Exception innerEx1 = e.Error.InnerException;
					failureStr += 
						Environment.NewLine +
						innerEx1.GetType().FullName + ": " + innerEx1.Message +
						Environment.NewLine +
						innerEx1.StackTrace;
					if (innerEx1.InnerException is Exception) {
						Exception innerEx2 = innerEx1.InnerException;
						failureStr += 
							Environment.NewLine +
							innerEx2.GetType().FullName + ": " + innerEx2.Message +
							Environment.NewLine +
							innerEx2.StackTrace;
					}
				}
			} else if (e.Cancelled) {
				failureStr = "Sending notification has been canceled.";
			}
			if (failureStr.Length > 0) Mailer.storeFailure(failureStr);
		}
		protected static MailMessage getMessage (string msg, string logLevel, bool htmlOut) {
			MailMessage result = new MailMessage();
			Dictionary<string, object> notifySettings = Mailer.notifySettings;
			Assembly entryAssembly = Dispatcher.EnvType == EnvType.Web 
				? Tools.GetWebEntryAssembly() 
				: Tools.GetWindowsEntryAssembly() ;
			string logLevelName = logLevel.Substring(0, 1).ToUpper() + logLevel.Substring(1);
			result.Subject = String.Format(
				"Desharp {0} - assembly: {1}", 
				logLevelName, entryAssembly.GetName().Name
			);
			if (!notifySettings.ContainsKey("from") && !notifySettings.ContainsKey("user"))
				throw new Exception("Configuration JSON doesn't contain key 'from' or any credentials.");
			string from = notifySettings.ContainsKey("from") 
				? notifySettings["from"].ToString() 
				: notifySettings["user"].ToString();
			result.From = new MailAddress(from);
			if (!notifySettings.ContainsKey("to"))
				throw new Exception("Configuration JSON doesn't contain key 'to'.");
			string[] toRecps = notifySettings["to"].ToString().Split(new char[] { ';' });
			for (int i = 0, l = toRecps.Length; i < l; i += 1) 
				result.To.Add(new MailAddress(toRecps[i]));
			result.IsBodyHtml = htmlOut;
			result.SubjectEncoding = System.Text.Encoding.UTF8;
			result.BodyEncoding = System.Text.Encoding.UTF8;
			result.Priority = MailPriority.Normal;
			if (notifySettings.ContainsKey("priority")) {
				string priority = notifySettings["priority"].ToString();
				if (priority == "high") result.Priority = MailPriority.High;
				if (priority == "normal") result.Priority = MailPriority.Normal;
				if (priority == "low") result.Priority = MailPriority.Low;
			}
			if (htmlOut) {
				result.Body += @"<!DOCTYPE HTML><html lang=""en-US""><head><meta charset=""UTF-8"" /><style>"
					+ Assets.logs_css + ((logLevel == "exception") ? Assets.exception_css : Assets.dumps_css)
					+ @"</style></head><body id=""desharp-mail"">"
					+ ((logLevel == "exception") ? msg : @"<div class=""desharp-dump"">" + msg + "</div>")
					+ "</body></html>";
			} else {
				result.Body = msg;
			}
			return result;
		}
		protected static SmtpClient getSmtpClient () {
			Dictionary<string, object> notifySettings = Mailer.notifySettings;
			if (!notifySettings.ContainsKey("host"))
				throw new Exception("Configuration JSON doesn't contain key 'host'.");
			string host = notifySettings["host"].ToString();
			int port = notifySettings.ContainsKey("port") 
				? (int)notifySettings["port"] 
				: 25;
			SmtpClient smtp = new SmtpClient(host, port);
			if (notifySettings.ContainsKey("user") && notifySettings.ContainsKey("password")) {
				string user = notifySettings["user"].ToString();
				string password = notifySettings["password"].ToString();
				string domain = "";
				if (notifySettings.ContainsKey("domain")) 
					domain = notifySettings["domain"].ToString();
				NetworkCredential credential;
				if (domain.Length > 0) {
					credential = new NetworkCredential(user, password, domain);
				} else {
					credential = new NetworkCredential(user, password);
				}
				smtp.Credentials = credential;
			} else {
				smtp.UseDefaultCredentials = true;
			}
			smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
			smtp.EnableSsl = notifySettings.ContainsKey("ssl") ? (bool)notifySettings["ssl"] : false;
			smtp.Timeout = notifySettings.ContainsKey("timeout") ? (int)notifySettings["timeout"] : 10000;
			return smtp;
		}
		protected static bool failFileExist () {
			if (!Mailer.failureFileExists.HasValue) {
				Mailer.failureFileExists = File.Exists(Dispatcher.Directory + "/" + Mailer.MAIL_NOT_SENDED_FILE);
			}
			return Mailer.failureFileExists.Value;
		}
		protected static bool notificationSended (string levelValue) {
			if (Mailer.successNotifycationLevels == null) {
				Mailer.successNotifycationLevels = new List<string>();
				string fullPath = Dispatcher.Directory + "/" + Mailer.MAIL_SENDED_FILE;
				if (File.Exists(fullPath)) {
					try {
						Mailer.successNotifycationLevels = Mailer.jsonSerializer.Deserialize<List<string>>(
                            File.ReadAllText(fullPath)
                        );
					} catch { }
				}
			}
			return Mailer.successNotifycationLevels.Contains(levelValue);
		}
		private static void storeSuccess (string logLevel) {
			Mailer.successNotifycationLevels.Add(logLevel);
			try {
				string jsonData = Mailer.jsonSerializer.Serialize(
					Mailer.successNotifycationLevels
				);
				File.WriteAllText(
					Dispatcher.Directory + "/" + Mailer.MAIL_SENDED_FILE, 
					jsonData, System.Text.Encoding.UTF8
				);
			} catch {
			}
		}
		protected static void storeFailure (string msg) {
			try {
				File.WriteAllText(
					Dispatcher.Directory + "/" + Mailer.MAIL_NOT_SENDED_FILE, 
					msg, System.Text.Encoding.UTF8
				);
			} catch {
			}
			Mailer.failureFileExists = true;
		}
	}
}