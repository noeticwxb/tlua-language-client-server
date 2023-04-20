using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    public abstract class Statement: AstNode
    {
        Scope m_scope = null;
        public Scope Scope
        {
            get { return m_scope; }
            set { m_scope = value; }
        }
        public bool HasSemicolon = false;
        public int LineNumber = 0;

        public List<Token> ScannedTokens = new List<Token>();
        public Token SemicolonToken;

        public virtual Statement Simplify()
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
