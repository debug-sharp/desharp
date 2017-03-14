using System;

namespace Desharp {
	public struct DebugConfig {
        public int? Depth;
        public bool? Enabled;
		public string Directory;
		public int? LogWriteMilisecond;
		public OutputType OutputType;
		public EnvType EnvType;
		public Type[] Panels;
	}
}
