using Desharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace Desharp.Renderers {
    internal class FileLink {
        internal static string Render (StackTraceItem stackTraceItem, bool htmlOut) {
            if (stackTraceItem.File.ToString().Length == 0 && stackTraceItem.Line.ToString().Length == 0)
                return "";
			if (htmlOut) {
				return @"<a class=""desharp-dump desharp-dump-link"" href=""editor://open/?file=" + HttpUtility.UrlEncode(stackTraceItem.File.ToString())
					+ "&line=" + stackTraceItem.Line
					+ "&editor=" + Tools.Editor
					+ @""">" + Tools.RelativeSourceFullPath(stackTraceItem.File.ToString()) + "</a>";
				
			} else {
				return Tools.RelativeSourceFullPath(stackTraceItem.File.ToString()) + ":" + stackTraceItem.Line;
			}
		}
    }
}
