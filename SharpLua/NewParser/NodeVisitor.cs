using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpLua.Ast.Statement;
using SharpLua.Ast.Expression;
using SharpLua.Ast;

namespace SharpLua
{
    /// <summary>
    /// Visitor模式的访问器
    /// </summary>
    public class NodeVisitor
    {
        //public List<LuaSourceException> Errors = new List<LuaSourceException>();

        public bool ThrowParsingErrors = true;

        public void error(string msg)
        {
            //LuaSourceException ex = new LuaSourceException(line, col, msg);

            //Errors.Add(ex);
            ////Console.WriteLine(ex.GenerateMessage("sd"));
            //if (ThrowParsingErrors)
            //    throw ex;
        }

        public virtual void Traverse(AstNode node)
        {
            node.Traverse(this);          
        }

        public virtual void Apply(AstNode node)
        {
            Traverse(node);
        }

        public virtual void Apply(Variable v)
        {
            Apply((AstNode)v);
        }

        public virtual void Apply(Statement smt)
        {
            Apply((AstNode)smt);
        }

        public virtual void Apply(TLuaUnknownStatement smt)
        {
            Apply((Statement)smt);
        }

        public virtual void Apply(TLuaUsingStatement smt)
        {
            Apply((Statement)smt);
        }

        public virtual void Apply(Chunk smt)
        {
            Apply((Statement)smt);
        }

        public virtual void Apply(AssignmentStatement smt)
        {
            Apply((Statement)smt);
        }

        public virtual void Apply(AugmentedAssignmentStatement smt)
        {
            Apply((AssignmentStatement)smt);
        }

        public virtual void Apply(BreakStatement smt)
        {
            Apply((Statement)smt);
        }

        public virtual void Apply(CallStatement smt)
        {
            Apply((Statement)smt);
        }

        public virtual void Apply(ContinueStatement smt)
        {
            Apply((Statement)smt);
        }

        public virtual void Apply(DoStatement smt)
        {
            Apply((Chunk)smt);
        }

        public virtual void Apply(NumericForStatement smt)
        {
            Apply((Chunk)smt);
        }

        public virtual void Apply(GenericForStatement smt)
        {
            Apply((Chunk)smt);
        }

        public virtual void Apply(FunctionStatement smt)
        {
            Apply((Chunk)smt);
        }

        public virtual void Apply(GotoStatement smt)
        {
            Apply((Statement)smt);
        }

        public virtual void Apply(IfStmt smt)
        {
            Apply((Chunk)smt);
        }

        public virtual void Apply(SubIfStmt smt)
        {
            Apply((Chunk)smt);
        }

        public virtual void Apply(ElseIfStmt smt)
        {
            Apply((SubIfStmt)smt);
        }

        public virtual void Apply(ElseStmt smt)
        {
            Apply((SubIfStmt)smt);
        }

        public virtual void Apply(LabelStatement smt)
        {
            Apply((Statement)smt);
        }

        public virtual void Apply(RepeatStatement smt)
        {
            Apply((Chunk)smt);
        }
        public virtual void Apply(ReturnStatement smt)
        {
            Apply((Statement)smt);
        }
        public virtual void Apply(UsingStatement smt)
        {
            Apply((Chunk)smt);
        }
        public virtual void Apply(WhileStatement smt)
        {
            Apply((Chunk)smt);
        }

        public virtual void Apply(Expression expr)
        {
            Apply((AstNode)expr);
        }

        public virtual void Apply(AnonymousFunctionExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(BinOpExpr expr)
        {
            Apply((Expression)expr);
        }
        public virtual void Apply(BoolExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(CallExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(IndexExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(InlineFunctionExpression expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(MemberExpr expr)
        {
            Apply((Expression)expr);
        }
        public virtual void Apply(NilExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(NumberExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(StringCallExpr expr)
        {
            Apply((CallExpr)expr);
        }

        public virtual void Apply(StringExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(TableCallExpr expr)
        {
            Apply((CallExpr)expr);
        }

        public virtual void Apply(TableConstructorExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(TableConstructorKeyExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(TableConstructorNamedFunctionExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(TableConstructorStringKeyExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(TableConstructorValueExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(UnOpExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(VarargExpr expr)
        {
            Apply((Expression)expr);
        }

        public virtual void Apply(VariableExpression expr)
        {
            Apply((Expression)expr);
        }
    }
}
