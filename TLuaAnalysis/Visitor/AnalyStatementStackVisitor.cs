using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLua.Ast.Statement;

namespace TLua.Analysis
{
    /// <summary>
    /// 找到当前位置的最小Statement
    /// </summary>
    public class AnalyStatementStackVisitor: SharpLua.NodeVisitor
    {

        Stack<Statement> m_StatementStack = new Stack<Statement>();

        int Line { get; set; }
        int Column { get; set; }

        public Statement Result 
        {
            get
            {
                if (m_StatementStack.Count == 0)
                {
                    return null;
                }
                else
                {
                    return m_StatementStack.Peek();
                }
            }
        }

        public FunctionStatement ResultFunc
        {
            get
            {
                return Result as FunctionStatement;
            }
        }

        public bool IsFunctionDefine {
            get { return ResultFunc != null; }
        }

        /// 从栈中获取第一个FunctionStatement
        public FunctionStatement GetLastFunction()
        {
            var itor = m_StatementStack.GetEnumerator();
            while (itor.MoveNext())
            {
                FunctionStatement funcSmt = itor.Current as FunctionStatement;
                if (funcSmt != null)
                {
                    return funcSmt;
                }
            }

            return null;
        }

        public void Analy(Statement smt, int line, int col)
        {
            if (smt == null)
                return;

            Line = line;
            Column = col;

            smt.Accept(this);
        }

        public override void Apply(Statement smt)
        {
            if(VisitorHelper.IsIncludeInStatement(smt,Line,Column))
            {
                m_StatementStack.Push(smt);
            }

            base.Apply(smt);
        }

        // if语句包含很多子语句，比如elseif else什么的。如果返回一个elseif语句，后面的解析不能当做一个完整的语句处理。所以返回整个if语句
        public override void Apply(SubIfStmt smt)
        {
            return;
            //base.Apply(smt);
        }
    }
}
