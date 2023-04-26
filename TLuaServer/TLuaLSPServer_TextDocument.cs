using LspTypes;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace TLuaServer
{
    public partial class TLuaLSPServer : INotifyPropertyChanged, IDisposable
    {

        [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
        public void TextDocumentDidOpenName(JToken arg)
        {
            lock (_object)
            {
                try
                {
                    var method_params = arg.ToObject<DidOpenTextDocumentParams>();
                    TextDocumentItem item = method_params.TextDocument;

                    LogInfo("TextDocumentDidOpenName " + item.Uri.ToString());

                    BufferManager.instance.UpdateText(item.Uri.ToString(), item.Text);

                }
                catch (SystemException e)
                {
                    LogException(e);
                }
            }
        }

        [JsonRpcMethod(Methods.TextDocumentDidSaveName)]
        public void TextDocumentDidSaveName(JToken arg)
        {
            lock (_object)
            {
                try
                {
                    var method_param = arg.ToObject<DidSaveTextDocumentParams>();
                    TextDocumentIdentifier item = method_param.TextDocument;
                    string text = method_param.Text;

                    LogInfo("TextDocumentDidSaveName " + item.Uri);

                    if (!string.IsNullOrEmpty(text))
                    {
                        BufferManager.instance.UpdateText(item.Uri, text);
                    }
                    else
                    {
                        LogError("TextDocumentDidSaveName but text is empty");
                    }


                }
                catch (SystemException e)
                {
                    LogException(e);
                }
            }
        }

        [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
        public void TextDocumentDidChangeName(JToken arg)
        {
            lock (_object)
            {
                try
                {
                    var method_params = arg.ToObject<DidChangeTextDocumentParams>();
                    VersionedTextDocumentIdentifier item = method_params.TextDocument;

                    string uri = item.Uri;

                    if(this.SyncKind == TextDocumentSyncKind.Full)
                    {
                        TextDocumentContentChangeEvent[] events = method_params.ContentChanges;
                        for (int i = 0; i < events.Length; ++i)
                        {
                            TextDocumentContentChangeEvent changeEvent = events[i];
                            BufferManager.instance.UpdateText(uri, changeEvent.Text);
                        }
                    }
                    else
                    {
                        LogError("not support " + SyncKind);
                    }

                    //LogInfo("TextDocumentDidChangeName " + item.Uri.ToString());

                }
                catch (SystemException e)
                {
                    LogException(e);
                }
            }
        }

        [JsonRpcMethod(Methods.TextDocumentDidCloseName)]
        public void TextDocumentDidCloseName(JToken arg)
        {
            lock (_object)
            {
                try
                {
                    var method_params = arg.ToObject<DidCloseTextDocumentParams>();
                    LogInfo("TextDocumentDidCloseName  " + method_params.TextDocument.Uri);
                }
                catch (SystemException e)
                {
                    LogException(e);
                }
            }
        }
    }
}
