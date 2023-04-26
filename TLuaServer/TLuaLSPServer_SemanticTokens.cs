using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LspTypes;
using Newtonsoft.Json.Linq;
using SharpLua;
using StreamJsonRpc;

namespace TLuaServer
{
    public partial class TLuaLSPServer : INotifyPropertyChanged, IDisposable
    {
        private SemanticTokensLegend tokenLegend = new SemanticTokensLegend();
        private Dictionary<string, int> LSPTokenTypeIndexMap = new Dictionary<string, int>();
        SharpLua.Lexer2 m_LexerScanner = new SharpLua.Lexer2();

        void init_SemanticTokensLegend()
        {
            List<string> tokenTypes = new List<string>();
            foreach (SemanticTokenTypes item in Enum.GetValues(typeof(SemanticTokenTypes)))
            {
                string value = Util.GetEnumMemberValue<SemanticTokenTypes>(item);
                tokenTypes.Add(value);
            }

            List<string> tokenModifiers = new List<string>();
            foreach (SemanticTokenModifiers item in Enum.GetValues(typeof(SemanticTokenModifiers)))
            {
                string value = Util.GetEnumMemberValue<SemanticTokenModifiers>(item);
                tokenModifiers.Add(value);
            }

            tokenLegend.tokenTypes = tokenTypes.ToArray();
            tokenLegend.tokenModifiers = tokenModifiers.ToArray();
        }

        [JsonRpcMethod(Methods.TextDocumentSemanticTokensFull)]
        public object TextDocumentSemanticTokensFull(JToken arg)
        {
            lock (_object)
            {
                SemanticTokens result = null;

                try
                {
                    var method_params = arg.ToObject<SemanticTokensParams>();
                    string text = BufferManager.instance.GetText(method_params.TextDocument.Uri);
                    TokenReader reader = m_LexerScanner.Lex(text);

                    //LogInfo("TextDocumentSemanticTokensFull");
                }
                catch (SystemException e)
                {
                    LogException(e);
                }

                return result;
            }
        }
    }
}
