using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace TLuaServer
{
    class ProjectConfig { 
        public string Name { get; set; }
        public string Path { get; set; }
    }



    public partial class TLuaLSPServer : INotifyPropertyChanged, IDisposable
    {
        Dictionary<string, object> m_WorkSpaceConfig;
        string m_outputPath;
        string m_rootPath;
        Dictionary<string,ProjectConfig> m_projectConfig = new Dictionary<string, ProjectConfig> ();

        void resetConfig()
        {
            m_WorkSpaceConfig = null;
            m_rootPath = null;
            m_outputPath = null;
            m_projectConfig = new Dictionary<string, ProjectConfig>();
        }

        void readWorkspaceConfig(string rootPath)
        {
            try
            {
                resetConfig();

                m_rootPath = rootPath;

                LogInfo(string.Format("open workspace path = {0}", rootPath));
                string config_path = Path.Combine(rootPath, "tlua_config.json");
                if (System.IO.File.Exists(config_path))
                {
                    var config_content = File.ReadAllText(config_path);
                    m_WorkSpaceConfig = LitJson.JsonMapper.ToObject<Dictionary<string, object>>(config_content);
                    if (m_WorkSpaceConfig == null)
                    {
                        LogError(string.Format("not read tlua workspace config {0}", config_path));
                        return;
                    }              
                }
              
                object output_path;
                if(m_WorkSpaceConfig.TryGetValue("output",out output_path))
                {
                    m_outputPath = output_path.ToString();
                }

                readProjectsConfig();
                readExternDeclConfig();

                // default one project
                if(m_projectConfig.Count == 0)
                {
                    ProjectConfig config = new ProjectConfig();
                    config.Name = "tlua_default";
                    config.Path = string.Empty;
                    m_projectConfig[config.Name] = config;
                }
                readProjectDecl();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }      
        }

        private void readProjectsConfig()
        {        
            object val;
            if(m_WorkSpaceConfig.TryGetValue("projects", out val))
            {
                m_projectConfig = new Dictionary<string, ProjectConfig>();
                object[] project = val as object[];
                foreach (object item in project)
                {
                    readOneProjectConfig(item);
                }
            }
        }

        void readOneProjectConfig(object item)
        {
            try
            {
                Dictionary<string, object> item_config = item as Dictionary<string, object>;
                if (item_config == null)
                {
                    LogError("Error Project Config " + item.ToString());
                    return;
                }
                string name = item_config["name"].ToString();
                string path = item_config["path"].ToString();

                ProjectConfig config = new ProjectConfig();
                config.Name = name;
                config.Path = path;

                if (m_projectConfig.ContainsKey(config.Name))
                {
                    LogError("repeat project name " + config.Name);
                }
                m_projectConfig[config.Name] = config;
            }
            catch(SystemException ex)
            {
                LogException(ex);
            }
        }

        private void readExternDeclConfig()
        {
            object val;
            if (m_WorkSpaceConfig.TryGetValue("decls", out val))
            {
                object[] project = val as object[];
                foreach (object item in project)
                {
                    var decl_name = item.ToString();
                    var decl_path = Path.Combine(m_rootPath, decl_name);

                }
            }
        }

        private void readProjectDecl()
        {

        }
    }
}


