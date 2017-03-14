using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

namespace Desharp.Core {
	internal class Config {
		internal const string APP_SETTINGS_ENABLED = "Desharp:Enabled"; // true | false | 1 | 0
		internal const string APP_SETTINGS_EDITOR = "Desharp:Editor"; // MSVS2005 | MSVS2008 | MSVS2010 | MSVS2012 | MSVS2013 | MSVS2015 | MSVS2017
		internal const string APP_SETTINGS_OUTPUT = "Desharp:Output"; // text | html
		internal const string APP_SETTINGS_DEBUG_IPS = "Desharp:DebugIps"; // 127.0.0.1,88.31.45.67,...
		internal const string APP_SETTINGS_LEVELS = "Desharp:Levels"; // exception,-debug,info,notice,warning,error,critical,alert,emergency,-javascript
		internal const string APP_SETTINGS_PANELS = "Desharp:Panels"; // Desharp.Panels.Session,Desharp.Panels.Routing
		internal const string APP_SETTINGS_DIRECTORY = "Desharp:Directory"; // ~/logs
        internal const string APP_SETTINGS_DEPTH = "Desharp:Depth"; // 5
		internal const string APP_SETTINGS_MAX_LENGTH = "Desharp:MaxLength"; // 1024
		private static Dictionary<string, string> _appSettings = new Dictionary<string, string>();
		static Config () {
			string itemKey;
			string itemValue;
			foreach (string key in ConfigurationManager.AppSettings) {
				itemKey = key.Trim();
				itemValue = ConfigurationManager.AppSettings[key].ToString();
				if (itemKey.ToLower().IndexOf("desharp:") == 0) { 
					Config._appSettings[itemKey] = itemValue.Trim();
				}
			}
		}
		internal static bool? GetEnabled () {
			if (Config._appSettings.ContainsKey(Config.APP_SETTINGS_ENABLED)) {
				string rawValue = Config._appSettings[Config.APP_SETTINGS_ENABLED].Trim().ToLower();
				return (rawValue == "false" || rawValue == "0" || rawValue == "") ? false : true;
			} else {
				return null;
			}
		}
		internal static Type[] GetDebugPanels () {
			List<Type> result = new List<Type>();
			if (Config._appSettings.ContainsKey(Config.APP_SETTINGS_PANELS)) {
				string rawValue = Config._appSettings[Config.APP_SETTINGS_PANELS].Trim();
				Regex r = new Regex(@"[^a-zA-Z0-9_\,\.]");
				rawValue = r.Replace(rawValue, "");
				List<string> rawItems = rawValue.Split(',').ToList<string>();
				Type value;
				foreach (string rawItem in rawItems) {
					try {
						value = Type.GetType(rawItem, true);
						if (value != null && !result.Contains(value)) {
							result.Add(value);
						}
					} catch (Exception e) { }
				}
			}
			return result.ToArray();
		}
		internal static string GetEditor () {
			if (Config._appSettings.ContainsKey(Config.APP_SETTINGS_EDITOR)) {
				return Config._appSettings[Config.APP_SETTINGS_EDITOR].Trim();
			} else {
				return null;
			}
		}
		internal static OutputType? GetOutput () {
			if (Config._appSettings.ContainsKey(Config.APP_SETTINGS_OUTPUT)) {
				string rawValue = Config._appSettings[Config.APP_SETTINGS_OUTPUT].Trim().ToLower();
				if (rawValue == "html") return OutputType.Html;
				if (rawValue == "text") return OutputType.Text;
			}
			return null;
		}
		internal static List<string> GetDebugIps () {
			List<string> result = new List<string>();
			if (Config._appSettings.ContainsKey(Config.APP_SETTINGS_DEBUG_IPS)) {
				string rawValue = Config._appSettings[Config.APP_SETTINGS_DEBUG_IPS].Trim().ToLower();
				Regex r = new Regex(@"[^a-f0-9\.\,:]");
				rawValue = r.Replace(rawValue, "");
				result = rawValue.Split(',').ToList<string>();
			}
			return result;
		}
		internal static Dictionary<string, bool> GetLevels () {
			Dictionary<string, bool> result = new Dictionary<string, bool>();
			if (Config._appSettings.ContainsKey(Config.APP_SETTINGS_LEVELS)) {
				string rawValue = Config._appSettings[Config.APP_SETTINGS_LEVELS].Trim().ToLower();
				Regex r = new Regex(@"[^a-z\,\-\+]");
				rawValue = r.Replace(rawValue, "");
				List<string> rawItems = rawValue.Split(',').ToList<string>();
				string key;
				bool value;
				foreach (string rawItem in rawItems) {
					if (rawItem.Substring(0, 1) == "-") {
						key = rawItem.Substring(1);
						value = false;
					} else if (rawItem.Substring(0, 1) == "+") {
						key = rawItem.Substring(1);
						value = true;
					} else {
						key = rawItem;
						value = true;
					}
					if (result.ContainsKey(key)) {
						result[key] = value;
					} else {
						result.Add(key, value);
					}
				}
			}
			return result;
		}
		internal static string GetDirectory () {
			if (Config._appSettings.ContainsKey(Config.APP_SETTINGS_DIRECTORY)) {
				return Config._appSettings[Config.APP_SETTINGS_DIRECTORY].Trim();
			}
			return "";
        }
        internal static int GetDepth () {
            if (Config._appSettings.ContainsKey(Config.APP_SETTINGS_DEPTH)) {
                string rawValue = Config._appSettings[Config.APP_SETTINGS_DEPTH].Trim();
                rawValue = new Regex("[^0-9]").Replace(rawValue, "");
                if (rawValue.Length > 0) { 
                    return Int32.Parse(rawValue);
                }
            }
            return 0;
		}
		internal static int GetMaxLength () {
			if (Config._appSettings.ContainsKey(Config.APP_SETTINGS_MAX_LENGTH)) {
				string rawValue = Config._appSettings[Config.APP_SETTINGS_MAX_LENGTH].Trim();
				rawValue = new Regex("[^0-9]").Replace(rawValue, "");
				if (rawValue.Length > 0) {
					return Int32.Parse(rawValue);
				}
			}
			return 0;
		}
	}
}
