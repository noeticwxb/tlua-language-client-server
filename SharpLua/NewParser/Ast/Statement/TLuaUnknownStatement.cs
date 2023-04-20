using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    /// <summary>
    /// 用于用户输入不合法的代码时，生成一个未决的表达式。
    /// 用于是为了在自动补全时，能够尽可能多的去分析用户输入的代码
    /// </summary>
    public class TLuaUnknownStatement: Statement
    {
        public Expression.Expression Expression;

        public override void Traverse(NodeVisitor nv)
        {
            if (Expression != null)
            {
                Expression.Accept(nv);
            }

            base.Traverse(nv);
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

    }
}
