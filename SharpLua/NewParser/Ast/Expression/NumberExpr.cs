using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class NumberExpr : Expression
    {
        public string Value;

        public NumberExpr() { }
        public NumberExpr(string value) { Value = value; }
        public NumberExpr(double value) { Value = value.ToString(); }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        //显示本节点的log信息
        public override string ToLogString(int depth = 0)
        {
            string result = "";
            result += "[" + GetType().Name + "]" + " Value:" + Value;
            return result;
        }
    }
}
