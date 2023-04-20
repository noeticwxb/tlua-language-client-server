using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLua
{
    /// <summary>
    /// 原始拷贝至Lexer.cs。 为了配合VS插件的语法高亮功能，把多行文本，多行注释解析成单个的Token
    /// </summary>
    public class Lexer2
    {
        string src = "";

        int p = 0;
        int ln = 1;
        int col = 1;

        char peek(int n = 0)
        {
            if (src.Length < p + n + 1)
                return '\0';
            else
                return src.Substring(p + n, 1)[0];
        }

        char read()
        {
            if (src.Length < p + 1)
                return '\0';
            else
            {
                char c = src.Substring(p, 1)[0];
                if (c == '\n')
                {
                    ln++;
                    col = 0; // incremented 2 lines down
                }
                col++;
                p++;
                return c;
            }
        }

        static bool IsSymbol(char c)
        {
            foreach (char c2 in (new char[] { '+', '-', '*', '/', '^', '%', ',', 
                '{', '}', '[', ']', '(', ')', ';', '#',
            '|', '&', }))
                if (c == c2)
                    return true;
            return false;
        }

        static bool IsHexDigit(char c)
        {
            return char.IsDigit(c) ||
                c == 'A' ||
                c == 'a' ||
                c == 'B' ||
                c == 'b' ||
                c == 'C' ||
                c == 'c' ||
                c == 'D' ||
                c == 'd' ||
                c == 'E' ||
                c == 'e' ||
                c == 'F' ||
                c == 'f';
        }

        static bool IsKeyword(string word)
        {
            return
                word == "and" ||
                word == "break" ||
                word == "do" ||
                word == "else" ||
                word == "elseif" ||
                word == "end" ||
                word == "false" ||
                word == "for" ||
                word == "function" ||
#if !VANILLA_LUA
 word == "goto" ||
#endif
 word == "if" ||
                word == "in" ||
                word == "local" ||
                word == "nil" ||
                word == "not" ||
                word == "or" ||
                word == "repeat" ||
                word == "return" ||
                word == "then" ||
                word == "true" ||
                word == "until" ||
                word == "while" ||
                word == TLuaGrammar.T_as || // as 标记类型选择
                word == TLuaGrammar.T_using || // 导入库的命名空间符号/或者某个Lua表的符号 
                word == TLuaGrammar.T_number || // 数字类型关键字
                word == TLuaGrammar.T_string || // 字符串类型关键字
                word == TLuaGrammar.T_typeclass || //类定义
                word == TLuaGrammar.T_Alias  ||  // 别名
                word == TLuaGrammar.T_bool || // bool 类型关键字
                word == TLuaGrammar.T_void || // void 标示函数没有返回值
                
                word == TLuaGrammar.T_vary   // 在运行时才能确定的
#if !VANILLA_LUA
 || word == "using"
                || word == "continue";
#else
;
#endif
        }

        string readnl()
        {
            if (peek() == '\r')
            {
                read();
                read();
                return "\r\n";
            }
            else
            {
                read();
                return "\n";
            }
        }

        bool matchpeek(string chars)
        {
            char c = peek();
            foreach (char c2 in chars)
                if (c == c2)
                    return true;
            return false;
        }

        public TokenReader Lex(string s)
        {
            List<Token> tokens = new List<Token>();
            src = s;
            p = 0;
            ln = 1; //  行
            col = 1;    //  列

            while (true)
            {
                List<Token> leading = new List<Token>();
                // eat whitespace
                while (true)
                {
                    char c_ = peek();
                    if (c_ == '#' && peek(1) == '!' && ln == 1)
                    {
                        // linux shebang
                        string sh = "";
                        while (peek() != '\n' && peek() != '\r' && peek() != '\0')
                        {
                            sh += read();
                        }
                        //readnl();
                        leading.Add(new Token { Type = TokenType.Shebang, Data = sh });
                    }
                    else if (c_ == ' ' || c_ == '\t')
                    {
                        Token whitepace = new Token { Type = c_ == ' ' ? TokenType.WhitespaceSpace : TokenType.WhitespaceTab, Data = c_.ToString() };
                        whitepace.Line = ln;
                        whitepace.Column = col;
                        leading.Add(whitepace);
                        if(whitepace.Type == TokenType.WhitespaceSpace)
                        {
                            tokens.Add(whitepace);
                        }
                        read();
                    }
                    else if (c_ == '\n' || c_ == '\r')
                    {
                        leading.Add(new Token { Type = c_ == '\n' ? TokenType.WhitespaceN : TokenType.WhitespaceR, Data = c_.ToString() });
                        // read handles line changing...
                        read();
                    }
                    else
                        break;
                }

                //tokens.AddRange(leading);

                Token t = new Token();
                t.Leading = leading;
                t.Line = ln;
                t.Column = col;

                char c = read();

                if (c == '\0')
                    t.Type = TokenType.EndOfStream;
                else if (char.IsLetter(c) || c == '_')
                {
                    // ident / keyword
                    string s4 = c.ToString();
                    while (char.IsLetter(peek()) ||
                        peek() == '_' ||
                        char.IsDigit(peek()) &&
                        peek() != '\0')
                    {
                        s4 += read();
                    }
                    t.Data = s4;
                    if (IsKeyword(s4))
                        t.Type = TokenType.Keyword;
                    else
                        t.Type = TokenType.Ident;
                }
                else if (char.IsDigit(c) ||
                    (c == '.' && char.IsDigit(peek())))
                { // negative numbers are handled in unary minus collection
                    string num = "";
                    if (c == '0' && matchpeek("xX"))
                    {
                        //read(); -> already done
                        num = "0" + read(); // 'xX'
                        while (IsHexDigit(peek()))
                            num += read();
                    }
                    else
                    {
                        num = c.ToString();
                        bool dec = false;
                        while (char.IsDigit(peek()) || peek() == '.')
                        {
                            num += read();
                            if (peek() == '.')
                            {
                                //if (dec)
                                //    error("Number has more than one decimal point");
                                dec = true;
                                num += read();
                            }
                        }
                    }

                    t.Data = num;
                    t.Type = TokenType.Number;
                }
                else if (c == '\'' || c == '"')
                {
                    // 单行字符串
                    char delim = c;
                    string str = delim.ToString();
                    while (true)
                    {
                        char c2 = read();
                        if (c2 == '\\')
                        {
                            str += "\\";
                            str += read(); // we won't parse \0xFF, \000, \n, etc here
                        }
                        else if (c2 == delim)
                        {
                            str += delim.ToString();
                            break;
                        }
                        else if (c2 == '\0')
                        {
                            // VS 语法高亮是每敲入一个字母，就会要求进行一次解析。 因此即使没有结束的'"'或者'\''，暂时不认为错误，等待用户输入结束标记
                            //error("expected '" + delim + "', not <eof>");
                            break;
                        }
                        else
                            str += c2;
                    }
                    t.Data = str;
                    t.Type = delim == '"' ? TokenType.DoubleQuoteString : TokenType.SingleQuoteString;
                }
                else if ( c=='-' && peek() == '-' && peek(1) == '[' && peek(2) == '[' )
                {
                    //// 多行注释的开始标志
                    read(); //  '-'
                    read(); //  '['
                    read(); //  '['
                    t.Type = TokenType.Symbol;
                    t.Data = "--[[";
                }
                else if (c == '-' && peek() == '-')
                {
                    // 单行注释， 这个判断应在多行注释if判断之后                
                    read(); //  '-'
                    string comment = "--";

                    // 读取所有的注释
                    while (peek() != '\n' && peek() != '\r' && peek() != '\0')
                        comment += read();

                    t.Type = TokenType.ShortComment;
                    t.Data = comment;
                }
                else if ( c=='['  && peek() == '[')
                {
                    /// 多行字符串的开始标志。 这个判断在多行注释if判断之后
                    /// 不支持 [==[   ]==] 这样的多行字符串
                    read(); // '['
                    t.Type = TokenType.Symbol;
                    t.Data = "[[";
                }
                else if (c==']' && peek() == ']')
                {
                    /// 多行字符串或者多行注释的结尾标记。 
                    /// 不支持]===]这样的结尾标记
                    read(); // ']'
                    t.Type = TokenType.Symbol;
                    t.Data = "]]";
                }
#if false
                else if (c == '[')
                {
                    string s3 = tryReadLongStr();
                    if (s3 == null)
                    {
                        t.Type = TokenType.Symbol;
                        t.Data = "[";
                    }
                    else
                    {
                        t.Type = TokenType.LongString;
                        t.Data = s3;
                    }
                }
#endif
                else if (c == '<' || c == '>' || c == '=')
                {
                    t.Type = TokenType.Symbol;
                    if (peek() == '=' ||
                        (c == '<' && peek() == '<') ||
                        (c == '>' && peek() == '>'))
                    {
                        t.Data = c.ToString() + read().ToString();
#if !VANILLA_LUA
                        if (peek() == '=' && (c == '<' || c == '>'))
                            t.Data += read(); // augmented, e.g. >>=, but not ===
#endif
                    }
                    else
                        t.Data = c.ToString();
                }
                else if (c == '~')
                {
                    if (peek() == '=')
                    {
                        read();
                        t.Type = TokenType.Symbol;
                        t.Data = "~=";
                    }
                    else
                    {
                        t.Type = TokenType.Symbol;
                        t.Data = "~";
                    }

                }
                else if (c == '.')
                {
                    t.Type = TokenType.Symbol;
                    if (peek() == '.')
                    {
                        read(); // read second '.
                        if (peek() == '.')
                        {
                            t.Data = "...";
                            read(); // read third '.'
                        }
#if !VANILLA_LUA
                        else if (peek() == '=') // ..=
                        {
                            t.Data = "..=";
                            read(); // read '='
                        }
#endif
                        else
                            t.Data = "..";
                    }
                    else
                    {
                        t.Data = ".";
                    }
                }
                else if (c == ':')
                {
                    t.Type = TokenType.Symbol;
#if !VANILLA_LUA
                    if (peek() == ':')
                    {
                        read();
                        t.Data = "::";
                    }
                    else
#endif
                        t.Data = ":";
                }
                else if (c == '-' && peek() == '>')
                {
                    read(); // read the '>'
                    t.Data = "->";
                    t.Type = TokenType.Symbol;
                }
                else if (c == '^')
                {
                    t.Type = TokenType.Symbol;

                    if (peek() == '^')
                    {
                        read();
#if !VANILLA_LUA
                        if (peek() == '=')
                        {
                            read();
                            t.Data = "^^=";
                        }
                        else
#endif
                            t.Data = "^^";
                    }
#if !VANILLA_LUA
                    else if (peek() == '=')
                    {
                        read();
                        t.Data = "^=";
                    }
#endif
                    else
                        t.Data = "^";
                }
#if !VANILLA_LUA
                else if (c == '!')
                {
                    t.Type = TokenType.Symbol;
                    if (peek() == '=')
                    {
                        read();
                        t.Data = "!=";
                    }
                    else t.Data = "!";
                }
#endif
                else if (IsSymbol(c))
                {
                    t.Type = TokenType.Symbol;
                    t.Data = c.ToString();
#if !VANILLA_LUA
                    if (peek() == '=')
                    {
                        char c2 = peek();
                        if (c == '+' ||
                            c == '-' ||
                            c == '/' ||
                            c == '*' ||
                            c == '^' ||
                            c == '%' ||
                            c == '&' ||
                            c == '|')
                        {
                            t.Data += "=";
                            read();
                        }
                    }
#endif
                }
                else
                {
                    //p--; // un-read token
                    //col--;
                    //error("Unexpected Symbol '" + c + "'");
                    //read();
                    t.Type = TokenType.UNKNOWN;
                    t.Data = c.ToString();
                }

                tokens.Add(t);

                if (peek() == '\0')
                    break;
            }
            if (tokens.Count > 0 && tokens[tokens.Count - 1].Type != TokenType.EndOfStream)
                tokens.Add(new Token { Type = TokenType.EndOfStream });
            if (tokens.Count > 1) // 2+
                tokens[tokens.Count - 2].FollowingEoSToken = tokens[tokens.Count - 1];
            return new TokenReader(tokens);
        }
    }
}
