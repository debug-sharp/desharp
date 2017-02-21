using Desharp.Outputers;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Hosting;

namespace Desharp.Core {
    internal class Environment {
		internal static EnvironmentType Type;
		internal static string AppRoot;
		internal static string Directory;
        internal static int Depth = 3;
        private static bool? _enabled = null;
		private static OutputType? _output = null;
		private static List<string> _debugIps;
        private static Dictionary<long, bool> _enabledBools;
		private static Dictionary<long, OutputType> _outputTypes;
		static Environment () {
			Environment._enabledBools = new Dictionary<long, bool>();
			Environment._outputTypes = new Dictionary<long, OutputType>();
			if (HttpRuntime.AppDomainAppId != null && HostingEnvironment.IsHosted) {
                Environment.Type = EnvironmentType.Web;
				Environment.AppRoot = HttpContext.Current.Server.MapPath("~").Replace('\\', '/').TrimEnd('/');
				Environment._debugIps = Config.GetDebugIps();
            } else {
                Environment.Type = EnvironmentType.Windows;
				Environment.AppRoot = System.IO.Path.GetDirectoryName(
					System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
				).Replace('\\', '/').TrimEnd('/');
			}
            int cfgDepth = Config.GetDepth();
            if (cfgDepth > 0) Environment.Depth = cfgDepth;
            Environment.InitEnabled();
			Environment.InitOutput();
			Environment.InitDirectory(Config.GetDirectory());
		}
		internal static void InitEnabled () {
			// determinate enabled bool globaly by main process compilation mode
			if (!Environment._enabled.HasValue) {
				if (System.Diagnostics.Debugger.IsAttached) {
					Environment._enabled = true;
				} else {
					Environment._enabled = Config.GetEnabled() == true;
				}
			}
			// for web - determinate enabled boolean for current request if there are any debug ips
			if (Environment.Type == EnvironmentType.Web) {
				bool enabled = false;
				long crt = Tools.GetRequestId();
				if (Environment._debugIps.Count > 0) {
					string clientIpAddress = Tools.GetClientIpAddress().ToLower();
					if (Environment._debugIps.Contains(clientIpAddress)) enabled = Environment._enabled == true;
					Environment._enabledBools[crt] = enabled;
				} else {
					Environment._enabledBools[crt] = Environment._enabled == true;
				}
			}
		}
		internal static void InitOutput () {
			if (Environment.Type == EnvironmentType.Web) {
				long crt = Tools.GetRequestId();
				OutputType defaultOutType = OutputType.Html;
				if (Config.GetOutput().HasValue) {
					defaultOutType = Config.GetOutput() == OutputType.Html ? OutputType.Html : OutputType.Text;
				}
				OutputType outType = Environment._output.HasValue ? (Environment._output == OutputType.Html ? OutputType.Html : OutputType.Text) : defaultOutType;
				Environment._outputTypes[crt] = outType;
			} else {
				if (Config.GetOutput().HasValue) {
					Environment._output = Config.GetOutput();
				} else {
					Environment._output = OutputType.Text;
				}
			}
		}
		internal static void InitDirectory (string dirRelOrFullPath = "") {
			string fullPath;
			if (dirRelOrFullPath.Length > 0) {
				fullPath = dirRelOrFullPath;
				if (dirRelOrFullPath.IndexOf("~") == 0) {
					dirRelOrFullPath = "~" + dirRelOrFullPath;
					dirRelOrFullPath.Replace("~", Environment.AppRoot);
				}
				fullPath = fullPath.Replace('\\', '/').TrimEnd('/');
			} else {
				fullPath = Environment.AppRoot;
			}
			Environment.Directory = fullPath;
			// create Logs directory if necessary
			if (Environment.AppRoot != Environment.Directory) {
				if (!(System.IO.Directory.Exists(Environment.Directory))) {
					try {
						System.IO.Directory.CreateDirectory(Environment.Directory);
					} catch (Exception e) { }
				}
			}
		}
		internal static bool GetEnabled () {
			if (Environment.Type == EnvironmentType.Web) {
				long crt = Tools.GetRequestId();
				if (!Environment._enabledBools.ContainsKey(crt)) Environment.InitEnabled();
				return Environment._enabledBools[crt];
			} else {
				return Environment._enabled == true;
			}
		}

		internal static OutputType GetOutput () {
			if (Environment.Type == EnvironmentType.Web) {
				long crt = Tools.GetRequestId();
				if (!Environment._outputTypes.ContainsKey(crt)) Environment.InitOutput();
				return Environment._outputTypes[crt];
			} else {
				return Environment._output == OutputType.Html ? OutputType.Html : OutputType.Text;
			}
		}
		internal static void Configure (DebugConfig cfg) {
			if (cfg.EnvironmentType != EnvironmentType.Auto) Environment.Type = cfg.EnvironmentType;
			if (cfg.Enabled.HasValue) {
				Environment._enabled = cfg.Enabled == true;
				if (Environment.Type == EnvironmentType.Web) {
					long crt = Tools.GetRequestId();
					if (Environment._enabledBools.ContainsKey(crt)) {
						Environment._enabledBools[crt] = cfg.Enabled == true;
					} else {
						Environment._enabledBools.Add(crt, cfg.Enabled == true);
					}
				}
			}
			if (cfg.OutputType != OutputType.Auto) {
				if (Environment.Type == EnvironmentType.Web) {
					long crt = Tools.GetRequestId();
					if (Environment._outputTypes.ContainsKey(crt)) {
						Environment._outputTypes[crt] = cfg.OutputType;
					} else {
						Environment._outputTypes.Add(crt, cfg.OutputType);
					}
				} else {
					Environment._output = cfg.OutputType;
				}
			}
			if (cfg.Directory != null && cfg.Directory.Length > 0) {
				Environment.InitDirectory(cfg.Directory);
            }
            if (cfg.Depth != null && cfg.Depth.Value > 0) {
                Environment.Depth = cfg.Depth.Value;
            }
        }
        internal static void WriteOutput (string dumpedCode) {
            if (Environment.Type == EnvironmentType.Web) {
                HtmlResponse.GetCurrentOutputBuffer().Append(dumpedCode);
            } else {
                Console.WriteLine(dumpedCode);
            }
		}
		internal static void RequestBegin (long crt) {
			Environment.InitEnabled();
			Environment.InitOutput();
		}
		internal static void RequestEnd (long crt) {
            if (Environment._enabledBools.ContainsKey(crt)) Environment._enabledBools.Remove(crt);
			if (Environment._outputTypes.ContainsKey(crt)) Environment._outputTypes.Remove(crt);
		}
	}
}
