using Desharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace Desharp.Producers {
    internal class HtmlResponse {
        private static StringBuilder _assets;
        static HtmlResponse () {
            HtmlResponse._assets = new StringBuilder();
            HtmlResponse._assets
                .Append(System.Environment.NewLine)
                .Append("<style>")
                .Append(Assets.bar_css)
                .Append(Assets.bar_window_css)
                .Append(Assets.bar_panels_css)
                .Append(Assets.bar_exception_css)
                .Append(Assets.exception_css)
                .Append(Assets.dumps_css)
                .Append("</style>")
                .Append(System.Environment.NewLine)
                .Append("<script>")
                .Append(Assets.dumps_js)
                .Append("</script>");
        }
        public static void SendRenderedExceptions (string renderedExceptions, string exceptionType) {
            HttpResponse res = HttpContext.Current.Response;
			try {
				res.ContentType = "text/html";
				res.StatusCode = (int)HttpStatusCode.InternalServerError;
			} catch { }
            StringBuilder str = new StringBuilder();
            str
                .Append(@"<!DOCTYPE HTML>" + System.Environment.NewLine)
                .Append(@"<html lang=""en-US"">" + System.Environment.NewLine)
                .Append(@"<head>")
                .Append(@"<meta charset=""UTF-8"" />")
                .Append(@"<title>" + exceptionType + @"</title>")
                .Append(@"<script>document.title='")
                .Append(HttpUtility.JavaScriptStringEncode(exceptionType))
                .Append(@"';</script>")
                .Append(HtmlResponse._assets.ToString())
                .Append(@"</head>" + System.Environment.NewLine)
                .Append(@"<body class=""desharp-exception""><div class=""desharp-screen"">")
                .Append(renderedExceptions)
                .Append(@"</div></body>")
                .Append(@"</html>");
            res.Write(str.ToString());
			Dispatcher.GetCurrent().WebAssetsInserted = true;
		}
		internal static void TransmitStaticErrorPagePrepareHeaders () {
			HttpContext.Current.Response.StatusCode = 500;
		}
		internal static void TransmitStaticErrorPageSendContent() {
			HttpContext.Current.Response.Write(Dispatcher.WebStaticErrorPage);
		}
		public static void WriteDebugBarToResponse (List<List<RenderedPanel>> allRequestRenderedPanels = null) {
			HttpResponse response = HttpContext.Current.Response;
			string jsCode = "Desharp.GetInstance()";
			List<string> jsCodeBarIcons = new List<string>();
			List<string> jsCodeWindowSizes = new List<string>();
			List<string> jsCodeAllBars = new List<string>();
			List<string> configuredPanelNames = new List<string>();
			List<string> jsCodeRequestBars;
			string jsCodeRequestBarContent;
			foreach (List<RenderedPanel> requestRenderedPanels in allRequestRenderedPanels) {
				jsCodeRequestBars = new List<string>();
				foreach (RenderedPanel renderedPanel in requestRenderedPanels) {
					if (!configuredPanelNames.Contains(renderedPanel.Name)) {
						if (renderedPanel.PanelIconType != PanelIconType.None) {
							jsCodeBarIcons.Add(
								renderedPanel.Name + ":["
									+ (renderedPanel.PanelIconType == PanelIconType.Class ? "'class'" : "'code'")
									+ ",'" + HttpUtility.JavaScriptStringEncode(renderedPanel.IconValue) + "'"
								+ "]"
							);
						}
						if (renderedPanel.DefaultWindowSizes != null) {
							jsCodeWindowSizes.Add(
								renderedPanel.Name + ":[" + String.Join(",", renderedPanel.DefaultWindowSizes) + "]"
							);
						}
					}
					jsCodeRequestBarContent = "";
					if (renderedPanel.Content != null && renderedPanel.Content.Length > 0) {
						jsCodeRequestBarContent = ",content:'" + Tools.JavascriptString(renderedPanel.Content) + "'";
					}
					if (jsCodeRequestBarContent.Length > 0 || (jsCodeRequestBarContent.Length == 0 && renderedPanel.AddIfEmpty)) {
						jsCodeRequestBars.Add("{" +
							"name:'" + renderedPanel.Name + "'," +
							"title:['" + String.Join("','", renderedPanel.Title) + "']," +
							"mode:" + HtmlResponse._getBarModeByPanelType(renderedPanel.PanelType).ToString() + 
							jsCodeRequestBarContent +
						"}");
					}
				}
				jsCodeAllBars.Add(".AddBar([" + String.Join(",", jsCodeRequestBars.ToArray()) + "])");
			}
			jsCode += ".BarIcons({" + String.Join(",", jsCodeBarIcons.ToArray()) + "})";
			jsCode += ".DefaultWindowsSizes({" + String.Join(",", jsCodeWindowSizes.ToArray()) + "})";
			jsCode += String.Join("", jsCodeAllBars.ToArray());
			jsCode += ".RenderBars();";
			StringBuilder responseCode = new StringBuilder();
			if (!Dispatcher.GetCurrent().WebAssetsInserted) responseCode.Append(HtmlResponse._assets);
			bool responseIsXml = Dispatcher.WebCheckIfResponseIsHtmlOrXml(true);
			responseCode
				.Append(System.Environment.NewLine + "<script>" + (responseIsXml ? "/* <![CDATA[ */" : ""))
				.Append(jsCode)
				.Append((responseIsXml ? "/* ]]> */" : "") + "</script>");
			response.Write(responseCode.ToString());
			//response.Flush();
		}
		internal static List<RenderedPanel> RenderDebugPanels (Dictionary<string, Panels.IPanel> requestPanels = null) {
			List<RenderedPanel> result = new List<RenderedPanel>();
			if (requestPanels == null) return result;
			Panels.IPanel panel;
			Type panelType;
			foreach (var item in requestPanels) {
				panel = item.Value;
				panelType = panel.GetType();
				result.Add(new RenderedPanel {
					Name = panel.Name,
					AddIfEmpty = (bool)panelType.GetProperty("AddIfEmpty").GetValue(panel, null),
					DefaultWindowSizes = (int[])panelType.GetProperty("DefaultWindowSizes").GetValue(panel, null),
					PanelType = (PanelType)panelType.GetProperty("PanelType").GetValue(panel, null),
					PanelIconType = panel.PanelIconType,
					IconValue = panel.IconValue,
					Title = panel.RenderBarTitle(),
					Content = panel.RenderWindowContent().Trim()
				});
			}
			return result;
		}
		private static int _getBarModeByPanelType (PanelType panelType) {
			if (panelType == PanelType.BarBtnAndScreen) return 3;
			if (panelType == PanelType.BarBtnAndWindow) return 2;
			if (panelType == PanelType.BarBtnWithJsHandler) return 1;
			return 0;
		}
	}
}
