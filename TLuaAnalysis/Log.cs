using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLua
{
    /// <summary>
    /// 可能是因为MPFProj的原因。向VS的OutOut窗口输出，总是有些问题。
    /// 我们在Output窗口新建一个TLua的面板，专门输出TLua相关的信息。包括日志，编译输出等。
    /// 需要使用的工程很多，放到底层库里面。由上层实现接口
    /// 奇怪：写了自己的Log之后，VS的Output的build窗口也能正常输出了
    /// </summary>
    public class Log
    {

        public abstract class ILogger
        {
            /// 自动加入回车
            public abstract void WriteLine(string msg);

            /// 直接输出
            public abstract void Write(string msg);

            public abstract void Debug(string msg);

            public abstract void ActivateOuputWin();

            public abstract void ClearOutputWin();
 
        }

        static ILogger s_Logger;

        public static void Init( ILogger logger)
        {
            s_Logger = logger;
        }

        public static void WriteLine(string msg)
        {
            if (s_Logger != null)
            {
                s_Logger.WriteLine(msg);
            }
            
        }

        public static void Write(string msg)
        {
            if (s_Logger != null)
            {
                s_Logger.Write(msg);
            }
        }

        public static void Debug(string msg)
        {
            if (s_Logger != null)
            {
                s_Logger.Debug(msg);
            }
        }

        public static void ActivateOuputWin()
        {
            if (s_Logger != null)
            {
                s_Logger.ActivateOuputWin();
            }
        }

        public static void ClearOutputWin()
        {
            if (s_Logger != null)
            {
                s_Logger.ClearOutputWin();
            }
        }
    }
}
