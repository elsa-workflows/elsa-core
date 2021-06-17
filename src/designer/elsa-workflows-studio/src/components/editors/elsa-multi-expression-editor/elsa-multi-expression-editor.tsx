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
  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '10em';
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
      <div class="elsa-mb-1">
        <div class="elsa-flex">
          <div class="elsa-flex-1">
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

    return <label htmlFor={fieldId} class="elsa-block elsa-text-sm elsa-font-medium elsa-text-gray-700">
      {fieldLabel}
    </label>;
  }

  renderContextMenuWidget() {
    if (this.supportedSyntaxes.length == 0)
      return undefined;

    const selectedSyntax = this.selectedSyntax;
    const advancedButtonClass = selectedSyntax ? 'elsa-text-blue-500' : 'elsa-text-gray-300'

    return <div class="elsa-relative" ref={el => registerClickOutside(this, el, this.closeContextMenu)}>
      <button type="button" class={`elsa-border-0 focus:elsa-outline-none elsa-text-sm ${advancedButtonClass}`} onClick={e => this.onSettingsClick(e)}>
        {this.renderContextMenuButton()}
      </button>
      <div>
        <div ref={el => this.contextMenu = el}
             data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
             data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
             data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
             data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
             data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
             data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
             class="hidden elsa-origin-top-right elsa-absolute elsa-right-1 elsa-mt-1 elsa-w-56 elsa-rounded-md elsa-shadow-lg elsa-bg-white elsa-ring-1 elsa-ring-black elsa-ring-opacity-5 elsa-divide-y elsa-divide-gray-100 focus:elsa-outline-none elsa-z-10" role="menu"
             aria-orientation="vertical"
             aria-labelledby="options-menu">
          <div class="elsa-py-1" role="none">
            <a onClick={e => this.selectSyntax(e, null)} href="#" class={`elsa-block elsa-px-4 elsa-py-2 elsa-text-sm hover:elsa-bg-gray-100 hover:elsa-text-gray-900 ${!selectedSyntax ? 'elsa-text-blue-700' : 'elsa-text-gray-700'}`} role="menuitem">Default</a>
          </div>
          <div class="elsa-py-1" role="none">
            {this.supportedSyntaxes.map(syntax => (
              <a onClick={e => this.selectSyntax(e, syntax)} href="#"
                 class={`elsa-block elsa-px-4 elsa-py-2 elsa-text-sm hover:elsa-bg-gray-100 hover:elsa-text-gray-900 ${selectedSyntax == syntax ? 'elsa-text-blue-700' : 'elsa-text-gray-700'}`} role="menuitem">{syntax}</a>
            ))}
          </div>
        </div>
      </div>
    </div>;
  }

  renderContextMenuButton() {
    if (!this.selectedSyntax)
      return <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-400" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
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
