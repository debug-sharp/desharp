using Desharp.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

namespace Desharp.Completers {
	internal class HttpHeaders {
		internal static List<string[]> CompletePossibleHttpHeaders () {
			List<string[]> headers = new List<string[]>();
			try {
				HttpRequest request = HttpContext.Current.Request;
				NameValueCollection headersCol = request.Headers;
				if (headersCol != null) {
					headers.Add(new string[] { "URL", request.Url.ToString() });
					headers.Add(new string[] { "IP", Tools.GetClientIpAddress() });
					for (int i = 0; i < headersCol.Count; i++) {
						headers.Add(new string[] {
							headersCol.GetKey(i), headersCol.Get(i)
						 });
					}
				}
			} catch (Exception e) { }
			return headers;
		}
	}
}
