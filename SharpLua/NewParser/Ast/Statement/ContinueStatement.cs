using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    public class ContinueStatement : Statement
    {
        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
