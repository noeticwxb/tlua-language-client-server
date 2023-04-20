using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class TableConstructorExpr : Expression
    {
        public List<Expression> EntryList = new List<Expression>();

        int m_col = 0;
        int m_Line = 0;
        string m_id;


        public int Column
        {
            get
            {
                return m_col;
            }
        }

        public int Line
        {
            get
            {
                return m_Line;
            }
        }

        public string Id
        {
            get
            {
                return m_id;
            }
        }

        public TableConstructorExpr(int line, int col)
        {
            m_Line = line;
            m_col = col;
            m_id = String.Format("{0}-{1}", m_Line, m_col);
        }

        public override Expression Simplify()
        {
            for (int i = 0; i < EntryList.Count; i++)
                EntryList[i] = EntryList[i].Simplify();

            return this;
        }

        public override void Traverse(NodeVisitor nv)
        {
            if(EntryList != null)
            {
                foreach(var expr in EntryList)
                {
                    expr.Accept(nv);
                }
            }
            base.Traverse(nv);
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        //显示本节点的log信息
        public override string ToLogString(int depth = 0)
        {
            string result = "";
            result += "-".Repeat(depth);
            result += "[" + GetType().Name + "]" + "\n";

            foreach (var entry in EntryList)
            {
                result += (entry.ToLogString(depth+1));
            }
            return result;
        }
    }
}
