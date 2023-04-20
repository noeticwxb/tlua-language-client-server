using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TLua;

namespace TLuaServer
{
    class TLuaLog: TLua.Log.ILogger
    {
        public override void WriteLine(string msg)
        {
            System.Console.WriteLine(msg);
        }

        /// 直接输出
        public override void Write(string msg)
        {
            System.Console.WriteLine(msg);
        }

        public override void Debug(string msg)
        {
            System.Console.WriteLine(msg);
        }

        public override void ActivateOuputWin()
        {
            return;
        }

        public override void ClearOutputWin()
        {
            return;
        }
    }
}
