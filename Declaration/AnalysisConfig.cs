using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    /// copy from https://msdn.microsoft.com/en-us/library/vstudio/bb165662(v=vs.110).aspx
    /// 根据实验，把Base + 改成了Base * 
    public enum IconImageIndex
    {
        // access types
        AccessPublic = 0,
        AccessInternal = 1,
        AccessFriend = 2,
        AccessProtected = 3,
        AccessPrivate = 4,
        AccessShortcut = 5,

        Base = 6,
        // Each of the following icon type has 6 versions,
        //corresponding to the access types
        Class = Base * 0,
        Constant = Base * 1,
        Delegate = Base * 2,
        Enumeration = Base * 3,
        EnumMember = Base * 4,
        Event = Base * 5,
        Exception = Base * 6,
        Field = Base * 7,
        Interface = Base * 8,
        Macro = Base * 9,
        Map = Base * 10,
        MapItem = Base * 11,
        Method = Base * 12,
        OverloadedMethod = Base * 13,
        Module = Base * 14,
        Namespace = Base * 15,
        Operator = Base * 16,
        Property = Base * 17,
        Struct = Base * 18,
        Template = Base * 19,
        Typedef = Base * 20,
        Type = Base * 21,
        Union = Base * 22,
        Variable = Base * 23,
        ValueType = Base * 24,
        Intrinsic = Base * 25,
        JavaMethod = Base * 26,
        JavaField = Base * 27,
        JavaClass = Base * 28,
        JavaNamespace = Base * 29,
        JavaInterface = Base * 30,
        // Miscellaneous icons with one icon for each type.
        // 这些值也有问题，需要全部减一
        Error = 187 - 1,
        GreyedClass = 188 - 1,
        GreyedPrivateMethod = 189 - 1,
        GreyedProtectedMethod = 190 - 1,
        GreyedPublicMethod = 191 - 1,
        BrowseResourceFile = 192 - 1 ,
        Reference = 193 - 1,
        Library = 194 - 1,
        VBProject = 195 - 1,
        VBWebProject = 196 - 1,
        CSProject = 197 - 1,
        CSWebProject = 198 - 1,
        VB6Project = 199 - 1,
        CPlusProject = 200 -1,
        Form = 201 - 1,
        OpenFolder = 202 - 1,
        ClosedFolder = 203 -1,
        Arrow = 204 -1,
        CSClass = 205-1,
        Snippet = 206 - 1,
        Keyword = 207 -1,
        Info = 208 -1,
        CallBrowserCall = 209 - 1,
        CallBrowserCallRecursive = 210 -1,
        XMLEditor = 211 - 1,
        VJProject = 212 -1,
        VJClass = 213 -1,
        ForwardedType = 214 -1,
        CallsTo = 215 -1,
        CallsFrom = 216 -1,
        Warning = 217 -1 ,
    } 

    public enum AnalysisType
    {
        TLuaType = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Type,
        KeyWorld = (int)IconImageIndex.Keyword,
        NameSpace = (int)IconImageIndex.AccessPublic +(int)IconImageIndex.Namespace,
        EnumMember = (int)IconImageIndex.AccessPublic +(int)IconImageIndex.EnumMember,
        Static_Function = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Method,
        Local_Variable = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Variable,
        Function_Param = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Variable,
        Global_Variable = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Variable,
        Class_Type = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Class,
        Class_Instace_Variable = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Property,
        Class_Static_Variable = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Property,
        Class_Instance_Function = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Method,
        Class_Static_Function = (int)IconImageIndex.AccessPublic + (int)IconImageIndex.Method,
    }

    public class AnalysisConfig
    {
        #region 内置类型说明. 需要和ShapeLua保存一致

        /// 函数返回值专用
        public const string T_void = "VOID";        // 用于指明函数没有返回值
        public const string T_number = "NUMBER"; //  数字类型关键字
        public const string T_string = "STRING"; //  字符串类型关键字
        public const string T_vary = "VARY"; //  易变类型关键字。即在编译器不确定，任何类型都可以。是否正确，在运行期判断
        public const string T_bool = "BOOL";        // bool 对象
        public const string T_typeclass = "TCLASS"; //   Lua类定义专用. 标示某个表对象是一个类定义而不是变量
        public const string T_Alias = "ALIAS";  // 导入一个别名

        #endregion


        public const string Label_Parameter = "(parameter)";  //
        public const string Label_GlobalVar = "(global variable)";
        public const string Label_LocalVar = "(local variable)";
        public const string Label_GlobalFunc = "(global function)";
        public const string Label_LocalFunc = "(local function)";

        public const string Label_ClassStatic = "(static)";

        public const string Label_ReadOnly = "(readonly)";

        public static int TypeImageIndex(AnalysisType t )
        {
            return (int)t;
        }
    }

    public static class StringExtension
    {
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class
         | AttributeTargets.Method)]
    public sealed class ExtensionAttribute : Attribute { }
}