using System.Collections.Generic;

namespace Desharp {
	internal class RouteMatchedParam {
		internal object PrimaryValue = null;
		internal string[] PrimaryValueDescription = new[] { "" };
		internal Dictionary<string, object> ValueVariations = new Dictionary<string, object>();
	}
}
