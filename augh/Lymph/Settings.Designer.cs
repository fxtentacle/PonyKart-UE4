﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Lymph {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("media/gui/")]
        public string MiyagiResources_file_location {
            get {
                return ((string)(this["MiyagiResources_file_location"]));
            }
            set {
                this["MiyagiResources_file_location"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("media/worlds/")]
        public string SaveFileLocation {
            get {
                return ((string)(this["SaveFileLocation"]));
            }
            set {
                this["SaveFileLocation"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Level0")]
        public string FirstLevelName {
            get {
                return ((string)(this["FirstLevelName"]));
            }
            set {
                this["FirstLevelName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".save")]
        public string SaveFileExtension {
            get {
                return ((string)(this["SaveFileExtension"]));
            }
            set {
                this["SaveFileExtension"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".muffin")]
        public string DefaultSaveFileExtension {
            get {
                return ((string)(this["DefaultSaveFileExtension"]));
            }
            set {
                this["DefaultSaveFileExtension"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("media/scripts/")]
        public string LevelScriptLocation {
            get {
                return ((string)(this["LevelScriptLocation"]));
            }
            set {
                this["LevelScriptLocation"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("/init/")]
        public string LevelScriptFolderExtension {
            get {
                return ((string)(this["LevelScriptFolderExtension"]));
            }
            set {
                this["LevelScriptFolderExtension"] = value;
            }
        }
    }
}
