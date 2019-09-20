namespace Desharp {
    internal struct FireDumpItem {
        internal FireDumpType Type;
        internal object Content;
        internal string File;
        internal string Line;
        internal string Label;
        internal FireDumpItem (FireDumpType type, object content, string file, string line, string label) {
            this.Type = type;
            this.Content = content;
            this.File = file;
            this.Line = line;
            this.Label = label;
        }
    }
}
