using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpLua.Ast;
using SharpLua.Ast.Expression;
using SharpLua.Ast.Statement;

namespace TLua.Analysis
{
    /// <summary>
    /// 用于分析一个Chunk，一般是一个文件的AST, 获取在该Chunk定义的全局函数、lua class等。类似导出符号表的概念。
    /// 对于在Chunk内定义的函数或者类（包括函数内定义一个新的函数/表的情况），编译期很难知道该Chunk是否被执行了，即实现了定义。或者反过来，通过代码执行把一个local的函数或者表导入到全局空间，这种情况也不能判断。
    /// 我们只考虑静态特性：
    ///     1、用local修饰的，只在所在作用域可见
    ///     2、除上述情况外，均在全局作用域可见(即AnalyChunkVisitor分析的内容)
    ///       2.1 Native库，比如C#使用命名空间的概念。按照表和子表的关系来处理。local v as UnityEngine.GameObject
    ///         2.1.2 把子表引入全局空间如何处理？ 未来可能考虑using指令，目前暂时写全名
    ///     3、类型的继承？
    ///       3.1 C#的类型继承，可以直接解析处理。Lua的类型继承。只允许在使用class(***)的情况下使用。
    /// </summary>
    public class AnalyLuaClassVisitor: SharpLua.NodeVisitor
    {
        public ChunkDeclaration RootChunk = new ChunkDeclaration();

        Stack<FunctionDeclaration> m_FuncDeclStack = new Stack<FunctionDeclaration>();
        Stack<FunctionStatement> m_FuncStatemenStack = new Stack<FunctionStatement>();
        Stack<Scope> m_Scopes = new Stack<Scope>();

        Chunk m_Chunk;

        DeclaraionManager m_DeclManager;

        string m_FileName;
        
        /// <summary>
        /// 分析一个chunk
        /// </summary>
        /// <param name="c">待分析的chunk</param>
        public void Analy(DeclaraionManager declManager, Chunk c, string fileName)
        {
            if( c == null)
            {
                throw new System.NullReferenceException("Chunk or Declaration is null");
            }

            m_DeclManager = declManager;

            RootChunk = new ChunkDeclaration();
            RootChunk.FileName = fileName;
            RootChunk.Name = fileName;
            RootChunk.Chunk = c;
 
            m_Chunk = c;
            m_FileName = fileName;

            m_FuncDeclStack.Push(null);


            m_Chunk.Accept(this);
        }

        public static void AnalyLocation(Declaration decl, SharpLua.Ast.Statement.Statement smt, string findString , string fileName)
        {
            System.Diagnostics.Debug.Assert(decl != null && smt != null);

            decl.FileName = fileName;

            if (smt.ScannedTokens != null && smt.ScannedTokens.Count > 0)
            {
                if( string.IsNullOrEmpty(findString) )
                {
                    decl.Line = smt.ScannedTokens[0].Line;
                    decl.Col = smt.ScannedTokens[0].Column;
                    return;
                }
                else
                {
                    foreach(var token in smt.ScannedTokens)
                    {
                        if(token.Data == findString)
                        {
                            decl.Line = token.Line;
                            decl.Col = token.Column;
                            return;
                        }
                    }
                }
            }

            decl.Line = smt.LineNumber;
            decl.Col = 0;
        }

        public static void AnalyReturnType(FunctionDeclaration funcDecl, SharpLua.Ast.Statement.FunctionStatement smt)
        {
            System.Diagnostics.Debug.Assert(funcDecl != null && smt != null);

            if (smt.ReturnTypeList != null)
            {
                foreach (var typeIden in smt.ReturnTypeList)
                {
                    funcDecl.AddReturnType(typeIden);
                }
            }
        }

        /// @TODO 在运行时生成
        public static string FormatFunctionParamDesc(VariableDeclaration decl)
        {
            return string.Format("{0}{1} {2}", AnalysisConfig.Label_Parameter, decl.Type, decl.Name);
        }

        public static void AnalyParamList(FunctionDeclaration funcDecl, SharpLua.Ast.Statement.FunctionStatement smt, string fileName )
        {
            System.Diagnostics.Debug.Assert(smt != null);

            if(smt.IsVararg)
            {
                funcDecl.IsVararg = true;
            }
            else
            {
                if (smt.Arguments != null)
                {
                    foreach (var argu in smt.Arguments)
                    {
                        VariableDeclaration variableDecl = new VariableDeclaration();
                        AnalyLocation(variableDecl, smt, argu.Name, fileName);

                        variableDecl.Name = argu.Name;
                        variableDecl.Type = argu.Type;
                        variableDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Local_Variable);

                        variableDecl.Description = FormatFunctionParamDesc(variableDecl);
                        variableDecl.DisplayText = variableDecl.Name;

                        funcDecl.AddParam(variableDecl);
                    }
                }
            }
        }

        /// For Example: function kk.aa.bb()
        public static LuaClassDeclaration FindLuaClassDeclaration(ChunkDeclaration chunkDecl, bool isInGlobal,  MemberExpr memExpr )
        {
            if (chunkDecl == null)
            {
                return null;
            }
            System.Diagnostics.Debug.Assert(memExpr != null);
            Expression baseExpr = memExpr.Base;
            if(baseExpr != null && baseExpr is VariableExpression)
            {
                /// For Example: Get kk's  TableDeclaration
                VariableExpression varExpr = baseExpr as VariableExpression;
                if( varExpr.Var !=null )
                {
                    if (isInGlobal)
                    {
                        return chunkDecl.GetGlobal(varExpr.Var.Name) as LuaClassDeclaration;
                    }
                    else
                    {
                        return chunkDecl.GetLocal(varExpr.Var.Name) as LuaClassDeclaration;
                    }
                    
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }            
        }

        /// 分析函数名，创建函数描述，并且加入到正确的描述符号。
        protected FunctionDeclaration CreateFuncDeclararion(SharpLua.Ast.Statement.FunctionStatement smt)
        {
            System.Diagnostics.Debug.Assert(smt != null);
            if (smt.IsLocal)
                return null;

            Expression expr = smt.Name;

            if (expr is VariableExpression)
            {
                /// example: function kk() 
                VariableExpression varExpr = expr as VariableExpression;
                if( varExpr.Var == null )
                {
                    return null;
                }

                FunctionDeclaration funcDecl = new FunctionDeclaration();
                funcDecl.IsStatic = true;

                funcDecl.Name = varExpr.Var.Name;    //  get "kk"

                funcDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Static_Function);

                RootChunk.AddGlobals(funcDecl);
                         
                return funcDecl;

            }
            else if (expr is MemberExpr)
            {
                /// example: function kk.bb()
                MemberExpr memExpr = expr as MemberExpr;

                LuaClassDeclaration parentDecl = AnalyLuaClassVisitor.FindLuaClassDeclaration(RootChunk, true, memExpr);
                if (parentDecl == null)
                {
                    return null;
                }

                FunctionDeclaration funcDecl = new FunctionDeclaration();
                funcDecl.Name = memExpr.Ident;           // "bb"
                funcDecl.IsStatic = ((memExpr.Indexer == ":") ? false : true);

                AnalysisType ast = funcDecl.IsStatic ? AnalysisType.Class_Static_Function : AnalysisType.Class_Instance_Function;
                funcDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(ast);

                parentDecl.AddFunction(funcDecl, funcDecl.IsStatic);
                return funcDecl;                
            }

            return null;
        }

        protected LuaClassDeclaration CreateLuaClassDeclaration(Expression expr)
        {
            VariableExpression varExpr = expr as VariableExpression;

            if (varExpr == null || varExpr.Var == null
                || varExpr.Var.Type != SharpLua.TLuaGrammar.T_typeclass
                )
            {
                return null;
            }
           
            LuaClassDeclaration decl = new LuaClassDeclaration();
            decl.Name = varExpr.Var.Name;
            decl.Line = varExpr.Var.Line;
            decl.Col = varExpr.Var.Column;
            decl.FileName = m_FileName;
            decl.DisplayText = decl.Name;
            decl.Description = "class " + decl.Name;
            decl.FullName = decl.Name;
            decl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Type);

            /// 对于类定义，特殊处理，无论是否用local申明，只要是T_typeclass定义。一律作为符号表导入。
            if(varExpr.Var.IsGlobal)
            {
                RootChunk.AddGlobals(decl);
            }
            else
            {
                RootChunk.AddGlobals(decl);
            }

            return decl;
        }




        protected VariableDeclaration FindProperty(LuaClassDeclaration luaClassDecl, string name, bool isStatic)
        {
            if(luaClassDecl==null){
                return null;
            }

            VariableDeclaration varDecl = luaClassDecl.GetProperty(name, isStatic);
            if (varDecl != null)
            {
                return varDecl;
            }

            if (string.IsNullOrEmpty(luaClassDecl.BaseType))
            {
                return null;
            }

            LuaClassDeclaration parentDecl = m_DeclManager.GetGlobal(luaClassDecl.BaseType) as LuaClassDeclaration;

            return FindProperty(parentDecl, name, isStatic);
        }

        protected VariableDeclaration CreateClassMemberDeclaration(Expression expr)
        {
            MemberExpr mbrExpr = expr as MemberExpr;
            if (mbrExpr == null || mbrExpr.Indexer != ".")
                return null;

            VariableExpression selfExpr = mbrExpr.Base as VariableExpression;
            if (selfExpr == null || selfExpr.Var == null
                || !selfExpr.Var.IsGlobal 
                )
            {
                return null;
            }
            LuaClassDeclaration luaClassDecl = null;

            bool isStatic = true;

            if( selfExpr.Var.Name == "self")
            {
                /// 类似self.kkk，定义成员变量. 是实例成员变量 
                /// 在栈上找函数定义，根据函数的表达式找表定义
                FunctionDeclaration decl = m_FuncDeclStack.Peek();
                if (decl == null || string.IsNullOrEmpty(decl.ParentClassFullName) )
                {
                    return null;
                }

                luaClassDecl = RootChunk.GetGlobal(decl.ParentClassFullName) as LuaClassDeclaration;
                isStatic = false;
            }
            else
            {
                /// 类似  GGG.kkk 的定义，定义GGG类的类成员变量
                /// 对GGG的查找应该在所有可能的位置甚至整个工程查找。 简单处理：只在当前的chunk找GGG的定义，找不到就算了。
                luaClassDecl = RootChunk.GetGlobal(selfExpr.Var.Name) as LuaClassDeclaration;
                isStatic = true;
            }

            if (luaClassDecl == null)
                return null;


            /// 看变量是否有定义标记as
            ///     如果有定义标记，新建或者更新; 
            ///     如果没有定义标记并且类里面有没有查到已经有的定义。进行定义
            VariableDeclaration varDecl = FindProperty(luaClassDecl, mbrExpr.Ident, isStatic);

            if(varDecl!=null && string.IsNullOrEmpty(mbrExpr.OptionalIdentType) )
            {
                return varDecl;
            }

            varDecl = new VariableDeclaration();
            varDecl.FileName = m_FileName;
            varDecl.Col = mbrExpr.Column;
            varDecl.Line = mbrExpr.Line;
            varDecl.IsStatic = isStatic;
            varDecl.Name = mbrExpr.Ident;
            varDecl.DisplayText = varDecl.Name;
            varDecl.ReadOnly = false;
            varDecl.Type = mbrExpr.OptionalIdentType;

            varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(
                isStatic ? AnalysisType.Class_Static_Variable : AnalysisType.Class_Instace_Variable
                );

            if(isStatic)
            {
                varDecl.Description = AnalysisConfig.Label_ClassStatic + varDecl.Type + " " + luaClassDecl.Name + "." + varDecl.Name;                
            }
            else
            {
                 varDecl.Description = varDecl.Type + " " + luaClassDecl.Name + "." + varDecl.Name;
            }

            luaClassDecl.AddProperty(varDecl, varDecl.IsStatic);

            return varDecl;
        }

        //暂不支持prop的自动推导
        protected string ComputeClassPropType(Expression get_expr)
        {
            if (get_expr is AnonymousFunctionExpr)
            {
                AnonymousFunctionExpr get_func_expr = get_expr as AnonymousFunctionExpr;
                if (get_func_expr.ReturnTypeList != null && get_func_expr.ReturnTypeList.Count > 0)
                {
                    return get_func_expr.ReturnTypeList[0];
                }
            }

            return SharpLua.TLuaGrammar.T_vary;
        }

        protected VariableDeclaration CreateClassPropDeclaration(String className, String propName, Expression get_expr, int col, int line )
        {
            LuaClassDeclaration luaClassDecl = RootChunk.GetGlobal(className) as LuaClassDeclaration;
            if (luaClassDecl == null)
            {
                return null;      
            }

            bool isStatic = false;

            /// 看变量是否有定义标记as
            ///     如果有定义标记，新建或者更新; 
            ///     如果没有定义标记并且类里面有没有查到已经有的定义。进行定义
            VariableDeclaration varDecl = luaClassDecl.GetProperty(propName, isStatic);

            if (varDecl != null )
            {
                return varDecl;
            }

            varDecl = new VariableDeclaration();
            varDecl.FileName = m_FileName;
            varDecl.Col = col;
            varDecl.Line = line;
            varDecl.IsStatic = isStatic;
            varDecl.Name = propName;
            varDecl.DisplayText = varDecl.Name;
            varDecl.ReadOnly = false;
            varDecl.Type = this.ComputeClassPropType(get_expr);

            varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(
                isStatic ? AnalysisType.Class_Static_Variable : AnalysisType.Class_Instace_Variable
                );

            if (isStatic)
            {
                varDecl.Description = AnalysisConfig.Label_ClassStatic + varDecl.Type + " " + luaClassDecl.Name + "." + varDecl.Name;
            }
            else
            {
                varDecl.Description = varDecl.Type + " " + luaClassDecl.Name + "." + varDecl.Name;
            }

            luaClassDecl.AddProperty(varDecl, varDecl.IsStatic);

            return varDecl;
        }

        protected VariableDeclaration CreateChunkVariableDeclaration(Expression expr)
        {
            VariableExpression varExpr = expr as VariableExpression;
            if (varExpr == null || varExpr.Var == null
                || !varExpr.Var.IsGlobal
                || varExpr.Var.Type == SharpLua.TLuaGrammar.T_Alias)
                return null;

            VariableDeclaration varDecl = RootChunk.GetGlobal(varExpr.Var.Name) as VariableDeclaration;
            if (varDecl != null)
                return varDecl;

            varDecl = new VariableDeclaration();
            varDecl.FileName = m_FileName;
            varDecl.Col = varExpr.Var.Column;
            varDecl.Line = varExpr.Var.Line;
            varDecl.IsStatic = true;
            varDecl.Name = varExpr.Var.Name;            
            varDecl.ReadOnly = false;
            varDecl.Type = varExpr.Var.Type;
            varDecl.DisplayText = varDecl.Name;
            varDecl.Description = AnalysisConfig.Label_GlobalVar + varDecl.Type + " " + varDecl.Name;

            varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Global_Variable);
            RootChunk.AddGlobals(varDecl);

            return varDecl;
        }

        /// <summary>
        ///  约定：对于 GGG as typeclass = class(KKK)的情况，设置GGG的基类是KKK
        /// </summary>
        /// <param name="decl"></param>
        /// <param name="rhs"></param>
        protected void AnalyLuaClassBaseType(LuaClassDeclaration decl, Expression rhs)
        {
            CallExpr callExpr = rhs as CallExpr;
            if (callExpr == null || callExpr.Base == null || callExpr.Arguments == null || callExpr.Arguments.Count == 0)
                return;

            VariableExpression funcNameExpr = callExpr.Base as VariableExpression;
            if(funcNameExpr==null || funcNameExpr.Var == null 
                || funcNameExpr.Var.Name != "class")
            {
                return;
            }

            
            //  对class的两种写法的支持。客户端的定义是 Derived =  class(BaseT), 服务器是local Derived = class( "Derived" , BaseT)
            int baseClassIndex = callExpr.Arguments.Count > 1 ? 1:0;

            VariableExpression baseTypeExpr = callExpr.Arguments[baseClassIndex] as VariableExpression;
            if (baseTypeExpr == null || baseTypeExpr.Var == null)
                return;

            if (!m_DeclManager.IsClassLoop(decl, baseTypeExpr.Var.Name, null))
            {
                decl.BaseType = baseTypeExpr.Var.Name;
            }
            
        }

        List<string> ConverNameSpaceChain(Expression expr)
        {
            if (expr == null)
                return null;

            if (expr is VariableExpression)
            {
                VariableExpression varExpr = expr as VariableExpression;
                if(varExpr==null || varExpr.Var == null || string.IsNullOrEmpty(varExpr.Var.Name))
                {
                    return null;
                }

                List<string> chain = new List<string>();
                chain.Add(varExpr.Var.Name);
                return chain;
            }
            else if (expr is MemberExpr)
            {
                MemberExpr memExpr = expr as MemberExpr;
                if (memExpr == null || string.IsNullOrEmpty(memExpr.Ident) || memExpr.Indexer != ".")
                {
                    return null;
                }
                List<string> chain = ConverNameSpaceChain(memExpr.Base);
                if (chain == null)
                {
                    return null;
                }
                chain.Add(memExpr.Ident);
                return chain;
            }
            else
            {
                return null;
            }
        }

        protected bool AnalyAlias(Expression lhs, Expression rhs)
        {
            VariableExpression varExpr = lhs as VariableExpression;
            if (varExpr == null || varExpr.Var == null
                || !varExpr.Var.IsGlobal
                || varExpr.Var.Type != SharpLua.TLuaGrammar.T_Alias)
                return false;

            List<string> chain = ConverNameSpaceChain(rhs);
            if (chain != null)
            {
                RootChunk.AddAlias(varExpr.Var.Name, chain);
                return true;
            }
            return false;
        }

        protected void AnalyComment(Declaration decl, SharpLua.Ast.Statement.Statement smt)
        {
            if (decl == null)
                return;

            if (smt == null || smt.ScannedTokens == null || smt.ScannedTokens.Count == 0)
                return;

            SharpLua.Token firstToken = smt.ScannedTokens[0];
            if (firstToken == null || firstToken.Leading == null)
                return;

            StringBuilder builder = new StringBuilder();
            foreach (var t in firstToken.Leading)
            {
                if (t.Type == SharpLua.TokenType.LongComment
                    || t.Type == SharpLua.TokenType.ShortComment
                    || t.Type == SharpLua.TokenType.DocumentationComment)
                {
                    builder.Append(t.Data);
                    builder.Append("\n");
                }
            }

            if (builder.Length > 1)
            {
                builder.Remove(builder.Length - 1, 1);//remove "\n"
            }

            if (builder.Length > 0)
            {
                decl.CommentText = builder.ToString().Trim();
            }
        }

        // 判断是否是类似self.data.ss = "" 或者  T.data.ss = "“， T是类，这样的定义。把ss加到data的定义里面
        public VariableDeclaration createTempClassPropDecl(MemberExpr memberExpr, out string tempClassName)
        {
            tempClassName = "";

            if (memberExpr == null || memberExpr.Indexer != "."
                || string.IsNullOrEmpty(memberExpr.Ident)
                || memberExpr.Base == null)
            {
                return null;
            }

            MemberExpr propExpr = memberExpr.Base as MemberExpr;
            if (propExpr == null || propExpr.Indexer != "."
                || string.IsNullOrEmpty(memberExpr.Ident)
                || memberExpr.Base == null)
            {
                return null;
            }

            LuaClassDeclaration luaClassDecl = null;

            VariableExpression selfExpr = propExpr.Base as VariableExpression;
            if (selfExpr == null)
            {
                return null;
            }

            if (selfExpr.Var != null && selfExpr.Var.Name == "self")
            {
                /// 类似self.kkk，定义成员变量. 是实例成员变量 
                /// 在栈上找函数定义，根据函数的表达式找表定义
                FunctionDeclaration decl = m_FuncDeclStack.Peek();
                if (decl == null || string.IsNullOrEmpty(decl.ParentClassFullName))
                {
                    return null;
                }
                luaClassDecl = RootChunk.GetGlobal(decl.ParentClassFullName) as LuaClassDeclaration;
            }
            else if (selfExpr.Var.Type == SharpLua.TLuaGrammar.T_typeclass)
            {
                luaClassDecl = RootChunk.GetGlobal(selfExpr.Var.Name) as LuaClassDeclaration;
            }

            if (luaClassDecl == null)
            {
                return null;
            }

            VariableDeclaration tempDecl = luaClassDecl.GetMember(propExpr.Ident) as VariableDeclaration;
            if (tempDecl == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(tempDecl.Type) || tempDecl.Type == SharpLua.TLuaGrammar.T_vary)
            {
                return null;
            }

            // 找临时类的定义
            LuaClassDeclaration tempClassDecl = this.RootChunk.GetLocal(tempDecl.Type) as LuaClassDeclaration;
            if (tempClassDecl == null || !tempClassDecl.IsLocalTable)
            {
                return null;
            }

            // self.data.ss= ""  ss已经定义过了。
            if (tempClassDecl.GetMember(memberExpr.Ident) != null)
            {
                return null;
            }

            VariableDeclaration varDecl = new VariableDeclaration();
            varDecl.Name = memberExpr.Ident;

            varDecl.FileName = m_FileName;
            varDecl.Type = memberExpr.OptionalIdentType;

            varDecl.Col = memberExpr.Line;
            varDecl.Line = memberExpr.Column;
            varDecl.IsStatic = false;

            varDecl.DisplayText = varDecl.Name;
            varDecl.ReadOnly = false;
            varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Instace_Variable);

            varDecl.Description = tempClassDecl.Name;

            tempClassDecl.AddProperty(varDecl, false);

            tempClassName = tempClassDecl.Name;

            return varDecl;
        }


        /// 找类、类变量、变量定义
        public override void Apply(SharpLua.Ast.Statement.AssignmentStatement smt)
        {
            /// 支持下列情况。typeclass 不可少，否则不能区分是类定义还是变量定义
            ///     KKK as TCLASS = ***  定义一个名叫KKK 的类型
            ///     KKK as TCLASS = class(AAA) 定义KKK的类，继承自AAA
            ///     self.ppp [as **] = , 定义一个成员变量。必须使用self
            ///     KKK.ggg [as ***] ，  类的成员变量定义。
            ///     g1[as ...][, g2[as...]] =   在as不是typeclass的情况下，定义或者赋值多个全局变量。  

            
            for (int index = 0; index < smt.Lhs.Count; ++index)
            {
                Expression lhs = smt.Lhs[index];

                /// 先判断是否class定义
                LuaClassDeclaration luaClassDecl = CreateLuaClassDeclaration(lhs);
                if (luaClassDecl != null)
                {
                    if( index < smt.Rhs.Count)
                    {
                        /// 找基类
                        AnalyLuaClassBaseType(luaClassDecl, smt.Rhs[index]);
                    }

                    AnalyComment(luaClassDecl, smt);
                    continue;
                }

                Expression rhsExpr = null;
                if( index < smt.Rhs.Count){
                    rhsExpr = smt.Rhs[index];
                }

                /// 判断是否成员变量定义
                VariableDeclaration varDecl = CreateClassMemberDeclaration(lhs);
                if(varDecl!=null)
                {
                    if (varDecl.Type == SharpLua.TLuaGrammar.T_vary && rhsExpr != null)
                    {
                        FunctionStatement funcStatement = null;
                        if(m_FuncStatemenStack.Count > 0){
                            funcStatement = m_FuncStatemenStack.Peek();
                        }
                        List<string> types = VisitorHelper.computeLocalType(rhsExpr, m_Chunk, m_DeclManager, funcStatement,m_Scopes.Peek(), m_FileName,RootChunk);
                        if (types.Count > 0)
                        {
                            varDecl.Type = types[0];
                            luaClassDecl = RootChunk.GetGlobal(varDecl.ParentClassFullName) as LuaClassDeclaration;
                            if (luaClassDecl!=null)
                            {
                                if (varDecl.IsStatic)
                                {
                                    varDecl.Description = AnalysisConfig.Label_ClassStatic + varDecl.Type + " " + luaClassDecl.Name + "." + varDecl.Name;
                                }
                                else
                                {
                                    varDecl.Description = varDecl.Type + " " + luaClassDecl.Name + "." + varDecl.Name;
                                }
                            }
   
                        }
                    }

                    AnalyComment(varDecl, smt);
                    continue;
                }

                // 判断是否是类似self.data.ss = ""这样的定义。把ss加到data的定义里面
                string tempClassName;
                VariableDeclaration tempClassVarDecl = createTempClassPropDecl(lhs as MemberExpr, out tempClassName);
                if (tempClassVarDecl != null)
                {
                    if ((string.IsNullOrEmpty(tempClassVarDecl.Type) || tempClassVarDecl.Type == SharpLua.TLuaGrammar.T_vary) 
                         && rhsExpr != null)
                     {
                         FunctionStatement funcStatement = null;
                         if (m_FuncStatemenStack.Count > 0)
                         {
                             funcStatement = m_FuncStatemenStack.Peek();
                         }
                         List<string> types = VisitorHelper.computeLocalType(rhsExpr, m_Chunk, m_DeclManager, funcStatement, m_Scopes.Peek(), m_FileName, RootChunk);
                         if (types.Count > 0)
                         {
                             tempClassVarDecl.Type = types[0];
                             tempClassVarDecl.Description = string.Format("{0} {1}.{2}", tempClassVarDecl.Type, tempClassName, tempClassVarDecl.Name);
                         }
                     }

                    AnalyComment(varDecl, smt);
                    continue;
                }


                /// 全局变量定义
                varDecl = CreateChunkVariableDeclaration(lhs);
                if (varDecl != null)
                {
                    AnalyComment(varDecl, smt);
                    continue;
                }

                if (index < smt.Rhs.Count)
                {
                    /// 找别名
                    if (AnalyAlias(lhs, smt.Rhs[index]))
                    {
                        continue;
                    }
                }
          
            }

            base.Apply(smt);
        }

        public override void Apply(Chunk chunk)
        {
            m_Scopes.Push(chunk.Scope);
            base.Apply(chunk);
            m_Scopes.Pop();
        }

        /// 找类的函数定义
        public override void Apply(SharpLua.Ast.Statement.FunctionStatement smt)
        {
            System.Diagnostics.Debug.Assert(smt != null);

            /// Create and set funcDecl.Name、funcDecl.IsStatic
            FunctionDeclaration funcDecl = CreateFuncDeclararion(smt);

            if (funcDecl == null)
            {
                // 函数定义不合法，可能是table还没有定义。或者在当前的chunk当中没有找到table定义（目前不允许在不同的文件中给同一个table定义函数）
                // 不合法的情况下，这个函数里面的内容就不进行解析
                return;
            }

            /// FileName,Line, Col
            AnalyLocation(funcDecl, smt, funcDecl.Name,m_FileName);

            /// Return Type
            AnalyReturnType(funcDecl, smt);

            /// Param List
            AnalyParamList(funcDecl, smt, m_FileName);

            /// funcDecl.TypeImageIndex 
            //funcDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Static_Function);

            /// funcDecl.DisplayText
            funcDecl.DisplayText = funcDecl.Name;

            //funcDecl.Description
            funcDecl.FormatFunctionDesc();

            AnalyComment(funcDecl, smt);

            m_FuncDeclStack.Push(funcDecl);
            m_FuncStatemenStack.Push(smt);
            this.Apply((Chunk)smt);
            m_FuncDeclStack.Pop();
            m_FuncStatemenStack.Pop();

        }

        public override void Apply(SharpLua.Ast.Statement.TLuaUsingStatement smt)
        {
            RootChunk.AddUsingNameSpace(smt.NameSpaceChain);
            base.Apply(smt);
        }



        public override void Apply(SharpLua.Ast.Statement.CallStatement smt)
        {
            SharpLua.Ast.Expression.CallExpr callExpr = smt.Expression as SharpLua.Ast.Expression.CallExpr;
            if (callExpr == null)
            {
                base.Apply(smt);
                return;
            }

            VariableExpression funcName = callExpr.Base as VariableExpression;
            if (funcName != null && funcName.Var != null && funcName.Var.Name == "DEF_PROP" 
                && callExpr.Arguments != null
                && callExpr.Arguments.Count >=3 )
            {
                VariableExpression paramClassName = callExpr.Arguments[0] as VariableExpression;
                StringExpr paramPropName = callExpr.Arguments[1] as StringExpr;
                if (paramClassName != null && paramClassName.Var != null && paramPropName != null 
                    && !String.IsNullOrEmpty(paramClassName.Var.Name)
                    && !String.IsNullOrEmpty(paramPropName.Value))
                {
                    String className = paramClassName.Var.Name;
                    String propName = paramPropName.Value;
                    Expression expr = callExpr.Arguments[2];

                    VariableDeclaration varDecl = CreateClassPropDeclaration(className, propName, expr, callExpr.OpenBracketColumn, callExpr.OpenBracketLine);
                    if (varDecl != null)
                    {
                        AnalyComment(varDecl, smt);
                    }
                }
            }

            base.Apply(smt);
        }

    }
}
