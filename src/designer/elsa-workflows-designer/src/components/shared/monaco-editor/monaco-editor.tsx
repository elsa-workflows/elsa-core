import {Component, Event, EventEmitter, h, Host, Method, Prop, Watch} from '@stencil/core';
import {initializeMonacoWorker, Monaco} from "./utils";
import monacoStore from '../../../data/monaco-store';

export interface MonacoValueChangedArgs {
  value: string;
  markers: Array<any>;
}

export interface MonacoLib {
  content: string;
  filePath?: string;
}

@Component({
  tag: 'elsa-monaco-editor',
  styleUrl: 'monaco-editor.scss',
  shadow: false
})
export class MonacoEditor {
  private monaco: Monaco;
  private container: HTMLElement;
  private editor: any;

  @Prop() public monacoLibPath?: string;
  @Prop() public editorHeight: string = '10em';
  @Prop() public value: string;
  @Prop() public language: string;
  @Prop() public singleLineMode: boolean = false;
  @Prop() public padding: string;
  @Event() public valueChanged: EventEmitter<MonacoValueChangedArgs>;

  @Watch('language')
  languageChangeHandler(newValue: string) {
    if (!this.editor)
      return;

    const model = this.editor.getModel();
    this.monaco.editor.setModelLanguage(model, this.language);
  }

  @Watch('value')
  valueChangeHandler(newValue: string) {
    if (!this.editor)
      return;

    const model = this.editor.getModel();
    //model.setValue(newValue || '');
  }

  @Method()
  async setJavaScriptLibs(libs: Array<MonacoLib>) {
    const monaco = this.monaco;
    const editor = this.editor;

    const defaultLib: MonacoLib = {
      content: "<reference lib=\"es5\" />",
      filePath: "lib.d.ts"
    };

    libs = [...libs];
    monaco.languages.typescript.javascriptDefaults.setExtraLibs(libs);

    for (const lib of libs) {

      const oldModel = editor.getModel(lib.filePath);

      if (oldModel)
        oldModel.dispose();

      const newModel = monaco.editor.createModel(lib.content, 'typescript', monaco.Uri.parse(lib.filePath));
    }
  }

  public async componentWillLoad() {
    const monacoLibPath = this.monacoLibPath || monacoStore.monacoLibPath;
    this.monaco = await initializeMonacoWorker(monacoLibPath);
  }

  public async componentDidLoad() {
    const monaco = this.monaco;
    const language = this.language;

    // Validation settings.
    monaco.languages.typescript.javascriptDefaults.setDiagnosticsOptions({
      noSemanticValidation: true,
      noSyntaxValidation: false,
    });

    // Compiler options.
    monaco.languages.typescript.javascriptDefaults.setCompilerOptions({
      target: monaco.languages.typescript.ScriptTarget.ES2020,
      lib: [],
      allowNonTsExtensions: true
    });

    monaco.languages.typescript.javascriptDefaults.setEagerModelSync(true);

    const defaultOptions = {
      value: this.value,
      language: language,
      fontFamily: "Roboto Mono, monospace",
      renderLineHighlight: 'none',
      minimap: {
        enabled: false
      },
      automaticLayout: true,
      lineNumbers: "on",
      theme: "vs",
      roundedSelection: true,
      scrollBeyondLastLine: false,
      readOnly: false,
      overviewRulerLanes: 0,
      overviewRulerBorder: false,
      lineDecorationsWidth: 0,
      hideCursorInOverviewRuler: true,
      glyphMargin: false
    };

    let options = defaultOptions;

    if (this.singleLineMode) {
      options = {
        ...defaultOptions, ...{
          wordWrap: 'off',
          lineNumbers: 'off',
          lineNumbersMinChars: 0,
          folding: false,
          scrollBeyondLastColumn: 0,
          scrollbar: {horizontal: 'hidden', vertical: 'hidden'},
          find: {addExtraSpaceOnTop: false, autoFindInSelection: 'never', seedSearchStringFromSelection: false},
        }
      }
    }

    this.editor = monaco.editor.create(this.container, options);

    await this.setJavaScriptLibs([]);
    this.registerLiquid();

    this.editor.onDidChangeModelContent(e => {
      const value = this.editor.getValue();
      const markers = monaco.editor.getModelMarkers({owner: language});
      this.valueChanged.emit({value: value, markers: markers});
    });

    if (this.singleLineMode) {
      this.editor.onKeyDown(e => {
        if (e.keyCode == monaco.KeyCode.Enter) {
          // We only prevent enter when the suggest model is not active
          if (this.editor.getContribution('editor.contrib.suggestController').model.state == 0) {
            e.preventDefault();
          }
        }
      });

      this.editor.onDidPaste(e => {
        if (e.range.endLineNumber > 1) {
          let newContent = "";
          const model = this.editor.getModel();
          let lineCount = model.getLineCount();
          for (let i = 0; i < lineCount; i++) {
            newContent += model.getLineContent(i + 1);
          }
          model.setValue(newContent);
        }
      });
    }
  }

  disconnectedCallback() {
    const editor = this.editor;

    if (!!editor)
      editor.dispose();
  }

  registerLiquid() {
    const monaco = this.monaco;
    monaco.languages.register({id: 'liquid'});

    monaco.languages.registerCompletionItemProvider('liquid', {
      provideCompletionItems: () => {
        const autocompleteProviderItems = [];
        const keywords = ['assign', 'capture', 'endcapture', 'increment', 'decrement',
          'if', 'else', 'elsif', 'endif', 'for', 'endfor', 'break',
          'continue', 'limit', 'offset', 'range', 'reversed', 'cols',
          'case', 'endcase', 'when', 'block', 'endblock', 'true', 'false',
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

  render() {
    const padding = this.padding || 'pt-1.5 pl-1';
    return (
      <Host
        class="monaco-editor-host focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 sm:text-sm border-gray-300 p-4"
        style={{'min-height': this.editorHeight}}>
        <div ref={el => this.container = el} class={`rounded-md monaco-editor-container ${padding}`}/>
      </Host>
    )
  }
}
