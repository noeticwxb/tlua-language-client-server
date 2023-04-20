using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    /// <summary>
    /// Chunk可能是一个文件，或者是一个命名空间。ChunkDeclaration和TLuaClassDeclaration区分不是特别清晰。
    /// 对于C#而言，必须都是类。静态函数也必须在类中
    /// 对于C++而言，静态函数和变量可以仅仅在命名空间中
    /// </summary>
    public class ChunkDeclaration: Declaration
    {
        /// <summary>
        /// 记录当前Chunk的全局变量/全局函数， 类定义等
        /// </summary>
        Dictionary<string, Declaration> m_Globals = new Dictionary<string, Declaration>();

        /// <summary>
        /// Local的数据是不存储的，临时使用
        /// </summary>
        Dictionary<string, Declaration> m_Locals = new Dictionary<string, Declaration>();

        /// using UnityEngine
        List<List<string>> m_UsingNameSpace = new List<List<string>>();

        /// GameObject as ALIAS = UnityEning.GameObject
        Dictionary<string, List<string>> m_Alias = new Dictionary<string,List<string>>();


        public SharpLua.Ast.Chunk Chunk
        {
            get;
            set;
        }


        public virtual ICollection<Declaration> Globals
        {
            get { return m_Globals.Values; }
        }

        public virtual ICollection<Declaration> Locals
        {
            get { return m_Locals.Values; }
        }

        public virtual ICollection<List<string>> UsingNameSpace 
        {
            get { return m_UsingNameSpace; }
        }

        public virtual ICollection<KeyValuePair<string, List<string>>> Alias
        {
            get { return m_Alias; }
        }

        protected override void DeepCopy(Declaration src)
        {
            throw new Exception("ChunkDeclaration not support DeepCopy now");
        }


        public virtual void AddUsingNameSpace(List<String> namespaceChain)
        {
            if (namespaceChain != null && namespaceChain.Count != 0)
            {
                m_UsingNameSpace.Add(namespaceChain);
            }
        }

        public virtual void AddAlias(string aliasName, List<String> namespaceChain)
        {
            if (!string.IsNullOrEmpty(aliasName)
                && namespaceChain != null && namespaceChain.Count != 0)
            {
                m_Alias[aliasName] = namespaceChain;
            } 
        }

        public virtual bool AddGlobals(Declaration decl, bool updateOld = true)
        {
            if (decl == null || string.IsNullOrEmpty(decl.Name) || decl == this)
                return false;

            if (updateOld || !m_Globals.ContainsKey(decl.Name))
            {
                m_Globals[decl.Name] = decl;
                return true;
            }
            else
            {
                return false;
            }
            
        }

        public virtual void CombineGlobals(ChunkDeclaration declCombine)
        {
            if (declCombine == null || string.IsNullOrEmpty(declCombine.Name) || declCombine == this)
                return;

            foreach(var item in declCombine.Globals)
            {
                if (m_Globals.ContainsKey(item.Name))
                {
                    Declaration declOld = m_Globals[item.Name];

                    if (declOld is ChunkDeclaration && item is ChunkDeclaration)
                    {
                        ChunkDeclaration chunkOld = declOld as ChunkDeclaration;
                        ChunkDeclaration chunkNew = item as ChunkDeclaration;
                        chunkOld.CombineGlobals(chunkNew);
                    }
                    else
                    {
                        m_Globals[item.Name] = item;
                    }
                }
                else
                {
                    m_Globals[item.Name] = item;
                }
            }
        }

        public virtual bool AddGlobalsAlias(string alias, Declaration decl, bool updateOld = true)
        {
            if (decl == null || string.IsNullOrEmpty(decl.Name) || decl == this || string.IsNullOrEmpty(alias))
                return false;

            if (updateOld || !m_Globals.ContainsKey(alias))
            {
                m_Globals[alias] = decl;
                return true;
            }
            else
            {
                return false;
            }
        }


        public virtual bool AddLocal(Declaration decl, bool updateOld = true)
        {
            if (decl == null || string.IsNullOrEmpty(decl.Name) || decl == this )
                return false;

            if (updateOld || !m_Locals.ContainsKey(decl.Name))
            {
                m_Locals[decl.Name] = decl;
                return true;
            }
            else 
            {
                return false;
            }
        }

        public virtual Declaration GetLocal(string name)
        {
            Declaration decl;
            if( m_Locals.TryGetValue(name, out decl) )
            {
                return decl;
            }
            else
            {
                return null;
            }
        }

        public virtual Declaration GetGlobal(string name)
        {
            Declaration decl;
            if( m_Globals.TryGetValue(name,out decl))
            {
                return decl;
            }
            else
            {
                return null;
            }
        }

        public virtual void ClearGlobal()
        {
            m_Globals.Clear();
        }

        public virtual void ClearLocal()
        {
            m_Locals.Clear();
        }

        public override void Accept(DeclarationVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(DeclarationVisitor nv)
        {
            foreach(var item in m_Locals)
            {
                item.Value.Accept(nv);
            }

            foreach(var item in m_Globals)
            {
                item.Value.Accept(nv);
            }

        }
    }
}
