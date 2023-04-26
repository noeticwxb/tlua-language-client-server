using LspTypes;
using Newtonsoft.Json.Linq;
using SharpLua;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLuaServer
{
    public partial class TLuaLSPServer : INotifyPropertyChanged, IDisposable
    {
        [JsonRpcMethod(Methods.TextDocumentCompletionName)]
        public object TextDocumentCompletionName(JToken arg)
        {
            lock (_object)
            {
                CompletionList result = new CompletionList();

                try
                {
                    var method_params = arg.ToObject<CompletionParams>();





                }
                catch (SystemException e)
                {
                    LogException(e);
                }

                return result;
            }
        }

        private void example_add()
        {
            List<CompletionItem> items = new List<CompletionItem>();
            items.Add(new CompletionItem() { Label = "self", Detail = "test self" });
            items.Add(new CompletionItem() { Label = "sss", Detail = "test sss" });
            items.Add(new CompletionItem() { Label = "send", Detail = "test send" });
            items.Add(new CompletionItem() { Label = "socket", Detail = "test socket" });
            LogInfo("TextDocumentCompletionName");
        }
    }
}
