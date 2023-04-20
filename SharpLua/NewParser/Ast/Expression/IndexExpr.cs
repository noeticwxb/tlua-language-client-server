using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class IndexExpr : Expression
    {
        public Expression Base = null;
        public Expression Index = null;

        public override Expression Simplify()
        {
            Base = base.Simplify();
            Index = Index.Simplify();

            return this;
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Base != null)
            {
                Base.Accept(nv);
            }

            if (Index != null)
            {
                Index.Accept(nv);
            }

            base.Traverse(nv);
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
