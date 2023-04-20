using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLua.Analysis
{
    public class StringHelper
    {
        public static string GetRelativeName(string fullPath, string relativePath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(relativePath)
                || relativePath.Length >= fullPath.Length)
                return fullPath;

            if (fullPath.StartsWith(relativePath))
            {
                return fullPath.Substring(relativePath.Length).TrimStart('\\','/');
            }
            else
            {
                return fullPath;
            }
        }

        public static string[] SplitLine(string code)
        {
            if (code == null)
                return null;

            return code.Split('\n');
        }

        public static string GetLine(string code, int line )
        {
            string[] lines = SplitLine(code);
            if(lines!=null && lines.Length > line)
            {
                return lines[line];
            }
            return string.Empty;
        }

        public static string GetSubLine(string code, int line, int col)
        {
            string lineCode = GetLine(code, line);

            if(col > lineCode.Length)
            {
                return lineCode;
            }
            else
            {
                return lineCode.Substring(0,col);
            }
        }

        public delegate void LogCallBack(string msg);
        public static LogCallBack _GlobalLog = null;

        public static void LogString(string msg)
        {
            if(_GlobalLog!=null)
            {
                _GlobalLog(msg);
            }
        }
    }
}
