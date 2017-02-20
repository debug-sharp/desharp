using Desharp.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Web;

namespace Desharp.Outputers {
    public class HtmlResponse {
        private static List<string> _htmlXmlMimeTypes = new List<string>() {
            "text/html",
            "application/xhtml+xml",
            "text/xml",
            "application/xml",
            "image/svg+xml",
            "application/rss+xml",
        };
        private static List<string> _jsMimeTypes = new List<string>() {
            "application/x-javascript",
            "application/javascript",
            "application/ecmascript",
            "text/javascript",
            "text/ecmascript",
        };
        private static string _assets;
        private static Dictionary<long, StringBuilder> _outputBuffers = new Dictionary<long, StringBuilder>();
        private static Dictionary<long, bool> _assetsInserted = new Dictionary<long, bool>();
        static HtmlResponse () {
            ResourceManager rm = new ResourceManager("Desharp.Assets", Assembly.GetExecutingAssembly());
            string cssContent = rm.GetString("DumpsCss").Replace("\r", "").Replace("\n", "").Replace("\t", "");
            string jsContent = rm.GetString("DumpsJs");//.Replace("\r", "").Replace("\n", "").Replace("\t", "");
            HtmlResponse._assets = System.Environment.NewLine
                + "<style>" + cssContent + "</style>"
                + System.Environment.NewLine
                + "<script>" + jsContent + "</script>";
        }
        public static void SendRenderedExceptions (string renderedExceptions, string exceptionType) {
            HttpContext.Current.Response.ContentType = "text/html";
            HttpContext.Current.Response.Write(
                "<!DOCTYPE HTML>" + System.Environment.NewLine
                + "<html lang=\"en-US\">" + System.Environment.NewLine 
                    + "<head>"
                        + "<meta charset=\"UTF-8\">" 
                        + "<title>" + exceptionType + "</title>"
                        + "<script>document.title='" + HttpUtility.JavaScriptStringEncode(exceptionType) + "';</script>"
                        + HtmlResponse._assets
                    + "</head>" + System.Environment.NewLine
                    + "<body class=\"debug-exception\">"
                        + renderedExceptions
                    + "</body>"
                + "</html>"
            );
            HtmlResponse._assetsInserted[Tools.GetRequestId()] = true;
            HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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
            HttpResponse response = HttpContext.Current.Response;
            string debugCode = HtmlResponse.GetCurrentOutputBuffer().ToString().Trim();
            string contentType = response.ContentType.ToLower();
            string processingTime = "";
            if (debugCode.Length > 0 || Core.Environment.GetEnabled()) {
                if (HtmlResponse._isMimeTypeResponse(contentType, HtmlResponse._htmlXmlMimeTypes)) {
                    processingTime = "<!-- processing time: " + Debug.GetProcessingTime().ToString() + " s -->";
                } else if (HtmlResponse._isMimeTypeResponse(contentType, HtmlResponse._jsMimeTypes)) {
                    processingTime = "/* processing time: " + Debug.GetProcessingTime().ToString() + " s  */";
                }
            }
            if (debugCode.Length > 0) {
                if (!HtmlResponse._assetsInserted.ContainsKey(crt)) {
                    response.Write(HtmlResponse._assets);
                }
                response.Write(
                    System.Environment.NewLine
                    + "<script>new Desharp([" + Tools.StringToUnicodeIndexes(debugCode) + "]);</script>"
                );
                if (processingTime.Length > 0 ) response.Write(System.Environment.NewLine + processingTime);
            }
            if (HtmlResponse._outputBuffers.ContainsKey(crt)) { 
                HtmlResponse._outputBuffers.Remove(crt);
            }
            if (HtmlResponse._assetsInserted.ContainsKey(crt)) {
                HtmlResponse._assetsInserted.Remove(crt);
            }
        }
        private static bool _isMimeTypeResponse (string responseContentType, List<string> mimeTypes) {
            bool result = false;
            foreach (string mimeType in mimeTypes) {
                if (responseContentType.IndexOf(mimeType) > -1) {
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}
