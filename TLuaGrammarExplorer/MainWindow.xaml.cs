using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using SharpLua;
using TLua.Analysis;

using System.Reflection;

namespace TLuaGrammarExplorer
{

    class TokenItem
    {
        public string Type { get; set; }
        public string Data { get; set; }

        public TokenItem(Token t)
        {
            Type = t.Type.ToString();
            Data = t.Data;
        }
    }

    public class AstTreeViewItem: System.Windows.Controls.TreeViewItem
    {
        
        public SharpLua.Ast.AstNode node;
    }

    public class DeclTreeViewItem : System.Windows.Controls.TreeViewItem
    {
        public Declaration node;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public delegate TokenReader LexDelegate(string code);

        List<TokenItem> CreateTakenList(LexDelegate callback, string code)
        {
            List<TokenItem> tokenItemList = new List<TokenItem>();
            try
            {
                TokenReader reader = callback(code);
                
                while (!reader.IsEof())
                {
                    Token t = reader.Get();
                    tokenItemList.Add(new TokenItem(t));
                }
                tbParseError.Text = "Lexer Color OK";

                return tokenItemList;
            }
            catch(LuaSourceException e)
            {
                tbParseError.Text = e.GenerateMessage("Test Code");
            }
            catch (Exception e)
            {
                tbParseError.Text = e.ToString();
            }

            return tokenItemList;
        }

        enum TestEnum
        {
            Enum1 = 1,
            Enum2 = 2,
            Enum3 = 3
        }

        class TestClass
        {
            public string Name { get; set; }
        }

        private void BtnParser_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ChunkDeclaration declRoot = new ChunkDeclaration();
            declRoot.Name = "U3D.decl";
           
            DeclarationGen gen = new DeclarationGen();
            gen.RootChunk = declRoot;
            //gen.Generate(typeof(TestEnum));
            //gen.Generate(typeof(List<int>),"ListInt");
            gen.Generate(typeof(TestClass));
            //gen.Generate(typeof(CreateAstTreeVisitor));
            //gen.Generate(typeof(DeclarationGen));
            using(System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create("C:\\test.decl", new System.Xml.XmlWriterSettings { Indent = true }))
            //using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create("C:\\test.decl"))
            {
                DeclarationSerialize serialize = new DeclarationSerialize();
                serialize.Write(writer, gen.RootChunk, true);
                writer.Flush();
                writer.Close();
            }

            
#if false

            tbParseError.Clear();
            lvTokenList.ItemsSource = null;
            tvParseTree.Items.Clear();
            tvDeclarationTree.Items.Clear();

            string code = tbLuaCode.Text;
            if (string.IsNullOrEmpty(code))
                return;

            bool? bIsColorToken = cbPaserColorToken.IsChecked;
            if (bIsColorToken!=null && (bool)bIsColorToken == true)
            {
                Lexer2 lex2 = new Lexer2();
                lvTokenList.ItemsSource = CreateTakenList(lex2.Lex, code);
                return;
            }


            Lexer l = new Lexer();
            lvTokenList.ItemsSource = CreateTakenList(l.Lex, code); ;

            try
            {
                Parser p = new Parser(l.Lex(code));
                p.ThrowParsingErrors = (bool)cbThrowError.IsChecked;

                if( !p.ThrowParsingErrors )
                {
                    p.UseUnKnownStatement = true;
                }

                SharpLua.Ast.Chunk c = p.Parse();


                TLua.Analysis.AnalyLuaClassVisitor nv = new TLua.Analysis.AnalyLuaClassVisitor();
                nv.Analy(c, "C:\\test.tlua");
                if (nv.RootChunk != null && false)
                {
                    DeclarationSerialize serialize = new DeclarationSerialize();

                    System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create("C:\\test.decl", new System.Xml.XmlWriterSettings { Indent = true});
                    serialize.Write(writer, nv.RootChunk, true);
                    writer.Flush();
                    writer.Close();

                    System.Xml.XmlReader reader = System.Xml.XmlReader.Create("C:\\test.decl");
                    Declaration declNew = serialize.Read(reader);
                    writer = System.Xml.XmlWriter.Create("C:\\test2.decl", new System.Xml.XmlWriterSettings { Indent = true });
                    serialize.Write(writer, declNew, true);
                    writer.Flush();
                    writer.Close();

                }

                CreateAstTreeVisitor ctv = new CreateAstTreeVisitor();
                c.Accept(ctv);

                tvParseTree.Items.Add(ctv.Root);


                AnalyLuaClassVisitor alcv = new AnalyLuaClassVisitor();
                alcv.Analy(c, "test.tlua");

                CreateDeclarationTreeVisitor cdtv = new CreateDeclarationTreeVisitor();
                alcv.RootChunk.Accept(cdtv);

                tvDeclarationTree.Items.Add(cdtv.Root);

                tbParseError.Text = "Parse OK";

            }
            catch(LuaSourceException parseError)
            {
                tbParseError.Text = parseError.GenerateMessage("Test Code");
            }
            catch(Exception errorMsg)
            {
                tbParseError.Text = errorMsg.ToString();
            }
#endif
        }

        private void tvParseTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            lvStatementDetail.Items.Clear();


            AstTreeViewItem treeItem = this.tvParseTree.SelectedItem as AstTreeViewItem;
            if (treeItem != null)
            {
                if( treeItem.node is SharpLua.Ast.Variable)
                {
                    SharpLua.Ast.Variable v = treeItem.node as SharpLua.Ast.Variable;
                    lvStatementDetail.Items.Add( "Type: " + v.Type);
                    lvStatementDetail.Items.Add("Name:" + v.Name);
                    lvStatementDetail.Items.Add("IsGlobal: " + v.IsGlobal);
                    lvStatementDetail.Items.Add("References: " + v.References);
                }
                else if (treeItem.node is SharpLua.Ast.Statement.Statement)
                {
                    if(treeItem.node is SharpLua.Ast.Statement.FunctionStatement)
                    {
                        SharpLua.Ast.Statement.FunctionStatement funcSmt = treeItem.node as SharpLua.Ast.Statement.FunctionStatement;
                        foreach (string rt in funcSmt.ReturnTypeList)
                        {
                            lvStatementDetail.Items.Add("Return: " + rt);
                        }
                    }

                    SharpLua.Ast.Statement.Statement smt = treeItem.node as SharpLua.Ast.Statement.Statement;
                    foreach (Token x in smt.ScannedTokens)
                    {
                        lvStatementDetail.Items.Add(x.Print());
                    }

                }
                else if(treeItem.node is SharpLua.Ast.Expression.Expression)
                {
                    if (treeItem.node is SharpLua.Ast.Expression.AnonymousFunctionExpr)
                    {
                        SharpLua.Ast.Expression.AnonymousFunctionExpr funcExpr = treeItem.node as SharpLua.Ast.Expression.AnonymousFunctionExpr;
                        foreach (string rt in funcExpr.ReturnTypeList)
                        {
                            lvStatementDetail.Items.Add("Return: " + rt);
                        }
                    }
                    else if(treeItem.node is SharpLua.Ast.Expression.MemberExpr)
                    {
                        var memberExpr = treeItem.node as SharpLua.Ast.Expression.MemberExpr;
                        lvStatementDetail.Items.Add("Ident: " + memberExpr.Ident);
                        lvStatementDetail.Items.Add(memberExpr.Indexer);
                    }
                }
            }
            
        }

        private void  tvDeclarationTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            lvStatementDetail.Items.Clear();

            DeclTreeViewItem treeItem = this.tvDeclarationTree.SelectedItem as DeclTreeViewItem;
            if(treeItem!=null)
            {
                Declaration decl = treeItem.node;
                if( decl !=null )
                {
                    lvStatementDetail.Items.Add(decl.Description);
                }
                
            }

        }
    }
}
