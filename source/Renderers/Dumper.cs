using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using Desharp.Core;
using Desharp.Completers;
using System.Collections;
using System.Web;
using System.Text.RegularExpressions;

namespace Desharp.Renderers {
	public class Dumper {
		public static string Dump (object obj, int level = 0, bool htmlOut = false, int maxDepth = 0) {
			return Dumper._dump(obj, level, htmlOut, null, maxDepth).Content;
		}
		private static DumpItem _dump (object obj, int level = 0, bool htmlOut = false, List<int> ids = null, int maxDepth = 0) {
			if (ids == null) ids = new List<int>();
			if (maxDepth == 0) maxDepth = Core.Environment.Depth;
			if (obj != null) {
				int objId = obj.GetHashCode();
				if (ids.Contains(objId)) {
					// detected recursion
					return new DumpItem {
						Complex = false,
						Content = htmlOut ? "<span class=\"recursion click click-" + objId.ToString() + "\">{*** RECURSION ***}</span>" : "{*** RECURSION ***}"
					};
				} else {
					ids.Add(objId);
				}
			}
			if (level == maxDepth) {
				// detected last level depth
				if (Detector.IsTypeObject(obj)) {
					return Dumper._dumpTypeObject(obj as Type, level, htmlOut);
				} else if (Detector.IsEnum(obj)) {
					return Dumper._dumpEnum(obj, level, htmlOut);
				} else if (Detector.IsSimpleType(obj)) {
					return Dumper._dumpSimpleType(obj, level, htmlOut);
				} else {
					DumpType type = Dumper.GetDumpTypes(obj, "", htmlOut, true);
					return new DumpItem {
						Complex = false,
						Content = type.ValueTypeCode
					};
				}
			}
			DumpItem result;
			if (Detector.IsTypeObject(obj)) {
				result = Dumper._dumpTypeObject(obj as Type, level, htmlOut);
			} else if (Detector.IsEnum(obj)) {
				result = Dumper._dumpEnum(obj, level, htmlOut);
			} else if (Detector.IsSimpleType(obj)) {
				result = Dumper._dumpSimpleType(obj, level, htmlOut);
			} else if (Detector.IsSimpleArray(obj)) {
				result = Dumper._dumpSimpleArray(obj, level, htmlOut, ids, maxDepth);
			} else if (Detector.IsDictionary(obj)) {
				result = Dumper._dumpDictionary(obj, level, htmlOut, ids, maxDepth);
			} else if (Detector.IsDictionaryInnerCollection(obj)) {
				result = Dumper._dumpDictionaryInnerCollection(obj, level, htmlOut, ids, maxDepth);
			} else if (Detector.IsEnumerable(obj)) {
				result = Dumper._dumpEnumerable(obj, level, htmlOut, ids, maxDepth);
			} else if (Detector.IsDbResult(obj)) {
				result = Dumper._dumpDbResult(obj, level, htmlOut, ids, maxDepth);
			} else if (!(Detector.IsReflectionObject(obj) || Detector.IsReflectionObjectArray(obj))) {
				result = Dumper._dumpUnknown(obj, level, htmlOut, ids, maxDepth);
			} else {
				result = new DumpItem {
					Complex = false,
					Content = obj.ToString()
				};
			}
			if (htmlOut) {
				if (level == 0) {
					result.Content = "<div class=\"item\">" + result.Content + "</div>";
				}
			}
			return result;
		}
		internal static DumpType GetDumpTypes (object obj, string length = "", bool htmlOut = false, bool fullTypeName = false) {
			string typeStr = "";
			string valueTypeHtml = "";
			string nameTypeClass = "";
			string valueTypeClickClass = "";
			if (obj == null) {
				typeStr = "null";
			} else {
				valueTypeClickClass = (length.Length > 0 && !(obj is string)) ? "click click-" + obj.GetHashCode().ToString() : "";
				Type type = obj.GetType() as Type;
				if (type != null) {
					Type[] gta = type.GetGenericArguments();
					if (fullTypeName) {
						typeStr = type.FullName.ToString();
					} else {
						typeStr = type.Name.ToString();
					}
					if (gta.Length > 0) {
						if (typeStr.IndexOf('`') > -1) {
							List<string> gtaStrs = new List<string>();
							for (int i = 0, l = gta.Length; i < l; i++) {
								gtaStrs.Add(gta[i].Name.ToString());
							}
							if (htmlOut) {
								typeStr = typeStr.Substring(0, typeStr.IndexOf('`')) + "&lt;" + String.Join(",", gtaStrs.ToArray()) + "&gt;";
							} else {
								typeStr = typeStr.Substring(0, typeStr.IndexOf('`')) + "<" + String.Join(",", gtaStrs.ToArray()) + ">";
							}
						}
					}
				}
			}
			if (typeStr.IndexOf("<>f__AnonymousType2") > -1) { // mostly simple base object with key/value passed into Dump method
				typeStr = "Object" + typeStr.Substring(19);
			}
			if (htmlOut) nameTypeClass = typeStr.ToLower();
			if (length.Length > 0) {
				int lastArrCharsPos = typeStr.LastIndexOf("[]");
				if (lastArrCharsPos == typeStr.Length - 2) {
					int startIndex = lastArrCharsPos; // typeStr.Length - 2
					int arrCharsPos;
					while (true) {
						arrCharsPos = typeStr.IndexOf("[]", startIndex - 2);
						if (arrCharsPos > -1 && arrCharsPos < lastArrCharsPos) {
							lastArrCharsPos = arrCharsPos;
							startIndex -= 2;
						} else {
							break;
						}
					}
					typeStr = typeStr.Substring(0, lastArrCharsPos) + "[" + length + "]" + typeStr.Substring(lastArrCharsPos + 2);
				} else if (obj != null) {
					typeStr = typeStr + "(" + length + ")";
				}
			}
			if (htmlOut) {
				valueTypeHtml = "<span class=\"type " + valueTypeClickClass + "\">[" + typeStr + "]</span>";
			} else {
				valueTypeHtml = "[" + typeStr + "]";
			}
			if (htmlOut) {
				return new DumpType {
					Text = typeStr.ToLower(),
					ValueTypeCode = valueTypeHtml,
					NameCssClass = nameTypeClass
				};
			} else {
				return new DumpType {
					Text = typeStr,
					ValueTypeCode = valueTypeHtml,
				};
			}
		}
		private static DumpItem _dumpEnum (object obj, int level, bool htmlOut) {
			DumpType type = Dumper.GetDumpTypes(obj, "", htmlOut, true);
			List<string> result = new List<string>();
			Type objType = obj.GetType();
			Enum objEnum = (Enum)obj;
			IEnumerable<FieldInfo> fis = objType.GetFields();
			object val;
			bool hasFlag = false;
			foreach (FieldInfo fi in fis) {
				val = fi.GetValue(obj);
				try {
					hasFlag = objEnum.HasFlag((Enum)val);
				} catch (Exception e) {
					hasFlag = false;
				}
				if (hasFlag) result.Add(fi.Name);
			}
			return new DumpItem {
				Complex = false,
				Content = String.Join(", ", result.ToArray()) + " " + type.ValueTypeCode
			};
		}
		private static DumpItem _dumpTypeObject (Type obj, int level = 0, bool htmlOut = true) {
			string htmlValue = obj == null ? "null" : obj.FullName;
			DumpType type = Dumper.GetDumpTypes(obj, "", htmlOut);
			DumpItem result = new DumpItem { Complex = false };
			if (htmlOut) {
				result.Content = "<span class=\"" + type.Text + "\">" + htmlValue + "</span>&nbsp;" + type.ValueTypeCode;
			} else {
				result.Content = htmlValue + " [" + type.Text + "]";
			}
			return result;
		}
		private static DumpItem _dumpSimpleType (object obj, int level = 0, bool htmlOut = true) {
			string htmlValue = "";
			if (obj == null) {
				htmlValue = "null";
			} else {
				htmlValue = obj.ToString();
				if (obj is char || obj is string) {
					if (htmlOut) htmlValue = Tools.HtmlEntities(htmlValue.ToString());
				} else if (obj is bool) {
					htmlValue = htmlValue.ToLower();
				} else if (obj is double) {
					htmlValue = htmlValue.Replace(',', '.'); // Microsoft .NET environment ToString() translates double into shits
				}
			}
            DumpType type = (obj is string) ? Dumper.GetDumpTypes(obj, obj.ToString().Length.ToString(), htmlOut) : Dumper.GetDumpTypes(obj, "", htmlOut);
            DumpItem result = new DumpItem { Complex = false };
            if (htmlOut) {
                result.Content = "<span class=\"" + type.NameCssClass + "\">" + htmlValue + "</span>&nbsp;" + type.ValueTypeCode;
            } else {
                result.Content = htmlValue + " [" + type.Text + "]";
            }
            return result;
        }
        private static DumpItem _dumpSimpleArray (object obj, int level = 0, bool htmlOut = true, List<int> ids = null, int maxDepth = 0) {
            dynamic simpleArray = Dumper._getSimpleTypeArray(obj);
            DumpType type = Dumper.GetDumpTypes(obj, simpleArray.length.ToString(), htmlOut);
            string result = type.ValueTypeCode;
            DumpItem dumpedChild;
            object child;
            if (htmlOut) result += "<div class=\"item dump dump-" + obj.GetHashCode().ToString() + "\">";
            for (int i = 0, l = simpleArray.length; i < l; i += 1) {
                child = simpleArray.array[i];
                dumpedChild = Dumper._dump(child, level + 1, htmlOut, new List<int>(ids), maxDepth);
                if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + Dumper._tabsIndent(level + 1, htmlOut)
                        + "<span class=\"int\">" 
                            + i.ToString() 
                        + "</span>:&nbsp;"
                        + dumpedChild.Content;
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                        + i.ToString() + ": "
                        + dumpedChild.Content;
                }
            }
            if (htmlOut) result += "</div>";
            return new DumpItem {
                Complex = true,
                Content = result
            };
        }
        private static DumpItem _dumpDictionary (object obj, int level = 0, bool htmlOut = false, List<int> ids = null, int maxDepth = 0) {
            dynamic objDct = (dynamic)obj;
            DumpType type = Dumper.GetDumpTypes(obj, objDct.Count.ToString(), htmlOut);
            string result = type.ValueTypeCode;
            DumpItem dumpedChild;
            object child;
            string keyStr;
            DumpType subTypeKey;
            DumpType subTypeValue;
			if (htmlOut) result += "<div class=\"item dump dump-" + obj.GetHashCode().ToString() + "\">";
			int i = 0;
			foreach (dynamic item in objDct) {
                child = item.Value;
                keyStr = item.Key.ToString();
                subTypeKey = Dumper.GetDumpTypes(item.Key, "", htmlOut);
                subTypeValue = Dumper.GetDumpTypes(child, "", htmlOut);
				child = child == null ? "null" : child;
				dumpedChild = Dumper._dump(child, level + 1, htmlOut, new List<int>(ids), maxDepth);
                if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + Dumper._tabsIndent(level + 1, htmlOut)
                        + "<span class=\"" + subTypeKey.NameCssClass + "\">" + keyStr + "</span>"
                        + ":&nbsp;" + dumpedChild.Content;
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                        + keyStr + ": " + dumpedChild.Content;
                }
				i++;
			}
			if (htmlOut) result += "</div>";
			return new DumpItem {
                Complex = true,
                Content = result
            };
        }
        private static DumpItem _dumpDictionaryInnerCollection (object obj, int level = 0, bool htmlOut = false, List<int> ids = null, int maxDepth = 0) {
            string result = "";
            int length = 0;
            List<string> objList = new List<string>();
            if (obj is Dictionary<string, object>.KeyCollection) {
                Dictionary<string, object>.KeyCollection objKeyCol = obj as Dictionary<string, object>.KeyCollection;
                objList = objKeyCol.ToList();
            } else if (obj is Dictionary<string, object>.ValueCollection) {
                Dictionary<string, object>.ValueCollection objValCol = obj as Dictionary<string, object>.ValueCollection;
                foreach (var item in objValCol) {
                    objList.Add(item.ToString());
                }
            }
            length = objList.Count;
            DumpType type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut);
            result += type.ValueTypeCode;
			DumpItem dumpedChild;
			object child;
            string keyStr;
			if (htmlOut) result += "<div class=\"item dump dump-" + obj.GetHashCode().ToString() + "\">";
			for (int i = 0; i < length; i++) {
                child = objList[i];
                keyStr = i.ToString();
				child = child == null ? "null" : child;
				dumpedChild = Dumper._dump(child, level + 1, htmlOut, new List<int>(ids), maxDepth);
				if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + Dumper._tabsIndent(level + 1, htmlOut)
                        + "<span class=\"int\">" + keyStr + "</span>"
                        + ":&nbsp;" + dumpedChild.Content;
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                        + keyStr + ": " + dumpedChild.Content;
                }
			}
			if (htmlOut) result += "</div>";
			return new DumpItem {
				Complex = true,
				Content = result
			};
		}
        private static DumpItem _dumpEnumerable (object obj, int level = 0, bool htmlOut = false, List<int> ids = null, int maxDepth = 0) {
            string result = "";
            int length = 0;
            dynamic objEnum = (dynamic)obj;
            if (obj is System.Data.SqlClient.SqlDataReader) {
                length = objEnum.FieldCount;
            } else if (obj is System.Array) {
                length = objEnum.Length;
            } else {
				length = objEnum.Count;
            }
            DumpType type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut);
            result += type.ValueTypeCode;
			DumpItem dumpedChild;
			object child;
            string keyStr;
			if (htmlOut) result += "<div class=\"item dump dump-" + obj.GetHashCode().ToString() + "\">";
			for (int i = 0; i < length; i++) {
                child = objEnum[i];
                keyStr = i.ToString();
				child = child == null ? "null" : child;
				dumpedChild = Dumper._dump(child, level + 1, htmlOut, new List<int>(ids), maxDepth);
				if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + Dumper._tabsIndent(level + 1, htmlOut)
                    + "<span class=\"int\">" + keyStr + "</span>"
                    + ":&nbsp;" + dumpedChild.Content;
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                    + keyStr + ": " + dumpedChild.Content;
                }
			}
			if (htmlOut) result += "</div>";
			return new DumpItem {
				Complex = true,
				Content = result
			};
		}
        private static DumpItem _dumpDbResult (object obj, int level = 0, bool htmlOut = false, List<int> ids = null, int maxDepth = 0) {
            string result = "";
            DumpType type;
            int length = 0;
			DumpItem dumpedChild;
            object child;
			if (htmlOut) result += "<div class=\"item dump dump-" + obj.GetHashCode().ToString() + "\">";
			if (obj is DataSet) {
                DataSet ds = obj as DataSet;
                length = ds.Tables.Count;
                type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut);
                result += type.ValueTypeCode;
                for (int i = 0; i < length; i++) {
                    child = ds.Tables[i];
                    DataTable table = ds.Tables[i];
					dumpedChild = Dumper._dump(child, level + 1, htmlOut, new List<int>(ids), maxDepth);
					if (htmlOut) {
                        result += (i > 0 ? "<br />" : "") + Dumper._tabsIndent(level + 1, htmlOut)
                            + "<span class=\"table\">" + table.TableName + "</span>:&nbsp;" 
                            + dumpedChild.Content;
                    } else {
                        result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                            + table.TableName + ": " + dumpedChild.Content;
                    }
				}
			} else if (obj is DataTable) {
                DataTable ds = obj as DataTable;
                DataRowCollection rows = ds.Rows;
                length = rows.Count;
                type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut);
                result += type.ValueTypeCode;
                for (int i = 0; i < length; i++) {
                    child = rows[i];
					dumpedChild = Dumper._dump(child, level + 1, htmlOut, new List<int>(ids), maxDepth);
					if (htmlOut) {
                        result += (i > 0 ? "<br />" : "") + Dumper._tabsIndent(level + 1, htmlOut)
                            + "<span class=\"int\">" + i.ToString() + "</span>:&nbsp;" 
                            + dumpedChild.Content;
                    } else {
                        result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                            + i.ToString() + ": " + dumpedChild.Content;
                    }
				}
			} else if (obj is DataRow) {
                DataRow row = obj as DataRow;
                DataColumnCollection columns = row.Table.Columns;
                length = columns.Count;
                type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut);
                result += type.ValueTypeCode;
				int i = 0;
                foreach (DataColumn column in columns) {
                    DumpType subTypeValue = Dumper.GetDumpTypes(row[column], "", htmlOut);
					string subTypeCls = subTypeValue.Text.ToString().ToLower();
                    string val = subTypeCls == "dbnull" ? "DBNull" : row[column].ToString();
					if (htmlOut) {
                        result += (i > 0 ? "<br />" : "") + Dumper._tabsIndent(level + 1, htmlOut)
                        + "<span class=\"column\">" + column.ToString() + "</span>:&nbsp;"
                        + "<span class=\" " + subTypeCls + "\">" + val + "</span>&nbsp;" + subTypeValue.ValueTypeCode;
                    } else {
                        result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                        + column.ToString() + ": "
                        + val + " " + subTypeValue.ValueTypeCode;
                    }
					i++;
                }
			}
			if (htmlOut) result += "</div>";
			return new DumpItem {
				Complex = true,
				Content = result
			};
		}
        private static DumpItem _dumpUnknown (object obj, int level = 0, bool htmlOut = true, List<int> ids = null, int maxDepth = 0) {
			if (obj == null) {
				return Dumper._dumpUnknownNotTyped(obj, level, htmlOut, ids, maxDepth);
			} else {
				Type objType = obj.GetType();
				if (objType == null) {
					return Dumper._dumpUnknownNotTyped(obj, level, htmlOut, ids, maxDepth);
				} else {
					return Dumper._dumpUnknownTyped(obj, level, htmlOut, ids, maxDepth);
				}
			}
        }
        private static DumpItem _dumpUnknownNotTyped (object obj, int level = 0, bool htmlOut = true, List<int> ids = null, int maxDepth = 0) {
            PropertyDescriptorCollection objProperties = TypeDescriptor.GetProperties(obj);
            DumpType type = Dumper.GetDumpTypes(obj, objProperties.Count.ToString(), htmlOut);
            string result = type.ValueTypeCode;
            string name;
            object child;
			DumpItem dumpedChild;
			if (htmlOut) result += "<div class=\"item dump dump-" + obj.GetHashCode().ToString() + "\">";
			int i = 0;
			foreach (PropertyDescriptor descriptor in objProperties) {
                name = descriptor.Name;
                try {
                    child = descriptor.GetValue(obj);
                } catch (Exception e) {
                    child = e;
                }
				child = child == null ? "null" : child;
				dumpedChild = Dumper._dump(child, level + 1, htmlOut, new List<int>(ids), maxDepth);
				if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + Dumper._tabsIndent(level + 1, htmlOut)
                        + "<span class=\"field\">" + name + "</span>:&nbsp;" 
                        + dumpedChild.Content;
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut) + name + ": " + dumpedChild.Content;
                }
				i++;
			}
			if (htmlOut) result += "</div>";
			return new DumpItem {
				Complex = true,
				Content = result
			};
		}
        private static DumpItem _dumpUnknownTyped (object obj, int level = 0, bool htmlOut = true, List<int> ids = null, int maxDepth = 0) {
            Type objType = obj.GetType();
			int flagsLength = 0;
			int namesLength = 0;
			Dictionary<string, object[]> items = new Dictionary<string, object[]>();
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, objType, BindingFlags.Static | BindingFlags.Public);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, objType, BindingFlags.Instance | BindingFlags.Public);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, objType, BindingFlags.Static | BindingFlags.Public);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, objType, BindingFlags.Instance | BindingFlags.Public);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, objType, BindingFlags.Static | BindingFlags.NonPublic);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, objType, BindingFlags.Instance | BindingFlags.NonPublic);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, objType, BindingFlags.Static | BindingFlags.NonPublic);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, objType, BindingFlags.Instance | BindingFlags.NonPublic);
			// for static properties and static fields only defined in parent classes,
			// search through in parent classes, because they are not automaticly returned through Type reflection
			Type objectType = typeof(Object);
			Type currentType = objType;
			while (true) {
				currentType = currentType.BaseType;
				if (currentType == null || currentType.Equals(objectType)) break;
				Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, currentType, BindingFlags.Static | BindingFlags.Public);
				Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, currentType, BindingFlags.Static | BindingFlags.Public);
				Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, currentType, BindingFlags.Static | BindingFlags.NonPublic);
				Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, currentType, BindingFlags.Static | BindingFlags.NonPublic);
			}
            DumpType type = Dumper.GetDumpTypes(obj, items.Count.ToString(), htmlOut, true);
            string flags;
			string name;
			object child;
			string cssClass;
			DumpItem dumpedChild;
            string result = type.ValueTypeCode;
			if (htmlOut) result += "<div class=\"item dump dump-" + obj.GetHashCode().ToString() + "\">";
			int i = 0;
            foreach (var item in items) {
                name = item.Key;
                if (!htmlOut) name += Tools.SpaceIndent(namesLength - item.Key.Length, htmlOut);
                flags = item.Value[0].ToString();
                if (!htmlOut) flags = "[" + flags + "]" + Tools.SpaceIndent(flagsLength - flags.Length - 3, htmlOut);
                child = item.Value[1];
				cssClass = flags.Replace(",", " ");
				child = child == null ? "null" : child;
				dumpedChild = Dumper._dump(child, level + 1, htmlOut, new List<int>(ids), maxDepth);
				if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + Dumper._tabsIndent(level + 1, htmlOut)
						+ "<span class=\"" + cssClass + "\" title=\"" + flags + "\">" + name + "</span>:&nbsp;" 
                        + dumpedChild.Content;
                } else {
					result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut) + flags + " " + name + ": " + dumpedChild.Content;
				}
				i++;
			}
			if (htmlOut) result += "</div>";
			return new DumpItem {
				Complex = true,
				Content = result
			};
		}
        private static string _getClickableClassIfNecessary (ref object child, ref string renderedChild) {
            string clickableClass = "";
            if (renderedChild.IndexOf("<span class=\"dump dump-") == 0) {
                clickableClass = "click click-" + child.GetHashCode().ToString() + " ";
            }
            return clickableClass;
        }
        private static void _getUnknownTypedProperties (Dictionary<string, object[]> items, ref int flagsLength, ref int namesLength, object obj, Type objType, BindingFlags bindingFlags) {
			Dictionary<string, object[]> newItems = new Dictionary<string, object[]>();
			List<string> flags = new List<string>();
			if (bindingFlags.HasFlag(BindingFlags.Public)) flags.Add("public");
			if (bindingFlags.HasFlag(BindingFlags.NonPublic)) flags.Add("nonpublic");
			if (bindingFlags.HasFlag(BindingFlags.Static)) flags.Add("static");
			flags.Add("property");
			string flag = String.Join(",", flags);
			IEnumerable<PropertyInfo> props = objType.GetProperties(bindingFlags);
			object child;
			foreach (PropertyInfo prop in props) {
				if (prop.CanRead) {
					try{
                        child = prop.GetValue(obj, null);
					} catch (Exception e) {
						child = "Exception: " + e.Message;
					}
				} else {
					child = "Unable to read property.";
				}
				if (!items.ContainsKey(prop.Name) && !newItems.ContainsKey(prop.Name)) {
					newItems.Add(prop.Name, new object[] { flag, child });
				}
			}
			IOrderedEnumerable<KeyValuePair<string, object[]>> sorter = newItems.OrderBy(key => key.Key);
			newItems = sorter.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);
			foreach (var item in newItems) {
				if (item.Key.Length > namesLength) namesLength = item.Key.Length;
				if (flag.Length > flagsLength) flagsLength = flag.Length;
				items.Add(item.Key, item.Value);
			}
		}
		private static void _getUnknownTypedFields (Dictionary<string, object[]> items, ref int flagsLength, ref int namesLength, object obj, Type objType, BindingFlags bindingFlags) {
			Dictionary<string, object[]> newItems = new Dictionary<string, object[]>();
			List<string> flags = new List<string>();
			if (bindingFlags.HasFlag(BindingFlags.Public)) flags.Add("public");
			if (bindingFlags.HasFlag(BindingFlags.NonPublic)) flags.Add("nonpublic");
			if (bindingFlags.HasFlag(BindingFlags.Static)) flags.Add("static");
			flags.Add("field");
			string flag = String.Join(",", flags);
			string flag2;
			IEnumerable<FieldInfo> fields = objType.GetFields(bindingFlags);
			object child;
			foreach (FieldInfo field in fields) {
				if (field.IsPrivate && !bindingFlags.HasFlag(BindingFlags.Public)) {
					flag2 = flag.Replace("nonpublic", "private");
				} else {
					flag2 = flag.Replace("nonpublic", "protected");
				}
				try {
					child = field.GetValue(obj);
				} catch (Exception e) {
					child = "Exception: " + e.Message;
				}
				if (!items.ContainsKey(field.Name) && !newItems.ContainsKey(field.Name)) {
					if (field.Name.Length > namesLength) namesLength = field.Name.Length;
					newItems.Add(field.Name, new object[] { flag2, child });
				}
			}
			IOrderedEnumerable<KeyValuePair<string, object[]>> sorter = newItems.OrderBy(key => key.Key);
			newItems = sorter.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);
			foreach (var item in newItems) {
				flag = item.Value[0].ToString();
				if (flag.Length > flagsLength) flagsLength = flag.Length;
				items.Add(item.Key, item.Value);
			}
		}
		private static dynamic _getSimpleTypeArray (object obj) {
            if (obj is sbyte[]) return new { array = (sbyte[])obj, length = ((sbyte[])obj).Length };
            if (obj is byte[]) return new { array = (byte[])obj, length = ((byte[])obj).Length };
            if (obj is short[]) return new { array = (short[])obj, length = ((short[])obj).Length };
            if (obj is ushort[]) return new { array = (ushort[])obj, length = ((ushort[])obj).Length };
            if (obj is int[]) return new { array = (int[])obj, length = ((int[])obj).Length };
            if (obj is uint[]) return new { array = (uint[])obj, length = ((uint[])obj).Length };
            if (obj is long[]) return new { array = (long[])obj, length = ((long[])obj).Length };
            if (obj is ulong[]) return new { array = (ulong[])obj, length = ((ulong[])obj).Length };
            if (obj is float[]) return new { array = (float[])obj, length = ((float[])obj).Length };
            if (obj is double[]) return new { array = (double[])obj, length = ((double[])obj).Length };
            if (obj is decimal[]) return new { array = (decimal[])obj, length = ((decimal[])obj).Length };
            if (obj is char[]) return new { array = (char[])obj, length = ((char[])obj).Length };
            if (obj is string[]) return new { array = (string[])obj, length = ((string[])obj).Length };
            if (obj is bool[]) return new { array = (bool[])obj, length = ((bool[])obj).Length };
            if (obj is object[]) return new { array = (object[])obj, length = ((object[])obj).Length };
            return new { array = new object[] { }, length = 0 };
        }
        private static string _tabsIndent (int tabs = 0, bool htmlOut = true) {
			string s = "";
			if (htmlOut) {
				s = "<s style=\"width:" + (27*tabs) + "px\"></s>";
			} else {
				for (var i = 0; i < tabs; i++) s += "   ";
			}
			return s;
		}
	}
}
