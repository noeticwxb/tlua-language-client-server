﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class BoolExpr : Expression
    {
        public bool Value = false;

        public BoolExpr() { }
        public BoolExpr(bool value) { Value = value; }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
