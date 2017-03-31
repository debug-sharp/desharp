using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;

namespace Desharp.Completers {
	/// <summary>
	/// Detecting class to check value types to dump them correctly, always internaly used by Desharp, but it should be used for any general purposes.
	/// </summary>
    public class Detector {
		/// <summary>
		/// True if value is Type object.
		/// </summary>
		/// <param name="obj">Any value or null.</param>
		public static bool IsTypeObject (object obj) {
            if (obj is Type) return true;
            return false;
		}
		/// <summary>
		/// True if value is sbyte | byte | short | ushort | int | uint | long | ulong | float | double | decimal | char | bool | string | object.
		/// </summary>
		/// <param name="obj">Any value or null.</param>
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
		/// <summary>
		/// True if value is MethodInfo | PropertyInfo | FieldInfo | EventInfo | MemberInfo | ConstructorInfo.
		/// </summary>
		/// <param name="obj">Any value or null.</param>
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
		/// <summary>
		/// True if value is MethodInfo[] | PropertyInfo[] | FieldInfo[] | EventInfo[] | MemberInfo[] | ConstructorInfo[].
		/// </summary>
		/// <param name="obj">Any value or null.</param>
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
		/// <summary>
		/// True if value is sbyte[] | byte[] | short[] | ushort[] | int[] | uint[] | long[] | ulong[] | float[] | double[] | decimal[] | char[] | bool[] | string[] | object[].
		/// </summary>
		/// <param name="obj">Any value or null.</param>
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
		/// <summary>
		/// True if value is enum.
		/// </summary>
		/// <param name="obj">Any value or null.</param>
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
		/// <summary>
		/// True if value is DataSet | DataTable | DataRow.
		/// </summary>
		/// <param name="obj">Any value or null.</param>
		public static bool IsDbResult (object obj) {
			if (obj is DataSet || obj is DataTable || obj is DataRow) return true;
            return false;
		}
		/// <summary>
		/// True if value is implementing IList | System.Array | System.Collections.ArrayList | System.Data.SqlClient.SqlDataReader.
		/// </summary>
		/// <param name="obj">Any value or null.</param>
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
		/// <summary>
		/// True if value is ICollection.
		/// </summary>
		/// <param name="obj">Any value or null.</param>
		public static bool IsCollection (object obj) {
			if (obj is ICollection) return true;
			return false;
		}
		/// <summary>
		/// True if value is IDictionary and it has properties Count, Keys and Values.
		/// </summary>
		/// <param name="obj">Any value or null.</param>
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
		/// <summary>
		/// True if value is NameValueCollection.
		/// </summary>
		/// <param name="obj">Any value or null.</param>
		public static bool IsNameValueCollection (object obj) {
			if (obj is NameValueCollection) return true;
			return false;
		}
	}
}
