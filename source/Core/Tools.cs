using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
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
			return Thread.CurrentThread.ManagedThreadId;
		}
		public static long GetThreadId () {
			if (HttpContext.Current == null) return 0; // windows, unit testing
			return HttpContext.Current.Timestamp.Ticks;
		}
		internal static string[] StringToUnicodeIndexes (string s) {
            List<string> r = new List<string>();
            char[] chars = s.ToCharArray();
            for (int i = 0, l = chars.Length; i < l; i += 1) {
                r.Add(
                    Convert.ToUInt16(chars[i]).ToString()
                );
            }
            return r.ToArray();
        }
        // TODO
        internal static bool IsHtmlResponse () {
            bool r = true;
            string requestPath = HttpContext.Current.Request.Path;
            int questionMarkPos = requestPath.IndexOf("?");
            if (questionMarkPos > -1) {
                requestPath = requestPath.Substring(0, questionMarkPos);
            }
            if (
                HttpContext.Current.Response.ContentType != "text/html"/* ||
                requestPath.LastIndexOf("/Scripts/") == 0 ||
                requestPath.LastIndexOf(".js") == 0 ||
                requestPath.LastIndexOf(".css") == 0 ||
                requestPath.LastIndexOf(".ico") == 0*/
            ) {
                r = false;
            }
            return r;
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
