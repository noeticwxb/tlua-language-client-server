using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    /// 标示关键字声明
    public class KeywordDeclaration: Declaration
    {
        public KeywordDeclaration(string name):base(name)
        {
            base.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.KeyWorld);
        }

        public KeywordDeclaration():base()
        {
            base.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.KeyWorld);
        }
    }
}
