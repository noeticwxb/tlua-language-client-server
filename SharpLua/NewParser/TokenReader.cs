using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua
{
    public class TokenReader
    {
        Stack<int> savedP = new Stack<int>();
        public int p = 0;
        public List<Token> tokens;

        public TokenReader(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        //getters
        public Token Peek(int n = 0)
        {
            return tokens[Math.Min(tokens.Count - 1, p + n)];
        }

        public Token Get()
        {
            Token t = tokens[p];
            p = Math.Min(p + 1, tokens.Count);
            return t;
        }

        /// 调用Get之后，p已经增加了，所以默认值n=1，返回的是get返回的token之前的那个token
        public Token ReversePeek(int n = 1)
        {
            if( (p - 1 - n) < 0 )
            {
                return null;
            }

            return tokens[p - 1 - n];
        }

        public bool Is(TokenType t)
        {
            return Peek().Type == t;
        }

        //save / restore points in the stream
        public void Save()
        {
            savedP.Push(p);
        }

        public void Commit()
        {
            savedP.Pop();
        }

        public void Restore()
        {
            p = savedP.Pop();
        }

        //either return a symbol if there is one, or return true if the requested
        //symbol was gotten.
        public bool ConsumeSymbol(string symb)
        {
            Token t = Peek();
            if (t.Type == TokenType.Symbol)
            {
                if (t.Data == symb)
                {
                    Get();
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public bool ConsumeSymbol(char sym)
        {
            Token t = Peek();
            if (t.Type == TokenType.Symbol)
            {
                if (t.Data == sym.ToString())
                {
                    Get();
                    return true;
                }
                else
                    return false;
            }
            else
                return false;

        }

        public Token ConsumeSymbol()
        {
            Token t = Peek();
            if (t.Type == TokenType.Symbol)
            {
                Get();
                return t;
            }
            else
                return null;
        }

        public bool ConsumeKeyword(string kw)
        {
            Token t = Peek();
            if (t.Type == TokenType.Keyword && t.Data == kw)
            {
                Get();
                return true;
            }
            else
                return false;
        }

        /// 可以在赋值表达的左部作为as声明的内置类型关键字
        public bool IsVarTypeKeyWord()
        {
            if(IsKeyword(TLuaGrammar.T_number) 
                || IsKeyword(TLuaGrammar.T_string)
                || IsKeyword(TLuaGrammar.T_vary)
                || IsKeyword(TLuaGrammar.T_bool)
                || IsKeyword(TLuaGrammar.T_typeclass)
                || IsKeyword(TLuaGrammar.T_Alias)
                )
            {
                return true;
            }

            return false;
        }


        /// 可以作为函数返回值多次出现的内置类型关键字
        public bool IsReturnTypeKeyWord()
        {
            if (IsKeyword(TLuaGrammar.T_number)
                || IsKeyword(TLuaGrammar.T_string)
                || IsKeyword(TLuaGrammar.T_vary)
                || IsKeyword(TLuaGrammar.T_bool)
                )
            {
                return true;
            }

            return false;
        }


        public bool IsKeyword(string kw)
        {
            Token t = Peek();
            return t.Type == TokenType.Keyword && t.Data == kw;
        }

        public bool IsSymbol(string s)
        {
            Token t = Peek();
            return t.Type == TokenType.Symbol && t.Data == s;
        }

        public bool IsSymbol(char c)
        {
            Token t = Peek();
            return t.Type == TokenType.Symbol && t.Data == c.ToString();
        }

        public bool IsEof()
        {
            return Peek().Type == TokenType.EndOfStream;
        }

        public List<Token> Range(int start, int end)
        {
            List<Token> t = new List<Token>();
            for (int i = start; i < end; i++)
                t.Add(tokens[i]);
            return t;
        }
    }
}
