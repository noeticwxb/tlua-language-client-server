using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpLua.Ast;
using SharpLua.Ast.Statement;

namespace TLua.Analysis
{
    /// <summary>
    /// 找所有的chunk，为outlining准备数据
    /// </summary>
    public class AnalyChunkVisitor: SharpLua.NodeVisitor
    {
        List<SharpLua.Ast.Chunk> m_ChunkList = new List<SharpLua.Ast.Chunk>();

        public IEnumerable<Chunk> ChunkList
        {
            get { return m_ChunkList; }
        }

        public override void Apply(Chunk smt)
        {
            m_ChunkList.Add(smt);
            base.Apply(smt);
        }
    }
}
