using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpLua;
using SharpLua.Ast;
using SharpLua.Ast.Statement;
using SharpLua.Ast.Expression;

namespace TLua.Analysis
{
    /// <summary>
    /// 对成员表示式进行评估计算，计算结果是一个LuaClass
    /// </summary>
    public class ExcuteExprVisitor: NodeVisitor
    {
        public enum CallType
        {
            Type_Ident,   //  静态定义定义的类型符号
            Var_Ident,      //  运行期的动态变量

            Unknown,
        }

        enum ExcuteMode
        {
            MemberExpr, // 输入.时调用
            CallExpr,   // 输入(时调用

            Unknown,
        }

        ExcuteMode Mode
        {
            get;
            set;
        }


        SharpLua.Ast.Chunk CurChunk { get; set; }

        /// 考虑匿名函数的情况，当前scope和当前chunk不一定是相等的
        SharpLua.Ast.Scope CurScope { get; set; }

        /// 考虑在if语句的条件上调用self等函数。CurChunk和CurFunc也是不同
        SharpLua.Ast.Statement.FunctionStatement CurFunc { get; set; }


        DeclaraionManager m_DeclarationMgr;

        /// m_DeclStack表示某个表达式的运行结果。 m_CallTypeStack标记是用什么样的调用获得这个结果的。
        Stack<Declaration> m_DeclStack = new Stack<Declaration>();
        Stack<CallType> m_CallTypeStack = new Stack<CallType>();

        bool m_ForcePushVarAndFunc = false;
        public bool ForcePushVarAndFunc
        {
            get { return m_ForcePushVarAndFunc; }
            set { m_ForcePushVarAndFunc = value; }
        }

        /// 这个变量主要用于quick info的时候，能够生成函数定义的提示。function GGG:func()    在解析的时候，使用GGG这个类型的“:”来调用func，一般情况是不正确；除了函数定义以外
        bool m_IgnoreIndexerInMemberFunc = false;
        public bool IgnoreIndexerInMemberFunc
        {
            get { return m_IgnoreIndexerInMemberFunc; }
            set { m_IgnoreIndexerInMemberFunc = value; }
        }

        public bool isSuper { get; set; }

        void Init()
        {
            Mode = ExcuteMode.Unknown;
            this.isSuper = false;
            m_DeclStack.Clear();
            m_CallTypeStack.Clear();
        }

        public Declaration PeekDeclStack()
        {
            if (m_DeclStack.Count == 0)
            {
                return null;
            }
            else
            {
                return m_DeclStack.Peek();
            }
        }

        public CallType PeekCTStack()
        {
            if (m_CallTypeStack.Count == 0)
            {
                return CallType.Unknown;
            }
            else
            {
                return m_CallTypeStack.Peek();
            }
        }

        public bool PopStack()
        {
            if (m_CallTypeStack.Count == 0)
            {
                return false;
            }

            m_CallTypeStack.Pop();
            m_DeclStack.Pop();
            return true;
        }

        public bool ValidStack()
        {
            return m_DeclStack.Count != 0 && m_DeclStack.Peek() != null
                && m_CallTypeStack.Count != 0 && m_CallTypeStack.Peek() != CallType.Unknown;
        }

        /// 执行结果是成员表达式的base的类型。正确的情况下是一个luaclass
        public void ExcuteMemberExpr(MemberExpr expr, DeclaraionManager mgr, SharpLua.Ast.Chunk curChunk, SharpLua.Ast.Scope curScope, SharpLua.Ast.Statement.FunctionStatement curFunc)
        {
            if (expr == null || mgr == null || curChunk == null || curScope == null)
            {
                return;
            }

            Init();
            Mode = ExcuteMode.MemberExpr;
            AnalyExpr(expr, mgr, curChunk, curScope, curFunc);
        }

        public void ExcuteCallExpr(CallExpr expr, DeclaraionManager mgr, SharpLua.Ast.Chunk curChunk, SharpLua.Ast.Scope curScope, SharpLua.Ast.Statement.FunctionStatement curFunc)
        {
            if (expr == null || mgr == null || curChunk == null || curScope == null)
            {
                return;
            }

            Init();
            Mode = ExcuteMode.CallExpr;
            AnalyExpr(expr, mgr, curChunk, curScope, curFunc);
            
            if(ValidStack())
            {
                /// 把分析的返回值弹出去
                m_DeclStack.Pop();
                m_CallTypeStack.Pop();
            }
        }

        void AnalyExpr(Expression expr, DeclaraionManager mgr, SharpLua.Ast.Chunk curChunk, SharpLua.Ast.Scope curScope, SharpLua.Ast.Statement.FunctionStatement curFunc)
        {
            m_DeclarationMgr = mgr;
            this.CurChunk = curChunk;
            this.CurScope = curScope;
            this.CurFunc = curFunc;
            expr.Accept(this);
        }
             
        void PushStack(Declaration decl, CallType ct)
        {
            m_DeclStack.Push(decl);
            m_CallTypeStack.Push(ct);
            this.isSuper = false;
        }

        void PushFunction(FunctionDeclaration declFunc, bool isStatic)
        {
            if(declFunc==null )
            {             
                PushStack(null, CallType.Unknown);
                return;
            }

            if(declFunc.IsStatic != isStatic )
            {
                if (declFunc.OverloadList != null && declFunc.OverloadList.Count() != 0)
                {
                    foreach (FunctionDeclaration overFunc in declFunc.OverloadList)
                    {
                        /// 还是push主函数。完整的解析等执行调用表达式才执行
                        if (overFunc.IsStatic == isStatic)
                        {
                            if (isStatic)
                            {
                                PushStack(declFunc, CallType.Type_Ident);
                            }
                            else
                            {
                                PushStack(declFunc, CallType.Var_Ident);
                            }
                            return;
                        }
                    }
                }

                PushStack(null, CallType.Unknown);
                return;
            }
            else
            {
                if (isStatic)
                {
                    PushStack(declFunc, CallType.Type_Ident);
                }
                else
                {
                    PushStack(declFunc, CallType.Var_Ident);
                }
                
            }
        }

        void PushVar(VariableDeclaration declVar, bool isStatic)
        {
            if(declVar==null||declVar.IsStatic!=isStatic)
            {
                PushStack(null, CallType.Unknown);
            }
            else
            {
                if (this.ForcePushVarAndFunc)
                {
                    PushStack(declVar, CallType.Var_Ident);
                }


                TypeDeclaration declVarClass = m_DeclarationMgr.FindDeclrationByFullName(declVar.Type) as TypeDeclaration;
                if (declVarClass != null)
                {
                    PushStack(declVarClass, CallType.Var_Ident);
                }
                else
                {
                    PushStack(null, CallType.Unknown);
                    return;
                }
            }
        }

        FunctionDeclaration SelectBestFunc(CallExpr expr, FunctionDeclaration mainFunc, bool isStatic)
        {
            System.Diagnostics.Debug.Assert(expr != null && mainFunc != null && mainFunc.OverloadList != null && mainFunc.OverloadList.Count() != 0);

            /// 正确的算法，应该先判断数量和static，找到所有可行的。然后判断类型是否匹配。类型匹配还要考虑继承等。太复杂。先不做。
            int paramCount = expr.Arguments.Count;
            if (mainFunc.ParamCount == paramCount && mainFunc.IsStatic == isStatic)
            {
                return mainFunc;
            }

            foreach (var overFunc in mainFunc.OverloadList)
            {
                if (overFunc.ParamCount == paramCount && mainFunc.IsStatic == isStatic)
                {
                    return overFunc;
                }
            }

            return null;
        }

        /// call表达式只需要简单的Traverse
        public override void Apply(CallExpr expr)
        {
             if(expr.Base==null)
            {
                PushStack(null,CallType.Unknown);
                return;
            }
            else
            {
                expr.Base.Accept(this);
            }

            if (!ValidStack())
                return;

            FunctionDeclaration declFunc = PeekDeclStack() as FunctionDeclaration;
            if(declFunc==null)
            {
                PushStack(null, CallType.Unknown);
                return;
            }

            if (declFunc.OverloadList != null && declFunc.OverloadList.Count() > 0)
            {
                /// 需要静态函数还是实例函数
                bool needStatic = (PeekCTStack() == CallType.Type_Ident);

                declFunc = SelectBestFunc(expr, declFunc, needStatic);
            }

            if (declFunc == null)
            {
                PushStack(null, CallType.Unknown);
                return;
            }

            if( declFunc.ReturnTypeList == null || declFunc.ReturnTypeList.Count() == 0)
            {
                if (Mode != ExcuteMode.CallExpr && !this.ForcePushVarAndFunc)
                {
                    PushStack(null, CallType.Unknown);
                    return;
                }
                else
                {
                    PushStack(m_DeclarationMgr.FindDeclrationByFullName(TLuaGrammar.T_void), CallType.Type_Ident);
                    return;
                }
            }

            string typeName = declFunc.ReturnTypeList.ElementAt(0);

            TypeDeclaration declClass = m_DeclarationMgr.FindDeclrationByFullName(typeName) as TypeDeclaration;
            if(declClass!=null)
            {
                PushStack(declClass, CallType.Var_Ident);
                return;
            }
            else
            {
                PushStack(null, CallType.Unknown);
                return;
            }         
        }

        public override void Apply(MemberExpr expr)
        {
            if (expr.Base == null)
            {
                PushStack(null, CallType.Unknown);
                return;
            }
            else
            {
                expr.Base.Accept(this);
            }

            if (!ValidStack())
                return;

            
            if(string.IsNullOrEmpty(expr.Ident))
            {
                if (Mode == ExcuteMode.MemberExpr)
                {
                    // 标识符号为空，分析结束了
                    return;
                }
                else
                {
                    PushStack(null, CallType.Unknown);
                    return;
                }
            }

            Declaration declParent = PeekDeclStack();
            CallType ctParent = PeekCTStack();

            if(expr.Indexer==".")
            {
                if(ctParent == CallType.Var_Ident)
                {
                    // declParent必须是一个类， 解析结果是一个非静态成员变量，且是一个对象类型，将这个类型压栈
                    VariableDeclaration declVar = m_DeclarationMgr.FindMemberInClassAndBase(declParent as LuaClassDeclaration, expr.Ident) as VariableDeclaration;
                    PushVar(declVar, false);
                }
                else
                {
                    ///declParent是一个Chunk(一般是调用外部库)，cur可以是一个Chunk，Class或者全局函数或者全局变量.  变量解析出类型压栈，其他情况直接压栈    
                    if(declParent is ChunkDeclaration)
                    {                      
                        ChunkDeclaration declChunkParent = declParent as ChunkDeclaration;
                        Declaration declCur = declChunkParent.GetGlobal(expr.Ident);
                        if(declCur==null)
                        {
                            PushStack(null, CallType.Unknown);
                            return;
                        }
                        else if( declCur is VariableDeclaration)
                        {
                            PushVar(declCur as VariableDeclaration, true);
                            return;
                        }
                        else if(declCur is ChunkDeclaration ||　declCur is LuaClassDeclaration)
                        {
                            PushStack(declCur, CallType.Type_Ident);
                            return;
                        }
                        else if(declCur is FunctionDeclaration)
                        {
                            PushFunction(declCur as FunctionDeclaration, true);
                        }
                        else
                        {
                            PushStack(null, CallType.Unknown);
                            return;
                        }
                    }
                    else if(declParent is LuaClassDeclaration)
                    {
                        if (expr.Ident == "super" )
                        {
                            var baseParent = m_DeclarationMgr.FindDeclrationByFullName( (declParent as LuaClassDeclaration).BaseType );
                            if (baseParent is LuaClassDeclaration)
                            {                              
                                PushStack(baseParent, CallType.Var_Ident);
                                this.isSuper = true;
                            }
                            else
                            {
                                PushStack(null, CallType.Unknown);
                            }
                           
                            return;
                        }

                        ///declParent一个Class，cur可以是静态成员函数或者静态成员变量
                        Declaration declMember = m_DeclarationMgr.FindMemberInClassAndBase(declParent as LuaClassDeclaration, expr.Ident);
                        if(declMember==null)
                        {
                            PushStack(null, CallType.Unknown);
                            return;
                        }

                        if(declMember is VariableDeclaration)
                        {
                            PushVar(declMember as VariableDeclaration, true);
                            return;
                        }
                        else if (declMember is FunctionDeclaration)
                        {

                            PushFunction(declMember as FunctionDeclaration, true);
                        }
                        else if(declMember is LuaClassDeclaration)
                        {
                            // C#中类里面定义了类
                            PushStack(declMember, CallType.Type_Ident);
                            return;
                        }
                        else
                        {
                            PushStack(null, CallType.Unknown);
                            return;
                        }

                    }
                    else
                    {
                        PushStack(null, CallType.Unknown);
                        return;
                    }
                }
            }
            else if(expr.Indexer==":")
            {
                /// ctParent必须是一个变量, declParent必须是一个class, 解析出一个非静态成员函数调用。
                if (ctParent != CallType.Var_Ident && !this.IgnoreIndexerInMemberFunc)
                {
                    PushStack(null, CallType.Unknown);
                    return;
                }
                FunctionDeclaration declFunc = m_DeclarationMgr.FindMemberInClassAndBase(declParent as LuaClassDeclaration, expr.Ident) as FunctionDeclaration;
                PushFunction(declFunc, false);
                return;
            }

        }

        /// 解析正确表达式的时候，需要自行遍历。
        /// 该函数判断执行不可识别的表达式
        public override void Apply(Expression expr)
        {
            PushStack(null, CallType.Unknown);
            return;
        }

        //// 从当前作用域向外找到第一个函数域
        //FunctionStatement FindFirstFunction(Chunk start)
        //{ 
        //    start.Scope
        //}

        /// kkk.ddd   fff(....).ddfdf  两种情况，
        public override void Apply(VariableExpression expr)
        {
            /// 一定是只解析到一个变量表达式，而且是起点
            if(m_DeclStack.Count!=0 || expr.Var==null || string.IsNullOrEmpty(expr.Var.Name) )
            {
                PushStack(null, CallType.Unknown);
                return;
            }
          
            /// 看self, 从当前的函数域（必须是GGG:func）的形式，找变量类型
            if (expr.Var.Name == "self")
            {
                FunctionStatement fsmt = this.CurChunk as FunctionStatement;
                if (fsmt == null)
                {
                    if (CurFunc == null)
                    {
                        PushStack(null, CallType.Unknown);
                        return;
                    }
                    else
                    {
                        fsmt = CurFunc;
                    }
                }

                string className = VisitorHelper.FindClassNameInFunctionSmt(fsmt, true, false);

                if (string.IsNullOrEmpty(className))
                {
                    PushStack(null, CallType.Unknown);
                    return;   
                }

                PushStack(m_DeclarationMgr.FindDeclrationByFullName(className), CallType.Var_Ident);

                return;
            }


            /// 为了避免粘连，执行的表达式只是一个当前行解析生成的单独表达式，需要从全局的域来找变量定义。
            Variable realVar = this.CurScope.GetVariable(expr.Var.Name);

            if (realVar == null)
            {
                /// 没有定义，考虑外部引用。用名字作为类型去尝试
                Declaration decl = m_DeclarationMgr.FindDeclrationByFullName(expr.Var.Name);
                if (decl == null)
                {
                    PushStack(null, CallType.Unknown);
                }
                else if (decl is VariableDeclaration)
                {
                    PushVar(decl as VariableDeclaration, true);
                }
                else if (decl is FunctionDeclaration)
                {
                    PushFunction(decl as FunctionDeclaration, true);
                }
                else
                {
                    PushStack(decl, CallType.Type_Ident);
                }

                return; 
            }

            string typeName = TLuaGrammar.T_vary;
            CallType ct = CallType.Unknown;

            if (realVar.Type == TLuaGrammar.T_typeclass || realVar.Type == TLuaGrammar.T_Alias)
            {
                typeName = realVar.Name;
                ct = CallType.Type_Ident;
            }
            else
            {
                if (realVar.IsGlobal)
                {
                    if (VisitorHelper.IsNullOrVary(realVar.Type)) 
                    {
                        /// 全局变量，类型是空或者未决时，用名字作为类型去尝试
                        typeName = realVar.Name;
                        Declaration decl = m_DeclarationMgr.FindDeclrationByFullName(typeName);
                        if (decl == null)
                        {
                            PushStack(null, CallType.Unknown);
                        }
                        else if (decl is VariableDeclaration)
                        {
                            PushVar(decl as VariableDeclaration, true);
                        }
                        else if (decl is FunctionDeclaration)
                        {
                            PushFunction(decl as FunctionDeclaration, true);
                        }
                        else
                        {
                            PushStack(decl, CallType.Type_Ident);
                        }

                        return;
                    }
                    else
                    {
                        typeName = realVar.Type;
                        ct = CallType.Var_Ident;
                    }
                }
                else
                {
                    typeName = realVar.Type;
                    ct = CallType.Var_Ident;
                }
            }

            Declaration find_decl = m_DeclarationMgr.FindDeclrationByFullName(typeName);

            PushStack(find_decl, ct);
        }
    }
}
