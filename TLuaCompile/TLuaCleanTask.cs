using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace TLua.TLuaCompile
{
    public class TLuaCleanTask : Microsoft.Build.Utilities.Task
    {
        private ITaskItem[] m_Items;

        [Required]
        // Directories to create. 
        public ITaskItem[] Items
        {
            get
            {
                return m_Items;
            }

            set
            {
                m_Items = value;
            }
        }

        private string m_BuildOutputPath;

        [Required]
        public string BuildOutputPath
        {
            get { return m_BuildOutputPath; }
            set { m_BuildOutputPath = value; }
        }

        //public void Log

        public void LogMessage(string msg,
            string file = null,
            int lineNumber = 0, int columNumber = 0,
            int endLineNumber = 0, int endColumNumber = 0,
            string subcategory = null, string code = null, string helpKeyword = null)
        {
            TLua.Log.WriteLine(msg);
            //Log.LogCriticalMessage(subcategory, code, helpKeyword, file, lineNumber, columNumber, endLineNumber, endColumNumber, msg);
            Log.LogMessage(msg);
        }

        public void LogWarnning(string msg,
            string file = null,
            int lineNumber = 0, int columNumber = 0,
            int endLineNumber = 0, int endColumNumber = 0,
            string subcategory = null, string code = null, string helpKeyword = null)
        {
            TLua.Log.WriteLine("Warning: " + msg);
            Log.LogWarning(subcategory, code, helpKeyword, file, lineNumber, columNumber, endLineNumber, endColumNumber, msg);

        }

        public void LogError(string msg,
            string file = null,
            int lineNumber = 0, int columNumber = 0,
            int endLineNumber = 0, int endColumNumber = 0,
            string subcategory = null, string code = null, string helpKeyword = null)
        {
            TLua.Log.WriteLine("Error: " + msg);
            Log.LogError(subcategory, code, helpKeyword, file, lineNumber, columNumber, endLineNumber, endColumNumber, msg);
        }


        //public string SourceDir { get; set; }

        public string DestDir { get; set; }


        public override bool Execute()
        {
            /// 对于.tlua 结尾的文件，编译成lua文件
            /// 对于工程中的其他文件，直接拷贝到目的文件夹

            if (string.IsNullOrEmpty(BuildOutputPath))
            {
                DestDir = "Bin\\Output";
            }
            else
            {
                char[] crim = { '\\', '/' };
                DestDir = BuildOutputPath.TrimEnd(crim);
            }


            foreach (ITaskItem node in Items)
            {
                string source_FullPath = node.GetMetadata("FullPath");      // Example: "C:\MyProject\Source\Program.cs"
                string source_Extension = node.GetMetadata("Extension");    // Example:   ".cs"
                string source_RelativePath = node.GetMetadata("RelativeDir");   //  Example: "Source\"
                string source_FileName = node.GetMetadata("Filename");          // Example: "Program"

                if (source_Extension == ".tlua")
                {
                    string dest_FullPath = DestDir + "\\" + source_RelativePath + source_FileName + ".lua";
                    if (System.IO.File.Exists(dest_FullPath))
                    {
                        System.IO.File.Delete(dest_FullPath);
                    }
                }
                else
                {
                    string dest_FullPath = DestDir + "\\" + source_RelativePath + source_FileName + source_Extension;
                    if (System.IO.File.Exists(dest_FullPath))
                    {
                        System.IO.File.Delete(dest_FullPath);
                    }
                }

            }


            return true;

        }
    }
}