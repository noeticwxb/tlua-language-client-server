using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast
{
    public class Variable: AstNode
    {
        string m_Type = TLuaGrammar.T_vary;    //  默认情况下， 变量的类型T_vary

        int m_Line = -1;
        int m_Col = -1;
        bool m_IsGlobal = false;
        bool m_IsFuncParam = false;

        public int Line 
        {
            get { return m_Line; }
            set { m_Line = value; }
        }

        public int Column
        {
            get { return m_Col; }
            set { m_Col = value; }
        }

        public string Type
        {
            get
            {
                return m_Type;
            }
            set
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(m_Type));
                m_Type = value;
            }
        }

        public bool IsFuncParam 
        {
            get {return m_IsFuncParam;}
            set { m_IsFuncParam = value; }
        }

        public bool IsGlobal
        {
            get { return m_IsGlobal; }
            set { m_IsGlobal = value; }
        }

        public string Name;
        
        public int References = 0;
 
        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

    }
}
