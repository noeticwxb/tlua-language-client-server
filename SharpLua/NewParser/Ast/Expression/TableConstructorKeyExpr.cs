using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class TableConstructorKeyExpr : Expression
    {
        public Expression Key = null;
        public string KeyType = null; 
        public Expression Value = null;

        public override Expression Simplify()
        {
            Key = Key.Simplify();
            Value = Value.Simplify();
            return this;
        }

        public override void Traverse(NodeVisitor nv)
        {
            if( Key!=null)
            {
                Key.Accept(nv);
            }

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
