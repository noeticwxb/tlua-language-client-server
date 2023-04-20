using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpLua.Ast.Statement;
using SharpLua.Ast.Expression;
using SharpLua.Ast;
using SharpLua;

namespace TLua.Analysis
{
    public class AutoLocalTypeVisitor : SharpLua.NodeVisitor
    {
        DeclaraionManager m_DeclarationMgr;

        Chunk m_chunk;

        ChunkDeclaration m_chunkDecl;

        string m_fileName;

        Stack<FunctionStatement> m_FuncStatements = new Stack<FunctionStatement>();
        Stack<Scope> m_Scopes = new Stack<Scope>();

        protected AutoLocalTypeVisitor(DeclaraionManager declMgr, Chunk c, string fileName)
        {
            m_DeclarationMgr = declMgr;
            m_chunk = c;
            m_fileName = fileName;
            if (m_DeclarationMgr != null && !string.IsNullOrEmpty(fileName))
            {
                m_chunkDecl = m_DeclarationMgr.GetChunk(fileName) as ChunkDeclaration;
            }
        }

        // 自动进行local变量的类型推导
        public static void DoSetAutoType(Chunk c, DeclaraionManager declMgr, string fileName)
        {
            AutoLocalTypeVisitor vs = new AutoLocalTypeVisitor(declMgr,c, fileName );
            c.Accept(vs);
        }

        public override void Apply(Chunk chunk)
        {
            m_Scopes.Push(chunk.Scope);
            base.Apply(chunk);
            m_Scopes.Pop();
        }

        public LuaClassDeclaration FindLuaClassDeclaration(MemberExpr memExpr)
        {
            System.Diagnostics.Debug.Assert(memExpr != null);
            Expression baseExpr = memExpr.Base;
            if (baseExpr != null && baseExpr is VariableExpression)
            {
                /// For Example: Get kk's  TableDeclaration
                VariableExpression varExpr = baseExpr as VariableExpression;
                return getParentTempClassDecl(varExpr);
            }
            else
            {
                return null;
            }
        }

        protected FunctionDeclaration CreateFuncDeclararion(SharpLua.Ast.Statement.FunctionStatement smt)
        {
            System.Diagnostics.Debug.Assert(smt != null);
            if (smt.IsLocal)
                return null;

            Expression expr = smt.Name;

            if (expr is MemberExpr)
            {
                /// example: function kk.bb()
                MemberExpr memExpr = expr as MemberExpr;

                LuaClassDeclaration parentDecl = FindLuaClassDeclaration( memExpr);
                if (parentDecl == null)
                {
                    return null;
                }

                FunctionDeclaration funcDecl = new FunctionDeclaration();
                funcDecl.Name = memExpr.Ident;           // "bb"
                funcDecl.IsStatic = true;

                AnalysisType ast = funcDecl.IsStatic ? AnalysisType.Class_Static_Function : AnalysisType.Class_Instance_Function;
                funcDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(ast);

                parentDecl.AddFunction(funcDecl, funcDecl.IsStatic);
                return funcDecl;
            }

            return null;
        }


        public override void Apply(FunctionStatement smt)
        {
            /// Create and set funcDecl.Name、funcDecl.IsStatic
            FunctionDeclaration funcDecl = CreateFuncDeclararion(smt);

            // 合法的table定义的 函数描述
            if (funcDecl != null)
            {
                /// FileName,Line, Col
                AnalyLuaClassVisitor.AnalyLocation(funcDecl, smt, funcDecl.Name,m_fileName);

                /// Return Type
                AnalyLuaClassVisitor.AnalyReturnType(funcDecl, smt);

                /// Param List
                AnalyLuaClassVisitor.AnalyParamList(funcDecl, smt, m_fileName);

                /// funcDecl.TypeImageIndex 
                //funcDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Static_Function);

                /// funcDecl.DisplayText
                funcDecl.DisplayText = funcDecl.Name;

                //funcDecl.Description
                funcDecl.FormatFunctionDesc();
            }


            m_FuncStatements.Push(smt);

            this.Apply((Chunk)smt);

            m_FuncStatements.Pop();
        }

        LuaClassDeclaration getParentTempClassDecl(Expression exprParent){
            if (exprParent is VariableExpression)
            {
                VariableExpression varExpr = exprParent as VariableExpression;
                if (varExpr.Var != null && !string.IsNullOrEmpty(varExpr.Var.Type) && m_chunkDecl != null)
                {
                    return m_chunkDecl.GetLocal(varExpr.Var.Type) as LuaClassDeclaration;
                }
            }

            return null;
        }


        public override void Apply(AssignmentStatement smt)
        {
            
            List<String> last_types = new List<string>();
            for (int cursor = 0; cursor < smt.Lhs.Count && cursor < smt.Rhs.Count ; ++cursor)
            {
                // 非local的AnalyLuaClassVisitor按照class去解释
                VariableExpression varExpr = smt.Lhs[cursor] as VariableExpression;
                if (varExpr != null && varExpr.Var != null
                    && !varExpr.Var.IsGlobal
                    && varExpr.Var.Type == SharpLua.TLuaGrammar.T_vary)
                {
                    FunctionStatement funcStatement = null;
                    if (m_FuncStatements.Count > 0)
                    {
                        funcStatement =  m_FuncStatements.Peek();
                    }
                    last_types = VisitorHelper.computeLocalType(smt.Rhs[cursor], m_chunk, this.m_DeclarationMgr, funcStatement, m_Scopes.Peek(),  m_fileName, m_chunkDecl);
                    if (last_types.Count > 0)
                    {
                        varExpr.Var.Type = last_types[0];
                    }        
                }

                // 支持特别简单的临时类的补充成员定义。类似ttt.ss = ""，这样补充定义ttt里面保守ss
                MemberExpr memberExpr = smt.Lhs[cursor] as MemberExpr;
                if (memberExpr != null && memberExpr.Indexer == "." 
                    && !string.IsNullOrEmpty(memberExpr.Ident)
                    && memberExpr.Base != null)
                {
                    LuaClassDeclaration classDecl = getParentTempClassDecl(memberExpr.Base);
                    // 只有临时类，才在这里计算。正常类在LuaClassDeclation里面处理
                    if (classDecl != null && classDecl.IsLocalTable)
                    {
                        Declaration declOld = classDecl.GetMember(memberExpr.Ident);

                        if(declOld != null){
                            continue;
                        }

                        FunctionStatement funcStatement = null;
                        if (m_FuncStatements.Count > 0)
                        {
                            funcStatement = m_FuncStatements.Peek();
                        }

                        string valType = memberExpr.OptionalIdentType;
                        if(string.IsNullOrEmpty(valType)){
                            last_types = VisitorHelper.computeLocalType(smt.Rhs[cursor], m_chunk, this.m_DeclarationMgr, funcStatement, m_Scopes.Peek(), m_fileName, m_chunkDecl);
                            if(last_types.Count > 0 ){
                                valType = last_types[0];
                            }
                        }
                        
                        if (last_types.Count > 0)
                        {
                            valType = last_types[0];
                            VariableDeclaration varDecl = new VariableDeclaration();
                            varDecl.Name = memberExpr.Ident;
                            varDecl.Type = valType;
                            varDecl.FileName = m_fileName;
                            varDecl.Col = memberExpr.Line;
                            varDecl.Line = memberExpr.Column;
                            varDecl.IsStatic = false;

                            varDecl.DisplayText = varDecl.Name;
                            varDecl.ReadOnly = false;
                            varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Instace_Variable);

                            varDecl.Description = string.Format("{0} {1} in {2}", varDecl.Type, varDecl.Name, classDecl.Name);

                            classDecl.AddProperty(varDecl, false);

                        }
                    }
                }
            }

            if (smt.Lhs.Count > smt.Rhs.Count && last_types.Count > 1)
            {
                for (int cursor = smt.Rhs.Count; cursor < smt.Lhs.Count; ++cursor)
                {
                    VariableExpression varExpr = smt.Lhs[cursor] as VariableExpression;
                    if (varExpr != null && varExpr.Var != null
                        && !varExpr.Var.IsGlobal
                        && varExpr.Var.Type == SharpLua.TLuaGrammar.T_vary)
                    {
                        int offset = cursor - smt.Rhs.Count + 1;
                        if (offset >= 0 && offset < last_types.Count)
                        {
                            varExpr.Var.Type = last_types[offset];
                        }

                    }
                }
                                 
            }


            Apply((Statement)smt);
        }

        public override void Apply(GenericForStatement smt)
        {
            if (smt.VariableList != null && smt.VariableList.Count > 0
                && smt.Generators != null && smt.Generators.Count > 0)
            {
                FunctionStatement funcStatement = null;
                if (m_FuncStatements.Count > 0)
                {
                    funcStatement = m_FuncStatements.Peek();
                }

                List<string> return_Types = new List<string>();
                foreach (var expr in smt.Generators)
                {
                    List<string> types = VisitorHelper.computeLocalType(expr, m_chunk, this.m_DeclarationMgr, funcStatement, smt.Scope, m_fileName, m_chunkDecl);
                    return_Types.AddRange(types);
                }

                for (int i = 0; i < smt.VariableList.Count && i < return_Types.Count; ++i)
                {
                    Variable var = smt.VariableList[i];
                    if (string.IsNullOrEmpty(var.Type) || var.Type == AnalysisConfig.T_vary)
                    {
                        var.Type = return_Types[i];
                    }
                }
            }


            this.Apply((Chunk)smt);
        }

    }
}
