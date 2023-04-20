using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    public class RepeatStatement : Chunk
    {
        public Expression.Expression Condition = null;

        public RepeatStatement(Scope s)
            : base(new Scope(s))
        {

        }

        public override Statement Simplify()
        {
            Condition = Condition.Simplify();
            return base.Simplify();
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Condition != null)
                Condition.Accept(nv);

            base.Traverse(nv);
        }
    }
}
