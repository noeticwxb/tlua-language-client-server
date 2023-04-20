using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLua.Ast.Statement;
using SharpLua.Ast;

namespace TLua.Analysis
{
    public class AnalyChunkStackVisitor : SharpLua.NodeVisitor
    {

        /// 记录所有的chunk
        Stack<SharpLua.Ast.Chunk> m_ChunkStack = new Stack<SharpLua.Ast.Chunk>();

        int Line { get; set; }
        int Column { get; set; }

        public Chunk Result
        {
            get
            {
                if (m_ChunkStack.Count == 0)
                {
                    return null;
                }
                else
                {
                    return m_ChunkStack.Peek();
                }
            }
        }

        public Scope ResultScope
        {
            get 
            {
                if (Result == null)
                {
                    return null;
                }

                return Result.Scope;
            }
        }

        public void Analy(Statement smt, int line, int col)
        {
            try{
                if (smt == null)
                    return;

                Line = line;
                Column = col;

                smt.Accept(this);
            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
            }

        }

        public override void Apply(SharpLua.Ast.Chunk smt)
        {
            if (VisitorHelper.IsIncludeInStatement(smt, Line, Column))
            {
                m_ChunkStack.Push(smt);
                base.Apply(smt);
            }
        }
    }
}
