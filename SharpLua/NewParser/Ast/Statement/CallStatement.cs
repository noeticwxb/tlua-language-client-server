using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    public class CallStatement : Statement
    {
        // Is a CallExpr
        public Expression.Expression Expression = null;

        public override Statement Simplify()
        {
            Expression = Expression.Simplify();
            return base.Simplify();
        }

        public override void Traverse(NodeVisitor nv)
        {
            if(Expression!=null)
            {
                Expression.Accept(nv);
            }
            base.Traverse(nv);
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
