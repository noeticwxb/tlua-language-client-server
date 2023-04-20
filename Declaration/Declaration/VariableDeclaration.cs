using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    /// <summary>
    /// 表示一个变量
    /// </summary>
    public class VariableDeclaration: Declaration
    {
        string m_Type = AnalysisConfig.T_vary;

        /// <summary>
        /// 变量类型
        /// </summary>
        public string Type
        {
            get
            {
                return m_Type;
            }
            set
            {
                m_Type = value;
                if(string.IsNullOrEmpty(m_Type))
                {
                    m_Type = AnalysisConfig.T_vary;
                }
            }
        }

        public string ParentClassFullName
        {
            get;
            set;
        }


        /// <summary>
        /// 变量是否是只读的
        ///     1、为了更好的支持Native库导入Lua的接口。比如对于C#，只读属性意味着，只有get，没有set操作
        /// </summary>
        bool m_IsReadOnly = false;
        public bool ReadOnly
        {
            get { return m_IsReadOnly; }
            set { m_IsReadOnly = value; }
        }

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

        public override string GetDescriptionWithComment()
        {
            String desc = base.GetDescriptionWithComment();

            if (!string.IsNullOrEmpty(desc) && desc.IndexOf(AnalysisConfig.T_vary) !=-1 && this.Type != AnalysisConfig.T_vary)
            {
                desc = desc.ReplaceFirst(AnalysisConfig.T_vary, this.Type);
            }

            return desc;

        }

        public override void ReplaceTemplateTypes(string parentName, List<string> realTypes)
        {
            base.ReplaceTemplateTypes(parentName, realTypes);
            this.ParentClassFullName = parentName;
            this.Type = ReplaceTemplateType(this.Type, realTypes);
            return;
        }

        public override void Accept(DeclarationVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
