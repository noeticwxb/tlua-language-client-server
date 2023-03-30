"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.deactivate = exports.activate = void 0;
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
const vscode = require("vscode");
const path = require("path");
const os = require("os");
const node_1 = require("vscode-languageclient/node");
let client;
// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
function activate(context) {
    // Use the console to output diagnostic information (console.log) and errors (console.error)
    // This line of code will only be executed once when your extension is activated
    console.log('Congratulations, your extension "tluaclient" is now active!');
    // The command has been defined in the package.json file
    // Now provide the implementation of the command with registerCommand
    // The commandId parameter must match the command field in package.json
    let disposable = vscode.commands.registerCommand('tluaclient.helloWorld', () => {
        // The code you place here will be executed every time your command is executed
        // Display a message box to the user
        vscode.window.showInformationMessage('Hello World from tluaclient!');
    });
    context.subscriptions.push(disposable);
    // The server is implemented in C#
    let serverCommand = context.asAbsolutePath(path.join('server_bin', 'TLuaServer.exe'));
    let commandOptions = { cwd: process.cwd(), detached: false };
    // If the extension is launched in debug mode then the debug server options are used
    // Otherwise the run options are used
    let serverOptions = (os.platform() === 'win32') ? {
        run: { command: serverCommand, options: commandOptions },
        debug: { command: serverCommand, options: commandOptions }
    } : {
        run: { command: 'mono', args: [serverCommand], options: commandOptions },
        debug: { command: 'mono', args: [serverCommand], options: commandOptions }
    };
    // Options to control the language client
    let clientOptions = {
        // Register the server for plain text documents
        documentSelector: [{ "language": "tlua", "pattern": "**/*.tlua" }],
        synchronize: {
            // Synchronize the setting section 'TLuaLanguageServerConfig' to the server
            configurationSection: 'TLuaLanguageServerConfig',
            // Notify the server about file changes to '.clientrc files contain in the workspace
            fileEvents: vscode.workspace.createFileSystemWatcher("**/*.tlua")
        }
    };
    // Create the language client and start the client.
    client = new node_1.LanguageClient('languageServerExample', 'TLua Language Server', serverOptions, clientOptions);
    // Start the client. This will also launch the server
    client.start();
    vscode.languages.registerDocumentSemanticTokensProvider;
}
exports.activate = activate;
// This method is called when your extension is deactivated
function deactivate() { }
exports.deactivate = deactivate;
//# sourceMappingURL=extension.js.map