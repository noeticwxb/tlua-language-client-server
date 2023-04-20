using LspTypes;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TLuaServer
{
    // 支持哪些协议，协议的参数，参考 https://microsoft.github.io/language-server-protocol/
    // 具体协议在VSCode中的效果，参考 https://code.visualstudio.com/api/language-extensions/programmatic-language-features
    public partial class TLuaLSPServer : INotifyPropertyChanged, IDisposable
    {
        private readonly JsonRpc rpc;
        private readonly ManualResetEvent disconnectEvent = new ManualResetEvent(false);
        private Dictionary<string, DiagnosticSeverity> diagnostics;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Disconnected;
        private bool isDisposed;

        public static TLuaLSPServer Ins;

        public TLuaLSPServer(Stream sender, Stream reader)
        {
            rpc = JsonRpc.Attach(sender, reader, this);
            rpc.Disconnected += OnRpcDisconnected;
        }
        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Exit();
        }
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            if (disposing)
            {
                // free managed resources
                disconnectEvent.Dispose();
            }
            isDisposed = true;
        }
        public void WaitForExit()
        {
            disconnectEvent.WaitOne();
        }
        ~TLuaLSPServer()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        public void Exit()
        {
            disconnectEvent.Set();
            Disconnected?.Invoke(this, new EventArgs());
            System.Environment.Exit(0);
        }



        private static readonly object _object = new object();
        private readonly bool trace = true;

        public readonly TextDocumentSyncKind SyncKind = TextDocumentSyncKind.Full;

        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(JToken arg)
        {
            lock (_object)
            {
                if (trace)
                {
                    System.Console.Error.WriteLine("<-- Initialize");
                    System.Console.Error.WriteLine(arg.ToString());
                }

                var init_params = arg.ToObject<InitializeParams>();

                ServerCapabilities capabilities = new ServerCapabilities
                {
                    TextDocumentSync = new TextDocumentSyncOptions
                    {
                        OpenClose = true,
                        Change = SyncKind, // 如果有性能问题，就要考虑增量式的，这个就要修改BufferManager
                        Save = new SaveOptions
                        {
                            IncludeText = true
                        }
                    },

                    CompletionProvider = null,

                    HoverProvider = true,

                    SignatureHelpProvider = null,

                    DefinitionProvider = false,

                    TypeDefinitionProvider = false,

                    ImplementationProvider = false,

                    ReferencesProvider = false,

                    DocumentHighlightProvider = false,

                    DocumentSymbolProvider = false,

                    CodeLensProvider = null,

                    DocumentLinkProvider = null,

                    DocumentFormattingProvider = false,

                    DocumentRangeFormattingProvider = false,

                    RenameProvider = false,

                    FoldingRangeProvider = new SumType<bool, FoldingRangeOptions, FoldingRangeRegistrationOptions>(false),

                    ExecuteCommandProvider = null,

                    WorkspaceSymbolProvider = false
                };

                InitializeResult result = new InitializeResult
                {
                    Capabilities = capabilities
                };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(result);
                if (trace)
                {
                    System.Console.Error.WriteLine("--> " + json);
                }
                return result;
            }
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public void InitializedName(JToken arg)
        {
            lock (_object)
            {
                try
                {
                    if (trace)
                    {
                        System.Console.Error.WriteLine("<-- Initialized");
                        System.Console.Error.WriteLine(arg.ToString());
                    }
                }
                catch (Exception)
                { }
            }
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public JToken ShutdownName()
        {
            lock (_object)
            {
                try
                {
                    if (trace)
                    {
                        System.Console.Error.WriteLine("<-- Shutdown");
                    }
                }
                catch (Exception)
                { }
                return null;
            }
        }

        [JsonRpcMethod(Methods.ExitName)]
        public void ExitName()
        {
            lock (_object)
            {
                try
                {
                    if (trace)
                    {
                        System.Console.Error.WriteLine("<-- Exit");
                    }
                    Exit();
                }
                catch (Exception)
                { }
            }
        }

        public void LogException(System.Exception ex)
        {
            System.Console.Error.WriteLine(ex.ToString());
        }

        public void LogError(string err)
        {
            System.Console.Error.WriteLine(err);
        }

        public void LogInfo(string info)
        {
            if (trace)
            {
                System.Console.Error.WriteLine(info);
            }         
        }
    }
}
