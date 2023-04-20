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
    /// <summary>
    /// 把当前点的Scope找到
    /// </summary>
    public class FindCurScopeVisitor : SharpLua.NodeVisitor
    {

        Stack<Scope> m_ScopeStack = new Stack<Scope>();

        public ChunkDeclaration Chunk { get; set; }

        /// 寻找Line和Colume处的所有可用的局部变量
        int m_TokenLine = -1;
        int m_TokenColumn = -1;

        public void Analy(Chunk c, int curLine, int curColumn, string fileName)
        {
            if (c == null)
                return;

            m_TokenLine = curLine;
            m_TokenColumn = curColumn;

            c.Accept(this);

            if (m_ScopeStack.Count == 0)
                return;


            Chunk = new ChunkDeclaration();

            Scope curScope = m_ScopeStack.Peek();

            FillWithVars(curScope.GetLocals(true,true,false), Chunk, fileName);           
           
        }

        static void FillWithVars(List<Variable> vars,  ChunkDeclaration declParent, string fileName)
        {
            if (vars == null)
            {
                return;
            }

            foreach (var item in vars)
            {
                VariableDeclaration varDecl = CreateVariableDeclaraion(item, fileName);
                if (varDecl != null)
                {
                    /// ResultScope.GetLocals 的顺序是从最里层的Scope开始的，如果有同名的local变量，我们只使用最里层的。
                    declParent.AddLocal(varDecl, false);
                }
            }
        }

        public static VariableDeclaration CreateVariableDeclaraion(Variable var, string fileName)
        {
            if (var == null)
                return null;

            VariableDeclaration varDecl = new VariableDeclaration();
            varDecl.FileName = fileName;
            varDecl.Col = var.Column;
            varDecl.Line = var.Line;
            varDecl.IsStatic = true;
            varDecl.Name = var.Name;
            varDecl.ReadOnly = false;
            varDecl.Type = var.Type;
            varDecl.DisplayText = varDecl.Name;

            if (var.IsGlobal)
            {
                // global
                varDecl.Description = AnalysisConfig.Label_GlobalVar + varDecl.Type + " " + varDecl.Name;
                varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Global_Variable);
            }
            else if (var.IsFuncParam )
            {
                // local but funcparam
                varDecl.Description = AnalysisConfig.Label_Parameter + varDecl.Type + " " + varDecl.Name;
                varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Function_Param);
            }
            else
            {
                // local but not func param
                varDecl.Description = AnalysisConfig.Label_LocalVar + varDecl.Type + " " + varDecl.Name;
                varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Local_Variable);
            }
        
            return varDecl;
        }

        //public override void Apply(Chunk smt)
        //{
        //    if (VisitorHelper.IsIncludeInStatement(smt,m_TokenLine,m_TokenColumn))
        //    {
        //        Scope s = smt.Scope;
        //        if(s!=null)
        //        {
        //            m_ScopeStack.Push(s);
        //        }
        //        base.Apply(smt);
        //        if(s!=null)
        //        {
        //            m_ScopeStack.Pop();
        //        }
        //    }
        //}

        public override void Apply(Statement smt)
        {
           if (VisitorHelper.IsIncludeInStatement(smt, m_TokenLine, m_TokenColumn))
            {
                Scope s = smt.Scope;
                if (s != null)
                {
                    m_ScopeStack.Push(s);
                }
                base.Apply(smt);
            }
        }

        //public override void Apply(AnonymousFunctionExpr expr)
        //{
        //    Scope s = expr.Scope;
        //    if (s != null)
        //    {
        //        m_ScopeStack.Push(s);
        //    }

        //    base.Apply(expr);

        //    if (s != null)
        //    {
        //        m_ScopeStack.Pop();
        //    }
        //}

    }

}
