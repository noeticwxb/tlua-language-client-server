using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpLua;
using SharpLua.Ast;
using SharpLua.Ast.Statement;
using SharpLua.Ast.Expression;

namespace TLua.Analysis
{
    // 从一个表达式，分析表达式的最终类型
    class AnalyTypeFromExprVisitor: SharpLua.NodeVisitor
    {
        List<string> m_typeList = new List<string>();

        Chunk m_Chunk;
        DeclaraionManager m_declManager;
        ChunkDeclaration m_chunkDecl;
        FunctionStatement m_curFunc;
        Scope m_curScope;
        string m_FileName;

        public List<string> TypeList
        {
            get
            {
                return this.m_typeList;
            }
        }

        public Scope CurScope
        {
            get {
                if(m_curScope != null){
                    return m_curScope;
                }
                Scope curScope = null;
                if (m_curFunc != null)
                {
                    curScope = m_curFunc.Scope;
                }
                if (m_curFunc == null && m_Chunk != null)
                {
                    curScope = m_Chunk.Scope;
                }
                return curScope;
            }

        }

        public void Excute(Expression expr,  Chunk c , DeclaraionManager _declMgr, FunctionStatement _curFunc, Scope _curScope, string fileName, ChunkDeclaration fileChunkDecl){
            if(expr==null){
                m_typeList.Add(SharpLua.TLuaGrammar.T_vary);
                return;
            }

            if (expr is AnonymousFunctionExpr)
            {
                m_typeList.Add(SharpLua.TLuaGrammar.T_FUNCTION);
                return;
            }
            
            m_Chunk = c;
            m_declManager = _declMgr;
            m_curFunc = _curFunc;
            m_curScope = _curScope;
            m_FileName = fileName;

            m_chunkDecl = fileChunkDecl;

            expr.Accept(this);
        }

        public override void Apply(CallExpr expr)
        {
            if (m_declManager == null)
            {
                m_typeList.Add(SharpLua.TLuaGrammar.T_vary);
                return;
            }

            CallExpr callExpr = expr as CallExpr;
            VariableExpression funcNameExpr = callExpr.Base as VariableExpression;
            if (funcNameExpr != null && funcNameExpr.Var != null)
            {
                Declaration decl = m_declManager.FindDeclrationByFullName(funcNameExpr.Var.Name);

                if (decl is LuaClassDeclaration)
                {
                    m_typeList.Add((decl as LuaClassDeclaration).Name);
                    return;
                }
                else if (decl is FunctionDeclaration)
                {
                    FunctionDeclaration funcDecl = decl as FunctionDeclaration;
                    if(funcDecl.ReturnTypeList != null){
                        m_typeList.AddRange(funcDecl.ReturnTypeList);
                    }
                }
                else
                {
                    m_typeList.Add(SharpLua.TLuaGrammar.T_vary);
                    return;
                }
            }

            ExcuteExprVisitor excuterVT = new ExcuteExprVisitor();
            excuterVT.ExcuteCallExpr(callExpr, m_declManager, m_Chunk, this.CurScope, m_curFunc);

            ExcuteExprVisitor.CallType ret_ct = excuterVT.PeekCTStack();
            Declaration ret_decl = excuterVT.PeekDeclStack();

            if (ret_decl is FunctionDeclaration)
            {
                FunctionDeclaration func_Decl = ret_decl as FunctionDeclaration;
                foreach (string rt in func_Decl.ReturnTypeList)
                {
                    m_typeList.Add(rt);
                }
                return;
            }

            m_typeList.Add(SharpLua.TLuaGrammar.T_vary);
            return;
        }

        public override void Apply(MemberExpr expr)
        {
            if (m_declManager == null)
            {
                m_typeList.Add(SharpLua.TLuaGrammar.T_vary);
                return;
            }

            MemberExpr mbrExpr = expr as MemberExpr;
            ExcuteExprVisitor excuterVT = new ExcuteExprVisitor();

            excuterVT.ExcuteMemberExpr(mbrExpr, m_declManager, m_Chunk, this.CurScope, m_curFunc);
            ExcuteExprVisitor.CallType ct = excuterVT.PeekCTStack();
            Declaration ret_decl = excuterVT.PeekDeclStack();

            if (ret_decl is TypeDeclaration)
            {
                m_typeList.Add((ret_decl as TypeDeclaration).Name);
                return;
            }

            if (ret_decl is LuaClassDeclaration)
            {
                m_typeList.Add((ret_decl as LuaClassDeclaration).Name);
                return;
            }


            if (ret_decl is FunctionDeclaration)
            {
                m_typeList.Add(SharpLua.TLuaGrammar.T_FUNCTION);
                return;
            }

            m_typeList.Add(SharpLua.TLuaGrammar.T_vary);
            return;
        }

        private Declaration GetOrCreateTempClassDefine(string className)
        {
            if (this.m_chunkDecl == null)
            {
                return null;
            }

            Declaration find = this.m_chunkDecl.GetLocal(className);
            if (find == null)
            {
                LuaClassDeclaration newDecl = new LuaClassDeclaration();
                find = newDecl;
                newDecl.IsLocalTable = true;
                find.Name = className;
                this.m_chunkDecl.AddLocal(find);
            }

            return find;
        }

        private VariableDeclaration createDeclarationFrom(TableConstructorKeyExpr expr)
        {
            if (expr == null)
            {
                return null;
            }

            if (!(expr.Key is StringExpr))
            {
                return null;
            }

            VariableDeclaration varDecl = new VariableDeclaration();
            varDecl.Name = (expr.Key as StringExpr).Value;
            if (!string.IsNullOrEmpty(expr.KeyType) && expr.KeyType != TLuaGrammar.T_vary)
            {
                varDecl.Type = expr.KeyType;
            }
            else
            {
                List<string> type_names = VisitorHelper.computeLocalType(expr.Value, m_Chunk, m_declManager, m_curFunc,CurScope,  m_FileName, m_chunkDecl);
                if (type_names != null && type_names.Count > 0)
                {
                    varDecl.Type = type_names[0];
                }
            }

            return varDecl;
           
        }

        private VariableDeclaration createDeclarationFrom(TableConstructorStringKeyExpr expr)
        {
            if (expr == null)
            {
                return null;
            }

            VariableDeclaration varDecl = new VariableDeclaration();
            varDecl.Name = expr.Key;
            if (!string.IsNullOrEmpty(expr.KeyType) && expr.KeyType != TLuaGrammar.T_vary)
            {
                varDecl.Type = expr.KeyType;        
            }
            else
            {
                List<string> type_names = VisitorHelper.computeLocalType(expr.Value, m_Chunk, m_declManager, m_curFunc,CurScope, m_FileName, m_chunkDecl);
                if(type_names !=null && type_names.Count > 0 ){
                    varDecl.Type = type_names[0];
                } 
            }

            return varDecl;
        }

        public void CreateVaribleDeclForTable(List<Expression> entryList, LuaClassDeclaration classDecl, int line, int col)
        {
            if (entryList == null || entryList.Count == 0)
            {
                return;
            }

            foreach (var expr in entryList)
            {
                VariableDeclaration varDecl = null;
                if (expr is TableConstructorKeyExpr)
                {
                    varDecl = createDeclarationFrom(expr as TableConstructorKeyExpr);
                }
                else if (expr is TableConstructorStringKeyExpr)
                {
                    varDecl = createDeclarationFrom(expr as TableConstructorStringKeyExpr);
                }

                if (varDecl != null)
                {
                    varDecl.FileName = m_FileName;
                    varDecl.Col = line;
                    varDecl.Line = col;
                    varDecl.IsStatic = false;

                    varDecl.DisplayText = varDecl.Name;
                    varDecl.ReadOnly = false;
                    varDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Instace_Variable);

                    varDecl.Description = string.Format("{0} {1} in {2}",varDecl.Type, varDecl.Name, classDecl.Name );

                    classDecl.AddProperty(varDecl, false);
                }
            }
        }
    
        public override void Apply(TableConstructorExpr expr)
        {
            string table_id = expr.Id;
            string className = string.Format("{0}:{1}", m_FileName, table_id);
            LuaClassDeclaration classDecl = GetOrCreateTempClassDefine(className) as LuaClassDeclaration;
            if (classDecl == null)
            {
                return;
            }

            classDecl.Name = className;
            classDecl.Line = expr.Line;
            classDecl.Col = expr.Column;
            classDecl.FileName = m_FileName;
            classDecl.DisplayText = className;
            classDecl.Description = "temp table class " + classDecl.Name;
            classDecl.FullName = classDecl.Name;
            classDecl.TypeImageIndex = AnalysisConfig.TypeImageIndex(AnalysisType.Class_Type);

            this.CreateVaribleDeclForTable(expr.EntryList, classDecl, expr.Line, expr.Column);

            m_typeList.Add(className);
            return;
        }

        public override void Apply(StringExpr expr)
        {
            m_typeList.Add(SharpLua.TLuaGrammar.T_string);
        }

        public override void Apply(BoolExpr expr)
        {
            m_typeList.Add(SharpLua.TLuaGrammar.T_bool);
        }

        public override void Apply(NumberExpr expr)
        {
            m_typeList.Add(SharpLua.TLuaGrammar.T_number);
        }

        public override void Apply(VariableExpression expr)
        {
            var varExpr = expr as VariableExpression;
            if (varExpr.Var != null)
            {
                if (varExpr.Var.Name == "self")
                {
                    if (m_curFunc != null)
                    {
                        string className = VisitorHelper.FindClassNameInFunctionSmt(m_curFunc, true, false);
                        if (!String.IsNullOrEmpty(className))
                        {
                            m_typeList.Add(className);
                        }
                        else
                        {
                            m_typeList.Add(className);
                        }
                        
                    }     
                }
                else
                {
                    m_typeList.Add(varExpr.Var.Type);
                }
                
            }
        }


    }
}
