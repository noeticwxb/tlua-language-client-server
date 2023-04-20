using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    /// <summary>
    /// 所有类型声明的基类
    /// </summary>
    public class TypeDeclaration:Declaration
    {
        public TypeDeclaration(string name):base(name)
        {
            base.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.TLuaType);
        }

        public TypeDeclaration(): base()
        {
            base.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.TLuaType);
        }
    }
}
