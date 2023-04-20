using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class VariableExpression : Expression
    {
        public Variable Var;

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Var != null)
                Var.Accept(nv);

            base.Traverse(nv);
        }

        //显示本节点的log信息
        public override string ToLogString(int depth = 0)
        {
            string result = "";
            result += "[" + GetType().Name + "]" + " Var.Name:" + Var.Name;
            return result;
        }
    }
}
