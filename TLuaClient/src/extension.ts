// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import * as path from 'path';
import * as os from 'os';

import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	ExecutableOptions,
	Executable
} from 'vscode-languageclient/node';

let client: LanguageClient;

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {

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
	let commandOptions: ExecutableOptions = { cwd: process.cwd(),  detached: false };

	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	let serverOptions: ServerOptions =
		(os.platform() === 'win32') ? {
			run: <Executable>{ command: serverCommand, options: commandOptions },
			debug: <Executable>{ command: serverCommand, options: commandOptions }
		} : {
			run: <Executable>{ command: 'mono', args: [serverCommand], options: commandOptions },
			debug: <Executable>{ command: 'mono', args: [serverCommand], options: commandOptions }
		};

	// Options to control the language client
	let clientOptions: LanguageClientOptions = {
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
	client = new LanguageClient(
		'languageServerExample',
		'TLua Language Server',
		serverOptions,
		clientOptions
	);

	// Start the client. This will also launch the server
	client.start();

	vscode.languages.registerDocumentSemanticTokensProvider
}

// This method is called when your extension is deactivated
export function deactivate() { }
