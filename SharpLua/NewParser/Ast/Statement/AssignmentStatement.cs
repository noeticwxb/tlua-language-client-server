using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//解决赋值语句缺少元素时报错问题 modified by zheng.che @ 2015-11-20

namespace SharpLua.Ast.Statement
{
    public class AssignmentStatement : Statement
    {
        public List<Expression.Expression> Lhs = new List<Expression.Expression>();
        public List<Expression.Expression> Rhs = new List<Expression.Expression>();
        public bool IsLocal = false;

        public override Statement Simplify()
        {
            for (int i = 0; i < Lhs.Count; i++)
                Lhs[i] = Lhs[i].Simplify();
            for (int i = 0; i < Rhs.Count; i++)
                Rhs[i] = Rhs[i].Simplify();
            return base.Simplify();
        }

        public override void Traverse(NodeVisitor nv)
        {
            if(Lhs!=null)
            {
                foreach(var lhsItem in Lhs)
                {
                    //by zheng.che
                    if (lhsItem != null)
                        lhsItem.Accept(nv);
                }
            }

            if(Rhs!=null)
            {
                foreach(var rhsItem in Rhs)
                {
                    //by zheng.che
                    if (rhsItem != null)
                        rhsItem.Accept(nv);
                }
            }

            base.Traverse(nv);
        }

        public override void Accept(NodeVisitor nv)
        {
            nv.Apply(this);
        }

        //显示本节点的log信息
        public override string ToLogString(int depth = 0)
        {
            string result = "";
            result += "[" + GetType().Name + "]" + " IsLocal:" + IsLocal;
            foreach (var lhsItem in Lhs)
            {
                result += "\n";
                result += "-".Repeat(depth + 1);
                result += "lhsItem:";
                result += lhsItem != null ? lhsItem.ToLogString(depth + 1) : "(null)";
            }
            foreach (var rhsItem in Rhs)
            {
                result += "\n";
                result += "-".Repeat(depth + 1);
                result += "rhsItem:";
                result += rhsItem != null ? rhsItem.ToLogString(depth + 1) : "(null)";
            }
            return result;
        }
    }
}
