using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Desharp {
    /// <summary>
    /// Add hidden attribute to hide any property or field in your class 
    /// to print it by Desharp.Debug.Dump() or Desharp.Debug.Log() methods.
    /// To print always everything, add to your (App|Web).config: 
    /// &#60;add key="Desharp:DumpCompillerGenerated" value="true" /&#62;
    /// </summary>
	[ComVisible(true)]
    public class HiddenAttribute: Attribute {
    }
}
