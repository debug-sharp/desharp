using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Desharp {
    /// <summary>
    /// Fire dumper message type to choose in client browser propert console method
    /// lie console.log(), console.debug(). console.trace() exc...
    /// </summary>
	[ComVisible(true)]
    public enum FireDumpType {
        /// <summary>
        /// Print value in browser console by: console.debug();
        /// </summary>
        Debug,
        /// <summary>
        /// Print value in browser console by: console.log();
        /// </summary>
        Log,
        /// <summary>
        /// Print value in browser console by: console.trace();
        /// </summary>
        Trace,
        /// <summary>
        /// Print value in browser console by: console.trace();
        /// </summary>
        Info,
        /// <summary>
        /// Print value in browser console by: console.warn();
        /// </summary>
        Warn,
        /// <summary>
        /// Print value in browser console by: console.error();
        /// </summary>
        Error,
        /// <summary>
        /// Print value in browser console by: console.table();
        /// </summary>
        Table
    }
}
