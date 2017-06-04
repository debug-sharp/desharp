using Desharp.Core;
using Desharp.Renderers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Routing;

namespace Desharp.Panels.Routings {
	internal class MatchedCompleter {
		internal Route Route = null;
		internal RouteTarget Target = null;
		internal List<string> UrlParams = new List<string>();
		private List<string> _resolvedParams = new List<string>();
		private List<string> _predefinedParams = new List<string>();
		private List<string> _queryParams = new List<string>();
		private delegate RouteMatchedParam ParamsCompleterHandler (
			Type actionType, Type routeDefaultsType, Type targetType, string paramName
		);
		private static RouteMatchedParam _setUpNewMatchedParamResult (string paramName, object rawValue, Type targetType = null) {
			RouteMatchedParam result = new RouteMatchedParam();
			if (targetType is Type) {
				try {
					targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
					result.PrimaryValue = Convert.ChangeType(rawValue, targetType);
				} catch { }
			}
			return result;
		}
		internal string Render () {
			string result = "";
			if (this.Route.RouteHandler is StopRoutingHandler) return result;
			this._completeParamsInUrl();
			HttpRequest request = HttpContext.Current.Request;

			this._completePredefinedParams(
				this.UrlParams, 
				this._completeParamsByRouteUrlWhenRouteValuesContainsValue, 
				this._completeParamsByRouteUrlWhenRouteValuesContainsNothing
			);
			this._completePredefinedParams(
				this.Route.Defaults.Keys.ToList<string>(),
				this._completeParamsByRouteDefaultsWhenRouteValuesContainsValue,
				this._completeParamsByRouteDefaultsWhenRouteValuesContainsNothing
			);
			
			//this._completeParamsByRouteUrl(ref predefinedParams, ref resolvedParams);
			//this._completeParamsByRouteDefaults(ref predefinedParams, ref resolvedParams);
			//this._completeParamsByPredictedQueryParams(ref queryParams, ref resolvedParams);
			//this._completeParamsByFreeQueryParams(ref queryParams, ref resolvedParams);

			result = @"<div class=""params"">";
			foreach (string renderedPredefinedParam in this._predefinedParams) {
				result += renderedPredefinedParam;
			}
			foreach (string renderedQueryParam in this._queryParams) {
				result += renderedQueryParam;
			}
			result += "</div>";
			return result;
		}
		private void _completeParamsInUrl () {
			FieldInfo fi = this.Route.GetType().GetField("_parsedRoute", BindingFlags.NonPublic | BindingFlags.Instance);
			object parsedRoute = fi.GetValue(this.Route);
			PropertyInfo pi = parsedRoute.GetType().GetProperty("PathSegments", BindingFlags.NonPublic | BindingFlags.Instance);
			IEnumerable<object> pathSegments = pi.GetValue(parsedRoute, null) as IEnumerable<object>;
			Type pathSegmentType;
			IEnumerator pathSegmentsEnum = pathSegments.GetEnumerator();
			IEnumerable<object> subsegments;
			Type subsegmentType;
			IEnumerator subsegmentsEnum;
			string paramName;
			try {
				while (pathSegmentsEnum.MoveNext()) {
					pathSegmentType = pathSegmentsEnum.Current.GetType();
					if (pathSegmentType.Name == "SeparatorPathSegment") continue;
					pi = pathSegmentsEnum.Current.GetType().GetProperty("Subsegments", BindingFlags.Public | BindingFlags.Instance);
					subsegments = pi.GetValue(pathSegmentsEnum.Current, null) as IEnumerable<object>;
					subsegmentsEnum = subsegments.GetEnumerator();
					while (subsegmentsEnum.MoveNext()) {
						subsegmentType = subsegmentsEnum.Current.GetType();
						if (subsegmentType.Name == "ParameterSubsegment") {
							pi = subsegmentType.GetProperty("ParameterName", BindingFlags.Public | BindingFlags.Instance);
							// sometimes somebody could make a mistake and put star char after param name, then it's not removed by microsoft
							paramName = pi.GetValue(subsegmentsEnum.Current, null).ToString().Replace("*", "");
							this.UrlParams.Add(paramName);
						}
					}
				}
			} catch { }
		}
		private void _completePredefinedParams (
			List<string> paramsCollection, 
			ParamsCompleterHandler paramsCompleterWhenRouteValuesContainsValue, 
			ParamsCompleterHandler paramsCompleterWhenRouteValuesContainsNothing
		) {
			RouteValueDictionary routeValues = HttpContext.Current.Request.RequestContext.RouteData.Values;
			Type actionType;
			Type routeDefaultsType;
			Type targetType;
			Type[] types;
			RouteMatchedParam matchedParamToRender;
			foreach (string paramName in paramsCollection) {
				if (paramName == "controller" || paramName == "action" || this._resolvedParams.Contains(paramName)) continue;
				// set params has been resolved
				this._resolvedParams.Add(paramName);
				// try to resolve target type
				types = this._getActionTypeRouteDefaultTypeAndTargetType(paramName);
				actionType = types[0];
				routeDefaultsType = types[1];
				targetType = types[2];
				if (routeValues.ContainsKey(paramName)) {
					matchedParamToRender = paramsCompleterWhenRouteValuesContainsValue(
						actionType, routeDefaultsType, targetType, paramName
					);
				} else {
					matchedParamToRender = paramsCompleterWhenRouteValuesContainsNothing(
						actionType, routeDefaultsType, targetType, paramName
					);
				}
				this._predefinedParams.Add(
					MatchedCompleter._renderParamWithVariations(paramName, matchedParamToRender)
				);
			}
		}

		private RouteMatchedParam _completeParamsByRouteUrlWhenRouteValuesContainsValue (
			Type actionType, Type routeDefaultsType, Type targetType, string paramName
		) {
			RouteValueDictionary routeValues = HttpContext.Current.Request.RequestContext.RouteData.Values;
			RouteValueDictionary routeDefaults = this.Route.Defaults;
			// if there is any target type resolved (there was something in action or in defaults - try to retype route.Values into it
			object rawValue = routeValues[paramName];
			RouteMatchedParam result = MatchedCompleter._setUpNewMatchedParamResult(paramName, rawValue, targetType);
			if (result.PrimaryValue != null) {
				// retyping was possible - there could be used value from url successfully retyped or default value successfully retyped
				if (routeDefaultsType is Type) {
					if (
						routeDefaultsType.FullName == routeValues[paramName].GetType().FullName &&
						routeDefaults[paramName].GetHashCode() == routeValues[paramName].GetHashCode()
					) {
						if (actionType is Type) {
							result.PrimaryValueDescription = new[] { "default", "from route default" };
						} else {
							result.PrimaryValueDescription = new[] { "default", "from route default in route values" };
						}
					}
				}
				if (result.PrimaryValueDescription.Length < 2) {
					if (actionType is Type) {
						result.PrimaryValueDescription = new[] { "ok", "from url" };
					} else {
						result.PrimaryValueDescription = new[] { "ok", "in route values" };
						result.ValueVariations.Add("after manual retype", result.PrimaryValue);
						result.PrimaryValue = rawValue;
					}
				}
			} else {
				// there is some value in url, but not matching exactly target type
				// so there should be used default value from route.Default or default value from action or nothing
				if (actionType is Type && this.Target.Params[paramName].DefaultValue is System.DBNull) {
					// method has argument named as paramName but with no default value - doesn't matter if method argument type is Nullable(?) or not
					if (this.Target.Params[paramName].IsNullable) {
						result.PrimaryValue = null;
						if (routeDefaultsType is Type && routeDefaultsType.FullName == actionType.FullName) {
							result.PrimaryValueDescription = new[] { "weird", "from action default", "WTF: why it's not used the route default value?" };
						} else {
							// some value in url, not matching exactly target type, method has paramName without default, but nullable
							result.PrimaryValueDescription = new[] { "default", "from action default" };
						}
						// display route.Values[paramName] as subvalue if any
						if (routeValues.ContainsKey(paramName) && rawValue != null) {
							Type rawValueType = rawValue.GetType();
							rawValueType = Nullable.GetUnderlyingType(rawValueType) ?? rawValueType;
							if (rawValueType.Name != "UrlParameter" && rawValueType.FullName != actionType.FullName) {
								result.ValueVariations.Add("in route values", routeValues[paramName]);
							} else if (rawValueType.Name != "UrlParameter") {
								result.ValueVariations.Add("url value", routeValues[paramName]);
							}
						}
					} else {
						// here is always MVC Exception - so display at least what is in route.Values[paramName]
						result.PrimaryValue = routeValues[paramName];
						result.PrimaryValueDescription = new[] { "weird", "not possible", "add default value to action:-(" };
					}
				} else if (actionType is Type) {
					// method has it's own default argument value
					if (routeDefaultsType is Type && routeDefaultsType.FullName == actionType.FullName) {
						result.PrimaryValueDescription = new[] { "weird", "from action default", "WTF: why it's not used the default route value?" };
					} else {
						result.PrimaryValueDescription = new[] { "default", "from action default" };
					}
					result.PrimaryValue = this.Target.Params[paramName].DefaultValue;
					// display route.Values[paramName] as subvalue if any
					if (routeValues.ContainsKey(paramName)) if (rawValue != null) if (rawValue.GetType().Name != "UrlParameter")
								result.ValueVariations.Add("url value", routeValues[paramName]);
				} else {
					// method has not any argument with paramName
					if (routeValues.ContainsKey(paramName)) if (rawValue != null) if (rawValue.GetType().Name == "UrlParameter") {
								result.PrimaryValue = System.DBNull.Value;
								result.PrimaryValueDescription = new[] { "empty", "nothing, not contained in route values" };
							}
					if (result.PrimaryValueDescription.Length < 2) {
						result.PrimaryValue = routeValues[paramName];
						result.PrimaryValueDescription = new[] { "raw", "from url in route values" };
					}
				}
			}
			return result;
		}

		private RouteMatchedParam _completeParamsByRouteUrlWhenRouteValuesContainsNothing (
			Type actionType, Type routeDefaultsType, Type targetType, string paramName
		) {
			RouteValueDictionary routeDefaults = this.Route.Defaults;
			RouteMatchedParam result = new RouteMatchedParam();
			// no param value defined in url and no constraint or constraint is too benevolent
			// - display only default value from route default or from action
			if (routeDefaultsType is Type && routeDefaultsType.Name != "UrlParameter" && actionType is Type && actionType.FullName == routeDefaultsType.FullName) {
				result.PrimaryValue = routeDefaults[paramName];
				result.PrimaryValueDescription = new[] { "default", "from route default" };
			} else if (routeDefaultsType is Type && routeDefaultsType.Name != "UrlParameter" && actionType is Type && actionType.FullName != routeDefaultsType.FullName) {
				try {
					targetType = Nullable.GetUnderlyingType(actionType) ?? actionType;
					result.PrimaryValue = Convert.ChangeType(routeDefaults[paramName], targetType);
					result.PrimaryValueDescription = new[] { "default", "from route default" };
					// display as subvalue action default value if exist
					if (!(this.Target.Params[paramName].DefaultValue is System.DBNull)) {
						result.ValueVariations.Add("action default", this.Target.Params[paramName].DefaultValue);
					}
				} catch { }
			}
			// - only for cases when route default value was completely weird 
			//   - like method requires decimal and default values are alphabetical chars...
			if (result.PrimaryValueDescription.Length < 2 && actionType is Type) {
				if (!(this.Target.Params[paramName].DefaultValue is System.DBNull)) {
					result.PrimaryValue = this.Target.Params[paramName].DefaultValue;
					result.PrimaryValueDescription = new[] { "default", "from action default" };
				}
			}
			return result;
		}
		
		private RouteMatchedParam _completeParamsByRouteDefaultsWhenRouteValuesContainsValue (
			Type actionType, Type routeDefaultsType, Type targetType, string paramName
		) {
			RouteValueDictionary routeValues = HttpContext.Current.Request.RequestContext.RouteData.Values;
			RouteValueDictionary routeDefaults = this.Route.Defaults;
			// if there is any target type resolved (there was something in action or in defaults - try to retype route.Values into it
			object rawValue = routeValues[paramName];
			RouteMatchedParam result = MatchedCompleter._setUpNewMatchedParamResult(paramName, rawValue, targetType);
			if (actionType is Type) {
				// pokud je typ parametru na akci definován a má i nemá defaultní hodnotu, tak i když je v url 
				// zkonvertovatelná hodnota, nesmyslná hodnota či nic, vždy se použije do hodnoty parametru v akci to,
				// co je definováno za defauktní hodnotu routy - překonvertovateln do typu parametru akce.
				// - pro takové případy kdyby se měla stejně nakonec použít překonvertovatelná hodnota z url a místo toho se použije route.Default- zobrazt WEIRD!
				// - pokud tedy dojde k úspěčné konverzi z hodnoty query stringu - zobrazíme navíc i tuto hodnotu jako variantu
				// - pokud nedojde k dobré konverzi pak taky jako podvariantu ale s jiným komentářem
				// - pokud v query stringu nic není - zobrazíme jen to co je nakonec dosazeno z route default
				// - pokud v route default je třeba nějaký nezkonvertovatelný nesmysl - pak se routa nematchne, nikdy k situaci nedochází


			} else {
				// pokud není typ parametru v akci definován a metoda je bez něj, zbývá zobrazit buď jen hlášky:
				// - nothing, not contained in action params - pro případ že nic v query stringu ani neni
				//	 - někdy když je query string hodnota zkonvertovatelna do ciloveho typu podle route.Defaults, pak to zobrazit jako variantu
				//	   že je možné použít i toto po manuálním přetypování, pokud to přetypovat nepujde - zobrazit co v query stringu bylo jako raw...
				//   - vždy se do route.Values stejně dosadí to co je v route.Defaults - i tak zobrazit

			}
			return result;
		}
		private RouteMatchedParam _completeParamsByRouteDefaultsWhenRouteValuesContainsNothing (
			Type actionType, Type routeDefaultsType, Type targetType, string paramName
		) {
			RouteValueDictionary routeValues = HttpContext.Current.Request.RequestContext.RouteData.Values;
			RouteValueDictionary routeDefaults = this.Route.Defaults;
			RouteMatchedParam result = new RouteMatchedParam();
			
			return result;
		}




		private void _completeParamsByPredictedQueryParams (ref List<string> queryParams, ref List<string> resolvedParams) {

			//NameValueCollection queryString = HttpContext.Current.Request.QueryString;
		}
		private void _completeParamsByFreeQueryParams (ref List<string> queryParams, ref List<string> resolvedParams) {

			//NameValueCollection queryString = HttpContext.Current.Request.QueryString;
		}




		private static string _renderParamWithVariations (string paramName, RouteMatchedParam paramData) {
			Type valType = null;
			string primaryValueHashCode = "";
			if (paramData.PrimaryValue != null && !(paramData.PrimaryValue is System.DBNull)) {
				valType = paramData.PrimaryValue.GetType();
				primaryValueHashCode = paramData.PrimaryValue.GetHashCode().ToString();
			} else {
				string newDummyValue = "";
				primaryValueHashCode = newDummyValue.GetHashCode().ToString();
			}
			DumpType type = (paramData.PrimaryValue is string)
				? Dumper.GetDumpTypes(paramData.PrimaryValue, paramData.PrimaryValue.ToString().Length.ToString(), true, false, "99")
				: (paramData.PrimaryValue is System.DBNull
					? new DumpType { ValueTypeCode = "" }
					: Dumper.GetDumpTypes(paramData.PrimaryValue, "", true, false, "99")
				);
			string renderedValue = paramData.PrimaryValue == null
				? Dumper.GetNullCode(true)
				: Dumper.RenderPrimitiveTypeValue(paramData.PrimaryValue, true, Dispatcher.DumpMaxLength);
			string renderedDescription = @"<i class=""" + paramData.PrimaryValueDescription[0] + @"""" + (paramData.PrimaryValueDescription.Length > 2 ? @" title=""" + paramData.PrimaryValueDescription[2] + @"""" : "") + ">"
					+ paramData.PrimaryValueDescription[1].Replace(" ", "&nbsp;")
				+ "</i>";
			if (paramData.PrimaryValue == null || paramData.PrimaryValue is System.DBNull) {
				type.ValueTypeCode = "<span></span>";
				if (paramData.ValueVariations.Count > 0)
					renderedDescription = @"<span class=""click click-99" + primaryValueHashCode + @" type"">" + renderedDescription + "</span>";
			} else {
				if (paramData.ValueVariations.Count > 0) type.ValueTypeCode = type.ValueTypeCode
					.Replace(@"<span class=""", @"<span class=""click click-99" + primaryValueHashCode + " ");
			}
			type.ValueTypeCode = type.ValueTypeCode.Replace("</span>", (paramData.PrimaryValue is System.DBNull ? "" : "&nbsp;") + renderedDescription + "</span>");
			string result = @"<div class=""desharp-dump"">"
				+ @"<span class=""route-param"">" + Tools.HtmlEntities(paramName) + "</span>"
				+ (paramData.PrimaryValue is System.DBNull ? @"<s>:</s>" : @"<s>:&nbsp;</s>")
				+ (paramData.PrimaryValue == null
					? renderedValue + "&nbsp;" + renderedDescription
					: @"<span class=""" + type.NameCssClass + @""">" + renderedValue + "</span>&nbsp;" + type.ValueTypeCode
				);
			if (paramData.ValueVariations.Count > 0) {
				result += @"<div class=""item dump dump-99" + primaryValueHashCode + @""">";
				int i = 0;
				foreach (var item in paramData.ValueVariations) {
					if (i > 0) result += "<br />";
					type = (item.Value is string)
						? Dumper.GetDumpTypes(item.Value, item.Value.ToString().Length.ToString(), true, false, "99")
						: Dumper.GetDumpTypes(item.Value, "", true, false, "99");
					renderedValue = item.Value == null
						? Dumper.GetNullCode(true)
						: Dumper.RenderPrimitiveTypeValue(item.Value, true, Dispatcher.DumpMaxLength);
					result += Dumper.TabsIndent(1, true)
						+ @"<span class=""param-place"">" + item.Key.Replace(" ", "&nbsp;") + "</span>"
						+ @"<s>:&nbsp;</s>"
						+ @"<span class=""" + type.NameCssClass + @""">" + renderedValue + "</span>&nbsp;" + type.ValueTypeCode;
				}
				result += "</div>";
			}
			result += "</div>";
			return result;
		}
		private Type[] _getActionTypeRouteDefaultTypeAndTargetType (string paramName) {
			Type routeDefaultsType = null;
			Type actionType = this.Target.Params.ContainsKey(paramName) ? this.Target.Params[paramName].Type : null;
			if (this.Route.Defaults.ContainsKey(paramName)) {
				routeDefaultsType = this.Route.Defaults[paramName].GetType();
				if (routeDefaultsType.Name == "UrlParameter") routeDefaultsType = null;
			}
			Type targetType = actionType is Type ? actionType : (routeDefaultsType is Type ? routeDefaultsType : null);
			return new Type[] {
				actionType, routeDefaultsType, targetType
			};
		}
	}
}
