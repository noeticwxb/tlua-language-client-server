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
    public class VisitorHelper
    {
        public static void GetBeginAndEnd(Statement smt, out Token beginToken, out Token endToken)
        {
            if (smt != null)
            {
                GetBeginAndEnd(smt.ScannedTokens, out beginToken, out endToken);
            }
            else
            {
                beginToken = null;
                endToken = null;
            }
        }

        public static void GetBeginAndEnd(List<Token> scanned, out Token beginToken, out Token endToken)
        {
            beginToken = null;
            endToken = null;

            if (scanned != null && scanned.Count > 1)
            {
                if (scanned[0].Type != TokenType.EndOfStream)
                {
                    beginToken = scanned[0];
                }

                /// 反向查找第一个不是TokenType.EndOfStream的

                for (int iReverse = scanned.Count - 1; iReverse >= 0; --iReverse)
                {
                    if (scanned[iReverse].Type != TokenType.EndOfStream)
                    {
                        endToken = scanned[iReverse];
                        break;
                    }
                }

                if (endToken == null)
                {
                    endToken = beginToken;
                }
            }
            else if (scanned != null && scanned.Count == 1)
            {
                if (scanned[0].Type != TokenType.EndOfStream)
                {
                    beginToken = scanned[0];
                    endToken = beginToken;
                }
            }
        }

        public static bool IsInRange(int line, int column, int beginLine, int beginColumn, int endLine, int endColumn)
        {
            if (beginLine > endLine)
            {
                return false;
            }

            if (line < beginLine || line > endLine)
            {
                return false;
            }

            if (line == beginLine && column < beginColumn)
            {
                return false;
            }

            if (line == endLine && column >= endColumn)
            {
                return false;
            }

            return true;
        }


        public static bool IsIncludeInStatement(Statement smt, int token_line, int token_col)
        {
            Token beginToken;
            Token endToken;
            VisitorHelper.GetBeginAndEnd(smt, out beginToken, out endToken);

            if (beginToken != null && endToken != null)
            {
                return IsInRange(token_line,
                                 token_col,
                                 beginToken.Line,
                                 beginToken.Column,
                                 endToken.Line,
                                 endToken.Column + endToken.Data.Length);
            }

            return false;
        }

        public static bool IsIncludeInMemberExpr(MemberExpr expr, int token_line, int token_col)
        {
            if (expr == null)
            {
                return false;
            }

            /// 在没有完整输入的情况下，标识符可能是空。但是indexer肯定存在，否则解析不出MemberExpr

            int begineLine = expr.IndexerLine;
            int beginColumn = expr.IndexerColumn;

            int endLine = expr.Line;
            int endColumn = expr.Column;

            if (endLine == -1)
                endLine = begineLine;

            if (endColumn == -1)
                endColumn = beginColumn;

            if (!string.IsNullOrEmpty(expr.Ident))
            {
                endColumn += expr.Ident.Length;
            }
            else
            {
                endColumn += 1; //  补充一个结尾大小
            }

            return IsInRange(token_line, token_col, begineLine, beginColumn, endLine, endColumn);

        }

        public static bool IsIncludeInCallExpr(CallExpr expr, int token_line, int token_col)
        {
            if (expr == null)
                return false;

            return IsInRange(token_line,
                             token_col,
                             expr.OpenBracketLine,
                             expr.OpenBracketColumn,
                             expr.CloseBracketLine,
                             expr.CloseBracketColumn + 1
                             );
        }

        public static bool IsNullOrVary(string name)
        {
            if (string.IsNullOrEmpty(name) 
                || name == TLuaGrammar.T_vary 
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string FindClassNameInFunctionSmt(SharpLua.Ast.Statement.FunctionStatement smt, bool colonIndexer, bool dotIndexer)
        {
            if (smt.IsLocal)
            {
                return string.Empty;
            }

            MemberExpr memExpr = smt.Name as MemberExpr;

            if (memExpr != null)
            {
                VariableExpression varExpr = memExpr.Base as VariableExpression;
                if (varExpr != null && varExpr.Var != null)
                {
                    if (colonIndexer && memExpr.Indexer == ":")
                    {
                        return varExpr.Var.Name;
                    }
                    else if (dotIndexer && memExpr.Indexer == ".")
                    {
                        return varExpr.Var.Name;
                    }
                }
            }

            return string.Empty;
        }


        public static int FindTokenIndex(List<Token> scanned, int token_line, int token_column)
        {
            if (scanned == null)
                return -1;

            int findIndex = 0;
            for (; findIndex < scanned.Count; ++findIndex)
            {
                Token t = scanned[findIndex];
                int endColumn = string.IsNullOrEmpty(t.Data) ? t.Column + 1 : t.Column + t.Data.Length;
                if (t.Line == token_line
                    && token_column >= t.Column
                    && token_column < endColumn
                    )
                {
                    break;
                }
            }

            return findIndex;
        }

        public static Token FindToken(List<Token> scanned, int token_line, int token_column, bool findPrevious)
        {
            int findIndex = FindTokenIndex(scanned, token_line, token_column);

            if (!findPrevious)
            {
                if (findIndex >= 0 && findIndex < scanned.Count)
                {
                    return scanned[findIndex];
                }
            }
            else
            {
                if (findIndex > 0 && findIndex < scanned.Count)
                {
                    return scanned[findIndex - 1];
                }
            }

            return null;
        }


      
        // 根据表达式自动计算类型. 返回的list一定有效
        static public List<string> computeLocalType(Expression expr, Chunk chunk,  DeclaraionManager declManager, FunctionStatement curFunc, Scope scope,  string fileName, ChunkDeclaration chunkDecl)
        {
            AnalyTypeFromExprVisitor nv = new AnalyTypeFromExprVisitor();
            nv.Excute(expr, chunk, declManager, curFunc, scope, fileName, chunkDecl);
            return nv.TypeList;
        }
    }
}
