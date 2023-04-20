﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua.Ast.Expression
{
    public class AnonymousFunctionExpr : Expression
    {
        public List<Statement.Statement> Body = null;
        public bool IsVararg = false;
        public List<Variable> Arguments = new List<Variable>();

        List<string> m_ReturnTypeList = null;
        public List<string> ReturnTypeList
        {
            get
            {
                return m_ReturnTypeList;
            }
            set
            {
                m_ReturnTypeList = value;
            }
        }

        public override Expression Simplify()
        {
            for (int i = 0; i < Body.Count; i++)
                Body[i] = Body[i].Simplify();

            if (Refactoring.CanInline(this))
                return Refactoring.InlineFunction(this).Simplify(); // Simplify call here may be redundant

            return this;
        }

        public override void Traverse(NodeVisitor nv)
        {
            if (Arguments!=null)
            {
                foreach(var itemVar in Arguments)
                {
                    itemVar.Accept(nv);
                }
            }

            if (Body != null)
            {
                foreach (Statement.Statement smt in Body)
                {
                    smt.Accept(nv);
                }
            }
        
            base.Traverse(nv);
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }
    }
}
