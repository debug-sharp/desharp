using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Routing;

namespace Desharp.Panels {
	public class Routing: Abstract {
		public static string PanelName = "routing";
		private bool _barTextAndPanelContentReady = false;
		private string _matchedRouteUrl;
		private string _generatedUrlInfo;
		private string _routeDataTable;
		private string _dataTokensTable;
		private string _allRouteTable;
		private string _currentRelExecPath;
		private string _panelContent = String.Empty;
		private string _barText = String.Empty;
		public new int[] DefaultWindowSizes {
			get { return new int[] { 350, 250 }; }
		}
		public override string IconValue {
			get { return Routing.PanelName; }
		}
		public override string Name {
			get { return Routing.PanelName; }
		}
		public override PanelIconType PanelIconType {
			get { return PanelIconType.Class; }
		}
		public override string RenderBarText () {
			this._completeBarTextAndPanelContent();
			return this._barText;
		}
		public override string RenderWindowContent () {
			this._completeBarTextAndPanelContent();
			return this._panelContent;
		}

		private void _completeBarTextAndPanelContent () {
			if (this._barTextAndPanelContentReady) return;
			try {
			this._completeMatchedRouteUrl();
			this._completeGeneratedUrlInfo();
			this._completeRouteDataTable();
			this._completeDataTokensTable();
			this._completeBarTextAndRoutesTable();
			this._completeCurrentRelExecPath();

			this._panelContent = this._matchedRouteUrl
				+ this._generatedUrlInfo
				+ this._routeDataTable
				+ this._dataTokensTable
				+ this._allRouteTable
				+ this._currentRelExecPath;
			} catch (Exception e) {
				Debug.Log(e);
			}
			this._barTextAndPanelContentReady = true;
		}
		private void _completeMatchedRouteUrl () {
			this._matchedRouteUrl = "n/a";
			RouteBase matchedRouteBase = HttpContext.Current.Request.RequestContext.RouteData.Route;
			if (matchedRouteBase is Route) {
				Route matchedRoute = matchedRouteBase as Route;
				if (matchedRoute != null) this._matchedRouteUrl = matchedRoute.Url;
			} else {
				this._matchedRouteUrl = @"<b class=""heading"">No matched route.</b>";
			}
			this._matchedRouteUrl = @"<b class=""heading"">Matched route:</b><pre><code>""" + this._matchedRouteUrl + @"""</code></pre>";
		}
		private void _completeGeneratedUrlInfo () {
			this._generatedUrlInfo = "";
			HttpRequest request = HttpContext.Current.Request;
			if (request.QueryString.Count > 0) {
				RequestContext requestContext = request.RequestContext;
				RouteValueDictionary rvalues = new RouteValueDictionary();
				foreach (string key in request.QueryString.Keys) {
					if (key != null) {
						rvalues.Add(key, request.QueryString[key]);
					}
				}
				VirtualPathData vpd = RouteTable.Routes.GetVirtualPath(requestContext, rvalues);
				if (vpd != null) {
					this._generatedUrlInfo = @"<p><label>Generated URL:</label>: ";
					this._generatedUrlInfo += "<strong style=\"color:red;\">" + vpd.VirtualPath + "</strong>";
					Route vpdRoute = vpd.Route as Route;
					if (vpdRoute != null) {
						this._generatedUrlInfo += " using the route \"" + vpdRoute.Url + "\"</p>";
					}
				}
			}
		}
		private void _completeRouteDataTable () {
			string routeDataTableRows = "";
			RequestContext requestContext = HttpContext.Current.Request.RequestContext;
			RouteData routeData = requestContext.RouteData;
			RouteValueDictionary routeValues = routeData.Values;
			RouteBase matchedRouteBase = routeData.Route;
			if (matchedRouteBase is Route) {
				foreach (string key in routeValues.Keys) {
					routeDataTableRows += String.Format(
						"<tr><td>{0}</td><td>{1}&nbsp;</td></tr>",
						key, routeValues[key]
					);
				}
			}
			this._routeDataTable = @"<b class=""heading"">Route Data:</b>
				<table>
					<thead><tr><th>Key</th><th>Value</th></tr></thead>
					<tbody>" + routeDataTableRows + @"</tbody>
				</table>";
		}
		private void _completeDataTokensTable () {
			string dataTokensTableRows = "";
			RequestContext requestContext = HttpContext.Current.Request.RequestContext;
			RouteData routeData = requestContext.RouteData;
			RouteBase matchedRouteBase = routeData.Route;
			if (matchedRouteBase is Route && routeData.DataTokens.Keys.Count > 0) {
				foreach (string key in routeData.DataTokens.Keys) {
					dataTokensTableRows += String.Format(
						"<tr><td>{0}</td><td>{1}&nbsp;</td></tr>", 
						key, routeData.DataTokens[key]
					);
				}
				this._dataTokensTable = @"<b class=""heading"">Data Tokens:</b>
					<table>
						<thead><tr><th>Key</th><th>Value</th></tr></thead>
						<tbody>" + dataTokensTableRows + @"</tbody>
					</table>";
			} else { 
				this._dataTokensTable = "<p>Route has no data tokens.</p>";
			}
		}
		private void _completeAllRoutesTableOld () {
			RequestContext requestContext = HttpContext.Current.Request.RequestContext;
			RouteData routeData = requestContext.RouteData;
			RouteValueDictionary routeValues = routeData.Values;
			RouteBase matchedRouteBase = routeData.Route;
			bool matchesCurrentRequest;
			int routesCount = 0;
			string allRouteTableRows = "";
			string checkedStr;
			string rowClass;
			string url;
			string defaults;
			string constraints;
			string dataTokens;
			bool routeMatched = false;
			//using (RouteTable.Routes.GetReadLock()) {
				routesCount = RouteTable.Routes.Count;
				foreach (RouteBase routeBase in RouteTable.Routes) {
					matchesCurrentRequest = routeBase.GetRouteData(requestContext.HttpContext) != null;
					rowClass = "";
					checkedStr = "";
					if (!routeMatched && matchesCurrentRequest) {
						checkedStr = "&#10003;";
						rowClass = @" class=""matched""";
						routeMatched = true;
					}
					url = "n/a";
					defaults = "n/a";
					constraints = "n/a";
					dataTokens = "n/a";
					Route route = this._safeCastRouteBaseToRoute(routeBase);
					if (route is Route) {
						url = route.Url;
						defaults = Routing._formatDictionary(route.Defaults);
						constraints = Routing._formatDictionary(route.Constraints);
						dataTokens = Routing._formatDictionary(route.DataTokens);
					}
					allRouteTableRows += String.Format(
						@"<tr{0}><td>{1}</td><td>/{2}</td><td>{3}</td><td>{4}</td><td>{5}</td></tr>",
						rowClass, checkedStr, url, defaults, constraints, dataTokens
					);
				}
			//}
			this._barText = "Routing (" + routesCount + ")";
			this._allRouteTable = @"<b class=""heading"">All Routes:</b>
				<table><thead><tr class=""header"">
					<th></th>
					<th>Url</th>
					<th>Defaults</th>
					<th>Constraints</th>
					<th>Data Tokens</th>
				</tr></thead><tbody>" + allRouteTableRows + "</tbody></table>";
		}
		private void _completeBarTextAndRoutesTable () {
			HttpContextBase httpContext = HttpContext.Current.Request.RequestContext.HttpContext;
			bool matched;
			bool matchedRouteFound = false;
			StringBuilder allRouteTableRows = new StringBuilder();
			foreach (RouteBase routeBase in RouteTable.Routes) {
				matched = routeBase.GetRouteData(httpContext) != null;
				Route route = this._safeCastRouteBaseToRoute(routeBase);
				RouteTarget routeTarget = this._completeRouteTarget(route);
				if (!matchedRouteFound && matched && route is Route) {
					this._completeBarTextByMatchedRoute(routeTarget);
				}
				allRouteTableRows.Append(
					this._completeRoutesTableRow(
						route, routeTarget,
						matched ? (!matchedRouteFound ? 2 : 1) : 0
					)
				);
				if (!matchedRouteFound && matched) matchedRouteFound = true;
			}
			if (!matchedRouteFound) this._completeBarTextByMatchedRoute(new RouteTarget { FullName = "No matched route" });
			this._allRouteTable = @"<b class=""heading"">All Routes:</b>
				<table><thead><tr class=""header"">
					<th></th>		
					<th>Pattern</th>
					<th>Defaults</th>
					<th>Matched as</th>
				</tr></thead><tbody>" + allRouteTableRows.ToString() + "</tbody></table>";
		}

		private RouteTarget _completeRouteTarget (Route route) {
			RouteTarget result = new RouteTarget {
				Controller = "*",
				Action = "*",
				FullName = "",
				Namespaces = new string[] {}
			};
			if (route.RouteHandler is StopRoutingHandler) {
				result.Controller = "";
				result.Action = "";
			} else {
				IDictionary<string, object> routeDefaults = route.Defaults as IDictionary<string, object>;
				if (routeDefaults.ContainsKey("controller")) {
					result.Controller = routeDefaults["controller"].ToString();
					if (result.Controller.Length == 0) result.Controller = @"<span class=""type"">[Optional]</span>";
				}
				if (routeDefaults.ContainsKey("action")) {
					result.Action = routeDefaults["action"].ToString();
					if (result.Action.Length == 0) result.Action = @"<span class=""type"">[Optional]</span>";
				}
				result.FullName = result.Controller + ":" + result.Action;
			}
			return result;
		}

		private void _completeBarTextByMatchedRoute (RouteTarget routeTarget) {
			this._barText = routeTarget.FullName + " (" + RouteTable.Routes.Count + ")";
		}
		private string _completeRoutesTableRow (Route route, RouteTarget routeTarget, int matched) {
			string rowCssClass = String.Empty;
			string firstColumnContent = String.Empty;
			string routePattern = "/" + route.Url;
			if (matched == 2) {
				rowCssClass = @" class=""matched""";
				firstColumnContent = "&#10003;";
			} else if (matched == 1) {
				firstColumnContent = "&#8776;";
			} else if (route.RouteHandler is StopRoutingHandler) {
				rowCssClass = @" class=""ignore""";
				firstColumnContent = "&#215;";
			}
			Regex r = new Regex(@"\{([a-zA-Z0-9_\*]*)\}");
			routePattern = @"<div class=""pattern"">""" + r.Replace(routePattern, @"<b>{$1}</b>") + @"""</div>";
			string routeName = String.Empty;
			if (routeTarget.FullName.IndexOf(":") > -1) {
				routeName = @"<div class=""target"">" + routeTarget.FullName + "</div>";
			} else {
				routeName = routeTarget.FullName;
			}
			string routeParams = "";
			if (!(route.RouteHandler is StopRoutingHandler)) {
				routeParams = @"<div class=""params"">";
				RouteValueDictionary routeDefaults = route.Defaults;
				RouteValueDictionary routeConstraints = route.Constraints;
				if (routeConstraints == null) routeConstraints = new RouteValueDictionary();
				foreach (var item in routeDefaults) {
					if (item.Key == "controller" || item.Key == "action") continue;
					routeParams += @"<div class=""desharp-dump""><span class=""routekey"">" + item.Key + "</span>";
					if (item.Value.GetType().Name == "UrlParameter") {
						routeParams += @" <span class=""type"">[Optional]</span>";
					} else {
						routeParams += "<s>:&nbsp;</s>" + Renderers.Dumper.DumpPrimitiveType(item.Value, 0, true, 512, "");
					}
					if (routeConstraints.ContainsKey(item.Key)) {
						if (routeConstraints[item.Key] != null) {
							if (routeConstraints[item.Key] is string) {
								routeParams += @"&nbsp;<span class=""string"">""" + routeConstraints[item.Key].ToString() + @"""</span>";
							} else {
								routeParams += @"&nbsp;<span class=""type"">" + routeConstraints[item.Key].ToString() + @"</span>";
							}
						}
					}
					routeParams += "</div>";
				}
				routeParams += "</div>";
			}
			HttpContextBase httpContext = HttpContext.Current.Request.RequestContext.HttpContext;
			Debug.Log(route.GetRouteData(httpContext), Desharp.Level.DEBUG);
			return String.Format(
				@"<tr{0}><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
				rowCssClass,
				firstColumnContent,
				routePattern,
				routeName + routeParams,
				"matched as content"
			);
		}
		private Route _safeCastRouteBaseToRoute (RouteBase routeBase) {
			Route route = routeBase as Route;
			if (route == null) {
				PropertyInfo property = routeBase.GetType().GetProperty("__DebugRoute", BindingFlags.NonPublic | BindingFlags.Instance);
				if (property != null) {
					route = property.GetValue(routeBase, null) as Route;
				}
			}
			return route;
		}


		private void _completeCurrentRelExecPath () {
			string path = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath;
			this._currentRelExecPath = @"<b class=""heading"">Current relative execution file path:</b><pre><code>""" + path + @"""</code></pre>";
		}

		
		private static string _formatDictionary (IDictionary<string, object> values) {
			if (values == null) return "(null)";
			if (values.Count == 0) return "(empty)";
			string display = String.Empty;
			foreach (string key in values.Keys) {
				display += String.Format("{0} = {1}, ", key, Routing._formatObject(values[key]));
			}
			if (display.EndsWith(", ")) display = display.Substring(0, display.Length - 2);
			return display;
		}
		private static string _formatObject (object value) {
			if (value == null) {
				return "(null)";
			}
			var values = value as object[];
			if (values != null) {
				return string.Join(", ", values);
			}
			var dictionaryValues = value as IDictionary<string, object>;
			if (dictionaryValues != null) {
				return Routing._formatDictionary(dictionaryValues);
			}
			if (value.GetType().Name == "UrlParameter") {
				return "UrlParameter.Optional";
			}
			return value.ToString();
		}
	}
}