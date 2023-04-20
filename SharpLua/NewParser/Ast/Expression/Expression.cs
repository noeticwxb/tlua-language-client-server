using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public abstract class Expression : AstNode
    {
        public int ParenCount = 0;
        public Scope Scope { get; set; }

        public virtual Expression Simplify()
        {
            return this;
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        //显示本节点的log信息
        public virtual string ToLogString(int depth = 0)
        {
            string result = "";
            result += "[" + GetType().Name + "]";
            return result;
        }
    }
}
