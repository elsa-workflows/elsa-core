import {Mutex} from "async-mutex";
import {isNullOrWhitespace} from "../../../utils";

const win = window as any;
const require = win.require;

export interface Monaco {
  editor: any;
  languages: any;
  KeyCode: any;
  Uri: any;
}

export interface EditorVariable {
  variableName: string;
  type: string;
}

export var EditorVariables: Array<EditorVariable> = [];

let isInitialized: boolean;
const mutex = new Mutex();

export async function initializeMonacoWorker(libPath?: string): Promise<Monaco> {

  return await mutex.runExclusive(async () => {

    if (isInitialized) {
      return win.monaco;
    }

    if (isNullOrWhitespace(libPath)) {
      // The lib path is not set, which means external code will be responsible for loading the Monaco editor.
      // In this case, spin while we wait for the editor to be loaded.

      // Create a promise that will resolve after the win.monaco variable exists. Do this in a tight loop, checking for win.monaco to exist on each iteration. Within the same iteration, request the next animation frame as to not tw-block the UI.
      // If we don't get win.monaco after 3 seconds, reject the promise.

      return new Promise<Monaco>((resolve, reject) => {
        const startTime = Date.now();
        const timeout = 3000;

        const checkForMonaco = () => {
          if (win.monaco) {
            resolve(win.monaco);
            return;
          }

          if (Date.now() - startTime > timeout) {
            reject('Monaco editor not loaded.');
            return;
          }

          requestAnimationFrame(checkForMonaco);
        };

        checkForMonaco();
      });

    }

    const origin = document.location.origin;
    const baseUrl = libPath.startsWith('http') ? libPath : `${origin}/${libPath}`;

    require.config({paths: {'vs': `${baseUrl}/vs`}});
    win.MonacoEnvironment = {getWorkerUrl: () => proxy};

    let proxy = URL.createObjectURL(new Blob([`
	self.MonacoEnvironment = {
		baseUrl: '${baseUrl}'
	};
	importScripts('${baseUrl}/vs/base/worker/workerMain.js');
`], {type: 'text/javascript'}));

    return new Promise(resolve => {
      require(["vs/editor/editor.main"], () => {
        isInitialized = true;
        registerLiquid(win.monaco);
        registerSql(win.monaco);
        resolve(win.monaco);
      });
    });
  });
}

function registerLiquid(monaco: any) {
  monaco.languages.register({id: 'liquid'});

  monaco.languages.registerCompletionItemProvider('liquid', {
    provideCompletionItems: () => {
      const autocompleteProviderItems = [];
      const keywords = ['assign', 'capture', 'endcapture', 'increment', 'decrement',
        'if', 'else', 'elsif', 'endif', 'for', 'endfor', 'break',
        'continue', 'limit', 'offset', 'range', 'reversed', 'cols',
        'case', 'endcase', 'when', 'tw-block', 'endblock', 'true', 'false',
        'in', 'unless', 'endunless', 'cycle', 'tablerow', 'endtablerow',
        'contains', 'startswith', 'endswith', 'comment', 'endcomment',
        'raw', 'endraw', 'editable', 'endentitylist', 'endentityview', 'endinclude',
        'endmarker', 'entitylist', 'entityview', 'forloop', 'image', 'include',
        'marker', 'outputcache', 'plugin', 'style', 'text', 'widget',
        'abs', 'append', 'at_least', 'at_most', 'capitalize', 'ceil', 'compact',
        'concat', 'date', 'default', 'divided_by', 'downcase', 'escape',
        'escape_once', 'first', 'floor', 'join', 'last', 'lstrip', 'map',
        'minus', 'modulo', 'newline_to_br', 'plus', 'prepend', 'remove',
        'remove_first', 'replace', 'replace_first', 'reverse', 'round',
        'rstrip', 'size', 'slice', 'sort', 'sort_natural', 'split', 'strip',
        'strip_html', 'strip_newlines', 'times', 'truncate', 'truncatewords',
        'uniq', 'upcase', 'url_decode', 'url_encode'];

      for (let i = 0; i < keywords.length; i++) {
        autocompleteProviderItems.push({'label': keywords[i], kind: monaco.languages.CompletionItemKind.Keyword});
      }

      return {suggestions: autocompleteProviderItems};
    }
  });
}

function registerSql(monaco: any) {

  monaco.languages.registerCompletionItemProvider('sql', {
    triggerCharacters: ["@"],
    provideCompletionItems: (model, position) => {

      const word = model.getWordUntilPosition(position)

      const autocompleteProviderItems = [];
      for (const variable of EditorVariables) {
        autocompleteProviderItems.push({
          label: `${variable.variableName}: ${variable.type}`,
          kind: monaco.languages.CompletionItemKind.Variable,
          insertText: variable.variableName
        });
      }

      return {suggestions: autocompleteProviderItems};
    }
  });

}

