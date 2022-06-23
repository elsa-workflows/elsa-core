import {Component, h, Prop, State} from '@stencil/core';
import {
  ActivityDefinitionProperty, ActivityModel,
  ActivityPropertyDescriptor,
  RuntimeSelectListProviderSettings,
  SelectList,
  SelectListItem,
  SyntaxNames
} from "../../../../models";
import Tunnel from "../../../../data/workflow-editor";
import {getSelectListItems} from "../../../../utils/select-list-items";

@Component({
  tag: 'elsa-dropdown-property',
  shadow: false,
})
export class ElsaDropdownProperty {

  @Prop() activityModel: ActivityModel;
  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({mutable: true}) serverUrl: string;
  @State() currentValue?: string;

  selectList: SelectList = {items: [], isFlagsEnum: false};


  async componentWillLoad() {
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.currentValue = this.propertyModel.expressions[defaultSyntax] || undefined;

    if (this.propertyDescriptor.options?.context.dependsOnEvent) {
      const initialDepsValue = {};
      let index = 0;

      for (const dependsOnEvent of this.propertyDescriptor.options.context.dependsOnEvent) {

        const element = dependsOnEvent.element;
        const array = dependsOnEvent.array;

        this.activityModel.properties.forEach((value, index, array) => {
          initialDepsValue[value.name] = value.expressions["Literal"];
        });

        // Listen for change events on the dropdown list.
        const dependentInputElement = await this.awaitElement('#' + element);

        // Setup a change handler for when the user changes the selected dropdown list item.
        dependentInputElement.addEventListener('change', async e => await this.ReloadSelectListFromDeps(e));

        index++;
      }

      let options: RuntimeSelectListProviderSettings = {
        context: {
          ...this.propertyDescriptor.options.context,
          depValues: initialDepsValue
        },
        runtimeSelectListProviderType: (this.propertyDescriptor.options as RuntimeSelectListProviderSettings).runtimeSelectListProviderType

      }
      this.selectList = await getSelectListItems(this.serverUrl, {options: options} as ActivityPropertyDescriptor);
      if (this.currentValue == undefined) {
        const firstOption: any = this.selectList.items[0];

        if (firstOption) {
          const optionIsObject = typeof (firstOption) == 'object';
          this.currentValue = optionIsObject ? firstOption.value : firstOption.toString();
        }
      }

    } else {
      this.selectList = await getSelectListItems(this.serverUrl, this.propertyDescriptor);


      if (this.currentValue == undefined) {
        const firstOption: any = this.selectList.items[0];

        if (firstOption) {
          const optionIsObject = typeof (firstOption) == 'object';
          this.currentValue = optionIsObject ? firstOption.value : firstOption.toString();
        }
      }
    }
  }


  async ReloadSelectListFromDeps(e) {
    let depValues = {};

    for (const dependsOnValue of this.propertyDescriptor.options.context.dependsOnValue) {
      const element = dependsOnValue.element;

      let value = this.activityModel.properties.find((prop) => {
        return prop.name == element;
      })
      depValues[element] = value.expressions["Literal"];
    }

    // Need to get the latest value of the component that just changed.
    // For this we need to get the value from the event.
    depValues[e.currentTarget.id] = e.currentTarget.value;

    let options: RuntimeSelectListProviderSettings = {
      context: {
        ...this.propertyDescriptor.options.context,
        depValues: depValues
      },
      runtimeSelectListProviderType: (this.propertyDescriptor.options as RuntimeSelectListProviderSettings).runtimeSelectListProviderType
    }

    this.selectList = await getSelectListItems(this.serverUrl, {options: options} as ActivityPropertyDescriptor);

    const firstOption: any = this.selectList.items[0];
    let currentSelectList = await this.awaitElement('#' + this.propertyDescriptor.name);

    if (firstOption) {
      const optionIsObject = typeof (firstOption) == 'object';
      this.currentValue = optionIsObject ? firstOption.value : firstOption.toString();

      // Rebuild the dropdown list to avoid issue between dispatchevent vs render() for the content of the HTMLElement.
      currentSelectList.innerHTML = "";
      for (const prop of this.selectList.items) {
        let item: any = prop;
        const optionIsObject = typeof (item) == 'object';
        const selected = (optionIsObject ? item.value : item.toString()) === this.currentValue;
        const option = new Option(optionIsObject ? item.text : item.toString(), optionIsObject ? item.value : item.toString(), selected, selected);
        currentSelectList.options.add(option);
      }
    }
    // Dispatch event to the next dependant input.
    currentSelectList.dispatchEvent(new Event("change"));
  }

  awaitElement = async selector => {
    while (document.querySelector(selector) === null) {
      await new Promise(resolve => requestAnimationFrame(resolve))
    }
    return document.querySelector(selector);
  };

  onChange(e: Event) {
    const select = (e.target as HTMLSelectElement);
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.propertyModel.expressions[defaultSyntax] = this.currentValue = select.value;
  }

  onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    let currentValue = this.currentValue;
    const {items} = this.selectList;

    if (currentValue == undefined) {
      const defaultValue = this.propertyDescriptor.defaultValue;
      currentValue = defaultValue ? defaultValue.toString() : undefined;
    }

    return (
      <elsa-property-editor
        activityModel={this.activityModel}
        propertyDescriptor={propertyDescriptor}
        propertyModel={propertyModel}
        onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
        single-line={true}>
        <select id={fieldId}
                name={fieldName}
                onChange={e => this.onChange(e)}
                class="elsa-mt-1 elsa-block focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-w-full elsa-shadow-sm sm:elsa-max-w-xs sm:elsa-text-sm elsa-border-gray-300 elsa-rounded-md">
          {items.map(item => {
            const optionIsObject = typeof (item) == 'object';
            const value = optionIsObject ? item.value : item.toString();
            const text = optionIsObject ? item.text : item.toString();
            return <option value={value} selected={value === currentValue}>{text}</option>;
          })}
        </select>
      </elsa-property-editor>
    );
  }
}

Tunnel.injectProps(ElsaDropdownProperty, ['serverUrl']);
