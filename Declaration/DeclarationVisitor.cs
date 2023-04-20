using System;
using System.Collections.Generic;
using System.Text;

namespace TLua.Analysis
{
    public class DeclarationVisitor
    {
        public virtual void Traverse(Declaration decl)
        {
            decl.Traverse(this);
        }

        public virtual void Apply(Declaration decl)
        {
            Traverse(decl);
        }

        public virtual void Apply(ChunkDeclaration decl)
        {
            Apply((Declaration)decl);
        }

        public virtual void Apply(FunctionDeclaration decl)
        {
            Apply((Declaration)decl);
        }

        public virtual void Apply(LuaClassDeclaration decl)
        {
            Apply((TypeDeclaration)decl);
        }

        public virtual void Apply(TypeDeclaration decl)
        {
            Apply((Declaration)decl);
        }

        public virtual void Apply(VariableDeclaration decl)
        {
            Apply((Declaration)decl);
        }

        public virtual void Apply(KeywordDeclaration decl)
        {
            Apply((Declaration)decl);
        }
    }
}
