using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    public class FunctionStatement : Chunk
    {
        public bool IsLocal = false;
        public bool IsVararg = false;
        public List<Variable> Arguments = new List<Variable>();
        public Expression.Expression Name = null;

        List<string> m_ReturnTypeList = null;
        public List<string> ReturnTypeList
        {
            get{
            return m_ReturnTypeList;
            }
            set
            {
                m_ReturnTypeList = value;
            }
        }

        public FunctionStatement(Scope s)
            //: base(s)
            : base( new Scope(s) )  //  BUGFIX: 
        {

        }

        public override Statement Simplify()
        {
            Name = Name.Simplify();
            return base.Simplify();
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if(Arguments!=null)
            {
                foreach(var item in Arguments)
                {
                    item.Accept(nv);
                }
            }

            if (Name != null)
                Name.Accept(nv);

            base.Traverse(nv);
        }
    }
}
