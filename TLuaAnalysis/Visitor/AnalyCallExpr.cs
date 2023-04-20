using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLua.Analysis
{

    public class AnalyCallExpr
    {       
        /// <summary>
        /// 分析当前函数调用栈的',', 确定是在哪个函数的第几个‘，’ 
        /// </summary>
        /// <param name="tokenList"></param>
        /// <param name="bracket_index">"(" 开始的位置</param>
        /// <param name="comma_index">结束的","位置</param>
        /// <returns> 返回comma_index 对应的同一个函数调用栈上的","所在的token位置</returns>
        public static List<SharpLua.Token> AnalyComma(List<SharpLua.Token> tokenList, int bracket_index, int comma_index)
        {
            if(tokenList==null || bracket_index < 0 || bracket_index >= tokenList.Count
                || comma_index < 0 || comma_index >= tokenList.Count)
                return null;

            if(tokenList[bracket_index].Data!="("
                || tokenList[comma_index].Data!=",")
            {
                return null;
            }
            
            try
            {
                Stack< List<SharpLua.Token> > m_CommaStack = new Stack< List<SharpLua.Token> >();
                for (int index = bracket_index; index <= comma_index; ++index)
                {
                    SharpLua.Token t = tokenList[index];
                    if (t.Data == "(")
                    {
                        m_CommaStack.Push( new List<SharpLua.Token>() );
                    }
                    else if (t.Data == ")")
                    {
                        m_CommaStack.Pop();
                    }
                    else if (t.Data == ".")
                    {
                        m_CommaStack.Peek().Add(t);
                    }
                }      

                return m_CommaStack.Peek();
            }
            catch(System.Exception )
            {
                return null;
            }
   
        }
    }
}
