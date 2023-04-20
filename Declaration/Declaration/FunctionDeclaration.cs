using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{

    /// <summary>
    /// 表示一个函数声明
    /// </summary>
    public class FunctionDeclaration : Declaration
    {
        Dictionary<string, VariableDeclaration> m_ParamSet = new Dictionary<string, VariableDeclaration>();

        List<string> m_ReturnTypeList = new List<string>();

        /// 重载的函数列表
        List<FunctionDeclaration> m_OverloadFuncList;

        public ICollection<FunctionDeclaration> OverloadList { get { return m_OverloadFuncList; } }


        /// <summary>
        /// 是静态还是实例化的.
        ///     对于Lua而言，只有使用Lua的class框架，使用self语法糖定义的table成员，我们才认为是非静态的。其他情况都是静态的
        ///     对于Native的语言，按照Native导入到Lua层的语义来定义
        /// </summary>
        bool m_IsStatic = true;
        public bool IsStatic
        {
            get { return m_IsStatic; }
            set { m_IsStatic = value; }
        }


        public string ParentClassFullName { get; set; }

        /// <summary>
        /// 函数参数列表
        /// </summary>
        public ICollection<VariableDeclaration> ParamList
        {
            get { return m_ParamSet.Values; }
        }

        public int ParamCount
        {
            get { return m_ParamSet.Count; }
        }

        /// 函数参数是 ...
        public bool IsVararg { get; set; }

        protected override void DeepCopy(Declaration src)
        {
            base.DeepCopy(src);

            FunctionDeclaration from = src as FunctionDeclaration;
         
            this.m_ParamSet = new Dictionary<string, VariableDeclaration>();
            foreach (var item in from.m_ParamSet)
            {
                this.m_ParamSet.Add(item.Key, item.Value.DeepClone() as VariableDeclaration);
            }

            this.m_ReturnTypeList = new List<string>();
            this.m_ReturnTypeList.AddRange(from.m_ReturnTypeList);

            if (from.m_OverloadFuncList != null)
            {
                this.m_OverloadFuncList = new List<FunctionDeclaration>();
                foreach(var funcDecl in from.m_OverloadFuncList){
                    this.m_OverloadFuncList.Add(funcDecl.DeepClone() as FunctionDeclaration);
                }
            }
            else
            {
                this.m_OverloadFuncList = null;
            }

            return;
        }

        public override void ReplaceTemplateTypes(string parentName, List<string> realTypes)
        {
            base.ReplaceTemplateTypes(parentName, realTypes);
            this.ParentClassFullName = parentName;
            foreach (var item in this.m_ParamSet)
            {
                item.Value.ReplaceTemplateTypes(parentName, realTypes);
            }
            for (int i = 0; i < this.m_ReturnTypeList.Count; ++i)
            {
                this.m_ReturnTypeList[i] = this.ReplaceTemplateType(this.m_ReturnTypeList[i], realTypes);
            }
            if (m_OverloadFuncList != null)
            {
                foreach (var funcDecl in this.m_OverloadFuncList)
                {
                    funcDecl.ReplaceTemplateTypes(parentName, realTypes);
                }
            }             
        }

        /// <summary>
        /// 返回值类型列表
        /// </summary>
        public IEnumerable<String> ReturnTypeList
        {
            get { return m_ReturnTypeList; }
        }

        public void AddParam(VariableDeclaration param)
        {
            if (param == null || string.IsNullOrEmpty(param.Name))
                return;

            m_ParamSet[param.Name] = param;
            //AddLocal(param);
        }

        public void AddReturnType(string typeName)
        {
            m_ReturnTypeList.Add(typeName);
        }

        public void ClearReturnType()
        {
            m_ReturnTypeList.Clear();
        }

        public void AddOverloadFunction(FunctionDeclaration decl)
        {
            if (decl == null || decl==this)
                return;

            System.Diagnostics.Debug.Assert(decl.OverloadList == null);

            if(m_OverloadFuncList==null)
            {
                m_OverloadFuncList = new List<FunctionDeclaration>();
            }

            m_OverloadFuncList.Add(decl);
        }

        public override void Accept(DeclarationVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(DeclarationVisitor nv)
        {
            foreach(var item in m_ParamSet)
            {
                item.Value.Accept(nv);
            }

            if(m_OverloadFuncList!=null)
            {
                foreach(var item in m_OverloadFuncList)
                {
                    item.Accept(nv);
                }
            }
        }

        public void FormatFunctionDesc()
        {
            StringBuilder desc = new StringBuilder();

            string classTypeName = this.ParentClassFullName;
            if (string.IsNullOrEmpty(classTypeName))
            {
                desc.Append(AnalysisConfig.Label_GlobalFunc);
            }
            else
            {
                if (this.IsStatic)
                {
                    desc.Append(AnalysisConfig.Label_ClassStatic);
                }
            }

            if (this.m_ReturnTypeList.Count == 0)
            {
                desc.Append("...");
            }
            else
            {
                foreach (var item in this.ReturnTypeList)
                {
                    desc.Append(item);
                    desc.Append(", ");
                }

                desc.Length = desc.Length - 2;// remove ", " at last
            }

            desc.Append(" ");

            //TODO Find Parent Type
            if (!string.IsNullOrEmpty(classTypeName))
            {
                desc.Append(classTypeName);
                if (this.IsStatic)
                {
                    desc.Append(".");
                }
                else
                {
                    desc.Append(":");
                }
            }

            desc.Append(this.Name);

            desc.Append("(");

            if (this.IsVararg)
            {
                desc.Append("...");
            }
            else if (this.m_ParamSet.Count > 0)
            {
                foreach (var item in this.ParamList)
                {
                    desc.Append(item.Type);
                    desc.Append(" ");
                    desc.Append(item.Name);
                    //desc.Append(" as ");
                    //desc.Append(item.Type);
                    desc.Append(", ");
                }
                desc.Length = desc.Length - 2;// remove ", " at last
            }

            desc.Append(")");

            this.Description = desc.ToString();
        }
    }
}
