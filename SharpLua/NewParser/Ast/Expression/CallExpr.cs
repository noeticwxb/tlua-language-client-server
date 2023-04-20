using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class CallExpr : Expression
    {
        public Expression Base = null;
        public List<Expression> Arguments = new List<Expression>();

        int m_OpenBracketLine = -1; // line of "("
        int m_OpenBracketColumn = -1; // column of "("
        int m_CloseBracketLine = -1; // line of ")"
        int m_CloseBracketColunm = -1;// column of ")"

        public int OpenBracketLine
        {
            get { return m_OpenBracketLine; }
            set { m_OpenBracketLine = value; }
        }

        public int OpenBracketColumn
        {
            get { return m_OpenBracketColumn; }
            set { m_OpenBracketColumn = value; }
        }

        public int CloseBracketLine
        {
            get { return m_CloseBracketLine; }
            set { m_CloseBracketLine = value; }
        }

        public int CloseBracketColumn
        {
            get { return m_CloseBracketColunm; }
            set { m_CloseBracketColunm = value; }
        }


        public override Expression Simplify()
        {
            Base = Base.Simplify();
            for (int i = 0; i < Arguments.Count; i++)
                Arguments[i] = Arguments[i].Simplify();
            return this;
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Base != null)
            {
                Base.Accept(nv);
            }

            if (Arguments != null)
            {
                foreach (Expression expr in Arguments)
                {
                    if(expr!=null)
                    {
                        expr.Accept(nv);
                    }                 
                }
            }

            base.Traverse(nv);
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
