using System.Collections.Generic;

namespace Desharp {
	internal class RouteTarget {
		internal string Controller;
		internal string Action;
		internal string FullName;
		internal string[] Namespaces;
		internal string[] NamespacesLower;
		internal string Namespace;
		internal string NamespaceLower;
		internal Dictionary<string, RouteTargetArg> Params;
	}
}
