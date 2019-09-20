using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Desharp.Completers {
	/// <summary>
	/// Detecting class to check value types to dump them correctly, always internally used by Desharp, but it should be used for any general purposes.
	/// </summary>
	[ComVisible(true)]
    public class Detector {
		/// <summary>
		/// Common `System.Enum` `Type` object instance
		/// </summary>
		protected static Type enumType = typeof(System.Enum);
		/// <summary>
		/// Common `System.MulticastDelegate` `Type` object instance
		/// </summary>
		protected static Type multicastDelegateType = typeof(System.MulticastDelegate);
		/// <summary>
		/// Common `System.Delegate` `Type` object instance
		/// </summary>
		protected static Type delegateType = typeof(System.Delegate);
		/// <summary>
		/// `System.Tuple` string value (backwards compatibility for .NET 4.0+)
		/// </summary>
		protected static string systemTupleStr = "System.Tuple";
		/// <summary>
		/// `System.Tuple` string value length (backwards compatibility for .NET 4.0+)
		/// </summary>
		protected static int systemTupleStrLen = 12;

		/// <summary>
		/// True if value is sbyte | byte | short | ushort | int | uint | long | ulong | float | double | decimal | char | bool | string | object.
		/// </summary>
		/// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` is any primitive value except `enum`, `struct` and `unmanaged`.</returns>
		public static bool IsPrimitiveType (ref object obj, ref Type objType) {
			// https://docs.microsoft.com/en-US/dotnet/api/system.type.isprimitive?view=netframework-4.8
			if (objType.IsPrimitive || obj is string || obj is decimal || obj is System.DBNull) return true; // decimal and string is not primitive
			return false;
		}
		/// <summary>
		/// True if value is sbyte[] | byte[] | short[] | ushort[] | int[] | uint[] | long[] | ulong[] | float[] | double[] | decimal[] | char[] | bool[] | string[] | object[].
		/// </summary>
		/// <param name="obj">Any value or null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` is array from any primitive value except `enum`, `struct` and `unmanaged`.</returns>
		public static bool IsArray (ref object obj, ref Type objType) {
			if (objType.IsArray) return true;
            return false;
		}
		/// <summary>
		/// True if value is NameValueCollection.
		/// </summary>
		/// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if given `obj` is `NameValueCollection`.</returns>
		public static bool IsNameValueCollection (ref object obj, ref Type objType) {
			if (obj is NameValueCollection) return true;
			return false;
		}
		/// <summary>
		/// True if value is IDictionary and it has properties Count, Keys and Values.
		/// </summary>
		/// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` is any `Dictionary`.</returns>
		public static bool IsDictionary (ref object obj, ref Type objType) {
			if (obj is IDictionary) {
				return true;
			} else if (objType != null) {
				// detect any other dictionary type like: ModelStateDictionary<...>, ModelBinderDictionary<...>, ...
				if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
					return true;
				} else if (
					objType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public) is PropertyInfo && 
					objType.GetProperty("Keys", BindingFlags.Instance | BindingFlags.Public) is PropertyInfo && 
					objType.GetProperty("Values", BindingFlags.Instance | BindingFlags.Public) is PropertyInfo
				) {
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// True if value is DataSet | DataTable | DataRow.
		/// </summary>
		/// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` is `IsDbResult`, `DataTable` or `DataRow`.</returns>
		public static bool IsDbResult (ref object obj, ref Type objType) {
			if (obj is DataSet || obj is DataTable || obj is DataRow) return true;
            return false;
		}
		/// <summary>
		/// True if value is `Enum`.
		/// </summary>
		/// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` is `Enum`.</returns>
		public static bool IsEnum (ref object obj, ref Type objType) {
			if (enumType.IsAssignableFrom(objType.BaseType)) return true;
			return false;
		}
		/// <summary>
		/// True if value is Type object.
		/// </summary>
		/// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` is `Type` object.</returns>
        public static bool IsTypeObject (ref object obj, ref Type objType) {
            if (obj is _Type) return true;
            return false;
		}
        /// <summary>
        /// True if value is implementing IList | System.Array | System.Collections.ArrayList | System.Data.Common.DbDataReader.
        /// </summary>
        /// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` implements `IEnumerable`.</returns>
        public static bool IsEnumerable (ref object obj, ref Type objType) {
			// https://docs.microsoft.com/cs-cz/dotnet/api/system.collections.ienumerable?view=netframework-4.8
			if (obj is IEnumerable) return true;
            return false;
		}
		/// <summary>
		/// True if value is ICollection.
		/// </summary>
		/// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` implements `ICollection`.</returns>
		public static bool IsCollection (ref object obj, ref Type objType) {
			if (obj is ICollection) return true;
			return false;
		}
		/// <summary>
		/// True if obj is System.Tuple
		/// </summary>
		/// <param name="obj">Any value except null</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` is `System.Func&lt;,&gt;`.</returns>
		public static bool IsTuple(ref object obj, ref Type objType) {
			// this string comparison is because of backward compatibility for .NET 4.0+
			//if (objType.FullName.IndexOf("System.Tuple") == 0) return true;
			string objTypefullName = objType.FullName;
			int sysTupleLen = Detector.systemTupleStrLen;
			if (objTypefullName.Length >= sysTupleLen && Detector.systemTupleStr == objTypefullName.Substring(0, sysTupleLen)) return true;
			return false;
		}
		/// <summary>
		/// True if obj is System.Func&lt;&gt;.
		/// </summary>
		/// <param name="obj">Any value except null</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` is `System.Func&lt;&gt;`.</returns>
		public static bool IsFunc(ref object obj, ref Type objType) {
			if (obj.ToString().IndexOf("System.Func`") == 0) return true;
			return false;
		}
		/// <summary>
		/// True if obj is System.Delegate.
		/// </summary>
		/// <param name="obj">Any value except null</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` is `System.Delegate`.</returns>
		public static bool IsDelegate(ref object obj, ref Type objType) {
			if (
				Detector.multicastDelegateType.IsAssignableFrom(objType.BaseType) || 
				Detector.delegateType.IsAssignableFrom(objType.BaseType)
			) return true;
			return false;
		}
		/// <summary>
		/// True if value is MethodInfo | PropertyInfo | FieldInfo | EventInfo | MemberInfo | ConstructorInfo.
		/// </summary>
		/// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if value is MethodInfo | PropertyInfo | FieldInfo | EventInfo | MemberInfo | ConstructorInfo.</returns>
		public static bool IsReflectionObject (ref object obj, ref Type objType) {
            if (obj is _MemberInfo) return true;
            return false;
		}
		/// <summary>
		/// True if `obj` implements `IFormattable` or if `obj` is `Stringbuilder`
		/// </summary>
		/// <param name="obj">Any value except null.</param>
		/// <param name="objType">Type object for value or null.</param>
		/// <returns>True if `obj` implements `IFormattable` or if `obj` is `Stringbuilder`</returns>
		public static bool IsExtraFormatedObject (ref object obj, ref Type objType) {
            if (obj is IFormattable || obj is StringBuilder || obj is System.Guid) return true;
            return false;
		}
	}
}
