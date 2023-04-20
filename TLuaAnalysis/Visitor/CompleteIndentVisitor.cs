using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLua.Analysis
{
    public class CompleteIndentVisitor: SharpLua.NodeVisitor
    {
        public int IndentLine { get; set; }

        public int IndentColumn { get; set; }

        public bool IsFuncParam { get; set; }

        public bool IsFuncName { get; set; }

        public CompleteIndentVisitor(int line, int col)
        {
            IndentLine = line;
            IndentColumn = col;

            IsFuncParam = false;
            IsFuncName = false;
        }

        public override void Apply(SharpLua.Ast.Statement.FunctionStatement smt)
        {

            if( smt.Arguments != null )
            {
                foreach(var item in smt.Arguments)
                {
                    if (item != null)
                    {
                        if (item.Line == IndentLine && item.Column == IndentColumn)
                        {
                            IsFuncParam = true;
                            return;
                        }
                    }
                }
            }

            SharpLua.Ast.Expression.VariableExpression funcName = smt.Name as SharpLua.Ast.Expression.VariableExpression;
            if(funcName!=null && funcName.Var!=null)
            {
                if(funcName.Var.Line == IndentLine && funcName.Var.Column == IndentColumn)
                {
                    IsFuncName = true;
                    return;
                }
            }

            base.Apply(smt);
        }

    }
}
