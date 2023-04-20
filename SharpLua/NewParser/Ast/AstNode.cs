using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast
{
    /// <summary>
    ///  为Vistor访问模式添加
    /// </summary>
    public class AstNode
    {
        public virtual void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public virtual void Traverse(NodeVisitor nv)
        {

        }
    }
}
