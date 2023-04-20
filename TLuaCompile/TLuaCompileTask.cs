using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace TLua.TLuaCompile
{
    public class TLuaCompileTask: Microsoft.Build.Utilities.Task
    {
        private ITaskItem[] m_Items;

        [Required]
        // Directories to create. 
        public ITaskItem[] Items
        {
            get
            {
                return m_Items;
            }

            set
            {
                m_Items = value;
            }
        }

        private string m_BuildOutputPath;

        [Required]
        public string BuildOutputPath
        {
            get{return m_BuildOutputPath;}
            set{m_BuildOutputPath = value;}
        }

        //public void Log

        public void LogMessage(string msg,
            string file = null,
            int lineNumber = 0, int columNumber = 0,
            int endLineNumber = 0, int endColumNumber = 0,
            string subcategory = null, string code = null, string helpKeyword = null)
        {
            TLua.Log.WriteLine(msg);
            //Log.LogCriticalMessage(subcategory, code, helpKeyword, file, lineNumber, columNumber, endLineNumber, endColumNumber, msg);
            Log.LogMessage(msg);
        }

        public void LogWarnning(string msg, 
            string file = null,
            int lineNumber = 0, int columNumber = 0, 
            int endLineNumber = 0, int endColumNumber = 0,
            string subcategory = null, string code = null, string helpKeyword = null )
        {
            TLua.Log.WriteLine("Warning: " + msg);
            Log.LogWarning(subcategory, code, helpKeyword, file, lineNumber, columNumber, endLineNumber, endColumNumber, msg);

        }

        public void LogError(string msg, 
            string file = null,
            int lineNumber = 0, int columNumber = 0, 
            int endLineNumber = 0, int endColumNumber = 0,
            string subcategory = null, string code = null, string helpKeyword = null )
        {
            TLua.Log.WriteLine("Error: " + msg);
            Log.LogError(subcategory, code, helpKeyword, file, lineNumber, columNumber, endLineNumber, endColumNumber, msg);
        }


        //public string SourceDir { get; set; }

        public string DestDir { get; set; }


        public override bool Execute()
        {
            /// 对于.tlua 结尾的文件，编译成lua文件
            /// 对于工程中的其他文件，直接拷贝到目的文件夹

            if (string.IsNullOrEmpty(BuildOutputPath))
            {
                DestDir = "Bin\\Output";
            }
            else 
            {
                char[] crim = { '\\', '/' };
                DestDir = BuildOutputPath.TrimEnd(crim);
            }
            

            foreach (ITaskItem node in Items)
            {
                string source_FullPath = node.GetMetadata("FullPath");      // Example: "C:\MyProject\Source\Program.cs"
                string source_Extension = node.GetMetadata("Extension");    // Example:   ".cs"
                string source_RelativePath = node.GetMetadata("RelativeDir");   //  Example: "Source\"
                string source_FileName = node.GetMetadata("Filename");          // Example: "Program"

                if (source_Extension == ".tlua")
                {
                    string dest_FullPath = DestDir + "\\" + source_RelativePath + source_FileName + ".lua";
                    CompileToLua(source_FullPath, dest_FullPath);
                }
                else
                {
                    string dest_FullPath = DestDir + "\\" + source_RelativePath + source_FileName + source_Extension;
                    CopyToDest(source_FullPath, dest_FullPath);
                }

            }


            return true;
 
        }

        class ToLuaCompile
        {
            SharpLua.TokenReader Reader;
            System.Text.StringBuilder Builder;


            bool isAsorUsing(SharpLua.Token t)
            {
                if (t.Type == SharpLua.TokenType.Keyword &&
                    ( t.Data == SharpLua.TLuaGrammar.T_as || t.Data == SharpLua.TLuaGrammar.T_using) )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override string ToString()
            {
                while (!Reader.IsEof())
                {
                    var t = Reader.Get();
                    if (isAsorUsing(t))
                    {
                        WriteTokenOnlyLeading(t);

                        // 丢弃as或者using紧跟的Token，以及用.连接的token
                        WriteTLuaTypeToken();
                    }
                    else
                    {
                        WriteToken(t);
                    }
                }

                return Builder.ToString();
            }


            int temlatepDepth = 0;

            void WriteTLuaTypeToken()
            {
                var t = Reader.Get();   // type
                WriteTokenOnlyLeading(t);

                var dotToken = Reader.Peek();
                if (dotToken.Type == SharpLua.TokenType.Symbol && dotToken.Data == ".")
                {
                    dotToken = Reader.Get();   //  read dotToken
                    WriteTokenOnlyLeading(dotToken);
                    WriteTLuaTypeToken();   //  read next type
                }

                // begin template
                 if(dotToken.Type == SharpLua.TokenType.Symbol && dotToken.Data == "<" )
                 {
                     temlatepDepth++;
                     dotToken = Reader.Get();   //  read dotToken
                     WriteTokenOnlyLeading(dotToken);
                     WriteTLuaTypeToken();   //  read next type
                 }

                var commaToken = Reader.Peek();
                var asToken = Reader.Peek(1);
                if (commaToken.Type == SharpLua.TokenType.Symbol && commaToken.Data == ","
                    && isAsorUsing(asToken)
                    )
                {
                    commaToken = Reader.Get();   //  "," 
                    WriteTokenOnlyLeading(commaToken);

                    asToken = Reader.Get();   //  "as"
                    WriteTokenOnlyLeading(asToken);

                    WriteTLuaTypeToken();   //  read next type
                }
                else if (commaToken.Type == SharpLua.TokenType.Symbol && commaToken.Data == ","
                    && temlatepDepth > 0)
                {
                    commaToken = Reader.Get();   //  "," 
                    WriteTokenOnlyLeading(commaToken);

                    WriteTLuaTypeToken();   //  read next type
                }



                if (commaToken.Type == SharpLua.TokenType.Symbol && commaToken.Data == ">")
                {
                    if( asToken.Type == SharpLua.TokenType.Symbol && commaToken.Data == "," ){
                        temlatepDepth--;
                        commaToken = Reader.Get();   //  ">" 
                        WriteTokenOnlyLeading(commaToken);

                        asToken = Reader.Get();   //  ","
                        WriteTokenOnlyLeading(asToken);

                        WriteTLuaTypeToken();   //  read next template type
                    }
                    else
                    {
                        commaToken = Reader.Get();   //  ">" 
                        WriteTokenOnlyLeading(commaToken);
                        temlatepDepth--;

                        while (!Reader.IsEof())
                        {
                            var bigToken = Reader.Peek();
                            if (bigToken.Type == SharpLua.TokenType.Symbol && bigToken.Data == ">")
                            {
                                temlatepDepth--;
                                bigToken = Reader.Get();   //  ">" 
                                WriteTokenOnlyLeading(bigToken);
                            }
                            else
                            {
                                break;
                            }                      
                        }
                    }
                }
           
            }

            void WriteToken(SharpLua.Token t)
            {
                WriteTokenOnlyLeading(t);
                WriteText(t);
            }

            void WriteTokenOnlyLeading(SharpLua.Token t)
            {
                var itor = t.Leading.GetEnumerator();
                while (itor.MoveNext())
                {
                    WriteText(itor.Current);
                }
            }

            void WriteText(SharpLua.Token t)
            {
                if (t.Type == SharpLua.TokenType.DoubleQuoteString)
                {
                    Builder.Append("\"");
                    Builder.Append(t.Data);
                    Builder.Append("\"");
                }
                else if (t.Type == SharpLua.TokenType.SingleQuoteString)
                {
                    Builder.Append("\'");
                    Builder.Append(t.Data);
                    Builder.Append("\'");
                }
                else
                {
                    Builder.Append(t.Data);
                }
            }

            public ToLuaCompile(SharpLua.TokenReader tr)
            {
                Reader = new SharpLua.TokenReader(tr.tokens);
                Builder = new StringBuilder();
            }
        }
        

        public static void Compile(string source_FullPath, string dest_FullPath)
        {
            var encoding_t = EncodingType.GetType(source_FullPath);
            string source_code = System.IO.File.ReadAllText(source_FullPath, encoding_t);
            SharpLua.Lexer l = new SharpLua.Lexer();
            SharpLua.TokenReader reader = l.Lex(source_code);

            SharpLua.TokenReader readToWrite = new SharpLua.TokenReader(reader.tokens);

            SharpLua.Parser parser = new SharpLua.Parser(reader);
            SharpLua.Ast.Chunk c = parser.Parse();

            SharpLua.Visitors.BasicBeautifier compiler = new SharpLua.Visitors.BasicBeautifier();
            string dest_code = compiler.Beautify(c);

            //SharpLua.Visitors.LuaCompatibleOutput compiler = new SharpLua.Visitors.LuaCompatibleOutput();
            //string dest_code = compiler.Format(c);

            string newPath = System.IO.Path.GetDirectoryName(dest_FullPath);
            if (!System.IO.Directory.Exists(newPath))
            {
                System.IO.Directory.CreateDirectory(newPath);
            }

            string destCode2 = (new ToLuaCompile(readToWrite)).ToString();

            System.Text.UTF8Encoding utf8_t = new UTF8Encoding(false);

            System.IO.File.WriteAllText(dest_FullPath, destCode2, utf8_t);      
        }

        public static void Copy(string source_FullPath, string dest_FullPath)
        {
            if (source_FullPath == dest_FullPath)
            {
                return;
            }

            string newPath = System.IO.Path.GetDirectoryName(dest_FullPath);
            if (!System.IO.Directory.Exists(newPath))
            {
                System.IO.Directory.CreateDirectory(newPath);
            }

            System.IO.File.Copy(source_FullPath, dest_FullPath, true);
        }


        public void CompileToLua(string source_FullPath, string dest_FullPath)
        {
            /// 已经在SharpLua中关闭了原作者添加的扩展语法。对于我们来说，编译过程就是去除as这些，输出
            try
            {
                TLuaCompileTask.Compile(source_FullPath, dest_FullPath);

                LogMessage(string.Format("Sucess Compile() {0} to {1} ", source_FullPath, dest_FullPath));
            }
            catch (SharpLua.LuaSourceException le)
            {
                if (le.IsEnd)
                {
                    LogError(le.Message, source_FullPath, le.ErrorToken.Line, le.ErrorToken.Column,
                        le.ErrorToken.Line, le.ErrorToken.Column);
                }
                else if(le.ReverseToken!=null && le.ErrorToken!=null && le.ReverseToken.Type != SharpLua.TokenType.EndOfStream
                    && le.ErrorToken.Type != SharpLua.TokenType.EndOfStream
                    )
                {
                    LogError(le.Message, source_FullPath, le.ReverseToken.Line, le.ReverseToken.Column,
                        le.ErrorToken.Line,le.ErrorToken.Column);
                }
                else if( le.ReverseToken!=null && le.ReverseToken.Type != SharpLua.TokenType.EndOfStream)
                {
                    LogError(le.Message, source_FullPath, le.ReverseToken.Line, le.ReverseToken.Column);
                }
                else
                {
                    LogError(le.Message, source_FullPath);
                }                       
            }
            catch (System.Exception e)
            {
                LogError(e.Message, source_FullPath);
            }         
        }

        public void CopyToDest(string source_FullPath, string dest_FullPath)
        {
            if (source_FullPath == dest_FullPath)
            {
                LogWarnning("Compile(Copy) Repeat Path:" + source_FullPath, source_FullPath);
                return;
            }

            try
            {
                TLuaCompileTask.Copy(source_FullPath, dest_FullPath);

                LogMessage(string.Format("Sucess Compile(Copy) {0} to {1} ", source_FullPath, dest_FullPath));
            }
            catch (System.Exception e)
            {
                LogError("Compile(Copy) Error: " + e.ToString(), dest_FullPath);
            }
        }



    }
}
