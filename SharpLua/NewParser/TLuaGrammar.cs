using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua
{

    public class TLuaGrammar
    {
        #region 语法关键字
        ///  语法关键字
        public const string T_as = "as"; //  关键字，用于声明类型标注
        public const string T_using = "using";      // 用于向某个chunk导入Native库的命名空间
        #endregion

        #region 内置类型说明

        /// 函数返回值专用
        public const string T_void = "VOID";        // 用于指明函数没有返回值
        public const string T_number = "NUMBER"; //  数字类型关键字
        public const string T_string = "STRING"; //  字符串类型关键字
        public const string T_vary = "VARY"; //  易变类型关键字。即在编译器不确定，任何类型都可以。是否正确，在运行期判断
        public const string T_bool = "BOOL";        // bool 对象
        public const string T_typeclass = "TCLASS"; //   Lua类定义专用. 标示某个表对象是一个类定义而不是变量
        public const string T_Alias = "ALIAS";  // 导入一个别名
        public const string T_FUNCTION = "FUNCTION"; 

        #endregion

        public const string T_ErrorMsg = "NUMBER,STRING,VARY,BOOL,TYPECLASS,ALIAS,user defined class Identifier expected";

        public const string T_ReturnErrorMsg = "function expect VOID,NUMBER,STRING,VARY,BOOL,user defined class Identifier";
         
    }
}
