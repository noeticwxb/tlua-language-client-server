using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    public class LuaClassProxyDeclaration: LuaClassDeclaration
    {
        LuaClassDeclaration m_Agent;

        public LuaClassProxyDeclaration(LuaClassDeclaration agent, string proxyName, string proxyDisplayName)
        {
            m_Agent = agent;
            this.Name = proxyName;
            this.DisplayText = proxyDisplayName;
        }

        // 类的基类名
        public override string BaseType 
        {
            get { return m_Agent.BaseType; }
            set { m_Agent.BaseType = value; }
        }

        // 类的全名 
        public override string FullName
        {
            get { return m_Agent.FullName; }
            set { m_Agent.FullName = value; }
        }

        public override ICollection<Declaration> Members 
        {
            get { return m_Agent.Members; } 
        }

        public override string FileName
        {
            get { return m_Agent.FileName; }
            set { m_Agent.FileName = value; }
        }

        /// 行数
        public override int Line
        {
            get { return m_Agent.Line; }
            set { m_Agent.Line = value; }
        }

        /// 列数
        public override int Col
        {
            get { return m_Agent.Col; }
            set { m_Agent.Col = value; }
        }

        /// <summary>
        /// 表达标识符的图标索引
        /// </summary>
        public override int TypeImageIndex
        {
            get { return m_Agent.TypeImageIndex; }
            set { m_Agent.TypeImageIndex = value; }
        }

        /// <summary>
        /// 标识符的提示/说明文字
        /// </summary>
        public override string Description
        {
            get { return m_Agent.Description; }
            set { m_Agent.Description = value; }
        }

        public override string CommentText
        {
            get { return m_Agent.CommentText; }
            set { m_Agent.CommentText = value; }
        }

        public override Declaration GetMember(string name)
        {
            return m_Agent.GetMember(name);
        }

        public override bool AddProperty(VariableDeclaration decl, bool isStatic)
        {
            return m_Agent.AddProperty(decl, isStatic);
        }

        public override VariableDeclaration GetProperty(string name, bool isStatic)
        {
            return m_Agent.GetProperty(name, isStatic);
        }

        public override bool AddFunction(FunctionDeclaration decl, bool isStatic, bool AllowOverloading = false)
        {
            return m_Agent.AddFunction(decl, isStatic, AllowOverloading);
        }

        public override void Accept(DeclarationVisitor nv)
        {
            m_Agent.Accept(nv);
        }

        public override void Traverse(DeclarationVisitor nv)
        {
            m_Agent.Traverse(nv);
        }
    }
}
