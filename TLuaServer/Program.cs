using LspTypes;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TLuaServer;

namespace Server
{
    class Program
    {
        private static void Main(string[] args) => MainAsync(args).Wait();

        //static void Main(string[] args)
        //{
        //    Program program = new Program();
        //    program.MainAsync(args).GetAwaiter().GetResult();
        //}

        private static async Task MainAsync(string[] args)
        {
            System.IO.Stream stdin = Console.OpenStandardInput();
            System.IO.Stream stdout = Console.OpenStandardOutput();
            stdin = new Tee(stdin, new Dup("editor"), Tee.StreamOwnership.OwnNone);
            stdout = new Tee(stdout, new Dup("server"), Tee.StreamOwnership.OwnNone);
            BufferManager.Init();
            TLuaLSPServer.Ins = new TLuaLSPServer(stdout, stdin);
            TLua.Log.Init(new TLuaLog());

            await Task.Delay(-1);
        }
    }


}
