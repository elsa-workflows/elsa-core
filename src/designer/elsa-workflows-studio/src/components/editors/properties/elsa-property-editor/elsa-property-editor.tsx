import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";

@Component({
  tag: 'elsa-property-editor',
  styleUrl: 'elsa-property-editor.css',
  shadow: false,
})
export class ElsaPropertyEditor {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '6em';
  @Prop({attribute: 'single-line', reflect: true}) singleLineMode: boolean = false;
  @Prop({attribute: 'context', reflect: true}) context?: string;
  @State() selectedSyntax?: string;
  @State() currentValue?: string
  @State() advancedMode: boolean

  async componentWillLoad() {
    this.selectedSyntax = this.propertyModel.syntax;

    let currentValue = this.propertyModel.expression;

    if (currentValue == undefined) {
      const defaultValue = this.propertyDescriptor.defaultValue;
      currentValue = defaultValue ? defaultValue.toString() : undefined;
    }

    this.currentValue = currentValue;
  }

  onAdvancedModeClick(e: Event) {
    this.advancedMode = !this.advancedMode;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldLabel = propertyDescriptor.label || propertyName;
    const fieldHint = propertyDescriptor.hint;
    const advancedButtonClass = this.advancedMode ? 'text-blue-500' : 'text-gray-300'

    return <div>

      <div class="mb-1">
        <div class="flex">
          <div class="flex-1">
            <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
              {fieldLabel}
            </label>
          </div>
          <div class="relative" x-data="{ open: true }">
            <div x-description="Dropdown menu, show/hide based on menu state." x-show="open" x-transition:enter="transition ease-out duration-100" x-transition:enter-start="transform opacity-0 scale-95"
                 x-transition:enter-end="transform opacity-100 scale-100" x-transition:leave="transition ease-in duration-75" x-transition:leave-start="transform opacity-100 scale-100" x-transition:leave-end="transform opacity-0 scale-95"
                 class="origin-top-right absolute right-1 mt-2 w-56 rounded-md shadow-lg bg-white ring-1 ring-black ring-opacity-5 divide-y divide-gray-100 focus:outline-none z-10" role="menu" aria-orientation="vertical"
                 aria-labelledby="options-menu">
              <div class="py-1" role="none">
                <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">Text</a>
              </div>
              <div class="py-1" role="none">
                <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">Literal</a>
                <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">JavaScript</a>
                <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">Liquid</a>
              </div>
              <div class="py-1" role="none">
                <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">Output</a>
                <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">Variable</a>
              </div>
            </div>

          </div>
          <div>
            <button type="button" class={`border-0 focus:outline-none ${advancedButtonClass}`} onClick={e => this.onAdvancedModeClick(e)}>
              <svg class="h-5 w-5" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                <path stroke="none" d="M0 0h24v24H0z"/>
                <polyline points="7 8 3 12 7 16"/>
                <polyline points="17 8 21 12 17 16"/>
                <line x1="14" y1="4" x2="10" y2="20"/>
              </svg>
            </button>
          </div>
        </div>
      </div>
      {this.renderEditor()}
      {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
    </div>
  }

  renderEditor() {
    const propertyDescriptor = this.propertyDescriptor;
    const options = propertyDescriptor.options || {};
    const syntaxes = options.syntaxes || ['Literal', 'JavaScript', 'Liquid'];
    const selectedSyntax = this.selectedSyntax ?? (syntaxes.length > 0 ? syntaxes[0] : '');
    const fieldName = propertyDescriptor.name;
    const value = this.currentValue;
    const advancedMode = this.advancedMode;
    const expressionEditorClass = advancedMode ? 'block' : 'hidden';
    const defaultEditorClass = advancedMode ? 'hidden' : 'block';

    return (
      [
        <div class={expressionEditorClass}>
          <elsa-expression-editor fieldName={fieldName} expression={value} syntaxes={syntaxes} syntax={selectedSyntax} editorHeight={this.editorHeight} singleLineMode={this.singleLineMode} context={this.context}/>
        </div>,
        <div class={defaultEditorClass}>
          <slot/>
        </div>
      ]
    );
  }
}
