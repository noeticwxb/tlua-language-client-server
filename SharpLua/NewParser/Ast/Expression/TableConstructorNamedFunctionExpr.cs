﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpLua.Ast.Statement;

namespace SharpLua.Ast.Expression
{
    public class TableConstructorNamedFunctionExpr : Expression
    {
        public FunctionStatement Value;

        public override Expression Simplify()
        {
            Value.Simplify(); // FunctionStatements do not simplify into something else
            return this;
        }

        public override void Traverse(NodeVisitor nv)
        {
            if(Value!=null)
            {
                Value.Accept(nv);
            }

            base.Traverse(nv);
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
