using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    public class UsingStatement : Chunk
    {
        public AssignmentStatement Vars = null;

        public UsingStatement(Scope s)
            : base(new Scope(s))
        {

        }

        public override Statement Simplify()
        {
            Vars.Simplify();
            return base.Simplify();
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Vars != null)
            {
                Vars.Accept(nv);
            }
            base.Traverse(nv);
        }
    }
}
