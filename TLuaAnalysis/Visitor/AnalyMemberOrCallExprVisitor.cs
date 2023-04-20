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
    /// 分析当前位置所在的语句是否包含成员调用，类似"GGG.kk"或者"GGG:kk"，甚至包括"GGG."的形式
    /// Or
    /// 分析当前位置所在的语句是否包含函数调用，类似"GGG.rr()"或者"fff()"
    /// 如果存在，找到当前的表达式和Scope
    /// </summary>
    public class AnalyMemberOrCallExprVisitor : NodeVisitor
    {

        Stack<Scope> m_ScopeStack = new Stack<Scope>();

        Stack<Chunk> m_ChunkStack = new Stack<Chunk>();

        /// 成员表达式或者函数表达式也是可以嵌套的。我们需要最里层的
        /// GGG:kkk(GGG.ere(),erer.ererer())
        Stack<MemberExpr> m_MemberExprStack = new Stack<MemberExpr>();
        Stack<CallExpr> m_CallExprStack = new Stack<CallExpr>();
        Stack<Scope> m_ResultScopeStack = new Stack<Scope>();
        Stack<Chunk> m_ResultChunkStack = new Stack<Chunk>();

        public MemberExpr MemberExpr
        {
            get
            {
                if (m_MemberExprStack.Count != 0)
                {
                    return m_MemberExprStack.Peek();
                }
                else
                {
                    return null;
                }
            }
        }

        public CallExpr CallExpr
        {
            get
            {
                if (m_CallExprStack.Count != 0)
                {
                    return m_CallExprStack.Peek();
                }
                else
                {
                    return null;
                }
            }
        }

        /// 默认是分析MemberExpr
        bool IsAnalyCallExpr
        {
            get;
            set;
        }

        public Scope Scope
        {
            get
            {
                if (m_ResultScopeStack.Count != 0)
                {
                    return m_ResultScopeStack.Peek();
                }
                else
                {
                    return null;
                }
            }
        }

        public SharpLua.Ast.Chunk Chunk
        {
            get
            {
                if (m_ResultChunkStack.Count != 0)
                {
                    return m_ResultChunkStack.Peek();
                }
                else
                {
                    return null;
                }
            }
        }

        int Line { get; set; }
        int Column { get; set; }

        public void Analy(Statement smt, int line, int col, bool analyCallExpr = false)
        {
            if (smt == null)
                return;

            m_ScopeStack.Clear();

            m_ChunkStack.Clear();

            m_MemberExprStack.Clear();
            m_CallExprStack.Clear();

            Line = line;
            Column = col;

            IsAnalyCallExpr = analyCallExpr;

            smt.Accept(this);
        }

        public override void Apply(Statement smt)
        {
            if (VisitorHelper.IsIncludeInStatement(smt, Line, Column))
            {
                if (smt.Scope != null)
                {
                    m_ScopeStack.Push(smt.Scope);
                }

                base.Apply(smt);

                if (smt.Scope != null)
                {
                    m_ScopeStack.Pop();
                }
            }
        }

        public override void Apply(Chunk smt)
        {
            if (VisitorHelper.IsIncludeInStatement(smt, Line, Column))
            {
                if (smt.Scope != null)
                {
                    m_ScopeStack.Push(smt.Scope);
                }

                m_ChunkStack.Push(smt);

                base.Traverse(smt);     // 用Traverse，直接压栈。 减少一次IsIncludeInStatement判断。

                m_ChunkStack.Pop();

                if (smt.Scope != null)
                {
                    m_ScopeStack.Pop();
                }
            }
        }

        public override void Apply(Expression expr)
        {
            if (expr.Scope != null)
            {
                m_ScopeStack.Push(expr.Scope);
            }

            base.Apply(expr);

            if (expr.Scope != null)
            {
                m_ScopeStack.Pop();
            }
        }

        /// MemberExpr 的数据结构和CallExpr不同，在Apply上有区别
        /// MemberExpr 判断的范围是indexer以及其后的标识符 。比如"kkkk.gggg"判断".gggg"
        public override void Apply(MemberExpr expr)
        {
            if (IsAnalyCallExpr)
            {
                base.Apply(expr);
            }
            else
            {
                if (VisitorHelper.IsIncludeInMemberExpr(expr, Line, Column))
                {
                    this.m_MemberExprStack.Push(expr);

                    /// SharpLua好像不是给每个Expression都设置了Scope。如果没有,这里取当前Scope栈的最后一个
                    if (expr.Scope != null)
                    {
                        this.m_ResultScopeStack.Push(expr.Scope);
                    }
                    else if (m_ScopeStack.Count > 0)
                    {
                        this.m_ResultScopeStack.Push(m_ScopeStack.Peek());
                    }

                    if (m_ChunkStack.Count > 0)
                    {
                        this.m_ResultChunkStack.Push(m_ChunkStack.Peek());
                    }
                }
                else
                {
                    base.Apply(expr);
                }
            }
        }

        /// CallExpr 判断范围是"（","）"之间。 括号中间可能还有调用表达式
        public override void Apply(CallExpr expr)
        {
            if (!IsAnalyCallExpr)
            {
                base.Apply(expr);
            }
            else
            {
                if (VisitorHelper.IsIncludeInCallExpr(expr, this.Line, this.Column))
                {

                    this.m_CallExprStack.Push(expr);

                    /// SharpLua好像不是给每个Expression都设置了Scope。如果没有,这里取当前Scope栈的最后一个
                    if (expr.Scope != null)
                    {
                        this.m_ResultScopeStack.Push(expr.Scope);
                    }
                    else if (m_ScopeStack.Count > 0)
                    {
                        this.m_ResultScopeStack.Push(m_ScopeStack.Peek());
                    }

                    if (m_ChunkStack.Count > 0)
                    {
                        this.m_ResultChunkStack.Push(m_ChunkStack.Peek());
                    }


                    base.Apply(expr);
                }
            }


        }
    }
}
