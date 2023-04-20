using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast
{
    public class Chunk : Statement.Statement
    {
        public List<Statement.Statement> Body = new List<Statement.Statement>();

        public Chunk()
        {

        }

        public Chunk(Scope s)
        {
            Scope = s;
        }

        public override Statement.Statement Simplify()
        {
            for (int i = 0; i < Body.Count; i++)
                Body[i] = Body[i].Simplify();

            return this;
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if(Body!=null)
            {
                foreach(var item in Body)
                {
                    item.Accept(nv);
                }
            }

            base.Traverse(nv);
        }

        //显示本节点的log信息
        public override string ToLogString(int depth = 0)
        {
            string result = "";
            result += "[" + GetType().Name + "]" + " [" + Body.Count + "]";

            foreach (var entry in Body)
            {
                result += "\n";
                result += "-".Repeat(depth + 1);
                result += "entry:";
                result += (entry.ToLogString(depth+1));
            }
            return result;
        }
    }
}
