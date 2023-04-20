using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    public class IfStmt : Chunk
    {
        public List<SubIfStmt> Clauses = new List<SubIfStmt>();

        public override Statement Simplify()
        {
            for (int i = 0; i < Clauses.Count; i++)
                Clauses[i].Simplify();
            return base.Simplify();
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if(Clauses!=null)
            {
                foreach(var item in Clauses)
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
            result += "[" + GetType().Name + "]";

            foreach (var entry in Clauses)
            {
                result += "\n";
                result += "-".Repeat(depth + 1);
                result += "entry:";
                result += (entry.ToLogString(depth + 1));
            }
            return result;
        }
    }

    public abstract class SubIfStmt : Chunk
    {
        public SubIfStmt(Scope s)
            : base(s)
        {

        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }

    public class ElseIfStmt : SubIfStmt
    {
        public Expression.Expression Condition = null;

        public ElseIfStmt(Scope s)
            : base(new Scope(s))
        {

        }

        public override Statement Simplify()
        {
            Condition = Condition.Simplify();
            return base.Simplify();
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Condition != null)
                Condition.Accept(nv);

            base.Traverse(nv);
        }

        //显示本节点的log信息
        public override string ToLogString(int depth = 0)
        {
            string result = "";
            result += "[" + GetType().Name + "]";
            result += "\n";
            result += "-".Repeat(depth + 1);
            result += "Condition:";
            result += Condition.ToLogString(depth+1);
            return result;
        }
    }

    public class ElseStmt : SubIfStmt
    {
        public ElseStmt(Scope s)
            : base(new Scope(s))
        {

        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
