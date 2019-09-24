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
					string fullUrl = HttpUtility.HtmlEncode(request.Url.AbsoluteUri.ToString());
					headers.Add(new string[] { "URL", fullUrl });
					headers.Add(new string[] { "IP", Tools.GetClientIpAddress() });
					string headerName;
					string headerValue;
					for (int i = 0; i < headersCol.Count; i++) {
						headerName = headersCol.GetKey(i);
						headerValue = HttpUtility.HtmlEncode(headersCol.Get(i));
						if (headerName.Length > Dispatcher.DumpMaxLength)
							headerName = headerName.Substring(0, Dispatcher.DumpMaxLength);
						if (headerValue.Length > Dispatcher.DumpMaxLength)
							headerValue = headerValue.Substring(0, Dispatcher.DumpMaxLength);
						headers.Add(new string[] {
							headerName, headerValue
						 });
					}
				}
			} catch { }
			return headers;
		}
	}
}
