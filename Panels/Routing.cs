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

namespace Desharp.Panels {
	public class Routing: Abstract {
		public static string PanelName = "routing";
		private static string _defaultControllersNamespace;
		private static volatile List<RouteTarget> _routeTargets = null;
		private static object _ctlrActionsCompletingLock = new object { };
		private bool _barTextAndPanelContentReady = false;
		private string _allRouteTable;
		private string _dataTokensTable;
		private RouteTarget _matchedRouteTarget = null;
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
			this._completeData();
			return this._barText;
		}
		public override string RenderWindowContent () {
			this._completeData();
			return this._panelContent;
		}
		private static void _completeRouteTargetsAndDefaultNamespace () {
			lock (Routing._ctlrActionsCompletingLock) {
				if (Routing._routeTargets == null) {
					Routing._defaultControllersNamespace = String.Format("{0}.Controllers", Tools.GetWebEntryAssembly().GetName().Name);
					Routing._routeTargets = new List<RouteTarget>();
					Assembly entryAssembly = Tools.GetWebEntryAssembly();
					Type systemWebMvcCtrlType = Tools.GetTypeGlobaly("System.Web.Mvc.Controller");
					Type systemWebMvcNonActionAttrType = Tools.GetTypeGlobaly("System.Web.Mvc.NonActionAttribute");
					if (systemWebMvcCtrlType is Type && systemWebMvcNonActionAttrType is Type) {
						IEnumerable<MethodInfo> controllersActions = entryAssembly.GetTypes()
							.Where(type => systemWebMvcCtrlType.IsAssignableFrom(type)) //filter controllers
							.SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
							.Where(method => method.IsPublic && !method.IsDefined(systemWebMvcNonActionAttrType, false));
						foreach (MethodBase controllerAction in controllersActions) {
							ParameterInfo[] args = controllerAction.GetParameters();
							Dictionary<string, object[]> targetArgs = new Dictionary<string, object[]>();
							List<Type> argsTypes = new List<Type>();
							for (int i = 0, l = args.Length; i < l; i += 1) {
								targetArgs.Add(
									args[i].Name,
									new [] {
										args[i].ParameterType, args[i].DefaultValue
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
					}
				}
			}
		}
		private void _completeData () {
			if (this._barTextAndPanelContentReady) return;
			if (Routing._routeTargets == null) Routing._completeRouteTargetsAndDefaultNamespace();
			try {
				this._completeRoutes();
				this._completeBarText();
				this._completeMatchedDataTokens();
				this._panelContent = @"<div class=""content" 
					+ (this._matchedRouteTarget is RouteTarget ? "" : " nomatch") + @""">"
					+ this._allRouteTable
					+ this._getCurrentRelExecPath()
					+ this._dataTokensTable
				+ "</div>";
			} catch (Exception e) {
				Debug.Log(e);
			}
			this._barTextAndPanelContentReady = true;
		}
		private void _completeRoutes () {
			HttpContextBase httpContext = HttpContext.Current.Request.RequestContext.HttpContext;
			bool matched;
			bool matchedRouteFound = false;
			StringBuilder allRouteTableRows = new StringBuilder();
			foreach (RouteBase routeBase in RouteTable.Routes) {
				matched = routeBase.GetRouteData(httpContext) != null;
				Route route = this._safeCastRouteBaseToRoute(routeBase);
				RouteTarget routeTarget = this._completeRouteTarget(route, route.Defaults);
				allRouteTableRows.Append(this._completeRoute(
					route, routeTarget, matched ? (!matchedRouteFound ? 2 : 1) : 0
				));
				if (!matchedRouteFound && matched) {
					this._matchedRouteTarget = routeTarget;
					matchedRouteFound = true;
				}
			}
			this._allRouteTable = @"<b class=""heading"">All Routes:</b><table class=""routes"">"
				+ @"<thead><tr class=""header""><th></th><th>Pattern</th><th>Defaults</th><th>Matched as</th></tr></thead>"
				+ @"<tbody>" + allRouteTableRows.ToString() + "</tbody></table>";
		}
		private Route _safeCastRouteBaseToRoute (RouteBase routeBase) {
			Route route = routeBase as Route;
			if (route == null && routeBase != null) {
				PropertyInfo property = routeBase.GetType().GetProperty("__DebugRoute", BindingFlags.NonPublic | BindingFlags.Instance);
				if (property != null) {
					route = property.GetValue(routeBase, null) as Route;
				}
			}
			return route;
		}
		private RouteTarget _completeRouteTarget (Route route, RouteValueDictionary dictionaryContainingCtrlAndAction) {
			RouteTarget result = new RouteTarget {
				Controller = "",
				Action = "",
				FullName = "",
				Namespaces = new string[] {},
				NamespacesLower = new string[] {},
				Namespace = Routing._defaultControllersNamespace,
				Params = new Dictionary<string, object[]>()
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
					result = this._completeRouteTargetCompleteActionParams(result);
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
		private RouteTarget _completeRouteTargetCompleteActionParams (RouteTarget routeTarget) {
			RouteTarget routeTargetLocal;
			List<RouteTarget> targets = new List<RouteTarget>();
			bool searchForCtrl = routeTarget.Controller != "*";
			bool searchForAction = routeTarget.Action != "*";
			bool searchForNamespace = routeTarget.Namespaces.Length > 0;
			string searchedCtrl = routeTarget.Controller + "Controller";
			foreach (RouteTarget item in Routing._routeTargets) {
				if (searchForCtrl && item.Controller != searchedCtrl) continue;
				if (searchForAction && item.Action != routeTarget.Action) continue;
				if (searchForNamespace) {
					if (routeTarget.NamespacesLower.Contains(item.NamespaceLower)) {
						targets.Add(item);
						break;
					}
				} else {
					targets.Add(item);
					break;
				}
			}
			if (targets.Count > 0) {
				routeTargetLocal = targets.First();
				List<string> targetParams = new List<string>();
				Type targetParamType;
				object targetParamDefaultValue;
				foreach (var targetParamItem in routeTargetLocal.Params) {
					targetParamType = (Type)targetParamItem.Value[0];
					targetParamDefaultValue = targetParamItem.Value[1];
					targetParams.Add(targetParamType.Name + " " + targetParamItem.Key);
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
			return routeTarget;
		}
		private string _completeRoute (Route route, RouteTarget routeTarget, int matched) {
			string[] rowCssClassAndFirstColumn = this._completeRouteCssClassAndFirstColumn(route, matched);
			string secondColumn = this._completeRouteSecondColumn(route);
			string configuredRouteName = this._completeRouteConfiguredRouteName(routeTarget);
			string routeParams = this._completeRouteParamsValuesConstraints(route);
			string routeMatches = String.Empty;
			string matchedRouteName = String.Empty;
			if (matched == 2) {
				routeMatches = this._completeRouteMatchedValues(route, routeTarget);
				matchedRouteName = this._completeRouteMatchedRouteName(route);
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
		private string[] _completeRouteCssClassAndFirstColumn (Route route, int matched) {
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
		private string _completeRouteSecondColumn (Route route) {
			string result = "~/" + route.Url;
			Regex r = new Regex(@"\{([a-zA-Z0-9_\*]*)\}");
			return @"<div class=""pattern"">""" + r.Replace(result, @"<b>{$1}</b>") + @"""</div>";
		}
		private string _completeRouteConfiguredRouteName (RouteTarget routeTarget) {
			return routeTarget.FullName.IndexOf(":") > -1
				? @"<div class=""target"">" + routeTarget.FullName + "</div>"
				: routeTarget.FullName;
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
						+ this._renderUrlParamValue(item.Value);
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
		private string _completeRouteMatchedValues (Route route, RouteTarget routeTarget) {
			string result = "";
			if (route.RouteHandler is StopRoutingHandler) return result;
			HttpRequest request = HttpContext.Current.Request;
			RouteValueDictionary routeDataValues = request.RequestContext.RouteData.Values;
			List<string[]> matchedRouteParams = new List<string[]>();
			List<string[]> otherQueryParams = new List<string[]>();
			List<string> resolvedParams = new List<string>();
			this._completeRouteMatchedParamsByQueryString(
				route, routeTarget, ref matchedRouteParams, ref otherQueryParams, ref resolvedParams
			);
			this._completeRouteMatchedParamsByDefaults(
				route, routeTarget, ref matchedRouteParams, ref otherQueryParams, ref resolvedParams
			);
			result = @"<div class=""params"">";
			foreach (string[] item in matchedRouteParams) {
				result += @"<div class=""desharp-dump"">"
					+ @"<span class=""routeparam"">" + item[0] + "</span>" + "<s>:&nbsp;</s>" + item[1]
					+ (item[2].Length > 0 ? @"&nbsp;(<span class=""string"">""" + item[2] + @"""</span>)" : "") + "</div>";
			}
			foreach (string[] item in otherQueryParams) {
				result += @"<div class=""desharp-dump"">"
					+ @"<span class=""queryparam"">" + item[0] + "</span>" + "<s>:&nbsp;</s>" + item[1] + "</div>";
			}
			result += "</div>";
			return result;
		}
		// todo otestovat - i to zda když nevedu v route defaultni parametr ale je uveden v metode zda se to taky pretypuje
		private void _completeRouteMatchedParamsByQueryString (Route route, RouteTarget routeTarget, ref List<string[]> matchedRouteParams, ref List<string[]> otherQueryParams, ref List<string> resolvedParams) {
			HttpRequest request = HttpContext.Current.Request;
			NameValueCollection queryString = request.QueryString;
			RouteValueDictionary routeDefaults = route.Defaults;
			for (int i = 0, l = queryString.AllKeys.Length; i < l; i += 1) {
				// fill default values for each param
				string paramName = queryString.AllKeys[i];
				string rawValue = "";
				try { 
					rawValue = queryString.Get(paramName);
				} catch (Exception e) {
					rawValue = "";
				}
				Type defaultsType = null;
				Type targetMethodType = null;
				Type targetType = null;
				object typedValue = null;
				bool matchingConstraint;
				// set params has been resolved
				resolvedParams.Add(paramName);
				if (paramName == "controller" || paramName == "action") continue;
				// try to resolve target types
				targetMethodType = routeTarget.Params.ContainsKey(paramName) && routeTarget.Params[paramName].Length > 0 
					? routeTarget.Params[paramName][0] as Type 
					: null;
				if (routeDefaults.ContainsKey(paramName)) {
					defaultsType = routeDefaults[paramName].GetType();
					if (defaultsType.Name == "UrlParameter") defaultsType = null;
				}
				targetType = targetMethodType is Type ? targetMethodType : (defaultsType is Type ? defaultsType : null);
				// lets try to retype raw param into target type as System.Web.Mvc library does if param pass throw it's constraint:
				if (targetType is Type) {
					try {
						targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
						typedValue = Convert.ChangeType(rawValue, targetType);
					} catch { }
					if (typedValue != null) {
						matchedRouteParams.Add(new[] {
							paramName, this._renderUrlParamValue(typedValue), ""
						});
					} else if (routeDefaults.ContainsKey(paramName)) {
						matchedRouteParams.Add(new[] {
							paramName, this._renderUrlParamValue(routeDefaults[paramName]), rawValue
						});
					} else if (routeTarget.Params.ContainsKey(paramName) && routeTarget.Params[paramName][1] != null) {
						matchedRouteParams.Add(new[] {
							paramName, this._renderUrlParamValue(routeTarget.Params[paramName][1]), rawValue
						});
					}
				} else {
					// if there was not possible to convert raw input value to target type 
					// - use value from defaults if there is any or use raw value to display
					if (routeDefaults.ContainsKey(paramName)) {
						matchedRouteParams.Add(new[] {
							paramName, this._renderUrlParamValue(routeDefaults[paramName]), rawValue
						});
					} else {
						otherQueryParams.Add(new[] {
							paramName, this._renderUrlParamValue(rawValue)
						});
					}
				}
			}
		}
		private void _completeRouteMatchedParamsByDefaults (Route route, RouteTarget routeTarget, ref List<string[]> matchedRouteParams, ref List<string[]> otherQueryParams, ref List<string> resolvedParams) {
			HttpRequest request = HttpContext.Current.Request;
			foreach (var item in route.Defaults) {
				string paramName = item.Key;
				if (paramName == "controller" || paramName == "action" || resolvedParams.Contains(paramName)) continue;
				object typedValue = item.Value;
				matchedRouteParams.Add(new[] {
					paramName, this._renderUrlParamValue(typedValue), ""
				});
			}
		}
		private string _renderUrlParamValue (object obj) {
			if (obj == null) {
				return Dumper.GetNullCode(true);
			} else if (obj.GetType().Name == "UrlParameter") {
				return @"<span class=""type"">[Optional]</span>";
			} else {
				return Dumper.DumpPrimitiveType(obj, 0, true, Dispatcher.DumpMaxLength, "");
			}
		}
		private string _completeRouteMatchedRouteName (Route route) {
			string result = "";
			if (!(route.RouteHandler is StopRoutingHandler)) {
				RouteTarget routeTarget = this._completeRouteTarget(
					route, HttpContext.Current.Request.RequestContext.RouteData.Values
				);
				result = @"<div class=""target"">" + routeTarget.FullName + "</div>";
			}
			return result;
		}
		private void _completeBarText () {
			if (this._matchedRouteTarget is RouteTarget) {
				int firstBracketPos = this._matchedRouteTarget.FullName.IndexOf("(");
				this._barText = this._matchedRouteTarget.FullName.Substring(0, firstBracketPos) + " (" + RouteTable.Routes.Count + ")";
			} else {
				this._barText = "No matched route";
			}
		}
		private void _completeMatchedDataTokens () {
			string dataTokensTableRows = "";
			RouteData routeData = HttpContext.Current.Request.RequestContext.RouteData;
			Route route = this._safeCastRouteBaseToRoute(routeData.Route);
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
		private string _getCurrentRelExecPath () {
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