using Desharp.Renderers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;

namespace Desharp.Completers {
    public class Detector {
		public static bool IsTypeObject (object obj) {
            if (obj is Type) return true;
            return false;
        }
        public static bool IsPrimitiveType (object obj) {
            if (
				obj is char || obj is string || obj is bool ||
				obj is int || obj is uint || obj is long || obj is ulong ||
				obj is float || obj is double || obj is decimal ||
				obj is sbyte || obj is byte ||
                obj is short || obj is ushort
            ) {
                return true;
            }
            return false;
        }
        public static bool IsReflectionObject (object obj) {
            if (
                obj is MethodInfo || obj is PropertyInfo ||
                obj is FieldInfo || obj is EventInfo ||
                obj is MemberInfo || obj is ConstructorInfo
            ) {
                return true;
            }
            return false;
        }
        public static bool IsReflectionObjectArray (object obj) {
			if (
                obj is MethodInfo[] || obj is PropertyInfo[] ||
                obj is FieldInfo[] || obj is EventInfo[] ||
                obj is MemberInfo[] || obj is ConstructorInfo[]
            ) {
                return true;
            }
            return false;
        }
        public static bool IsArray (object obj) {
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
				if (objType.BaseType is Type && objType.BaseType.Name == "Enum") {
					if (objType.BaseType.BaseType is Type && objType.BaseType.BaseType.Name == "ValueType") {
						if (objType.BaseType.BaseType.BaseType is Type && objType.BaseType.BaseType.BaseType.Name == "Object") {
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
		public static bool IsCollection (object obj) {
			if (obj is ICollection) return true;
			return false;
		}
		public static bool IsDictionary (object obj) {
			if (obj is IDictionary) {
				return true;
			} else {
				// detect any other dictionary type like: ModelStateDictionary<...>, ModelBinderDictionary<...>, ...
				Type objType = obj.GetType();
				if (objType != null) {
					if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
						return true;
					} else if (objType.GetProperty("Count") is PropertyInfo && objType.GetProperty("Keys") is PropertyInfo && objType.GetProperty("Values") is PropertyInfo) {
						return true;
					}
				}
			}
			return false;
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
		public static bool IsNameValueCollection (object obj) {
			if (obj is NameValueCollection) return true;
			return false;
		}
	}
}
