using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Desharp {
	internal class RouteMatchedParam {
		internal object PrimaryValue = null;
		internal string[] PrimaryValueDescription = new[] { "" };
		internal Dictionary<string, object> ValueVariations = new Dictionary<string, object>();
	}
}
