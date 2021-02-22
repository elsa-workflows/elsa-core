import {Component, Host, h, Prop, State, Listen, Method, Watch, Event, EventEmitter} from '@stencil/core';
import {createElsaClient} from "../../../../services/elsa-client";
import Tunnel, {WorkflowEditorState} from '../../../data/workflow-editor';

// Until I figure out why the ESM loader doesn't work properly, we need to include these scripts manually from index.html
// import * as monaco from 'monaco-editor/esm/vs/editor/editor.api';

@Component({
  tag: 'elsa-monaco',
  styleUrl: 'elsa-monaco.css',
  shadow: false,
})
export class ElsaMonaco {

  monaco = (window as any).monaco;

  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '6em';
  @Prop() value: string;
  @Prop() syntax: string;
  @Prop() serverUrl: string;
  @Prop() workflowDefinitionId: string;
  @Event({eventName: 'valueChanged'}) valueChanged: EventEmitter<string>;

  container: HTMLElement;
  editor: any;

  @Watch('syntax')
  syntaxChangeHandler(newValue: string) {
    if(!this.editor)
      return;

    const language = this.mapSyntaxToLanguage(newValue);
    const model = this.editor.getModel();
    this.monaco.editor.setModelLanguage(model, language);
  }

  componentWillLoad() {
    this.registerLiquid();

    debugger;
  }

  componentDidLoad() {
    const require = (window as any).require;

    require(['require', 'vs/editor/editor.main'], async require => {
      const monaco = this.monaco;
      const language = this.mapSyntaxToLanguage(this.syntax);

      // Validation settings.
      monaco.languages.typescript.javascriptDefaults.setDiagnosticsOptions({
        noSemanticValidation: true,
        noSyntaxValidation: false
      });

      // Compiler options.
      monaco.languages.typescript.javascriptDefaults.setCompilerOptions({
        target: monaco.languages.typescript.ScriptTarget.ES6,
        lib: [],
        allowNonTsExtensions: true,
        allowJs: true
      });

      // Extra libraries.
      const elsaClient = createElsaClient(this.serverUrl);
      const libSource = await elsaClient.scriptingApi.getJavaScriptTypeDefinitions(this.workflowDefinitionId);
      const libUri = `ts:filename/elsa.d.ts`;

      monaco.languages.typescript.javascriptDefaults.addExtraLib(libSource, libUri);
      monaco.editor.createModel(libSource, 'typescript', monaco.Uri.parse(libUri));

      this.editor = monaco.editor.create(this.container, {
        value: this.value,
        language: language,
        fontFamily: "Roboto Mono, monospace",
        minimap: {
          enabled: false
        },
        lineNumbers: "on",
        theme: "vs-dark",
        roundedSelection: true,
        scrollBeyondLastLine: false,
        readOnly: false,
      });

      this.editor.onDidChangeModelContent(e => {
        const value = this.editor.getValue();
        this.valueChanged.emit(value);
      });
    });
  }

  disconnectedCallback() {
    this.editor.destroy();
  }

  mapSyntaxToLanguage(syntax: string): any {
    switch (syntax) {
      case 'JavaScript':
        return 'javascript';
      case 'Liquid':
        return 'handlebars';
      case 'Literal':
      default:
        return 'plaintext';
    }
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
    return (
      <Host class="monaco-editor-host" style={{'min-height': this.editorHeight}}>
        <div ref={el => this.container = el} class="monaco-editor-container rounded"/>
      </Host>
    )
  }
}

Tunnel.injectProps(ElsaMonaco, ['serverUrl', 'workflowDefinitionId']);
