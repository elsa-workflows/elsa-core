import {Component, Event, EventEmitter, h, Prop, State} from '@stencil/core';
import {SyntaxNames} from "../../../models";
import {registerClickOutside} from "stencil-click-outside";
import {enter, leave, toggle} from 'el-transition'
import {Map, mapSyntaxToLanguage} from "../../../utils/utils";

@Component({
  tag: 'elsa-multi-expression-editor',
  shadow: false,
})
export class ElsaMultiExpressionEditor {
  
  @Prop() label: string;
  @Prop() fieldName?: string;
  @Prop() syntax?: string;
  @Prop() defaultSyntax: string = SyntaxNames.Literal;
  @Prop() expressions: Map<string> = {};
  @Prop() supportedSyntaxes: Array<string> = [];
  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '6em';
  @Prop({attribute: 'single-line', reflect: true}) singleLineMode: boolean = false;
  @Prop({attribute: 'context', reflect: true}) context?: string;

  @Event() syntaxChanged: EventEmitter<string>;
  @Event() expressionChanged: EventEmitter<string>;
  
  @State() selectedSyntax?: string;
  @State() currentValue?: string;

  contextMenu: HTMLElement;
  expressionEditor: HTMLElsaExpressionEditorElement;
  defaultSyntaxValue: string;

  async componentWillLoad() {
    this.selectedSyntax = this.syntax;
    this.currentValue = this.expressions[this.selectedSyntax ? this.selectedSyntax : this.defaultSyntax];
  }

  toggleContextMenu() {
    toggle(this.contextMenu);
  }

  openContextMenu() {
    enter(this.contextMenu);
  }

  closeContextMenu() {
    leave(this.contextMenu);
  }

  selectDefaultEditor(e: Event) {
    e.preventDefault();
    this.selectedSyntax = undefined;
    this.closeContextMenu();
  }

  async selectSyntax(e: Event, syntax: string) {
    e.preventDefault();
    
    this.selectedSyntax = syntax;
    this.syntaxChanged.emit(syntax);
    
    this.currentValue = this.expressions[syntax ? syntax : this.defaultSyntax || SyntaxNames.Literal];
    await this.expressionEditor.setExpression(this.currentValue);
    
    this.closeContextMenu();
  }

  onSettingsClick(e: Event) {
    this.toggleContextMenu();
  }

  onExpressionChanged(e: CustomEvent<string>) {
    const expression = e.detail;
    this.expressions[this.selectedSyntax || this.defaultSyntax] = expression;
    this.expressionChanged.emit(expression);
  }

  render() {
    return <div>
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

  renderLabel() {
    if (!this.label)
      return undefined;
    
    const fieldId = this.fieldName;
    const fieldLabel = this.label || fieldId;

    return <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
      {fieldLabel}
    </label>;
  }

  renderContextMenuWidget() {
    if (this.supportedSyntaxes.length == 0)
      return undefined;

    const selectedSyntax = this.selectedSyntax;
    const advancedButtonClass = selectedSyntax ? 'text-blue-500' : 'text-gray-300'

    return <div class="relative" ref={el => registerClickOutside(this, el, this.closeContextMenu)}>
      <button type="button" class={`border-0 focus:outline-none text-sm ${advancedButtonClass}`} onClick={e => this.onSettingsClick(e)}>
        {this.renderContextMenuButton()}
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

  renderContextMenuButton() {
    if (!this.selectedSyntax)
      return <svg class="h-5 w-5 text-gray-400" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <circle cx="12" cy="12" r="9"/>
        <line x1="8" y1="12" x2="8" y2="12.01"/>
        <line x1="12" y1="12" x2="12" y2="12.01"/>
        <line x1="16" y1="12" x2="16" y2="12.01"/>
      </svg>;

    return <span>{this.selectedSyntax}</span>;
  }

  renderEditor() {
    const selectedSyntax = this.selectedSyntax;
    const monacoLanguage = mapSyntaxToLanguage(selectedSyntax);
    const value = this.currentValue;
    const expressionEditorClass = selectedSyntax ? 'block' : 'hidden';
    const defaultEditorClass = selectedSyntax ? 'hidden' : 'block';

    return (
      <div>
        <div class={expressionEditorClass}>
          <elsa-expression-editor ref={el => this.expressionEditor = el}
                                  onExpressionChanged={e => this.onExpressionChanged(e)}
                                  expression={value}
                                  language={monacoLanguage}
                                  editorHeight={this.editorHeight}
                                  singleLineMode={this.singleLineMode}
                                  context={this.context}/>
        </div>
        <div class={defaultEditorClass}>
          <slot/>
        </div>
      </div>
    );
  }
}
