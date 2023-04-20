using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpLua;

namespace TLuaGrammarExplorer
{
    class CreateAstTreeVisitor: NodeVisitor
    {

        Stack<AstTreeViewItem> m_Stack = new Stack<AstTreeViewItem>();
        public AstTreeViewItem Root { get; set; }

        public CreateAstTreeVisitor()
        {
            Root = new AstTreeViewItem();
            Root.Header = "Root";
            m_Stack.Push(Root);
        }

        public override void Apply(SharpLua.Ast.AstNode node)
        {
            AstTreeViewItem parent = m_Stack.Peek();
            AstTreeViewItem item = new AstTreeViewItem();
            item.Header = node.GetType().Name;
            item.node = node;

            parent.Items.Add(item);

            m_Stack.Push(item);
            base.Apply(node);
            m_Stack.Pop();
        }
    }
}
