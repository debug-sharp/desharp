using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using Desharp.Core;
using Desharp.Completers;

namespace Desharp.Renderers {
    public class Dumper {
        public static string Dump (object obj, int level = 0, bool htmlOut = false, List<int> ids = null) {
			if (ids == null) ids = new List<int>();
			if (obj != null) {
				int objId = obj.GetHashCode();
				if (ids.Contains(objId)) {
					return "{*** RECURSION ***}";
				} else {
					ids.Add(objId);
				}
			}
			string result = "";
            if (htmlOut && level == 0) {
                result += "<div>";
            }
            if (Detector.IsTypeObject(obj)) {
                result += Dumper._dumpTypeObject(obj as Type, level, htmlOut);
            } else if (Detector.IsEnum(obj)) {
				result += Dumper._dumpEnum(obj, level, htmlOut);
			} else if (Detector.IsSimpleType(obj)) {
                result += Dumper._dumpSimpleType(obj, level, htmlOut);
            } else if (Detector.IsSimpleArray(obj)) {
                result += Dumper._dumpSimpleArray(obj, level, htmlOut, ids);
            } else if (Detector.IsDictionary(obj)) {
                result += Dumper._dumpDictionary(obj, level, htmlOut, ids);
            } else if (Detector.IsDictionaryInnerCollection(obj)) {
                result += Dumper._dumpDictionaryInnerCollection(obj, level, htmlOut, ids);
            } else if (Detector.IsEnumerable(obj)) {
                result += Dumper._dumpEnumerable(obj, level, htmlOut, ids);
            } else if (Detector.IsDbResult(obj)) {
                result += Dumper._dumpDbResult(obj, level, htmlOut, ids);
            } else {
                result += Dumper._dumpUnknown(obj, level, htmlOut, ids);
            }
            if (htmlOut && level == 0) {
                result += "</div>";
            }
            return result;
		}
		internal static DumpType GetDumpTypes (object obj, string length = "", bool htmlOut = false, bool fullTypeName = false) {
			string typeStr = "";
			string html = "";
			if (obj == null) {
				typeStr = "null";
			} else {
				Type type = obj.GetType() as Type;
				if (type == null) {
					typeStr = "null";
				} else {
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
			if (htmlOut) {
				html = "<span class=\"type\">[" + typeStr + "]</span>";
			} else {
				html = "[" + typeStr + "]";
			}
			if (length.Length > 0) {
				if (htmlOut) {
					html = "<span class=\"type\">[" + typeStr + "(" + length + ")]</span>";
				} else {
					html = "[" + typeStr + "(" + length + ")]";
				}
			}
			if (htmlOut) {
				return new DumpType {
					Text = typeStr.ToLower(),
					Html = html
				};
			} else {
				return new DumpType {
					Text = typeStr,
					Html = html
				};
			}
		}
		private static string _dumpEnum (object obj, int level, bool htmlOut) {
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
			return String.Join(", ", result.ToArray()) + " " + type.Html;
		}
		private static string _dumpTypeObject (Type obj, int level = 0, bool htmlOut = true) {
            string htmlValue = obj == null ? "null" : obj.FullName;
            DumpType type = Dumper.GetDumpTypes(obj, "", htmlOut);
            if (htmlOut) return "<span class=\"" + type.Text + "\">" + htmlValue + "</span>&nbsp;" + type.Html;
            return htmlValue + " [" + type.Text + "]";
        }
        private static string _dumpSimpleType (object obj, int level = 0, bool htmlOut = true) {
            string htmlValue = obj == null ? "null" : obj.ToString();
            DumpType type = (obj is string) ? Dumper.GetDumpTypes(obj, obj.ToString().Length.ToString(), htmlOut) : Dumper.GetDumpTypes(obj, "", htmlOut);
            if (htmlOut) return "<span class=\"" + type.Text + "\">" + htmlValue + "</span>&nbsp;" + type.Html;
            return htmlValue + " [" + type.Text + "]";
        }
        private static string _dumpSimpleArray (object obj, int level = 0, bool htmlOut = true, List<int> ids = null) {
            dynamic simpleArray = Dumper._getSimpleTypeArray(obj);
            DumpType type = Dumper.GetDumpTypes(obj, simpleArray.length.ToString(), htmlOut);
            string result = type.Html;
            for (int i = 0, l = simpleArray.length; i < l; i += 1) {
                if (htmlOut) {
                    result += "<br />" + Dumper._tabsIndent(level + 1, htmlOut)
                        + "<span class=\"int\">" + i.ToString() + "</span>:&nbsp;"
                        + Dumper.Dump(simpleArray.array[i], level + 1, htmlOut, new List<int>(ids));
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                        + i.ToString() + ": "
                        + Dumper.Dump(simpleArray.array[i], level + 1, htmlOut, new List<int>(ids));
                }
            }
            return result;
        }
        private static string _dumpDictionary (object obj, int level = 0, bool htmlOut = false, List<int> ids = null) {
            dynamic objDct = (dynamic)obj;
            DumpType type = Dumper.GetDumpTypes(obj, objDct.Count.ToString(), htmlOut);
            string result = type.Html;
            foreach (dynamic item in objDct) {
                object child = item.Value;
                string keyStr = item.Key.ToString();
                DumpType subTypeKey = Dumper.GetDumpTypes(item.Key, "", htmlOut);
                DumpType subTypeValue = Dumper.GetDumpTypes(child, "", htmlOut);
				child = child == null ? "null" : child;
				if (htmlOut) {
                    result += "<br />" + Dumper._tabsIndent(level + 1, htmlOut)
                        + "<span class=\"" + subTypeKey.Text + "\">" + keyStr + "</span>"
                        + ":&nbsp;" + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                        + keyStr + ": " + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
                }
            }
            return result;
        }
        private static string _dumpDictionaryInnerCollection (object obj, int level = 0, bool htmlOut = false, List<int> ids = null) {
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
            result += type.Html;
            for (int i = 0; i < length; i++) {
                object child = objList[i];
                string keyStr = i.ToString();
				child = child == null ? "null" : child;
				if (htmlOut) {
                    result += "<br />" + Dumper._tabsIndent(level + 1, htmlOut)
                        + "<span class=\"property\">" + keyStr + "</span>"
                        + ":&nbsp;" + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                        + keyStr + ": " + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
                }
            }
            return result;
        }
        private static string _dumpEnumerable (object obj, int level = 0, bool htmlOut = false, List<int> ids = null) {
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
            result += type.Html;
            for (int i = 0; i < length; i++) {
                object child = objEnum[i];
                string keyStr = i.ToString();
				child = child == null ? "null" : child;
				if (htmlOut) {
                    result += "<br />" + Dumper._tabsIndent(level + 1, htmlOut)
                    + "<span class=\"property\">" + keyStr + "</span>"
                    + ":&nbsp;" + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                    + keyStr + ": " + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
                }
            }
            return result;
        }
        private static string _dumpDbResult (object obj, int level = 0, bool htmlOut = false, List<int> ids = null) {
            string result = "";
            DumpType type;
            int length = 0;
            if (obj is DataSet) {
                DataSet ds = obj as DataSet;
                length = ds.Tables.Count;
                type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut);
                result += type.Html;
                for (int i = 0; i < length; i++) {
                    DataTable table = ds.Tables[i];
                    if (htmlOut) {
                        result += "<br />" + Dumper._tabsIndent(level + 1, htmlOut)
                            + "<span class=\"table\">" + table.TableName + "</span>:&nbsp;" + Dumper.Dump(table, level + 1, htmlOut, new List<int>(ids));
                    } else {
                        result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                            + table.TableName + ": " + Dumper.Dump(table, level + 1, htmlOut, new List<int>(ids));
                    }
                }
            } else if (obj is DataTable) {
                DataTable ds = obj as DataTable;
                DataRowCollection rows = ds.Rows;
                length = rows.Count;
                type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut);
                result += type.Html;
                for (int i = 0; i < length; i++) {
                    if (htmlOut) {
                        result += "<br />" + Dumper._tabsIndent(level + 1, htmlOut)
                            + "<span class=\"int\">" + i.ToString() + "</span>:&nbsp;" + Dumper.Dump(rows[i], level + 1, htmlOut, new List<int>(ids));
                    } else {
                        result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                            + i.ToString() + ": " + Dumper.Dump(rows[i], level + 1, htmlOut, new List<int>(ids));
                    }
                }
            } else if (obj is DataRow) {
                DataRow row = obj as DataRow;
                DataColumnCollection columns = row.Table.Columns;
                length = columns.Count;
                type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut);
                result += type.Html;
                foreach (DataColumn column in columns) {
                    DumpType subTypeValue = Dumper.GetDumpTypes(row[column], "", htmlOut);
                    string subTypeCls = subTypeValue.Text.ToString().ToLower();
                    string val = subTypeCls == "dbnull" ? "DBNull" : row[column].ToString();
                    if (htmlOut) {
                        result += "<br />" + Dumper._tabsIndent(level + 1, htmlOut)
                        + "<span class=\"column\">" + column.ToString() + "</span>:&nbsp;"
                        + "<span class=\"" + subTypeCls + "\">" + val + "</span>&nbsp;" + subTypeValue.Html;
                    } else {
                        result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                        + column.ToString() + ": "
                        + val + " " + subTypeValue.Html;
                    }

                }
            }
            return result;
        }
        private static string _dumpUnknown (object obj, int level = 0, bool htmlOut = true, List<int> ids = null) {
			if (obj == null) {
				return Dumper._dumpUnknownNotTyped(obj, level, htmlOut, ids);
			} else {
				Type objType = obj.GetType();
				if (objType == null) {
					return Dumper._dumpUnknownNotTyped(obj, level, htmlOut, ids);
				} else {
					return Dumper._dumpUnknownTyped(obj, level, htmlOut, ids);
				}
			}
        }
        private static string _dumpUnknownNotTyped (object obj, int level = 0, bool htmlOut = true, List<int> ids = null) {
            DumpType type = Dumper.GetDumpTypes(obj, "", htmlOut);
            string result = type.Html;
            PropertyDescriptorCollection objProperties = TypeDescriptor.GetProperties(obj);
            foreach (PropertyDescriptor descriptor in objProperties) {
				string name = descriptor.Name;
                object child;
                try {
                    child = descriptor.GetValue(obj);
                } catch (Exception e) {
                    child = e;
                }
				child = child == null ? "null" : child;
				if (htmlOut) {
                    result += "<br />" + Dumper._tabsIndent(level + 1, htmlOut)
                    + "<span class=\"property\">" + name + "</span>:&nbsp;" + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
                } else {
                    result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
                    + name + ": " + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
                }
            }
            return result;
        }
        private static string _dumpUnknownTyped (object obj, int level = 0, bool htmlOut = true, List<int> ids = null) {
            DumpType type = Dumper.GetDumpTypes(obj, "", htmlOut, true);
            string result = type.Html;
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
			string flags;
			string name;
			object child;
			foreach (var item in items) {
				name = item.Key + Tools.SpaceIndent(namesLength - item.Key.Length, htmlOut);
				flags = item.Value[0].ToString();
				flags = "[" + flags + "]" + Tools.SpaceIndent(flagsLength - flags.Length - 3, htmlOut);
				child = item.Value[1];
				child = child == null ? "null" : child;
				if (htmlOut) {
					result += "<br />" + Dumper._tabsIndent(level + 1, htmlOut)
						+ "<span class=\"property\">" + flags + " " + name + "</span>:&nbsp;" + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
				} else {
					result += "\r\n" + Dumper._tabsIndent(level + 1, htmlOut)
						+ flags + " " + name + ": " + Dumper.Dump(child, level + 1, htmlOut, new List<int>(ids));
				}
			}
            return result;
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
					try {
						child = prop.GetValue(obj);
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
			IEnumerable<FieldInfo> fields = objType.GetFields(bindingFlags);
			object child;
			foreach (FieldInfo field in fields) {
				if (field.IsPrivate && !bindingFlags.HasFlag(BindingFlags.Public)) {
					flag = flag.Replace("nonpublic", "private");
				} else {
					flag = flag.Replace("nonpublic", "protected");
				}
				try {
					child = field.GetValue(obj);
				} catch (Exception e) {
					child = "Exception: " + e.Message;
				}
				if (!items.ContainsKey(field.Name) && !newItems.ContainsKey(field.Name)) {
					if (field.Name.Length > namesLength) namesLength = field.Name.Length;
					newItems.Add(field.Name, new object[] { flag, child });
				}
			}
			IOrderedEnumerable<KeyValuePair<string, object[]>> sorter = newItems.OrderBy(key => key.Key);
			newItems = sorter.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);
			foreach (var item in newItems) {
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
            for (var i = 0; i < tabs; i++) {
                s += htmlOut ? "&nbsp;&nbsp;&nbsp;" : "   ";
            }
            return s;
        }
	}
}
