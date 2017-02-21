using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace Desharp.Core {
    public class Tools {
		internal static string Editor = "";
		private const string _EDITOR_DEFAULT = "MSVS2015";
		static Tools () {
			// prefered editor for <a href="editor://open/..."></a> links
			string cfgEditor = Config.GetEditor();
			if (cfgEditor == null || cfgEditor.Length == 0) {
				cfgEditor = Tools._EDITOR_DEFAULT;
			}
			Tools.Editor = cfgEditor;
		}
		internal static string GetClientIpAddress () {
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
        public static long GetRequestId () {
            if (HttpContext.Current == null) return 0; // windows, unit testing
            return HttpContext.Current.Timestamp.Ticks;
        }
		public static long GetThreadId () {
            return Thread.CurrentThread.ManagedThreadId;
		}
		public static string HtmlEntities (string value) {
			value = HttpUtility.JavaScriptStringEncode(value);
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
		internal static string StringToUnicodeIndexes (string s) {
            List<string> r = new List<string>();
            char[] chars = s.ToCharArray();
            for (int i = 0, l = chars.Length; i < l; i += 1) {
                r.Add(
                    Convert.ToUInt16(chars[i]).ToString()
                );
            }
            return String.Join(",", r.ToArray());
        }
        internal static string Md5 (string s) {
            System.Security.Cryptography.MD5 md5Hash = System.Security.Cryptography.MD5.Create();
            byte[] data = md5Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++) {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
		internal static string SpaceIndent (int spaces = 0, bool htmlOut = true) {
			string s = "";
			for (var i = 0; i < spaces; i++) {
				s += htmlOut ? "&nbsp;" : " ";
			}
			return s;
		}
	}
}
