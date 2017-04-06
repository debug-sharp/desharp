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
using System.Collections.Specialized;
using System.Text;

namespace Desharp.Renderers {
	/// <summary>
	/// Reflection class to dump any type value into string representation by Desharp.Renderers.Dumper.Dump(value);
	/// </summary>
	public class Dumper {
		internal static string[] HtmlDumpWrapper = new string[] { @"<div class=""desharp-dump"">", "</div>" };
		internal static string[] TooLongIndicator = new string[] { @"<span class=""too-deep"">...</span>", "..." };
		/// <summary>
		/// Dump any type value into string representation and returns itg, this is direct dumper, no Desharp configuration will be used to process this dump.
		/// </summary>
		/// <param name="obj">Any type value to dump into string</param>
		/// <param name="htmlOut">True to dump values as HTML, false to dump as TEXT.</param>
		/// <param name="maxDepth">How many levels at maximum in complex type variables will be iterated throw to dump all it's properties, fields and other values.</param>
		/// <param name="maxLength">If any dumped string length is larger than this value, it will be cutted into this max length.</param>
		/// <returns>Returns HTML or TEXT representation of any called value.</returns>
		public static string Dump (object obj, bool htmlOut = false, int maxDepth = 0, int maxLength = 0) {
			if (maxDepth == 0) maxDepth = Dispatcher.DumpDepth;
			if (maxLength == 0) maxLength = Dispatcher.DumpMaxLength;
			string newDumpSequence = Dispatcher.GetCurrent().DumperSequence.ToString();
			Dispatcher.GetCurrent().DumperSequence += 1;
			StringBuilder result = new StringBuilder();
			if (htmlOut) result.Append(Dumper.HtmlDumpWrapper[0]);
			result.Append(Dumper._dumpRecursive(obj, htmlOut, maxDepth, maxLength, 0, new List<int>(), newDumpSequence));
			result.Append(htmlOut ? Dumper.HtmlDumpWrapper[1] : System.Environment.NewLine);
			return result.ToString();
		}
		private static string _dumpRecursive (object obj, bool htmlOut, int maxDepth, int maxLength, int level, List<int> ids, string sequence) {
			if (obj == null) {
				return Dumper._getNullCode(htmlOut);
			} else {
				int objId = obj.GetHashCode();
				if (ids.Contains(objId)) {
					// detected recursion
					return htmlOut ? @"<span class=""recursion click click-" + sequence + objId.ToString() + @""">{*** RECURSION ***}</span>" : "{*** RECURSION ***}";
				} else {
					ids.Add(objId);
				}
			}
			if (level == maxDepth) return Dumper._dumpRecursiveHandleLastLevelDepth(obj, htmlOut, maxLength, level, sequence);
			string result;
			if (Detector.IsPrimitiveType(obj)) {
				result = Dumper.DumpPrimitiveType(obj, level, htmlOut, maxLength, sequence);
			} else if (Detector.IsArray(obj)) {
				result = Dumper._dumpArray(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsNameValueCollection(obj)) {
				result = Dumper._dumpNameValueCollection(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsDictionary(obj)) {
				result = Dumper._dumpDictionary(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsDbResult(obj)) {
				result = Dumper._dumpDbResult(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsEnum(obj)) {
				result = Dumper._dumpEnum(obj, level, htmlOut, sequence);
			} else if (Detector.IsTypeObject(obj)) {
				result = Dumper._dumpTypeObject(obj as Type, level, htmlOut, sequence);
			} else if (Detector.IsEnumerable(obj)) {
				result = Dumper._dumpEnumerable(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsCollection(obj)) {
				result = Dumper._dumpCollection(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (!Detector.IsReflectionObject(obj) && !Detector.IsReflectionObjectArray(obj)) {
				result = Dumper._dumpUnknown(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else {
				result = obj.ToString();
			}
			return result;
		}
		internal static DumpType GetDumpTypes (object obj, string length, bool htmlOut, bool fullTypeName, string sequence) {
			string typeStr = "";
			string typeName = "";
			string typeFullName = "";
			string valueTypeHtml = "";
			string nameTypeClass = "";
			string valueTypeClickClass = "";
			string[] lowerAndbiggerThenChars = htmlOut ? new string[] { "&lt;", "&gt;" } : new string[] { "<", ">" };
			int backSingleQuotPos = 0;
			if (obj == null) {
				typeStr = "null";
			} else {
				int lengthInt = length.Length > 0 ? Int32.Parse(length) : 0;
				valueTypeClickClass = (length.Length > 0 && lengthInt > 0 && !(obj is string)) ? "click click-" + sequence + obj.GetHashCode().ToString() : "";
				Type type = obj.GetType() as Type;
				if (type != null) {
					Type[] gta = type.GetGenericArguments();
					typeName = type.Name;
					typeFullName = type.FullName;
					typeStr = fullTypeName ? typeFullName : typeName;
					if (gta.Length > 0) {
						backSingleQuotPos = typeStr.IndexOf('`');
						if (backSingleQuotPos > -1) {
							typeStr = typeStr.Substring(0, backSingleQuotPos);
							List<string> gtaStrs = new List<string>();
							for (int i = 0, l = gta.Length; i < l; i++) {
								gtaStrs.Add(gta[i].Name.ToString());
							}
							typeStr = typeStr + lowerAndbiggerThenChars[0] + String.Join(",", gtaStrs.ToArray()) + lowerAndbiggerThenChars[1];
							backSingleQuotPos = typeName.IndexOf('`');
							if (backSingleQuotPos > -1) typeName = typeName.Substring(0, backSingleQuotPos);
							if (typeStr.IndexOf(typeName) == -1) {
								backSingleQuotPos = typeName.IndexOf('`');
								if (backSingleQuotPos > -1) typeName = typeName.Substring(0, backSingleQuotPos);
								typeStr += "." + typeName;
							}
						}
					}
				}
			}
			if (typeStr.IndexOf("<>f__AnonymousType2") > -1) { // mostly simple base object with key/value passed into Dump method
				typeStr = "Object" + typeStr.Substring(19);
			}
			nameTypeClass = typeStr.ToLower();
			if (length.Length > 0) {
				int lastArrCharsPos = typeStr.LastIndexOf("[]");
				if (lastArrCharsPos == typeStr.Length - 2) {
					int startIndex = lastArrCharsPos; // typeStr.Length - 2
					int arrCharsPos;
					int safeCounter = 0;
					while (true && safeCounter < 50) {
						arrCharsPos = typeStr.IndexOf("[]", startIndex - 2);
						if (arrCharsPos > -1 && arrCharsPos < lastArrCharsPos) {
							lastArrCharsPos = arrCharsPos;
							startIndex -= 2;
						} else {
							break;
						}
						safeCounter++;
					}
					typeStr = typeStr.Substring(0, lastArrCharsPos) + "[" + length + "]" + typeStr.Substring(lastArrCharsPos + 2);
				} else if (obj != null) {
					typeStr = typeStr + "(" + length + ")";
				}
			}
			if (htmlOut) {
				valueTypeHtml = @"<span class=""type " + valueTypeClickClass + @""">[" + typeStr + "]</span>";
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
					NameCssClass = nameTypeClass
				};
			}
		}
		private static string _dumpRecursiveHandleLastLevelDepth (object obj, bool htmlOut, int maxLength, int level, string sequence) {
			// in last level - print out only single line prints, complex object only as: ... [Type]
			if (Detector.IsPrimitiveType(obj)) {
				return Dumper.DumpPrimitiveType(obj, level, htmlOut, maxLength, sequence);
			} else if (Detector.IsEnum(obj)) {
				return Dumper._dumpEnum(obj, level, htmlOut, sequence);
			} else if (Detector.IsTypeObject(obj)) {
				return Dumper._dumpTypeObject(obj as Type, level, htmlOut, sequence);
			} else {
				DumpType type = Dumper.GetDumpTypes(obj, "", htmlOut, true, sequence);
				return (htmlOut ? @"<span class=""too-deep"">...</span>" : "...") + type.ValueTypeCode;
			}
		}
		private static string _dumpEnum (object obj, int level, bool htmlOut, string sequence) {
			DumpType type = Dumper.GetDumpTypes(obj, "", htmlOut, true, sequence);
			List<string> resultItems = new List<string>();
			string result;
			Array objPossibleValues = Enum.GetValues(obj.GetType());
			object objPossibleValue;
			for (int i = 0; i < objPossibleValues.Length; i++) {
				objPossibleValue = objPossibleValues.GetValue(i);
				if (obj.Equals(objPossibleValue)) {
					resultItems.Add(objPossibleValue.ToString());
				}
			}
			result = String.Join(", ", resultItems.ToArray());
			if (htmlOut) {
				result = @"<span class=""enum"">" + result + "</span>&nbsp;" + type.ValueTypeCode;
			} else {
				result = result + " [" + type.Text + "]";
			}
			return result;
		}
		private static string _dumpTypeObject (Type obj, int level, bool htmlOut, string sequence) {
			string renderedValue = obj == null ? Dumper._getNullCode(htmlOut) : obj.FullName;
			DumpType type = Dumper.GetDumpTypes(obj, "", htmlOut, false, sequence);
			string result = "";
			if (htmlOut) {
				result = @"<span class=""" + type.Text + @""">" + renderedValue + "</span>&nbsp;" + type.ValueTypeCode;
			} else {
				result = renderedValue + " [" + type.Text + "]";
			}
			return result;
		}
		internal static string DumpPrimitiveType (object obj, int level, bool htmlOut, int maxLength, string sequence) {
			string renderedValue = "";
			if (obj == null) {
				renderedValue = Dumper._getNullCode(htmlOut);
			} else {
				renderedValue = obj.ToString();
				if (obj is char) {
					if (htmlOut) {
						renderedValue = "'" + Tools.HtmlEntities(renderedValue) + "'";
					} else {
						renderedValue = "'" + renderedValue + "'";
					}
				} else if (obj is string) {
					bool tooLong = false;
					if (renderedValue.Length > maxLength) {
						tooLong = true;
						renderedValue = renderedValue.Substring(0, maxLength) + "...";
					}
					if (htmlOut) {
						renderedValue = @"""" + Tools.HtmlEntities(renderedValue) + @"""";
						if (tooLong) renderedValue = renderedValue.Substring(0, renderedValue.Length - 4) + @"<span class=""too-deep"">...</span>""";
					} else {
						renderedValue = @"""" + renderedValue + @"""";
					}
				} else if (obj is bool) {
					renderedValue = renderedValue.ToLower();
				} else if (obj is double || obj is float || obj is decimal) {
					renderedValue = renderedValue.Replace(',', '.'); // Microsoft .NET environment ToString() translates double/float/decimal into shit form with comma
				} else if (obj is byte || obj is sbyte) {
					byte byteAbs;
					string sign = " ";
					if (obj is sbyte && (sbyte)obj < 0) {
						sbyte objSbyte = (sbyte)obj;
						int objSbyteInt = Math.Abs((int)objSbyte);
						byteAbs = (byte)objSbyteInt;
						sign = "-";
					} else {
						byteAbs = (byte)obj;
					}
					byte[] byteArr = new byte[] { byteAbs };
					string dec = Convert.ToInt64(byteAbs).ToString();
					string hex = BitConverter.ToString(byteArr);
					string str = System.Text.Encoding.ASCII.GetString(byteArr);
					if (htmlOut) str = Tools.HtmlEntities(str);
					renderedValue = "HEX:" + sign + hex + ", ASCII:" + sign + "'" + str + "', DEC:" + sign + dec;
				} 
			}
            DumpType type = (obj is string) 
				? Dumper.GetDumpTypes(obj, obj.ToString().Length.ToString(), htmlOut, false, sequence) 
				: Dumper.GetDumpTypes(obj, "", htmlOut, false, sequence);
            string result = "";
            if (htmlOut) {
                result = @"<span class=""" + type.NameCssClass + @""">" + renderedValue + "</span>&nbsp;" + type.ValueTypeCode;
            } else {
                result = renderedValue + " [" + type.Text + "]";
            }
            return result;
        }
		private static string _dumpNameValueCollection (object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			NameValueCollection objCol = (NameValueCollection)obj;
			string[] objColKeys = objCol.AllKeys;
			DumpType type = Dumper.GetDumpTypes(obj, objColKeys.Length.ToString(), htmlOut, true, sequence);
			string result = type.ValueTypeCode;
			string dumpedChild;
			object key;
			string keyStr;
			int maxKeyLength = 0;
			object child;
			DumpType subTypeKey;
			DumpType subTypeValue;
			string tabsStr = Dumper._tabsIndent(level + 1, htmlOut);
			if (htmlOut) {
				result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
				for (int i = 0, l = objColKeys.Length; i < l; i += 1) {
					key = objColKeys[i];
					keyStr = key.ToString();
					child = objCol.Get(keyStr);
					subTypeKey = Dumper.GetDumpTypes(key, "", htmlOut, false, sequence);
					subTypeValue = Dumper.GetDumpTypes(child, "", htmlOut, false, sequence);
					dumpedChild = Dumper._dumpRecursive(child, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					if (subTypeKey.NameCssClass == "char") keyStr = "'" + keyStr + "'";
					if (subTypeKey.NameCssClass == "string") keyStr = @"""" + keyStr + @"""";
					result += (i > 0 ? "<br />" : "") + tabsStr
						+ @"<span class=""" + subTypeKey.NameCssClass + @""">" + keyStr + "</span>"
						+ "<s>:&nbsp;</s>" + dumpedChild;
				}
				result += "</div>";
			} else {
				List<string[]> dumpedItems = new List<string[]>();
				for (int i = 0, l = objColKeys.Length; i < l; i += 1) {
					key = objColKeys[i];
					keyStr = key.ToString();
					child = objCol.Get(keyStr);
					subTypeKey = Dumper.GetDumpTypes(key, "", htmlOut, false, sequence);
					subTypeValue = Dumper.GetDumpTypes(child, "", htmlOut, false, sequence);
					dumpedChild = Dumper._dumpRecursive(child, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					if (subTypeKey.NameCssClass == "char") keyStr = "'" + keyStr + "'";
					if (subTypeKey.NameCssClass == "string") keyStr = @"""" + keyStr + @"""";
					dumpedItems.Add(new string[] { keyStr, dumpedChild });
					if (keyStr.Length > maxKeyLength) maxKeyLength = keyStr.Length;
				}
				foreach (string[] dumpedItem in dumpedItems) {
					result += System.Environment.NewLine + tabsStr + dumpedItem[0] 
						+ Tools.SpaceIndent(maxKeyLength - dumpedItem[0].Length, false) + ": " + dumpedItem[1];
				}
			}
			return result;
		}
		private static string _dumpCollection (object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			dynamic objCol = (ICollection)obj;
			IEnumerator objEnum = objCol.GetEnumerator();
			DumpType type = Dumper.GetDumpTypes(obj, objCol.Count.ToString(), htmlOut, true, sequence);
			string result = type.ValueTypeCode;
			string dumpedChild;
			string keyStr;
			int maxKeyLength = 0;
			DumpType subTypeKey;
			DumpType subTypeValue;
			string tabsStr = Dumper._tabsIndent(level + 1, htmlOut);
			int i = 0;
			if (htmlOut) {
				result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
				while (objEnum.MoveNext()) {
					dumpedChild = Dumper._dumpRecursive(objEnum.Current, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					subTypeKey = Dumper.GetDumpTypes(i, "", htmlOut, false, sequence);
					subTypeValue = Dumper.GetDumpTypes(objEnum.Current, "", htmlOut, false, sequence);
					keyStr = i.ToString();
					result += (i > 0 ? "<br />" : "")
						+ tabsStr + @"<span class=""" + subTypeKey.NameCssClass + @""">" + keyStr + "</span>" 
						+ "<s>:&nbsp;</s>" + dumpedChild;
					i++;
				}
				result += "</div>";
			} else {
				List<string[]> dumpedItems = new List<string[]>();
				while (objEnum.MoveNext()) {
					dumpedChild = Dumper._dumpRecursive(objEnum.Current, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					subTypeKey = Dumper.GetDumpTypes(i, "", htmlOut, true, sequence);
					subTypeValue = Dumper.GetDumpTypes(objEnum.Current, "", htmlOut, true, sequence);
					keyStr = i.ToString();
					if (keyStr.Length > maxKeyLength) maxKeyLength = keyStr.Length;
					dumpedItems.Add(new string[] { keyStr, dumpedChild });
					i++;
				}
				foreach (string[] dumpedItem in dumpedItems) {
					result += System.Environment.NewLine
						+ tabsStr + dumpedItem[0] + Tools.SpaceIndent(maxKeyLength - tabsStr.Length, htmlOut) + ": " + dumpedItem[1];
				}
			}
			return result;
		}
		private static string _dumpArray (object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			dynamic simpleArray = Dumper._getSimpleTypeArray(obj);
			string simpleArrayLengthStr = simpleArray.Length.ToString();
			DumpType type = Dumper.GetDumpTypes(obj, simpleArrayLengthStr, htmlOut, false, sequence);
			string result = type.ValueTypeCode;
			string dumpedChild;
			object child;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper._tabsIndent(level + 1, htmlOut);
			int maxKeyDigits = simpleArrayLengthStr.Length;
			string key;
			for (int i = 0, l = simpleArray.Length; i < l; i += 1) {
				key = i.ToString();
				child = simpleArray.Data[i];
				dumpedChild = Dumper._dumpRecursive(child, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
				if (htmlOut) {
					result += (i > 0 ? "<br />" : "") + tabsStr
						+ @"<span class=""int"">" + key + "</span><s>:&nbsp;</s>"
						+ dumpedChild;
				} else {
					result += System.Environment.NewLine + tabsStr
						+ Tools.SpaceIndent(maxKeyDigits - key.Length, htmlOut) + key + ": "
						+ dumpedChild;
				}
			}
			if (htmlOut) result += "</div>";
			return result;
		}
		private static string _dumpDictionary (object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			// IDictionary objDct = (IDictionary)obj; // do not use this retype - causes exception for additional ditionaries
			Type objType = obj.GetType();
			PropertyInfo countPropInfo = objType.GetProperty("Count");
			PropertyInfo keysPropInfo = objType.GetProperty("Keys");
			PropertyInfo valuesPropInfo = objType.GetProperty("Values");
			int count = (int)countPropInfo.GetValue(obj, null);
			IEnumerable keys = keysPropInfo.GetValue(obj, null) as IEnumerable;
			IEnumerable values = valuesPropInfo.GetValue(obj, null) as IEnumerable;
			IEnumerator keysEnum = keys.GetEnumerator();
			IEnumerator valuesEnum = values.GetEnumerator();
			DumpType type = Dumper.GetDumpTypes(obj, count.ToString(), htmlOut, false, sequence);
			string result = type.ValueTypeCode;
			string dumpedChild;
			object key;
			string keyStr;
			string tabsStr = Dumper._tabsIndent(level + 1, htmlOut);
			DumpType subTypeKey;
			if (htmlOut) {
				result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
				int i = 0;
				while (keysEnum.MoveNext()) {
					key = keysEnum.Current;
					valuesEnum.MoveNext();
					keyStr = Detector.IsPrimitiveType(key) ? key.ToString() : Dumper._dumpRecursive(key, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					subTypeKey = Dumper.GetDumpTypes(key, "", htmlOut, false, sequence);
					dumpedChild = Dumper._dumpRecursive(valuesEnum.Current, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					if (subTypeKey.NameCssClass == "char") keyStr = "'" + Tools.HtmlEntities(keyStr) + "'";
					if (subTypeKey.NameCssClass == "string") keyStr = @"""" + Tools.HtmlEntities(keyStr) + @"""";
					result += (i++ > 0 ? "<br />" : "") + tabsStr
						+ @"<span class=""" + subTypeKey.NameCssClass + @""">" + keyStr + "</span>"
						+ "<s>:&nbsp;</s>" + dumpedChild;
				}
				result += "</div>";
			} else {
				List<string[]> dctItems = new List<string[]>();
				int allKeysMaxLength = 0;
				while (keysEnum.MoveNext()) {
					key = keysEnum.Current;
					valuesEnum.MoveNext();
					keyStr = Detector.IsPrimitiveType(key) ? key.ToString() : Dumper._dumpRecursive(key, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					subTypeKey = Dumper.GetDumpTypes(key, "", htmlOut, false, sequence);
					dumpedChild = Dumper._dumpRecursive(valuesEnum.Current, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					if (subTypeKey.NameCssClass == "char") keyStr = "'" + HttpUtility.JavaScriptStringEncode(keyStr) + "'";
					if (subTypeKey.NameCssClass == "string") keyStr = @"""" + HttpUtility.JavaScriptStringEncode(keyStr) + @"""";
					dctItems.Add(new string[] { keyStr, dumpedChild });
					if (keyStr.Length > allKeysMaxLength) allKeysMaxLength = keyStr.Length;
				}
				foreach (string[] dctItem in dctItems) {
					result += System.Environment.NewLine + tabsStr + dctItem[0] 
						+ Tools.SpaceIndent(allKeysMaxLength - dctItem[0].Length, htmlOut) + ": " + dctItem[1];
				}
			}
			return result;
        }
        private static string _dumpEnumerable (object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
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
            DumpType type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut, true, sequence);
            result += type.ValueTypeCode;
			string dumpedChild;
			object child;
            string keyStr;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper._tabsIndent(level + 1, htmlOut);
			for (int i = 0; i < length; i++) {
                child = objEnum[i];
                keyStr = i.ToString();
				dumpedChild = Dumper._dumpRecursive(child, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
				if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + tabsStr
					+ @"<span class=""int"">" + keyStr + "</span>"
                    + "<s>:&nbsp;</s>" + dumpedChild;
                } else {
                    result += System.Environment.NewLine + tabsStr + keyStr + ": " + dumpedChild;
                }
			}
			if (htmlOut) result += "</div>";
			return result;
		}
        private static string _dumpDbResult (object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
            string result = "";
            DumpType type;
            int length = 0;
			string dumpedChild;
            object child;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper._tabsIndent(level + 1, htmlOut);
			if (obj is DataSet) {
                DataSet ds = obj as DataSet;
                length = ds.Tables.Count;
                type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut, false, sequence);
                result += type.ValueTypeCode;
                for (int i = 0; i < length; i++) {
                    child = ds.Tables[i];
                    DataTable table = ds.Tables[i];
					dumpedChild = Dumper._dumpRecursive(child, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					if (htmlOut) {
                        result += (i > 0 ? "<br />" : "") + tabsStr
							+ @"<span class=""table"">" + table.TableName + "</span><s>:&nbsp;</s>"
							+ dumpedChild;
                    } else {
                        result += System.Environment.NewLine + tabsStr + table.TableName + ": " + dumpedChild;
                    }
				}
			} else if (obj is DataTable) {
                DataTable ds = obj as DataTable;
                DataRowCollection rows = ds.Rows;
                length = rows.Count;
                type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut, false, sequence);
                result += type.ValueTypeCode;
                for (int i = 0; i < length; i++) {
                    child = rows[i];
					dumpedChild = Dumper._dumpRecursive(child, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					if (htmlOut) {
                        result += (i > 0 ? "<br />" : "") + tabsStr
							+ @"<span class=""int"">" + i.ToString() + "</span><s>:&nbsp;</s>" 
                            + dumpedChild;
                    } else {
                        result += System.Environment.NewLine + tabsStr + i.ToString() + ": " + dumpedChild;
                    }
				}
			} else if (obj is DataRow) {
                DataRow row = obj as DataRow;
                DataColumnCollection columns = row.Table.Columns;
                length = columns.Count;
                type = Dumper.GetDumpTypes(obj, length.ToString(), htmlOut, false, sequence);
                result += type.ValueTypeCode;
				int i = 0;
                foreach (DataColumn column in columns) {
                    DumpType subTypeValue = Dumper.GetDumpTypes(row[column], "", htmlOut, false, sequence);
					string subTypeCls = subTypeValue.Text.ToString().ToLower();
                    string val = subTypeCls == "dbnull" ? "DBNull" : row[column].ToString();
					if (htmlOut) {
                        result += (i > 0 ? "<br />" : "") + tabsStr
						+ @"<span class=""column"">" + column.ToString() + "</span><s>:&nbsp;</s>"
                        + @"<span class=""" + subTypeCls + @""">" + val + "</span>&nbsp;" + subTypeValue.ValueTypeCode;
                    } else {
                        result += System.Environment.NewLine + tabsStr
						+ column.ToString() + ": "
                        + val + " " + subTypeValue.ValueTypeCode;
                    }
					i++;
                }
			}
			if (htmlOut) result += "</div>";
			return result;
		}
        private static string _dumpUnknown (object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			if (obj == null) {
				return Dumper._dumpUnknownNotTyped(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else {
				Type objType = obj.GetType();
				if (objType == null) {
					return Dumper._dumpUnknownNotTyped(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
				} else {
					return Dumper._dumpUnknownTyped(obj, level, htmlOut, ids, maxDepth, maxLength, sequence);
				}
			}
        }
        private static string _dumpUnknownNotTyped (object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
            PropertyDescriptorCollection objProperties = TypeDescriptor.GetProperties(obj);
            DumpType type = Dumper.GetDumpTypes(obj, objProperties.Count.ToString(), htmlOut, false, sequence);
            string result = type.ValueTypeCode;
            string name;
            object child;
			string dumpedChild;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper._tabsIndent(level + 1, htmlOut);
			int i = 0;
			foreach (PropertyDescriptor descriptor in objProperties) {
                name = descriptor.Name;
                try {
                    child = descriptor.GetValue(obj);
					dumpedChild = Dumper._dumpRecursive(child, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
				} catch (Exception e) {
					child = htmlOut ? @"<span class=""getexc"">" + e.Message + "</span>" : "Exception: " + e.Message;
					dumpedChild = child.ToString();
				}
				if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + tabsStr
						+ @"<span class=""field"">" + name + "</span><s>:&nbsp;</s>" 
                        + dumpedChild;
                } else {
                    result += System.Environment.NewLine + tabsStr + name + ": " + dumpedChild;
                }
				i++;
			}
			if (htmlOut) result += "</div>";
			return result;
		}
        private static string _dumpUnknownTyped (object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
            Type objType = obj.GetType();
			int flagsLength = 0;
			int namesLength = 0;
			Dictionary<string, object[]> items = new Dictionary<string, object[]>();
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, BindingFlags.Static | BindingFlags.Public);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, BindingFlags.Instance | BindingFlags.Public);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, BindingFlags.Static | BindingFlags.Public);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, BindingFlags.Instance | BindingFlags.Public);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, BindingFlags.Static | BindingFlags.NonPublic);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, BindingFlags.Instance | BindingFlags.NonPublic);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, BindingFlags.Static | BindingFlags.NonPublic);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, BindingFlags.Instance | BindingFlags.NonPublic);
			// for static properties and static fields only defined in parent classes,
			// search through in parent classes, because they are not automaticly returned through Type reflection
			Type objectType = typeof(Object);
			Type currentType = objType;
			int safeCounter = 0;
			while (true && safeCounter < 50) {
				currentType = currentType.BaseType;
				if (currentType == null || currentType.Equals(objectType)) break;
				Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, BindingFlags.Static | BindingFlags.Public);
				Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, BindingFlags.Static | BindingFlags.Public);
				Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, BindingFlags.Static | BindingFlags.NonPublic);
				Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, BindingFlags.Static | BindingFlags.NonPublic);
				safeCounter++;
			}
            DumpType type = Dumper.GetDumpTypes(obj, items.Count.ToString(), htmlOut, true, sequence);
            string flags;
			string name;
			string cssClass;
			string dumpedChild;
            string result = type.ValueTypeCode;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper._tabsIndent(level + 1, htmlOut);
			int i = 0;
            foreach (var item in items) {
                name = item.Key;
                if (!htmlOut) name += Tools.SpaceIndent(namesLength - item.Key.Length, htmlOut);
                flags = item.Value[0].ToString();
                if (!htmlOut) flags = "[" + flags + "]" + Tools.SpaceIndent(flagsLength - flags.Length - 3, htmlOut);
				dumpedChild = item.Value[1].ToString();
				cssClass = flags.Replace(",", " ");
				if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + tabsStr
						+ @"<span class=""" + cssClass + @""" title =""" + flags + @""">" + name + "</span><s>:&nbsp;</s>"
						+ dumpedChild;
                } else {
					result += System.Environment.NewLine + tabsStr + flags + " " + name + ": " + dumpedChild;
				}
				i++;
			}
			if (htmlOut) result += "</div>";
			return result;
		}
        private static void _getUnknownTypedProperties (Dictionary<string, object[]> items, ref int flagsLength, ref int namesLength, object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence, Type objType, BindingFlags bindingFlags) {
			Dictionary<string, object[]> newItems = new Dictionary<string, object[]>();
			List<string> flags = new List<string>();
			if (bindingFlags.HasFlag(BindingFlags.Public)) flags.Add("public");
			if (bindingFlags.HasFlag(BindingFlags.NonPublic)) flags.Add("nonpublic");
			if (bindingFlags.HasFlag(BindingFlags.Static)) flags.Add("static");
			flags.Add("property");
			string flag = String.Join(",", flags);
			IEnumerable<PropertyInfo> props = objType.GetProperties(bindingFlags);
			object child;
			string dumpedChild;
			foreach (PropertyInfo prop in props) {
				if (!items.ContainsKey(prop.Name) && !newItems.ContainsKey(prop.Name)) {
					if (prop.CanRead) {
						try {
							child = prop.GetValue(obj, null);
							dumpedChild = Dumper._dumpRecursive(child, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
						} catch (Exception e) {
							child = htmlOut ? @"<span class=""getexc"">" + e.Message + "</span>" : "Exception: " + e.Message;
							dumpedChild = child.ToString();
						}
					} else {
						child = "Unable to read property.";
						child = htmlOut ? @"<span class=""getexc"">" + child + "</span>" : "Exception: " + child;
						dumpedChild = child.ToString();
					}
					newItems.Add(
						prop.Name, 
						new object[] { flag, dumpedChild }
					);
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
		private static void _getUnknownTypedFields (Dictionary<string, object[]> items, ref int flagsLength, ref int namesLength, object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence, Type objType, BindingFlags bindingFlags) {
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
			string dumpedChild;
			foreach (FieldInfo field in fields) {
				if (!items.ContainsKey(field.Name) && !newItems.ContainsKey(field.Name)) {
					if (field.Name.Length > namesLength) namesLength = field.Name.Length;
					if (field.IsPrivate && !bindingFlags.HasFlag(BindingFlags.Public)) {
						flag2 = flag.Replace("nonpublic", "private");
					} else {
						flag2 = flag.Replace("nonpublic", "protected");
					}
					try {
						child = field.GetValue(obj);
						dumpedChild = Dumper._dumpRecursive(child, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					} catch (Exception e) {
						child = htmlOut ? @"<span class=""getexc"">" + e.Message + "</span>" : "Exception: " + e.Message;
						dumpedChild = child.ToString();
					}
					newItems.Add(
						field.Name, 
						new object[] { flag2, dumpedChild }
					);
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
		private static string _tabsIndent (int tabs, bool htmlOut) {
			string s = "";
			if (htmlOut) {
				s = @"<s style=""width:" + (27*tabs) + @"px""></s>";
			} else {
				for (var i = 0; i < tabs; i++) s += "   ";
			}
			return s;
		}
		private static string _getNullCode (bool htmlOut) {
			return htmlOut ? @"<span class=""null"">null</span>" : "null";
		}
		private static dynamic _getSimpleTypeArray (object obj) {
			if (obj is sbyte[]) return new { Data = (sbyte[])obj, Length = ((sbyte[])obj).Length };
			if (obj is byte[]) return new { Data = (byte[])obj, Length = ((byte[])obj).Length };
			if (obj is short[]) return new { Data = (short[])obj, Length = ((short[])obj).Length };
			if (obj is ushort[]) return new { Data = (ushort[])obj, Length = ((ushort[])obj).Length };
			if (obj is int[]) return new { Data = (int[])obj, Length = ((int[])obj).Length };
			if (obj is uint[]) return new { Data = (uint[])obj, Length = ((uint[])obj).Length };
			if (obj is long[]) return new { Data = (long[])obj, Length = ((long[])obj).Length };
			if (obj is ulong[]) return new { Data = (ulong[])obj, Length = ((ulong[])obj).Length };
			if (obj is float[]) return new { Data = (float[])obj, Length = ((float[])obj).Length };
			if (obj is double[]) return new { Data = (double[])obj, Length = ((double[])obj).Length };
			if (obj is decimal[]) return new { Data = (decimal[])obj, Length = ((decimal[])obj).Length };
			if (obj is char[]) return new { Data = (char[])obj, Length = ((char[])obj).Length };
			if (obj is string[]) return new { Data = (string[])obj, Length = ((string[])obj).Length };
			if (obj is bool[]) return new { Data = (bool[])obj, Length = ((bool[])obj).Length };
			if (obj is object[]) return new { Data = (object[])obj, Length = ((object[])obj).Length };
			return new { Data = new object[] { }, Length = 0 };
		}
	}
}