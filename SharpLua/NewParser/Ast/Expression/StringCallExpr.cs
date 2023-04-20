using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class StringCallExpr : CallExpr
    {

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
