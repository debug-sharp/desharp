using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Compilation;

namespace Desharp.Core {
	/// <summary>
	/// Usefull static functions used by Desharp assembly internaly, but it should be used for any general purposes.
	/// </summary>
    public class Tools {
		internal static string Editor = "";
		private const string _EDITOR_DEFAULT = "MSVS2015";
		private static Dictionary<string, string> _versionsVsEditors = new Dictionary<string, string>() {
			{  "7.0", "MSVS2002" },
			{  "7.1", "MSVS2003" },
			{  "8.0", "MSVS2005" },
			{  "9.0", "MSVS2008" },
			{ "10.0", "MSVS2010" },
			{ "11.0", "MSVS2012" },
			{ "12.0", "MSVS2013" },
			{ "14.0", "MSVS2015" },
			{ "16.0", "MSVS2017" },
		};
		static Tools () {
			// prefered editor for <a href="editor://open/..."></a> links
			string cfgEditor = Config.GetEditor();
			if (cfgEditor == null || cfgEditor.Length == 0) {
				// detect newest visual studio version on this computer automaticly
				int majorVersion = 0;
				int minorVersion = 0;
				RegistryKey registry = Registry.ClassesRoot;
				string[] allSubKeyNames = registry.GetSubKeyNames();
				List<string> subKeyNames = new List<string>();
				string visualStudioSubstr = "VisualStudio.edmx";
				Regex regex = new Regex(@"^VisualStudio\.edmx\.(\d+)\.(\d+)$");
				foreach (string subKeyName in allSubKeyNames) {
					if (subKeyName.IndexOf(visualStudioSubstr) == 0) {
						subKeyNames.Add(subKeyName);
					}
				}
				subKeyNames.Reverse();
				foreach (string subKeyName in allSubKeyNames) {
					Match match = regex.Match(subKeyName);
					if (match.Success) {
						majorVersion = Int32.Parse(match.Groups[1].Value);
						minorVersion = Int32.Parse(match.Groups[2].Value);
						break;
					}
				}
				string key = majorVersion + "." + minorVersion;
				if (Tools._versionsVsEditors.ContainsKey(key)) {
					cfgEditor = Tools._versionsVsEditors[key];
				} else {
					cfgEditor = Tools._EDITOR_DEFAULT;
				}
			}
			Tools.Editor = cfgEditor;
		}
		internal static string RelativeSourceFullPath (string fileName) {
			int appRootPos = fileName.IndexOf(Dispatcher.AppRoot);
			if (appRootPos == 0) {
				fileName = fileName.Substring(Dispatcher.AppRoot.Length);
			} else if (Dispatcher.SourcesRoot.Length > 0) {
				appRootPos = fileName.IndexOf(Dispatcher.SourcesRoot);
				if (appRootPos == 0) fileName = fileName.Substring(Dispatcher.SourcesRoot.Length);
			}
			return fileName;
		}
		/// <summary>
		/// Return client request IP address, in IPv4 or in IPv6 format, depends on client.
		/// </summary>
		public static string GetClientIpAddress () {
            string clientIpAddress = "";
            NameValueCollection serverVariables = System.Web.HttpContext.Current.Request.ServerVariables;
            string ipAddress = serverVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(ipAddress)) {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0) clientIpAddress = addresses[0].Trim(new char[] { ' ', '\r', '\n', '\t', '\v' });
            }
            if (string.IsNullOrEmpty(clientIpAddress)) clientIpAddress = serverVariables["REMOTE_ADDR"];
            return clientIpAddress;
        }
		/// <summary>
		/// Returns true if called assembly is builded as Debug release.
		/// </summary>
		/// <param name="assembly">Assembly to check for build mode.</param>
		public static bool IsAssemblyBuildAsDebug (Assembly assembly = null) {
			if (assembly == null) return false;
			object[] customAttributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);
			if ((customAttributes != null) && (customAttributes.Length == 1)) {
				DebuggableAttribute attribute = customAttributes[0] as DebuggableAttribute;
				return (attribute.IsJITOptimizerDisabled && attribute.IsJITTrackingEnabled);
			}
			return false;
		}
		/// <summary>
		/// Get desktop application entry assembly written by developer, no .NET framework system assembly.
		/// </summary>
		public static Assembly GetWindowsEntryAssembly () {
			return Assembly.GetEntryAssembly();
		}
		/// <summary>
		/// Get web application hosted in IIS server entry assembly written by developer, no .NET framework system assembly.
		/// </summary>
		public static Assembly GetWebEntryAssembly () {
			return BuildManager.GetGlobalAsaxType().BaseType.Assembly;
			/*
			if (HttpContext.Current == null || HttpContext.Current.ApplicationInstance == null) {
				return null;
			}
			int safeCounter = 0;
			Type type = System.Web.HttpContext.Current.ApplicationInstance.GetType();
			while (type != null && type.Namespace == "ASP" && safeCounter < 50) {
				type = type.BaseType;
				safeCounter++;
			}
			return type == null ? null : type.Assembly;
			*/
		}
		/// <summary>
		/// Get web request id, by HttpContext.Current.Timestamp.Ticks;
		/// </summary>
		/// <returns></returns>
		public static long GetRequestId () {
            if (HttpContext.Current == null) return 0; // windows, unit testing
            return HttpContext.Current.Timestamp.Ticks;
        }
		/// <summary>
		/// Get system process id, standardly displayed integer in Task manager.
		/// </summary>
		public static long GetProcessId () {
			return Process.GetCurrentProcess().Id;
		}
		/// <summary>
		/// Get .NET thread id of current process thread.
		/// </summary>
		public static long GetThreadId () {
            return Thread.CurrentThread.ManagedThreadId;
		}
		/// <summary>
		/// Convert any sstring or HTML into safe javascript string value representation.
		/// </summary>
		/// <param name="value">Any string to escape for safe javascript string.</param>
		/// <returns>Safe javascript string.</returns>
		public static string JavascriptString (string value) {
			return HttpUtility.JavaScriptStringEncode(value, false)
				.Replace("\\u003c", "<").Replace("\\u003e", ">").Replace("\\u0026", "&");
		}
		/// <summary>
		/// Convert any string into HTML entities where necessary.
		/// </summary>
		/// <param name="value">Any string value to process.</param>
		/// <returns>Safe HTML string.</returns>
		public static string HtmlEntities (string value) {
			value = HttpUtility.JavaScriptStringEncode(value);
			value = value.Replace(@"\\", @"&#92;");
			Regex r = new Regex(@"\\u([0-9a-f]{4})");
			MatchCollection m = r.Matches(value);
			long intItem;
			if (m.Count > 0) {
				string newValue = value.Substring(0, m[0].Index);
				int i = 0;
				int start;
				foreach (Match item in m) {
					intItem = Convert.ToInt64(item.Value.Substring(2), 16);
					newValue += "&#" + intItem.ToString() + ";";
					if (i + 1 < m.Count) {
						start = item.Index + 6;
						newValue += value.Substring(start, m[i + 1].Index - start);
					} else {
						newValue += value.Substring(item.Index + 6);
					}
					i++;
				}
				value = newValue;
			}
			return value;
		}
		/// <summary>
		/// Create MD5 hash from any string value.
		/// </summary>
		/// <param name="s">String value to hash.</param>
        public static string Md5 (string s) {
            System.Security.Cryptography.MD5 md5Hash = System.Security.Cryptography.MD5.Create();
            byte[] data = md5Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++) {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
		/// <summary>
		/// Return string with so many spaces as defined by first param.
		/// </summary>
		/// <param name="spaces">number of spaces in result string.</param>
		/// <param name="htmlOut">If true, all spaces will be rendered as &nbsp; entity.</param>
		public static string SpaceIndent (int spaces = 0, bool htmlOut = true) {
			string s = "";
			for (var i = 0; i < spaces; i++) {
				s += htmlOut ? "&nbsp;" : " ";
			}
			return s;
		}
		/// <summary>
		/// Determinate if platform is windows OS - PlatformID.Win32NT | Win32S | Win32Windows | WinCE;
		/// </summary>
		public static bool IsWindows () {
			return Environment.OSVersion.Platform == PlatformID.Win32NT ||
				Environment.OSVersion.Platform == PlatformID.Win32S ||
				Environment.OSVersion.Platform == PlatformID.Win32Windows ||
				Environment.OSVersion.Platform == PlatformID.WinCE;
		}
		/// <summary>
		/// Return Type object by string in forms: "Full.Class.Name" or "AssemblyName:Full.Class.Name", desired type should be in any loaded assembly in memory, not just in current assembly.
		/// </summary>
		/// <param name="fullClassName" type="String">"Full.Class.Name" or "AssemblyName:Full.Class.Name"</param>
		/// <returns type="Type">Desired type</returns>
		public static Type GetTypeGlobaly (string fullClassName) {
			// classic way to get type from current assembly
			Type type = Type.GetType(fullClassName);
			if (type is Type) return type;
			// try to get assembly by assembly name and full class name
			if (fullClassName.IndexOf(":") > 0) {
				string[] fullNameAndAssembly = fullClassName.Split(':');
				type = Tools.GetTypeGlobaly(fullNameAndAssembly[0], fullNameAndAssembly[1]);
				if (type is Type) return type;
			}
			// get all loaded assemblies
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies) {
				type = assembly.GetType(fullClassName);
				if (type is Type) break;
			}
			return type;
		}
		/// <summary>
		/// Return Type object by two strings in form: "AssemblyName", "Full.Class.Name", desired type should be in any loaded assembly in memory, not just in current assembly.
		/// </summary>
		/// <param name="assemblyName" type="String">"AssemblyName" for AssemblyName.dll</param>
		/// <param name="fullClassName" type="String">Full class name including namespace</param>
		/// <returns type="Type">Desired type</returns>
		public static Type GetTypeGlobaly (string assemblyName, string fullClassName) {
			// do not use this - it gets all loaded assemblies into memory
			//Dim assemblies As Reflection.Assembly() = AppDomain.CurrentDomain.GetAssemblies()
			// do not use this - it gets all assemblies from references
			//AppDomain.CurrentDomain.GetAssemblies()
			Type type = null;
			// use this - it gets all assemblies in aplication directory
			try {
				IEnumerable<Assembly> assemblies =
				from file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)
				where Path.GetExtension(file) == ".dll"
				select Assembly.LoadFrom(file);
				foreach (Assembly assembly in assemblies) {
					if (assembly.GetName().Name == assemblyName) {
						type = assembly.GetType(fullClassName);
						break;
					}
				}
			} catch (Exception e) {
			}
			return type;

		}
	}
}
