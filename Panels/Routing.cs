using Desharp.Core;
using Desharp.Renderers;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Routing;
using System.Collections;
using Desharp.Panels.Routings;
using System.Threading;
using System.Runtime.InteropServices;

namespace Desharp.Panels {
	[ComVisible(true)]
	public class Routing: IPanel {
		public static string PanelName = "routing";
		public new int[] DefaultWindowSizes => new int[] { 350, 250 };
		public string IconValue => Routing.PanelName;
		public string Name => Routing.PanelName;
		public PanelIconType PanelIconType => PanelIconType.Class;
		public bool AddIfEmpty => false;
		public PanelType PanelType => PanelType.BarBtnAndWindow;

		public void SessionBegin () {
			this._completeData();
		}
		public void SessionEnd () {}
		public string[] RenderBarTitle() {
			this._completeData();
			return new string[] { this._barText };
		}
		public string RenderWindowContent() {
			this._completeData();
			return this._panelContent;
		}

		private static string _defaultControllersNamespace;
        private static ReaderWriterLockSlim _routeTargetsLock = new ReaderWriterLockSlim();
        private static volatile List<RouteTarget> _routeTargets = null;
		private bool _dataCompleted = false;
		private string _barText = String.Empty;
		private string _panelContent = String.Empty;
		private string _allRoutesTable;
		private string _dataTokensTable;
		private MatchedCompleter _matchedCompleter = new MatchedCompleter();

		private static void _completeRouteTargetsAndDefaultNamespace () {
			Routing._defaultControllersNamespace = String.Format("{0}.Controllers", Tools.GetWebEntryAssembly().GetName().Name);
			Routing._routeTargets = new List<RouteTarget>();
			Assembly entryAssembly = Tools.GetWebEntryAssembly();
			Type systemWebMvcCtrlType = Tools.GetTypeGlobaly("System.Web.Mvc.Controller");
			Type systemWebMvcNonActionAttrType = Tools.GetTypeGlobaly("System.Web.Mvc.NonActionAttribute");
			if (systemWebMvcCtrlType is Type && systemWebMvcNonActionAttrType is Type) {
				try {
					IEnumerable<MethodInfo> controllersActions = entryAssembly.GetTypes()
						.Where(type => systemWebMvcCtrlType.IsAssignableFrom(type)) //filter controllers
						.SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
						.Where(method => method.IsPublic && !method.IsDefined(systemWebMvcNonActionAttrType, false));
					foreach (MethodBase controllerAction in controllersActions) {
						ParameterInfo[] args = controllerAction.GetParameters();
						Dictionary<string, RouteTargetArg> targetArgs = new Dictionary<string, RouteTargetArg>();
						Type targetArgType;
						Type[] genericTypes;
						string targetArgTypeName;
						bool nullable;
						for (int i = 0, l = args.Length; i < l; i += 1) {
							targetArgType = args[i].ParameterType;
							if (targetArgType.Name.IndexOf("Nullable`") == 0) {
								nullable = true;
								targetArgTypeName = "Nullable&lt;";
								genericTypes = targetArgType.GetGenericArguments();
								for (int j = 0, k = genericTypes.Length; j < k; j += 1) {
									targetArgTypeName += (j > 0 ? ",&nbsp;" : "") + genericTypes[i].Name;
								}
								targetArgTypeName += "&gt;";
							} else {
								nullable = false;
								targetArgTypeName = targetArgType.Name;
							}
							targetArgs.Add(
								args[i].Name,
								new RouteTargetArg {
									Type = targetArgType,
									DefaultValue = args[i].DefaultValue,
									IsNullable = nullable,
									HtmlName = targetArgTypeName
								}
							);
						}
						Routing._routeTargets.Add(new RouteTarget {
							Namespace = controllerAction.DeclaringType.Namespace,
							NamespaceLower = controllerAction.DeclaringType.Namespace.ToLower(),
							Controller = controllerAction.DeclaringType.Name,
							Action = controllerAction.Name,
							FullName = controllerAction.DeclaringType.Namespace
								+ "." + controllerAction.DeclaringType.Name
								+ ":" + controllerAction.Name,
							Params = targetArgs
						});
					}
				} catch {}
			}
		}
		private void _completeData () {
			if (this._dataCompleted) return;
            Routing._routeTargetsLock.EnterUpgradeableReadLock();
            if (Routing._routeTargets == null) {
                Routing._routeTargetsLock.EnterWriteLock();
                Routing._routeTargetsLock.ExitUpgradeableReadLock();
                Routing._completeRouteTargetsAndDefaultNamespace();
                Routing._routeTargetsLock.ExitWriteLock();
            } else {
                Routing._routeTargetsLock.ExitUpgradeableReadLock();
            }
			try {
				this._completeRoutes();
				this._completeBarText();
				this._completeMatchedDataTokens();
				this._panelContent = @"<div class=""content" 
					+ (this._matchedCompleter.Target is RouteTarget ? "" : " nomatch") + @""">"
					+ this._allRoutesTable
					+ Routing._getCurrentRelExecPath()
					+ this._dataTokensTable
				+ "</div>";
			} catch (Exception e) {
				Debug.Log(e);
			}
			this._dataCompleted = true;
		}
		private void _completeRoutes () {
			HttpContextBase httpContext = HttpContext.Current.Request.RequestContext.HttpContext;
			bool routeMatched;
			int routeMatchLevel;
			bool matchedRouteFound = false;
			StringBuilder allRouteTableRows = new StringBuilder();
			Route route;
			RouteTarget routeTarget;
			foreach (RouteBase routeBase in RouteTable.Routes) {
				routeMatched = routeBase.GetRouteData(httpContext) != null;
				route = Routing._safeCastRouteBaseToRoute(routeBase);
				routeTarget = this._completeRouteTarget(ref route, route.Defaults);
				routeMatchLevel = routeMatched ? (!matchedRouteFound ? 2 : 1) : 0;
				if (routeMatchLevel == 2) {
					this._matchedCompleter.Route = route;
					this._matchedCompleter.Target = routeTarget;
					matchedRouteFound = true;
				}
				allRouteTableRows.Append(this._completeRoute(
					ref route, ref routeTarget, routeMatchLevel
				));
			}
			this._allRoutesTable = @"<b class=""heading"">All Routes:</b><table class=""routes"">"
				+ @"<thead><tr class=""header""><th></th><th>Pattern</th><th>Defaults</th><th>Matched as</th></tr></thead>"
				+ @"<tbody>" + allRouteTableRows.ToString() + "</tbody></table>";
		}
		private RouteTarget _completeRouteTarget (ref Route route, RouteValueDictionary dictionaryContainingCtrlAndAction) {
			RouteTarget result = new RouteTarget {
				Controller = "",
				Action = "",
				FullName = "",
				Namespaces = new string[] {},
				NamespacesLower = new string[] {},
				Namespace = Routing._defaultControllersNamespace,
				Params = new Dictionary<string, RouteTargetArg>()
			};
			bool ctrlDefined = false;
			bool actionDefined = false;
			int ctrlNameDocPos;
			string namespacePartInsideControllerName;
			if (!(route.RouteHandler is StopRoutingHandler)) {
				if (route.DataTokens.ContainsKey("Namespaces")) {
					try {
						result.Namespaces = route.DataTokens["Namespaces"] as string[];
					} catch { }
					if (result.Namespaces == null) result.Namespaces = new string[] { };
				}
				if (dictionaryContainingCtrlAndAction.ContainsKey("controller")) {
					if (dictionaryContainingCtrlAndAction["controller"].GetType().Name == "UrlParameter") {
						result.Controller = "*";
					} else {
						result.Controller = dictionaryContainingCtrlAndAction["controller"].ToString();
						ctrlNameDocPos = result.Controller.LastIndexOf(".");
						if (ctrlNameDocPos > -1) {
							namespacePartInsideControllerName = result.Controller.Substring(0, ctrlNameDocPos);
							result.Controller = result.Controller.Substring(ctrlNameDocPos + 1);
							if (result.Namespaces.Length > 0) {
								for (int i = 0, l = result.Namespaces.Length; i < l; i += 1) {
									result.Namespaces[i] += "." + result.Controller.Substring(0, ctrlNameDocPos);
								}
							} else {
								result.Namespaces = new[] {
									Routing._defaultControllersNamespace + "." + namespacePartInsideControllerName
								};
							}
						}
						ctrlDefined = true;
					}
				}
				List<string> lowerNamespaces = new List<string>();
				for (int i = 0, l = result.Namespaces.Length; i < l; i += 1) {
					lowerNamespaces.Add(result.Namespaces[i].ToLower());
				}
				result.NamespacesLower = lowerNamespaces.ToArray();
				if (dictionaryContainingCtrlAndAction.ContainsKey("action")) {
					if (dictionaryContainingCtrlAndAction["action"].GetType().Name == "UrlParameter") {
						result.Action = "*";
					} else {
						result.Action = dictionaryContainingCtrlAndAction["action"].ToString();
						actionDefined = true;
					}
				}
				result.FullName = result.Controller + ":" + result.Action;
				if (ctrlDefined && actionDefined) {
					this._completeRouteTargetCompleteActionParams(ref result);
				} else {
					result.FullName += "()";
				}
				if (result.FullName.IndexOf(Routing._defaultControllersNamespace) == 0) {
					result.FullName = result.FullName.Substring(Routing._defaultControllersNamespace.Length + 1);
				}
				result.FullName = result.FullName.Replace("*", @"<span class=""type"">[Optional]</span>");
			}
			return result;
		}
		private void _completeRouteTargetCompleteActionParams (ref RouteTarget routeTarget) {
			RouteTarget routeTargetLocal = null;
			bool searchForCtrl = routeTarget.Controller != "*";
			bool searchForAction = routeTarget.Action != "*";
			bool searchForNamespace = routeTarget.Namespaces.Length > 0;
			string searchedCtrl = routeTarget.Controller + "Controller";
            Routing._routeTargetsLock.EnterReadLock();
            foreach (RouteTarget item in Routing._routeTargets) {
                if (searchForCtrl && item.Controller != searchedCtrl) continue;
                if (searchForAction && item.Action != routeTarget.Action) continue;
                if (searchForNamespace) {
                    if (routeTarget.NamespacesLower.Contains(item.NamespaceLower)) {
                        routeTargetLocal = item;
                        break;
                    }
                } else {
                    routeTargetLocal = item;
                    break;
                }
            }
            Routing._routeTargetsLock.ExitReadLock();
			if (routeTargetLocal is RouteTarget) {
				List<string> targetParams = new List<string>();
				Type targetParamType;
				foreach (var targetParamItem in routeTargetLocal.Params) {
					targetParamType = targetParamItem.Value.Type;
					targetParams.Add(targetParamItem.Value.HtmlName + " " + targetParamItem.Key);
				}
				routeTarget.FullName += "(" + String.Join(", ", targetParams) + ")";
				routeTarget.Namespace = routeTargetLocal.Namespace;
				if (routeTarget.Namespace != Routing._defaultControllersNamespace) {
					routeTarget.FullName = routeTarget.Namespace + "." + routeTarget.FullName;
				}
				routeTarget.Params = routeTargetLocal.Params;
			} else {
				routeTarget.FullName += "()";
			}
		}
		private string _completeRoute (ref Route route, ref RouteTarget routeTarget, int matched) {
			string[] rowCssClassAndFirstColumn = Routing._completeRouteCssClassAndFirstColumn(ref route, matched);
			string secondColumn = Routing._completeRouteSecondColumn(route);
			string configuredRouteName = Routing._completeRouteConfiguredRouteName(routeTarget);
			string routeParams = this._completeRouteParamsValuesConstraints(route);
			string routeMatches = String.Empty;
			string matchedRouteName = String.Empty;
			if (matched == 2) {
				routeMatches = this._matchedCompleter.Render();
				matchedRouteName = configuredRouteName;
			}
			return String.Format(
				@"<tr{0}><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
				rowCssClassAndFirstColumn[0],
				rowCssClassAndFirstColumn[1],
				secondColumn,
				configuredRouteName + routeParams,
				matchedRouteName + routeMatches
			);
		}
		private string _completeRouteParamsValuesConstraints (Route route) {
			string result = "";
			if (!(route.RouteHandler is StopRoutingHandler)) {
				result = @"<div class=""params"">";
				RouteValueDictionary routeDefaults = route.Defaults;
				RouteValueDictionary routeConstraints = route.Constraints;
				if (routeConstraints == null) routeConstraints = new RouteValueDictionary();
				foreach (var item in routeDefaults) {
					if (item.Key == "controller" || item.Key == "action") continue;
					result += @"<div class=""desharp-dump""><span class=""routeparam"">" 
						+ item.Key + "</span><s>:&nbsp;</s>" 
						+ Routing._renderUrlParamValue(item.Value);
					if (routeConstraints.ContainsKey(item.Key)) {
						if (routeConstraints[item.Key] != null) {
							if (routeConstraints[item.Key] is string) {
								result += @"&nbsp;<span class=""string"">""" + routeConstraints[item.Key].ToString() + @"""</span>";
							} else {
								result += @"&nbsp;<span class=""type"">" + routeConstraints[item.Key].ToString() + @"</span>";
							}
						}
					}
					result += "</div>";
				}
				result += "</div>";
			}
			return result;
		}
		private void _completeBarText () {
			if (this._matchedCompleter.Target is RouteTarget) {
				int firstBracketPos = this._matchedCompleter.Target.FullName.IndexOf("(");
				this._barText = this._matchedCompleter.Target.FullName.Substring(0, firstBracketPos) + " (" + RouteTable.Routes.Count + ")";
			} else {
				this._barText = "No matched route";
			}
		}
		private void _completeMatchedDataTokens () {
			string dataTokensTableRows = "";
			RouteData routeData = HttpContext.Current.Request.RequestContext.RouteData;
			Route route = Routing._safeCastRouteBaseToRoute(routeData.Route);
			if (route == null || route.RouteHandler is StopRoutingHandler) return;
			if (routeData.DataTokens.Keys.Count > 0) {
				foreach (string key in routeData.DataTokens.Keys) {
					dataTokensTableRows += "<tr><td>" + key + "</td><td>" + Dumper.Dump(
						routeData.DataTokens[key], true, Dispatcher.DumpDepth, Dispatcher.DumpMaxLength
					) + "</td></tr>";
				}
				this._dataTokensTable = @"<b class=""heading"">Matched route data tokens:</b><table>"
					+ "<thead><tr><th>Key</th><th>Value</th></tr></thead><tbody>" + dataTokensTableRows + @"</tbody></table>";
			} else {
				this._dataTokensTable = "<p>Matched route has no data tokens.</p>";
			}
		}
		private static Route _safeCastRouteBaseToRoute (RouteBase routeBase) {
			Route route = routeBase as Route;
			if (route == null && routeBase != null) {
				PropertyInfo property = routeBase.GetType().GetProperty("__DebugRoute", BindingFlags.NonPublic | BindingFlags.Instance);
				if (property != null) {
					route = property.GetValue(routeBase, null) as Route;
				}
			}
			return route;
		}
		private static string _renderUrlParamValue (object obj) {
			if (obj == null) {
				return Dumper.GetNullCode(true);
			} else if (obj.GetType().Name == "UrlParameter") {
				return @"<span class=""type"">[Optional]</span>";
			} else {
				return Dumper.DumpPrimitiveType(obj, 0, true, Dispatcher.DumpMaxLength, "");
			}
		}
		private static string[] _completeRouteCssClassAndFirstColumn (ref Route route, int matched) {
			string rowCssClass = String.Empty;
			string firstColumn = String.Empty;
			if (matched == 2) {
				rowCssClass = @" class=""matched""";
				firstColumn = "&#10003;";
			} else if (matched == 1) {
				firstColumn = "&#8776;";
			} else if (route.RouteHandler is StopRoutingHandler) {
				rowCssClass = @" class=""ignore""";
				firstColumn = "&#215;";
			}
			return new[] { rowCssClass, firstColumn };
		}
		private static string _completeRouteSecondColumn (Route route) {
			string result = "~/" + route.Url;
			Regex r = new Regex(@"\{([a-zA-Z0-9_\*]*)\}");
			return @"<div class=""pattern"">""" + r.Replace(result, @"<b>{$1}</b>") + @"""</div>";
		}
		private static string _completeRouteConfiguredRouteName (RouteTarget routeTarget) {
			return routeTarget.FullName.IndexOf(":") > -1
				? @"<div class=""target"">" + routeTarget.FullName + "</div>"
				: routeTarget.FullName;
		}
		private static string _getCurrentRelExecPath () {
			HttpRequest request = HttpContext.Current.Request;
			Uri url = request.Url;
			string requestedPath = request.AppRelativeCurrentExecutionFilePath.TrimStart('~');
			string requestedAndBasePath = url.LocalPath;
			int lastRequestPathPos = requestedAndBasePath.LastIndexOf(requestedPath);
			string basePath = "";
			if (lastRequestPathPos > -1) basePath = requestedAndBasePath.Substring(0, lastRequestPathPos);
			return @"<p class=""source-url"">" + request.HttpMethod.ToUpper() + "&nbsp;"
				+ url.Scheme + "://" + url.Host + (url.Port != 80 ? ":" + url.Port.ToString() : "") + basePath
				+ "<span>" + requestedPath + url.Query + "</span></p>";
		}
	}
}