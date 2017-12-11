using Desharp.Producers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.SessionState;
using static Desharp.Core.AppExitWatcher;

namespace Desharp.Core {
	internal class Dispatcher {
		internal static bool StaticInitialized = false;
        internal static ReaderWriterLockSlim StaticInitLock = new ReaderWriterLockSlim();
		internal static EnvType EnvType;
		internal static string AppRoot;
		internal static string SourcesRoot;
		internal static string Directory;
		internal static int LogWriteMilisecond = 0;
		internal static List<string> WebDebugIps = null;
		internal static int DumpDepth = 3;
		internal static int DumpMaxLength = 1024;
		internal static bool? EnabledGlobal = null;
        internal static bool DumpCompillerGenerated = false;
        internal static LogFormat? OutputGlobal = null;
		internal static Dictionary<string, int> Levels;
		internal static string WebStaticErrorPage;
		internal static readonly VirtualPathProvider VirtualPathProvider;

		protected static List<string> webHtmlXmlMimeTypes = new List<string>() {
			"text/html", "application/xhtml+xml", "text/xml",
			"application/xml", "image/svg+xml", "application/rss+xml",
		};
		
		//protected static ReaderWriterLockSlim dispatchersLock = new ReaderWriterLockSlim();
		protected static object dispatchersLock = new object { };
		protected static volatile Dictionary<string, Dispatcher> dispatchers = new Dictionary<string, Dispatcher>();

		protected static string callContextKey = typeof(Dispatcher).FullName;
		protected static Dictionary<string, Type> webBarRegisteredPanels = new Dictionary<string, Type>();

        internal Exception LastError = null;
		internal int DumperSequence = 0;
		internal string CurrentlyRendererView = "";
		internal LogFormat Output;
		internal bool? Enabled = null;
		internal Dictionary<string, double> Timers = new Dictionary<string, double>();
		internal bool WebAssetsInserted = false;
		internal int WebRequestState = 0;
		internal double WebRequestEndTime = 0;

        internal FireDump FireDump = null;
        protected bool? webRedirect = false;
		// -1 - it will be never rendered, 0 - it shoud be rendered but will not by default, 1 - it will be rendered
		protected int webRenderDesharpBar = 0;
		protected bool webTransmitErrorPage = false;
		protected List<List<RenderedPanel>> webReqEndSession = null;
		protected Dictionary<string, Panels.IPanel> webBarPanels = null;
		protected List<string> webExceptions = null;

		static Dispatcher () {
            Dispatcher.StaticInitLock.EnterUpgradeableReadLock();
			if (Dispatcher.StaticInitialized) {
				Dispatcher.StaticInitLock.ExitUpgradeableReadLock();
				return;
			}
			Dispatcher.StaticInitLock.EnterWriteLock();
			Dispatcher.StaticInitLock.ExitUpgradeableReadLock();
			int cfgDepth = Config.GetDepth();
            if (cfgDepth > 0) Dispatcher.DumpDepth = cfgDepth;
            int cfgMaxLength = Config.GetMaxLength();
            if (cfgMaxLength > 0) Dispatcher.DumpMaxLength = cfgMaxLength;
            Dispatcher.Levels = Config.GetLevels();
            Dispatcher.LogWriteMilisecond = Config.GetLogWriteMilisecond();
            Dispatcher.VirtualPathProvider = HostingEnvironment.VirtualPathProvider;
            if (HttpRuntime.AppDomainAppId != null && HostingEnvironment.IsHosted) {
                Dispatcher.EnvType = EnvType.Web;
                Dispatcher.AppRoot = HttpContext.Current.Server.MapPath("~").Replace('\\', '/').TrimEnd('/');
                Dispatcher.WebDebugIps = Config.GetDebugIps();
                Dispatcher.staticInitWebRegisterPanels(typeof(Panels.Exceptions), typeof(Panels.Dumps));
                Dispatcher.staticInitWebRegisterPanels(Config.GetDebugPanels());
                Dispatcher.staticInitWebErrorPage(Config.GetErrorPage());
            } else {
                Dispatcher.EnvType = EnvType.Windows;
                Dispatcher.AppRoot = System.IO.Path.GetDirectoryName(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
                ).Replace('\\', '/').TrimEnd('/');
                AppDomain.CurrentDomain.UnhandledException += delegate (object o, UnhandledExceptionEventArgs e) {
                    Debug.Log(e.ExceptionObject as Exception);
                    if (e.IsTerminating) Dispatcher.Disposed();
                };
                if (Dispatcher.LogWriteMilisecond > 0) {
                    bool exited = false;
                    AppDomain.CurrentDomain.ProcessExit += delegate (object o, EventArgs e) {
                        Dispatcher.Disposed();
                        exited = true;
                    };
                    AppExitWatcher.SetConsoleCtrlHandler(new HandlerRoutine((type) => {
                        if (exited) return true;
                        Dispatcher.Disposed();
                        return true;
                    }), true);
                }
            }
            if (Tools.IsWindows()) Dispatcher.AppRoot = Dispatcher.AppRoot.Substring(0, 1).ToUpper() + Dispatcher.AppRoot.Substring(1);
            if (
                Tools.IsWindows() && (
                    Dispatcher.AppRoot.IndexOf("/bin/Debug") == Dispatcher.AppRoot.Length - 10 ||
                    Dispatcher.AppRoot.IndexOf("/bin/Release") == Dispatcher.AppRoot.Length - 12
                )
            ) {
                Dispatcher.SourcesRoot = System.IO.Path.GetFullPath(Dispatcher.AppRoot + "/../..").Replace('\\', '/');
            } else {
                Dispatcher.SourcesRoot = "";
            }
            Dispatcher.staticInitEnabledGlobal();
            Dispatcher.staticInitOutputGlobal();
            Dispatcher.staticInitDumpCompillerGenerated();
            Dispatcher.staticInitDirectory(Config.GetDirectory());
            FileLog.StaticInit();
            Dispatcher.StaticInitLock.ExitWriteLock();
		}
		internal static Dispatcher GetCurrent (bool createIfNecessary = true) {
			string dispatchedKey = Dispatcher.EnvType == EnvType.Web 
                ? Tools.GetRequestId().ToString() 
                : $"{Tools.GetProcessId()}:{Tools.GetThreadId()}";
            Dispatcher result = null;

			/*result = CallContext.GetData(Dispatcher.callContextKey) as Dispatcher;
			if (!(result is Dispatcher)) {
				result = new Dispatcher();
				CallContext.SetData(Dispatcher.callContextKey, result);
			}*/

			/*Dispatcher.dispatchersLock.EnterUpgradeableReadLock();
            if (!Dispatcher.dispatchers.ContainsKey(dispatchedKey) & createIfNecessary) {
                Dispatcher.dispatchersLock.EnterWriteLock();
                Dispatcher.dispatchersLock.ExitUpgradeableReadLock();
                result = new Dispatcher();
                Dispatcher.dispatchers[dispatchedKey] = result;
                Dispatcher.dispatchersLock.ExitWriteLock();
            } else {
                Dispatcher.dispatchersLock.EnterReadLock();
                Dispatcher.dispatchersLock.ExitUpgradeableReadLock();
                result = Dispatcher.dispatchers[dispatchedKey];
                Dispatcher.dispatchersLock.ExitReadLock();
            }*/

			lock (Dispatcher.dispatchersLock) {
                if (!Dispatcher.dispatchers.ContainsKey(dispatchedKey) && createIfNecessary) {
                    Dispatcher.dispatchers[dispatchedKey] = new Dispatcher();
                }
                if (Dispatcher.dispatchers.ContainsKey(dispatchedKey)) {
                    result = Dispatcher.dispatchers[dispatchedKey];
                }
            }

			return result;
		}
		internal static bool Remove () {
			bool removed = false;
            string dispatchedKey = Dispatcher.EnvType == EnvType.Web
                ? Tools.GetRequestId().ToString()
                : $"{Tools.GetProcessId()}:{Tools.GetThreadId()}";

			/*CallContext.FreeNamedDataSlot(Dispatcher.callContextKey);
			return true;*/
			
			/*Dispatcher.dispatchersLock.EnterWriteLock();
            removed = Dispatcher.dispatchers.ContainsKey(dispatchedKey)
                ? Dispatcher.dispatchers.Remove(dispatchedKey)
                : false;
            Dispatcher.dispatchersLock.ExitWriteLock();*/

			lock (Dispatcher.dispatchersLock) {
                removed = Dispatcher.dispatchers.Remove(dispatchedKey);
            }

			return removed;
		}
		internal static void Disposed () {
			FileLog.Disposed();
		}
		internal static bool WebCheckIfRequestIsForHtml () {
			string[] requestAcceptTypes = HttpContext.Current.Request.AcceptTypes;
			string requestFirstAcceptType = requestAcceptTypes.Length > 0 ? requestAcceptTypes[0].ToLower() : "";
			bool result = false;
			foreach (string mimeType in Dispatcher.webHtmlXmlMimeTypes) {
				if (mimeType.IndexOf("html") > -1) {
					if (requestFirstAcceptType.IndexOf(mimeType) > -1) {
						result = true;
						break;
					}
				} else {
					break;
				}
			}
			return result;
		}
		internal static bool WebCheckIfResponseIsHtmlOrXml (bool checkForXmlOnly = false) {
			string responseContentType = HttpContext.Current.Response.ContentType.ToLower();
			bool result = false;
			if (checkForXmlOnly) {
				foreach (string mimeType in Dispatcher.webHtmlXmlMimeTypes) {
					if (mimeType.IndexOf("xml") > -1 && responseContentType.IndexOf(mimeType) > -1) {
						result = true;
						break;
					}
				}
			} else {
				foreach (string mimeType in Dispatcher.webHtmlXmlMimeTypes) {
					if (mimeType.IndexOf("html") > -1) {
						if (responseContentType.IndexOf(mimeType) > -1) {
							result = true;
							break;
						}
					} else {
						break;
					}
				}
			}
			return result;
		}
		protected static void staticInitEnabledGlobal () {
			if (Dispatcher.EnabledGlobal.HasValue) return;
			// determinate enabled bool globaly by main process compilation mode
			bool webEnvironment = Dispatcher.EnvType == EnvType.Web;
			// first - look into config if there is strictly defined debug mode on or off
			bool? configScrictValue = Config.GetEnabled();
			if (configScrictValue.HasValue) {
				Dispatcher.EnabledGlobal = configScrictValue.Value;
			} else {
				// try to determinate debug mode by entry assembly compilation type:
				bool entryAssemblyBuildedAsDebug = Tools.IsAssemblyBuildAsDebug(
					webEnvironment ? Tools.GetWebEntryAssembly() : Tools.GetWindowsEntryAssembly()
				);
				// try to determinate debug mode by (app|web).config if there is node with attribute bellow:
				//<configuration>
				//	<system.web>
				//		<compilation 
				//			debug ="true"	<----- THIS BOOLEAN
				//			targetFramework ="4.5"/>
				//  </system.web>
				//</configuration>
				bool debugModeByConfig = webEnvironment && HttpContext.Current.IsDebuggingEnabled;
				// try to determinate if debugger from visual studio is currently attached
				bool vsDebuggerAttached = System.Diagnostics.Debugger.IsAttached;
				// now set enabled boolean to true if any of these values is true
				Dispatcher.EnabledGlobal = entryAssemblyBuildedAsDebug || debugModeByConfig || vsDebuggerAttached;
            }
            if (!webEnvironment) {
				// for desktop apps - create every second checking if debugger is attached
				WinDebuggerAttaching.GetInstance().Changed += (o, e) => {
					Dispatcher.EnabledGlobal = ((WinDebuggerAttachingEventArgs)e).Attached;
				};
			}
		}
		protected static void staticInitOutputGlobal () {
			LogFormat? strictConfigValue = Config.GetLogFormat();
			if (strictConfigValue.HasValue) {
				Dispatcher.OutputGlobal = strictConfigValue;
			} else {
				Dispatcher.OutputGlobal = LogFormat.Text;
			}
		}
        protected static void staticInitDumpCompillerGenerated () {
            bool? dumpCompillerGenerated = Config.GetDumpCompillerGenerated();
            if (dumpCompillerGenerated.HasValue) Dispatcher.DumpCompillerGenerated = dumpCompillerGenerated.Value;
        }
        protected static void staticInitDirectory (string dirRelOrFullPath = "") {
			string fullPath;
			if (dirRelOrFullPath.Length > 0) {
				fullPath = dirRelOrFullPath;
				if (fullPath.IndexOf("~") > -1) {
					fullPath = fullPath.Replace("~", Dispatcher.AppRoot);
				}
				fullPath = Path.GetFullPath(fullPath);
				fullPath = fullPath.Replace('\\', '/').TrimEnd('/');
			} else {
				fullPath = Dispatcher.AppRoot;
			}
			if (Tools.IsWindows()) fullPath = fullPath.Substring(0, 1).ToUpper() + fullPath.Substring(1);
			Dispatcher.Directory = fullPath;
			// create the directory if doesn't exists and if ot's not a root dir
			if (Dispatcher.AppRoot != Dispatcher.Directory) {
				if (!(System.IO.Directory.Exists(Dispatcher.Directory))) {
					try {
						System.IO.Directory.CreateDirectory(Dispatcher.Directory);
					} catch (Exception e) {
						Dispatcher.Directory = Dispatcher.AppRoot;
						Debug.Dump(e);
					}
				}
			}
		}
		protected static void staticInitWebRegisterPanels (params Type[] panels) {
			Type panel;
			for (int i = 0; i < panels.Length; i++) {
				panel = panels[i];
				if (!Dispatcher.webBarRegisteredPanels.ContainsKey(panel.FullName)) {
					Dispatcher.webBarRegisteredPanels.Add(panel.FullName, panel);
				}
			}
			Type sysInfoPanelType = typeof(Panels.SystemInfo);
			string sysInfoPanelName = sysInfoPanelType.FullName;
			if (Dispatcher.webBarRegisteredPanels.ContainsKey(sysInfoPanelName)) {
				Dispatcher.webBarRegisteredPanels.Remove(sysInfoPanelName);
				Dispatcher.webBarRegisteredPanels = (
					new Dictionary<string, Type> { { sysInfoPanelName, sysInfoPanelType } }
				).Concat(Dispatcher.webBarRegisteredPanels)
				.ToDictionary(k => k.Key, v => v.Value);
			}
		}
		protected static void staticInitWebErrorPage (string cfgErrorPage) {
			string errorPage = "";
			if (cfgErrorPage.Length > 0) {
				if (cfgErrorPage.IndexOf("~") > -1) {
					cfgErrorPage = cfgErrorPage.Replace("~", Dispatcher.AppRoot);
				}
				cfgErrorPage = Path.GetFullPath(cfgErrorPage);
				cfgErrorPage = cfgErrorPage.Replace('\\', '/');
				if (File.Exists(cfgErrorPage)) {
					errorPage = File.ReadAllText(cfgErrorPage);
				}
			}
			if (errorPage.Length == 0) errorPage = Assets.error;
			Dispatcher.WebStaticErrorPage = errorPage;
		}
		protected static bool webCheckIfResponseIsRedirect () {
			HttpResponse response = HttpContext.Current.Response;
			int httpStatusCode = response.StatusCode;
			bool redirectCode = (httpStatusCode >= 300 && httpStatusCode < 400);
			bool redirectHeader = false;
			string[] headerNames = response.Headers.AllKeys;
			string header;
			for (int i = 0, l = headerNames.Length; i < l; i += 1) {
				header = headerNames[i].Trim().ToLower();
				if (header.IndexOf("location") == 0 || header.IndexOf("refresh") == 0) {
					redirectHeader = true;
					break;
				}
			}
			return redirectCode || redirectHeader;
		}
		protected static bool webIsRequestToFileWithDotNetExecExtension () {
			string ext = HttpContext.Current.Request.CurrentExecutionFilePathExtension.ToLower();
			// empty string for example in all MVC apps controller/action requests...
			return ext == "" || ext == ".aspx" || ext == ".asp" || ext == ".cshtml" || ext == ".cshtm" || ext == ".vbhtml" || ext == ".vbhtm" || ext == ".ashx" || ext == ".asmx";
		}
		protected static bool webIsRequestToHomepageOrDefaultHomepageFile () {
			string relativeRequestPath = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.ToLower();
			return relativeRequestPath == "~/" || relativeRequestPath == "~/default.aspx" || relativeRequestPath == "~/index.aspx";
		}
		protected static List<List<RenderedPanel>> webGetSessionStorrage () {
			List<List<RenderedPanel>> result = new List<List<RenderedPanel>>();
			HttpSessionState session = HttpContext.Current.Session;
			if (session is HttpSessionState && session[Debug.SESSION_STORAGE_KEY] is List<List<RenderedPanel>>) {
				result = (List<List<RenderedPanel>>)session[Debug.SESSION_STORAGE_KEY];
			}
			return result;
		}

        internal Dispatcher () {
            if (Dispatcher.EnvType == EnvType.Web) {
                this.webInitEnabled();
            } else {
                this.Enabled = Dispatcher.EnabledGlobal;
            }
			if (System.Diagnostics.Debugger.IsAttached) this.Enabled = true;
			this.Output = Dispatcher.OutputGlobal.Value;
		}
        internal FireDump GetFireDump () {
            if (this.FireDump == null) this.FireDump = new FireDump(this.Enabled == true);
            return this.FireDump;
        }
		internal void Configure (DebugConfig cfg) {
			if (cfg.EnvType != EnvType.Auto) Dispatcher.EnvType = cfg.EnvType;
			if (cfg.Enabled.HasValue) this.Enabled = cfg.Enabled.Value;
			if (cfg.LogFormat != LogFormat.Auto) this.Output = cfg.LogFormat;
			if (cfg.Directory != null && cfg.Directory.Length > 0) Dispatcher.staticInitDirectory(cfg.Directory);
			if (cfg.ErrorPage != null && cfg.ErrorPage.Length > 0) Dispatcher.staticInitWebErrorPage(cfg.ErrorPage);
			if (cfg.Depth != null && cfg.Depth.Value > 0) Dispatcher.DumpDepth = cfg.Depth.Value;
			if (cfg.LogWriteMilisecond != null && cfg.LogWriteMilisecond.Value > 0) {
				Dispatcher.LogWriteMilisecond = cfg.LogWriteMilisecond.Value;
				FileLog.InitBackgroundWritingIfNecessary();
			}
			if (cfg.Panels != null && cfg.Panels.Length > 0) Dispatcher.staticInitWebRegisterPanels(cfg.Panels);
		}
		internal void WriteDumpToOutput (string dumpedCode) {
			if (Dispatcher.EnvType == EnvType.Web) {
				if (!this.Enabled.HasValue) this.webInitEnabled();
				if (this.Enabled.Value != true) {
					FileLog.Log(dumpedCode, LevelValues.Values[Level.DEBUG]);
				} else {
					if (this.webBarPanels == null) {
						this.webRenderDesharpBar = 1; // forcely change to render web bar
						this.webInitWebBarPanels();
					}
					if (this.webBarPanels.ContainsKey(Panels.Dumps.PanelName)) {
						Panels.Dumps dumpsPanel = this.webBarPanels[Panels.Dumps.PanelName] as Panels.Dumps;
						dumpsPanel.AddRenderedDump(dumpedCode);
					}
				}
			} else {
				Console.Write(dumpedCode);
			}
		}
		internal void WriteExceptionToOutput (List<string> dumpedExceptions) {
			if (Dispatcher.EnvType == EnvType.Web) {
				if (!this.Enabled.HasValue) this.webInitEnabled();
				if (this.Enabled.Value != true) {
					foreach (string dumpedException in dumpedExceptions) {
						FileLog.Log(dumpedException, "exception");
					}
				} else {
					if (this.webBarPanels == null) {
						this.webRenderDesharpBar = 1; // forcely change to render web bar
						this.webInitWebBarPanels();
					}
					if (this.webBarPanels.ContainsKey(Panels.Dumps.PanelName)) {
						Panels.Exceptions exceptionsPanel = this.webBarPanels[Panels.Exceptions.PanelName] as Panels.Exceptions;
						foreach (string dumpedException in dumpedExceptions) {
							exceptionsPanel.AddRenderedException(dumpedException);
						}
					}
				}
			} else {
				Console.WriteLine(String.Join(Environment.NewLine, dumpedExceptions.ToArray()));
			}
		}
		internal void Stop () {
			if (Dispatcher.EnvType == EnvType.Web) {
				if (this.WebRequestState == 0) this.WebRequestBegin();
				if (this.WebRequestState == 1) this.WebRequestSessionBegin();
				if (this.WebRequestState == 2) this.WebRequestSessionEnd();
				this.WebRequestEnd();
				Dispatcher.Remove();
				HttpContext.Current.Response.End();
			} else {
                Environment.Exit(Environment.ExitCode);
            }
		}
		internal void WebRequestBegin () {
			if (this.Enabled == true) {
				this.webInitBarRendering();
				this.webInitWebBarPanels();
			}
			this.WebRequestState = 1;
		}
		internal void WebRequestSessionBegin () {
			if (this.Enabled != true || this.webBarPanels == null) return;
			foreach (var item in this.webBarPanels) {
				try {
					item.Value.SessionBegin();
				} catch (Exception e) { }
			}
			this.WebRequestState = 2;
		}
		internal void WebRequestSessionEnd () {
			if (this.Enabled == true) {
				this.WebRequestEndTime = Debug.GetProcessingTime();
				HttpSessionState session = HttpContext.Current.Session;
				List<List<RenderedPanel>> sessionStorrage = Dispatcher.webGetSessionStorrage();
				this.webRedirect = Dispatcher.webCheckIfResponseIsRedirect();
				if (this.webRedirect == true) {
					this.webRequestSessionEndCallBarPanelsSessionEnd();
					List<RenderedPanel> renderedPanels = HtmlResponse.RenderDebugPanels(this.webBarPanels);
					sessionStorrage.Insert(0, renderedPanels);
					if (session is HttpSessionState) {
						session[Debug.SESSION_STORAGE_KEY] = sessionStorrage;
					}
					this.webBarPanels = null; // frees memory
				} else {
					this.webReqEndSession = sessionStorrage;
					// clear session storage, panels will be rendered in this request end event
					if (session is HttpSessionState && session[Debug.SESSION_STORAGE_KEY] != null) {
						session.Remove(Debug.SESSION_STORAGE_KEY);
					}
					this.webRequestSessionEndCallBarPanelsSessionEnd();
				}
			}
			this.WebRequestState = 3;
		}
		internal void WebRequestEnd () {
			if (this.Enabled == true) {
				if (!this.webRedirect.HasValue) this.webRedirect = Dispatcher.webCheckIfResponseIsRedirect();
				// add possible rendered exceptions and debug bar if necessary
				if (this.webRenderDesharpBar > -1 && Dispatcher.WebCheckIfResponseIsHtmlOrXml()) {
					this.webRenderDesharpBar = 1;
				}
				if (this.webRenderDesharpBar == 1 && this.webRedirect != true) {
					string responseContentType = HttpContext.Current.Response.ContentType.ToLower();
					if (!Dispatcher.WebCheckIfResponseIsHtmlOrXml()) {
						// if there was necessary to render in output anything (by response type change to text/html)
						// change response to that type if it is not any proper type to render any html code
						HttpContext.Current.Response.ContentType = "text/html";
					}
					// manage Content-Security-Policy http header
					this.webManageContentSecurityPolicyHeader();
					// render debug bar for current request with any previous redirect records from session
					List<List<RenderedPanel>> renderedPanels = this.webReqEndSession 
						?? Dispatcher.webGetSessionStorrage();
					if (this.webBarPanels != null) {
						renderedPanels.Insert(0, HtmlResponse.RenderDebugPanels(this.webBarPanels));
						this.webBarPanels = null;
					}
					HtmlResponse.WriteDebugBarToResponse(renderedPanels);
				}
			} else {
				if (this.webTransmitErrorPage && Dispatcher.WebStaticErrorPage.Length > 0)
					HtmlResponse.TransmitStaticErrorPage();
			}
		}
		protected void webManageContentSecurityPolicyHeader () {
			NameValueCollection rawHeaders = HttpContext.Current.Response.Headers;
			List<string> headers = rawHeaders.AllKeys.ToList<string>();
			string headerName = "";
			string headerValue = "";
			if (headers.Contains("Content-Security-Policy")) headerName = "Content-Security-Policy";
			if (headers.Contains("X-Content-Security-Policy")) headerName = "X-Content-Security-Policy";
			if (headerName.Length == 0) return;
			headerValue = rawHeaders[headerName];
			List<string> headerValueExploded = headerValue.Split(';').ToList<string>();
			string explodedItem;
			List<string> resultValues = new List<string>();
			bool scriptSrcCatched = false;
			bool styleSrcCatched = false;
			bool imgSrcCatched = false;
			bool fontSrcCatched = false;
			for (int i = 0, l = headerValueExploded.Count; i < l; i += 1) {
				explodedItem = headerValueExploded[i].Trim();
				if (explodedItem.IndexOf("script-src") > -1) {
					scriptSrcCatched = true;
					if (explodedItem.IndexOf("'unsafe-inline'") == -1) explodedItem += " 'unsafe-inline'";
					if (explodedItem.IndexOf("'unsafe-eval'") == -1) explodedItem += " 'unsafe-eval'";
				}
				if (explodedItem.IndexOf("style-src") > -1) {
					styleSrcCatched = true;
					if (explodedItem.IndexOf("'unsafe-inline'") == -1) explodedItem += " 'unsafe-inline'";
				}
				if (explodedItem.IndexOf("img-src") > -1) {
					imgSrcCatched = true;
					if (explodedItem.IndexOf("'self'") == -1) explodedItem += " 'self'";
					if (explodedItem.IndexOf("data:") == -1) explodedItem += " data:";
				}
				if (explodedItem.IndexOf("font-src") > -1) {
					fontSrcCatched = true;
					if (explodedItem.IndexOf("'self'") == -1) explodedItem += " 'self'";
					if (explodedItem.IndexOf("data:") == -1) explodedItem += " data:";
				}
				resultValues.Add(explodedItem);
			}
			if (!scriptSrcCatched) resultValues.Add("script-src 'unsafe-inline' 'unsafe-eval'");
			if (!styleSrcCatched) resultValues.Add("style-src 'unsafe-inline'");
			if (!imgSrcCatched) resultValues.Add("img-src 'self' data:");
			if (!fontSrcCatched) resultValues.Add("font-src 'self' data:");
			HttpContext.Current.Response.Headers.Set(headerName, String.Join("; ", resultValues.ToArray()));
		}
		internal void WebRequestError () {
			// get causing exception object
			Exception lastException = HttpContext.Current.Server.GetLastError();
			if (lastException == null) return;
			// clear stupid microsoft error screen
			HttpContext.Current.Server.ClearError();
			// get request id
			long crt = Tools.GetRequestId();
			if (this.Enabled != true) {
				// write exception into hard drive
				Debug.Log(lastException);
				// clear everything bad, what shoud be written in response
				HttpContext.Current.Response.Clear();
				// transmit error page at request end
				this.webTransmitErrorPage = true;
			} else {
				// render exception and store it for request end to send to into client
				Debug.Dump(lastException, new DumpOptions {
					CatchedException = false
				});
				// keep everything bad, what should be written in response
			}
		}
		protected void webInitEnabled () {
			this.Enabled = Dispatcher.EnabledGlobal == true;
			// if there are defined any debug ips - then allow globaly allowed debug mode only for listed client ips
			if (this.Enabled == true && Dispatcher.WebDebugIps.Count > 0) {
				string clientIpAddress = Tools.GetClientIpAddress().ToLower();
				this.Enabled = Dispatcher.WebDebugIps.Contains(clientIpAddress);
			}
		}
		protected void webRequestSessionEndCallBarPanelsSessionEnd () {
			if (this.webBarPanels != null) {
				foreach (var item in this.webBarPanels) {
					try {
						// item.Value.SessionEnd(); // do not use this line, it always calls abstract method
						MethodInfo mi = item.Value.GetType().GetMethod("SessionEnd");
						mi.Invoke(item.Value, null);
					} catch (Exception e) { }
				}
			}
		}
		protected void webInitWebBarPanels (long crt = -1) {
			if (this.webRenderDesharpBar < 1) return; // do not register any panels for non html/xml outputs
			this.webBarPanels = new Dictionary<string, Panels.IPanel>();
			Panels.IPanel panel;
			foreach (var item in Dispatcher.webBarRegisteredPanels) {
				panel = (Panels.IPanel)Activator.CreateInstance(item.Value);
				if (this.webBarPanels.ContainsKey(panel.Name)) {
					throw new Exception(String.Format(
						"Panel with name: '{0}' has been already registered, use different panel name.", panel.Name
					));
				}
				this.webBarPanels.Add(panel.Name, panel);
			}
		}
		protected void webInitBarRendering () {
			string relativeRequestPath = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath;
			if (
				Dispatcher.webIsRequestToHomepageOrDefaultHomepageFile() == false &&
				Dispatcher.webIsRequestToFileWithDotNetExecExtension() == false && (
					Dispatcher.VirtualPathProvider.FileExists(relativeRequestPath) ||
					Dispatcher.VirtualPathProvider.DirectoryExists(relativeRequestPath)
				)
			) {
				this.webRenderDesharpBar = -1;
			} else if (Dispatcher.WebCheckIfRequestIsForHtml()) {
				this.webRenderDesharpBar = 1;
			} else {
				this.webRenderDesharpBar = 0;
			}
		}
	}
}