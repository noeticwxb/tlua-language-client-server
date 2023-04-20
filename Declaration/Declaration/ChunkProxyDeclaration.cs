using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    public class ChunkProxyDeclaration: ChunkDeclaration
    {

        ChunkDeclaration m_Agent;

        public ChunkProxyDeclaration(ChunkDeclaration agent, string proxyName, string proxyDisplayName)
        {
            m_Agent = agent;
            this.Name = proxyName;
            this.DisplayText = proxyDisplayName;
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

        public override ICollection<Declaration> Globals
        {
            get { return m_Agent.Globals; }
        }

        public override ICollection<Declaration> Locals
        {
            get { return m_Agent.Locals; }
        }

        public override ICollection<List<string>> UsingNameSpace
        {
            get { return m_Agent.UsingNameSpace; }
        }

        public override ICollection<KeyValuePair<string, List<string>>> Alias
        {
            get { return m_Agent.Alias; }
        }

        public override void AddUsingNameSpace(List<String> namespaceChain)
        {
            m_Agent.AddUsingNameSpace(namespaceChain);
        }

        public override void AddAlias(string aliasName, List<String> namespaceChain)
        {
            m_Agent.AddAlias(aliasName, namespaceChain);
        }

        public override bool AddGlobals(Declaration decl, bool updateOld = true)
        {
            return m_Agent.AddGlobals(decl, updateOld);
        }

        public override bool AddGlobalsAlias(string alias, Declaration decl, bool updateOld = true)
        {
            return m_Agent.AddGlobalsAlias(alias, decl, updateOld);
        }

        public override bool AddLocal(Declaration decl, bool updateOld = true)
        {
            return m_Agent.AddLocal(decl, updateOld);
        }

        public override void CombineGlobals(ChunkDeclaration declCombine)
        {
            m_Agent.CombineGlobals(declCombine);
        }


        public override Declaration GetLocal(string name)
        {
            return m_Agent.GetLocal(name);
        }

        public override Declaration GetGlobal(string name)
        {
            return m_Agent.GetGlobal(name);
        }

        public override void ClearGlobal()
        {
            m_Agent.ClearGlobal();
        }

        public override void ClearLocal()
        {
            m_Agent.ClearLocal();
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
