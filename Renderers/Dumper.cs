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
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;

namespace Desharp.Renderers {
	/// <summary>
	/// Reflection class to dump any type value into string representation by Desharp.Renderers.Dumper.Dump(value);
	/// </summary>
	[ComVisible(true)]
	public class Dumper {
		internal static string[] HtmlDumpWrapper = new string[] { @"<div class=""desharp-dump"">", "</div>" };
		internal static string[] NullCode = new string[] { @"<span class=""null"">null</span>", "null", @"<span class=""null"">DBNull</span>", "DBNull",  };
		private static string[] _tooLongIndicator = new string[] { @"<span class=""too-deep"">...</span>", "..." };
		private static CultureInfo _englishCultureInfo = new CultureInfo("en-US");
		private static Dictionary<Type, string> _extraFormats = new Dictionary<Type, string>() {
			// { typeof(System.DateTimeOffset), "o" },
			// { typeof(System.DateTime), "o" },
			// { typeof(System.TimeSpan), @"d\.hh\:mm\:ss\.fffffff" },
		};
		private static string[][] _anonymousTypeBaseNames = new string[][] {
			// beginning, generics begin char
			new string[] {"<>f__AnonymousType", "<"},
			new string[] {"VB$AnonymousType_", "(Of "}
		};
		private static Type _objectType = typeof(object);
		private static Type _hiddenAttributeType = typeof(HiddenAttribute);
		private static Type _debuggerBrowsableAttributeType = typeof(DebuggerBrowsableAttribute);
		private static Type _compilerGeneratedAttributeType = typeof(CompilerGeneratedAttribute);
		private static BindingFlags _staticPublic = BindingFlags.Static | BindingFlags.Public;
		private static BindingFlags _staticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;
		private static BindingFlags _instancePublic = BindingFlags.Instance | BindingFlags.Public;
		private static BindingFlags _instanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
		private static BindingFlags _staticInstancePrivatePublic = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
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
			Type objType = obj == null ? null : obj.GetType();
			result.Append(Dumper._dumpRecursive(ref obj, ref objType, htmlOut, maxDepth, maxLength, 0, new List<int>(), newDumpSequence));
			result.Append(htmlOut ? Dumper.HtmlDumpWrapper[1] : System.Environment.NewLine);
			return result.ToString();
		}
		internal static string TabsIndent (int tabs, bool htmlOut) {
			string s = "";
			if (htmlOut) {
				s = @"<s style=""width:" + (27 * tabs) + @"px""></s>";
			} else {
				for (var i = 0; i < tabs; i++) s += "   ";
			}
			return s;
		}
		internal static string DumpPrimitiveType (ref object obj, ref Type objType, ref Type underlyingType, int level, bool htmlOut, int maxLength, string sequence) {
			string renderedValue;
			bool underlyingTypeDefined = underlyingType != null;
			Type typeForDump = underlyingTypeDefined ? underlyingType : objType;
			if (obj == null || obj is System.DBNull) {
				return Dumper._getNullCode(ref obj, ref typeForDump, htmlOut, sequence);
			} else {
				renderedValue = Dumper.RenderPrimitiveTypeValue(ref obj, ref typeForDump, htmlOut, maxLength);
			}
			int? length = null;
			if (obj is string) length = obj.ToString().Length;
			DumpType type = Dumper.GetDumpTypes(ref obj, ref typeForDump, length, htmlOut, false, sequence, underlyingTypeDefined);
			string result = "";
			if (htmlOut) {
				result = @"<span class=""" + type.NameCssClass + @""">" + renderedValue + "</span>&nbsp;" + type.ValueTypeCode;
			} else {
				result = renderedValue + " [" + type.Text + "]";
			}
			return result;
		}
		internal static string RenderPrimitiveTypeValue (ref object obj, ref Type objType, bool htmlOut, int maxLength) {
			string renderedValue = "";
			if (obj is char) {
				renderedValue = obj.ToString();
				if (htmlOut) {
					renderedValue = "'" + Tools.HtmlEntities(renderedValue) + "'";
				} else {
					renderedValue = "'" + renderedValue + "'";
				}
			} else if (obj is string) {
				renderedValue = obj.ToString();
				bool tooLong = false;
				if (renderedValue.Length > maxLength) {
					tooLong = true;
					renderedValue = renderedValue.Substring(0, maxLength) + Dumper._tooLongIndicator[1];
				}
				if (htmlOut) {
					renderedValue = @"""" + Tools.HtmlEntities(renderedValue) + @"""";
					if (tooLong) renderedValue = renderedValue.Substring(0, renderedValue.Length - 4) + Dumper._tooLongIndicator[0] + @"""";
				} else {
					renderedValue = @"""" + renderedValue + @"""";
				}
			} else if (obj is bool) {
				renderedValue = obj.ToString().ToLower();
			} else if (obj is decimal) {
				renderedValue = ((decimal) obj).ToString(Dumper._englishCultureInfo);
			} else if (obj is double) {
				renderedValue = ((double) obj).ToString(Dumper._englishCultureInfo);
			} else if (obj is float) {
				renderedValue = ((float) obj).ToString(Dumper._englishCultureInfo);
			} else if (obj is byte || obj is sbyte) {
				byte byteAbs;
				string sign = " ";
				if (obj is sbyte) {
					sbyte objSbyte = (sbyte)obj;
					if (objSbyte < 0) {
						int objSbyteInt = Math.Abs((int)objSbyte);
						byteAbs = (byte)objSbyteInt;
						sign = "-";
					} else {
						byteAbs = (byte)objSbyte;
					}
				} else {
					byteAbs = (byte)obj;
				}
				byte[] byteArr = new byte[] { byteAbs };
				string dec = Convert.ToInt64(byteAbs).ToString();
				string hex = BitConverter.ToString(byteArr);
				string str = System.Text.Encoding.ASCII.GetString(byteArr);
				if (htmlOut) str = Tools.HtmlEntities(str);
				renderedValue = "HEX:" + sign + hex + ", ASCII:" + sign + "'" + str + "', DEC:" + sign + dec;
			} else {
				renderedValue = obj.ToString();
			}
			return renderedValue;
		}
		internal static DumpType GetDumpTypes (ref object obj, ref Type objType, int? length, bool htmlOut, bool fullTypeName, string sequence, bool? underlyingType = null) {
			string typeStr = "";
			string valueTypeHtml = "";
			string nameTypeClass = "";
			string valueTypeClickClass = "";
			string lengthStr = length.HasValue ? length.ToString() : "";
			string[] lowerAndbiggerThenChars = htmlOut ? new string[] { "&lt;", "&gt;" } : new string[] { "<", ">" };
			bool objIsNUll = objType == null;
			bool objTypeIsNull = objType == null;
			if ((objIsNUll && objTypeIsNull) || obj is System.DBNull) {
				return new DumpType {
					Text = "",
					ValueTypeCode = "",
					NameCssClass = ""
				};
			} else if (underlyingType.HasValue && !underlyingType.Value) {
				Type[] gta = objType.GetGenericArguments();
				if (gta.Length == 1) {
					typeStr = gta[0].Name;
					underlyingType = true;
				} else {
					typeStr = Dumper._getTypeStringWithGenerics(ref objType, htmlOut, fullTypeName);
				}
			} else {
				typeStr = Dumper._getTypeStringWithGenerics(ref objType, htmlOut, fullTypeName);
			}
			string anonymousGenericTypes = Dumper._getAnonymousTypeGenericNames(typeStr);
			if (anonymousGenericTypes != null)  // mostly simple base object with key/value passed into Dump method
				typeStr = "Object" + anonymousGenericTypes;
			nameTypeClass = typeStr.ToLower();
			if (underlyingType.HasValue && underlyingType.Value)
				typeStr += "?";
			if (length.HasValue) {
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
					typeStr = typeStr.Substring(0, lastArrCharsPos) + "[" + lengthStr + "]" + typeStr.Substring(lastArrCharsPos + 2);
				} else if (obj != null) {
					typeStr = typeStr + "[" + lengthStr + "]";
				}
			}
			if (htmlOut) {
				valueTypeClickClass = (length.HasValue && length > 0 && !(obj is string)) 
					? "click click-" + sequence + obj.GetHashCode().ToString() 
					: "";
				valueTypeHtml = @"<span class=""type " + valueTypeClickClass + @""" title=""" + (obj != null ? obj.GetHashCode().ToString() : "") + @""">[" + typeStr + "]</span>";
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
		private static string _getNullCode (ref object obj, ref Type objType, bool htmlOut, string sequence) {
			int nullCodeIndex = (htmlOut ? 0 : 1) + (obj is System.DBNull ? 2 : 0);
			string result = Dumper.NullCode[nullCodeIndex];
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, null, htmlOut, false, sequence);
			return result + (type.ValueTypeCode.Length > 0 ? " " + type.ValueTypeCode : "");
		}
		private static string _getTypeStringWithGenerics (ref Type objType, bool htmlOut, bool fullTypeName) {
			string result = "";
			if (objType != null) {
				string[] lowerAndbiggerThenChars = htmlOut 
					? new string[] { "&lt;", "&gt;" } 
					: new string[] { "<", ">" };
				Type[] gta = objType.GetGenericArguments();
				Type gtaItem = null;
				Type underType = null;
				string typeName = objType.Name;
				string typeFullName = objType.FullName;
				int backSingleQuotPos = 0;
				result = fullTypeName 
					? typeFullName 
					: typeName;
				if (gta.Length > 0) {
					backSingleQuotPos = result.IndexOf('`');
					if (backSingleQuotPos > -1) {
						result = result.Substring(0, backSingleQuotPos);
						List<string> gtaStrs = new List<string>();
						for (int i = 0, l = gta.Length; i < l; i++) {
							gtaItem = gta[i];
							underType = System.Nullable.GetUnderlyingType(gtaItem);
							if (underType != null) {
								gtaStrs.Add(underType.Name.ToString() + "?");
							} else {
								gtaStrs.Add(gtaItem.Name.ToString());
							}
						}
						result += lowerAndbiggerThenChars[0] + String.Join(
							",", 
							gtaStrs.ToArray()
						) + lowerAndbiggerThenChars[1];
						backSingleQuotPos = typeName.IndexOf('`');
						if (backSingleQuotPos > -1) typeName = typeName.Substring(0, backSingleQuotPos);
						if (result.IndexOf(typeName) == -1) {
							backSingleQuotPos = typeName.IndexOf('`');
							if (backSingleQuotPos > -1) typeName = typeName.Substring(0, backSingleQuotPos);
							result += "." + typeName;
						}
					}
				}
			}
			return result;
		}
		private static string _dumpRecursive (ref object obj,ref Type objType,  bool htmlOut, int maxDepth, int maxLength, int level, List<int> ids, string sequence) {
			if (obj == null) {
				return Dumper._getNullCode(ref obj, ref objType, htmlOut, sequence);
			} else {
				int objId = obj.GetHashCode();
				if (ids.Contains(objId)) {
					// detected recursion
					return htmlOut ? @"<span class=""recursion click click-" + sequence + objId.ToString() + @""">{*** RECURSION ***}</span>" : "{*** RECURSION ***}";
				} else {
					ids.Add(objId);
				}
			}
			if (level == maxDepth)
				return Dumper._dumpRecursiveHandleLastLevelDepth(ref obj, ref objType, htmlOut, maxLength, level, sequence);
			string result;
			bool isPrimitiveType = Detector.IsPrimitiveType(ref obj, ref objType);
			Type underlyingType = System.Nullable.GetUnderlyingType(objType);
			if (isPrimitiveType || (!isPrimitiveType && underlyingType != null)) {
				result = Dumper.DumpPrimitiveType(ref obj, ref objType, ref underlyingType, level, htmlOut, maxLength, sequence);
			} else if (Detector.IsArray(ref obj, ref objType)) {
				result = Dumper._dumpArray(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsNameValueCollection(ref obj, ref objType)) {
				result = Dumper._dumpNameValueCollection(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsDictionary(ref obj, ref objType)) {
				result = Dumper._dumpDictionary(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsDbResult(ref obj, ref objType)) {
				result = Dumper._dumpDbResult(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsEnum(ref obj, ref objType)) {
				result = Dumper._dumpEnum(ref obj, ref objType, level, htmlOut, sequence);
			} else if (Detector.IsTypeObject(ref obj, ref objType)) {
				result = Dumper._dumpTypeObject(ref obj, ref objType, level, htmlOut, sequence);
			} else if (Detector.IsEnumerable(ref obj, ref objType)) {
				result = Dumper._dumpEnumerable(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsCollection(ref obj, ref objType)) {
				result = Dumper._dumpCollection(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsTuple(ref obj, ref objType)) {
				result = Dumper._dumpTuple(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else if (Detector.IsFunc(ref obj, ref objType)) {
				result = Dumper._dumpFunc(ref obj, ref objType, level, htmlOut);
			} else if (Detector.IsDelegate(ref obj, ref objType)) {
				result = Dumper._dumpDelegate(ref obj, ref objType, level, htmlOut);
			} else if (Detector.IsReflectionObject(ref obj, ref objType)) {
				result = Dumper._dumpReflectionObject(ref obj, ref objType, htmlOut, sequence);
			} else if (Detector.IsExtraFormatedObject(ref obj, ref objType)) {
				result = Dumper._dumpExtraFormated(ref obj, ref objType, htmlOut, sequence);
			} else {
				result = Dumper._dumpUnknown(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
			}
			return result;
		}
		private static string _dumpRecursiveHandleLastLevelDepth (ref object obj, ref Type objType, bool htmlOut, int maxLength, int level, string sequence) {
			// in last level - print out only single line prints, complex object only as: ... [Type]
			bool isPrimitiveType = Detector.IsPrimitiveType(ref obj, ref objType);
			Type underlyingType = System.Nullable.GetUnderlyingType(objType);
			if (isPrimitiveType || (!isPrimitiveType && underlyingType != null)) {
				return Dumper.DumpPrimitiveType(ref obj, ref objType, ref underlyingType, level, htmlOut, maxLength, sequence);
			} else if (Detector.IsEnum(ref obj, ref objType)) {
				return Dumper._dumpEnum(ref obj, ref objType, level, htmlOut, sequence);
			} else if (Detector.IsTypeObject(ref obj, ref objType)) {
				return Dumper._dumpTypeObject(ref obj, ref objType, level, htmlOut, sequence);
			} else if (Detector.IsFunc(ref obj, ref objType)) {
				return Dumper._dumpFunc(ref obj, ref objType, level, htmlOut);
			} else if (Detector.IsDelegate(ref obj, ref objType)) {
				return Dumper._dumpDelegate(ref obj, ref objType, level, htmlOut);
			} else if (Detector.IsExtraFormatedObject(ref obj, ref objType)) {
				return Dumper._dumpExtraFormated(ref obj, ref objType, htmlOut, sequence);
			} else if (Detector.IsReflectionObject(ref obj, ref objType)) {
				return Dumper._dumpReflectionObject(ref obj, ref objType, htmlOut, sequence);
			} else {
				DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, null, htmlOut, true, sequence);
				return Dumper._tooLongIndicator[htmlOut ? 0 : 1] + type.ValueTypeCode;
			}
		}
		private static string _dumpReflectionObject (ref object obj, ref Type objType, bool htmlOut, string sequence) {
			string renderedValue;
			if (obj == null) {
				renderedValue = Dumper._getNullCode(ref obj, ref objType, htmlOut, sequence);
			} else {
				PropertyInfo fullNameProp = objType.GetProperty("FullName", Dumper._staticInstancePrivatePublic);
				if (fullNameProp != null) {
					renderedValue = fullNameProp.GetValue(obj, null).ToString();
				} else {
					renderedValue = (obj as _MemberInfo).Name;
				}
			}
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, null, htmlOut, false, sequence);
			string result;
			if (htmlOut) {
				result = @"<span class=""" + type.Text + @""">" + renderedValue + "</span>&nbsp;" + type.ValueTypeCode;
			} else {
				result = renderedValue + " [" + type.Text + "]";
			}
			return result;
		}
		private static string _dumpToString (ref object obj, bool htmlOut) {
			string result = obj.ToString();
			if (htmlOut) result = @"<span class=""unknown"">" + result + "</span>";
			return result;
		}
		private static string _dumpArray (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			dynamic objArr = obj;
			bool? nullableArray = null;
			if (objType.FullName.Length > 15 && objType.FullName.Substring(0, 15) == "System.Nullable") nullableArray = false; 
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, objArr.Length, htmlOut, false, sequence, nullableArray);
			string result = type.ValueTypeCode;
			string dumpedChild;
			object child;
			Type childType;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper.TabsIndent(level + 1, htmlOut);
			int maxKeyDigits = objArr.Length.ToString().Length;
			string key;
			for (int i = 0, l = objArr.Length; i < l; i += 1) {
				key = i.ToString();
				child = objArr[i];
				childType = child == null ? null : child.GetType();
				dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
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
		private static string _dumpNameValueCollection (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			NameValueCollection objCol = (NameValueCollection)obj;
			string[] objColKeys = objCol.AllKeys;
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, objColKeys.Length, htmlOut, true, sequence);
			string result = type.ValueTypeCode;
			string dumpedChild;
			object key;
			Type keyType;
			string keyStr;
			int maxKeyLength = 0;
			object child;
			Type childType;
			DumpType subTypeKey;
			DumpType subTypeValue;
			string tabsStr = Dumper.TabsIndent(level + 1, htmlOut);
			if (htmlOut) {
				result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
				for (int i = 0, l = objColKeys.Length; i < l; i += 1) {
					key = objColKeys[i];
					keyType = key == null ? null : key.GetType();
					keyStr = key.ToString();
					child = objCol.Get(keyStr);
					childType = child == null ? null : child.GetType();
					subTypeKey = Dumper.GetDumpTypes(ref key, ref keyType, null, htmlOut, false, sequence);
					subTypeValue = Dumper.GetDumpTypes(ref child, ref childType, null, htmlOut, false, sequence);
					dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					if (subTypeKey.NameCssClass == "char") keyStr = "'" + keyStr + "'";
					if (subTypeKey.NameCssClass == "string") keyStr = @"""" + keyStr + @"""";
					result += (i > 0 ? "<br />" : "") + tabsStr
						+ @"<span class=""" + subTypeKey.NameCssClass + @""">" + keyStr.Replace("<", "&lt;").Replace(">", "&gt;") + "</span>"
						+ "<s>:&nbsp;</s>" + dumpedChild;
				}
				result += "</div>";
			} else {
				List<string[]> dumpedItems = new List<string[]>();
				for (int i = 0, l = objColKeys.Length; i < l; i += 1) {
					key = objColKeys[i];
					keyType = key == null ? null : key.GetType();
					keyStr = key.ToString();
					child = objCol.Get(keyStr);
					childType = child == null ? null : child.GetType();
					subTypeKey = Dumper.GetDumpTypes(ref key, ref keyType, null, htmlOut, false, sequence);
					subTypeValue = Dumper.GetDumpTypes(ref child, ref childType, null, htmlOut, false, sequence);
					dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
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
		private static string _dumpDictionary (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			// IDictionary objDct = (IDictionary)obj; // do not use this retype - causes exception for additional ditionaries
			PropertyInfo countPropInfo = objType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo keysPropInfo = objType.GetProperty("Keys", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo valuesPropInfo = objType.GetProperty("Values", BindingFlags.Instance | BindingFlags.Public);
			int count = (int)countPropInfo.GetValue(obj, new object[] { });
			IEnumerable keys = keysPropInfo.GetValue(obj, null) as IEnumerable;
			IEnumerable values = valuesPropInfo.GetValue(obj, null) as IEnumerable;
			IEnumerator keysEnum = keys.GetEnumerator();
			IEnumerator valuesEnum = values.GetEnumerator();
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, count, htmlOut, false, sequence);
			string result = type.ValueTypeCode;
			string dumpedChild;
			object key;
			Type keyType;
			string keyStr;
			string tabsStr = Dumper.TabsIndent(level + 1, htmlOut);
			DumpType subTypeKey;
			object child;
			Type childType;
			if (htmlOut) {
				result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
				int i = 0;
				while (keysEnum.MoveNext()) {
					key = keysEnum.Current;
					keyType = key == null ? null : key.GetType();
					valuesEnum.MoveNext();
					child = valuesEnum.Current;
					childType = child == null ? null : child.GetType();
					keyStr = Detector.IsPrimitiveType(ref key, ref keyType) 
						? key.ToString().Replace("<", "&lt;").Replace(">", "&gt;") 
						: Dumper._dumpRecursive(ref key, ref keyType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					subTypeKey = Dumper.GetDumpTypes(ref key, ref keyType, null, htmlOut, false, sequence);
					dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					if (subTypeKey.NameCssClass == "char")
						keyStr = "'" + Tools.HtmlEntities(keyStr) + "'";
					if (subTypeKey.NameCssClass == "string")
						keyStr = @"""" + Tools.HtmlEntities(keyStr) + @"""";
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
					keyType = key == null ? null : key.GetType();
					valuesEnum.MoveNext();
					child = valuesEnum.Current;
					childType = child == null ? null : child.GetType();
					keyStr = Detector.IsPrimitiveType(ref key, ref keyType) 
						? key.ToString() 
						: Dumper._dumpRecursive(ref key, ref keyType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					subTypeKey = Dumper.GetDumpTypes(ref key, ref keyType, null, htmlOut, false, sequence);
					dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					if (subTypeKey.NameCssClass == "char")
						keyStr = "'" + HttpUtility.JavaScriptStringEncode(keyStr) + "'";
					if (subTypeKey.NameCssClass == "string")
						keyStr = @"""" + HttpUtility.JavaScriptStringEncode(keyStr) + @"""";
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
        private static string _dumpDbResult (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
            string result = "";
            DumpType type;
            int length = 0;
			string dumpedChild;
            object child;
			Type childType;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper.TabsIndent(level + 1, htmlOut);
			if (obj is DataSet) {
                DataSet ds = obj as DataSet;
                length = ds.Tables.Count;
                type = Dumper.GetDumpTypes(ref obj, ref objType, length, htmlOut, false, sequence);
                result += type.ValueTypeCode;
                for (int i = 0; i < length; i++) {
                    child = ds.Tables[i];
					childType = child == null ? null : child.GetType();
                    DataTable table = ds.Tables[i];
					dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
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
                type = Dumper.GetDumpTypes(ref obj, ref objType, length, htmlOut, false, sequence);
                result += type.ValueTypeCode;
                for (int i = 0; i < length; i++) {
                    child = rows[i];
					childType = child == null ? null : child.GetType();
					dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
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
                type = Dumper.GetDumpTypes(ref obj, ref objType, length, htmlOut, false, sequence);
                result += type.ValueTypeCode;
				int i = 0;
                foreach (DataColumn column in columns) {
					child = row[column];
					childType = child == null || child is System.DBNull ? null : child.GetType();
                    DumpType subTypeValue = Dumper.GetDumpTypes(ref child, ref childType, null, htmlOut, false, sequence);
					string subTypeCls = subTypeValue.Text.ToString().ToLower();
                    string val = subTypeCls == "dbnull" ? "DBNull" : child.ToString();
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
		private static string _dumpEnum (ref object obj, ref Type objType, int level, bool htmlOut, string sequence) {
			Enum objEnum = obj as Enum;
			int objInt = Convert.ToInt32(objEnum);
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, null, htmlOut, true, sequence);
			List<string> resultItems = new List<string>();
			string result;
			Array allValues = Enum.GetValues(objType);
			Enum valEnum;
			for (int i = 1; i < allValues.Length; i++) {
				valEnum = allValues.GetValue(i) as Enum;
				if (objEnum.HasFlag(valEnum)) 
					resultItems.Add(valEnum.ToString());
			}
			if (resultItems.Count == 0)
				resultItems.Add(allValues.GetValue(0).ToString());
			result = String.Join(", ", resultItems.ToArray());
			if (htmlOut) {
				result = @"<span class=""enum"">" + result + "</span>&nbsp;" + type.ValueTypeCode;
			} else {
				result = result + " [" + type.Text + "]";
			}
			return result;
		}
		private static string _dumpTypeObject (ref object obj, ref Type objType, int level, bool htmlOut, string sequence) {
			string renderedValue = obj == null 
				? Dumper._getNullCode(ref obj, ref objType, htmlOut, sequence) 
				: (obj as Type).FullName;
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, null, htmlOut, false, sequence);
			string result = "";
			if (htmlOut) {
				result = @"<span class=""" + type.Text + @""">" + renderedValue + "</span>&nbsp;" + type.ValueTypeCode;
			} else {
				result = renderedValue + " [" + type.Text + "]";
			}
			return result;
		}
		private static string _dumpEnumerable (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
            string result = htmlOut 
				? @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">"
				: "";
			string dumpedChild;
			object child;
			Type childType;
            string keyStr;
			string tabsStr = Dumper.TabsIndent(level + 1, htmlOut);
            IEnumerator enumerator = (obj as IEnumerable).GetEnumerator();
            int index = 0;
            while (enumerator.MoveNext()) {
                child = enumerator.Current;
				childType = child == null ? null : child.GetType();
                keyStr = index.ToString();
                dumpedChild = Dumper._dumpRecursive(
					ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence
				);
                if (htmlOut) {
                    result += (index > 0 ? "<br />" : "") + tabsStr
                    + @"<span class=""int"">" + keyStr + "</span>"
                    + "<s>:&nbsp;</s>" + dumpedChild;
                } else {
                    result += System.Environment.NewLine + tabsStr + keyStr + ": " + dumpedChild;
                }
                index += 1;
            }
			if (htmlOut) result += "</div>";
            DumpType dumpType = Dumper.GetDumpTypes(ref obj, ref objType, index, htmlOut, true, sequence);
            return dumpType.ValueTypeCode + result;
		}
		private static string _dumpCollection (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			dynamic objCol = (ICollection)obj;
			IEnumerator objEnum = objCol.GetEnumerator();
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, objCol.Count.ToString(), htmlOut, true, sequence);
			string result = type.ValueTypeCode;
			string dumpedChild;
			string keyStr;
			int maxKeyLength = 0;
			DumpType subTypeKey;
			DumpType subTypeValue;
			string tabsStr = Dumper.TabsIndent(level + 1, htmlOut);
			int i = 0;
			object iObj;
			Type intType = typeof(int);
			object child;
			Type childType;
			if (htmlOut) {
				result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
				while (objEnum.MoveNext()) {
					iObj = i;
					child = objEnum.Current;
					childType = child == null ? null : child.GetType();
					dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					subTypeKey = Dumper.GetDumpTypes(ref iObj, ref intType, null, htmlOut, false, sequence);
					subTypeValue = Dumper.GetDumpTypes(ref child, ref childType, null, htmlOut, false, sequence);
					keyStr = i.ToString().Replace("<", "&lt;").Replace(">", "&gt;");
					result += (i > 0 ? "<br />" : "")
						+ tabsStr + @"<span class=""" + subTypeKey.NameCssClass + @""">" + keyStr + "</span>" 
						+ "<s>:&nbsp;</s>" + dumpedChild;
					i++;
				}
				result += "</div>";
			} else {
				List<string[]> dumpedItems = new List<string[]>();
				while (objEnum.MoveNext()) {
					iObj = i;
					child = objEnum.Current;
					childType = child == null ? null : child.GetType();
					dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					subTypeKey = Dumper.GetDumpTypes(ref iObj, ref intType, null, htmlOut, true, sequence);
					subTypeValue = Dumper.GetDumpTypes(ref child, ref childType, null, htmlOut, true, sequence);
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
		private static string _dumpTuple (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			List<object> members = new List<object>();
			BindingFlags membersFlags = BindingFlags.Public | BindingFlags.Instance;
			// get items by C# 7.0+ implementation:
			FieldInfo[] fields = objType.GetFields(membersFlags);
			if (fields.Length > 0) {
				for (int i = 0, l = fields.Length; i < l; i += 1)
					members.Add(fields[i].GetValue(obj));
			} else { 
				// get items by `System.Tuple<>` nuget package for backward compatibility:
				PropertyInfo[] props = objType.GetProperties(membersFlags);
				for (int i = 0, l = props.Length; i < l; i += 1)
					members.Add(props[i].GetValue(obj, new object[] { }));
			}
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, members.Count, htmlOut, false, sequence);
			string result = type.ValueTypeCode;
			string dumpedChild;
			object child;
			Type childType;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper.TabsIndent(level + 1, htmlOut);
			int maxKeyDigits = (4 + members.Count).ToString().Length;
			string key;
			for (int j = 0, k = members.Count; j < k; j += 1) {
				key = "Item" + (j + 1).ToString();
				child = members[j];
				childType = child == null ? null : child.GetType();
				dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
				if (htmlOut) {
					result += (j > 0 ? "<br />" : "") + tabsStr
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
		private static string _dumpFunc(ref object obj, ref Type objType, int level, bool htmlOut) {
			string result = "";
			MethodInfo mi = objType.GetProperty("Method").GetValue(obj, null) as MethodInfo;
			ParameterInfo[] prms = mi.GetParameters();
			ParameterInfo prmItem;
			Type prmType;
			Type prmUnderType;
			string paramTypeName;
			if (prms.Length > 0) { 
				for (int i = 0, l = prms.Length; i < l; i += 1) {
					prmItem = prms[i];
					prmType = prms[i].ParameterType;
					prmUnderType = System.Nullable.GetUnderlyingType(prmType);
					paramTypeName = prmUnderType != null ? prmUnderType.Name + "?" : prmType.Name;
					result += (result.Length == 0
						? "Func<"
						: ", ") + paramTypeName + " " + prmItem.Name;
				}
				result += ", ";
			} else {
				result += "Func<";
			}
			prmUnderType = System.Nullable.GetUnderlyingType(mi.ReturnType);
			paramTypeName = prmUnderType != null ? prmUnderType.Name + "?" : mi.ReturnType.Name;
			result += "return " + paramTypeName + ">()";
			if (htmlOut) {
				result = $@"<span class=""runtimetype func"">"
					 + result.Replace("<", "&lt;").Replace(">", "&gt;")
				+ @"</span>&nbsp;<span class=""type"">[System.Func]</span>";
			}
			return result;
		}
		private static string _dumpDelegate(ref object obj, ref Type objType, int level, bool htmlOut) {
			string result = "";
			MethodInfo mi = objType.GetProperty("Method").GetValue(obj, null) as MethodInfo;
			ParameterInfo[] prms = mi.GetParameters();
			if (prms.Length > 0) { 
				for (int i = 0, l = prms.Length; i < l; i += 1) {
					result += (result.Length == 0
						? mi.ReturnType.Name + " " + objType.FullName + "("
						: ", ") + prms[i].ParameterType.Name;
				}
				result += ")";
			} else {
				result += mi.ReturnType.Name + " " + objType.FullName + "()";
			}
			if (htmlOut) {
				result = $@"<span class=""runtimetype delegate"">"
					 + result
				+ @"</span>&nbsp;<span class=""type"">[System.Delegate]</span>";
			}
			return result;
		}
		private static string _dumpUnknown (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
			if (objType == null) {
				return Dumper._dumpUnknownNotTyped(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
			} else {
				string anonymousGenericTypes = Dumper._getAnonymousTypeGenericNames(objType.FullName);
				if (anonymousGenericTypes != null) {
					return Dumper._dumpUnknownNotTyped(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);	
				} else { 
					return Dumper._dumpUnknownTyped(ref obj, ref objType, level, htmlOut, ids, maxDepth, maxLength, sequence);
				}
			}
        }
        private static string _dumpUnknownNotTyped (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
            PropertyDescriptorCollection objProperties = TypeDescriptor.GetProperties(obj);
            DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, objProperties.Count, htmlOut, false, sequence);
            string result = type.ValueTypeCode;
            string name;
            object child;
            Type childType;
			string dumpedChild;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper.TabsIndent(level + 1, htmlOut);
			int i = 0;
			foreach (PropertyDescriptor descriptor in objProperties) {
                name = descriptor.Name;
                try {
                    child = descriptor.GetValue(obj);
					childType = child == null ? System.Nullable.GetUnderlyingType(descriptor.PropertyType) : descriptor.PropertyType;
					dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
				} catch (Exception e) {
					child = htmlOut ? @"<span class=""getexc"">" + e.Message + "</span>" : "Exception: " + e.Message;
					dumpedChild = child.ToString();
				}
				if (htmlOut) {
                    result += (i > 0 ? "<br />" : "") + tabsStr
						+ @"<span class=""field"">"
							+ name.Replace("<", "&lt;").Replace(">", "&gt;")
						+ "</span><s>:&nbsp;</s>" 
                        + dumpedChild;
                } else {
                    result += System.Environment.NewLine + tabsStr + name + ": " + dumpedChild;
                }
				i++;
			}
			if (htmlOut) result += "</div>";
			return result;
		}
        private static string _dumpUnknownTyped (ref object obj, ref Type objType, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence) {
            string indexerPropertyName = Dumper._getIndexerPropertyName(objType);
            int flagsLength = 0;
			int namesLength = 0;
			//Dictionary<string, FieldInfo> allPotentialEvents = objType.GetFields(Dumper._staticInstancePrivatePublic).ToDictionary<FieldInfo, string>(item => item.Name);
			Dictionary<string, object[]> items = new Dictionary<string, object[]>();
			Dumper._getUnknownTypedEvents(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._staticPublic);
			Dumper._getUnknownTypedEvents(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._instancePublic);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._staticPublic, indexerPropertyName);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._instancePublic, indexerPropertyName);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._staticPublic);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._instancePublic);

			Dumper._getUnknownTypedEvents(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._staticNonPublic);
			Dumper._getUnknownTypedEvents(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._instanceNonPublic);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._staticNonPublic, indexerPropertyName);
			Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._instanceNonPublic, indexerPropertyName);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._staticNonPublic);
			Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, objType, Dumper._instanceNonPublic);
			// for static properties and static fields only defined in parent classes,
			// search through in parent classes, because they are not automaticly returned through Type reflection
			Type objectType = typeof(Object);
			Type currentType = objType;
			int safeCounter = 0;
			while (true && safeCounter < 50) {
				currentType = currentType.BaseType;
				if (currentType == null || currentType.Equals(objectType)) break;
				Dumper._getUnknownTypedEvents(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, Dumper._staticPublic);
				Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, Dumper._staticPublic, indexerPropertyName);
				Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, Dumper._staticPublic);
				Dumper._getUnknownTypedEvents(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, Dumper._staticNonPublic);
				Dumper._getUnknownTypedProperties(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, Dumper._staticNonPublic, indexerPropertyName);
				Dumper._getUnknownTypedFields(items, ref flagsLength, ref namesLength, obj, level, htmlOut, ids, maxDepth, maxLength, sequence, currentType, Dumper._staticNonPublic);
				safeCounter++;
			}
            DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, items.Count, htmlOut, true, sequence);
            string flags;
			string name;
			string cssClass;
			string dumpedChild;
            string result = type.ValueTypeCode;
			if (htmlOut) result += @"<div class=""item dump dump-" + sequence + obj.GetHashCode().ToString() + @""">";
			string tabsStr = Dumper.TabsIndent(level + 1, htmlOut);
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
						+ @"<span class=""" + cssClass + @""" title =""" + flags + @""">" 
							+ name.Replace("<", "&lt;").Replace(">", "&gt;") 
						+ "</span><s>:&nbsp;</s>"
						+ dumpedChild;
                } else {
					result += System.Environment.NewLine + tabsStr + flags + " " + name + ": " + dumpedChild;
				}
				i++;
			}
			if (htmlOut) result += "</div>";
			return result;
		}
        private static void _getUnknownTypedProperties (Dictionary<string, object[]> items, ref int flagsLength, ref int namesLength, object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence, Type objType, BindingFlags bindingFlags, string indexerPropertyName) {
            IEnumerable<PropertyInfo> props = objType.GetProperties(bindingFlags);
            if (props.Count() == 0) return;
            Dictionary<string, object[]> newItems = new Dictionary<string, object[]>();
			List<string> flags = new List<string>();
			if (bindingFlags.HasFlag(BindingFlags.Public)) flags.Add(htmlOut ? "public" : "publ");
			if (bindingFlags.HasFlag(BindingFlags.NonPublic)) flags.Add(htmlOut ? "nonpublic" : "nonpub");
			if (bindingFlags.HasFlag(BindingFlags.Static)) flags.Add(htmlOut ? "static" : "stat");
			flags.Add(htmlOut ? "property" : "prop");
			string flag = String.Join(",", flags);
			object child;
			Type childType;
			string dumpedChild;
			foreach (PropertyInfo prop in props) {
				if (!items.ContainsKey(prop.Name) && !newItems.ContainsKey(prop.Name) && prop.Name != indexerPropertyName && !Dumper._isCompilerGenerated(prop)) {
					if (prop.CanRead) {
						try {
							child = prop.GetValue(obj, null);
							childType = child == null ? System.Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType;
							dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
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
        private static string _getIndexerPropertyName (Type type) {
            Attribute defaultAttr = Attribute.GetCustomAttribute(type, typeof(DefaultMemberAttribute));
            return defaultAttr is DefaultMemberAttribute ? ((DefaultMemberAttribute)defaultAttr).MemberName : "";
        }
        private static void _getUnknownTypedFields (Dictionary<string, object[]> items, ref int flagsLength, ref int namesLength, object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence, Type objType, BindingFlags bindingFlags) {
            IEnumerable<FieldInfo> fields = objType.GetFields(bindingFlags);
            if (fields.Count() == 0) return;
            Dictionary<string, object[]> newItems = new Dictionary<string, object[]>();
			List<string> flags = new List<string>();
			if (bindingFlags.HasFlag(BindingFlags.Public)) flags.Add(htmlOut ? "public" : "publ");
			if (bindingFlags.HasFlag(BindingFlags.NonPublic)) flags.Add(htmlOut ? "nonpublic" : "nonpub");
			if (bindingFlags.HasFlag(BindingFlags.Static)) flags.Add(htmlOut ? "static" : "stat");
			flags.Add("field");
			string flag = String.Join(",", flags);
			string flag2;
			object child;
			Type childType;
			string dumpedChild;
			foreach (FieldInfo field in fields) {
				if (!items.ContainsKey(field.Name) && !newItems.ContainsKey(field.Name) && !Dumper._isCompilerGenerated(field)) {
					if (field.Name.Length > namesLength) namesLength = field.Name.Length;
					if (field.IsPrivate && !bindingFlags.HasFlag(BindingFlags.Public)) {
						flag2 = flag.Replace(htmlOut ? "nonpublic" : "nonpub", htmlOut ? "private" : "priv");
					} else {
						flag2 = flag.Replace(htmlOut ? "nonpublic" : "nonpub", htmlOut ? "protected" : "prot");
					}
					try {
						child = field.GetValue(obj);
						childType = child == null ? System.Nullable.GetUnderlyingType(field.FieldType)  : field.FieldType;
						dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					} catch (Exception e) {
						//Debug.Dump(e);
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
		private static void _getUnknownTypedEvents (Dictionary<string, object[]> items, ref int flagsLength, ref int namesLength, object obj, int level, bool htmlOut, List<int> ids, int maxDepth, int maxLength, string sequence, Type objType, BindingFlags bindingFlags) {
            IEnumerable<EventInfo> events = objType.GetEvents(bindingFlags);
            if (events.Count() == 0) return;
            Dictionary<string, object[]> newItems = new Dictionary<string, object[]>();
			List<string> flags = new List<string>();
			if (bindingFlags.HasFlag(BindingFlags.Public)) flags.Add(htmlOut ? "public" : "publ");
			if (bindingFlags.HasFlag(BindingFlags.NonPublic)) flags.Add(htmlOut ? "nonpublic" : "nonpub");
			if (bindingFlags.HasFlag(BindingFlags.Static)) flags.Add(htmlOut ? "static" : "stat");
			flags.Add("event");
			string flag = String.Join(",", flags);
			string flag2;
			List<System.Delegate> handlers;
			List<MethodInfo> handlerMethods;
			object child;
			Type childType;
			string dumpedChild;
			foreach (EventInfo evnt in events) {
				if (!items.ContainsKey(evnt.Name) && !newItems.ContainsKey(evnt.Name) && !Dumper._isCompilerGenerated(evnt)) {
					if (evnt.Name.Length > namesLength) namesLength = evnt.Name.Length;
					FieldInfo field = objType.GetField(evnt.Name, Dumper._staticInstancePrivatePublic);
					if (field != null) {
						if (field.IsPrivate && !bindingFlags.HasFlag(BindingFlags.Public)) {
							flag2 = flag.Replace(htmlOut ? "nonpublic" : "nonpub", htmlOut ? "private" : "priv");
						} else {
							flag2 = flag.Replace(htmlOut ? "nonpublic" : "nonpub", htmlOut ? "protected" : "prot");
						}
					} else {
						childType = evnt.EventHandlerType;
						child = null;
						flag2 = flag;
					}
					try {
						System.Delegate del = field.GetValue(obj) as System.Delegate;
						if (del != null) {
							childType = field.FieldType;
							handlers = del.GetInvocationList().ToList();
							handlerMethods = new List<MethodInfo>();
							foreach (var handler in handlers) 
								handlerMethods.Add(handler.Method);
							child = handlerMethods;
						} else {
							childType = evnt.EventHandlerType;
							child = null;
						}
						dumpedChild = Dumper._dumpRecursive(ref child, ref childType, htmlOut, maxDepth, maxLength, level + 1, new List<int>(ids), sequence);
					} catch (Exception e) {
						//Debug.Dump(e);
						child = htmlOut ? @"<span class=""getexc"">" + e.Message + "</span>" : "Exception: " + e.Message;
						dumpedChild = child.ToString();
					}
					newItems.Add(
						evnt.Name, 
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
		private static string _dumpExtraFormated (ref object obj, ref Type objType, bool htmlOut, string sequence) {
			string format = null;
			if (obj is System.DateTimeOffset) {
				format = @"yyyy\-MM\-dd HH\:mm\:ss\.fffffff zzz";
			} else if (obj is System.DateTime) {
				format = @"yyyy\-MM\-dd HH\:mm\:ss\.fffffff";
			} else if (obj is TimeSpan) {
				format = @"d\.hh\:mm\:ss\.fffffff";
			} /*else if (!(obj is StringBuilder) && Dumper._extraFormats.Count > 0) {
				foreach (var item in Dumper._extraFormats) {
					if (item.Key.IsAssignableFrom(objType)) {
						format = item.Value;
						break;
					}
				}
			}*/
			string result = format != null 
				? ((IFormattable)obj).ToString(format, Dumper._englishCultureInfo) 
				: obj.ToString();
			DumpType type = Dumper.GetDumpTypes(ref obj, ref objType, null, htmlOut, false, sequence);
			if (htmlOut) {
				return @"<span class=""" + type.NameCssClass + @""">" + result + "</span>&nbsp;" + type.ValueTypeCode;
			} else {
				return result + " [" + type.Text + "]";
			}
		}
		private static string _getAnonymousTypeGenericNames (string typeStr) {
			string result = null;
			string[] item;
			int pos;
			for (int i = 0, l = Dumper._anonymousTypeBaseNames.Length; i < l; i += 1) {
				item = Dumper._anonymousTypeBaseNames[i];
				pos = typeStr.IndexOf(item[0]);
				if (pos == 0) {
					pos = typeStr.IndexOf(item[1], item[0].Length);
					if (pos > 0) {
						result = typeStr.Substring(pos);
					} else {
						result = typeStr.Substring(item[0].Length + 1);
					}
					break;
				}
			}
			return result;
		}
        private static bool _isCompilerGenerated (MemberInfo fieldOrPropInfo) {
            if (Dispatcher.DumpCompillerGenerated) return false;
			if (Attribute.GetCustomAttribute(
				fieldOrPropInfo, Dumper._hiddenAttributeType
			) is HiddenAttribute) return true;
			if (Attribute.GetCustomAttribute(
				fieldOrPropInfo, Dumper._compilerGeneratedAttributeType
			) is CompilerGeneratedAttribute) return true;
			Attribute attr = Attribute.GetCustomAttribute(
				fieldOrPropInfo, Dumper._debuggerBrowsableAttributeType
			);
			if (attr is DebuggerBrowsableAttribute) {
				DebuggerBrowsableAttribute debuggerBrowsableAttr = attr as DebuggerBrowsableAttribute;
				if (!debuggerBrowsableAttr.State.Equals(DebuggerBrowsableState.Collapsed))
					return true;
			}
			return false;
        }
	}
}