using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class InlineFunctionExpression : Expression
    {
        public List<Expression> Expressions = new List<Expression>();
        public bool IsVararg = false;
        public List<Variable> Arguments = new List<Variable>();

        List<string> m_ReturnTypeList = null;
        public List<string> ReturnTypeList
        {
            get
            {
                return m_ReturnTypeList;
            }
            set
            {
                m_ReturnTypeList = value;
            }
        }

        public override Expression Simplify()
        {
            for (int i = 0; i < Expressions.Count; i++)
                Expressions[i] = Expressions[i].Simplify();

            return this;
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Arguments != null)
            {
                foreach (var itemVar in Arguments)
                {
                    itemVar.Accept(nv);
                }
            }

            if (Expressions != null)
            {
                foreach (var expr in Expressions)
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
    }
}
