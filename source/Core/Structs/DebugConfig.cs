namespace Desharp {
	public struct DebugConfig {
		public bool? Enabled;
		public string Directory;
		public OutputType OutputType;
		public EnvironmentType EnvironmentType;
		public DebugConfig (bool enabled) {
			this.Enabled = enabled;
			this.Directory = "";
			this.OutputType = OutputType.Auto;
			this.EnvironmentType = EnvironmentType.Auto;
		}
		public DebugConfig (string directory) {
			this.Enabled = null;
			this.Directory = directory;
			this.OutputType = OutputType.Auto;
			this.EnvironmentType = EnvironmentType.Auto;
		}
		public DebugConfig (OutputType outputType) {
			this.Enabled = null;
			this.Directory = "";
			this.OutputType = outputType;
			this.EnvironmentType = EnvironmentType.Auto;
		}
		public DebugConfig (EnvironmentType environmentType) {
			this.Enabled = null;
			this.Directory = "";
			this.OutputType = OutputType.Auto;
			this.EnvironmentType = environmentType;
		}

		public DebugConfig (bool enabled, string directory) {
			this.Enabled = enabled;
			this.Directory = directory;
			this.OutputType = OutputType.Auto;
			this.EnvironmentType = EnvironmentType.Auto;
		}
		public DebugConfig (bool enabled, OutputType outputType) {
			this.Enabled = enabled;
			this.Directory = "";
			this.OutputType = outputType;
			this.EnvironmentType = EnvironmentType.Auto;
		}
		public DebugConfig (bool enabled, EnvironmentType environmentType) {
			this.Enabled = enabled;
			this.Directory = "";
			this.OutputType = OutputType.Auto;
			this.EnvironmentType = environmentType;
		}
		public DebugConfig (string directory, OutputType outputType) {
			this.Enabled = null;
			this.Directory = directory;
			this.OutputType = outputType;
			this.EnvironmentType = EnvironmentType.Auto;
		}
		public DebugConfig (string directory, EnvironmentType environmentType) {
			this.Enabled = null;
			this.Directory = directory;
			this.OutputType = OutputType.Auto;
			this.EnvironmentType = environmentType;
		}
		public DebugConfig (OutputType outputType, EnvironmentType environmentType) {
			this.Enabled = null;
			this.Directory = "";
			this.OutputType = outputType;
			this.EnvironmentType = environmentType;
		}
		public DebugConfig (bool enabled, OutputType outputType, EnvironmentType environmentType) {
			this.Enabled = enabled;
			this.Directory = "";
			this.OutputType = outputType;
			this.EnvironmentType = environmentType;
		}
		public DebugConfig (string directory, OutputType outputType, EnvironmentType environmentType) {
			this.Enabled = null;
			this.Directory = directory;
			this.OutputType = outputType;
			this.EnvironmentType = environmentType;
		}
		public DebugConfig (bool enabled, string directory, OutputType outputType) {
			this.Enabled = enabled;
			this.Directory = directory;
			this.OutputType = outputType;
			this.EnvironmentType = EnvironmentType.Auto;
		}
		public DebugConfig (bool enabled, string directory, EnvironmentType environmentType) {
			this.Enabled = enabled;
			this.Directory = directory;
			this.OutputType = OutputType.Auto;
			this.EnvironmentType = environmentType;
		}
		public DebugConfig (bool enabled, string directory, OutputType outputType, EnvironmentType environmentType) {
			this.Enabled = enabled;
			this.Directory = directory;
			this.OutputType = outputType;
			this.EnvironmentType = environmentType;
		}
	}
}
