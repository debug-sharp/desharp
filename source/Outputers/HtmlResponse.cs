using Desharp.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;

namespace Desharp.Outputers {
    public class HtmlResponse {

        private const string CSS_FILES_PATH_FROM_DOCUMENT_ROOT_CONTEXT = "/static/css/components";
        private const string JS_FILES_PATH_FROM_DOCUMENT_ROOT_CONTEXT = "/static/js/libs";

        private const string EXCEPTION_HTML_CODE = "<!DOCTYPE HTML>\n<html lang=\"en-US\">\n<head><meta charset=\"UTF-8\"><title>%title</title><link rel=\"stylesheet\" href=\"%cssFilesPathFromDocumentRootContext/system-logs.css\" /><script src=\"%jsFilesPathFromDocumentRootContext/system-logs.js\" type=\"text/javascript\"></script><script src=\"%jsFilesPathFromDocumentRootContext/dot-net-debug.js\" type=\"text/javascript\"></script></head>\n<body onload=\"init();\" class=\"debug-exception\">%body</body></html>";
        private const string PROCESSING_TIME_HTML_CODE = "<!-- processing time: %processingTime -->";
        private const string PROCESSING_TIME_JS_CODE = "/* processing time: %processingTime */";
        private const string DEBUG_WINDOW_HTML_CODE = "<script src=\"%jsFilesPathFromDocumentRootContext/dot-net-debug.js\" type=\"text/javascript\"></script><script type=\"text/javascript\">new Debug([%encodedDebugData]);</script>";

        private static List<string> HTML_XML_MIMETYPES = new List<string>() {
            "text/html",
            "application/xhtml+xml",
            "text/xml",
            "application/xml",
            "image/svg+xml",
            "application/rss+xml",
        };

        private static Dictionary<long, StringBuilder> _outputBuffers;
        
        public static string InsertCurrentRequestOutput (string output) {
            StringBuilder currentOutputBuffer = HtmlResponse.GetCurrentOutputBuffer();
            string debugCode = currentOutputBuffer.ToString().Trim();
            
            string responseContentType = HttpContext.Current.Response.ContentType;
            string processingTime = "";
            if (HtmlResponse.HTML_XML_MIMETYPES.Contains(responseContentType)) {
                processingTime = HtmlResponse.PROCESSING_TIME_HTML_CODE.Replace("%processingTime", Debug.GetProcessingTime().ToString() + " s");
            }
            Debug.RequestEnd();
            if (!Tools.IsHtmlResponse() || !Debug.Enabled() || debugCode.Length == 0) {
                return HtmlResponse._injectDebugCodeAfterHtmlEndTagIfAny(output, processingTime);
            } else {
                StringBuilder o = new StringBuilder();
                o.Append(processingTime);
                o.Append(
                    HtmlResponse.DEBUG_WINDOW_HTML_CODE
                        .Replace("%jsFilesPathFromDocumentRootContext", HtmlResponse.JS_FILES_PATH_FROM_DOCUMENT_ROOT_CONTEXT.TrimEnd('/'))
                        .Replace("%encodedDebugData", String.Join(",", Tools.StringToUnicodeIndexes(debugCode)))
                );
                return HtmlResponse._injectDebugCodeAfterHtmlEndTagIfAny(output, o.ToString());
            }
        }

        private static string _injectDebugCodeAfterHtmlEndTagIfAny (string output = "", string debugInjectCode = "") {
            // add debug output right after </body> element closing tag - before any other debuging mechanisms
            int htmlEndElmPos = output.IndexOf("</html>");
            if (htmlEndElmPos > -1) {
                output = output.Substring(0, htmlEndElmPos) + debugInjectCode + output.Substring(htmlEndElmPos);
            } else {
                output += debugInjectCode;
            }
            return output;
        }
        public static void SendRenderedExceptions (string renderedExceptions, string exceptionType) {
            HttpContext.Current.Response.ContentType = "text/html";
            HttpContext.Current.Response.Write(
                HtmlResponse.EXCEPTION_HTML_CODE
                    .Replace("%cssFilesPathFromDocumentRootContext", HtmlResponse.CSS_FILES_PATH_FROM_DOCUMENT_ROOT_CONTEXT.TrimEnd('/'))
                    .Replace("%jsFilesPathFromDocumentRootContext", HtmlResponse.JS_FILES_PATH_FROM_DOCUMENT_ROOT_CONTEXT.TrimEnd('/'))
                    .Replace("%title", exceptionType)
                    .Replace("%body", renderedExceptions)
            );
            HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            //HttpContext.Current.Response.End();
            //HttpContext.Current.Response.Close(); // do not close connection - if close, there are allways incompleted html results in browser
        }
        public static StringBuilder GetCurrentOutputBuffer () {
            if (!(HtmlResponse._outputBuffers is Dictionary<long, StringBuilder>)) {
                HtmlResponse._outputBuffers = new Dictionary<long, StringBuilder>();
            }
            long currentRequestTick = Tools.GetRequestId();
            if (!HtmlResponse._outputBuffers.ContainsKey(currentRequestTick)) {
                StringBuilder outputBuffer = new StringBuilder("");
                HtmlResponse._outputBuffers[currentRequestTick] = outputBuffer;
            }
            return HtmlResponse._outputBuffers[currentRequestTick];
        }
        public static void RequestEnd (long crt) {
            if (HtmlResponse._outputBuffers.ContainsKey(crt)) HtmlResponse._outputBuffers.Remove(crt);
        }
    }
}
