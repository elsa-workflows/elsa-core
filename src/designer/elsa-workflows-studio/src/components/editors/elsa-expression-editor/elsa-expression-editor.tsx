import { Component, EventEmitter, h, Method, Prop, State, Watch, Event } from '@stencil/core';
import { createElsaClient } from '../../../services';
import Tunnel from '../../../data/workflow-editor';
import { MonacoValueChangedArgs } from '../../controls/elsa-monaco/elsa-monaco';
import { IntellisenseContext } from '../../../models';
import { monacoEditorDialogService } from '../../../services/monaco-editor-dialog-service';
import { IconName, iconProvider } from '../../../services/icon-provider';

@Component({
  tag: 'elsa-expression-editor',
  shadow: false,
})
export class ElsaExpressionEditor {
  @Event() expressionChanged: EventEmitter<string>;
  @Prop() opensModal: boolean = false;
  @Prop() language: string;
  @Prop() expression: string;
  @Prop({ attribute: 'editor-height', reflect: true }) editorHeight: string = '6em';
  @Prop({ attribute: 'single-line', reflect: true }) singleLineMode: boolean = false;
  @Prop() padding: string;
  @Prop() context?: IntellisenseContext;
  @Prop({ mutable: true }) serverUrl: string;
  @Prop({ mutable: true }) workflowDefinitionId: string;
  @State() currentExpression?: string;

  monacoEditor: HTMLElsaMonacoElement;

  @Watch('expression')
  expressionChangedHandler(newValue: string) {
    this.currentExpression = newValue;
  }

  @Method()
  async setExpression(value: string) {
    await this.monacoEditor.setValue(value);
  }

  async componentWillLoad() {
    this.currentExpression = this.expression;
  }

  async componentDidLoad() {
    const elsaClient = await createElsaClient(this.serverUrl);
    const libSource = await elsaClient.scriptingApi.getJavaScriptTypeDefinitions(this.workflowDefinitionId, this.context);
    const libUri = 'defaultLib:lib.es6.d.ts';
    await this.monacoEditor.addJavaScriptLib(libSource, libUri);
    if (monacoEditorDialogService.monacoEditor) {
      monacoEditorDialogService.monacoEditor.addJavaScriptLib(libSource, libUri);
    }
  }

  async onMonacoValueChanged(e: MonacoValueChangedArgs) {
    this.currentExpression = e.value;
    await this.expressionChanged.emit(e.value);
  }

  onEditorClick = e => {
    e.preventDefault();
    monacoEditorDialogService.show(this.language, this.currentExpression, (val: string) => this.setExpression(val));
  };

  render() {
    const language = this.language;
    const value = this.currentExpression;

    return (
      <elsa-monaco
        value={value}
        language={language}
        editor-height={this.editorHeight}
        single-line={this.singleLineMode}
        padding={this.padding}
        onValueChanged={e => this.onMonacoValueChanged(e.detail)}
        ref={el => (this.monacoEditor = el)}
      >
        {this.opensModal &&
          <button
            class="elsa-absolute elsa-z-10"
            style={{ left: "0.25rem", top: "0.35rem" }}
            onClick={this.onEditorClick}
            >
            { iconProvider.getIcon(IconName.OpenInDialog) }
          </button>
        }
      </elsa-monaco>
    );
  }
}

Tunnel.injectProps(ElsaExpressionEditor, ['serverUrl', 'workflowDefinitionId']);
