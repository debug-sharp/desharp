using Desharp.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

namespace Desharp.Completers {
	internal class HttpHeaders {
		internal static Dictionary<string, string> CompletePossibleHttpHeaders () {
			Dictionary<string, string> headers = new Dictionary<string, string>();
			try {
				HttpRequest request = HttpContext.Current.Request;
				NameValueCollection headersCol = request.Headers;
				if (headersCol != null) {
					headers.Add("URL", request.Url.ToString());
					headers.Add("IP", Tools.GetClientIpAddress());
					for (int i = 0; i < headersCol.Count; i++) {
						headers.Add(
							headersCol.GetKey(i), headersCol.Get(i)
						);
					}
				}
			} catch (Exception e) { }
			return headers;
		}
	}
}
