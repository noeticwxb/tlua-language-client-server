using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class MemberExpr : Expression
    {
        public Expression Base = null;
        public string Indexer = ""; // either '.' or ':'
        public string Ident = "";

        int m_Line = -1;
        int m_Col = -1;

        int m_IndexerLine = -1;
        int m_IndexerColumn = -1;

        public int Line
        {
            get { return m_Line; }
            set { m_Line = value; }
        }

        public int Column
        {
            get { return m_Col; }
            set { m_Col = value; }
        }

        public int IndexerLine
        {
            get { return m_IndexerLine; }
            set { m_IndexerLine = value; }
        }

        public int IndexerColumn
        {
            get { return m_IndexerColumn; }
            set { m_IndexerColumn = value; }
        }


        string m_OptionalIdentType = string.Empty;    //  可选的标示符号类型. 不为空，代表定义；为空，代表只是使用，不进行解析
        public string OptionalIdentType
        {
            get { return m_OptionalIdentType; }
            set { m_OptionalIdentType = value; }
        }

        public override Expression Simplify()
        {
            Base = Base.Simplify();
            return this;
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Base != null)
            {
                Base.Accept(nv);
            }
            base.Traverse(nv);
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }


}
