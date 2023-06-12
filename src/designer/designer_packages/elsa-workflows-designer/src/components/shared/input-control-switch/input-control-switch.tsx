import {Component, Event, EventEmitter, Listen, h, Prop, State} from '@stencil/core';
import {debounce} from 'lodash';
import {enter, leave, toggle} from 'el-transition'
import {IntellisenseContext, SyntaxNames} from "../../../models";
import {SyntaxSelectorIcon} from "../../icons/tooling/syntax-selector";
import {MonacoValueChangedArgs} from "../monaco-editor/monaco-editor";
import {Hint} from "../forms/hint";
import {mapSyntaxToLanguage} from "../../../utils";
import {Container} from "typedi";
import {ElsaClientProvider} from "../../../services";
import InputControlSwitchContextState from "./state";

export interface ExpressionChangedArs {
  expression: string;
  syntax: string;
}

@Component({
  tag: 'elsa-input-control-switch',
  shadow: false,
})
export class InputControlSwitch {
  private contextMenu: HTMLElement;
  private monacoEditor: HTMLElsaMonacoEditorElement;
  private contextMenuWidget: HTMLElement;
  private readonly onExpressionChangedDebounced: (e: MonacoValueChangedArgs) => void;

  constructor() {
    this.onExpressionChangedDebounced = debounce(this.onExpressionChanged, 10);
  }

  // Tunneled props.
  @Prop({mutable: true}) workflowDefinitionId: string;
  @Prop({mutable: true}) activityType: string;
  @Prop({mutable: true}) propertyName: string;

  @Prop() label: string;
  @Prop() hideLabel: boolean;
  @Prop() hint: string;
  @Prop() fieldName?: string;
  @Prop() syntax?: string;
  @Prop() expression?: string;
  @Prop() defaultSyntax: string = SyntaxNames.Literal;
  @Prop() supportedSyntaxes: Array<string> = ['JavaScript', 'Liquid']; // TODO: Get available syntaxes from server.
  @Prop() isReadOnly?: boolean;
  @Prop() codeEditorHeight: string = '16em';
  @Prop() codeEditorSingleLineMode: boolean = false;
  @Prop() context?: IntellisenseContext;

  @Event() syntaxChanged: EventEmitter<string>;
  @Event() expressionChanged: EventEmitter<ExpressionChangedArs>;

  @State() currentExpression?: string;

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.contextMenuWidget || !this.contextMenuWidget.contains(target))
      this.closeContextMenu();
  }

  async componentWillLoad() {
    this.currentExpression = this.expression;
  }

  async componentDidLoad() {
    const elsaClient = await Container.get(ElsaClientProvider).getElsaClient();
    const workflowDefinitionId = this.workflowDefinitionId;
    const activityTypeName = this.activityType;
    const propertyName = this.propertyName;
    const typeDefinitions = await elsaClient.scripting.javaScriptApi.getTypeDefinitions({workflowDefinitionId, activityTypeName, propertyName});
    const libUri = 'defaultLib:lib.es6.d.ts';
    await this.monacoEditor.addJavaScriptLib(typeDefinitions, libUri);
  }

  render() {

    if (this.hideLabel && !this.shouldRenderMonaco()) {
      return <div class="tw-p-4">
        <div class="tw-flex">
          <div class="tw-flex-1">
            {this.renderEditor()}
          </div>
          {this.renderContextMenuWidget()}
        </div>
      </div>
    }

    return <div class="tw-p-4">
      <div class="tw-mb-1">
        <div class="tw-flex">
          <div class="tw-flex-1">
            {this.renderLabel()}
          </div>
          {this.renderContextMenuWidget()}
        </div>
      </div>
      {this.renderEditor()}
    </div>
  }

  private renderLabel() {
    const fieldId = this.fieldName;
    const fieldLabel = this.label || fieldId;

    return <label htmlFor={fieldId} class="tw-block tw-text-sm tw-font-medium tw-text-gray-700">
      {fieldLabel}
    </label>;
  }

  private renderContextMenuWidget() {
    if (this.supportedSyntaxes.length == 0)
      return undefined;

    const selectedSyntax = this.syntax;
    const advancedButtonClass = selectedSyntax ? 'tw-text-blue-500' : 'tw-text-gray-300'

    return <div class="tw-relative" ref={el => this.contextMenuWidget = el}>
      <button type="button" class={`tw-border-0 focus:tw-outline-none tw-text-sm ${advancedButtonClass}`} onClick={e => this.onSettingsClick(e)}>
        {!this.isReadOnly ? this.renderContextMenuButton() : ""}
      </button>
      <div>
        <div ref={el => this.contextMenu = el}
             data-transition-enter="tw-transition tw-ease-out tw-duration-100"
             data-transition-enter-start="tw-transform tw-opacity-0 tw-scale-95"
             data-transition-enter-end="tw-transform tw-opacity-100 tw-scale-100"
             data-transition-leave="tw-transition tw-ease-in tw-duration-75"
             data-transition-leave-start="tw-transform tw-opacity-100 tw-scale-100"
             data-transition-leave-end="tw-transform tw-opacity-0 tw-scale-95"
             class="hidden tw-origin-top-right tw-absolute tw-right-1 tw-mt-1 tw-w-56 tw-rounded-md tw-shadow-lg tw-bg-white tw-ring-1 tw-ring-black tw-ring-opacity-5 tw-divide-y tw-divide-gray-100 focus:tw-outline-none tw-z-10" role="menu"
             aria-orientation="vertical"
             aria-labelledby="options-menu">
          <div class="tw-py-1" role="none">
            <a onClick={e => this.selectSyntax(e, null)} href="#" class={`tw-block tw-px-4 tw-py-2 tw-text-sm hover:tw-bg-gray-100 hover:tw-text-gray-900 ${!selectedSyntax ? 'tw-text-blue-700' : 'tw-text-gray-700'}`} role="menuitem">Default</a>
          </div>
          <div class="tw-py-1" role="none">
            {this.supportedSyntaxes.map(syntax => (
              <a onClick={e => this.selectSyntax(e, syntax)} href="#"
                 class={`tw-block tw-px-4 tw-py-2 tw-text-sm hover:tw-bg-gray-100 hover:tw-text-gray-900 ${selectedSyntax == syntax ? 'tw-text-blue-700' : 'tw-text-gray-700'}`} role="menuitem">{syntax}</a>
            ))}
          </div>
        </div>
      </div>
    </div>;
  }

  private shouldRenderMonaco = () => !!this.syntax && this.syntax != 'Literal' && !!this.supportedSyntaxes.find(x => x === this.syntax)
  private renderContextMenuButton = () => this.shouldRenderMonaco() ? <span>{this.syntax}</span> : <SyntaxSelectorIcon/>;

  private renderEditor = () => {
    const selectedSyntax = this.syntax;
    const monacoLanguage = mapSyntaxToLanguage(selectedSyntax);
    const value = this.expression;
    const showMonaco = !!selectedSyntax && selectedSyntax != 'Literal' && !!this.supportedSyntaxes.find(x => x === selectedSyntax);
    const expressionEditorClass = showMonaco ? 'tw-block' : 'hidden';
    const defaultEditorClass = showMonaco ? 'hidden' : 'tw-block';

    return (
      <div>
        <div class={expressionEditorClass}>
          <elsa-monaco-editor
            value={value}
            language={monacoLanguage}
            editor-height={this.codeEditorHeight}
            single-line={this.codeEditorSingleLineMode}
            onValueChanged={e => this.onExpressionChangedDebounced(e.detail)}
            ref={el => this.monacoEditor = el}/>
        </div>
        <div class={defaultEditorClass}>
          <slot/>
        </div>
        <Hint text={this.hint}/>
      </div>
    );
  }

  private toggleContextMenu() {
    toggle(this.contextMenu);
  }

  private openContextMenu() {
    enter(this.contextMenu);
  }

  private closeContextMenu() {
    if (!!this.contextMenu)
      leave(this.contextMenu);
  }

  private selectDefaultEditor(e: Event) {
    e.preventDefault();
    this.syntax = undefined;
    this.closeContextMenu();
  }

  private async selectSyntax(e: Event, syntax: string) {
    e.preventDefault();

    this.syntax = syntax;
    this.syntaxChanged.emit(syntax);
    this.expressionChanged.emit({expression: this.currentExpression, syntax});

    this.closeContextMenu();
  }

  private onSettingsClick(e: Event) {
    this.toggleContextMenu();
  }

  private onExpressionChanged(e: MonacoValueChangedArgs) {
    const expression = e.value;
    this.currentExpression = expression;
    this.expressionChanged.emit({expression, syntax: this.syntax});
  }
}

InputControlSwitchContextState.injectProps(InputControlSwitch, ['workflowDefinitionId', 'activityType', 'propertyName'])
