using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Statement
{
    public class NumericForStatement : Chunk
    {
        public Variable Variable = null;
        public Expression.Expression Start = null;
        public Expression.Expression End = null;
        public Expression.Expression Step = null;

        public NumericForStatement(Scope s)
            : base(new Scope(s))
        {

        }

        public override Statement Simplify()
        {
            Start = Start.Simplify();
            End = End.Simplify();
            Step = Step.Simplify();
            return base.Simplify();
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Variable != null)
                Variable.Accept(nv);

            if (Start != null)
                Start.Accept(nv);

            if (End != null)
                End.Accept(nv);

            if (Step != null)
                Step.Accept(nv);

            base.Traverse(nv);
        }
    }

    public class GenericForStatement : Chunk
    {
        public List<Variable> VariableList = null;
        public List<Expression.Expression> Generators = null;

        public GenericForStatement(Scope s)
            : base(new Scope(s))
        {

        }

        public override Statement Simplify()
        {
            for (int i = 0; i < Generators.Count; i++)
                Generators[i] = Generators[i].Simplify();
            return base.Simplify();
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        public override void Traverse(NodeVisitor nv)
        {
            if(Generators!=null)
            {
                foreach(var item in Generators)
                {
                    item.Accept(nv);
                }
            }
            base.Traverse(nv);
        }
    }
}
