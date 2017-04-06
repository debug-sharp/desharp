namespace Desharp {
    using System;
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Assets {
        private static global::System.Resources.ResourceManager resourceMan;
        private static global::System.Globalization.CultureInfo resourceCulture;
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Assets() {
        }
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Desharp.Assets", typeof(Assets).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        internal static string bar_css {
            get {
                return ResourceManager.GetString("bar_css", resourceCulture);
            }
        }
        internal static string bar_exception_css {
            get {
                return ResourceManager.GetString("bar_exception_css", resourceCulture);
            }
        }
        internal static string bar_panels_css {
            get {
                return ResourceManager.GetString("bar_panels_css", resourceCulture);
            }
        }
        internal static string bar_window_css {
            get {
                return ResourceManager.GetString("bar_window_css", resourceCulture);
            }
        }
        internal static string dumps_css {
            get {
                return ResourceManager.GetString("dumps_css", resourceCulture);
            }
        }
        internal static string dumps_js {
            get {
                return ResourceManager.GetString("dumps_js", resourceCulture);
            }
        }
        internal static string error {
            get {
                return ResourceManager.GetString("error", resourceCulture);
            }
        }
        internal static string exception_css {
            get {
                return ResourceManager.GetString("exception_css", resourceCulture);
            }
        }
        internal static string logs_css {
            get {
                return ResourceManager.GetString("logs_css", resourceCulture);
            }
        }
        internal static string logs_js {
            get {
                return ResourceManager.GetString("logs_js", resourceCulture);
            }
        }
    }
}
