using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    /// <summary>
    ///  表示一个类的描述。 C#和Lua的类，属性和函数的名称都要求唯一性。但是有差异：
    ///  1、C#支持函数重载而Lua不支持
    ///  2、Lua的实例变量和类变量可以重名。C#不可以。从Lua class的实现只是用元表来模拟。只是__index表，而没有__newindex表
    /// 我们生成或者显示类的描述的时候，不处理同名的情况。同名的情况是语法检查来决定的。
    /// 对于类中含子类的情况，使用AddMember直接添加
    /// </summary>
    public class LuaClassDeclaration : TypeDeclaration
    {
        Dictionary<string, Declaration> m_MemberSet = new Dictionary<string, Declaration>();

        protected bool m_isLocalTable = false;

        public bool IsLocalTable {
            get
            {
                return m_isLocalTable;
            }
            set {
                m_isLocalTable = value;
            }
        }

        // 类的基类名
        public virtual string BaseType { get; set; }

        // 类的全名 
        public virtual string FullName { get; set; }

        public virtual ICollection<Declaration> Members { get { return m_MemberSet.Values; } }

        protected override void DeepCopy(Declaration src)
        {
            base.DeepCopy(src);
            LuaClassDeclaration from = src as LuaClassDeclaration;
            this.m_MemberSet = new Dictionary<string, Declaration>();
            foreach(var item in from.m_MemberSet){
                this.m_MemberSet.Add(item.Key, item.Value.DeepClone());
            }
        }

        public override void ReplaceTemplateTypes(string parentName, List<string> realTypes)
        {
            base.ReplaceTemplateTypes(parentName, realTypes);

            this.BaseType = parentName;

            foreach (var item in this.m_MemberSet)
            {
                item.Value.ReplaceTemplateTypes(this.FullName, realTypes);
            }
        }

        public virtual Declaration GetMember(string name)
        {
            Declaration decl;
            if (m_MemberSet.TryGetValue(name, out decl))
            {               
                return decl;
            }
            else
            {
                return null;
            }
        }

        /// 存储用
        public virtual void AddMember(Declaration decl)
        {
            if (decl == null || string.IsNullOrEmpty(decl.Name))
                return;

            m_MemberSet[decl.Name] = decl;
        }

        public virtual bool AddProperty(VariableDeclaration decl, bool isStatic)
        {
            if (decl == null || string.IsNullOrEmpty(decl.Name) )
                return false;

            m_MemberSet[decl.Name] = decl;

            decl.IsStatic = isStatic;
            decl.ParentClassFullName = this.FullName;

            return true;
        }

        public virtual VariableDeclaration GetProperty(string name, bool isStatic)
        {
            VariableDeclaration decl = GetMember(name) as VariableDeclaration;
            if (decl!=null)
            {
                if (decl.IsStatic != isStatic)
                {
                    return null;
                }
                else
                {
                    return decl;
                }
            }
            else
            {
                return null;
            }
            
        }

        public virtual bool AddFunction(FunctionDeclaration decl, bool isStatic, bool AllowOverloading = false)
        {
            if( decl==null || string.IsNullOrEmpty(decl.Name) )
                return false;

            FunctionDeclaration mainFunc = GetMember(decl.Name) as FunctionDeclaration;
            if (AllowOverloading && mainFunc !=null )
            {
                mainFunc.AddOverloadFunction(decl);
            }
            else
            {
                m_MemberSet[decl.Name] = decl;
            }


            decl.ParentClassFullName = this.FullName;
            decl.IsStatic = isStatic;

            return true;

        }

        public override void Accept(DeclarationVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(DeclarationVisitor nv)
        {
            foreach(var item in m_MemberSet)
            {
                item.Value.Accept(nv);
            }
        }

    }
}
