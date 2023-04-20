using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpLua.Ast.Expression;
using SharpLua.Ast.Statement;
using SharpLua.Ast;

namespace SharpLua
{
    public class Parser
    {
        public List<LuaSourceException> Errors = new List<LuaSourceException>();
        public bool ThrowParsingErrors = true;


        public bool UseUnKnownStatement = false;

        TokenReader reader;

        Token endPerfectPos;

        public Parser(TokenReader tr)
        {
            reader = tr;
        }

        public void error(string msg, int line = -1, int col = -1, Token tok = null)
        {
            if (tok == null)
                tok = reader.Peek();

            if (line == -1)
                line = tok.Line;
            if (col == -1)
                col = tok.Column;
            msg += ", got '" + tok.Data + "'";

            LuaSourceException ex = new LuaSourceException(line, col, msg);
            ex.ErrorToken = tok;
            ex.ReverseToken = reader.ReversePeek(0);
            Errors.Add(ex);
            //Console.WriteLine(ex.GenerateMessage("sd"));
            if (ThrowParsingErrors)
                throw ex;
        }

        public void error_end(string msg, int line = -1, int col = -1, Token tok = null)
        {
            if (tok == null)
                tok = reader.Peek();

            if (endPerfectPos != null)
            {
                tok = endPerfectPos;
            }

            if (line == -1)
                line = tok.Line;
            if (col == -1)
                col = tok.Column;
            msg += ", got '" + tok.Data + "'";

            LuaSourceException ex = new LuaSourceException(line, col, msg);
            ex.ErrorToken = tok;
            ex.ReverseToken = reader.ReversePeek(0);
            ex.IsEnd = true;
            Errors.Add(ex);
            //Console.WriteLine(ex.GenerateMessage("sd"));
            if (ThrowParsingErrors)
                throw ex;
        }

        string PaserTypeName()
        {
            string typename = reader.Get().Data;

            while (reader.ConsumeSymbol('.'))
            {
                if (!reader.Is(TokenType.Ident))
                {
                    error(TLuaGrammar.T_ErrorMsg);
                    typename += ".";
                    break;
                }
                else
                {
                    typename += "." + (reader.Get().Data);
                }
            }

            if (reader.ConsumeSymbol('<'))
            {
                typename += "<";
                typename += PaserTypeName();
                while (reader.ConsumeSymbol(','))
                {
                    typename += ",";
                    typename += PaserTypeName();                
                }

                if (reader.ConsumeSymbol('>'))
                {
                    typename += ">";
                    typename = "@" + typename;
                }
                else
                {
                    error(TLuaGrammar.T_ErrorMsg);
                }
            }
       
            return typename;
        }

        string ParseVariableType(Variable arg = null, bool useVaryAsDefault = true)
        {
            if (reader.ConsumeKeyword(TLuaGrammar.T_as))
            {
                if (reader.IsVarTypeKeyWord())
                {
                    string typename = reader.Get().Data;
                    if(arg!=null)
                    {
                        arg.Type = typename;
                    }
                    return typename;
                }
                else if (reader.Is(TokenType.Ident))
                {
                    string typename = PaserTypeName();
                    if (arg != null)
                    {
                        arg.Type = typename;
                    }
                    return typename;
                }
                else 
                {
                    error(TLuaGrammar.T_ErrorMsg);
                }
            }
            return useVaryAsDefault ? TLuaGrammar.T_vary : string.Empty;
        }

        List<string> ParseReturnTypeNameList()
        {
            List<string> returnTypeNameList = new List<string>();
            while (true)
            {
                if (reader.ConsumeKeyword(TLuaGrammar.T_as))
                {
                    if (reader.IsReturnTypeKeyWord())
                    {
                        returnTypeNameList.Add(reader.Get().Data);
                        if (!reader.ConsumeSymbol(','))
                        {
                            break;
                        }
                    }
                    else if( reader.IsKeyword(TLuaGrammar.T_void) )
                    {
                        returnTypeNameList.Add(reader.Get().Data);
                        break;
                    }
                    else if (reader.Is(TokenType.Ident))
                    {
                        string typename = PaserTypeName();

                        returnTypeNameList.Add(typename);
                        if (!reader.ConsumeSymbol(','))
                        {
                            break;
                        }
                    }
                    else
                    {
                        error(TLuaGrammar.T_ReturnErrorMsg);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return returnTypeNameList;
        }

        AnonymousFunctionExpr ParseExprFunctionArgsAndBody(Scope scope)
        {
            AnonymousFunctionExpr func = new AnonymousFunctionExpr();
            func.Scope = new Scope(scope);

            if (reader.ConsumeSymbol('(') == false)
                error("'(' expected");

            // arg list
            List<Variable> arglist = new List<Variable>();
            bool isVarArg = false;
            while (reader.ConsumeSymbol(')') == false)
            {
                if (reader.Is(TokenType.Ident))
                {
                    Variable arg = new Variable();
                    arg.Line = reader.Peek().Line;
                    arg.Column = reader.Peek().Column;
                    arg.Name = reader.Get().Data;
                    ParseVariableType(arg);
                    arg.IsFuncParam = true;
                    func.Scope.AddLocal(arg);
                    arglist.Add(arg);
                    if (!reader.ConsumeSymbol(','))
                    {
                        if (reader.ConsumeSymbol(')'))
                            break;
                        else
                            error("')' expected");
                        break;
                    }
                }
                else if (reader.ConsumeSymbol("..."))
                {
                    isVarArg = true;
                    if (!reader.ConsumeSymbol(')'))
                        error("'...' must be the last argument of a function");
                    break;
                }
                else
                {
                    error("Argument name or '...' expected");
                    break;
                }
            }

            // optional return type
            func.ReturnTypeList = ParseReturnTypeNameList();

            // body
            List<Statement> body = ParseStatementList(func.Scope);
            // end
            if (!reader.ConsumeKeyword("end"))
                error_end("'end' expected after function body");

            //nodeFunc.AstType = AstType.Function;
            func.Arguments = arglist;
            func.Body = body;
            func.IsVararg = isVarArg;

            return func;
        }

        void recordPerfectEnd()
        {
            this.endPerfectPos = reader.Peek();
        }

        FunctionStatement ParseFunctionArgsAndBody(Scope scope)
        {
            FunctionStatement func = new FunctionStatement(scope);

            if (reader.ConsumeSymbol('(') == false)
                error("'(' expected");

            // arg list
            List<Variable> arglist = new List<Variable>();
            bool isVarArg = false;
            while (reader.ConsumeSymbol(')') == false)
            {
                if (reader.Is(TokenType.Ident))
                {
                    Variable arg = new Variable();
                    arg.Line = reader.Peek().Line;
                    arg.Column = reader.Peek().Column;
                    arg.Name = reader.Get().Data;
                    ParseVariableType(arg);
                    arg.IsFuncParam = true;
                    func.Scope.AddLocal(arg);
                    arglist.Add(arg);
                    if (!reader.ConsumeSymbol(','))
                    {
                        if (reader.ConsumeSymbol(')'))
                            break;
                        else
                            error("')' expected");
                        break;
                    }
                }
                else if (reader.ConsumeSymbol("..."))
                {
                    isVarArg = true;
                    if (!reader.ConsumeSymbol(')'))
                        error("'...' must be the last argument of a function");
                    break;
                }
                else
                {
                    error("Argument name or '...' expected");
                    break;
                }
            }

            // optional return type
            func.ReturnTypeList = ParseReturnTypeNameList();

            // body
            List<Statement> body = ParseStatementList(func.Scope);
           
            // end
            if (!reader.ConsumeKeyword("end"))
                error_end("'end' expected after function body");

            //nodeFunc.AstType = AstType.Function;
            func.Arguments = arglist;
            func.Body = body;
            func.IsVararg = isVarArg;

            return func;
        }

        Expression ParsePrimaryExpr(Scope c)
        {
            //Console.WriteLine(tok.Peek().Type + " " + tok.Peek().Data);
            if (reader.ConsumeSymbol('('))
            {
                Expression ex = ParseExpr(c);
                if (!reader.ConsumeSymbol(')'))
                    error("')' expected");

                // save the information about parenthesized expressions somewhere
                ex.ParenCount = ex.ParenCount + 1;
                return ex;
            }
            else if (reader.Is(TokenType.Ident))
            {
                Token id = reader.Get();
                
                VariableExpression v = new VariableExpression();
                Variable var = c.GetLocal(id.Data);
                if (var == null)
                {
                    var = c.GetGlobal(id.Data);
                    if (var == null)
                    {
                        v.Var = c.CreateGlobal(id.Data);
                        v.Var.Line = id.Line;
                        v.Var.Column = id.Column;
                        ParseVariableType(v.Var);
                    }
                    else
                    {
                        /// 当前作用域的global变量只允许在声明时设置一次类型。类型不允许再次被修改
                        string id_type = ParseVariableType(null, false);
                        if (!string.IsNullOrEmpty(id_type))
                        {
                            error("modify scope global Variable type is not allowed");
                        }

                        v.Var = var;
                        v.Var.References++;
                    }
                }
                else
                {
                    /// local 变量只允许在local声明时设置一次类型。类型不允许再次被修改
                     string id_type = ParseVariableType(null, false);
                    if(!string.IsNullOrEmpty(id_type))
                    {
                        error("modify local Variable type is not allowed");
                    }

                    v.Var = var;
                    v.Var.References++;
                }
                return v;
            }
            else
                error("primary expression expected");

            return null; // satisfy the C# compiler, but this will never happen
        }

        Expression ParseSuffixedExpr(Scope scope, bool onlyDotColon = false)
        {
            // base primary expression
            Expression prim = ParsePrimaryExpr(scope);

            while (true)
            {
                if (reader.IsSymbol('.') || reader.IsSymbol(':'))
                {
                    Token id_symb = reader.Get();
                    string symb = id_symb.Data; // '.' or ':'

                    Token id = null;
                    // TODO: should we allow keywords? I vote no.
                    if (!reader.Is(TokenType.Ident))
                    {
                        error("<Ident> expected");
                    }
                    else
                    {
                        id = reader.Get();
                    }

                    MemberExpr m = new MemberExpr();
                    m.Base = prim;
                    m.Indexer = symb;
                    if (id != null)
                    {
                        m.Ident = id.Data;
                        m.Line = id.Line;
                        m.Column = id.Column;
                    }
                    else
                    {
                        m.Ident = string.Empty;
                        m.Line = id_symb.Line;
                        m.Column = id_symb.Column;
                    }

                    m.IndexerLine = id_symb.Line; //  记录"."或者":"的位置。这个符号标志着成员表达式的开始
                    m.IndexerColumn = id_symb.Column;

                    m.OptionalIdentType = ParseVariableType(null, false);

                    prim = m;
                }
                else if (!onlyDotColon && reader.ConsumeSymbol('['))
                {
                    int pass = 0;
                    const int maxamount = 100;
                    bool wasLastNumeric = false;
                    bool first = true;
                    bool hadComma = false;
                    do
                    {
                        Token tok = reader.Peek();
                        int col = tok.Column;
                        int line = tok.Line;

                        Expression ex = ParseExpr(scope);
                        //if (!reader.ConsumeSymbol(']'))
                        //error("']' expected");

                        IndexExpr i = new IndexExpr();
                        i.Base = prim;
                        i.Index = ex;

                        prim = i;

                        if ((first || wasLastNumeric) && ex is NumberExpr && hadComma == false)
                        {
                            tok = reader.Peek();
                            bool cma = reader.ConsumeSymbol(',');
                            if (cma && hadComma == false && first == false)
                                error("Unexpected ',' in matrice indexing", tok.Line, tok.Column, tok);
                            //else if (cma == false && hadComma)
                            //    ;
                            hadComma = cma;
                        }
                        else
                        {
                            tok = reader.Peek();
                            bool cma = reader.ConsumeSymbol(',');
                            //if (cma == false)
                            //    break;
                            if (cma && hadComma == false)
                                error("Unexpected ',' in matrice indexing", -1, -1, tok);
                            else if (cma == false && ex is NumberExpr == false && wasLastNumeric && hadComma == false)
                            {
                                error("Expected numeric constant in matrice indexing", line, col, tok);
                            }
                            else if (cma == false && hadComma)
                                if (tok.Type == TokenType.Symbol && tok.Data == "]")
                                    ;
                                else
                                    error("Expected ','", -1, -1, tok);
                            else if (cma == false)
                            {
                                break;
                            }
                            else
                            {
                                hadComma = true;
                            }
                            hadComma = cma;
                        }

                        if (pass++ >= maxamount)
                            error("Maximum index depth reached");

                        wasLastNumeric = ex is NumberExpr;
                        first = false;
                    } while (!(reader.Peek().Data == "]"));
                    if (!reader.ConsumeSymbol(']'))
                        error("']' expected");
                }
                else if (!onlyDotColon && reader.IsSymbol('('))
                {
                    Token openBracket = reader.Get();   //  吃掉"("
                    Token closeBracket = null;

                    List<Expression> args = new List<Expression>();
                    while (!reader.IsSymbol(')'))
                    {
                        Expression ex = ParseExpr(scope);

                        args.Add(ex);
                        if (!reader.ConsumeSymbol(','))
                        {
                            if (reader.IsSymbol(')'))
                                break;
                            else
                                error("')' expected");
                            break;
                        }
                    }

                    /// 语法正确的情况下，这里必须吃掉 “）”。考虑可能是不完全的表达式。我们把已经解析的参数都作为函数调用参数的一部分
                    if (reader.IsSymbol(')'))
                    {
                        closeBracket = reader.Get();
                    }
                    else
                    {
                        int reverseIndex = 0;
                        closeBracket = reader.ReversePeek(reverseIndex);

                        /// 反向寻找第一个不为EndOfStream的token. 直到找到一个前括号或者队列结束
                        while (closeBracket != null && closeBracket != openBracket && closeBracket.Type == TokenType.EndOfStream)
                        {
                            reverseIndex++;
                            closeBracket = reader.ReversePeek(reverseIndex);
                        }

                        if (closeBracket == null)
                        {
                            closeBracket = openBracket;
                        }
                    }

                    CallExpr c = new CallExpr();
                    c.Base = prim;
                    c.Arguments = args;

                    c.OpenBracketLine = openBracket.Line;
                    c.OpenBracketColumn = openBracket.Column;
                    if (openBracket == closeBracket)
                    {
                        c.CloseBracketLine = closeBracket.Line;
                        c.CloseBracketColumn = closeBracket.Column;
                    }
                    else
                    {
                        c.CloseBracketLine = closeBracket.Line;
                        c.CloseBracketColumn = closeBracket.Column + closeBracket.Data.Length - 1;
                    }



                    prim = c;
                }
                else if (!onlyDotColon &&
                        (reader.Is(TokenType.SingleQuoteString) ||
                        reader.Is(TokenType.DoubleQuoteString) ||
                        reader.Is(TokenType.LongString)))
                {
                    //string call

                    StringCallExpr e = new StringCallExpr();
                    e.Base = prim;
                    e.Arguments = new List<Expression> { new StringExpr(reader.Peek().Data) { StringType = reader.Peek().Type } };
                    reader.Get();
                    prim = e;
                }
                else if (!onlyDotColon && reader.IsSymbol('{'))
                {
                    // table call

                    // Fix for the issue with whole expr being parsed, not just table.
                    // See LuaMinify issue #2 (https://github.com/stravant/LuaMinify/issues/2)
                    //Expression ex = ParseExpr(scope);
                    Expression ex = ParseSimpleExpr(scope);

                    TableCallExpr t = new TableCallExpr();
                    t.Base = prim;
                    t.Arguments = new List<Expression> { ex };

                    prim = t;
                }
                else
                    break;
            }
            return prim;
        }

        Expression ParseSimpleExpr(Scope scope)
        {
            Token t = reader.Peek();

            if (reader.Is(TokenType.Number))
                return new NumberExpr { Value = reader.Get().Data };
            else if (reader.Is(TokenType.DoubleQuoteString) || reader.Is(TokenType.SingleQuoteString) || reader.Is(TokenType.LongString))
            {
                StringExpr s = new StringExpr
                {
                    Value = reader.Peek().Data,
                    StringType = reader.Peek().Type
                };
                reader.Get();
                return s;
            }
            else if (reader.ConsumeKeyword("nil"))
                return new NilExpr();
            else if (reader.IsKeyword("false") || reader.IsKeyword("true"))
                return new BoolExpr { Value = reader.Get().Data == "true" };
            else if (reader.ConsumeSymbol("..."))
                return new VarargExpr();
            else if (reader.ConsumeSymbol('{'))
            {
                TableConstructorExpr v = new TableConstructorExpr(t.Line, t.Column);
                while (true)
                {
                    if (reader.IsSymbol('['))
                    {
                        // key
                        reader.Get(); // eat '['
                        Expression key = ParseExpr(scope);

                        if (!reader.ConsumeSymbol(']'))
                        {
                            error("']' expected");
                            break;
                        }

                        string keyType = ParseVariableType(null, false);

                        if (!reader.ConsumeSymbol('='))
                        {
                            error("'=' Expected");
                            break;
                        }

                        Expression value = ParseExpr(scope);

                        v.EntryList.Add(new TableConstructorKeyExpr
                        {
                            Key = key,
                            KeyType = keyType,
                            Value = value,
                        });
                    }
                    else if (reader.Is(TokenType.Ident))
                    {
                        // value or key
                        Token lookahead = reader.Peek(1);
                        if (lookahead.Type == TokenType.Keyword && lookahead.Data == TLuaGrammar.T_as)
                        {
                            // we are a key
                            Token key = reader.Get();
                            string keyType = ParseVariableType(null, false);

                            lookahead = reader.Peek();
                            if (lookahead.Type == TokenType.Symbol && lookahead.Data == "=")
                            {
                                if (!reader.ConsumeSymbol('='))
                                    error("'=' Expected");

                                Expression value = ParseExpr(scope);

                                v.EntryList.Add(new TableConstructorStringKeyExpr
                                {
                                    Key = key.Data,
                                    KeyType = keyType,
                                    Value = value,
                                });
                            }
                            else
                            {
                                error("'=' Expected");
                            }
                        }
                        else if (lookahead.Type == TokenType.Symbol && lookahead.Data == "=")
                        {
                            // we are a key
                            Token key = reader.Get();

                            if (!reader.ConsumeSymbol('='))
                                error("'=' Expected");

                            Expression value = ParseExpr(scope);

                            v.EntryList.Add(new TableConstructorStringKeyExpr
                            {
                                Key = key.Data,
                                KeyType = "",
                                Value = value,
                            });
                        }
                        else
                        {
                            // we are a value
                            Expression val = ParseExpr(scope);

                            v.EntryList.Add(new TableConstructorValueExpr
                            {
                                Value = val
                            });

                        }
                    }
                    else if (reader.ConsumeSymbol('}'))
                        break;
                    else
                    {
                        //value
                        Expression value = ParseExpr(scope);
                        v.EntryList.Add(new TableConstructorValueExpr
                        {
                            Value = value
                        });
                    }

                    if (reader.ConsumeSymbol(';') || reader.ConsumeSymbol(','))
                    {
                        // I could have used just an empty statement (';') here, instead of { }
                        // but that leaves a warning, which clutters up the output
                        // other than that, all is good
                    }
                    else if (reader.ConsumeSymbol('}'))
                        break;
                    else
                    {
                        error("'}' or table entry Expected");
                        break;
                    }
                }
                return v;
            }
            else if (reader.ConsumeKeyword("function"))
            {
                AnonymousFunctionExpr func = ParseExprFunctionArgsAndBody(scope);
                //func.IsLocal = true;
                return func;
            }
            else
                return ParseSuffixedExpr(scope);
        }

        bool isUnOp(string o)
        {
            foreach (string s in new string[] { "-", "not", "#"})
                if (s == o)
                    return true;
            return false;
        }

        int unopprio = 8;

        class priority_
        {
            public string op;
            public int l;
            public int r;

            public priority_(string op, int l, int r)
            {
                this.op = op;
                this.l = l;
                this.r = r;
            }
        }

        priority_[] priority = new priority_[] {
 		new priority_("+", 6,6),
 		new priority_("-", 6,6),
 		new priority_("%", 7,7),
 		new priority_("/", 7,7),
 		new priority_("*", 7,7),
 		new priority_("^", 10,9),
 		new priority_("..", 5,4),
 		new priority_("==", 3,3),
 		new priority_("<", 3,3),
 		new priority_("<=", 3,3),
 		new priority_("~=", 3,3),
 		new priority_(">", 3,3),
 		new priority_(">=", 3,3),
 		new priority_("and", 2,2),
 		new priority_("or", 1,1),
 	};

        priority_ getpriority(string d)
        {
            foreach (priority_ p in priority)
                if (p.op == d)
                    return p;
            return null;
        }

        Expression ParseSubExpr(Scope scope, int level)
        {
            // base item, possibly with unop prefix
            Expression exp = null;
            if (isUnOp(reader.Peek().Data) &&
                (reader.Peek().Type == TokenType.Symbol || reader.Peek().Type == TokenType.Keyword))
            {
                string op = reader.Get().Data;
                exp = ParseSubExpr(scope, unopprio);
                exp = new UnOpExpr { Rhs = exp, Op = op };
            }
            else
                exp = ParseSimpleExpr(scope);

            if (exp is InlineFunctionExpression)
                return exp; // inline functions cannot have any extra parts

            // next items in chain
            while (true)
            {
                priority_ prio = getpriority(reader.Peek().Data);
                if (prio != null && prio.l > level)
                {
                    string op = reader.Get().Data;
                    Expression rhs = ParseSubExpr(scope, prio.r);

                    BinOpExpr binOpExpr = new BinOpExpr();
                    binOpExpr.Lhs = exp;
                    binOpExpr.Op = op;
                    binOpExpr.Rhs = rhs;
                    exp = binOpExpr;
                }
                else
                    break;
            }
            return exp;
        }

        Expression ParseExpr(Scope scope)
        {
            return ParseSubExpr(scope, 0);
        }

        Statement ParseStatement(Scope scope)
        {
            int startP = reader.p;
            int startLine = reader.Peek().Line;
            Statement stat = null;
            // print(tok.Peek().Print())
            if (reader.ConsumeKeyword("if"))
            {
                recordPerfectEnd();
                //setup
                IfStmt _if = new IfStmt();

                //clauses
                do
                {
                    int sP = reader.p;
                    Expression nodeCond = ParseExpr(scope);

                    if (!reader.ConsumeKeyword("then"))
                        error("'then' expected");

                    List<Statement> nodeBody = ParseStatementList(scope);

                    List<Token> range = new List<Token>();
                    range.Add(reader.tokens[sP - 1]);
                    range.AddRange(reader.Range(sP, reader.p));

                    _if.Clauses.Add(new ElseIfStmt(scope)
                    {
                        Condition = nodeCond,
                        Body = nodeBody,
                        ScannedTokens = range
                    });
                }
                while (reader.ConsumeKeyword("elseif"));

                // else clause
                if (reader.ConsumeKeyword("else"))
                {
                    int sP = reader.p;
                    List<Statement> nodeBody = ParseStatementList(scope);
                    List<Token> range = new List<Token>();
                    range.Add(reader.tokens[sP - 1]);
                    range.AddRange(reader.Range(sP, reader.p));

                    _if.Clauses.Add(new ElseStmt(scope)
                    {
                        Body = nodeBody,
                        ScannedTokens = range
                    });
                }

                // end
                if (!reader.ConsumeKeyword("end"))
                    error_end("'end' expected");

                stat = _if;
            }
            else if (reader.ConsumeKeyword("while"))
            {
                recordPerfectEnd();
                WhileStatement w = new WhileStatement(scope);

                // condition
                Expression nodeCond = ParseExpr(scope);

                // do
                if (!reader.ConsumeKeyword("do"))
                    error("'do' expected");

                // body
                List<Statement> body = ParseStatementList(scope);

                //end
                if (!reader.ConsumeKeyword("end"))
                    error_end("'end' expected");


                // return
                w.Condition = nodeCond;
                w.Body = body;
                stat = w;
            }
            else if (reader.ConsumeKeyword("do"))
            {
                recordPerfectEnd();
                // do block
                List<Statement> b = ParseStatementList(scope);

                if (!reader.ConsumeKeyword("end"))
                    error_end("'end' expected");

                stat = new DoStatement(scope) { Body = b };
            }
            else if (reader.ConsumeKeyword("for"))
            {
                recordPerfectEnd();
                //for block
                if (!reader.Is(TokenType.Ident))
                    error("<ident> expected");

                Token baseVarName = reader.Get();
                if (reader.ConsumeSymbol('='))
                {
                    //numeric for
                    NumericForStatement forL = new NumericForStatement(scope);
                    Variable forVar = new Variable() { Name = baseVarName.Data, Type = TLuaGrammar.T_number };
                    forL.Scope.AddLocal(forVar);

                    Expression startEx = ParseExpr(scope);

                    if (!reader.ConsumeSymbol(','))
                        error("',' expected");

                    Expression endEx = ParseExpr(scope);

                    Expression stepEx = null;
                    if (reader.ConsumeSymbol(','))
                    {
                        stepEx = ParseExpr(scope);
                    }
                    if (!reader.ConsumeKeyword("do"))
                        error("'do' expected");


                    List<Statement> body = ParseStatementList(forL.Scope);

                    if (!reader.ConsumeKeyword("end"))
                        error_end("'end' expected");


                    forL.Variable = forVar;
                    forL.Start = startEx;
                    forL.End = endEx;
                    forL.Step = stepEx;
                    forL.Body = body;
                    stat = forL;
                }
                else
                {
                    // generic for
                    GenericForStatement forL = new GenericForStatement(scope);
                    Variable generic_for_k = forL.Scope.CreateLocal(baseVarName.Data);
                    ParseVariableType(generic_for_k);
                    List<Variable> varList = new List<Variable> { generic_for_k };
                    while (reader.ConsumeSymbol(','))
                    {
                        if (!reader.Is(TokenType.Ident))
                            error("for variable expected");
                        Token genericID = reader.Get();
                        Variable generic_for_v = forL.Scope.CreateLocal(genericID.Data);
                        generic_for_v.Line = genericID.Line;
                        generic_for_v.Column = genericID.Column;
                        ParseVariableType(generic_for_v);
                        varList.Add(generic_for_v);
                    }
                    if (!reader.ConsumeKeyword("in"))
                        error("'in' expected");

                    List<Expression> generators = new List<Expression>();
                    Expression first = ParseExpr(scope);

                    generators.Add(first);
                    while (reader.ConsumeSymbol(','))
                    {
                        Expression gen = ParseExpr(scope);
                        generators.Add(gen);
                    }
                    if (!reader.ConsumeKeyword("do"))
                        error("'do' expected");

                    List<Statement> body = ParseStatementList(forL.Scope);

                    if (!reader.ConsumeKeyword("end"))
                        error_end("'end' expected");

                    forL.VariableList = varList;
                    forL.Generators = generators;
                    forL.Body = body;
                    stat = forL;
                }
            }
            else if (reader.ConsumeKeyword("repeat"))
            {
                recordPerfectEnd();
                List<Statement> body = ParseStatementList(scope);

                if (!reader.ConsumeKeyword("until"))
                    error("'until' expected");

                Expression cond = ParseExpr(scope);

                RepeatStatement r = new RepeatStatement(scope);
                r.Condition = cond;
                r.Body = body;
                stat = r;
            }
            else if (reader.ConsumeKeyword("function"))
            {
                recordPerfectEnd();
                if (!reader.Is(TokenType.Ident))
                    error("function name expected");

                Expression name = ParseSuffixedExpr(scope, true);
                // true: only dots and colons

                FunctionStatement func = ParseFunctionArgsAndBody(scope);

                func.IsLocal = false;
                func.Name = name;
                stat = func;
            }
            else if (reader.ConsumeKeyword("local"))
            {
                if (reader.Is(TokenType.Ident))
                {
                    List<Token> varList = new List<Token> { reader.Get() };
                    List<string> varTypeList = new List<string>();
                    varTypeList.Add(ParseVariableType());

                    while (reader.ConsumeSymbol(','))
                    {
                        if (!reader.Is(TokenType.Ident))
                            error("local variable name expected");
                        varList.Add(reader.Get());
                        varTypeList.Add(ParseVariableType());
                    }

                    List<Expression> initList = new List<Expression>();
                    if (reader.ConsumeSymbol('='))
                    {
                        do
                        {
                            Expression ex = ParseExpr(scope);
                            initList.Add(ex);
                        } while (reader.ConsumeSymbol(','));
                    }

                    //now patch var list
                    //we can't do this before getting the init list, because the init list does not
                    //have the locals themselves in scope.
                    List<Expression> newVarList = new List<Expression>();
                    for (int i = 0; i < varList.Count; i++)
                    {
                        /// bug fix: 既然定义了local，那是当前scope的词法域的新定义。创建时，不应该修改父scope可能已经有的local定义
                        Variable x = scope.CreateLocal(varList[i].Data,false);
                        x.Type = varTypeList[i];
                        x.Line = varList[i].Line;
                        x.Column = varList[i].Column;
                        newVarList.Add(new VariableExpression { Var = x });
                    }

                    AssignmentStatement l = new AssignmentStatement();
                    l.Lhs = newVarList;
                    l.Rhs = initList;
                    l.IsLocal = true;
                    stat = l;
                }
                else if (reader.ConsumeKeyword("function"))
                {
                    recordPerfectEnd();
                    if (!reader.Is(TokenType.Ident))
                        error("Function name expected");
                    string name = reader.Get().Data;
                    Variable localVar = scope.CreateLocal(name,false);

                    FunctionStatement func = ParseFunctionArgsAndBody(scope);

                    func.Name = new VariableExpression { Var = localVar };
                    func.IsLocal = true;

                    stat = func;
                }
                else
                    error("local variable or function definition expected");
            }
            else if (reader.ConsumeKeyword("return"))
            {
                List<Expression> exprList = new List<Expression>();
                if (!reader.IsKeyword("end") && !reader.IsEof())
                {
                    Expression firstEx = ParseExpr(scope);
                    exprList.Add(firstEx);
                    while (reader.ConsumeSymbol(','))
                    {
                        Expression ex = ParseExpr(scope);
                        exprList.Add(ex);
                    }
                }
                ReturnStatement r = new ReturnStatement();
                r.Arguments = exprList;
                stat = r;
            }
            else if (reader.ConsumeKeyword("break"))
            {
                stat = new BreakStatement();
            }
            else if (reader.ConsumeKeyword("continue"))
            {
                stat = new ContinueStatement();
            }
            else if (reader.ConsumeKeyword(TLuaGrammar.T_using))
            {
                if (reader.Is(TokenType.Ident))
                {
                    List<string> namespaceChain = new List<string>();
                    namespaceChain.Add(reader.Get().Data);
                    while (reader.ConsumeSymbol('.'))
                    {
                        if (!reader.Is(TokenType.Ident))
                        {
                            error("namespace name expected");
                            namespaceChain.Add(string.Empty);
                            break;
                        }
                        else
                        {
                            namespaceChain.Add(reader.Get().Data);
                        }                      
                    }

                    TLuaUsingStatement smt = new TLuaUsingStatement();
                    smt.NameSpaceChain = namespaceChain;
                    stat = smt;
                }
                else
                {
                    error("namespace name expected");
                }
            }
            else
            {
                // statementParseExpr
                Expression suffixed = ParseSuffixedExpr(scope);
                // assignment or call?
                if (suffixed != null && (reader.IsSymbol(',') || reader.IsSymbol('=')) )
                {
                    // check that it was not parenthesized, making it not an lvalue
                    if (suffixed.ParenCount > 0)
                        error("Can not assign to parenthesized expression, it is not an lvalue");

                    // more processing needed
                    List<Expression> lhs = new List<Expression> { suffixed };
                    while (reader.ConsumeSymbol(','))
                    {
                        lhs.Add(ParseSuffixedExpr(scope));
                    }

                    // equals
                    if (!reader.ConsumeSymbol('='))
                        error("'=' expected");

                    //rhs
                    List<Expression> rhs = new List<Expression>();
                    rhs.Add(ParseExpr(scope));
                    while (reader.ConsumeSymbol(','))
                    {
                        rhs.Add(ParseExpr(scope));
                    }

                    AssignmentStatement a = new AssignmentStatement();
                    a.Lhs = lhs;
                    a.Rhs = rhs;
                    stat = a;
                }
                else if (suffixed is CallExpr ||
                       suffixed is TableCallExpr ||
                       suffixed is StringCallExpr)
                {
                    //it's a call statement
                    CallStatement c = new CallStatement();
                    c.Expression = suffixed;
                    stat = c;
                }
                else
                {
                    error("assignment statement expected");

                    /// 在error不抛出异常的情况下，记录未完成的表达式
                    if (UseUnKnownStatement && suffixed!=null)
                    {
                        TLuaUnknownStatement smt = new TLuaUnknownStatement();
                        smt.Expression = suffixed;
                        stat = smt;
                    }
                    else
                    {
                        /// 完全解析不了表达式, 我们简单的吃掉一个token，保证token被消耗。避免死循环
                        TLuaUnknownStatement smt = new TLuaUnknownStatement();
                        reader.Get();
                        stat = smt;
                    }
                }
            }

            stat.ScannedTokens = reader.Range(startP, reader.p);
            if (reader.Peek().Data == ";" && reader.Peek().Type == TokenType.Symbol)
            {
                stat.HasSemicolon = true;
                stat.SemicolonToken = reader.Get();
            }
            if (stat.Scope == null)
                stat.Scope = scope;
            stat.LineNumber = startLine;
            return stat;
        }

        bool isClosing(string s)
        {
            foreach (string w in new string[] { "end", "else", "elseif", "until" })
                if (w == s)
                    return true;
            return false;
        }

        List<Statement> ParseStatementList(Scope scope)
        {
            List<Statement> c = new List<Statement>();

            while (!isClosing(reader.Peek().Data) && !reader.IsEof())
            {
                Statement nodeStatement = ParseStatement(scope);
                //stats[#stats+1] = nodeStatement
                c.Add(nodeStatement);
            }
            return c;
        }

        public Chunk Parse()
        {
            Scope s = new Scope();
            return new Chunk
            {
                Body = ParseStatementList(s),
                Scope = s,
                ScannedTokens = reader.tokens
            };
        }
    }
}
