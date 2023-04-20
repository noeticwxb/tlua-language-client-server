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
    // 对函数进行自动推导
    public class AutoFuncReturnTypeVisitor: SharpLua.NodeVisitor
    {
        DeclaraionManager m_DeclarationMgr;

        Chunk m_chunk;

        ChunkDeclaration m_chunkDecl;

        string m_fileName;

        Stack<FunctionStatement> m_FuncStatements = new Stack<FunctionStatement>();
        Stack<Scope> m_Scopes = new Stack<Scope>();

        protected AutoFuncReturnTypeVisitor(DeclaraionManager declMgr, Chunk c, string fileName)
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
            AutoFuncReturnTypeVisitor vs = new AutoFuncReturnTypeVisitor(declMgr,c, fileName );
            c.Accept(vs);
        }

        public override void Apply(Chunk chunk)
        {
            m_Scopes.Push(chunk.Scope);
            base.Apply(chunk);
            m_Scopes.Pop();
        }

        public override void Apply(FunctionStatement smt)
        {
            m_FuncStatements.Push(smt);
            this.doAnaly(smt);
            this.Apply((Chunk)smt);
            m_FuncStatements.Pop();
        }

        /// For Example: function kk.aa.bb()
        protected LuaClassDeclaration FindLuaClassDeclaration(MemberExpr memExpr)
        {
            System.Diagnostics.Debug.Assert(memExpr != null);
            Expression baseExpr = memExpr.Base;
            if (baseExpr != null && baseExpr is VariableExpression)
            {
                /// For Example: Get kk's  TableDeclaration
                VariableExpression varExpr = baseExpr as VariableExpression;
                if (varExpr.Var != null)
                {
                    return m_chunkDecl.GetGlobal(varExpr.Var.Name) as LuaClassDeclaration;
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

        protected FunctionDeclaration GetFunctionDeclrarion(FunctionStatement smt)
        {
            System.Diagnostics.Debug.Assert(smt != null);
            if (smt.IsLocal)
                return null;

            Expression expr = smt.Name;

            if (expr is VariableExpression)
            {
                /// example: function kk() 
                VariableExpression varExpr = expr as VariableExpression;
                if (varExpr.Var == null)
                {
                    return null;
                }

                return m_chunkDecl.GetGlobal(varExpr.Var.Name) as FunctionDeclaration;

            }
            else if (expr is MemberExpr)
            {
                /// example: function kk.bb()
                MemberExpr memExpr = expr as MemberExpr;

                LuaClassDeclaration parentDecl = FindLuaClassDeclaration(memExpr);
                if (parentDecl == null)
                {
                    return null;
                }

                bool IsStatic = ((memExpr.Indexer == ":") ? false : true);

                return parentDecl.GetMember(memExpr.Ident) as FunctionDeclaration;
            }

            return null;
        }


        protected void doAnaly(FunctionStatement smt){
            System.Diagnostics.Debug.Assert(smt != null);
            if (smt.IsLocal)
                return ;

            FunctionDeclaration funcDecl = GetFunctionDeclrarion(smt);
            if (funcDecl == null)
            {
                return;
            }

            if (smt.ReturnTypeList != null && smt.ReturnTypeList.Count > 0)
            {
                foreach (var rt in smt.ReturnTypeList)
                {
                    if (!string.IsNullOrEmpty(rt) && rt != TLuaGrammar.T_vary)
                    {
                        return;
                    }
                }
            }

            ReturnStatement firstReturn = null;
            foreach (var bodyStatement in smt.Body)
            {
                if (bodyStatement is ReturnStatement)
                {
                    firstReturn = bodyStatement as ReturnStatement;
                    break;
                }
            }

            if (firstReturn != null && firstReturn.Arguments != null && firstReturn.Arguments.Count > 0)
            {
                List<string> type_from_compute = new List<string>();
                funcDecl.ClearReturnType();
                foreach (Expression expr in firstReturn.Arguments)
                {
                    List<string> return_types = VisitorHelper.computeLocalType(expr, m_chunk, m_DeclarationMgr, smt,m_Scopes.Peek(), m_fileName, m_chunkDecl);
                    if (return_types != null)
                    {
                        foreach (string t in return_types)
                        {
                            funcDecl.AddReturnType(t);
                            type_from_compute.Add(t);
                        }
                    }
                }
                smt.ReturnTypeList = type_from_compute;
                funcDecl.FormatFunctionDesc();
            }
        }

    }
}
