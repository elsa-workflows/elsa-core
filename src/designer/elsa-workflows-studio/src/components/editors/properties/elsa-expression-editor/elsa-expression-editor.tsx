import {Component, h, Host, Prop, State, Watch} from '@stencil/core';
import {createElsaClient} from "../../../../services/elsa-client";
import Tunnel from '../../../data/workflow-editor';
import {MonacoValueChangedArgs} from "../../monaco/elsa-monaco/elsa-monaco";

@Component({
  tag: 'elsa-expression-editor',
  styleUrl: 'elsa-expression-editor.css',
  shadow: false,
})
export class ElsaExpressionEditor {

  @Prop() fieldName: string;
  @Prop() syntax: string;
  @Prop() expression: string;
  @Prop() syntaxes?: Array<string>;
  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '6em';
  @Prop({attribute: 'single-line', reflect: true}) singleLineMode: boolean = false;
  @Prop({attribute: 'context', reflect: true}) context?: string;
  @Prop({mutable: true}) serverUrl: string;
  @Prop({mutable: true}) workflowDefinitionId: string;
  @State() selectedSyntax?: string;
  @State() currentExpression?: string
  monacoEditor: HTMLElsaMonacoElement;

  @Watch("syntax")
  syntaxChangedHandler(newValue: string) {
    this.selectedSyntax = newValue;
  }

  @Watch("expression")
  expressionChangedHandler(newValue: string) {
    this.currentExpression = newValue;
  }

  async componentWillLoad() {
    this.selectedSyntax = this.syntax;
    this.currentExpression = this.expression;
  }

  async componentDidLoad() {
    const elsaClient = createElsaClient(this.serverUrl);
    const libSource = await elsaClient.scriptingApi.getJavaScriptTypeDefinitions(this.workflowDefinitionId, this.context);
    const libUri = 'defaultLib:lib.es6.d.ts';
    await this.monacoEditor.addJavaScriptLib(libSource, libUri);
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

  onSyntaxListChange(e: Event) {
    this.selectedSyntax = (e.currentTarget as HTMLSelectElement).value;
  }

  onSyntaxSelect(e: Event, syntax: string) {
    e.preventDefault()
    this.selectedSyntax = syntax;
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    this.currentExpression = e.value;
  }

  render() {
    const syntaxes = this.syntaxes || ['Literal', 'JavaScript', 'Liquid'];
    const selectedSyntax = this.selectedSyntax ?? (syntaxes.length > 0 ? syntaxes[0] : '');
    const monacoLanguage = this.mapSyntaxToLanguage(selectedSyntax);
    const fieldName = this.fieldName;
    const syntaxFieldName = `${fieldName}Syntax`;
    const value = this.currentExpression;
    const reversedSyntaxes = [...syntaxes];

    return (
      <Host>
        <div class="mt-1">
          <elsa-monaco value={value}
                       language={monacoLanguage}
                       editor-height={this.editorHeight}
                       single-line={this.singleLineMode}
                       onValueChanged={e => this.onMonacoValueChanged(e.detail)}
                       ref={el => this.monacoEditor = el}/>
        </div>
        <input type="hidden" name={fieldName} value={value}/>
        <input type="hidden" name={syntaxFieldName} value={selectedSyntax}/>
      </Host>
    )
  }
}

Tunnel.injectProps(ElsaExpressionEditor, ['serverUrl', 'workflowDefinitionId']);
