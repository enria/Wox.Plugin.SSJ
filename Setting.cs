using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using Wox.Plugin;

namespace Tip {
    public class Setting {
        public string DocRoot { set; get; }
        public string DefaultDoc { set; get; } = "默认文档";
        public string DocFileSufix { set; get; } = ".md";

        readonly static string SETTING_FILE = ".setting";

        string PluginDirctory { set; get; }

        public static Setting Load(PluginInitContext context) {
            Setting st=new Setting();
            st.PluginDirctory = context.CurrentPluginMetadata.PluginDirectory;
            if (File.Exists(Path.Combine(st.PluginDirctory, SETTING_FILE))){
                string setting_json=File.ReadAllText(Path.Combine(st.PluginDirctory, SETTING_FILE));
                JavaScriptSerializer js = new JavaScriptSerializer();
                try {
                    Dictionary<string, string> setting_dic = js.Deserialize<Dictionary<string, string>>(setting_json);
                    st.DocRoot = setting_dic["root"];
                } catch {
                    st.Save();
                }
            } else {
                st.Save();
            }
            return st;
        }

        public void Save() {
            
            try {
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, string> setting_dic = new Dictionary<string, string>();
                setting_dic["root"] = DocRoot;
                string json= js.Serialize(setting_dic);
                File.WriteAllText(Path.Combine(PluginDirctory, SETTING_FILE), json);
            } catch {
                
            }
            
        }
    }
}
