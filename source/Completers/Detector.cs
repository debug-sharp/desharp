using Desharp.Renderers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Desharp.Completers {
    public class Detector {
		public static bool IsTypeObject (object obj) {
            if (obj is Type) return true;
            return false;
        }
        public static bool IsSimpleType (object obj) {
            if (
                obj is sbyte || obj is byte ||
                obj is short || obj is ushort ||
                obj is int || obj is uint || obj is long || obj is ulong ||
                obj is float || obj is double || obj is decimal ||
                obj is char || obj is string || obj is bool
            ) {
                return true;
            }
            return false;
        }
        public static bool IsSimpleArray (object obj) {
            if (
                obj is sbyte[] || obj is byte[] ||
                obj is short[] || obj is ushort[] ||
                obj is int[] || obj is uint[] || obj is long[] || obj is ulong[] ||
                obj is float[] || obj is double[] || obj is decimal[] ||
                obj is char[] || obj is bool[] || obj is string[] || obj is object[]
            ) {
                return true;
            }
            return false;
        }
		public static bool IsEnum (object obj) {
			Type objType = obj.GetType();
			if (objType != null) {
				if (objType.BaseType.Name == "Enum") {
					if (objType.BaseType.BaseType.Name == "ValueType") {
						if (objType.BaseType.BaseType.BaseType.Name == "Object") {
							return true;
						}
					}
				}
			}
			return false;
		}
		public static bool IsDbResult (object obj) {
            if (obj is DataSet || obj is DataTable || obj is DataRow) return true;
            return false;
        }
        public static bool IsEnumerable (object obj) {
            if (
                obj is IList ||
                obj is System.Array ||
                obj is System.Collections.ArrayList ||
                obj is System.Data.SqlClient.SqlDataReader
            ) {
                return true;
            }
            return false;
        }
        public static bool IsDictionary (object obj) {
            var r = false;
            if (obj is IDictionary) {
                r = true;
            } else {
                DumpType type = Dumper.GetDumpTypes(obj);
                if (type.Text.IndexOf("dictionary") > -1) {
                    try {
                        IDictionary objDct = obj as IDictionary;
                        var keys = objDct.Keys;
                        var value = objDct.Values;
                        r = true;
                    } catch (Exception e) { }
                }
            }
            return r;
        }
        public static bool IsDictionaryInnerCollection (object obj) {
            if (
                obj is Dictionary<string, object>.KeyCollection ||
                obj is Dictionary<string, object>.ValueCollection
            ) {
                return true;
            }
            return false;
        }
    }
}
