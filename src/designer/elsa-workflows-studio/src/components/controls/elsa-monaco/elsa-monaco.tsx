import {Component, Host, h, Prop, State, Listen, Method, Watch, Event, EventEmitter} from '@stencil/core';
import {initializeMonacoWorker} from "./elsa-monaco-utils";
import state from '../../../utils/store';

// Until I figure out why the ESM loader doesn't work properly, we need to include these scripts manually from index.html
// import * as monaco from 'monaco-editor/esm/vs/editor/editor.api';

export interface MonacoValueChangedArgs {
  value: string;
  markers: Array<any>;
}

@Component({
  tag: 'elsa-monaco',
  styleUrl: 'elsa-monaco.css',
  shadow: false
})
export class ElsaMonaco {

  monaco = (window as any).monaco;

  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '5em';
  @Prop() value: string;
  @Prop() language: string;
  @Prop({attribute: 'single-line', reflect: true}) singleLineMode: boolean = false;
  @Prop() padding: string;
  @Event({eventName: 'valueChanged'}) valueChanged: EventEmitter<MonacoValueChangedArgs>;

  container: HTMLElement;
  editor: any;

  @Watch('language')
  languageChangeHandler(newValue: string) {
    if (!this.editor)
      return;

    const model = this.editor.getModel();
    this.monaco.editor.setModelLanguage(model, this.language);
  }

  @Method()
  async setValue(value: string) {
    if (!this.editor)
      return;

    const model = this.editor.getModel();
    model.setValue(value || '');
  }

  @Method()
  async addJavaScriptLib(libSource: string, libUri: string) {
    const monaco = this.monaco;
    monaco.languages.typescript.javascriptDefaults.setExtraLibs([{
      content: libSource,
      filePath: libUri
    }]);

    const oldModel = monaco.editor.getModel(libUri);

    if (oldModel)
      oldModel.dispose();

    const newModel = monaco.editor.createModel(libSource, 'typescript', monaco.Uri.parse(libUri));
  }

  componentWillLoad() {
    initializeMonacoWorker(state.monacoLibPath);
    this.registerLiquid();
  }

  componentDidLoad() {
    const require = (window as any).require;

    require(['require', 'vs/editor/editor.main'], async require => {
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
        allowNonTsExtensions: true,
        allowJs: true
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
    });
  }

  disconnectedCallback() {
    const editor = this.editor;

    if (!!editor)
      editor.dispose();
  }

  registerLiquid() {
    const monaco = (window as any).monaco;
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
    const padding = this.padding || 'elsa-pt-1.5 elsa-pl-1';
    return (
      <Host class="elsa-monaco-editor-host elsa-border focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300 elsa-p-4" style={{'min-height': this.editorHeight}}>
        <div ref={el => this.container = el} class={`elsa-monaco-editor-container ${padding}`}/>
      </Host>
    )
  }
}
