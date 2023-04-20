using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    public class ReturnStatement : Statement
    {
        public List<Expression.Expression> Arguments = null;

        public override Statement Simplify()
        {
            for (int i = 0; i < Arguments.Count; i++)
                Arguments[i] = Arguments[i].Simplify();
            return base.Simplify();
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if(Arguments!=null)
            {
                foreach(var item in Arguments)
                {
                    if (item!=null)
                    {
                        item.Accept(nv);
                    }
                    
                }

            }
            base.Traverse(nv);
        }
    }
}
