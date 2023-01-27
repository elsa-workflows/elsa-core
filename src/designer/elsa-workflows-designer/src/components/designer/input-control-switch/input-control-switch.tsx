import {Component, Event, EventEmitter, Listen, h, Prop, State} from '@stencil/core';
import {debounce} from 'lodash';
import {enter, leave, toggle} from 'el-transition'
import {IntellisenseContext, SyntaxNames} from "../../../models";
import {SyntaxSelectorIcon} from "../../icons/tooling/syntax-selector";
import {MonacoValueChangedArgs} from "../../shared/monaco-editor/monaco-editor";
import {Hint} from "../../shared/forms/hint";
import {mapSyntaxToLanguage} from "../../../utils";
import descriptorsStore from "../../../data/descriptors-store";
import {Container} from "typedi";
import {ElsaClient, ElsaClientProvider} from "../../../services";
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
  @Prop() containerType: string;
  @Prop() containerId: string;
  @Prop() activityType: string;
  @Prop() propertyName: string;

  @Prop() label: string;
  @Prop() hideLabel: boolean;
  @Prop() hint: string;
  @Prop() fieldName?: string;
  @Prop() syntax?: string;
  @Prop() expression?: string;
  @Prop() defaultSyntax: string = SyntaxNames.Literal;
  @Prop() supportedSyntaxes: Array<string> = ['JavaScript', 'Liquid']; // TODO: Get available syntaxes from some more centralized settings.
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
    const containerType = this.containerType;
    const containerId = this.containerId;
    const activityTypeName = this.activityType;
    const propertyName = this.propertyName;
    const typeDefinitions = await elsaClient.scripting.javaScriptApi.getTypeDefinitions({containerType, containerId, activityTypeName, propertyName});
    const libUri = 'defaultLib:lib.es6.d.ts';
    await this.monacoEditor.addJavaScriptLib(typeDefinitions, libUri);
  }

  render() {

    if (this.hideLabel && !this.shouldRenderMonaco()) {
      return <div class="p-4">
        <div class="flex">
          <div class="flex-1">
            {this.renderEditor()}
          </div>
          {this.renderContextMenuWidget()}
        </div>
      </div>
    }

    return <div class="p-4">
      <div class="mb-1">
        <div class="flex">
          <div class="flex-1">
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

    return <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
      {fieldLabel}
    </label>;
  }

  private renderContextMenuWidget() {
    if (this.supportedSyntaxes.length == 0)
      return undefined;

    const selectedSyntax = this.syntax;
    const advancedButtonClass = selectedSyntax ? 'text-blue-500' : 'text-gray-300'

    return <div class="relative" ref={el => this.contextMenuWidget = el}>
      <button type="button" class={`border-0 focus:outline-none text-sm ${advancedButtonClass}`} onClick={e => this.onSettingsClick(e)}>
        {!this.isReadOnly ? this.renderContextMenuButton() : ""}
      </button>
      <div>
        <div ref={el => this.contextMenu = el}
             data-transition-enter="transition ease-out duration-100"
             data-transition-enter-start="transform opacity-0 scale-95"
             data-transition-enter-end="transform opacity-100 scale-100"
             data-transition-leave="transition ease-in duration-75"
             data-transition-leave-start="transform opacity-100 scale-100"
             data-transition-leave-end="transform opacity-0 scale-95"
             class="hidden origin-top-right absolute right-1 mt-1 w-56 rounded-md shadow-lg bg-white ring-1 ring-black ring-opacity-5 divide-y divide-gray-100 focus:outline-none z-10" role="menu"
             aria-orientation="vertical"
             aria-labelledby="options-menu">
          <div class="py-1" role="none">
            <a onClick={e => this.selectSyntax(e, null)} href="#" class={`block px-4 py-2 text-sm hover:bg-gray-100 hover:text-gray-900 ${!selectedSyntax ? 'text-blue-700' : 'text-gray-700'}`} role="menuitem">Default</a>
          </div>
          <div class="py-1" role="none">
            {this.supportedSyntaxes.map(syntax => (
              <a onClick={e => this.selectSyntax(e, syntax)} href="#"
                 class={`block px-4 py-2 text-sm hover:bg-gray-100 hover:text-gray-900 ${selectedSyntax == syntax ? 'text-blue-700' : 'text-gray-700'}`} role="menuitem">{syntax}</a>
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
    const expressionEditorClass = showMonaco ? 'block' : 'hidden';
    const defaultEditorClass = showMonaco ? 'hidden' : 'block';
    const propertyType = "Object";
    const typeDescriptor = descriptorsStore.variableDescriptors.find(x => x.typeName == propertyType);
    const propertyTypeName = typeDescriptor?.displayName ?? propertyType;

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

InputControlSwitchContextState.injectProps(InputControlSwitch, ['containerType', 'containerId', 'activityType', 'propertyName'])
