using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    /// <summary>
    ///  用于向某个chunk中，导入Native库的命名空间。导入后，可以直接使用as来使用命名空间中的类
    ///  using UnityEngine  导入UnityEngine表中的所有类型，在当前的符号分析中可用。
    /// </summary>
    public class TLuaUsingStatement: Statement
    {
        public List<string> NameSpaceChain
        {
            get;
            set;
        }

        public TLuaUsingStatement()
        {
            NameSpaceChain = new List<string>();
        }

        public virtual Statement Simplify()
        {
            return base.Simplify();
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
