using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TLua.Analysis;

namespace TLuaGrammarExplorer
{
    class CreateDeclarationTreeVisitor : DeclarationVisitor
    {
        Stack<DeclTreeViewItem> m_Stack = new Stack<DeclTreeViewItem>();

        public DeclTreeViewItem Root { get; set; }

        public CreateDeclarationTreeVisitor()
        {
            Root = new DeclTreeViewItem();
            Root.Header = "Root";
            m_Stack.Push(Root);
        }

        public override void Apply(Declaration node)
        {
            DeclTreeViewItem parent = m_Stack.Peek();
            DeclTreeViewItem item = new DeclTreeViewItem();
            item.Header = node.GetType().Name;
            item.node = node;

            parent.Items.Add(item);

            m_Stack.Push(item);
            base.Apply(node);
            m_Stack.Pop();
        }
    }
}
