using Desharp.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;

namespace Desharp.Producers {
    public class HtmlResponse {
        private static string _assets;
        static HtmlResponse () {
            HtmlResponse._assets = System.Environment.NewLine
                + "<style>" + Assets.bar_css + Assets.bar_window_css + Assets.bar_exception_css + Assets.exception_css + Assets.dumps_css + "</style>"
                + System.Environment.NewLine
                + "<script>" + Assets.dumps_js + "</script>";
        }
        public static void SendRenderedExceptions (string renderedExceptions, string exceptionType) {
            HttpContext.Current.Response.ContentType = "text/html";
            HttpContext.Current.Response.Write(
                "<!DOCTYPE HTML>" + System.Environment.NewLine
                + @"<html lang=""en-US"">" + System.Environment.NewLine 
                    + "<head>"
                        + @"<meta charset=""UTF-8"" />"
						+ "<title>" + exceptionType + "</title>"
                        + "<script>document.title='" + HttpUtility.JavaScriptStringEncode(exceptionType) + "';</script>"
                        + HtmlResponse._assets
                    + "</head>" + System.Environment.NewLine
                    + @"<body class=""debug-exception"">"
                        + renderedExceptions
                    + "</body>"
                + "</html>"
            );
			Dispatcher.GetCurrent().WebAssetsInserted = true;
            HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
		}
		internal static void TransmitStaticErrorPage () {
			HttpResponse response = HttpContext.Current.Response;
			response.StatusCode = 500;
			response.Write(Dispatcher.WebStaticErrorPage);
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
							"title:'" + renderedPanel.Title + "'," +
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
        }
		internal static List<RenderedPanel> RenderDebugPanels (Dictionary<string, Panels.Abstract> requestPanels = null) {
			List<RenderedPanel> result = new List<RenderedPanel>();
			result.Add(new RenderedPanel {
				Name = "exec",
				AddIfEmpty = true,
				PanelType = PanelType.BarText,
				PanelIconType = PanelIconType.None,
				Title = Dispatcher.GetCurrent().WebRequestEndTime.ToString().Replace(',', '.') + " s",
				Content = ""
			});
			if (requestPanels == null) return result;
			Panels.Abstract panel;
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
					Title = panel.RenderBarText(),
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
